﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services;
using Agp2p.Linq2SQL;
using Newtonsoft.Json;
using Agp2p.Common;

namespace Agp2p.Web.UI.Page
{
    public partial class add_order : usercenter
    {
        Agp2pDataContext context = new Agp2pDataContext();
        protected int goods_id;
        protected string order_count;
        protected dt_article articleModel;
        protected dt_article_attribute_value articleDetail;

        /// <summary>
        /// 重写父类的虚方法,此方法将在Init事件前执行
        /// </summary>
        protected override void ShowPage()
        {
            Init += Page_Init;
            goods_id = DTRequest.GetQueryInt("id");
            order_count = DTRequest.GetQueryString("count");
            articleModel = context.dt_article.SingleOrDefault(a => a.id == goods_id);
            articleDetail = articleModel.dt_article_attribute_value;
        }

        /// <summary>
        /// OnInit事件
        /// </summary>
        void Page_Init(object sender, EventArgs e)
        {

        }

        [WebMethod]
        public new static string AjaxQueryAddress()
        {
            var userInfo = GetUserInfoByLinq();
            HttpContext.Current.Response.TrySkipIisCustomErrors = true;
            if (userInfo == null)
            {
                HttpContext.Current.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                return "请先登录";
            }
            var context = new Agp2pDataContext();
            var addressBook = context.dt_user_addr_book.Where(b => b.user_id == userInfo.id).Select(a => new
            {
                orderName = a.accept_name,
                orderPhone=a.telphone,
                address = a.address,
                postalCode=a.post_code,
            });
            return JsonConvert.SerializeObject(addressBook);
        }

        [WebMethod]
        public new static string AjaxAppendAddress(string address, string postalCode, string orderName, string orderPhone)
        {
            var userInfo = GetUserInfoByLinq();
            HttpContext.Current.Response.TrySkipIisCustomErrors = true;
            if (userInfo == null)
            {
                HttpContext.Current.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
                return "请先登录";
            }
            if (!new Regex(@"^\d{11,}$").IsMatch(orderPhone))
            {
                HttpContext.Current.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return "手机号码格式不正确";
            }
            var context= new Agp2pDataContext();
            var addressBook = new dt_user_addr_book
            {
                user_id = userInfo.id,
                user_name = userInfo.real_name,
                accept_name = orderName,
                area = "",
                address = address,
                telphone = orderPhone,
                post_code = postalCode,
                add_time = DateTime.Now,
            };
            context.dt_user_addr_book.InsertOnSubmit(addressBook);
            context.SubmitChanges();
            return "保存收货地址信息成功";
        }

        [WebMethod]
        public new static string AjaxDeleteAddress(string address)
        {
            var userInfo = GetUserInfoByLinq();
            HttpContext.Current.Response.TrySkipIisCustomErrors = true;
            if (userInfo == null)
            {
                HttpContext.Current.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return "请先登录";
            }
            var context = new Agp2pDataContext();
            var addressBook = context.dt_user_addr_book.SingleOrDefault(b => b.address == address);
            context.dt_user_addr_book.DeleteOnSubmit(addressBook);
            context.SubmitChanges();
            return "删除成功";
        }
    }
}
