﻿using System;
using System.Linq;
using Agp2p.Common;
using Agp2p.Core.Message;
using Agp2p.Core.Message.PayApiMsg;
using Agp2p.Linq2SQL;

namespace Agp2p.Core.PayApiLogic
{
    /// <summary>
    /// 项目交易接口响应处理
    /// </summary>
    internal class ProjectTransHandle
    {
        internal static void DoSubscribe()
        {
            MessageBus.Main.Subscribe<BidRespMsg>(Bid);//投资
            MessageBus.Main.Subscribe<WithDrawalRespMsg>(Withdrawal);//撤销投标
            MessageBus.Main.Subscribe<RepealProjectRespMsg>(RepealProject);//流标
            MessageBus.Main.Subscribe<MakeLoanRespMsg>(MakeLoan);//放款
            MessageBus.Main.Subscribe<RepayRespMsg>(Repay);//还款
            MessageBus.Main.Subscribe<ReturnPrinInteRespMsg>(ReturnPrinInte);//本息到账
            MessageBus.Main.Subscribe<CreditAssignmentRespMsg>(CreditAssignment);//债权转让
        }

        /// <summary>
        /// 个人投标/自动投标 响应
        /// </summary>
        /// <param name="msg"></param>
        private static void Bid(BidRespMsg msg)
        {
            try
            {
                //检查签名
                if (msg.CheckSignature())
                {
                    //检查请求处理结果
                    if (msg.CheckResult())
                    {
                        Agp2pDataContext context = new Agp2pDataContext();
                        //查找对应的交易流水
                        var trans = context.li_project_transactions.SingleOrDefault(u => u.no_order == msg.RequestId);
                        if (trans != null)
                        {
                            //TODO 更新流水信息 投标 检查用户资金信息


                            context.SubmitChanges();
                            msg.HasHandle = true;
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
        /// 个人撤标响应
        /// </summary>
        /// <param name="msg"></param>
        private static void Withdrawal(WithDrawalRespMsg msg)
        {
            try
            {
                //检查签名
                if (msg.CheckSignature())
                {
                    //检查请求处理结果
                    if (msg.CheckResult())
                    {
                        Agp2pDataContext context = new Agp2pDataContext();
                        //查找对应的原交易流水
                        var trans = context.li_project_transactions.SingleOrDefault(u => u.no_order == msg.BidRequestId);
                        if (trans != null)
                        {
                            //TODO 撤销投资


                            context.SubmitChanges();
                            msg.HasHandle = true;
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
        /// 流标普通项目
        /// </summary>
        /// <param name="msg"></param>
        private static void RepealProject(RepealProjectRespMsg msg)
        {
            try
            {
                //检查签名
                if (msg.CheckSignature())
                {
                    //检查请求处理结果
                    if (msg.CheckResult())
                    {
                        Agp2pDataContext context = new Agp2pDataContext();
                        //查找对应的项目
                        var pro = context.li_projects.SingleOrDefault(p => p.id == Utils.StrToInt(msg.ProjectCode, 0));
                        if (pro != null)
                        {
                            //TODO 流标


                            context.SubmitChanges();
                            msg.HasHandle = true;
                        }
                        else
                        {
                            msg.Remarks = "没有找到平台项目，项目编号为：" + msg.ProjectCode;
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
        /// 项目放款
        /// </summary>
        /// <param name="msg"></param>
        private static void MakeLoan(MakeLoanRespMsg msg)
        {
            try
            {
                //检查签名
                if (msg.CheckSignature())
                {
                    //检查请求处理结果
                    if (msg.CheckResult())
                    {
                        Agp2pDataContext context = new Agp2pDataContext();
                        //查找对应的项目
                        var pro = context.li_projects.SingleOrDefault(p => p.id == Utils.StrToInt(msg.ProjectCode, 0));
                        if (pro != null)
                        {
                            //TODO　放款
                            if (!msg.Sync)
                            {
                                
                            }


                            context.SubmitChanges();
                            msg.HasHandle = true;
                        }
                        else
                        {
                            msg.Remarks = "没有找到平台项目，项目编号为：" + msg.ProjectCode;
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
        /// 还款
        /// </summary>
        /// <param name="msg"></param>
        private static void Repay(RepayRespMsg msg)
        {
            try
            {
                //检查签名
                if (msg.CheckSignature())
                {
                    //检查请求处理结果
                    if (msg.CheckResult())
                    {
                        Agp2pDataContext context = new Agp2pDataContext();
                        //查找对应的项目
                        var pro = context.li_projects.SingleOrDefault(p => p.id == Utils.StrToInt(msg.ProjectCode, 0));
                        if (pro != null)
                        {
                            //TODO 还款


                            context.SubmitChanges();
                            msg.HasHandle = true;
                        }
                        else
                        {
                            msg.Remarks = "没有找到平台项目，项目编号为：" + msg.ProjectCode;
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
        /// 本息到账
        /// </summary>
        /// <param name="msg"></param>
        private static void ReturnPrinInte(ReturnPrinInteRespMsg msg)
        {
            try
            {
                //检查签名
                if (msg.CheckSignature())
                {
                    //检查请求处理结果
                    if (msg.CheckResult())
                    {
                        Agp2pDataContext context = new Agp2pDataContext();
                        //查找对应的项目
                        var pro = context.li_projects.SingleOrDefault(p => p.id == Utils.StrToInt(msg.ProjectCode, 0));
                        if (pro != null)
                        {
                            //TODO 回款


                            context.SubmitChanges();
                            msg.HasHandle = true;
                        }
                        else
                        {
                            msg.Remarks = "没有找到平台项目，项目编号为：" + msg.ProjectCode;
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
        /// 债权转让
        /// </summary>
        /// <param name="msg"></param>
        private static void CreditAssignment(CreditAssignmentRespMsg msg)
        {
            try
            {
                //检查签名
                if (msg.CheckSignature())
                {
                    //检查请求处理结果
                    if (msg.CheckResult())
                    {
                        Agp2pDataContext context = new Agp2pDataContext();
                        //查找对应的债权
                        var pro = context.li_projects.SingleOrDefault(p => p.id == Utils.StrToInt(msg.ProjectCode, 0));
                        if (pro != null)
                        {
                            //TODO 债权转让

                            context.SubmitChanges();
                            msg.HasHandle = true;
                        }
                        else
                        {
                            msg.Remarks = "没有找到平台项目，项目编号为：" + msg.ProjectCode;
                        }
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