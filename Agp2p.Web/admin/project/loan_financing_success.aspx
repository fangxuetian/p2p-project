﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="loan_financing_success.aspx.cs" Inherits="Agp2p.Web.admin.project.loan_financing_success" %>

<%@ Import Namespace="Agp2p.Common" %>
<%@ Import Namespace="Agp2p.Linq2SQL" %>
<%@ Import Namespace="Agp2p.Web.UI" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>满标借款</title>
    <script type="text/javascript" src="../../scripts/jquery/jquery-1.10.2.min.js"></script>
    <script type="text/javascript" src="../../scripts/jquery/jquery.lazyload.min.js"></script>
    <script type="text/javascript" src="../../scripts/lhgdialog/lhgdialog.js?skin=idialog"></script>
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
            <span>满标借款</span>
        </div>
        <!--/导航栏-->
        <!--工具栏-->
        <div class="toolbar-wrap">
            <div id="floatHead" class="toolbar">
                <div class="l-list">
                    <div class="rule-multi-radio" style="display: inline-block; float: left; margin-right: 10px;">
                        <asp:RadioButtonList ID="rblStatus" runat="server" RepeatDirection="Horizontal" RepeatLayout="Flow" AutoPostBack="True"
                            OnSelectedIndexChanged="rblStatus_OnSelectedIndexChanged">
                        </asp:RadioButtonList>
                    </div>
                </div>
                <div class="r-list">
                    <div class="menu-list rl" style="display: inline-block;">
                        <div class="rule-single-select">
                            <asp:DropDownList ID="ddlCategoryId" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlCategoryId_SelectedIndexChanged"></asp:DropDownList>
                        </div>
                    </div>
                    <asp:TextBox ID="txtKeywords" runat="server" CssClass="keyword" onkeydown="return Enter(event);" OnTextChanged="txtPageNum_TextChanged" AutoPostBack="True" />
                    <asp:LinkButton ID="lbtnSearch" runat="server" CssClass="btn-search">查询</asp:LinkButton>
                </div>
            </div>
        </div>
        <!--/工具栏-->

        <asp:Repeater ID="rptList1" runat="server">
            <HeaderTemplate>
                <table width="100%" border="0" cellspacing="0" cellpadding="0" class="ltable">
                    <tr>
                        <th width="3%">序号</th>
                        <th align="left" width="15%">标题</th>
                        <th align="left" width="13%">借款人</th>
                        <% if (ProjectStatus == (int)Agp2pEnums.ProjectStatusEnum.FinancingSuccess)
                            { %>
                        <th align="left" width="8%">满标时间</th>
                        <% }
                            else if (ProjectStatus == (int)Agp2pEnums.ProjectStatusEnum.ProjectRepaying)
                            { %>
                        <th align="left" width="8%">放款时间</th>
                        <% }
                            else if (ProjectStatus >= (int)Agp2pEnums.ProjectStatusEnum.ProjectRepaying)
                            { %>
                        <th align="left" width="8%">完成时间</th>
                        <% } %>
                        <th align="left" width="8%">发布时间</th>
                        <th align="left" width="8%">产品</th>
                        <th align="left" width="10%">借款金额(元)</th>
                        <th align="left" width="8%">借款期限</th>
                        <th align="left" width="8%">年化利率(%)</th>
                        <th align="left" width="8%">还款方式</th>
                        <th align="left" width="5%">状态</th>
                    </tr>
            </HeaderTemplate>
            <ItemTemplate>
                <tr>
                    <td align="center"><%# Container.ItemIndex + PageSize * (PageIndex - 1) + 1 %></td>
                    <td><a href="loan_detail.aspx?channel_id=<%=this.ChannelId %>&id=<%#Eval("id")%>&status=<%#Eval("status")%>"><%#Eval("title")%></a></td>
                    <td><%#QueryLoaner(((li_projects) Container.DataItem).id)%></td>
                    <% if (ProjectStatus == (int)Agp2pEnums.ProjectStatusEnum.FinancingSuccess)
                        { %>
                    <td><%#string.Format("{0:g}",Eval("invest_complete_time"))%></td>
                    <% }
                        else if (ProjectStatus == (int)Agp2pEnums.ProjectStatusEnum.ProjectRepaying)
                        { %>
                    <td><%#string.Format("{0:g}",Eval("make_loan_time"))%></td>
                    <% }
                        else if (ProjectStatus >= (int)Agp2pEnums.ProjectStatusEnum.RepayCompleteIntime)
                        { %>
                    <td><%#string.Format("{0:g}",Eval("complete_time"))%></td>
                    <% } %>
                    <td><%#string.Format("{0:g}",Eval("publish_time"))%></td>
                    <td><%#CategoryIdTitleMap[Convert.ToInt32(Eval("category_id"))]%></td>
                    <td><%#string.Format("{0:c}", Eval("financing_amount"))%></td>
                    <td><%#Eval("repayment_term_span_count")%> <%#Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectRepaymentTermSpanEnum)Utils.StrToInt(Eval("repayment_term_span").ToString(), 0))%></td>
                    <td><%#string.Format("{0:0.00}", Eval("profit_rate_year"))%></td>
                    <td><%#Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectRepaymentTypeEnum)Utils.StrToInt(Eval("repayment_type").ToString(), 0))%></td>
                    <td><%#Utils.GetAgp2pEnumDes((Agp2pEnums.ProjectStatusEnum)Utils.StrToInt(Eval("status").ToString(), 0))%></td>
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
