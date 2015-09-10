﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Lip2p.BLL;
using Lip2p.Common;
using Lip2p.Core;
using Lip2p.Linq2SQL;

namespace Lip2p.Web.admin.transact
{
    public partial class project_repayment_list : UI.ManagePage
    {
        protected int totalCount;
        protected int page;
        protected int pageSize;

        protected string project_id;
        private Lip2pDataContext context = new Lip2pDataContext();

        protected void Page_Load(object sender, EventArgs e)
        {
            project_id = DTRequest.GetQueryString("project_id");

            pageSize = GetPageSize(10); //每页数量
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
            int projectId = Convert.ToInt32(project_id);
            var query = context.li_repayment_tasks.Where(b => b.project == projectId);

            totalCount = query.Count();
            rptList.DataSource = query.Skip(pageSize * (page - 1)).Take(pageSize).ToList();
            rptList.DataBind();

            //绑定页码
            txtPageNum.Text = pageSize.ToString();
            string pageUrl = Utils.CombUrlTxt("project_repayment_list.aspx", "page={0}&project_id={1}", "__id__", project_id);
            PageContent.InnerHtml = Utils.OutPageList(pageSize, page, totalCount, pageUrl, 8);
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
            Response.Redirect(Utils.CombUrlTxt("project_repayment_list.aspx", "project_id={0}", project_id));
        }

        //批量删除
        protected void btnDelete_Click(object sender, EventArgs e)
        {
            ChkAdminLevel("manage_project_transaction", DTEnums.ActionEnum.Delete.ToString()); //检查权限
            int sucCount = 0;
            int errorCount = 0;
            for (int i = 0; i < rptList.Items.Count; i++)
            {
                int id = Convert.ToInt32(((HiddenField)rptList.Items[i].FindControl("hidId")).Value);
                CheckBox cb = (CheckBox)rptList.Items[i].FindControl("chkId");
                if (cb.Checked)
                {
                    var preDel = context.li_repayment_tasks.FirstOrDefault(q => q.id == id);
                    if (preDel != null)
                    {
                        sucCount += 1;
                        context.li_repayment_tasks.DeleteOnSubmit(preDel);
                    }
                    else errorCount += 1;
                }
            }
            try
            {
                context.SubmitChanges();
                AddAdminLog(DTEnums.ActionEnum.Delete.ToString(), "删除还款计划 " + sucCount + " 条，失败 " + errorCount + " 条"); //记录日志
                JscriptMsg("删除还款计划成功" + sucCount + "条，失败" + errorCount + "条！", Utils.CombUrlTxt("project_repayment_list.aspx", "project_id={0}", project_id), "Success");
            }
            catch (Exception)
            {
                JscriptMsg("删除失败！", Utils.CombUrlTxt("project_repayment_list.aspx", "project_id={0}", project_id), "Failure");
            }
        }

        protected string GetRepaymentTaskType(li_repayment_tasks repayment)
        {
            var trType = repayment.repay_interest != 0 && repayment.repay_principal != 0
                ? Lip2pEnums.ProjectTransactionTypeEnum.RepaidPrincipalAndInterest
                : (repayment.repay_interest != 0
                    ? Lip2pEnums.ProjectTransactionTypeEnum.RepaidInterest
                    : Lip2pEnums.ProjectTransactionTypeEnum.RepaidPrincipal);
            return Utils.GetLip2pEnumDes(trType);
        }

        protected void btnRepayNow_OnClick(object sender, EventArgs e)
        {
            int repaymentId = Convert.ToInt32(((Button)sender).CommandArgument);
            try
            {
                var repayment = context.ExecuteRepaymentTask(repaymentId, false);
                var remark = "执行放款计划成功, 利息: " + repayment.repay_interest + " 返还本金: " + repayment.repay_principal;
                AddAdminLog(DTEnums.ActionEnum.Edit.ToString(), remark); //记录日志
                JscriptMsg(remark, Utils.CombUrlTxt("project_repayment_list.aspx", "project_id={0}", project_id), "Success");
            }
            catch (Exception ex)
            {
                JscriptMsg("执行放款计划失败！" + ex.Message, Utils.CombUrlTxt("project_repayment_list.aspx", "project_id={0}", project_id), "Failure");
            }
        }
    }
}