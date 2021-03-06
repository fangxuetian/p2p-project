﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Agp2p.Common;
using Agp2p.Core.Message;
using Agp2p.Linq2SQL;
using Microsoft.AspNet.SignalR;

namespace Agp2p.Core.NotifyLogic
{
    class ManagerNotifier
    {
        internal static void DoSubscribe()
        {
            MessageBus.Main.Subscribe<NewUserCreatedMsg>(t => HandleNewUserCreatedMsg(t.UserId, t.RegTime));
            MessageBus.Main.Subscribe<BankTransactionFinishedMsg>(t => HandleBankTransactionFinishedMsg(t.Transaction));
            MessageBus.Main.Subscribe<BankTransactionCreatedMsg>(t => HandleBankTransactionCreatedMsg(t.Transaction));
            MessageBus.Main.Subscribe<UserInvestedMsg>(t => HandleUserInvestedMsg(t.ProjectTransactionId, t.InvestTime));
            MessageBus.Main.Subscribe<ProjectFinancingCompletedMsg>(t => HandleProjectFinancingCompletedMsg(t.ProjectId));
            MessageBus.Main.Subscribe<ProjectFinancingTimeoutMsg>(t => HandleProjectFinancingTimeoutMsg(t.ProjectId));
            MessageBus.Main.Subscribe<ProjectFinancingCompleteEvenTimeoutMsg>(t => HandleProjectFinancingCompleteEvenTimeoutMsg(t.ProjectId));
            MessageBus.Main.Subscribe<ProjectFinancingFailMsg>(t => HandleProjectFinancingFailMsg(t.ProjectId));
            MessageBus.Main.Subscribe<ProjectRepaidMsg>(t => HandleProjectRepaidMsg(t.RepaymentTaskId));

            MessageBus.Main.Subscribe<HuoqiWithdrawMsg>(t => HandleHuoqiWithdraw(t.UserId, t.ProjectId, t.Amount));
        }

        private static void HandleHuoqiWithdraw(int userId, int projectId, decimal amount)
        {
            var context = new Agp2pDataContext();

            // 找出所有需要赎回的债权，判断中间人的余额是否足够支付，并提醒中间人
            var needTransferClaims = context.li_claims.Where(c =>
                    c.profitingProjectId == projectId && c.status == (int) Agp2pEnums.ClaimStatusEnum.NeedTransfer &&
                    !c.Children.Any()).ToList();

            needTransferClaims.GroupBy(c => c.dt_users_agent).ToDictionary(g => g.Key, g => g.Sum(c => c.principal)).ForEach(
                pair =>
                {
                    var msgContent = $"有用户进行了活期转出，目前总转出金额为 {pair.Value}，您的余额为 {pair.Key.li_wallets.idle_money}，请于一日内保证账号内的余额足以赎回活期债权";
                    try
                    {
                        var errorMsg = string.Empty;
                        if (!SMSHelper.SendTemplateSms(pair.Key.mobile, msgContent, out errorMsg))
                        {
                            context.AppendAdminLogAndSave("Huoqi",
                                $"发送活期转出提醒失败：{errorMsg}，中间人：{pair.Key.GetFriendlyUserName()}，短信内容：{msgContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        context.AppendAdminLogAndSave("Huoqi",
                            $"发送活期转出提醒失败：{ex.GetSimpleCrashInfo()}，中间人：{pair.Key.GetFriendlyUserName()}，短信内容：{msgContent}");
                    }
                });
        }

        private static IQueryable<dt_manager> GetMessageSubscribers(Agp2pDataContext context, Agp2pEnums.ManagerMessageSourceEnum source)
        {
            return context.dt_manager.Where(ma => !ma.li_manager_notification_settings.Select(se => se.source).Contains((int) source));
        }

        private static void HandleProjectRepaidMsg(int repaymentTaskId)
        {
            var context = new Agp2pDataContext();
            var repaymentTask = context.li_repayment_tasks.Single(task => task.id == repaymentTaskId);

            var content = repaymentTask.li_projects.IsNewbieProject1()
                ? string.Format("新手标于 {0} 回款本金 {1}，利息 {2} 到 {3}", repaymentTask.repay_at, repaymentTask.repay_principal,
                    repaymentTask.repay_interest, repaymentTask.dt_users.GetFriendlyUserName())
                : string.Format("项目 {0} 已于 {1} 进行了第 {2} 期回款，共计回款本金：{3}，利息：{4}",
                    repaymentTask.li_projects.title, repaymentTask.repay_at, repaymentTask.GetRepaymentTaskProgress(),
                    repaymentTask.repay_principal, repaymentTask.repay_interest);

            GetMessageSubscribers(context, Agp2pEnums.ManagerMessageSourceEnum.ProjectRepaid).ForEach(m =>
            {
                context.AppendAdminMessage(m, Agp2pEnums.ManagerMessageSourceEnum.ProjectRepaid, "项目回款", content, repaymentTask.repay_at.Value);
            });
            context.SubmitChanges();
            ManagerMessageHubFacade.Instance.OnNewMsg();
        }

        private static void HandleProjectFinancingFailMsg(int projectId)
        {
            var context = new Agp2pDataContext();
            var proj = context.li_projects.Single(p => p.id == projectId);

            var createTime = DateTime.Now;
            var content = $"项目 {proj.title} 于 {createTime} 流标，款项已退还给 {proj.GetInvestedUserCount(Agp2pEnums.ProjectTransactionStatusEnum.Rollback)} 个投资者";

            GetMessageSubscribers(context, Agp2pEnums.ManagerMessageSourceEnum.ProjectFinancingFail).ForEach(m =>
            {
                context.AppendAdminMessage(m, Agp2pEnums.ManagerMessageSourceEnum.ProjectFinancingFail, "项目流标", content, createTime);
            });
            context.SubmitChanges();
            ManagerMessageHubFacade.Instance.OnNewMsg();
        }

        private static void HandleProjectFinancingTimeoutMsg(int projectId)
        {
            var context = new Agp2pDataContext();
            var proj = context.li_projects.Single(p => p.id == projectId);

            var createTime = DateTime.Now;
            var content = $"项目 {proj.title} 于 {createTime} 融资超时";

            GetMessageSubscribers(context, Agp2pEnums.ManagerMessageSourceEnum.ProjectFinancingTimeout).ForEach(m =>
            {
                context.AppendAdminMessage(m, Agp2pEnums.ManagerMessageSourceEnum.ProjectFinancingTimeout, "项目融资超时", content, createTime);
            });
            context.SubmitChanges();
            ManagerMessageHubFacade.Instance.OnNewMsg();
        }

        private static void HandleProjectFinancingCompleteEvenTimeoutMsg(int projectId)
        {
            var context = new Agp2pDataContext();
            var proj = context.li_projects.Single(p => p.id == projectId);

            var createTime = DateTime.Now;
            var content = $"项目 {proj.title} 于 {createTime} 截标, 实际可投 {proj.financing_amount}，实际已投 {proj.investment_amount}，进度 {proj.GetInvestmentProgressPercent().ToString("p1")}";

            GetMessageSubscribers(context, Agp2pEnums.ManagerMessageSourceEnum.ProjectFinancingSuccessEvenTimeout).ForEach(m =>
            {
                context.AppendAdminMessage(m, Agp2pEnums.ManagerMessageSourceEnum.ProjectFinancingSuccessEvenTimeout, "项目截标", content, createTime);
            });
            context.SubmitChanges();
            ManagerMessageHubFacade.Instance.OnNewMsg();
        }

        private static void HandleProjectFinancingCompletedMsg(int projectId)
        {
            var context = new Agp2pDataContext();
            var proj = context.li_projects.Single(p => p.id == projectId);

            var createTime = DateTime.Now;
            var content = $"项目 {proj.title} 于 {createTime} 满标";

            GetMessageSubscribers(context, Agp2pEnums.ManagerMessageSourceEnum.ProjectFinancingSuccess).ForEach(m =>
            {
                context.AppendAdminMessage(m, Agp2pEnums.ManagerMessageSourceEnum.ProjectFinancingSuccess, "项目满标", content, createTime);
            });
            context.SubmitChanges();
            ManagerMessageHubFacade.Instance.OnNewMsg();
        }

        private static void HandleUserInvestedMsg(int projectTransactionId, DateTime investTime)
        {
            var context = new Agp2pDataContext();
            var tr = context.li_project_transactions.Single(ptr => ptr.id == projectTransactionId);

            var createTime = DateTime.Now;
            var content = $"用户 {tr.dt_users.GetFriendlyUserName()} 投资了 {tr.principal} 到项目 {tr.li_projects.title}，目前其进度为 {tr.li_projects.GetInvestmentProgressPercent().ToString("p1")}";

            GetMessageSubscribers(context, Agp2pEnums.ManagerMessageSourceEnum.UserInvested).ForEach(m =>
            {
                context.AppendAdminMessage(m, Agp2pEnums.ManagerMessageSourceEnum.UserInvested, "用户投标", content, createTime);
            });
            context.SubmitChanges();

            ManagerMessageHubFacade.Instance.OnNewMsg();
        }

        private static void HandleNewUserCreatedMsg(int userId, DateTime regTime)
        {
            var context = new Agp2pDataContext();
            var user = context.dt_users.Single(u => u.id == userId);

            var createTime = regTime;
            var content = $"于 {createTime} 有新用户注册了：{user.GetFriendlyUserName()}";

            GetMessageSubscribers(context, Agp2pEnums.ManagerMessageSourceEnum.NewUserRegisted).ForEach(m =>
            {
                context.AppendAdminMessage(m, Agp2pEnums.ManagerMessageSourceEnum.NewUserRegisted, "新用户注册", content, createTime);
            });
            context.SubmitChanges();
            ManagerMessageHubFacade.Instance.OnNewMsg();
        }

        private static void HandleBankTransactionFinishedMsg(li_bank_transactions tr)
        {
            if (tr.type != (int)Agp2pEnums.BankTransactionTypeEnum.Charge || tr.status != (int)Agp2pEnums.BankTransactionStatusEnum.Confirm)
            {
                return;
            }

            var context = new Agp2pDataContext();

            var createTime = tr.create_time;
            var content = $"用户 {tr.dt_users.GetFriendlyUserName()} 于 {createTime} 成功充值了 {tr.value}";

            GetMessageSubscribers(context, Agp2pEnums.ManagerMessageSourceEnum.UserRechargeSuccess).ForEach(m =>
            {
                context.AppendAdminMessage(m, Agp2pEnums.ManagerMessageSourceEnum.UserRechargeSuccess, "用户充值成功", content, createTime);
            });
            context.SubmitChanges();
            ManagerMessageHubFacade.Instance.OnNewMsg();
        }

        private static void HandleBankTransactionCreatedMsg(li_bank_transactions tr)
        {
            if (tr.type != (int) Agp2pEnums.BankTransactionTypeEnum.Withdraw)
            {
                return;
            }

            var context = new Agp2pDataContext();

            var createTime = tr.create_time;
            var content = $"用户 {tr.li_bank_accounts.dt_users.GetFriendlyUserName()} 于 {createTime} 申请了提现 {tr.value}";

            GetMessageSubscribers(context, Agp2pEnums.ManagerMessageSourceEnum.UserWithdrawApply).ForEach(m =>
            {
                context.AppendAdminMessage(m, Agp2pEnums.ManagerMessageSourceEnum.UserWithdrawApply, "用户申请提现", content, createTime);
            });
            context.SubmitChanges();
            ManagerMessageHubFacade.Instance.OnNewMsg();
        }
    }
}
