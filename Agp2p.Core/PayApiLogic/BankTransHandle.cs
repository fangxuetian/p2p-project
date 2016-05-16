﻿using System;
using System.Linq;
using Agp2p.Common;
using Agp2p.Core.Message;
using Agp2p.Core.Message.PayApiMsg;
using Agp2p.Linq2SQL;

namespace Agp2p.Core.PayApiLogic
{
    /// <summary>
    /// 个人资金账户接口响应
    /// </summary>
    internal class BankTransHandle
    {
        internal static void DoSubscribe()
        {
            MessageBus.Main.Subscribe<RechargeRespMsg>(Recharge);
            MessageBus.Main.Subscribe<WithdrawRespMsg>(WithDraw);
        }

        /// <summary>
        /// 网银/一键充值 响应
        /// </summary>
        /// <param name="msg"></param>
        private static void Recharge(RechargeRespMsg msg)
        {
            try
            {
                //检查请求处理结果 TODO 一键充值只处理异步
                if (msg.CheckResult())
                {
                    //检查签名
                    if (msg.CheckSignature())
                    {
                        Agp2pDataContext context = new Agp2pDataContext();
                        //查找对应的交易流水
                        var trans = context.li_bank_transactions.SingleOrDefault(u => u.no_order == msg.RequestId);
                        if (trans != null)
                        {
                            if (trans.status == (int)Agp2p.Common.Agp2pEnums.BankTransactionStatusEnum.Acting)
                            {
                                context.ConfirmBankTransaction(trans.id, null);

                                //TODO 检查用户资金信息 一键充值后自动取消注销银行卡（同卡进出只能使用绑定卡提现）
                                msg.HasHandle = true;
                            }
                        }
                        else
                        {
                            msg.Remarks = "没有找到平台交易流水记录，交易流水号为：" + msg.RequestId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                msg.Remarks = "内部错误：" + ex.Message;
            }
        }

        /// <summary>
        /// 提现响应处理
        /// </summary>
        /// <param name="msg"></param>
        private static void WithDraw(WithdrawRespMsg msg)
        {
            try
            {
                Agp2pDataContext context = new Agp2pDataContext();
                //同步返回是正在提现申请受理，异步返回才是提现处理
                if (msg.Sync)
                {
                    if (msg.CheckResult())
                    {
                        if (msg.CheckSignature())
                        {
                            //查找对应的请求
                            var requestLog = context.li_pay_request_log.SingleOrDefault(u => u.id == msg.RequestId);
                            if (requestLog != null)
                            {
                                //创建提现申请记录
                                context.Withdraw(Utils.StrToInt(requestLog.remarks, 0),
                                    Utils.StrToDecimal(msg.Sum, 0), msg.RequestId);
                            }
                            else
                            {
                                msg.Remarks = "没有找到对应的请求，流水号为：" + msg.RequestId;
                            }
                        }
                    }
                }
                else
                {
                    //查找对应的交易流水
                    var trans = context.li_bank_transactions.SingleOrDefault(u => u.no_order == msg.RequestId);
                    if (trans != null)
                    {
                        if (msg.CheckResult())
                        {
                            if (msg.CheckSignature())
                            {
                                //当noticeType＝0时，00000代表受理成功；当noticeType＝1时，00000代表提现成功；
                                if (msg.NoticeType == "1")
                                {
                                    context.ConfirmBankTransaction(trans.id, null);
                                    //TODO 检查用户资金信息
                                }
                                msg.HasHandle = true;
                            }
                        }
                        //TODO 自动取消提现 暂人工取消
                        //if (!msg.HasHandle)
                        //    context.CancelBankTransaction(trans.id, 1);
                    }
                    else
                    {
                        msg.Remarks = "没有找到对应的交易记录，交易流水号为：" + msg.RequestId;
                    }
                }
            }
            catch (Exception ex)
            {
                msg.Remarks = "内部错误：" + ex.Message;
            }
        }
    }
}
