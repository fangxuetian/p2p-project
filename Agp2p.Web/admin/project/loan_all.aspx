﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="loan_all.aspx.cs" Inherits="Agp2p.Web.admin.project.loan_all" %>

<%@ Import Namespace="Agp2p.Common" %>
<%@ Import Namespace="Agp2p.Linq2SQL" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>所有借款</title>
    <script type="text/javascript" src="../../scripts/jquery/jquery-1.10.2.min.js"></script>
    <script type="text/javascript" src="../../scripts/jquery/jquery.lazyload.min.js"></script>
    <script type="text/javascript" src="../../scripts/lhgdialog/lhgdialog.js?skin=idialog"></script>
    <script type="text/javascript" src="../../scripts/datepicker/WdatePicker.js"></script>
    <script type="text/javascript" src="../js/layout.js"></script>
    <link href="../skin/default/style.css" rel="stylesheet" type="text/css" />
    <link href="../../css/pagination.css" rel="stylesheet" type="text/css" />
</head>
<body class="mainbody">
    <form id="form1" runat="server">
        <!--导航栏-->
        <div class="location">
            <a href="javascript:history.back(-1);" class="back"><i></i><span>返回上一页</span></a>
            <a href="../center.aspx" class="home"><i></i><span>首页</span></a>
            <i class="arrow"></i>
            <span>所有借款</span>
        </div>
        <!--/导航栏-->
        <!--工具栏-->
        <div class="toolbar-wrap">
            <div id="floatHead" class="toolbar">
                <div class="r-list">
                    <div style="display: inline-block;" class="rl">时间段：</div>
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
                    <div class="menu-list rl" style="display: inline-block;">
                        <div class="rule-single-select">
                            <asp:DropDownList ID="ddlCategoryId" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlCategoryId_OnSelectedIndexChanged"></asp:DropDownList>
                        </div>
                        <div class="rule-single-select">
                            <asp:DropDownList ID="ddlStatus" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlStatus_OnSelectedIndexChanged"></asp:DropDownList>
                        </div>
                    </div>
                    <asp:TextBox ID="txtKeywords" runat="server" CssClass="keyword" onkeydown="return Enter(event);"/>
                    <asp:LinkButton ID="lbtnSearch" runat="server" CssClass="btn-search" OnClick="btnSearch_Click">查询</asp:LinkButton>
                </div>
            </div>
        </div>
        <!--/工具栏-->

        <asp:Repeater ID="rptList1" runat="server">
            <HeaderTemplate>
                <table width="100%" border="0" cellspacing="0" cellpadding="0" class="ltable">
                    <tr>
                        <th width="4%">序号</th>
                        <th align="left" width="13%">标题</th>
                        <th align="left" width="5%">状态</th>    
                        <th align="left" width="12%">借款人</th>
                        <th align="left" width="10%">借款编号</th>
                        <th align="left" width="8%">产品</th>
                        <th align="left" width="5%">标识</th>
                        <th align="left" width="5%">浏览次数</th>
                        <th align="left" width="10%">借款金额(元)</th>                        
                        <th align="left" width="5%">借款期限</th>
                        <th align="left" width="5%">年化利率(%)</th>
                        <th align="left" width="5%">还款方式</th>     
                        <th align="left" width="8%">申请时间</th>
                        <th align="left" width="6%">操作</th>
                    </tr>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td align="center"><%# Container.ItemIndex + PageSize * (PageIndex - 1) + 1 %></td>
                    <td><a href="loan_detail.aspx?channel_id=<%=this.ChannelId %>&action=<%=DTEnums.ActionEnum.Edit%>&id=<%#Eval("id")%>"><%#Eval("title")%></a></td>
                    <td><%#Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectStatusEnum)Utils.StrToInt(Eval("status").ToString(), 0))%></td>             
                    <td><%#QueryLoaner(((li_projects) Container.DataItem).id)%></td>
                    <td><%#Eval("no")%></td>
                    <td><%#Eval("dt_article_category.title")%></td>
                    <td><%#GetTagString(Eval("tag"))%></td>
                    <td><%#Eval("click")%></td>
                    <td><%#string.Format("{0:c}", Eval("financing_amount"))%></td>                    
                    <td><%#Eval("repayment_term_span_count")%> <%#Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectRepaymentTermSpanEnum)Utils.StrToInt(Eval("repayment_term_span").ToString(), 0))%></td>
                    <td><%#string.Format("{0:0.00}", Eval("profit_rate_year"))%></td>
                    <td><%#Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectRepaymentTypeEnum)Utils.StrToInt(Eval("repayment_type").ToString(), 0))%></td>           
                    <td><%#string.Format("{0:g}",Eval("add_time"))%></td>         
                    <td>
                        <a href="loan_apply_detail.aspx?channel_id=<%=this.ChannelId %>&action=<%=DTEnums.ActionEnum.Copy%>&id=<%#Eval("id")%>">复制</a>
                    </td>            
                </tr>
            </ItemTemplate>
            <FooterTemplate>
                <%#rptList1.Items.Count == 0 ? "<tr><td align=\"center\" colspan=\"11\">暂无记录</td></tr>" : ""%>
                </table>
            </FooterTemplate>
        </asp:Repeater>

        <!--内容底部-->
        <div class="line20"></div>
        <div class="pagelist">
            <div class="l-btns">
                <span>显示</span><asp:TextBox ID="txtPageNum" runat="server" CssClass="pagenum" onkeydown="return checkNumber(event);" OnTextChanged="txtPageNum_TextChanged" AutoPostBack="True"></asp:TextBox><span>条/页</span>
            </div>
            <div id="PageContent" runat="server" class="default"></div>
        </div>
        <!--/内容底部-->
    </form>
</body>
</html>
