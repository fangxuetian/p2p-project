﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Agp2p.Common;
using Agp2p.Linq2SQL;

namespace Agp2p.Web.admin.transact
{
    public partial class project_profiting_list : UI.ManagePage
    {
        protected int totalCount;
        protected int page;
        protected int pageSize;

        protected string investor_id, project_id;
        private Agp2pDataContext context = new Agp2pDataContext();

        protected void Page_Load(object sender, EventArgs e)
        {
            investor_id = DTRequest.GetQueryString("investor_id");
            project_id = DTRequest.GetQueryString("project_id");

            pageSize = GetPageSize(GetType().Name + "_page_size");
            if (!Page.IsPostBack)
            {
                ChkAdminLevel("manage_project_transaction", DTEnums.ActionEnum.View.ToString()); //检查权限
                RptBind();
            }
        }

        #region 数据绑定=================================
        private void RptBind()
        {
            page = DTRequest.GetQueryInt("page", 1);
            int investorId = Convert.ToInt32(investor_id);
            int projectId = Convert.ToInt32(project_id);
            var query =
                context.li_project_transactions.Where(
                    t =>
                        t.project == projectId && t.investor == investorId &&
                        (t.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.RepayToInvestor && 0 < t.interest));

            totalCount = query.Count();
            rptList.DataSource = query.OrderByDescending(q => q.create_time).Skip(pageSize * (page - 1)).Take(pageSize).ToList();
            rptList.DataBind();

            //绑定页码
            txtPageNum.Text = pageSize.ToString();
            string pageUrl = Utils.CombUrlTxt("project_profiting_list.aspx", "page={0}&project_id={1}&investor_id={2}", "__id__", project_id, investor_id);
            PageContent.InnerHtml = Utils.OutPageList(pageSize, page, totalCount, pageUrl, 8);
        }
        #endregion

        //设置分页数量
        protected void txtPageNum_TextChanged(object sender, EventArgs e)
        {
            SetPageSize(GetType().Name + "_page_size", txtPageNum.Text.Trim());
            Response.Redirect(Utils.CombUrlTxt("project_profiting_list.aspx", "project_id={0}&investor_id={1}", project_id, investor_id));
        }

    }
}