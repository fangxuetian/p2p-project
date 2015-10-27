﻿using System;
using System.Collections.Generic;
using System.Linq;
using Agp2p.Common;
using Agp2p.Core;
using Agp2p.Linq2SQL;

namespace Agp2p.Web.admin.statistic
{
    public partial class pay_summary : UI.ManagePage
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
                ChkAdminLevel("pay_summary", DTEnums.ActionEnum.View.ToString()); //检查权限
                var keywords = DTRequest.GetQueryString("keywords");  //关键字查询
                if (!string.IsNullOrEmpty(keywords))
                    txtKeywords.Text = keywords;
                RptBind();
            }
        }

        #region 数据绑定=================================
        private void RptBind()
        {
            var beforePaging = GetRepaySummaryList();
            rptList.DataSource = beforePaging.Skip(pageSize * (page - 1)).Take(pageSize).ToList();
            rptList.DataBind();

            //绑定页码
            txtPageNum.Text = pageSize.ToString();
            string pageUrl = Utils.CombUrlTxt("pay_summary.aspx",
                "keywords={0}&page={1}", txtKeywords.Text, "__id__");
            PageContent.InnerHtml = Utils.OutPageList(pageSize, page, totalCount, pageUrl, 8);
        }

        private IEnumerable<PaySummary> GetRepaySummaryList()
        {
            var allRepayTask =
                context.li_repayment_tasks.Where(r => r.status != (int) Agp2pEnums.RepaymentStatusEnum.Invalid)
                    .OrderByDescending(r => r.should_repay_time);

            var query2 = allRepayTask.GroupBy(r => r.should_repay_time.Year + "年" + r.should_repay_time.Month + "月");
            var query3 = query2.AsEnumerable().OrderByDescending(r => r.Key).Zip(Utils.Infinite(1), (rt, no) => new { rt, no }).Select(rs =>
            {
                var repayTasks = rs.rt;
                PaySummary paySummary = new PaySummary
                {
                    Index = rs.no.ToString(),
                    YearMonth = repayTasks.Key
                };
                //按时还款
                var repayOnTimeTasks =
                    repayTasks.Where(
                        r =>
                            r.status != (int) Agp2pEnums.RepaymentStatusEnum.Unpaid && r.repay_at != null &&
                            r.repay_at <= r.should_repay_time).AsQueryable();
                int payOnTimeCount = 0;
                decimal payOnTimeAmount = 0;
                repayOnTimeTasks.ForEach(rot =>
                {
                    var profiting = getRepayTransactions(rot);
                    payOnTimeCount += profiting.Count();
                    payOnTimeAmount += profiting.Sum(p => p.interest + p.principal ?? p.principal);
                });
               
                //逾期还款
                var repayOverTimeTasks =
                    repayTasks.Where(
                        r =>
                            r.status != (int)Agp2pEnums.RepaymentStatusEnum.Unpaid && r.repay_at != null &&
                            r.repay_at > r.should_repay_time).AsQueryable();
                int payOverTimeCount = 0;
                decimal payOverTimeAmount = 0;
                repayOverTimeTasks.ForEach(ro =>
                {
                    var profiting = getRepayTransactions(ro);
                    payOverTimeCount += profiting.Count();
                    payOverTimeAmount += profiting.Sum(p => p.interest + p.principal ?? p.principal);
                });

                //逾期未还款
                var notRepayOverTimeTasks =
                    repayTasks.Where(
                        r =>
                            r.status == (int)Agp2pEnums.RepaymentStatusEnum.Unpaid && r.repay_at == null &&
                            DateTime.Now > r.should_repay_time).AsQueryable();
                int notRepayOverTimeCount = 0;
                decimal notRepayOverTimeAmount = 0;
                notRepayOverTimeTasks.ForEach(nro =>
                {
                    var profiting = getRepayTransactions(nro);
                    notRepayOverTimeCount += profiting.Count();
                    notRepayOverTimeAmount += profiting.Sum(p => p.interest + p.principal ?? p.principal);
                });

                //未到期还款
                var repayNoYetTasks =
                    repayTasks.Where(
                        r =>
                            r.status == (int)Agp2pEnums.RepaymentStatusEnum.Unpaid && r.repay_at == null &&
                            DateTime.Now <= r.should_repay_time).AsQueryable();
                int repayNoYetCount = 0;
                decimal repayNoYetAmount = 0;
                repayNoYetTasks.ForEach(rny =>
                {
                    var profiting = getRepayTransactions(rny);
                    repayNoYetCount += profiting.Count();
                    repayNoYetAmount += profiting.Sum(p => p.interest + p.principal ?? p.principal);
                });

                paySummary.ShouldRepayCount = payOnTimeCount + payOverTimeCount + notRepayOverTimeCount +
                                              repayNoYetCount;
                paySummary.ShouldRepayAmount = (payOnTimeAmount + payOverTimeAmount + notRepayOverTimeAmount +
                                               repayNoYetAmount).ToString("N");
                paySummary.RepayCount = payOnTimeCount + payOverTimeCount;
                paySummary.RepayAmount = (payOnTimeAmount + payOverTimeAmount + notRepayOverTimeAmount).ToString("N");
                paySummary.RepayRate = (paySummary.RepayCount/paySummary.ShouldRepayCount).ToString("P1");
                paySummary.RepayOnTimeCount = payOnTimeCount;
                paySummary.RepayOnTimeRate = (payOnTimeCount/paySummary.ShouldRepayCount).ToString("P1");
                paySummary.OverCount = payOverTimeCount + notRepayOverTimeCount;
                paySummary.OverRate = (paySummary.OverCount/ paySummary.ShouldRepayCount).ToString("P1");
                paySummary.OverNoRepayCount = notRepayOverTimeCount;
                paySummary.OverNoRepayRate = (notRepayOverTimeCount/paySummary.ShouldRepayCount).ToString("P1");

                return paySummary;
            }).AsQueryable();
            totalCount = query3.Count();
            return query3;
        }

        private List<li_project_transactions> getRepayTransactions(li_repayment_tasks repayment)
        {
            var pro = repayment.li_projects;
            List<li_project_transactions> profiting;
            if (repayment.status != (int)Agp2pEnums.RepaymentStatusEnum.Unpaid)
            {
                // 查询所有收益记录
                profiting = pro.li_project_transactions.Where(
                    t =>
                        t.create_time == repayment.repay_at && t.type != (int)Agp2pEnums.ProjectTransactionTypeEnum.Invest &&
                        t.status == (int)Agp2pEnums.ProjectTransactionStatusEnum.Success).ToList();
            }
            else
            {
                profiting = TransactionFacade.GenerateRepayTransactions(repayment, repayment.should_repay_time); // 临时预计收益
            }
            return profiting;
        }

        public class PaySummary
        {
            public string Index { get; set; }
            /// <summary>
            /// 统计年月
            /// </summary>
            public string YearMonth { get; set; }
            /// <summary>
            /// 应还总数
            /// </summary>
            public decimal ShouldRepayCount { get; set; }
            /// <summary>
            /// 应还总额
            /// </summary>
            public string ShouldRepayAmount { get; set; }
            /// <summary>
            /// 已还款数
            /// </summary>
            public decimal RepayCount { get; set; }
            /// <summary>
            /// 已还款金额
            /// </summary>
            public string RepayAmount { get; set; }
            /// <summary>
            /// 已还款完成率
            /// </summary>
            public string RepayRate { get; set; }
            /// <summary>
            /// 按时还款数
            /// </summary>
            public decimal RepayOnTimeCount { get; set; }
            /// <summary>
            /// 按时完成率
            /// </summary>
            public string RepayOnTimeRate { get; set; }
            /// <summary>
            /// 逾期还款数
            /// </summary>
            public decimal OverCount { get; set; }
            /// <summary>
            /// 逾期占比
            /// </summary>
            public string OverRate { get; set; }
            /// <summary>
            /// 逾期未还数
            /// </summary>
            public decimal OverNoRepayCount { get; set; }
            /// <summary>
            /// 逾期未还占比
            /// </summary>
            public string OverNoRepayRate { get; set; }
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
            Response.Redirect(Utils.CombUrlTxt("pay_summary.aspx", "keywords={0}&status={1}&year={2}&month={3}&orderby={4}",txtKeywords.Text));
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
            Response.Redirect(Utils.CombUrlTxt("pay_summary.aspx", "keywords={0}&status={1}&year={2}&month={3}&orderby={4}", txtKeywords.Text));
        }
        

        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            
        }
    }
}