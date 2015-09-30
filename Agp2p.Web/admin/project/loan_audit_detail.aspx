﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="loan_audit_detail.aspx.cs" Inherits="Agp2p.Web.admin.project.loan_audit_detail" %>

<%@ Import Namespace="Agp2p.Common" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>初审明细</title>
    <script type="text/javascript" src="../../scripts/jquery/jquery-1.10.2.min.js"></script>
    <script type="text/javascript" src="../js/layout.js"></script>
    <script type="text/javascript" src="../../scripts/swfupload/swfupload.handlers.js"></script>
    <script type="text/javascript" src="../../scripts/lhgdialog/lhgdialog.js?skin=idialog"></script>
    <link href="../skin/default/style.css" rel="stylesheet" type="text/css" />
</head>
<body class="mainbody">
    <form id="form1" runat="server">
        <!--导航栏-->
        <div class="location">
            <a href="loan_audit.aspx?channel_id=<%=this.ChannelId %>" class="back"><i></i><span>返回列表页</span></a> <a href="../center.aspx" class="home"><i></i><span>首页</span></a>
            <i class="arrow"></i><a href="loan_audit.aspx?channel_id=<%=this.ChannelId %>">
                <span>借款初审</span></a> <i class="arrow"></i><span>初审明细</span>
        </div>
        <div class="line10">
        </div>
        <!--/导航栏-->
        <!--内容-->
        <div class="content-tab-wrap">
            <div id="floatHead" class="content-tab">
                <div class="content-tab-ul-wrap">
                    <ul>
                        <% %>
                        <li><a href="javascript:;" onclick="tabs(this);" class="selected">借款详细</a></li>
                    </ul>
                </div>
            </div>
        </div>
        <div class="tab-content">
            <dl>
                <dt>基本信息</dt>
                <dd>
                    <table border="0" cellspacing="0" cellpadding="0" class="border-table" width="98%">
                        <tr>
                            <th width="20%">借款类别
                            </th>
                            <td>
                                <div class="position">
                                    <span id="spa_category" runat="server"></span>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <th>借款主体
                            </th>
                            <td>
                                <span id="spa_type" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>项目标题
                            </th>
                            <td>
                                <span id="spa_title" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>项目编号
                            </th>
                            <td>
                                <span id="spa_no" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>项目金额(元)
                            </th>
                            <td>
                                <span id="spa_amount" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>年化利率(%)
                            </th>
                            <td>
                                <span id="spa_profit_rate" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>还款期限
                            </th>
                            <td>
                                <span id="spa_repayment" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>还款方式
                            </th>
                            <td>
                                <span id="spa_repayment_type" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>申请时间
                            </th>
                            <td>
                                <span id="spa_add_time" runat="server"></span>
                            </td>
                        </tr>
                    </table>
                </dd>
            </dl>

            <dl>
                <dt>借款人信息</dt>
                <dd>
                    <table border="0" cellspacing="0" cellpadding="0" class="border-table" width="98%">
                        <tr>
                            <th width="20%">姓名
                            </th>
                            <td>
                                <div class="position">
                                    <span id="sp_loaner_name" runat="server"></span>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <th>性别
                            </th>
                            <td>
                                <span id="sp_loaner_gender" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>职业
                            </th>
                            <td>
                                <span id="sp_loaner_job" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>工作所在地
                            </th>
                            <td>
                                <span id="sp_loaner_working_at" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>手机号码
                            </th>
                            <td>
                                <span id="sp_loaner_tel" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>身份证号码
                            </th>
                            <td>
                                <span id="sp_loaner_id_card_number" runat="server"></span>
                            </td>
                        </tr>
                    </table>
                </dd>
            </dl>
            <% if (LoanType == (int)Agp2pEnums.LoanTypeEnum.Company)
                { %>
            <dl>
                <dt>企业信息</dt>
                <dd>
                    <table border="0" cellspacing="0" cellpadding="0" class="border-table" width="98%">
                        <tr>
                            <th width="20%">企业名称
                            </th>
                            <td>
                                <div class="position">
                                    <span id="sp_company_name" runat="server"></span>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <th>成立时间
                            </th>
                            <td>
                                <span id="sp_company_setup_time" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>注册资本
                            </th>
                            <td>
                                <span id="sp_company_registered_capital" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>经营范围
                            </th>
                            <td>
                                <span id="sp_company_business_scope" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>经营状况
                            </th>
                            <td>
                                <span id="sp_company_business_status" runat="server"></span>
                            </td>
                        </tr>
                        <tr>
                            <th>年收入
                            </th>
                            <td>
                                <span id="sp_company_income_yearly" runat="server"></span>
                            </td>
                        </tr>
                    </table>
                </dd>
            </dl>
            <% } %>
            <dl>
                <dt>抵押物信息</dt>
                <dd>
                    <asp:Repeater ID="rptList" runat="server">
                        <HeaderTemplate>
                            <table width="100%" border="0" cellspacing="0" cellpadding="0" class="ltable">
                                <tr>
                                    <th width="10%">选择</th>
                                    <th align="left">名称</th>
                                    <th align="left">类型</th>
                                    <th align="left">估值</th>
                                    <th align="left">状态</th>
                                </tr>
                        </HeaderTemplate>
                        <ItemTemplate>
                            <tr>
                                <td align="center">
                                    <asp:CheckBox ID="chkId" CssClass="checkall" runat="server" Style="vertical-align: middle;" Checked='<%# Eval("check")%>' Enabled="False" />
                                    <asp:HiddenField ID="hidId" Value='<%#Eval("id")%>' runat="server" />
                                </td>
                                <td><%# Eval("name")%></td>
                                <td><%# Eval("typeName")%></td>
                                <td><%# Eval("valuation")%></td>
                                <td title="<%#Loan.QueryUsingProject(((Agp2p.BLL.loan.MortgageItem) Container.DataItem).id)%>"><%# Utils.GetAgp2pEnumDes((Agp2pEnums.MortgageStatusEnum)Convert.ToByte(Eval("status")))%></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate>
                            <%#rptList.Items.Count == 0 ? "<tr><td align=\"center\" colspan=\"6\">暂无记录</td></tr>" : ""%>
                            </table>
                        </FooterTemplate>
                    </asp:Repeater>
                </dd>
            </dl>
            <!-- 风控信息 -->
            <dl>
                <dt>风控信息</dt>
                <dd>
                    <table border="0" cellspacing="0" cellpadding="0" class="border-table" width="98%">
                        <% if (LoanType == (int)Agp2pEnums.LoanTypeEnum.Creditor)
                            { %>
                        <tr>
                            <th width="20%">债权人
                            </th>
                            <td>
                                <div class="position">
                                    <span id="spa_creditor" runat="server"></span>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <th width="20%">债权人描述
                            </th>
                            <td>
                                <div class="position">
                                    <span id="spa_creditorContent" runat="server"></span>
                                </div>
                            </td>
                        </tr>
                        <% } %>
                        <tr>
                            <th width="20%">借款描述
                            </th>
                            <td>
                                <div class="position">
                                    <span id="spa_loanerContent" runat="server"></span>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <th width="20%">借款用途
                            </th>
                            <td>
                                <div class="position">
                                    <span id="spa_loanUse" runat="server"></span>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <th width="20%">还款来源
                            </th>
                            <td>
                                <div class="position">
                                    <span id="spa_repaymentSource" runat="server"></span>
                                </div>
                            </td>
                        </tr>
                        <tr>
                            <th width="20%">风控描述
                            </th>
                            <td>
                                <div class="position">
                                    <span id="spa_txtRiskContent" runat="server"></span>
                                </div>
                            </td>
                        </tr>
                    </table>
                </dd>
            </dl>
            <dl>
                <dt>借款合同</dt>
                <dd>
                    <div class="upload-box upload-album">
                    </div>
                    <div class="photo-list">
                        <ul>
                            <asp:Repeater ID="rptLoanAgreement" runat="server">
                                <ItemTemplate>
                                    <li>
                                        <input type="hidden" name="hid_photo_name" value="<%# Eval("id") %>|<%#Eval("original_path")%>|<%#Eval("thumb_path")%>" />
                                        <input type="hidden" name="hid_photo_remark" value="<%#Eval("remark")%>" />
                                        <div class="img-box" onclick="getBigPic(this);">
                                            <img src="<%#Eval("thumb_path")%>" bigsrc="<%#Eval("original_path")%>" />
                                            <span class="remark"><i>
                                                <%#Eval("remark").ToString() == "" ? "暂无描述..." : Eval("remark").ToString()%></i></span>
                                        </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                    </div>
                </dd>
                <input type="hidden" name="hid_photo_name" value="splitter" />
                <input type="hidden" name="hid_photo_remark" value="splitter" />
            </dl>
            <dl>
                <dt>抵押合同</dt>
                <dd>
                    <div class="upload-box upload-album">
                    </div>
                    <div class="photo-list">
                        <ul>
                            <asp:Repeater ID="rptMortgageContracts" runat="server">
                                <ItemTemplate>
                                    <li>
                                        <input type="hidden" name="hid_photo_name" value="<%# Eval("id") %>|<%#Eval("original_path")%>|<%#Eval("thumb_path")%>" />
                                        <input type="hidden" name="hid_photo_remark" value="<%#Eval("remark")%>" />
                                        <div class="img-box" onclick="getBigPic(this);">
                                            <img src="<%#Eval("thumb_path")%>" bigsrc="<%#Eval("original_path")%>" />
                                            <span class="remark"><i>
                                                <%#Eval("remark").ToString() == "" ? "暂无描述..." : Eval("remark").ToString()%></i></span>
                                        </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                    </div>
                </dd>
                <input type="hidden" name="hid_photo_name" value="splitter" />
                <input type="hidden" name="hid_photo_remark" value="splitter" />
            </dl>
            <dl>
                <dt>他项证</dt>
                <dd>
                    <div class="upload-box upload-album">
                    </div>
                    <div class="photo-list">
                        <ul>
                            <asp:Repeater ID="rptLienCertificates" runat="server">
                                <ItemTemplate>
                                    <li>
                                        <input type="hidden" name="hid_photo_name" value="<%# Eval("id") %>|<%#Eval("original_path")%>|<%#Eval("thumb_path")%>" />
                                        <input type="hidden" name="hid_photo_remark" value="<%#Eval("remark")%>" />
                                        <div class="img-box" onclick="getBigPic(this);">
                                            <img src="<%#Eval("thumb_path")%>" bigsrc="<%#Eval("original_path")%>" />
                                            <span class="remark"><i>
                                                <%#Eval("remark").ToString() == "" ? "暂无描述..." : Eval("remark").ToString()%></i></span>
                                        </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                    </div>
                </dd>
                <input type="hidden" name="hid_photo_name" value="splitter" />
                <input type="hidden" name="hid_photo_remark" value="splitter" />
            </dl>
        </div>
        <!--/内容-->

        <!--工具栏-->
        <div class="page-footer">
            <div class="btn-list">
                <asp:Button ID="btnAudit" runat="server" Text="通过" CssClass="btn" OnClick="btnAudit_OnClick"/>
                <asp:Button ID="btnNotAudit" runat="server" Text="不通过" CssClass="btn" OnClick="btnNotAudit_OnClick"/>
                <input name="btnReturn" type="button" value="返回上一页" class="btn yellow"
                    onclick="location.href='loan_audit.aspx?channel_id=<%=this.ChannelId%>'" />
            </div>
            <div class="clear">
            </div>
        </div>
        <!--/工具栏-->
    </form>
</body>
</html>