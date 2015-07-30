﻿using System;
using Lip2p.API.Payment.Ecpss;
using Lip2p.Linq2SQL;
using Lip2p.BLL;
using System.Linq;
using System.ComponentModel;

namespace Lip2p.Web.api.payment.ecpss
{
    public partial class notify_url : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string billNo = Request.Params["BillNo"];//商户流水号
            string amount = Request.Params["Amount"];//实际成交金额
            string succeed = Request.Params["Succeed"];//状态码
            string result = Request.Params["Result"];//支付结果描述
            string signMD5info = Request.Params["SignMD5info"];//md5签名

            if (Helper.CheckReturnMD5(billNo, amount, succeed, signMD5info))
            {
                //md5校验成功，输出OK
                Response.Write("ok");
                if (succeed.Equals("88"))
                {
                    //开始下面的操作，处理订单
                    try
                    {
                        var context = new Lip2p.Linq2SQL.Lip2pDataContext();
                        var order = context.li_bank_transactions.FirstOrDefault(b => b.no_order == billNo);
                        if (order != null && order.status == (int)Lip2p.Common.Lip2pEnums.BankTransactionStatusEnum.Acting)
                        {
                            RechargeComplete.DoRecharge(context, order);
                            new BLL.manager_log().Add(1, "admin", "ReCharge", "支付成功（" + billNo + "）！来自异步通知。");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidEnumArgumentException("确认充值信息失败（" + billNo + "）：" + ex.Message);
                    }
                }
                else
                    new BLL.manager_log().Add(1, "admin", "ReCharge", "支付失败（" + billNo + "）：" + Helper.GetResultInfo(succeed));
            }
            else
            {
                new BLL.manager_log().Add(1, "admin", "ReCharge", "支付失败（" + billNo + "），MD5验证不通过！");
                Response.Write("fail");
            }
        }
    }

}
