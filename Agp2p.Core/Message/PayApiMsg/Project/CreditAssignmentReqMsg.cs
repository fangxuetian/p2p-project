﻿using System;
using System.Collections.Generic;
using Agp2p.Common;

namespace Agp2p.Core.Message.PayApiMsg
{
    /// <summary>
    /// 个人债权转让
    /// </summary>
    public class CreditAssignmentReqMsg : FrontEndReqMsg
    {
        public string OriginalRequestId { get; set; } //原请求流水号
        public string OriginalOrderSum { get; set; } //原投标金额
        public string AssignmentSum { get; set; } //转让价格
        public string UndertakeSum { get; set; } //购买价格
        public string PayType { get; set; } //手续费收取方式
        public string CreditValue { get; set; } //转让债权价值
        public string UndertakePercentage { get; set; } //承接系数
        public string ProjectDescription { get; set; } //项目描述
        public string MainAccountType { get; set; } //主账户类型
        public string MainAccountCode { get; set; } //主账户编码 
        public int ClaimId { get; set; } //债权编号
        public string SubledgerList { get; set; } //分账列表
        public string BackUrl { get; set; }
        public string RequestType { get; set; }

        public CreditAssignmentReqMsg(int userId, int claimId, string undertakeSum, string creditValue = "", string undertakePercentage = "", string payType = "1", string mainAccountType = "",
            string mainAccountCode = "")
        {
            UserId = userId;
            ClaimId = claimId;
            UndertakeSum = undertakeSum;
            PayType = payType;
            CreditValue = creditValue;
            UndertakePercentage = undertakePercentage;
            MainAccountType = mainAccountType;
            MainAccountCode = mainAccountCode;

            Api = (int) Agp2pEnums.SumapayApiEnum.CreAs;
            ApiInterface = SumapayConfig.ApiUrl + "user/creditAssignment_toCreditAssignment";
            RequestId = Agp2pEnums.SumapayApiEnum.CreAs.ToString().ToUpper() + Utils.GetOrderNumberLonger();
        }

        public CreditAssignmentReqMsg(int userId, int claimId, string undertakeSum, string backUrl, string payType = "1", string mainAccountType = "", string mainAccountCode = "")
        {
            UserId = userId;
            ClaimId = claimId;
            UndertakeSum = undertakeSum;
            PayType = payType;
            MainAccountType = mainAccountType;
            MainAccountCode = mainAccountCode;
            BackUrl = backUrl;

            RequestType = "PFT0012";
            Api = (int)Agp2pEnums.SumapayApiEnum.CreAM;
            ApiInterface = SumapayConfig.MobileApiUrl + "p2pMobileUser/merchant.do";
            RequestId = Agp2pEnums.SumapayApiEnum.CreAM.ToString().ToUpper() + Utils.GetOrderNumberLonger();
        }

        public override string GetSignature()
        {
            return !string.IsNullOrEmpty(RequestType)
                ? SumaPayUtils.GenSign(
                    RequestType + RequestId + SumapayConfig.MerchantCode + UserId + ProjectCode + ProjectDescription +
                    OriginalRequestId + OriginalOrderSum + AssignmentSum + UndertakeSum + PayType + SubledgerList + CreditValue +
                    UndertakePercentage + MainAccountType + MainAccountCode + SumapayConfig.NoticeUrl +
                    SuccessReturnUrl + FailReturnUrl, SumapayConfig.Key)
                : SumaPayUtils.GenSign(RequestId + SumapayConfig.MerchantCode + UserId + ProjectCode + OriginalRequestId +
                                       OriginalOrderSum + AssignmentSum + UndertakeSum + PayType + SubledgerList +
                                       SuccessReturnUrl + FailReturnUrl, SumapayConfig.Key);
        }

        public override SortedDictionary<string, string> GetSubmitPara()
        {
            var sd = new SortedDictionary<string, string>
            {
                {"requestId", RequestId},
                {"merchantCode", SumapayConfig.MerchantCode},
                {"userIdIdentity", UserId.ToString()},
                {"projectCode", ProjectCode.ToString()},
                {"originalRequestId", OriginalRequestId},
                {"originalOrderSum", OriginalOrderSum},
                {"assignmentSum", AssignmentSum},
                {"undertakeSum", UndertakeSum},
                {"payType", PayType},
                {"subledgerList", SubledgerList},
                {"successReturnUrl", SuccessReturnUrl},
                {"failReturnUrl", FailReturnUrl},
                {"noticeUrl", SumapayConfig.NoticeUrl},
                {"signature", GetSignature()}
            };
            if (!string.IsNullOrEmpty(ProjectDescription)) sd.Add("projectDescription", ProjectDescription);
            if (!string.IsNullOrEmpty(CreditValue)) sd.Add("creditValue", CreditValue);
            if (!string.IsNullOrEmpty(UndertakePercentage)) sd.Add("undertakePercentage", UndertakePercentage);
            if (!string.IsNullOrEmpty(MainAccountType)) sd.Add("mainAccountType", MainAccountType);
            if (!string.IsNullOrEmpty(MainAccountCode)) sd.Add("mainAccountCode", MainAccountCode);
            if (!string.IsNullOrEmpty(BackUrl)) sd.Add("backUrl", BackUrl);
            if (!string.IsNullOrEmpty(RequestType)) sd.Add("requestType", RequestType);

            return sd;
        }

        public void SetSubledgerList(decimal fee, string buyClaimerId)
        {
            var loanSum = Utils.StrToDecimal(UndertakeSum, 0);
            var list = new List<object>
            {
                //转让人收到的款
                new
                {
                    roleType = "0",
                    roleCode = buyClaimerId,
                    inOrOut = "0",
                    sum = (loanSum - fee).ToString("f2")
                }
            };
            //服务费为0不能发生生成分账列表
            if (fee > 0)
            {
                //平台服务费
                list.Add(new
                {
                    roleType = "1",
                    roleCode = SumapayConfig.MerchantCode,
                    inOrOut = "0",
                    sum = fee.ToString("f2")
                });
            }
            SubledgerList = JsonHelper.ObjectToJSON(list);
        }
        
    }
}
