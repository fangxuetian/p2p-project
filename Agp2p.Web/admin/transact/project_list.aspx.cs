﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Agp2p.BLL;
using Agp2p.Common;
using Agp2p.Core;
using Agp2p.Linq2SQL;

namespace Agp2p.Web.admin.transact
{
    public partial class project_list : UI.ManagePage
    {
        protected int totalCount;
        protected int page;
        protected int pageSize;

        protected string keywords = string.Empty;
        private Agp2pDataContext context = new Agp2pDataContext();

        protected void Page_Load(object sender, EventArgs e)
        {
            //keywords = DTRequest.GetQueryString("keywords");

            pageSize = GetPageSize(GetType().Name + "_page_size");
            if (!Page.IsPostBack)
            {
                ChkAdminLevel("manage_project_transaction", DTEnums.ActionEnum.View.ToString()); //检查权限
                var keywords = DTRequest.GetQueryString("keywords");
                if (!string.IsNullOrEmpty(keywords))
                    txtKeywords.Text = keywords;
                RptBind();
            }
        }

        #region 数据绑定=================================
        private void RptBind()
        {
            page = DTRequest.GetQueryInt("page", 1);
           //txtKeywords.Text = keywords;
            var query = context.li_projects
                .Where(p => (int)Agp2pEnums.ProjectStatusEnum.Financing <= p.status && p.title.Contains(txtKeywords.Text));

            totalCount = query.Count();
            rptList.DataSource = query.OrderByDescending(q => q.add_time).Skip(pageSize * (page - 1)).Take(pageSize).ToList();
            rptList.DataBind();

            //绑定页码
            txtPageNum.Text = pageSize.ToString();
            string pageUrl = Utils.CombUrlTxt("project_list.aspx", "keywords={0}&page={1}", txtKeywords.Text, "__id__");
            PageContent.InnerHtml = Utils.OutPageList(pageSize, page, totalCount, pageUrl, 8);
        }
        #endregion

        //关健字查询
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            Response.Redirect(Utils.CombUrlTxt("project_list.aspx", "keywords={0}", txtKeywords.Text));
        }

        //设置分页数量
        protected void txtPageNum_TextChanged(object sender, EventArgs e)
        {
            SetPageSize(GetType().Name + "_page_size", txtPageNum.Text.Trim());
            Response.Redirect(Utils.CombUrlTxt("project_list.aspx", "keywords={0}", txtKeywords.Text));
        }

        protected string CalcProjectProgress(li_projects project)
        {
            return (project.investment_amount/project.financing_amount).ToString("P2");
        }

        protected string QueryRepaymentProgress(li_projects pro)
        {
            if (pro.status < (int) Agp2pEnums.ProjectStatusEnum.ProjectRepaying)
                return "未开始还款";
            var repayments = pro.li_repayment_tasks.Select(r => r.status).ToList();
            return string.Format("{0}/{1}", repayments.Count(r => r != (int) Agp2pEnums.RepaymentStatusEnum.Unpaid && r != (int) Agp2pEnums.RepaymentStatusEnum.OverTime), repayments.Count);
        }

        protected void btnFinishInvestment_OnClick(object sender, EventArgs e)
        {
            try
            {
                int projectId = Convert.ToInt32(((Button)sender).CommandArgument);
                var pro = context.FinishInvestment(projectId);
                AddAdminLog(DTEnums.ActionEnum.Cancel.ToString(), "截标成功：" + pro.title); //记录日志
                JscriptMsg("截标成功：" + pro.title, Utils.CombUrlTxt("project_list.aspx", "page={0}&keywords={1}", page.ToString(), txtKeywords.Text), "Success");
            }
            catch (Exception ex)
            {
                JscriptMsg("截标失败！" + ex.Message, Utils.CombUrlTxt("project_list.aspx", "page={0}&keywords={1}", page.ToString(), txtKeywords.Text), "Failure");
            }
        }

        protected void btnEarlierRepay_OnClick(object sender, EventArgs e)
        {
            try
            {
                int projectId = Convert.ToInt32(((Button)sender).CommandArgument);
                var pro = context.EarlierRepayAll(projectId, (decimal) Costconfig.earlier_pay);
                AddAdminLog(DTEnums.ActionEnum.Edit.ToString(), "提前还款成功：" + pro.title + " 保留 " + Costconfig.earlier_pay + "% 利润"); //记录日志
                JscriptMsg("提前还款成功：" + pro.title + " 保留 " + Costconfig.earlier_pay + "% 利润",
                    Utils.CombUrlTxt("project_list.aspx", "page={0}&keywords={1}", page.ToString(), txtKeywords.Text), "Success");
            }
            catch (Exception ex)
            {
                JscriptMsg("提前还款失败！" + ex.Message, Utils.CombUrlTxt("project_list.aspx", "page={0}&keywords={1}", page.ToString(), txtKeywords.Text), "Failure");
            }
        }
    }
}