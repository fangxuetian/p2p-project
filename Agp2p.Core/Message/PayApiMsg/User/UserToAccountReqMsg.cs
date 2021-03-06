﻿using System;
using System.Collections.Generic;
using Agp2p.Common;


namespace Agp2p.Core.Message.PayApiMsg
{
    /// <summary>
    /// 跳转个人账户管理
    /// </summary>
    public class UserToAccountReqMsg : FrontEndReqMsg
    {
        public string BackUrl { get; set; }
        public string RequestType { get; set; }

        public UserToAccountReqMsg(int userId)
        {
            UserId = userId;

            Api = (int) Agp2pEnums.SumapayApiEnum.Accou;
            ApiInterface = SumapayConfig.ApiUrl + "user/accountManage_toAccountManage";
            RequestId = Agp2pEnums.SumapayApiEnum.Accou.ToString().ToUpper() + Utils.GetOrderNumberLonger();
        }

        public UserToAccountReqMsg(int userId, string backUrl)
        {
            UserId = userId;
            BackUrl = backUrl;

            RequestType = "PFT0013";
            Api = (int)Agp2pEnums.SumapayApiEnum.AccoM;
            ApiInterface = SumapayConfig.MobileApiUrl + "p2pMobileUser/merchant.do";
            RequestId = Agp2pEnums.SumapayApiEnum.AccoM.ToString().ToUpper() + Utils.GetOrderNumberLonger();
        }

        protected UserToAccountReqMsg()
        {
        }

        public override string GetSignature()
        {
            return !string.IsNullOrEmpty(RequestType)
                ? SumaPayUtils.GenSign(
                    RequestType + RequestId + SumapayConfig.MerchantCode + UserId + SumapayConfig.NoticeUrl +
                    SuccessReturnUrl + FailReturnUrl, SumapayConfig.Key)
                : SumaPayUtils.GenSign(RequestId + SumapayConfig.MerchantCode + UserId, SumapayConfig.Key);
        }

        public override SortedDictionary<string, string> GetSubmitPara()
        {
            var sd = new SortedDictionary<string, string>
            {
                {"requestId", RequestId},
                {"merchantCode", SumapayConfig.MerchantCode},
                {"userIdIdentity", UserId.ToString()},
                {"signature", GetSignature()}
            };
            if (!string.IsNullOrEmpty(BackUrl)) sd.Add("backUrl", BackUrl);
            if (!string.IsNullOrEmpty(BackUrl)) sd.Add("noticeUrl", SumapayConfig.NoticeUrl);
            if (!string.IsNullOrEmpty(SuccessReturnUrl)) sd.Add("successReturnUrl", SuccessReturnUrl);
            if (!string.IsNullOrEmpty(FailReturnUrl)) sd.Add("failReturnUrl", FailReturnUrl);
            if (!string.IsNullOrEmpty(RequestType)) sd.Add("requestType", RequestType);
            return sd;
        }
    }
}
