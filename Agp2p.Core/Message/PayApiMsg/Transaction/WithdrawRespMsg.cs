﻿namespace Agp2p.Core.Message.PayApiMsg
{
    /// <summary>
    /// 个人提现响应
    /// </summary>
    public class WithdrawRespMsg : BaseRespMsg
    {
        public string Sum { get; set; }//提现金额
        public string UserBalance { get; set; }//账户总额
        public string WithdrawableBalance { get; set; }//可用余额
        public string FrozenBalance { get; set; }//冻结余额
        public string UnsettledBalance { get; set; }//未结金额
        public string PayType { get; set; }//手续费收取方式
        public string MainAccountType { get; set; }//主账户类型
        public string MainAccountCode { get; set; }//主账户编码
        public string BankAccount { get; set; }//银行账号
        public string BankName { get; set; }//银行名称
        public string Name { get; set; }//用户姓名
        public string RequestTime { get; set; }//受理时间
        public string DealTime { get; set; }//处理时间
        public string FailReason { get; set; }//失败原因描述
        public bool Sync { get; set; }

        public WithdrawRespMsg(bool sync = false)
        {
            Sync = sync;
        }

        public override bool CheckSignature()
        {
            return base.CheckSignature(RequestId + Result + Sum + UserIdIdentity + UserBalance);
        }
    }
}
