﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="investor_invest_detail.aspx.cs" Inherits="Agp2p.Web.admin.statistic.investor_invest_detail" %>
<%@ Import namespace="Agp2p.Common" %>
<%@ Import Namespace="Agp2p.Linq2SQL" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>投资明细</title>
<script type="text/javascript" src="../../scripts/jquery/jquery-1.10.2.min.js"></script>
<script type="text/javascript" src="../../scripts/tablesorter/jquery.tablesorter.min.js"></script>
<script type="text/javascript" src="../../scripts/lhgdialog/lhgdialog.js?skin=idialog"></script>
<script type="text/javascript" src="../js/layout.js"></script>
<script type="text/javascript" src="../../scripts/datepicker/WdatePicker.js"></script>
<link href="../skin/default/style.css" rel="stylesheet" type="text/css" />
<link  href="../../css/pagination.css" rel="stylesheet" type="text/css" />
<link href="../../scripts/tablesorter/table-sorter-style.css" rel="stylesheet" type="text/css" />
<style>
tr.sum td { color: red; }
td { text-align: center; }
</style>
    <script>
        $(function () {
            $.tablesorter.addParser({
                id: 'rmb',
                is: function () { return false; },
                format: function (s) { return s.replace(/[¥,]/g, ''); },
                type: 'numeric'
            });
            $("table#invest").tablesorter({
                headers: {
                    2: { sorter: 'rmb' },
                    3: { sorter: 'rmb' },
                    4: { sorter: 'rmb' },
                    5: { sorter: 'rmb' },
                    6: { sorter: 'rmb' },
                    7: { sorter: 'rmb' },
                    8: { sorter: 'rmb' },
                    9: { sorter: 'rmb' }
                }
            });
        });
</script>
</head>

<body class="mainbody">
<form id="form1" runat="server">
<!--导航栏-->
<div class="location">
  <a href="javascript:history.back(-1);" class="back"><i></i><span>返回上一页</span></a>
  <a href="../center.aspx" class="home"><i></i><span>首页</span></a>
  <i class="arrow"></i>
  <span>投资明细</span>
</div>
<!--/导航栏-->

    <!--工具栏-->
    <div class="toolbar-wrap">
        <div id="floatHead" class="toolbar">
            <div class="l-list">
                <ul class="icon-list">
                    <li><asp:LinkButton ID="btnExportExcel" runat="server" CssClass="quotes" OnClick="btnExportExcel_Click"><i></i><span>导出 Excel</span></asp:LinkButton></li>
                </ul>
            </div>
            <div class="r-list">
                <div class="rule-multi-radio" style="display: inline-block; margin-right: 10px; float: left;">
                        <asp:RadioButtonList ID="rblType" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" AutoPostBack="True" OnSelectedIndexChanged="rblType_OnSelectedIndexChanged">
                            <asp:ListItem Value="0" Selected="True">明细</asp:ListItem>
                            <asp:ListItem Value="1">汇总</asp:ListItem>
                        </asp:RadioButtonList>
                    </div>
                <div style="display: inline-block;" class="rl">按投资时间查询：</div>
                <div class="input-date" style="display: inline-block; float: left;">
                        <asp:TextBox ID="txtStartTime" runat="server" CssClass="input date" onfocus="WdatePicker({dateFmt:'yyyy-MM-dd'})"
                            datatype="/^\s*$|^\d{4}\-\d{1,2}\-\d{1,2}\s{1}(\d{1,2}:){2}\d{1,2}$/" errormsg="请选择正确的日期"
                            sucmsg=" " Style="font-size: 15px" />
                        <i></i>
                    </div>
                    <span class="rl">到</span>
                    <div class="input-date" style="display: inline-block; float: left; margin-right: 10px;">
                        <asp:TextBox ID="txtEndTime" runat="server" CssClass="input date" onfocus="WdatePicker({dateFmt:'yyyy-MM-dd'})"
                            datatype="/^\s*$|^\d{4}\-\d{1,2}\-\d{1,2}\s{1}(\d{1,2}:){2}\d{1,2}$/" errormsg="请选择正确的日期"
                            sucmsg=" " Style="font-size: 15px" />
                        <i></i>
                    </div>
                <div style="display: inline-block;">
                    <asp:TextBox ID="txtKeywords" runat="server" CssClass="keyword" onkeydown="return Enter(event);" ontextchanged="txtPageNum_TextChanged" AutoPostBack="True" />
                    <asp:LinkButton ID="lbtnSearch" runat="server" CssClass="btn-search" OnClick="btnSearch_Click">查询</asp:LinkButton>
                </div>
            </div>
        </div>
    </div>
    <!--/工具栏-->

<!--列表-->
<asp:Repeater ID="rptList" runat="server">
<HeaderTemplate>
<table id="invest" width="100%" border="0" cellspacing="0" cellpadding="0" class="ltable">
    <thead>
        <tr>
            <th width="4%">序号</th>
            <th>投资者</th>
            <th align="left">标题</th>
            <th>产品</th>
            <th>投资时间</th>
            <th>到期时间</th>
            <th>期限</th>
            <th>年利率</th>
            <th>投资本金</th>
            <th>利息</th>
            <th>返还本息合计</th>
        </tr>
    </thead>
</HeaderTemplate>
<ItemTemplate>
  <tr <%# string.Equals(Eval("InvestorUserName"), "小计") || string.Equals(Eval("InvestorUserName"), "总计") ? "class='sum'" : ""%>>
    <td style="text-align: center;"><%# Eval("Index")%></td>
    <td><%# string.IsNullOrWhiteSpace((string) Eval("InvestorRealName")) ? Eval("InvestorUserName") : Eval("InvestorRealName") %></td>
    <td style="text-align: left; padding-left:5px;"><%# Eval("ProjectName")%></td>
     <td align="center"><%#Eval("Category")%></td>
    <td><%# Eval("InvestTime")%></td>
    <td><%# Eval("ProjectCompleteTime")%></td>
    <td><%# Eval("Term")%></td>
    <td><%# Eval("ProfitRateYear")%></td>
    <td class="money"><%# Eval("InvestValue") == null ? "" : Convert.ToDecimal(Eval("InvestValue")).ToString("c")%></td>
    <td class="money"><%# Eval("RepayTotal") == null ? "" : Convert.ToDecimal(Eval("RepayTotal")).ToString("c")%></td>
    <td class="money"><%# Convert.ToDecimal(Eval("Total")).ToString("c") %></td>
  </tr>
</ItemTemplate>
<FooterTemplate>
  <%#rptList.Items.Count == 0 ? "<tr><td align=\"center\" colspan=\"10\">暂无记录</td></tr>" : ""%>
</table>
</FooterTemplate>
</asp:Repeater>
<!--/列表-->
    <!--汇总列表-->
        <asp:Repeater ID="rptList_summary" runat="server" Visible="False">
            <HeaderTemplate>
                <table id="wallet" width="100%" border="0" cellspacing="0" cellpadding="0" class="ltable">
                    <thead>
                        <tr>
                            <th align="center" width="5%" style="padding-left: 1em;">序号</th>
                            <th align="center">投资者</th>
                            <th align="center">投资本金</th>
                            <th align="center">利息</th>
                            <th align="center">本息合计</th>
                        </tr>
                    </thead>
            </HeaderTemplate>
            <ItemTemplate>
                <tr <%# ((InvestorInvestDetail)Container.DataItem).Index == null ? "class='sum'" : ""%>>
                    <td style="text-align: center;"><%# Eval("Index") %></td>
                    <td style="text-align: center"><%# Eval("InvestorUserName")%></td>
                    <td style="text-align: center"><%# Convert.ToDecimal(Eval("InvestValue")).ToString("c")%></td>
                    <td style="text-align: center"><%# Convert.ToDecimal(Eval("RepayTotal")).ToString("c")%></td>
                    <td style="text-align: center"><%# Convert.ToDecimal(Eval("Total")).ToString("c")%></td>
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                <%#rptList_summary.Items.Count == 0 ? "<tr><td align=\"center\" colspan=\"7\">暂无记录</td></tr>" : ""%>
</table>
            </FooterTemplate>
        </asp:Repeater>
        <!--/汇总列表-->

<!--内容底部-->
<div class="line20"></div>
<div class="pagelist" id="div_page" runat="server">
  <div class="l-btns">
      <span>显示</span><asp:TextBox ID="txtPageNum" runat="server" CssClass="pagenum" onkeydown="return checkNumber(event);" ontextchanged="txtPageNum_TextChanged" AutoPostBack="True"/><span>条/页</span>
  </div>
  <div id="PageContent" runat="server" class="default"></div>
</div>
<!--/内容底部-->
</form>
</body>
</html>
