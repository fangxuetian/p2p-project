﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="bad_debt_summary.aspx.cs" Inherits="Agp2p.Web.admin.statistic.bad_debt_summary" %>
<%@ Import namespace="Agp2p.Common" %>
<%@ Import Namespace="Agp2p.Linq2SQL" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>坏账汇总</title>
<script type="text/javascript" src="../../scripts/jquery/jquery-1.10.2.min.js"></script>
<script type="text/javascript" src="../../scripts/lhgdialog/lhgdialog.js?skin=idialog"></script>
<script type="text/javascript" src="../js/layout.js"></script>
<link href="../skin/default/style.css" rel="stylesheet" type="text/css" />
<link  href="../../css/pagination.css" rel="stylesheet" type="text/css" />
<style>
td.money { text-align: right; }
td.center { text-align: center; }
</style>
</head>

<body class="mainbody">
<form id="form1" runat="server">
<!--导航栏-->
<div class="location">
  <a href="javascript:history.back(-1);" class="back"><i></i><span>返回上一页</span></a>
  <a href="../center.aspx" class="home"><i></i><span>首页</span></a>
  <i class="arrow"></i>
  <span>坏账汇总</span>
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
<table width="100%" border="0" cellspacing="0" cellpadding="0" class="ltable">
  <tr>
    <th align="center" width="4%">序号</th>
    <th align="left" width="7%">时间</th>
    <th align="left" width="6%">坏账总数</th>
    <th align="left" width="8%">坏账总金额</th>
    <th align="left" width="6%">未还坏账数</th>
    <th align="left" width="8%">未还坏账金额</th>
    <th align="left" width="6%">垫付坏账数</th>
    <th align="left" width="8%">垫付坏账金额</th>
    <th align="left" width="6%">坏账罚金</th>
    <th align="left" width="6%">坏账率</th>
  </tr>
</HeaderTemplate>
<ItemTemplate>
  <tr>
      <td align="center"><%# Eval("Index")%></td>
      <td><%# Eval("YearMonth")%></td>
      <td><%# Eval("TotalCount")%></td>
      <td><%# Eval("TotalAmount")%></td>
      <td><%# Eval("NotRepayCount")%></td>
      <td><%# Eval("NotRepayAmount")%></td>
      <td><%# Eval("PepayCount")%></td>
      <td><%# Eval("PepayAmount")%></td>
      <td><%# Eval("Cost")%></td>
      <td><%# Eval("Rate")%></td>
  </tr>
</ItemTemplate>
<FooterTemplate>
  <%#rptList.Items.Count == 0 ? "<tr><td align=\"center\" colspan=\"14\">暂无记录</td></tr>" : ""%>
</table>
</FooterTemplate>
</asp:Repeater>
<!--/列表-->

<!--内容底部-->
<div class="line20"></div>
<div class="pagelist">
  <div class="l-btns">
    <span>显示</span><asp:TextBox ID="txtPageNum" runat="server" CssClass="pagenum" onkeydown="return checkNumber(event);" ontextchanged="txtPageNum_TextChanged" AutoPostBack="True"></asp:TextBox><span>条/页</span>
  </div>
  <div id="PageContent" runat="server" class="default"></div>
</div>
<!--/内容底部-->
</form>
</body>
</html>
