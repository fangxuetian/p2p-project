﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Agp2p.Common;
using Agp2p.Core.Message;
using Agp2p.Linq2SQL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Agp2p.Core
{
    /// <summary>
    /// 事务门面类；支持充值/提现，充值/提现成功确认，投资和收益功能
    /// 也支持查询项目的投资进度的功能
    /// </summary>
    public static class TransactionFacade
    {
        public const decimal StandGuardFeeRate = 0;//0.006m;
        public const decimal DefaultHandlingFee = 0;//1;

        internal static void DoSubscribe()
        {
            MessageBus.Main.Subscribe<UserInvestedMsg>(m => CheckFinancingComplete(m.ProjectTransactionId)); // 项目满标需要生成还款计划
            MessageBus.Main.Subscribe<ProjectRepaidMsg>(m =>
            {
                if (m.IsProjectNeedComplete)
                {
                    CompleteProject(m.RepaymentTaskId);
                }
            });
        }

        /// <summary>
        /// 充值（待确认），等待充值成功的通知
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userId"></param>
        /// <param name="money"></param>
        /// <param name="payApi"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        public static li_bank_transactions Charge(this Agp2pDataContext context, int userId, decimal money,
            Agp2pEnums.PayApiTypeEnum payApi, string remark = null)
        {
            // 创建交易记录（充值进行中）
            var tr = new li_bank_transactions
            {
                charger = userId,
                transact_time = null,
                type = (int) Agp2pEnums.BankTransactionTypeEnum.Charge,
                status = (int) Agp2pEnums.BankTransactionStatusEnum.Acting,
                value = money,
                handling_fee = 0,
                handling_fee_type = (byte) Agp2pEnums.BankTransactionHandlingFeeTypeEnum.NoHandlingFee,
                no_order = Utils.GetOrderNumberLonger(),
                create_time = DateTime.Now,
                remarks = remark,
                pay_api = (byte) payApi
            };
            context.li_bank_transactions.InsertOnSubmit(tr);

            // 增加冻结资金
            var wallet = context.li_wallets.Single(b => b.user_id == userId);
            //wallet.locked_money += money;
            wallet.last_update_time = tr.create_time;

            // 创建交易历史
            var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.Charging);
            his.li_bank_transactions = tr;
            context.li_wallet_histories.InsertOnSubmit(his);

            context.SubmitChanges();

            MessageBus.Main.Publish(new BankTransactionCreatedMsg(tr));
            return tr;
        }

        /// <summary>
        /// 提现（待确认），等待提现成功的通知
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bankAccountId"></param>
        /// <param name="withdrawMoney"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        public static li_bank_transactions Withdraw(this Agp2pDataContext context, int bankAccountId,
            decimal withdrawMoney, string remark = null)
        {
            // 提现 100 起步，5w 封顶
            if (withdrawMoney < 100)
                throw new InvalidOperationException("操作失败：提现金额最低 100 元");
            if (50000 < withdrawMoney)
                throw new InvalidOperationException("操作失败：提现金额最高 50000 元");
            // 查询可用余额，足够的话才能提现
            var account = context.li_bank_accounts.Single(b => b.id == bankAccountId);
            var user = account.dt_users;
            var wallet = user.li_wallets;
            if (wallet.idle_money < withdrawMoney)
                throw new InvalidOperationException("操作失败：用户 " + user.user_name + " 的账户余额小于需要提现的金额");

            // 判断提现次数，每日每张卡的提现次数不能超过 3 次
            if (3 <= account.li_bank_transactions.Count(card => card.create_time.Date == DateTime.Today) && !Utils.IsDebugging())
            {
                throw new InvalidOperationException("每日每张卡的提现次数不能超过 3 次");
            }

            // 计算出产生防套现手续费的部分 (空闲 - 未投资 = 回款，提现回款金额无需手续费)
            var unusedMoney = wallet.idle_money - wallet.unused_money <= withdrawMoney
                ? wallet.unused_money - (wallet.idle_money - withdrawMoney)
                : 0;

            // 申请提现后将可用余额冻结
            wallet.idle_money -= withdrawMoney;
            wallet.locked_money += withdrawMoney;
            wallet.unused_money -= unusedMoney;
            wallet.last_update_time = DateTime.Now;

            // 创建交易记录（提现进行中）
            var tr = new li_bank_transactions
            {
                withdraw_account = bankAccountId,
                transact_time = null,
                type = (int) Agp2pEnums.BankTransactionTypeEnum.Withdraw,
                status = (int) Agp2pEnums.BankTransactionStatusEnum.Acting,
                value = withdrawMoney,
                // 防套现手续费公式：未投资金额 * 0.6%；有防提现手续费时不能在数据库里面直接设置默认的手续费(1元)，因为提现取消的时候需要靠这个数来恢复未投资金额
                handling_fee = unusedMoney == 0 ? DefaultHandlingFee : unusedMoney*StandGuardFeeRate,
                handling_fee_type =
                    (byte)
                        (unusedMoney == 0
                            ? Agp2pEnums.BankTransactionHandlingFeeTypeEnum.WithdrawHandlingFee
                            : Agp2pEnums.BankTransactionHandlingFeeTypeEnum.WithdrawUnusedMoneyHandlingFee),
                no_order = Utils.GetOrderNumberLonger(),
                remarks = remark,
                create_time = wallet.last_update_time // 时间应该一致
            };
            context.li_bank_transactions.InsertOnSubmit(tr);

            // 创建交易历史
            var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.Withdrawing);
            his.li_bank_transactions = tr;
            context.li_wallet_histories.InsertOnSubmit(his);

            context.SubmitChanges();

            MessageBus.Main.Publish(new BankTransactionCreatedMsg(tr));
            return tr;
        }

        /// <summary>
        /// 计算站岗手续费（防套现手续费）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userId"></param>
        /// <param name="withdrawMoney"></param>
        /// <returns></returns>
        public static decimal CalcStandGuardFee(this Agp2pDataContext context, int userId, decimal withdrawMoney)
        {
            var wallet = context.li_wallets.Single(w => w.user_id == userId);
            if (wallet.idle_money < withdrawMoney)
            {
                throw new Exception("提现金额超出用户余额");
            }
            // 计算出产生防套现手续费的部分 (空闲 - 未投资 = 回款，提现回款金额无需手续费)
            var unusedMoney = wallet.idle_money - wallet.unused_money <= withdrawMoney
                ? wallet.unused_money - (wallet.idle_money - withdrawMoney)
                : 0;
            return unusedMoney*StandGuardFeeRate;
        }

        /// <summary>
        /// 确认银行交易
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bankTransactionId"></param>
        /// <param name="approver"></param>
        /// <param name="saveChange"></param>
        /// <returns></returns>
        public static li_bank_transactions ConfirmBankTransaction(this Agp2pDataContext context, int bankTransactionId,
            int? approver, bool saveChange = true)
        {
            // 更新原事务（完成事务）
            var tr = context.li_bank_transactions.Single(t => t.id == bankTransactionId);
            if (tr.status != (int) Agp2pEnums.BankTransactionStatusEnum.Acting)
                throw new InvalidOperationException("该银行卡" +
                                                    Utils.GetAgp2pEnumDes((Agp2pEnums.BankTransactionTypeEnum) tr.type) +
                                                    "事务已经被确认或取消了");
            tr.status = (byte) Agp2pEnums.BankTransactionStatusEnum.Confirm;
            tr.transact_time = DateTime.Now;
            tr.approver = approver;

            if (tr.type == (int) Agp2pEnums.BankTransactionTypeEnum.Charge) // 充值确认
            {
                var wallet = tr.dt_users.li_wallets;
                // 修改钱包金额
                //wallet.locked_money -= tr.value;
                wallet.idle_money += tr.value;
                wallet.unused_money += (tr.pay_api == (byte) Agp2pEnums.PayApiTypeEnum.ManualAppend ? 0 : tr.value);
                // 手工充值 可能为活动返利，不计手续费
                wallet.total_charge += tr.value;
                wallet.last_update_time = tr.transact_time.Value; // 时间应该一致

                // 创建交易历史
                var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.ChargeConfirm);
                his.li_bank_transactions = tr;
                context.li_wallet_histories.InsertOnSubmit(his);

                //添加充值手续费
                //汇潮支付
                if (tr.pay_api != null && (tr.pay_api == (int)Agp2pEnums.PayApiTypeEnum.EcpssQ || tr.pay_api == (int)Agp2pEnums.PayApiTypeEnum.Ecpss))
                {
                    var rechangerFee = new li_company_inoutcome()
                    {
                        create_time = DateTime.Now,
                        user_id = (int) tr.charger,
                        outcome = tr.pay_api == (int)Agp2pEnums.PayApiTypeEnum.EcpssQ ? tr.value*0.005m : tr.value * 0.0025m,
                        type = (int)Agp2pEnums.OfflineTransactionTypeEnum.ReChangeFee,
                        remark = Utils.GetAgp2pEnumDes((Agp2pEnums.PayApiTypeEnum)tr.pay_api) + "充值手续费"
                    };
                    context.li_company_inoutcome.InsertOnSubmit(rechangerFee);
                }
                context.SubmitChanges();
            }
            else if (tr.type == (int) Agp2pEnums.BankTransactionTypeEnum.Withdraw) // 提款确认
            {
                var wallet = tr.li_bank_accounts.dt_users.li_wallets;
                // 修改钱包金额
                wallet.locked_money -= tr.value;
                wallet.total_withdraw += tr.value;
                wallet.last_update_time = tr.transact_time.Value;

                // 创建交易历史
                var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.WithdrawConfirm);
                his.li_bank_transactions = tr;
                context.li_wallet_histories.InsertOnSubmit(his);
            }
            else throw new InvalidEnumArgumentException("未知银行交易类型");
            if (saveChange) // 注意：外部保存话需要自己发送通知
            {
                context.SubmitChanges();
                MessageBus.Main.Publish(new BankTransactionFinishedMsg(tr));
            }
            return tr;
        }

        /// <summary>
        /// 取消银行交易
        /// </summary>
        /// <param name="context"></param>
        /// <param name="bankTransactionId"></param>
        /// <param name="approver"></param>
        /// <param name="saveChange"></param>
        /// <returns></returns>
        public static li_bank_transactions CancelBankTransaction(this Agp2pDataContext context, int bankTransactionId,
            int approver, bool saveChange = true)
        {
            // 更新原事务（完成事务）
            var tr = context.li_bank_transactions.Single(t => t.id == bankTransactionId);
            if (tr.status != (int) Agp2pEnums.BankTransactionStatusEnum.Acting)
                throw new InvalidOperationException("该银行卡" +
                                                    Utils.GetAgp2pEnumDes((Agp2pEnums.BankTransactionTypeEnum) tr.type) +
                                                    "事务已经被确认或取消了");
            tr.status = (byte) Agp2pEnums.BankTransactionStatusEnum.Cancel;
            tr.transact_time = DateTime.Now;
            tr.approver = approver;

            if (tr.type == (int) Agp2pEnums.BankTransactionTypeEnum.Charge) // 充值取消
            {
                var wallet = tr.dt_users.li_wallets;
                // 修改钱包金额
                //wallet.locked_money -= tr.value;
                wallet.last_update_time = tr.transact_time.Value; // 时间应该一致

                // 创建交易历史
                var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.ChargeCancel);
                his.li_bank_transactions = tr;
                context.li_wallet_histories.InsertOnSubmit(his);
            }
            else if (tr.type == (int) Agp2pEnums.BankTransactionTypeEnum.Withdraw) // 提款取消
            {
                var wallet = tr.li_bank_accounts.dt_users.li_wallets;
                // 修改钱包金额
                wallet.locked_money -= tr.value;
                wallet.idle_money += tr.value;
                if (tr.handling_fee_type ==
                    (int) Agp2pEnums.BankTransactionHandlingFeeTypeEnum.WithdrawUnusedMoneyHandlingFee)
                {
                    wallet.unused_money += StandGuardFeeRate == 0 ? 0 : tr.handling_fee/StandGuardFeeRate; // 恢复防套现手续费的部分
                }
                wallet.last_update_time = tr.transact_time.Value;

                // 创建交易历史
                var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.WithdrawCancel);
                his.li_bank_transactions = tr;
                context.li_wallet_histories.InsertOnSubmit(his);
            }
            if (saveChange) // 注意：外部保存话需要自己发送通知
            {
                context.SubmitChanges();
                MessageBus.Main.Publish(new BankTransactionFinishedMsg(tr));
            }
            return tr;
        }

        /// <summary>
        /// 根据钱包数据生成钱包历史
        /// </summary>
        /// <param name="wallet"></param>
        /// <param name="actionType"></param>
        /// <returns></returns>
        public static li_wallet_histories CloneFromWallet(li_wallets wallet, Agp2pEnums.WalletHistoryTypeEnum actionType)
        {
            return new li_wallet_histories
            {
                user_id = wallet.user_id,
                action_type = (byte) actionType,
                idle_money = wallet.idle_money,
                locked_money = wallet.locked_money,
                investing_money = wallet.investing_money,
                profiting_money = wallet.profiting_money,
                total_investment = wallet.total_investment,
                total_profit = wallet.total_profit,
                create_time = wallet.last_update_time
            };
        }

        /// <summary>
        /// 投资
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userId"></param>
        /// <param name="projectId"></param>
        /// <param name="investingMoney"></param>
        public static void Invest(this Agp2pDataContext context, int userId, int projectId, decimal investingMoney)
        {
            var pr = context.li_projects.Single(p => p.id == projectId);

            if ((int) Agp2pEnums.ProjectStatusEnum.Financing != pr.status && (int)Agp2pEnums.ProjectStatusEnum.FinancingTimeout != pr.status)
                throw new InvalidOperationException("项目不可投资！");
            // 判断投资金额的数额是否合理
            var canBeInvest = pr.financing_amount - pr.investment_amount;
            if (canBeInvest == 0)
                throw new InvalidOperationException("项目已经投满");
            if (canBeInvest < investingMoney)
                throw new InvalidOperationException("投资金额 " + investingMoney + " 超出项目可投资金额 " + canBeInvest);
            if (investingMoney < 100)
                throw new InvalidOperationException("投资金额最低 100 元");
            if (canBeInvest != investingMoney && canBeInvest - investingMoney < 100)
                throw new InvalidOperationException($"您投标 {investingMoney} 元后项目的可投金额（{canBeInvest - investingMoney}）低于 100 元，这样下一个人就不能投啦，所以请调整你的投标金额");

            // 修改钱包，将金额放到待收资金中，流标后再退回空闲资金
            var wallet = context.li_wallets.Single(w => w.user_id == userId);

            // TODO 解除投资限制
            if (wallet.dt_users.dt_user_groups.title != "普通会员" && wallet.dt_users.dt_user_groups.title != "借款人")
            {
                throw new InvalidOperationException("安广融合平台正在内部试运行中，具体全面开放请留意官网公告。");
            }

            if (wallet.idle_money < investingMoney)
                throw new InvalidOperationException("余额不足，无法投资");

            // 限制对新手体验标的投资，只能投资 100，只能投 1 次
            if (pr.IsNewbieProject())
            {
                if (investingMoney != 100)
                {
                    throw new InvalidOperationException("新手体验标规定只能投 100 元");
                }
                if (wallet.dt_users.li_project_transactions.Any(tra =>
                    tra.li_projects.dt_article_category.call_index == "newbie"
                    && tra.status == (int)Agp2pEnums.ProjectTransactionStatusEnum.Success
                    && tra.type == (int)Agp2pEnums.ProjectTransactionTypeEnum.Invest))
                {
                    throw new InvalidOperationException("你已经投资过新手体验标，不能再投资");
                }
            }
            else if (pr.IsHuoqiProject()) // 限制对活期项目的投资，最大投 5 w
            {
                var alreadyInvest = wallet.dt_users.li_claims.Where(c => c.profitingProjectId == projectId)
                    .Aggregate(0m, (sum, c) => sum + c.principal);
                if (50000 < alreadyInvest + investingMoney)
                {
                    throw new InvalidOperationException("对活期项目最多可投 ¥50,000，你目前已投 " + alreadyInvest.ToString("c"));
                }
            }

            // 修改项目已投资金额
            pr.investment_amount += investingMoney;

            // 满标时再计算待收益金额
            wallet.idle_money -= investingMoney;
            wallet.unused_money -= Math.Min(investingMoney, wallet.unused_money); // 投资的话优先使用未投资金额，再使用回款金额
            wallet.investing_money += investingMoney;
            wallet.total_investment += investingMoney;
            wallet.last_update_time = DateTime.Now;

            // 创建投资记录
            var tr = new li_project_transactions
            {
                dt_users = wallet.dt_users,
                li_projects = pr,
                type = (byte) Agp2pEnums.ProjectTransactionTypeEnum.Invest,
                principal = investingMoney,
                status = (byte) Agp2pEnums.ProjectTransactionStatusEnum.Success,
                create_time = wallet.last_update_time // 时间应该一致
            };
            context.li_project_transactions.InsertOnSubmit(tr);

            // 修改钱包历史
            var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.Invest);
            his.li_project_transactions = tr;
            context.li_wallet_histories.InsertOnSubmit(his);

            if (pr.IsHuoqiProject())
            {
                var exceed = AutoInvestment(context, investingMoney, tr);
                if (exceed != 0)
                {
                    throw new Exception("没有足够的项目可投，超出：" + exceed);
                }
            }
            else
            {
                // 创建债权
                var liClaims = new li_claims
                {
                    li_project_transactions1 = tr,
                    createTime = wallet.last_update_time,
                    projectId = projectId,
                    principal = investingMoney,
                    status = (byte) Agp2pEnums.ClaimStatusEnum.Nontransferable,
                    userId = wallet.user_id,
                    profitingProjectId = projectId,
                    number = Utils.HiResNowString
                };
                context.li_claims.InsertOnSubmit(liClaims);
            }

            context.SubmitChanges();

            MessageBus.Main.PublishAsync(new UserInvestedMsg(tr.id, wallet.last_update_time)); // 广播用户的投资消息
        }

        public static void TakeOverHuoqiProject(this Agp2pDataContext context, dt_users user, li_projects pr, decimal investingMoney)
        {
            if ((int)Agp2pEnums.ProjectStatusEnum.Financing != pr.status && (int)Agp2pEnums.ProjectStatusEnum.FinancingTimeout != pr.status)
                throw new InvalidOperationException("项目不可投资！");
            // 判断投资金额的数额是否合理

            // 修改钱包，将金额放到待收资金中，流标后再退回空闲资金
            var wallet = user.li_wallets;

            if (wallet.idle_money < investingMoney)
                throw new InvalidOperationException("余额不足，无法投资");

            Debug.Assert(pr.IsHuoqiProject());

            // 项目已投资金额不变
            // pr.investment_amount += investingMoney;

            // 修改钱包金额
            wallet.idle_money -= investingMoney;
            wallet.unused_money -= Math.Min(investingMoney, wallet.unused_money); // 投资的话优先使用未投资金额，再使用回款金额
            wallet.investing_money += investingMoney;
            wallet.total_investment += investingMoney;
            wallet.last_update_time = DateTime.Now;

            // 创建投资记录
            var tr = new li_project_transactions
            {
                dt_users = wallet.dt_users,
                li_projects = pr,
                type = (byte)Agp2pEnums.ProjectTransactionTypeEnum.Invest,
                principal = investingMoney,
                status = (byte)Agp2pEnums.ProjectTransactionStatusEnum.Success,
                create_time = wallet.last_update_time // 时间应该一致
            };
            context.li_project_transactions.InsertOnSubmit(tr);

            // 修改钱包历史
            var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.Invest);
            his.li_project_transactions = tr;
            context.li_wallet_histories.InsertOnSubmit(his);

            var exceed = AutoInvestment(context, investingMoney, tr, wallet.last_update_time);
            if (exceed != 0)
            {
                throw new Exception("没有足够的项目可投，超出：" + exceed);
            }

            // MessageBus.Main.PublishAsync(new UserInvestedMsg(tr.id, wallet.last_update_time)); // 广播用户的投资消息
        }

        public static void HuoqiProjectWithdraw(Agp2pDataContext context, int userId, int huoqiProjectId, decimal withdrawMoney)
        {
            // 将活期项目的债权设置为 需要转让
            var user = context.dt_users.Single(u => u.id == userId);
            var huoqiClaims =
                user.li_claims.Where(
                    c =>
                        c.profitingProjectId == huoqiProjectId &&
                        c.status == (int) Agp2pEnums.ClaimStatusEnum.Nontransferable).ToList();
            if (!huoqiClaims.Any())
                throw new InvalidOperationException("您目前没有投资此活期项目，无法提现");

            var withdrawTime = DateTime.Now;
            var sumOfPrincipal = huoqiClaims.Sum(c => c.principal);
            if (sumOfPrincipal < withdrawMoney)
            {
                throw new InvalidOperationException("您提现的金额不能超出您投资的本金：" + sumOfPrincipal.ToString("c"));
            }
            else if (sumOfPrincipal == withdrawMoney)
            {
                // 全部提现
                huoqiClaims.ForEach(c =>
                {
                    c.status = (int) Agp2pEnums.ClaimStatusEnum.NeedTransfer;
                    c.statusUpdateTime = withdrawTime;
                });
            }
            else
            {
                // 部分提现，优先提现接近完成的项目
                var sortedClaims = huoqiClaims.OrderBy(
                    c => c.li_projects.li_repayment_tasks.Last(t =>
                                t.status == (int) Agp2pEnums.RepaymentStatusEnum.Unpaid ||
                                t.status == (int) Agp2pEnums.RepaymentStatusEnum.OverTime).should_repay_time)
                    .ThenBy(c => c.principal)
                    .ToList();
                HuoqiClaimsPartialWithdraw(context, sortedClaims, withdrawMoney, withdrawTime);
            }
            context.SubmitChanges();
        }

        private static void HuoqiClaimsPartialWithdraw(Agp2pDataContext context, IEnumerable<li_claims> claims, decimal withdrawMoney, DateTime withdrawTime)
        {
            if (!claims.Any() || withdrawMoney == 0) return;
            Debug.Assert(0 < withdrawMoney, "提现的金额不能是负数");

            var headClaim = claims.First();
            if (headClaim.principal <= withdrawMoney)
            {
                headClaim.status = (byte) Agp2pEnums.ClaimStatusEnum.NeedTransfer;
                headClaim.statusUpdateTime = withdrawTime;
                HuoqiClaimsPartialWithdraw(context, claims.Skip(1), withdrawMoney - headClaim.principal, withdrawTime);
            }
            else
            {
                // 提现了某个债权的一部分，需要进行拆分
                var remain = new li_claims
                {
                    parentClaimId = headClaim.id,
                    createTime = withdrawTime,
                    principal = headClaim.principal - withdrawMoney,
                    userId = headClaim.userId,
                    status = (byte) Agp2pEnums.ClaimStatusEnum.Nontransferable,
                    projectId = headClaim.projectId,
                    profitingProjectId = headClaim.profitingProjectId,
                    li_project_transactions = headClaim.li_project_transactions,
                    number = headClaim.number
                };
                context.li_claims.InsertOnSubmit(remain);

                var splited = new li_claims
                {
                    parentClaimId = headClaim.id,
                    createTime = withdrawTime,
                    principal = withdrawMoney,
                    userId = headClaim.userId,
                    status = (byte)Agp2pEnums.ClaimStatusEnum.NeedTransfer,
                    projectId = headClaim.projectId,
                    profitingProjectId = headClaim.profitingProjectId,
                    li_project_transactions = headClaim.li_project_transactions,
                    number = headClaim.number
                };
                context.li_claims.InsertOnSubmit(splited);

                headClaim.status = (byte) Agp2pEnums.ClaimStatusEnum.Invalid;
                headClaim.statusUpdateTime = withdrawTime;
            }
        }

        /*TODO 活期项目：
            1、固定资金100万，100元起投，每个客户最大投资5万
            2、收益T+0，固定利率3.3%，次日开始返息
            3、购买活期项目后即设置为自动投标：
            第一优先匹配“活期项目的债权转让”，
            第二是“公司内部账号购买的标的债权转让”，
            最后是“正在发标中的项目”
            4、提现T+1，申请提现即转变为“活期项目的债权转让”项目，等待接手（下一个买入活期项目的客户），
                次日15:00回款前仍未有人接手，则使用公司内部账号自动购买此债权，15:00点返回客户提现资金到平台账户。*/

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="apportionAmount"></param>
        /// <param name="tr"></param>
        /// <param name="exceedCallback"></param>
        /// <returns>剩余可投</returns>
        private static decimal AutoInvestment(Agp2pDataContext context, decimal apportionAmount, li_project_transactions tr, DateTime? investTime = null)
        {
            // 接手债权或找出可投资项目并投资（创建债权），如果项目可投金额不足，则抛异常

            // 优先匹配需要转让的债权
            var needTransferClaims = context.li_claims.Where(c => c.status == (int) Agp2pEnums.ClaimStatusEnum.NeedTransfer).ToList();
            apportionAmount = ApportionToClaims(context, needTransferClaims, apportionAmount, tr, investTime);

            if (0 < apportionAmount)
            {
                // 匹配可转让债权（公司内部的）
                var transferableClaims = context.li_claims.Where(c => c.status == (int) Agp2pEnums.ClaimStatusEnum.Transferable).ToList();
                apportionAmount = ApportionToClaims(context, transferableClaims, apportionAmount, tr, investTime);
            }

            if (apportionAmount == 0) return apportionAmount;

            // 匹配项目
            var investableProjects =
                context.li_projects.Where(p => p.status == (int) Agp2pEnums.ProjectStatusEnum.Financing)
                    .AsEnumerable()
                    .Where(p => !p.IsNewbieProject() && !p.IsHuoqiProject())
                    .ToList();

            return ApportionToProjects(context, investableProjects, apportionAmount, tr, investTime);
        }

        private static decimal ApportionToProjects(Agp2pDataContext context, List<li_projects> investableProjects, decimal investingMoney, li_project_transactions tr, DateTime? investTime = null)
        {
            if (!investableProjects.Any() || investingMoney == 0)
                return investingMoney;
            Debug.Assert(0 < investingMoney);

            if (investableProjects.Sum(p => p.financing_amount - p.investment_amount) < investingMoney)
            {
                // 全部投资
                return investingMoney -
                       investableProjects.Select(
                           p => ClaimCreate(context, p, p.financing_amount - p.investment_amount, tr, investTime)).Sum();
            }

            var averageInvestment = investingMoney / investableProjects.Count;
            var priorityProjects = investableProjects.Where(p => p.financing_amount - p.investment_amount <= averageInvestment).ToList();
            if (priorityProjects.Any())
            {
                // 可投资金额低于平均值的项目，全部投资，其余的递归处理
                var notPriorityProjects = investableProjects.Where(p => averageInvestment < p.financing_amount - p.investment_amount).ToList();

                var consumed = priorityProjects.Select(p => ClaimCreate(context, p, p.financing_amount - p.investment_amount, tr, investTime)).Sum();
                return ApportionToProjects(context, notPriorityProjects, investingMoney - consumed, tr, investTime);
            }
            else
            {
                // 部分投资
                var perfectRounding = Utils.GetPerfectRounding(investingMoney.GetPerfectSplitStream(investableProjects.Count).ToList(), investingMoney, 0);
                return investingMoney -
                       investableProjects.Zip(perfectRounding,
                           (p, investAmount) => 0 < investAmount ? ClaimCreate(context, p, investAmount, tr, investTime) : 0).Sum();
            }
        }

        private static decimal ClaimCreate(Agp2pDataContext context, li_projects project, decimal investment, li_project_transactions tr, DateTime? investTime = null)
        {
            Debug.Assert(investment != 0);

            project.investment_amount += investment;
            var liClaims = new li_claims
            {
                li_project_transactions1 = tr,
                createTime = investTime.GetValueOrDefault(tr.create_time),
                projectId = project.id,
                profitingProjectId = tr.li_projects.id,
                userId = tr.dt_users.id,
                status = (byte) Agp2pEnums.ClaimStatusEnum.Nontransferable,
                principal = investment,
                number = Utils.HiResNowString
            };
            context.li_claims.InsertOnSubmit(liClaims);
            return investment;
        }

        private static decimal ApportionToClaims(Agp2pDataContext context, List<li_claims> needTransferClaims, decimal investingMoney, li_project_transactions tr, DateTime? investTime = null)
        {
            if (!needTransferClaims.Any() || investingMoney == 0)
                return investingMoney;
            Debug.Assert(0 < investingMoney);

            if (needTransferClaims.Sum(c => c.principal) <= investingMoney)
            {
                // 全部接手
                needTransferClaims.ForEach(c => ClaimTransfer(context, c, c.principal, tr, investTime));
                return investingMoney - needTransferClaims.Sum(c => c.principal);
            }

            var averageTransfer = investingMoney/needTransferClaims.Count;
            var priorityClaimses = needTransferClaims.Where(c => c.principal <= averageTransfer).ToList();
            if (priorityClaimses.Any())
            {
                // 本金低于平均值的债权，全部接手，其余的递归处理
                var notPriorityClaimses = needTransferClaims.Where(c => averageTransfer < c.principal).ToList();
                priorityClaimses.ForEach(c => ClaimTransfer(context, c, c.principal, tr, investTime));
                return ApportionToClaims(context, notPriorityClaimses, investingMoney - priorityClaimses.Sum(c => c.principal), tr, investTime);
            }
            else
            {
                // 全部拆分
                // 注意：债权的本金不能为小数
                var perfectRounding = Utils.GetPerfectRounding(investingMoney.GetPerfectSplitStream(needTransferClaims.Count).ToList(), investingMoney, 0);
                needTransferClaims.ZipEach(perfectRounding, (c, transferAmount) =>
                {
                    if (0 < transferAmount)
                    {
                        ClaimTransfer(context, c, transferAmount, tr, investTime);
                    }
                });
                return investingMoney - perfectRounding.Sum();
            }
        }

        private static li_claims ClaimTransfer(Agp2pDataContext context, li_claims originalClaim, decimal amount, li_project_transactions tr, DateTime? investTime = null)
        {
            if (originalClaim.status != (int) Agp2pEnums.ClaimStatusEnum.Transferable || originalClaim.status != (int) Agp2pEnums.ClaimStatusEnum.NeedTransfer)
                throw new InvalidOperationException("该债权不可转让");
            if (amount <= 0)
                throw new InvalidOperationException("债权转让金额不能小于0");
            if (originalClaim.principal < amount)
                throw new InvalidOperationException("债权转让金额不能超出债权的本金");

            var transactTime = investTime.GetValueOrDefault(tr.create_time);

            if (amount < originalClaim.principal)
            {
                // 债权未完全转让
                var remainClaim = new li_claims
                {
                    parentClaimId = originalClaim.id,
                    projectId = originalClaim.projectId,
                    userId = originalClaim.userId,
                    profitingProjectId = originalClaim.profitingProjectId,
                    principal = originalClaim.principal - amount,
                    status = originalClaim.status,
                    createTime = transactTime,
                    createFromInvestment = originalClaim.createFromInvestment,
                    number = originalClaim.number
                };
                context.li_claims.InsertOnSubmit(remainClaim);
            }
            var liClaims = new li_claims
            {
                parentClaimId = originalClaim.id,
                createTime = transactTime,
                principal = amount,
                userId = tr.dt_users.id,
                status = (byte)Agp2pEnums.ClaimStatusEnum.Nontransferable,
                projectId = originalClaim.projectId,
                profitingProjectId = tr.li_projects.id,
                li_project_transactions1 = tr,
                number = Utils.HiResNowString
            };
            context.li_claims.InsertOnSubmit(liClaims);

            if (originalClaim.status == (int) Agp2pEnums.ClaimStatusEnum.NeedTransfer)
            {
                // 提现 T + 1
                originalClaim.status = (byte) Agp2pEnums.ClaimStatusEnum.TransferredUnpaid;
                originalClaim.statusUpdateTime = transactTime;
            }
            else
            {
                originalClaim.status = (byte) Agp2pEnums.ClaimStatusEnum.Transferred;
                originalClaim.statusUpdateTime = transactTime;

                // TODO test 处理债权转让的本金交易
                var claimTransferPtr = new li_project_transactions
                {
                    principal = amount,
                    project = originalClaim.projectId,
                    create_time = transactTime,
                    investor = originalClaim.userId,
                    type = (byte)Agp2pEnums.ProjectTransactionTypeEnum.ClaimTransfer,
                    status = (byte)Agp2pEnums.ProjectTransactionStatusEnum.Success,
                    gainFromClaim = originalClaim.id,
                    remark = $"项目【{originalClaim.li_projects.title}】的 {originalClaim.principal} 债权转让成功，转让金额 {amount}，剩余债权金额 {originalClaim.principal - amount}"
                };
                context.li_project_transactions.InsertOnSubmit(claimTransferPtr);

                var wallet = originalClaim.dt_users.li_wallets;
                wallet.idle_money += amount;
                wallet.investing_money -= amount;
                wallet.last_update_time = transactTime;

                // 修改钱包历史
                var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.Invest);
                his.li_project_transactions = tr;
                context.li_wallet_histories.InsertOnSubmit(his);
            }

            return liClaims;
        }

        /// <summary>
        /// 用户投资过后，如果项目可投金额为 0，则将状态设置为满标
        /// </summary>
        /// <param name="projectTransactionId"></param>
        private static void CheckFinancingComplete(int projectTransactionId)
        {
            var context = new Agp2pDataContext();
            var ptr = context.li_project_transactions.Single(tr => tr.id == projectTransactionId);
            var project = ptr.li_projects;
            if (project.IsHuoqiProject()) {
                // TODO test 判断自动投标的项目是否满标
                var financingCompletedProject = ptr.li_claims1.Where(
                    c =>
                        c.li_projects.status == (int) Agp2pEnums.ProjectStatusEnum.Financing &&
                        c.li_projects.financing_amount == c.li_projects.investment_amount)
                    .Select(c => c.li_projects)
                    .ToList();
                financingCompletedProject.ForEach(p => FinishInvestment(context, p.id));
                return; // 活期项目不会满标
            }

            var canBeInvest = project.financing_amount - project.investment_amount;
            if (0 < canBeInvest) return; // 未满标
            if (project.IsNewbieProject()) return; // 新手标项目不会满标
            FinishInvestment(context, project.id);
        }

        /// <summary>
        /// 投资结束
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <param name="investCompleteTime"></param>
        /// <returns></returns>
        public static li_projects FinishInvestment(this Agp2pDataContext context, int projectId)
        {
            var project = context.li_projects.Single(p => p.id == projectId);
            if (project.status != (int) Agp2pEnums.ProjectStatusEnum.Financing)
                throw new InvalidOperationException("项目不是发标状态，不能设置为满标");
            if (project.IsHuoqiProject())
                throw new InvalidOperationException("活期项目不会满标");

            project.status = (int) Agp2pEnums.ProjectStatusEnum.FinancingSuccess;

            // 项目投资完成时间应该等于最后一个债权的创建时间
            var lastClaim = project.li_claims.Where(c => c.status < (int) Agp2pEnums.ClaimStatusEnum.Completed).OrderByDescending(c => c.createTime).FirstOrDefault();
            project.invest_complete_time = lastClaim?.createTime ?? DateTime.Now;

            context.SubmitChanges();

            MessageBus.Main.PublishAsync(new ProjectFinancingCompletedMsg(projectId)); // 广播项目满标的消息
            return project;
        }

        /// <summary>
        /// 投资超时，截标
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public static li_projects FinishInvestmentEvenTimeout(this Agp2pDataContext context, int projectId)
        {
            var project = context.li_projects.Single(p => p.id == projectId);
            if (project.status != (int)Agp2pEnums.ProjectStatusEnum.FinancingTimeout)
                throw new InvalidOperationException("项目不是投资超时状态，不能设置为截标");
            project.status = (int)Agp2pEnums.ProjectStatusEnum.FinancingSuccess;

            // 截标时间就是当前时间
            project.invest_complete_time = DateTime.Now;

            context.SubmitChanges();

            MessageBus.Main.PublishAsync(new ProjectFinancingCompleteEvenTimeoutMsg(projectId)); // 广播项目截标的消息
            return project;
        }

        /// <summary>
        /// 开始还款
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public static li_projects StartRepayment(this Agp2pDataContext context, int projectId)
        {
            var project = context.li_projects.Single(p => p.id == projectId);
            if (project.status != (int) Agp2pEnums.ProjectStatusEnum.FinancingSuccess)
                throw new InvalidOperationException("项目不是满标状态，不能设置为正在还款状态");

            // 修改项目状态为满标/截标
            project.status = (int) Agp2pEnums.ProjectStatusEnum.ProjectRepaying;
            project.make_loan_time = DateTime.Now; // 放款时间

            var repaymentType = (Agp2pEnums.ProjectRepaymentTypeEnum) project.repayment_type; // 还款类型

            // 满标时计算真实总利率
            project.profit_rate = project.GetFinalProfitRate();
            var termCount = project.CalcRealTermCount(); // 实际期数

            var repayPrincipal = project.investment_amount; // 本金投资总额
            var interestAmount = Math.Round(project.profit_rate*repayPrincipal, 2); // 利息总额

            List<li_repayment_tasks> repaymentTasks;
            if (repaymentType == Agp2pEnums.ProjectRepaymentTypeEnum.DengEr) // 等额本息
            {
                repaymentTasks = Enumerable.Range(1, termCount)
                    .Zip(interestAmount.GetPerfectSplitStream(termCount),
                        (termNumber, repayInterestEachTerm) => new {termNumber, repayInterestEachTerm})
                    .Zip(repayPrincipal.GetPerfectSplitStream(termCount),
                        (a, repayPrincipalEachTerm) =>
                            new {a.termNumber, a.repayInterestEachTerm, repayPrincipalEachTerm})
                    .Select(term => new li_repayment_tasks
                    {
                        li_projects = project,
                        repay_interest = term.repayInterestEachTerm,
                        repay_principal = term.repayPrincipalEachTerm,
                        status = (byte) Agp2pEnums.RepaymentStatusEnum.Unpaid,
                        term = (short) term.termNumber,
                        should_repay_time = project.CalcRepayTimeByTerm(term.termNumber)
                    }).ToList();
            }
            else if (repaymentType == Agp2pEnums.ProjectRepaymentTypeEnum.XianXi) // 先息后本
            {
                repaymentTasks = Enumerable.Range(1, termCount)
                    .Zip(interestAmount.GetPerfectSplitStream(termCount),
                        (termNumber, repayInterestEachTerm) => new {termNumber, repayInterestEachTerm})
                    .Select(term => new li_repayment_tasks
                    {
                        li_projects = project,
                        repay_interest = term.repayInterestEachTerm, // 只付利息
                        repay_principal = 0,
                        status = (byte) Agp2pEnums.RepaymentStatusEnum.Unpaid,
                        term = (short) term.termNumber,
                        should_repay_time = project.CalcRepayTimeByTerm(term.termNumber)
                    }).ToList();
                repaymentTasks.Last().repay_principal = repayPrincipal; // 最后额外添加一期返还全部本金
            }
            else if (repaymentType == Agp2pEnums.ProjectRepaymentTypeEnum.DaoQi) // 到期还款付息
            {
                if (termCount != 1)
                    throw new Exception("到期还款付息 只能有一期");
                repaymentTasks = Enumerable.Repeat(new li_repayment_tasks
                {
                    li_projects = project,
                    repay_interest = interestAmount,
                    repay_principal = repayPrincipal,
                    status = (byte) Agp2pEnums.RepaymentStatusEnum.Unpaid,
                    term = 1,
                    should_repay_time = project.CalcRepayTimeByTerm(1)
                }, 1).ToList();
            }
            else throw new InvalidEnumArgumentException("项目的还款类型值异常");
            context.li_repayment_tasks.InsertAllOnSubmit(repaymentTasks);

            // 计算每个投资人的待收益金额，因为不一定是投资当日满标，所以不能投资时就知道收益（不同时间满标/截标会对导致不同的回款时间间隔，从而导致利率不同）
            context.CalcProfitingMoneyAfterRepaymentTasksCreated(project, repaymentTasks);

            // 计算平台服务费
            if (!project.dt_article_category.call_index.Equals("newbie"))
            {
                if (project.loan_fee_rate != null && project.loan_fee_rate > 0)
                {
                    context.li_company_inoutcome.InsertOnSubmit(new li_company_inoutcome
                    {
                        user_id = project.li_risks.li_loaners.dt_users.id,
                        income = (decimal) (project.financing_amount*(project.loan_fee_rate/100)),
                        project_id = projectId,
                        type = (int) Agp2pEnums.OfflineTransactionTypeEnum.ManagementFeeOfLoanning,
                        create_time = DateTime.Now,
                        remark = $"借款项目'{project.title}'收取平台服务费"
                    });
                }

                //计算风险保证金
                if (project.bond_fee_rate != null && project.bond_fee_rate > 0)
                {
                    context.li_company_inoutcome.InsertOnSubmit(new li_company_inoutcome
                    {
                        user_id = project.li_risks.li_loaners.dt_users.id,
                        income = project.financing_amount*(project.bond_fee_rate/100) ?? 0,
                        project_id = projectId,
                        type = (int) Agp2pEnums.OfflineTransactionTypeEnum.BondFee,
                        create_time = DateTime.Now,
                        remark = $"借款项目'{project.title}'收取风险保证金"
                    });
                }
            }
            context.SubmitChanges();

            MessageBus.Main.PublishAsync(new ProjectStartRepaymentMsg(projectId)); // 广播项目开始还款的消息
            return project;
        }

        /// <summary>
        /// 完整分割 10 / 3 => 3.33 + 3.33 + 3.34
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="splitCount"></param>
        /// <param name="toFixed"></param>
        /// <returns></returns>
        public static IEnumerable<decimal> GetPerfectSplitStream(this decimal amount, int splitCount, int toFixed = 2)
        {
            if (splitCount <= 0)
            {
                throw new Exception("无法分割为 0 份");
            }
            var part = Math.Round(amount/splitCount, toFixed);
            var finalPart = Math.Round(amount - part*(splitCount - 1), toFixed);
            return Enumerable.Repeat(part, splitCount - 1).Concat(Enumerable.Repeat(finalPart, 1));
        }

        /// <summary>
        /// 根据用户从某个项目中能获得的实际收益来计算“待收金额”，完全避免精度问题
        /// </summary>
        /// <param name="context"></param>
        /// <param name="project"></param>
        /// <param name="tasks"></param>
        private static void CalcProfitingMoneyAfterRepaymentTasksCreated(this Agp2pDataContext context, li_projects project, List<li_repayment_tasks> tasks)
        {
            // 查询每个用户的债权记录（一个用户可能投资多次）
            var userClaims = project.li_claims1.Where(c => c.status < (int)Agp2pEnums.ClaimStatusEnum.Completed).ToLookup(c => c.dt_users);

            var wallets = userClaims.Select(ir => ir.Key.li_wallets).ToList();
            var walletDict = wallets.ToDictionary(w => w.user_id);

            // 重新计算代收本金前，先减去原来的投资金额
            userClaims.ForEach(uc =>
            {
                uc.Key.li_wallets.investing_money -= uc.Sum(c => c.principal);
            });

            // 修改钱包的值（待收益金额和时间）
            foreach (var task in tasks)
            {
                // 累加利润：避免直接计算总利润（利率 * 总投资额），这样可以避免精度问题
                var predicts = GenerateRepayTransactions(task, task.should_repay_time);
                predicts.ForEach(ptr =>
                {
                    var repayTo = walletDict[ptr.investor];
                    repayTo.profiting_money += ptr.interest.GetValueOrDefault();
                    repayTo.investing_money += ptr.principal;
                });
            }

            var projectInvestCompleteTime = project.make_loan_time.Value;
            wallets.ForEach(w => w.last_update_time = projectInvestCompleteTime); // 时间应该一致

            // 创建钱包历史
            var histories = wallets.Select(w =>
            {
                var his = CloneFromWallet(w, Agp2pEnums.WalletHistoryTypeEnum.InvestSuccess);
                his.li_project_transactions = userClaims[w.dt_users].Last().li_project_transactions1;
                return his;
            });
            context.li_wallet_histories.InsertAllOnSubmit(histories);
        }

        /// <summary>
        /// 计算每期的还款时间
        /// </summary>
        /// <param name="baseTime"></param>
        /// <param name="termSpan">还款期限跨度</param>
        /// <param name="termNumber">第几期</param>
        /// <param name="termUnitCount"></param>
        /// <returns></returns>
        public static DateTime CalcRepayTimeByTerm(this li_projects proj, int termNumber, DateTime? makeLoanTime = null)
        {
            var baseTime = proj.make_loan_time ?? makeLoanTime.Value;
            switch ((Agp2pEnums.ProjectRepaymentTermSpanEnum)proj.repayment_term_span)
            {
                case Agp2pEnums.ProjectRepaymentTermSpanEnum.Year:
                    return baseTime.AddYears(termNumber);
                case Agp2pEnums.ProjectRepaymentTermSpanEnum.Month:
                    return baseTime.AddMonthsPreferLastDay(termNumber); // 月尾满标的话总是在月尾付息
                case Agp2pEnums.ProjectRepaymentTermSpanEnum.Day:
                    Debug.Assert(termNumber == 1, "日标只还一期");
                    return baseTime.AddDays(proj.repayment_term_span_count);
                default:
                    throw new InvalidEnumArgumentException("异常的项目还款跨度值");
            }
        }

        /// <summary>
        /// 添加月份，匹配月尾（2月28日 => 3月31日)
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="months"></param>
        /// <returns></returns>
        private static DateTime AddMonthsPreferLastDay(this DateTime dateTime, int months)
        {
            var addedMonth = dateTime.AddMonths(months);
            if (dateTime.Day == DateTime.DaysInMonth(dateTime.Year, dateTime.Month)) // 是否月尾
            {
                var daysInMonth = DateTime.DaysInMonth(addedMonth.Year, addedMonth.Month);
                return addedMonth.AddDays(daysInMonth - addedMonth.Day); // 去到月尾
            }
            return addedMonth;
        }

        /// <summary>
        /// 计算真实还款期数
        /// </summary>
        /// <param name="termSpan"></param>
        /// <param name="termSpanCount"></param>
        /// <returns></returns>
        public static int CalcRealTermCount(this li_projects proj)
        {
            switch ((Agp2pEnums.ProjectRepaymentTermSpanEnum)proj.repayment_term_span)
            {
                case Agp2pEnums.ProjectRepaymentTermSpanEnum.Year:
                    return proj.repayment_term_span_count;
                case Agp2pEnums.ProjectRepaymentTermSpanEnum.Month:
                    return proj.repayment_term_span_count;
                case Agp2pEnums.ProjectRepaymentTermSpanEnum.Day: // 日的话只在最后一日还
                    return 1;
                default:
                    throw new InvalidEnumArgumentException("异常的项目还款跨度值");
            }
        }

        public static decimal GetFinalProfitRate(this li_projects proj, DateTime? makeLoanTime = null)
        {
            if (0 < proj.profit_rate)
                return proj.profit_rate;

            if (proj.dt_article_category.call_index == "ypb" || proj.dt_article_category.call_index == "ypl")
            {
                var projectRepaymentTermSpanEnum = (Agp2pEnums.ProjectRepaymentTermSpanEnum) proj.repayment_term_span;
                if (projectRepaymentTermSpanEnum != Agp2pEnums.ProjectRepaymentTermSpanEnum.Day)
                {
                    throw new Exception("银票宝的期数只能是按日算");
                }
                return (decimal) proj.profit_rate_year/100/360*proj.repayment_term_span_count;
            }
            return CalcFinalProfitRate(proj, makeLoanTime);
        }

        /// <summary>
        /// 计算最终的项目利润率（始终按天数算最终利润率）
        /// </summary>
        /// <param name="baseTime"></param>
        /// <param name="profitRateYear"></param>
        /// <param name="termSpanEnum"></param>
        /// <param name="termSpanCount"></param>
        /// <returns></returns>
        private static decimal CalcFinalProfitRate(li_projects proj, DateTime? makeLoanTime = null)
        {
            var baseTime = proj.make_loan_time ?? makeLoanTime.Value;
            var profitRateYear = proj.profit_rate_year / 100; // 年化利率未除以 100
            var termSpanCount = proj.repayment_term_span_count;

            switch ((Agp2pEnums.ProjectRepaymentTermSpanEnum) proj.repayment_term_span) // 公式：年利率 * 总天数 / 365
            {
                case Agp2pEnums.ProjectRepaymentTermSpanEnum.Year:
                    return profitRateYear*termSpanCount;
                case Agp2pEnums.ProjectRepaymentTermSpanEnum.Month:
                    // 最后那期还款的日期 - 满标的日期 = 总天数
                    var lastRepayDate = proj.CalcRepayTimeByTerm(termSpanCount, makeLoanTime).Date;
                    var days = lastRepayDate.Subtract(baseTime.Date).Days;
                    return profitRateYear*days/365;
                case Agp2pEnums.ProjectRepaymentTermSpanEnum.Day:
                    return profitRateYear*termSpanCount/365;
                default:
                    throw new InvalidEnumArgumentException("异常的项目还款跨度值");
            }
        }

        public static Agp2pEnums.WalletHistoryTypeEnum GetWalletHistoryTypeByProjectTransaction(li_project_transactions ptr)
        {
            if (ptr.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.RepayToInvestor)
            {
                if (ptr.principal != 0 && ptr.interest != 0)
                    return Agp2pEnums.WalletHistoryTypeEnum.RepaidPrincipalAndInterest;
                else if (ptr.principal == 0)
                    return Agp2pEnums.WalletHistoryTypeEnum.RepaidInterest;
                else if (ptr.interest == 0)
                    return Agp2pEnums.WalletHistoryTypeEnum.RepaidPrincipal;
            }
            else if (ptr.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.RepayOverdueFine)
            {
                return Agp2pEnums.WalletHistoryTypeEnum.RepaidOverdueFine;
            }
            else if (ptr.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.ClaimTransfer)
            {
                return Agp2pEnums.WalletHistoryTypeEnum.ClaimTransfer;
            }
            else if (ptr.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.AutoInvestFailRepay)
            {
                return Agp2pEnums.WalletHistoryTypeEnum.AutoInvestFailRepaySuccess;
            }
            else if (ptr.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.HuoqiProjectWithdraw)
            {
                return Agp2pEnums.WalletHistoryTypeEnum.HuoqiProjectWithdrawSuccess;
            }
            throw new Exception("项目交易状态异常");
        }

        /// <summary>
        /// 马上执行还款计划
        /// </summary>
        /// <param name="context"></param>
        /// <param name="repaymentId"></param>
        /// <param name="autoPaid"></param>
        public static li_repayment_tasks ExecuteRepaymentTask(this Agp2pDataContext context, int repaymentId,
            Agp2pEnums.RepaymentStatusEnum statusAfterPay = Agp2pEnums.RepaymentStatusEnum.AutoPaid)
        {
            var repaymentTask = context.li_repayment_tasks.Single(r => r.id == repaymentId);
            var proj = repaymentTask.li_projects;
            if (repaymentTask.status != (int) Agp2pEnums.RepaymentStatusEnum.Unpaid &&
                repaymentTask.status != (int) Agp2pEnums.RepaymentStatusEnum.OverTime)
                throw new InvalidOperationException("这个还款计划已经执行过了");
            if (statusAfterPay < Agp2pEnums.RepaymentStatusEnum.ManualPaid)
                throw new InvalidOperationException("还款计划的执行状态不正确");

            // 执行还款
            repaymentTask.status = (byte) statusAfterPay;
            repaymentTask.repay_at = DateTime.Now;

            var ptrs = GenerateRepayTransactions(repaymentTask, repaymentTask.repay_at.Value, true); //变更时间应该等于还款计划的还款时间
            context.li_project_transactions.InsertAllOnSubmit(ptrs);

            Dictionary<int, li_project_transactions> ptrAddedCost = null;
            if (repaymentTask.cost.GetValueOrDefault() != 0)
            {
                ptrAddedCost = GenerateRepayTransactions(repaymentTask, repaymentTask.repay_at.Value, false, true).ToDictionary(tr => tr.investor);
            }

            foreach (var ptr in ptrs)
            {
                // 增加钱包空闲金额与减去待收本金和待收利润
                var wallet = ptr.dt_users.li_wallets;
                wallet.idle_money += ptr.interest.GetValueOrDefault() + ptr.principal;
                wallet.investing_money -= ptr.principal;
                // 活期项目不减代收
                if (!proj.IsHuoqiProject())
                {
                    // 由于 提前还款/逾期还款 的缘故，需要修正待收益
                    wallet.profiting_money -= ptrAddedCost == null
                        ? ptr.interest.GetValueOrDefault()
                        : ptrAddedCost[ptr.investor].interest.GetValueOrDefault();
                }
                wallet.total_profit += ptr.interest.GetValueOrDefault();
                wallet.last_update_time = ptr.create_time;

                // 添加钱包历史
                var his = CloneFromWallet(wallet, GetWalletHistoryTypeByProjectTransaction(ptr));
                his.li_project_transactions = ptr;
                context.li_wallet_histories.InsertOnSubmit(his);
            }
            context.SubmitChanges();


            var projectNeedComplete = !proj.IsHuoqiProject() && !proj.IsNewbieProject() && !proj.li_repayment_tasks.Any(
                ta =>
                    ta.id != repaymentId &&
                    (ta.status == (int) Agp2pEnums.RepaymentStatusEnum.Unpaid ||
                     ta.status == (int) Agp2pEnums.RepaymentStatusEnum.OverTime));
            MessageBus.Main.PublishAsync(new ProjectRepaidMsg(repaymentId, projectNeedComplete)); // 广播项目还款的消息

            return repaymentTask;
        }

        private static void CompleteProject(int repaymentTaskId)
        {
            // 如果所有还款计划均已执行，将项目标记为完成
            var context = new Agp2pDataContext(); // 旧的 context 有缓存，查询的结果不正确
            var repaymentTask = context.li_repayment_tasks.Single(ta => ta.id == repaymentTaskId);
            var pro = repaymentTask.li_projects;
            if (!pro.IsNewbieProject() && !pro.IsHuoqiProject()
                && !pro.li_repayment_tasks.Any(r => r.status == (int)Agp2pEnums.RepaymentStatusEnum.Unpaid || r.status == (int)Agp2pEnums.RepaymentStatusEnum.OverTime))
            {
                pro.status = (int)Agp2pEnums.ProjectStatusEnum.RepayCompleteIntime;
                pro.complete_time = repaymentTask.repay_at;

                AutoInvestAfterProjectCompleted(context, pro);
                context.SubmitChanges();

                // 广播项目完成的消息
                MessageBus.Main.PublishAsync(new ProjectRepayCompletedMsg(pro.id, repaymentTask.repay_at.Value));
            }
        }

        private static void AutoInvestAfterProjectCompleted(Agp2pDataContext newContext, li_projects pro)
        {
            // 将债权设置为完成，活期收益的债权需要继续自动投标，如果自动投标失败，则部分退款
            var needComplete = pro.li_claims.Where(c => c.status < (int) Agp2pEnums.ClaimStatusEnum.Completed).ToList();

            // 自动投标的债权
            var huoqiProfitingClaims = needComplete.Where(c => c.profitingProjectId != c.projectId).ToList(); // 自动投标的项目
            var nonTransferableClaims = huoqiProfitingClaims.Where(c => c.status == (int) Agp2pEnums.ClaimStatusEnum.Nontransferable).ToList(); // 不可转让
            var transferableClaims = huoqiProfitingClaims.Where(c => c.status == (int) Agp2pEnums.ClaimStatusEnum.Transferable).ToList(); // 可转让
            var needTransferClaims = huoqiProfitingClaims.Where(c => c.status == (int) Agp2pEnums.ClaimStatusEnum.NeedTransfer).ToList(); // 需要转让

            Debug.Assert(!transferableClaims.Any(), "自动投标不应该产生可转让的债权");

            needComplete.ForEach(c =>
            {
                // 先全部完成，以免自动投标投了这些项目
                c.status = (byte) Agp2pEnums.ClaimStatusEnum.Completed;
                c.statusUpdateTime = pro.complete_time.Value;
            }); 

            // 提现中的活期债权状态设为完成，未回款
            needTransferClaims.ForEach(c =>
            {
                c.status = (byte) Agp2pEnums.ClaimStatusEnum.CompletedUnpaid;
                c.statusUpdateTime = pro.complete_time.Value;
            });

            var noMoreInvestable = false;
            // 不可转让的活期债权继续自动投标
            nonTransferableClaims.ToLookup(c => c.li_project_transactions1).ForEach(ptrcs =>
            {
                var needTransfer = ptrcs.Sum(c => c.principal);
                var srcPtr = ptrcs.Key;
                var exceed = noMoreInvestable ? needTransfer : AutoInvestment(newContext, needTransfer, srcPtr, pro.complete_time.Value);
                if (0 < exceed)
                {
                    noMoreInvestable = true; // 优化：没有项目可以投资的时候，直接跳过这个步骤

                    // 超出的部分退款
                    var autoInvestFailRepay = new li_project_transactions
                    {
                        type = (byte) Agp2pEnums.ProjectTransactionTypeEnum.AutoInvestFailRepay,
                        status = (byte) Agp2pEnums.ProjectTransactionStatusEnum.Success,
                        project = srcPtr.project,
                        create_time = pro.complete_time.Value,
                        investor = srcPtr.investor,
                        principal = exceed,
                        remark = $"自动续投失败回款，总续投：{needTransfer}，成功续投：{(needTransfer - exceed)}",
                    };
                    newContext.li_project_transactions.InsertOnSubmit(autoInvestFailRepay);

                    var wallet = srcPtr.dt_users.li_wallets;
                    wallet.idle_money += autoInvestFailRepay.principal;
                    wallet.investing_money -= autoInvestFailRepay.principal;
                    wallet.last_update_time = autoInvestFailRepay.create_time;

                    // 添加钱包历史
                    var his = CloneFromWallet(wallet, GetWalletHistoryTypeByProjectTransaction(autoInvestFailRepay));
                    his.li_project_transactions = srcPtr;
                    newContext.li_wallet_histories.InsertOnSubmit(his);
                }
            });
        }

        private static Dictionary<li_claims, decimal> GetClaimRatio(li_projects proj)
        {
            if (proj.IsHuoqiProject())
            {
                var claims = proj.li_claims1.Where(c => c.status < (int) Agp2pEnums.ClaimStatusEnum.Transferred).ToList();
                Debug.Assert(claims.Aggregate(0m, (sum, c) => sum + c.principal) == proj.investment_amount, "项目债权总金额应匹配项目已融资总额");
                return claims.ToDictionary(c => c, c => c.principal/proj.investment_amount);
            }

            var allClaims = proj.li_claims.Where(c => c.status < (int)Agp2pEnums.ClaimStatusEnum.Transferred).ToList();
            Debug.Assert(allClaims.Aggregate(0m, (sum, c) => sum + c.principal) == proj.investment_amount, "项目债权总金额应匹配项目已融资总额");

            // 只回款：claim.profitingProjectId 为当前还款计划所属项目，因为活期项目跟进自身的还款计划独立计息
            var profitingClaims = allClaims.Where(c => c.profitingProjectId == c.projectId);

            // 仅针对单个用户的还款
            if (proj.IsNewbieProject())
            {
                return profitingClaims.ToDictionary(c => c, c => 1m);
            }

            // 计算出每个债权的本金占比，公式：债权金额 / 项目投资总额
            return profitingClaims.ToDictionary(c => c, c => c.principal/proj.investment_amount);
        }

        /// <summary>
        /// 生成还款交易记录（未保存）
        /// </summary>
        /// <param name="repaymentTask"></param>
        /// <param name="transactTime"></param>
        /// <param name="unsafeCreateEntities"></param>
        /// <param name="applyCostIntoInterest">如果为 true ，则 interest 实际上为计算了 cost 的收益</param>
        /// <returns></returns>
        public static List<li_project_transactions> GenerateRepayTransactions(li_repayment_tasks repaymentTask, DateTime transactTime,
            bool unsafeCreateEntities = false, bool applyCostIntoInterest = false)
        {
            var moneyRepayRatio = GetClaimRatio(repaymentTask.li_projects);

            if (!moneyRepayRatio.Any())
                return Enumerable.Empty<li_project_transactions>().ToList();

            // 如果是针对单个用户的还款计划，则只生成对应用户的交易记录
            var rounded = moneyRepayRatio
                .Where(c => c.Key.status < (int) Agp2pEnums.ClaimStatusEnum.Completed) // 只为未完成/有效的债权回款
                .Where(pair => repaymentTask.only_repay_to == null || pair.Key.id == repaymentTask.only_repay_to)
                .Select(c =>
            {
                var realityInterest = Math.Round(c.Value*repaymentTask.repay_interest, 2);
                string remark = null;
                if (repaymentTask.status == (int) Agp2pEnums.RepaymentStatusEnum.EarlierPaid && 0 < repaymentTask.cost)
                {
                    var originalInterest = c.Value*(repaymentTask.repay_interest + repaymentTask.cost.GetValueOrDefault());
                    remark = $"提前还款：此债权本期原来的待收益 {originalInterest:f2}，实际收益 {realityInterest:f2}";
                    if (applyCostIntoInterest)
                    {
                        realityInterest = originalInterest;
                    }
                }
                else if (repaymentTask.status == (int) Agp2pEnums.RepaymentStatusEnum.OverTimePaid)
                {
                    var originalInterest = c.Value*(repaymentTask.repay_interest + repaymentTask.cost.GetValueOrDefault());
                    remark = $"逾期还款：此债权本期原来的待收益 {originalInterest:f2}，实际收益 {realityInterest:f2}";
                    if (applyCostIntoInterest)
                    {
                        realityInterest = originalInterest;
                    }
                }

                // 不能直接复制实体类，否则 context 保存后会添加记录
                var ptr = new li_project_transactions
                {
                    create_time = transactTime, // 变更时间应该等于还款计划的还款时间
                    type = (byte) Agp2pEnums.ProjectTransactionTypeEnum.RepayToInvestor,
                    project = repaymentTask.project,
                    investor = c.Key.dt_users.id,
                    status = (byte) Agp2pEnums.ProjectTransactionStatusEnum.Success,
                    principal = Math.Round(c.Value*repaymentTask.repay_principal, 2),
                    interest = realityInterest,
                    remark = remark,
                    gainFromClaim = c.Key.id
                };
                if (unsafeCreateEntities)
                {
                    ptr.dt_users = c.Key.dt_users;
                }
                return ptr;
            }).ToList();

            if (!rounded.Any())
                return Enumerable.Empty<li_project_transactions>().ToList();

            // 因为有自动投标债权的缘故，项目的回款计划必定不能全部回款，所以无需调整四舍五入
            if (repaymentTask.li_projects.li_claims.Any(c => c.profitingProjectId != c.projectId))
                return rounded;

            var notPerfectRoundedInterest = rounded.Select(ptr => ptr.interest.GetValueOrDefault()).ToList();
            var forceSum = applyCostIntoInterest
                ? repaymentTask.cost.GetValueOrDefault() + repaymentTask.repay_interest
                : repaymentTask.repay_interest;
            var perfectRoundedInterest = Utils.GetPerfectRounding(notPerfectRoundedInterest, forceSum, 2);
            rounded.ZipEach(perfectRoundedInterest, (ptr, newInterest) => ptr.interest = newInterest);

            var notPerfectRoundedPrincipal = rounded.Select(ptr => ptr.principal).ToList();
            var perfectRoundedPrincipal = Utils.GetPerfectRounding(notPerfectRoundedPrincipal, repaymentTask.repay_principal, 2);
            rounded.ZipEach(perfectRoundedPrincipal, (ptr, newPrincipal) => ptr.principal = newPrincipal);

            return rounded;
        }

        /// <summary>
        /// 提前还款，之后未还的还款计划作废，全部投资者减去该项目剩余的待收益金额，执行还款转账操作
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <param name="remainTermPrincipalRatePercent">剩余期数的本息百分比率（不包括当前这期）</param>
        public static li_projects EarlierRepayAll(this Agp2pDataContext context, int projectId,
            decimal remainTermPrincipalRatePercent)
        {
            var project = context.li_projects.Single(p => p.id == projectId);
            var unpaidTasks =
                project.li_repayment_tasks.Where(t => t.status == (int) Agp2pEnums.RepaymentStatusEnum.Unpaid).ToList();
            if (!unpaidTasks.Any()) throw new Exception("全部还款计划均已执行，不能进行提前还款");
            if (remainTermPrincipalRatePercent < 0 || 100 < remainTermPrincipalRatePercent)
                throw new Exception("剩余利息百分比率不正常");
            var remainTermPrincipalRate = remainTermPrincipalRatePercent;

            var currentTask = unpaidTasks.First();

            var willInvalidTasks = unpaidTasks.Skip(1).ToList();
            if (!willInvalidTasks.Any())
            {
                context.ExecuteRepaymentTask(currentTask.id, Agp2pEnums.RepaymentStatusEnum.EarlierPaid);
                return project;
            }

            willInvalidTasks.ForEach(t => t.status = (byte) Agp2pEnums.RepaymentStatusEnum.Invalid); // 原计划作废

            var remainPrincipal = willInvalidTasks.Sum(t => t.repay_principal);
            var remainInterest = willInvalidTasks.Sum(t => t.repay_interest);

            var willPayInterest = Math.Round(remainPrincipal*remainTermPrincipalRate, 2); // 未还本金 * 比率

            // 生成新的计划
            var earlierRepayTask = new li_repayment_tasks
            {
                li_projects = project,
                cost = remainInterest - willPayInterest,
                repay_interest = willPayInterest,
                repay_principal = remainPrincipal,
                should_repay_time = willInvalidTasks.Last().should_repay_time,
                term = willInvalidTasks.First().term,
                status = (byte) Agp2pEnums.RepaymentStatusEnum.Unpaid,
            };
            if (earlierRepayTask.cost < 0)
            {
                throw new Exception("数值异常：提前还款后投资者收到利息反而更高");
            }
            context.li_repayment_tasks.InsertOnSubmit(earlierRepayTask);

            context.SubmitChanges();

            context.ExecuteRepaymentTask(currentTask.id, Agp2pEnums.RepaymentStatusEnum.EarlierPaid);
            context.ExecuteRepaymentTask(earlierRepayTask.id, Agp2pEnums.RepaymentStatusEnum.EarlierPaid);

            return project;
        }

        /// <summary>
        /// 逾期还款
        /// </summary>
        /// <param name="context"></param>
        /// <param name="repayTaskId"></param>
        /// <param name="overTimePayRate"></param>
        public static void OverTimeRepay(this Agp2pDataContext context, int repayTaskId, Model.costconfig costconfig)
        {
            var repaymentTask = context.li_repayment_tasks.Single(r => r.id == repayTaskId);
            if (repaymentTask.status != (int) Agp2pEnums.RepaymentStatusEnum.OverTime)
                throw new InvalidOperationException("当前还款不是逾期还款！");

            //逾期罚息
            var overTimePayInterest = repaymentTask.repay_interest*costconfig.overtime_pay;
            repaymentTask.cost = repaymentTask.repay_interest - overTimePayInterest;
            repaymentTask.repay_interest = overTimePayInterest;
            //计算逾期管理费
            var projectTransaction = new li_company_inoutcome()
            {
                user_id = (int) repaymentTask.li_projects.li_risks.li_loaners.user_id,
                project_id = repaymentTask.project,
                type = (int) Agp2pEnums.OfflineTransactionTypeEnum.ManagementFeeOfOverTime,
                create_time = DateTime.Now,
                remark = $"收取'{repaymentTask.li_projects.title}'第{repaymentTask.term}期的逾期管理费"
            };

            var overDays = DateTime.Now.Subtract(repaymentTask.should_repay_time).Days;

            if (repaymentTask.li_projects.dt_article_category.call_index.ToUpper().Contains("YPB"))
            {
                //票据业务
                projectTransaction.income = repaymentTask.li_projects.financing_amount*overDays*
                                               costconfig.overtime_cost_bank;
            }
            else
            {
                //非票据业务
                if (overDays <= 30)
                    projectTransaction.income = repaymentTask.li_projects.financing_amount*overDays*
                                                   costconfig.overtime_cost;
                else
                    projectTransaction.income = repaymentTask.li_projects.financing_amount*overDays*
                                                   costconfig.overtime_cost2;
            }
            context.li_company_inoutcome.InsertOnSubmit(projectTransaction);

            context.SubmitChanges();
            context.ExecuteRepaymentTask(repayTaskId, Agp2pEnums.RepaymentStatusEnum.OverTimePaid);

        }

        /// <summary>
        /// 撤销投资（功能只在后台）
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectTransactionId"></param>
        /// <param name="refundTime"></param>
        /// <param name="save"></param>
        /// <returns></returns>
        public static li_project_transactions Refund(this Agp2pDataContext context, int projectTransactionId, DateTime refundTime, bool save = true)
        {
            // 判断项目状态
            var tr = context.li_project_transactions.Single(t => t.id == projectTransactionId);
            if ((int) Agp2pEnums.ProjectStatusEnum.ProjectRepaying <= tr.li_projects.status)
                throw new InvalidOperationException("项目所在的状态不能退款");

            // 更改交易状态
            tr.status = (byte) Agp2pEnums.ProjectTransactionStatusEnum.Rollback;

            // 债权失效
            tr.li_claims1.ForEach(c =>
            {
                c.status = (byte) Agp2pEnums.ClaimStatusEnum.Invalid;
                c.statusUpdateTime = refundTime;
            });

            // 修改项目已投资金额
            tr.li_projects.investment_amount -= tr.principal;

            // 更改钱包金额
            var wallet = tr.dt_users.li_wallets;

            // 如果项目已经正在还款再撤销，就需要减掉 待收利润；满标前撤销的话待收利润未计算，所以不用减
            var investedMoney = tr.principal;
            wallet.idle_money += investedMoney;
            wallet.investing_money -= investedMoney;
            wallet.total_investment -= investedMoney;
            wallet.last_update_time = refundTime;

            // 添加钱包历史
            var his = CloneFromWallet(wallet, Agp2pEnums.WalletHistoryTypeEnum.InvestorRefund);
            his.li_project_transactions = tr;
            context.li_wallet_histories.InsertOnSubmit(his);

            if (save)
            {
                context.SubmitChanges();
                MessageBus.Main.PublishAsync(new UserRefundMsg(projectTransactionId));
            }

            return tr;
        }

        /// <summary>
        /// 项目流标，将项目的全部投资退款
        /// </summary>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        public static void ProjectFinancingFail(this Agp2pDataContext context, int projectId)
        {
            // 判断项目状态
            var proj = context.li_projects.Single(t => t.id == projectId);
            if ((int) Agp2pEnums.ProjectStatusEnum.ProjectRepaying <= proj.status)
                throw new InvalidOperationException("项目所在的状态不能退款");

            var refundTime = DateTime.Now;
            proj.li_project_transactions.Where(
                tr =>
                    tr.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.Invest &&
                    tr.status == (int) Agp2pEnums.ProjectTransactionStatusEnum.Success)
                .Select(tr => tr.id)
                .ForEach(trId => context.Refund(trId, refundTime, false));

            proj.status = (int) Agp2pEnums.ProjectStatusEnum.FinancingFail;
            context.SubmitChanges();

            MessageBus.Main.PublishAsync(new ProjectFinancingFailMsg(projectId));
        }

        /// <summary>
        /// 取得投资进度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="projectId"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static T GetInvestmentProgress<T>(this li_projects pro, Func<decimal, decimal, T> callback)
        {
            Debug.Assert(pro.investment_amount <= pro.financing_amount);
            return callback(pro.investment_amount, pro.financing_amount);
        }

        public static decimal GetInvestmentProgressPercent(this li_projects pro)
        {
            return pro.GetInvestmentProgress((a, b) => (a/b));
        }

        public static string GetInvestmentBalance(this li_projects pro)
        {
            return pro.GetInvestmentProgress((a, b) => (b - a).ToString("n0")) + "元";
        }

        /// <summary>
        /// 获取投资人数
        /// </summary>
        /// <param name="pro"></param>
        /// <returns></returns>
        public static int GetInvestedUserCount(this li_projects pro, Agp2pEnums.ProjectTransactionStatusEnum filter = Agp2pEnums.ProjectTransactionStatusEnum.Success)
        {
            return
                pro.li_project_transactions.Where(t => t.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.Invest &&
                                                       t.status == (int) filter)
                    .GroupBy(t => t.investor)
                    .Count();
        }

        /// <summary>
        /// 累计赚取
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static decimal QueryTotalProfit(this Agp2pDataContext context)
        {
            return context.li_wallets.Select(w => w.total_profit).AsEnumerable().DefaultIfEmpty(0).Sum();
        }

        /// <summary>
        /// 累计投资
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static decimal QueryTotalInvested(this Agp2pDataContext context)
        {
            return context.li_wallets.Select(w => w.total_investment).AsEnumerable().DefaultIfEmpty(0).Sum();
        }

        /// <summary>
        /// 累计待收
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static decimal QueryTotalInvesting(this Agp2pDataContext context)
        {
            return context.li_wallets.Select(w => w.investing_money).AsEnumerable().DefaultIfEmpty(0).Sum();
        }

        /// <summary>
        /// N 日内的成交量
        /// </summary>
        /// <param name="context"></param>
        /// <param name="inDaysEarlier"></param>
        /// <returns></returns>
        public static decimal QueryTradingVolume(this Agp2pDataContext context, int inDaysEarlier)
        {
            var now = DateTime.Now;
            return context.li_project_transactions.Where(
                r =>
                    r.status == (int) Agp2pEnums.ProjectTransactionStatusEnum.Success &&
                    r.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.Invest &&
                    now.AddDays(-inDaysEarlier).Date <= r.create_time.Date && now.Date > r.create_time.Date)
                .Select(r => r.principal).AsEnumerable().DefaultIfEmpty(0).Sum();
        }

        /// <summary>
        /// 获取钱包历史中的收入金额（返回两个数，所以要用回调）
        /// </summary>
        /// <param name="his"></param>
        /// <returns></returns>
        public static T QueryTransactionIncome<T>(li_wallet_histories his, Func<decimal?, decimal?, T> callback)
        {
            if (his.li_bank_transactions != null)
            {
                var chargedValue = his.li_bank_transactions.type == (int) Agp2pEnums.BankTransactionTypeEnum.Withdraw
                    ? (decimal?) null
                    : his.li_bank_transactions.value;
                return callback(chargedValue, null);
            }
            if (his.li_project_transactions != null)
            {
                decimal? receivedPrincipal, profited;
                if (his.action_type == (int) Agp2pEnums.WalletHistoryTypeEnum.Invest || his.action_type == (int) Agp2pEnums.WalletHistoryTypeEnum.InvestSuccess)
                {
                    receivedPrincipal = profited = null;
                }
                else
                {
                    receivedPrincipal = his.li_project_transactions.principal;
                    profited = his.li_project_transactions.interest;
                }
                return callback(receivedPrincipal, profited);
            }
            if (his.li_activity_transactions != null &&
                his.li_activity_transactions.type == (int) Agp2pEnums.ActivityTransactionTypeEnum.Lost)
            {
                return callback(null, null);
            }
            var gainValue = his.li_activity_transactions != null ? his.li_activity_transactions.value : (decimal?) null;
            return callback(gainValue, null);
        }

        public static string QueryTransactionIncome<T>(li_wallet_histories his)
        {
            if (typeof (T) == typeof (string)) // 返回羊角符号
            {
                return QueryTransactionIncome(his, (principal, profit) =>
                {
                    if (principal != null && profit != null)
                        return string.Format("{0:c} + {1:c}", principal, profit);
                    else if (principal == null && profit == null)
                        return "";
                    else if (principal == null)
                        return profit.Value.ToString("c");
                    else
                        return principal.Value.ToString("c");
                });
            }
            else if (typeof (T) == typeof (decimal?)) // 没有羊角符号
            {
                return QueryTransactionIncome(his, (principal, profit) =>
                {
                    if (principal != null && profit != null)
                        return string.Format("{0} + {1}", principal, profit);
                    else if (principal == null && profit == null)
                        return "";
                    else if (principal == null)
                        return profit.Value.ToString();
                    else
                        return principal.Value.ToString();
                });
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 获取钱包历史中的支出金额
        /// </summary>
        /// <param name="his"></param>
        /// <returns></returns>
        public static decimal? QueryTransactionOutcome(this li_wallet_histories his)
        {
            if (his.li_bank_transactions != null)
            {
                return his.li_bank_transactions.type == (int) Agp2pEnums.BankTransactionTypeEnum.Charge
                    ? (decimal?) null
                    : his.li_bank_transactions.value; // 提现
            }
            if (his.li_project_transactions != null)
            {
                if (his.action_type == (int) Agp2pEnums.WalletHistoryTypeEnum.InvestSuccess) // 项目满标不显示支出
                    return null;
                return his.action_type == (int)Agp2pEnums.WalletHistoryTypeEnum.Invest
                    ? his.li_project_transactions.principal // 投资
                    : (decimal?) null;
            }
            // 活动扣除
            if (his.li_activity_transactions != null &&
                his.li_activity_transactions.type == (int) Agp2pEnums.ActivityTransactionTypeEnum.Gain) return null;
            return his.li_activity_transactions?.value;
        }

        public static string GetProjectTermSpanEnumDesc(this li_projects proj)
        {
            var desc = Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectRepaymentTermSpanEnum) proj.repayment_term_span);
            /*if ((Agp2pEnums.ProjectRepaymentTermSpanEnum) proj.repayment_term_span == Agp2pEnums.ProjectRepaymentTermSpanEnum.Month)
                return "个" + desc;*/
            return desc;
        }

        public static string GetProjectStatusDesc(this li_projects proj)
        {
            return Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectStatusEnum) proj.status);
        }

        public static string GetProjectRepaymentTypeDesc(this li_projects proj)
        {
            return Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectRepaymentTypeEnum)proj.repayment_type);
        }

        public static string GetProfitRateYearly(this li_projects proj)
        {
            return proj.dt_article_category.call_index == "newbie" ? "--" : (proj.profit_rate_year/100).ToString("p1");
        }

        public static string GetRepaymentTaskProgress(this li_repayment_tasks task)
        {
            var count = task.li_projects.li_repayment_tasks.Count(t => t.status != (int) Agp2pEnums.RepaymentStatusEnum.Invalid);
            return $"{task.term}/{count}";
        }

        public static string GetInvestContractContext(this Agp2pDataContext context, li_project_transactions investment, string templatePath)
        {
            var project = investment.li_projects;
            //获得投资协议模板（暂时为票据，TODO 其他产品的投资协议）
            var a4Template = File.ReadAllText(templatePath);
            //替换模板内容
            var lastRepaymentTask = project.li_repayment_tasks.LastOrDefault(t => t.status == (int) Agp2pEnums.RepaymentStatusEnum.Unpaid);
            return a4Template.Replace("{title}", project.title + " 票据质押借款协议")
                .Replace("{contract_no}", investment.agree_no)
                //甲方(借款人)信息
                .Replace("{company_name}",
                    project.li_risks.li_loaners.li_loaner_companies != null
                        ? project.li_risks.li_loaners.li_loaner_companies.name
                        : "")
                .Replace("{user_name_loaner}", project.li_risks.li_loaners.dt_users.real_name)
                .Replace("{business_license}", project.li_risks.li_loaners.li_loaner_companies?.business_license_no)
                .Replace("{organization_certificate}", project.li_risks.li_loaners.li_loaner_companies?.organization_no)

                //乙方(投资人)信息
                .Replace("{user_real_name_invester}", investment.dt_users.real_name)
                .Replace("{user_name_invester}", investment.dt_users.user_name)
                .Replace("{id_card_invester}", investment.dt_users.id_card_number)

                //借款明细
                .Replace("{loan_amount}", project.financing_amount.ToString("N0"))
                .Replace("{loan_amount_upper}", project.financing_amount.ToRmbUpper())
                .Replace("{invest_amount}", investment.principal.ToString("N0"))
                .Replace("{invest_amount_upper}", investment.principal.ToRmbUpper())
                .Replace("{profit_rate_year}", (project.profit_rate_year/100).ToString("p2"))
                .Replace("{repayment_term_span}", project.repayment_term_span_count + "天")
                .Replace("{make_loan_date}", project.make_loan_time?.ToString("yyyy年MM月dd日"))
                .Replace("{complete_date}", lastRepaymentTask?.should_repay_time.ToString("yyyy年MM月dd日") ?? "")

                //质押汇票明细
                .Replace("{bill_no}", project.GetMortgageInfo("no"))
                .Replace("{bill_amount}", project.GetMortgageInfo("amount"))
                .Replace("{bill_end_date}", project.GetMortgageInfo("end_time"))
                .Replace("{bill_bank}", project.GetMortgageInfo("bank"));
        }

        public static string GetMortgageInfo(this li_projects proj, string propertyKey)
        {
            if (proj.dt_article_category.call_index == "ypb")
            {
                return proj.li_risks.li_risk_mortgage.Select(rm => rm.li_mortgages).Select(m =>
                {
                    var schemeObj = (JObject)JsonConvert.DeserializeObject(m.li_mortgage_types.scheme);
                    var kv = (JObject) JsonConvert.DeserializeObject(m.properties);
                    var bankName = schemeObj.Cast<KeyValuePair<string, JToken>>().Where(p => p.Key.ToString() == propertyKey)
                        .Select(p => kv[p.Key].ToString()).SingleOrDefault();
                    return bankName;
                }).FirstOrDefault() ?? "";
            }
            return "";
        }

        public static string GetLonerName(this Agp2pDataContext context, int projectId)
        {
            var project = context.li_projects.SingleOrDefault(p => p.id == projectId);
            if (project != null)
            {
                switch (project.type)
                {
                    case (int)Agp2pEnums.LoanTypeEnum.Company:
                        return project.li_risks.li_loaners.li_loaner_companies.name;
                    default:
                        var user = project.li_risks.li_loaners.dt_users;
                        return $"{user.real_name}({user.user_name})";
                }
            }
            return "";
        }

        public static bool IsNewbieProject(this li_projects p)
        {
            return p.dt_article_category.call_index == "newbie";
        }

        public static bool IsHuoqiProject(this li_projects p)
        {
            return p.dt_article_category.call_index == "huoqi";
        }
    }
}