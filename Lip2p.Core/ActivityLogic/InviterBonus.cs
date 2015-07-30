﻿using System;
using System.Data.Linq;
using System.Linq;
using Lip2p.Common;
using Lip2p.Core.Message;
using Lip2p.Linq2SQL;
using Newtonsoft.Json;

namespace Lip2p.Core.ActivityLogic
{
    // 如果是首次投资，并且有邀请人，则其可额外奖励利润的 10%
    class InviterBonus
    {
        internal static void DoSubscribe()
        {
            MessageBus.Main.Subscribe<UserInvestedMsg>(m => HandleUserInvestedMsg(m.ProjectTransactionId)); // 如果有邀请人，则标记投资记录
            MessageBus.Main.Subscribe<ProjectInvestCompletedMsg>(m => HandleProjectInvestCompletedMsg(m.ProjectId)); // 推荐人收益已知，创建活动交易记录
            MessageBus.Main.Subscribe<ProjectRepayCompletedMsg>(m => HandleProjectRepayCompletedMsg(m.ProjectId, m.ProjectCompleteTime)); // 项目放款完成，发放推荐人奖金
        }

        private static void HandleProjectRepayCompletedMsg(int projectId, DateTime projectCompleteTime)
        {
            // 找出投资记录对应的活动奖励记录
            var context = new Lip2pDataContext();

            var invitees = context.li_invitations.Where(i => i.li_project_transactions.project == projectId).Select(i => i.dt_users1).Distinct();
            var atr = invitees.Select(i =>
                    i.li_activity_transactions.Single(a =>
                            a.activity_type == (int) Lip2pEnums.ActivityTransactionActivityTypeEnum.RefereeFirstTimeProfitBonus))
                            .ToList(); // 查出邀请人的奖励活动交易记录
            if (!atr.Any()) return;

            atr.ForEach(tr =>
            {
                tr.transact_time = projectCompleteTime;
                tr.status = (byte) Lip2pEnums.ActivityTransactionStatusEnum.Confirm;

                var wallet = tr.dt_users.li_wallets;
                wallet.idle_money += tr.value;
                wallet.last_update_time = projectCompleteTime;

                var his = TransactionFacade.CloneFromWallet(wallet, Lip2pEnums.WalletHistoryTypeEnum.GainConfirm);
                his.li_activity_transactions = tr;
                context.li_wallet_histories.InsertOnSubmit(his);
            });
            context.SubmitChanges();
        }

        private static void HandleProjectInvestCompletedMsg(int projectId)
        {
            var context = new Lip2pDataContext();

            var options = new DataLoadOptions();
            options.LoadWith<li_projects>(p => p.li_project_transactions);
            options.LoadWith<li_project_transactions>(p => p.dt_users);
            options.LoadWith<dt_users>(p => p.li_invitations);
            context.LoadOptions = options;

            // 为推荐人创建 activity_transaction
            var invs = context.li_invitations.Where(i => i.li_project_transactions.project == projectId).ToList();
            if (!invs.Any()) return;

            var project = context.li_projects.Single(p => p.id == projectId);
            var newComerIDs = invs.Select(i => i.user_id).ToList();
            var firstInvestProfits = project.li_project_transactions.Where(
                tr =>
                    newComerIDs.Contains(tr.investor) &&
                    tr.type == (int) Lip2pEnums.ProjectTransactionTypeEnum.Invest &&
                    tr.status == (int) Lip2pEnums.ProjectTransactionStatusEnum.Success)
                .ToLookup(t => t.dt_users)
                .ToDictionary(t => t.Key.li_invitations, t => t.First().value*project.profit_rate);

            var profitDate = project.li_repayment_tasks.Max(t => t.should_repay_time);
            var atr = firstInvestProfits.ToList().Select(d => new li_activity_transactions // 邀请人创建 activity_transaction
            {
                user_id = d.Key.inviter,
                create_time = project.invest_complete_time.Value,
                value = d.Value/10,
                remarks =
                    string.Format("好友 {0} 首次投资获利 {1:c}，你于 {2} 得到 {3:c}",
                        string.IsNullOrWhiteSpace(d.Key.dt_users.real_name)
                            ? d.Key.dt_users.user_name
                            : d.Key.dt_users.real_name, d.Value, profitDate.ToString("yyyy-MM-dd"), d.Value/10),
                details = JsonConvert.SerializeObject(new {Invitee = d.Key.user_id}), // 记录被邀请人
                type = (byte) Lip2pEnums.ActivityTransactionTypeEnum.Gain,
                status = (byte) Lip2pEnums.ActivityTransactionStatusEnum.Acting,
                activity_type = (byte) Lip2pEnums.ActivityTransactionActivityTypeEnum.RefereeFirstTimeProfitBonus,
            });
            context.li_activity_transactions.InsertAllOnSubmit(atr);

            context.SubmitChanges();
        }

        private static void HandleUserInvestedMsg(int projectTransactionId)
        {
            var now = DateTime.Now;
            var startTime = new DateTime(2015, 5, 3);
            if (startTime <= now.Date /*&& now.Date <= startTime.AddMonths(6)*/) // 判断是否活动期间
            {
                var context = new Lip2pDataContext();
                var options = new DataLoadOptions();
                options.LoadWith<li_project_transactions>(p => p.dt_users);
                options.LoadWith<dt_users>(p => p.li_invitations);
                context.LoadOptions = options;

                var investment = context.li_project_transactions.Single(tr => tr.id == projectTransactionId);

                // 用户只能有一个邀请人，所有一对一关系就是被邀请记录，一对多关系的是邀请记录
                var investor = investment.dt_users;
                if (investor.li_invitations == null || investor.li_invitations.li_project_transactions != null)
                    return; // 没有邀请人或已经记录过首次投资

                investor.li_invitations.li_project_transactions = investment;
                // 利润和收益时间需要等满标后才能计算
                context.SubmitChanges();
            }
        }
    }
}
