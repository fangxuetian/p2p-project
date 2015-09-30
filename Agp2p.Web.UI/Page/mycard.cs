﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Services;
using System.Web.Services;
using Agp2p.Linq2SQL;
using Newtonsoft.Json;

namespace Agp2p.Web.UI.Page
{
    public class mycard : usercenter
    {
        /// <summary>
        /// 重写虚方法,此方法在Init事件执行
        /// </summary>
        protected override void InitPage()
        {
            base.InitPage();
        }

        [WebMethod]
        public static string AjaxAppendCard(string cardNumber, string bankName, string bankLocation, string openingBank)
        {
            var userInfo = GetUserInfo();
            if (userInfo == null)
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return "请先登录";
            }
            // 检查用户的输入
            if (!new Regex(@"^\d{16,}$").IsMatch(cardNumber))
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "银行卡号格式不正确";
            }
            if (!new Regex(@"^[\u4e00-\u9fa5]+$").IsMatch(bankName))
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "银行名称格式不正确";
            }
            if (!new Regex(@"^[\u4e00-\u9fa5;]+$").IsMatch(bankLocation))
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "银行所在地格式不正确";
            }
            if (!new Regex(@"^[^<>]*$").IsMatch(openingBank))
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "开户行名称格式不正确";
            }
            
            var context = new Agp2pDataContext();
            var user = context.dt_users.Single(u => u.id == userInfo.id);
            var card = new li_bank_accounts
            {
                dt_users = user,
                account = cardNumber,
                bank = bankName,
                last_access_time = DateTime.Now,
                location = bankLocation,
                opening_bank = openingBank,
            };
            context.li_bank_accounts.InsertOnSubmit(card);
            context.SubmitChanges();
            return "保存银行卡信息成功";
        }

        [WebMethod]
        [ScriptMethod(UseHttpGet = true)]
        public static string AjaxQueryCardInfo(int cardId)
        {
            var userInfo = GetUserInfo();
            if (userInfo == null)
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return "请先登录";
            }

            var context = new Agp2pDataContext();
            var card = context.li_bank_accounts.SingleOrDefault(c => c.owner == userInfo.id && c.id == cardId);
            if (card == null)
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return "找不到卡的信息";
            }
            return JsonConvert.SerializeObject(new
            {
                CardNumber = card.account,
                BankName = card.bank,
                OpeningBank = card.opening_bank,
                BankLocation = card.location
            });
        }

        [WebMethod]
        public static string AjaxModifyCard(int cardId, string bankName, string bankLocation, string openingBank)
        {
            var userInfo = GetUserInfo();
            if (userInfo == null)
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return "请先登录";
            }
            var context = new Agp2pDataContext();
            var card = context.li_bank_accounts.SingleOrDefault(c => c.owner == userInfo.id && c.id == cardId);
            if (card == null)
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return "找不到卡的信息";
            }
            // 检查用户的输入
            if (!new Regex(@"^[\u4e00-\u9fa5]+$").IsMatch(bankName))
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "银行名称格式不正确";
            }
            if (!new Regex(@"^[\u4e00-\u9fa5;]+$").IsMatch(bankLocation))
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "银行所在地格式不正确";
            }
            if (!new Regex(@"^[^<>]*$").IsMatch(openingBank))
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return "开户行名称格式不正确";
            }

            card.bank = bankName;
            card.location = bankLocation;
            card.opening_bank = openingBank;
            context.SubmitChanges();
            return "修改成功";
        }

        [WebMethod]
        public static string AjaxDeleteCardInfo(int cardId)
        {
            var userInfo = GetUserInfo();
            if (userInfo == null)
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return "请先登录";
            }
            var context = new Agp2pDataContext();
            var card = context.li_bank_accounts.SingleOrDefault(c => c.owner == userInfo.id && c.id == cardId);
            if (card == null)
            {
                HttpContext.Current.Response.TrySkipIisCustomErrors = true;
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return "该卡已被删除";
            }
            context.li_bank_accounts.DeleteOnSubmit(card);
            context.SubmitChanges();
            return "删除成功";
        }
    }
}