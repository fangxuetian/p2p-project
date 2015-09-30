﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq;
using System.Linq;
using System.Web.UI.WebControls;
using Agp2p.Common;
using Agp2p.Linq2SQL;

namespace Agp2p.Web.admin.statistic
{
    public partial class project_repay_task : UI.ManagePage
    {
        protected int totalCount;
        protected int page;
        protected int pageSize;

        private Agp2pDataContext context = new Agp2pDataContext();

        protected void Page_Load(object sender, EventArgs e)
        {
            pageSize = GetPageSize(10); //每页数量
            page = DTRequest.GetQueryInt("page", 1);

            if (!Page.IsPostBack)
            {
                ChkAdminLevel("statistics_projects_repay_task", DTEnums.ActionEnum.View.ToString()); //检查权限
                var keywords = DTRequest.GetQueryString("keywords");  //关键字查询
                if (!string.IsNullOrEmpty(keywords))
                    txtKeywords.Text = keywords;
                var y = DTRequest.GetQueryString("year");
                var m = DTRequest.GetQueryString("month");

                //txtKeywords.Text = keywords;
                txtYear.Text = y == "" ? DateTime.Now.Year.ToString() : y;
                txtMonth.Text = m == "" ? DateTime.Now.Month.ToString() : m;

                var status = DTRequest.GetQueryString("status");
                if (!string.IsNullOrEmpty(status))
                    rblRepaymentStatus.SelectedValue = status;

                var orderBy = DTRequest.GetQueryString("orderby");
                if (!string.IsNullOrWhiteSpace(orderBy))
                {
                    ddlOrderBy.SelectedValue = orderBy;
                }

                RptBind();
            }
        }

        protected class ProjectDetail
        {
            public string Index { get; set; }
            public string Name { get; set; }
            public decimal? FinancingAmount { get; set; }
            public string ProfitRateYear { get; set; }
            public string Term { get; set; }
            public DateTime? InvestCompleteTime { get; set; }
            public DateTime? RepayCompleteTime { get; set; }
            public string LoanContractNumber { get; set; }
            public string Creditor { get; set; }
        }
        protected class RepaymentTaskAmountDetail
        {
            public ProjectDetail Project { get; set; }
            public DateTime RepayTime { get; set; }
            public string Status { get; set; }
            public decimal RepayPrincipal { get; set; }
            public decimal RepayInterest { get; set; }
            public decimal RepayTotal { get; set; }
        }

        #region 数据绑定=================================
        private void RptBind()
        {
            var beforePaging = QueryProjectRepayTaskData(out totalCount);
            rptList.DataSource = beforePaging.Skip(pageSize * (page - 1)).Take(pageSize).ToList();
            rptList.DataBind();

            //绑定页码
            txtPageNum.Text = pageSize.ToString();
            string pageUrl = Utils.CombUrlTxt("project_repay_task.aspx",
                "keywords={0}&page={1}&status={2}&year={3}&month={4}&orderby={5}", txtKeywords.Text, "__id__",
                rblRepaymentStatus.SelectedValue, txtYear.Text, txtMonth.Text, ddlOrderBy.SelectedValue);
            PageContent.InnerHtml = Utils.OutPageList(pageSize, page, totalCount, pageUrl, 8);
        }

        private IEnumerable<RepaymentTaskAmountDetail> QueryProjectRepayTaskData(out int count)
        {
            var loadOptions = new DataLoadOptions();
            loadOptions.LoadWith<li_repayment_tasks>(t => t.li_projects);
            context.LoadOptions = loadOptions;

            var query1 =
                context.li_repayment_tasks.Where(rt =>
                    (int) Agp2pEnums.ProjectStatusEnum.Financing < rt.li_projects.status && rt.li_projects.title.Contains(txtKeywords.Text))
                    .Where(r =>
                        rblRepaymentStatus.SelectedValue == "0" || Convert.ToByte(rblRepaymentStatus.SelectedValue) == r.status
                        || (Convert.ToByte(rblRepaymentStatus.SelectedValue) == (int) Agp2pEnums.RepaymentStatusEnum.ManualPaid && (int) Agp2pEnums.RepaymentStatusEnum.ManualPaid <= r.status));

            if (ddlOrderBy.SelectedValue == "invest_complete_time")
            {
                if (txtYear.Text != "-")
                    query1 = query1.Where(rt => rt.li_projects.invest_complete_time.Value.Year == Convert.ToInt32(txtYear.Text));
                if (txtMonth.Text != "-")
                    query1 = query1.Where(rt => rt.li_projects.invest_complete_time.Value.Month == Convert.ToInt32(txtMonth.Text));
            }
            else if (ddlOrderBy.SelectedValue == "should_repay_time")
            {
                if (txtYear.Text != "-")
                    query1 = query1.Where(rt => rt.should_repay_time.Year == Convert.ToInt32(txtYear.Text));
                if (txtMonth.Text != "-")
                    query1 = query1.Where(rt => rt.should_repay_time.Month == Convert.ToInt32(txtMonth.Text));
            }
            count = query1.Count();

            var query2 = query1.GroupBy(rt => rt.project);
            if (ddlOrderBy.SelectedValue == "invest_complete_time")
            {
                query2 = query2.OrderBy(g => g.First().li_projects.invest_complete_time);
            }
            else if (ddlOrderBy.SelectedValue == "should_repay_time")
            {
                query2 = query2.OrderBy(g => g.First().should_repay_time);
            }

            return query2.AsEnumerable().Zip(Utils.Infinite(1), (rg, i) => new {rg, i}).SelectMany(rgi =>
            {
                var p = rgi.rg.First().li_projects;
                var projectDetail = new ProjectDetail
                {
                    Index = rgi.i.ToString(),
                    Name = p.title,
                    FinancingAmount = p.financing_amount == 0 ? (decimal?) null : p.financing_amount,
                    ProfitRateYear = p.profit_rate_year.ToString(),
                    Term =
                        p.repayment_term_span_count + " " +
                        Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectRepaymentTermSpanEnum) p.repayment_term_span),
                    InvestCompleteTime = p.invest_complete_time,
                    RepayCompleteTime = p.li_repayment_tasks.Select(r => r.should_repay_time).Last(),
                    Creditor = p.li_risks.li_creditors.real_name
                };
                int j = 0;
                return rgi.rg.Select(rg => new RepaymentTaskAmountDetail
                {
                    Project = j++ == 0 ? projectDetail : new ProjectDetail(),
                    RepayTime = rg.should_repay_time,
                    Status = Utils.GetAgp2pEnumDes((Agp2pEnums.RepaymentStatusEnum) rg.status),
                    RepayPrincipal = rg.repay_principal,
                    RepayInterest = rg.repay_interest,
                    RepayTotal = (rg.repay_interest + rg.repay_principal),
                });
            });
        }

        #endregion

        #region 返回每页数量=============================
        private int GetPageSize(int _default_size)
        {
            int _pagesize;
            if (int.TryParse(Utils.GetCookie(GetType().Name + "_page_size"), out _pagesize))
            {
                if (_pagesize > 0)
                {
                    return _pagesize;
                }
            }
            return _default_size;
        }
        #endregion

        //关健字查询
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            Response.Redirect(Utils.CombUrlTxt("project_repay_task.aspx", "keywords={0}&status={1}&year={2}&month={3}&orderby={4}",
                txtKeywords.Text, rblRepaymentStatus.SelectedValue, txtYear.Text, txtMonth.Text, ddlOrderBy.SelectedValue));
        }

        //设置分页数量
        protected void txtPageNum_TextChanged(object sender, EventArgs e)
        {
            int _pagesize;
            if (int.TryParse(txtPageNum.Text.Trim(), out _pagesize))
            {
                if (_pagesize > 0)
                {
                    Utils.WriteCookie(GetType().Name + "_page_size", _pagesize.ToString(), 14400);
                }
            }
            Response.Redirect(Utils.CombUrlTxt("project_repay_task.aspx", "keywords={0}&status={1}&year={2}&month={3}&orderby={4}",
                txtKeywords.Text, rblRepaymentStatus.SelectedValue, txtYear.Text, txtMonth.Text, ddlOrderBy.SelectedValue));
        }

        protected DateTime GetRepaymentCompleteTime(li_projects liProjects)
        {
            return liProjects.li_repayment_tasks.Max(r => r.should_repay_time);
        }

        protected object EvalWhenFirstTerm(RepeaterItem container, string field)
        {
            return IsFirstTerm(container) ? Eval(field) : "";
        }

        protected static bool IsFirstTerm(RepeaterItem container)
        {
            return ((li_repayment_tasks) container.DataItem).term == 1;
        }

        protected void rblRepaymentStatus_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            RptBind();
        }

        protected void txtMonth_OnTextChanged(object sender, EventArgs e)
        {
            if (txtMonth.Text == "")
                txtMonth.Text = "-"; // 减号表示不限，如果不用减号的话会认为是首次加载网页，未填写月份，会默认设为当前月份
            RptBind();
        }

        protected void txtYear_OnTextChanged(object sender, EventArgs e)
        {
            if (txtYear.Text == "")
                txtYear.Text = "-";
            RptBind();
        }

        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            int totalCount;
            var beforePaging = QueryProjectRepayTaskData(out totalCount);
            var lsData = beforePaging.Skip(pageSize * (page - 1)).Take(pageSize).Select(d => new
            {
                d.Project.Index,
                d.Project.Name,
                d.Project.FinancingAmount,
                d.Project.ProfitRateYear,
                d.Project.Term,
                d.Project.InvestCompleteTime,
                d.Project.RepayCompleteTime,
                d.RepayTime,
                d.Status,
                d.RepayPrincipal,
                d.RepayInterest,
                d.RepayTotal,
                d.Project.LoanContractNumber,
                d.Project.Creditor,
            });

            var titles = new[] { "序号", "项目名称", "融资金额", "年利率", "期限", "满标日", "到期日", "应付本息时间", "状态", "应付本金", "应付利息", "本息合计", "对应借款合同号", "债权人" };
            Utils.ExportXls("项目投资明细", titles, lsData, Response);
        }

        protected void ddlOrderBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            RptBind();
        }
    }
}