﻿using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Web.UI.WebControls;
using Agp2p.BLL;
using Agp2p.Common;
using Agp2p.Linq2SQL;

namespace Agp2p.Web.admin.statistic
{
    public partial class project_investor_detail : UI.ManagePage
    {
        protected int totalCount;
        protected int page;
        protected int pageSize;

        protected int CategoryId;
        protected Dictionary<int, string> CategoryIdTitleMap;

        private Agp2pDataContext context = new Agp2pDataContext();

        protected void Page_Load(object sender, EventArgs e)
        {
            pageSize = GetPageSize(GetType().Name + "_page_size");
            page = DTRequest.GetQueryInt("page", 1);
            this.CategoryId = DTRequest.GetQueryInt("category_id");
            CategoryIdTitleMap = new Agp2pDataContext().dt_article_category.Where(c => c.channel_id == 6).OrderBy(c => c.sort_id).ToDictionary(c => c.id, c => c.title);

            if (!Page.IsPostBack)
            {
                ChkAdminLevel("statistics_projects_investor_detail", DTEnums.ActionEnum.View.ToString()); //检查权限
                var status = DTRequest.GetQueryString("status");
                if (!string.IsNullOrEmpty(status))
                    rblProjectStatus.SelectedValue = status;

                txtKeywords.Text = DTRequest.GetQueryString("keywords");
                txtStartTime.Text = DTRequest.GetQueryString("startTime");
                txtEndTime.Text = DTRequest.GetQueryString("endTime");
                TreeBind();
                RptBind();
            }
        }

        protected void TreeBind()
        {
            this.ddlCategoryId.Items.Clear();
            this.ddlCategoryId.Items.Add(new ListItem("所有产品", ""));
            this.ddlCategoryId.Items.AddRange(CategoryIdTitleMap.Select(c => new ListItem(c.Value, c.Key.ToString())).ToArray());
            if (this.CategoryId > 0)
            {
                this.ddlCategoryId.SelectedValue = this.CategoryId.ToString();
            }
        }

        protected class ProjectDetail
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public decimal? FinancingAmount { get; set; }
            public string ProfitRateYear { get; set; }
            public string Term { get; set; }
            public string PublishTime { get; set; }
            public string InvestCompleteTime { get; set; }
            public string RepayCompleteTime { get; set; }
            public string Category { get; set; }
        }
        protected class InvestorDetail
        {
            public ProjectDetail Project { get; set; }
            public string InvestorRealName { get; set; }
            public string InvestorUserName { get; set; }
            public string InvestorGroupName { get; set; }
            public decimal InvestValue { get; set; }
            public decimal UnInvestValue { get; set; }
            public string InvestTime { get; set; }
            public int CategoryId { get; set; }
        }

        #region 数据绑定=================================
        private void RptBind()
        {
            var beforePaging = QueryInvestorDetails();
            if (rblType.SelectedValue == "0")
            {
                totalCount = beforePaging.Count();
                rptList.DataSource = beforePaging.Skip(pageSize*(page - 1)).Take(pageSize).ToList();
                rptList.DataBind();

                //绑定页码
                txtPageNum.Text = pageSize.ToString();
                string pageUrl = Utils.CombUrlTxt("project_investor_detail.aspx",
                    "keywords={0}&page={1}&startTime={2}&endTime={3}&status={4}&category_id={5}", txtKeywords.Text, "__id__",
                    txtStartTime.Text,txtEndTime.Text, rblProjectStatus.SelectedValue, ddlCategoryId.SelectedValue);
                PageContent.InnerHtml = Utils.OutPageList(pageSize, page, totalCount, pageUrl, 8);
            }
            else
            {
                rptList_summary.DataSource = QueryGroupData(beforePaging);
                rptList_summary.DataBind();
            }
        }

        private List<InvestorDetail> QueryGroupData(List<InvestorDetail> beforePaging)
        {
            var summaryData =
                    beforePaging.Where(d => d.CategoryId != 0).GroupBy(d => d.CategoryId)
                        .Zip(Utils.Infinite(1), (dg, no) => new { dg, no })
                        .Select(d =>
                        {
                            var data = new InvestorDetail()
                            {
                                Project = new ProjectDetail()
                                {
                                    Index = d.no.ToString(),
                                    Category = CategoryIdTitleMap[d.dg.Key],
                                    FinancingAmount = d.dg.Sum(t => t.Project.FinancingAmount)
                                },
                                InvestValue = d.dg.Sum(t => t.InvestValue)
                            };
                            data.UnInvestValue = (decimal)(data.Project.FinancingAmount - data.InvestValue);
                            return data;
                        }).ToList();
            var totalData = new InvestorDetail()
            {
                Project = new ProjectDetail()
                {
                    Index = null,
                    Category = "总计",
                    FinancingAmount = summaryData.Sum(t => t.Project.FinancingAmount)
                },
                InvestValue = summaryData.Sum(t => t.InvestValue)
            };
            totalData.UnInvestValue = (decimal)(totalData.Project.FinancingAmount - totalData.InvestValue);
            summaryData.Add(totalData);
            return summaryData;
        }

        private List<InvestorDetail> QueryInvestorDetails()
        {
            var loadOptions = new DataLoadOptions();
            loadOptions.LoadWith<li_projects>(t => t.li_repayment_tasks);
            loadOptions.LoadWith<li_projects>(t => t.li_project_transactions);
            loadOptions.LoadWith<li_project_transactions>(t => t.dt_users);
            context.LoadOptions = loadOptions;

            var query = context.li_projects.Where(p => p.title.Contains(txtKeywords.Text));
            if (CategoryId > 0)
                query = query.Where(q => q.category_id == CategoryId);
            if (rblProjectStatus.SelectedValue == "1")
                query = query.Where(p => p.status == (int)Agp2pEnums.ProjectStatusEnum.Financing);
            else if (rblProjectStatus.SelectedValue == "2")
                query = query.Where(p => (int)Agp2pEnums.ProjectStatusEnum.Financing < p.status && p.status < (int) Agp2pEnums.ProjectStatusEnum.ProjectRepaying);
            else if (rblProjectStatus.SelectedValue == "3")
                query = query.Where(p => (int)Agp2pEnums.ProjectStatusEnum.ProjectRepaying <= p.status);
            else
                query = query.Where(p => (int)Agp2pEnums.ProjectStatusEnum.Financing <= p.status);

            if (!string.IsNullOrWhiteSpace(txtStartTime.Text))
            {
                query = query.Where(q => Convert.ToDateTime(txtStartTime.Text) <= q.invest_complete_time.GetValueOrDefault(DateTime.MaxValue).Date);
            }
            if (!string.IsNullOrWhiteSpace(txtEndTime.Text))
            {
                query = query.Where(q => q.invest_complete_time.GetValueOrDefault(DateTime.MaxValue).Date <= Convert.ToDateTime(txtEndTime.Text));
            }

            var beforePaging =
                query.OrderByDescending(r => r.invest_complete_time.GetValueOrDefault(DateTime.MaxValue))
                    .AsEnumerable()
                    .Zip(Utils.Infinite(1), (p, i) => new { project = p, index = i })
                    .SelectMany(pi =>
                    {
                        var p = pi.project;
                        var invested = p.li_project_transactions.Where(
                            t =>
                                t.type == (int)Agp2pEnums.ProjectTransactionTypeEnum.Invest &&
                                t.status == (int)Agp2pEnums.ProjectTransactionStatusEnum.Success).ToList();
                        var investorDetails = invested.Select(tr => new InvestorDetail
                        {
                            Project = new ProjectDetail(),
                            InvestorRealName = tr.dt_users.real_name,
                            InvestorUserName = tr.dt_users.user_name,
                            InvestorGroupName = tr.dt_users.dt_user_groups.title,
                            InvestValue = tr.principal,
                            InvestTime = tr.create_time.ToString(),
                            CategoryId = tr.li_projects.category_id
                        }).Concat(new[]
                        {
                            new InvestorDetail
                            {
                                Project = new ProjectDetail {Name = "小计"},
                                InvestValue = invested.Sum(t => t.principal),
                                CategoryId = 0
                            }
                        }).ToList();
                        investorDetails.First().Project = new ProjectDetail
                        {
                            Index = pi.index.ToString(),
                            Name = p.title,
                            Category = CategoryIdTitleMap[p.category_id],
                            FinancingAmount = p.financing_amount,
                            ProfitRateYear = p.profit_rate_year.ToString(),
                            Term =
                                p.repayment_term_span_count + " " +
                                Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectRepaymentTermSpanEnum)p.repayment_term_span),
                            PublishTime = p.publish_time.Value.ToString(),
                            InvestCompleteTime = p.invest_complete_time?.ToString() ?? "（未满标）",
                            RepayCompleteTime = p.li_repayment_tasks.Any() ? p.li_repayment_tasks.Max(r => r.should_repay_time).ToString() : "",
                        }; // 首个记录呈现项目信息
                        return investorDetails;
                    }).Concat(new[]
                    {
                        new InvestorDetail
                        {
                            Project = new ProjectDetail {Name = "总计"},
                            InvestValue = query.SelectMany(p => p.li_project_transactions.Where(t =>
                                t.type == (int) Agp2pEnums.ProjectTransactionTypeEnum.Invest &&
                                t.status == (int) Agp2pEnums.ProjectTransactionStatusEnum.Success))
                                .Select(t => t.principal)
                                .AsEnumerable().DefaultIfEmpty(0).Sum(),
                            CategoryId = 0
                        }
                    }).ToList();
            return beforePaging;
        }

        #endregion

        //关健字查询
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            Response.Redirect(Utils.CombUrlTxt("project_investor_detail.aspx", "keywords={0}&startTime={1}&endTime={2}&status={3}&category_id={4}", 
                txtKeywords.Text, txtStartTime.Text, txtEndTime.Text, rblProjectStatus.SelectedValue, ddlCategoryId.SelectedValue));
        }

        //设置分页数量
        protected void txtPageNum_TextChanged(object sender, EventArgs e)
        {
            SetPageSize(GetType().Name + "_page_size", txtPageNum.Text.Trim());
            Response.Redirect(Utils.CombUrlTxt("project_investor_detail.aspx", "keywords={0}&startTime={1}&endTime={2}&status={3}&category_id={4}", 
                txtKeywords.Text, txtStartTime.Text, txtEndTime.Text, rblProjectStatus.SelectedValue, ddlCategoryId.SelectedValue));
        }

        //筛选类别
        protected void ddlCategoryId_SelectedIndexChanged(object sender, EventArgs e)
        {
            Response.Redirect(Utils.CombUrlTxt("project_investor_detail.aspx", "keywords={0}&startTime={1}&endTime={2}&status={3}&category_id={4}",
                txtKeywords.Text, txtStartTime.Text, txtEndTime.Text, rblProjectStatus.SelectedValue, ddlCategoryId.SelectedValue));
        }

        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            ChkAdminLevel("statistics_projects_investor_detail", DTEnums.ActionEnum.DownLoad.ToString()); //检查权限
            var beforePaging = QueryInvestorDetails();
            if (rblType.SelectedValue == "0")
            {
                var lsData = beforePaging.Skip(pageSize*(page - 1)).Take(pageSize).Select(d => new
                {
                    d.Project.Index,
                    d.Project.Name,
                    d.Project.Category,
                    d.Project.FinancingAmount,
                    d.Project.ProfitRateYear,
                    d.Project.Term,
                    d.Project.PublishTime,
                    d.Project.InvestCompleteTime,
                    d.Project.RepayCompleteTime,
                    d.InvestorRealName,
                    d.InvestorUserName,
                    d.InvestorGroupName,
                    d.InvestValue,
                    d.InvestTime
                });
                var titles = new[]
                {"序号", "标题", "产品", "融资金额", "年利率", "期限", "发标时间", "满标时间", "到期时间", "投资者", "会员号", "会员组", "投资金额", "投资时间"};
                Utils.ExportXls("借款满标明细", titles, lsData, Response);
            }
            else
            {
                var lsData = QueryGroupData(beforePaging).Select(d => new
                {
                    d.Project.Index,
                    d.Project.Category,
                    d.Project.FinancingAmount,
                    d.InvestValue,
                    d.UnInvestValue
                });
                var titles = new[]
                {"序号", "产品", "借款金额", "已投金额", "未投金额"};
                Utils.ExportXls("借款满标汇总", titles, lsData, Response);
            }
        }

        protected void rblProjectStatus_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            RptBind();
        }

        /// <summary>
        /// 切换汇总/明细列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void rblType_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (rblType.SelectedValue == "0")
            {
                rptList.Visible = true;
                rptList_summary.Visible = false;
                div_page.Visible = true;
            }
            else
            {
                rptList.Visible = false;
                rptList_summary.Visible = true;
                div_page.Visible = false;
            }
            RptBind();
        }
    }
}