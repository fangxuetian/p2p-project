﻿using System;
using System.Collections;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Agp2p.BLL;
using Agp2p.Common;
using Agp2p.Core;
using Agp2p.Linq2SQL;

namespace Agp2p.Web.admin.transact
{
    public partial class bank_transaction_withdraw : UI.ManagePage
    {
        protected int id = 0, accountId, userId;
        private Agp2pDataContext context = new Agp2pDataContext();

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!int.TryParse(Request.QueryString["account_id"], out accountId))
            {
                JscriptMsg("传输参数不正确！", "back", "Error");
                return;
            }
            var account = context.li_bank_accounts.First(b => b.id == accountId);
            userId = account.owner;
            if (!Page.IsPostBack)
            {
                ChkAdminLevel("manage_users_charge_withdraw", DTEnums.ActionEnum.View.ToString()); //检查权限
                lblUser.Text = account.dt_users.user_name;
                lblBank.Text = account.bank;
                lblAccount.Text = account.account;
                lblIdleMoney.Text = account.dt_users.li_wallets.idle_money.ToString("c");
            }
        }

        #region 增加操作=================================
        private bool DoAdd()
        {
            try
            {
                context.Withdraw(accountId, Math.Abs(Convert.ToDecimal(txtValue.Text)), string.IsNullOrWhiteSpace(txtRemark.Text) ? null : txtRemark.Text.Trim());
                AddAdminLog(DTEnums.ActionEnum.Add.ToString(), string.Format("{0} 添加 {1} 账户 {2} 提款记录: {3} 备注: {4}", lblUser.Text, lblBank.Text, lblAccount.Text, txtValue.Text, txtRemark.Text)); //记录日志
                return true;
            }
            catch (Exception ex)
            {
                JscriptMsg(ex.Message, "", "Error");
                return false;
            }
        }
        #endregion

        //保存
        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            ChkAdminLevel("manage_users_charge_withdraw", DTEnums.ActionEnum.Add.ToString()); //检查权限
            if (!DoAdd())
            {
                JscriptMsg("保存过程中发生错误！", "", "Error");
                return;
            }
            JscriptMsg("添加银行账户交易记录成功！", Utils.CombUrlTxt("bank_account_list.aspx", "user_id={0}", userId.ToString()), "Success");
        }

        protected string GetUserName()
        {
            return context.dt_users.First(u => u.id == Convert.ToInt32(userId)).user_name;
        }
    }
}