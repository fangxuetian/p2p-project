﻿using System;
using System.Data.Linq;
using System.Linq;
using System.Web.UI.WebControls;
using Agp2p.BLL;
using Agp2p.Common;
using Agp2p.Linq2SQL;
using System.Collections.Generic;
using Agp2p.Core;
using Agp2p.Core.Message;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Agp2p.Web.admin.audit
{
    public partial class bank_transaction_withdrawing_list : UI.ManagePage
    {
        protected int totalCount;
        protected int page;
        protected int pageSize;
        protected int UserGroud;
        public decimal value = 0;
        protected decimal value1 = 0;

        private Agp2pDataContext context = new Agp2pDataContext();

        protected class GroupByUserGroupSummery
        {
            public int? Index { get; set; }
            public string GroupName { get; set; }
            public decimal WithdrawAmount { get; set; }
            public decimal TransactionAmount { get; set; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            UserGroud = DTRequest.GetQueryInt("UserGroud");

            pageSize = GetPageSize(GetType().Name + "_page_size"); //每页数量
            page = DTRequest.GetQueryInt("page", 1);
            if (!Page.IsPostBack)
            {
                ChkAdminLevel("manage_bank_transaction_withdraw", DTEnums.ActionEnum.View.ToString()); //检查权限
                var status = DTRequest.GetQueryString("status");
                if (!string.IsNullOrEmpty(status))
                {
                    rblBankTransactionStatus.SelectedValue = status;
                }
                txtKeywords.Text = DTRequest.GetQueryString("keywords");
                txtStartTime.Text = DTRequest.GetQueryString("startTime");
                txtEndTime.Text = DTRequest.GetQueryString("endTime");
                TreeBind();
                RptBind();
            }
        }

        #region 绑定用户分组=================================
        protected void TreeBind()
        {
            ddlUserGroud.Items.Clear();
            ddlUserGroud.Items.Add(new ListItem("所有会员组", ""));

            // 限制当前管理员对会员的查询
            var canAccessGroups = context.li_user_group_access_keys.Where(k => k.owner_manager == GetAdminInfo().id).Select(k => k.user_group).ToArray();

            ddlUserGroud.Items.AddRange(context.dt_user_groups.Where(g => g.is_lock == 0 && (!canAccessGroups.Any() || canAccessGroups.Contains(g.id)))
                .OrderByDescending(g => g.id)
                .Select(g => new ListItem(g.title, g.id.ToString()))
                .ToArray());
        }
        #endregion

        #region 数据绑定=================================
        private void RptBind()
        {
            var query = QueryWithdraws();

            if (rblTableType.SelectedValue == "0")
            {
                totalCount = query.Count();
                var bankTransactions = query.OrderBy(q => q.status)
                    .ThenByDescending(q => q.transact_time)
                    .ThenByDescending(q => q.create_time)
                    .Skip(pageSize*(page - 1))
                    .Take(pageSize)
                    .ToList();
                rptList.DataSource = bankTransactions;
                rptList.DataBind();

                //绑定页码
                txtPageNum.Text = pageSize.ToString();
                string pageUrl = Utils.CombUrlTxt("bank_transaction_withdrawing_list.aspx", "page={0}&status={1}&keywords={2}&UserGroud={3}&startTime={4}&endTime={5}", "__id__", rblBankTransactionStatus.SelectedValue, txtKeywords.Text.Trim(), UserGroud.ToString(), txtStartTime.Text, txtEndTime.Text);
                PageContent.InnerHtml = Utils.OutPageList(pageSize, page, totalCount, pageUrl, 8);
            }
            else
            {
                rptList_summary.DataSource = QueryGroupData(query);
                rptList_summary.DataBind();
            }
        }

        private List<GroupByUserGroupSummery> QueryGroupData(IQueryable<li_bank_transactions> query)
        {
            var groupByUserGroupSummeries = query.GroupBy(tr => tr.li_bank_accounts.dt_users.dt_user_groups).AsEnumerable()
                .Zip(Utils.Infinite(1), (gr, index) => new { gr, index })
                .Select(gi =>
                {
                    return new GroupByUserGroupSummery
                    {
                        Index = gi.index,
                        GroupName = gi.gr.Key.title,
                        WithdrawAmount = gi.gr.Aggregate(0m, (sum, tr) => sum + tr.value),
                        TransactionAmount = gi.gr.Aggregate(0m, (sum, tr) => sum + tr.value - GetHandlingFee(tr))
                    };
                }).ToList();
            groupByUserGroupSummeries.Add(new GroupByUserGroupSummery()
            {
                Index = null,
                GroupName = "总计",
                WithdrawAmount = groupByUserGroupSummeries.Aggregate(0m, (sum, tr) => sum + tr.WithdrawAmount),
                TransactionAmount = groupByUserGroupSummeries.Aggregate(0m, (sum, tr) => sum + tr.TransactionAmount)
            });
            return groupByUserGroupSummeries;
        }

       private IQueryable<li_bank_transactions> QueryWithdraws()
        {
            var loadOptions = new DataLoadOptions();
            loadOptions.LoadWith<li_bank_transactions>(tr => tr.li_bank_accounts);
            loadOptions.LoadWith<li_bank_accounts>(tr => tr.dt_users);
            loadOptions.LoadWith<li_bank_transactions>(tr => tr.li_wallet_histories);
            context.LoadOptions = loadOptions;

            var query = context.li_bank_transactions.Where(b => b.type == (int) (Agp2pEnums.BankTransactionTypeEnum.Withdraw));

            //用户分组查询
            if (0 < UserGroud) // 选择了某一组
            {
                ddlUserGroud.SelectedValue = UserGroud.ToString();
                query = query.Where(b => b.li_bank_accounts.dt_users.group_id == UserGroud);
            }
            else
            {
                // 限制当前管理员对会员的查询
                var canAccessGroups = context.li_user_group_access_keys.Where(k => k.owner_manager == GetAdminInfo().id).Select(k => k.user_group).ToArray();
                query = query.Where(u => !canAccessGroups.Any() || canAccessGroups.Contains(u.li_bank_accounts.dt_users.group_id));
            }

            if (!string.IsNullOrWhiteSpace(txtKeywords.Text))
            {
                query = query.Where(b => b.li_bank_accounts.dt_users.real_name.Contains(txtKeywords.Text) || b.li_bank_accounts.dt_users.user_name.Contains(txtKeywords.Text));
            }

            if (!string.IsNullOrWhiteSpace(txtStartTime.Text))
                query = query.Where(h => Convert.ToDateTime(txtStartTime.Text) <= h.create_time.Date);
            if (!string.IsNullOrWhiteSpace(txtEndTime.Text))
                query = query.Where(h => h.create_time.Date <= Convert.ToDateTime(txtEndTime.Text));
            if (rblBankTransactionStatus.SelectedValue != "0")
                query = query.Where(b => b.status == Convert.ToInt32(rblBankTransactionStatus.SelectedValue));

            if (!ChkAdminLevelReturn("manage_bank_transaction_withdraw", DTEnums.ActionEnum.BigOrderAudit.ToString()))
                query = query.Where(b => b.value <= 5000); // 没有大额审批权限，只能看到 5000 以下的提现

            return query;
        }

        #endregion

        //设置分页数量
        protected void txtPageNum_TextChanged(object sender, EventArgs e)
        {
            SetPageSize(GetType().Name + "_page_size", txtPageNum.Text.Trim());
            Response.Redirect(Utils.CombUrlTxt("bank_transaction_withdrawing_list.aspx", "status={0}&page={1}&keywords={2}&UserGroud={3}&startTime={4}&endTime={5}", rblBankTransactionStatus.SelectedValue, page.ToString(), txtKeywords.Text.Trim(), UserGroud.ToString(), txtStartTime.Text, txtEndTime.Text));
        }

        //关健字查询
        protected void btnSearch_Click(object sender, EventArgs e)
        {
            Response.Redirect(Utils.CombUrlTxt("bank_transaction_withdrawing_list.aspx", "status={0}&page={1}&keywords={2}&UserGroud={3}&startTime={4}&endTime={5}", rblBankTransactionStatus.SelectedValue, page.ToString(), txtKeywords.Text.Trim(), UserGroud.ToString(), txtStartTime.Text, txtEndTime.Text));
        }

        //筛选类别
        protected void ddlUserGroud_SelectedIndexChanged(object sender, EventArgs e)
        {
            Response.Redirect(Utils.CombUrlTxt("bank_transaction_withdrawing_list.aspx", "status={0}&page={1}&keywords={2}&UserGroud={3}&startTime={4}&endTime={5}", rblBankTransactionStatus.SelectedValue, page.ToString(), txtKeywords.Text.Trim(), ddlUserGroud.SelectedValue, txtStartTime.Text, txtEndTime.Text));
        }

        //批量确认/取消
        protected void btnConfirmCancel_Click(object sender, EventArgs e)
        {
            try
            {
                var doConfirm = ((LinkButton)sender).ID == "btnConfirm";
                ChkAdminLevel("manage_bank_transaction_withdraw", (doConfirm ? DTEnums.ActionEnum.Confirm : DTEnums.ActionEnum.Cancel).ToString());
                var preSaveTransaction = new List<li_bank_transactions>();
                for (int i = 0; i < rptList.Items.Count; i++)
                {
                    CheckBox cb = (CheckBox)rptList.Items[i].FindControl("chkId");
                    if (!cb.Checked) continue;
                    int id = Convert.ToInt32(((HiddenField)rptList.Items[i].FindControl("hidId")).Value);
                    if (doConfirm)
                    {
                        preSaveTransaction.Add(context.ConfirmBankTransaction(id, GetAdminInfo().id, false));
                    }
                    else
                    {
                        preSaveTransaction.Add(context.CancelBankTransaction(id, GetAdminInfo().id, false));
                    }
                }
                context.SubmitChanges();
                preSaveTransaction.ForEach(t => MessageBus.Main.Publish(new BankTransactionFinishedMsg(t)));

                AddAdminLog(DTEnums.ActionEnum.Delete.ToString(), "审批成功 " + preSaveTransaction.Count + " 条，失败 0 条"); //记录日志
                JscriptMsg("审批成功" + preSaveTransaction.Count + "条，失败 0 条！",
                    Utils.CombUrlTxt("bank_transaction_withdrawing_list.aspx", "status={0}&page={1}", rblBankTransactionStatus.SelectedValue, page.ToString()), "Success");
            }
            catch (Exception)
            {
                JscriptMsg("审批失败！", Utils.CombUrlTxt("bank_transaction_withdrawing_list.aspx", "status={0}&page={1}", rblBankTransactionStatus.SelectedValue, page.ToString()), "Failure");
            }
        }

        protected void rblBankTransactionStatus_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            RptBind();
        }

        protected decimal GetHandlingFee(li_bank_transactions tr)
        {
            return tr.handling_fee_type == (int)Agp2pEnums.BankTransactionHandlingFeeTypeEnum.NoHandlingFee
                ? 0
                : Math.Max(TransactionFacade.DefaultHandlingFee, tr.handling_fee);
        }

        protected string QueryIdleMoney(li_bank_transactions liBankTransactions)
        {
            return liBankTransactions.li_wallet_histories.Last().idle_money.ToString("c");
        }

        protected void btnExportExcel_Click(object sender, EventArgs e)
        {
            ChkAdminLevel("manage_bank_transaction_withdraw", DTEnums.ActionEnum.DownLoad.ToString()); //检查权限
            var withdraws = QueryWithdraws();
            if (rblTableType.SelectedValue == "0")
            {
                var lsData = withdraws.OrderBy(q => q.status).ThenByDescending(q => q.transact_time)
                    .ThenByDescending(q => q.create_time)
                    .Skip(pageSize*(page - 1))
                    .Take(pageSize)
                    .AsEnumerable()
                    .Select(bt => new
                    {
                        user = bt.li_bank_accounts.dt_users.user_name,
                        bt.value,
                        finalVal = bt.value - GetHandlingFee(bt),
                        bt.create_time,
                        bt.transact_time,
                        idleMoney = QueryIdleMoney(bt),
                        bt.li_bank_accounts.bank,
                        name = bt.li_bank_accounts.dt_users.real_name,
                        bt.li_bank_accounts.opening_bank,
                        bt.li_bank_accounts.account,
                        status = Utils.GetAgp2pEnumDes((Agp2pEnums.BankTransactionStatusEnum) bt.status),
                        handlingFee = GetHandlingFee(bt)
                    });
                var titles = new[]
                {"申请人", "提现金额", "实付金额", "申请时间", "付款时间", "操作后余额", "银行名称", "银行开户名", "银行开户行", "银行帐号", "提现状态", "手续费"};
                Utils.ExportXls("提现明细", titles, lsData, Response);
            }
            else
            {
                var lsData = QueryGroupData(withdraws).Select(q => new
                {
                    q.Index,
                    q.GroupName,
                    q.WithdrawAmount,
                    q.TransactionAmount
                });
                var titles = new[] {"序号", "会员组", "提现金额", "实付金额"};
                Utils.ExportXls("提现汇总", titles, lsData, Response);
            }
        }

        protected void rptList_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.AlternatingItem || e.Item.ItemType == ListItemType.Item)
            {
                li_bank_transactions bt = (li_bank_transactions) e.Item.DataItem;
                value = value + bt.value;
                value1 = value1 + ((bt.value) - GetHandlingFee(bt));
            }
        }

        protected void rblTableType_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (rblTableType.SelectedValue == "0")
            {
                rptList.Visible = true;
                rptList_summary.Visible = false;
                div_pagination.Visible = true;
            }
            else
            {
                rptList.Visible = false;
                rptList_summary.Visible = true;
                div_pagination.Visible = false;
            }
            RptBind();
        }
    }
}