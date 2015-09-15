﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="loan_apply_edit.aspx.cs" Inherits="Lip2p.Web.admin.project.loan_apply_edit" %>

<%@ Import Namespace="Lip2p.Common" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title>申请借款</title>
    <script type="text/javascript" src="../../scripts/jquery/jquery-1.10.2.min.js"></script>
    <script type="text/javascript" src="../../scripts/jquery/Validform_v5.3.2_min.js"></script>
    <script type="text/javascript" src="../../scripts/lhgdialog/lhgdialog.js?skin=idialog"></script>
    <script type="text/javascript" src="../../scripts/datepicker/WdatePicker.js"></script>
    <script type="text/javascript" src="../../scripts/swfupload/swfupload.js"></script>
    <script type="text/javascript" src="../../scripts/swfupload/swfupload.queue.js"></script>
    <script type="text/javascript" src="../../scripts/swfupload/swfupload.handlers.js"></script>
    <script type="text/javascript" charset="utf-8" src="../../editor/kindeditor-min.js"></script>
    <script type="text/javascript" charset="utf-8" src="../../editor/lang/zh_CN.js"></script>
    <script type="text/javascript" src="../js/layout.js"></script>
    <link href="../skin/default/style.css" rel="stylesheet" type="text/css" />
    <script type="text/javascript">
        $(function () {
            //初始化表单验证
            $("#form1").initValidform();
            //初始化编辑器
            var editor = KindEditor.create('.editor', {
                width: '98%',
                height: '350px',
                resizeType: 1,
                uploadJson: '../../tools/upload_ajax.ashx?action=EditorFile&IsWater=1',
                fileManagerJson: '../../tools/upload_ajax.ashx?action=ManagerFile',
                allowFileManager: true
            });
            var editorMini = KindEditor.create('.editor-mini', {
                width: '98%',
                height: '250px',
                resizeType: 1,
                uploadJson: '../../tools/upload_ajax.ashx?action=EditorFile&IsWater=1',
                items: [
				'fontname', 'fontsize', '|', 'forecolor', 'hilitecolor', 'bold', 'italic', 'underline',
				'removeformat', '|', 'justifyleft', 'justifycenter', 'justifyright', 'insertorderedlist',
				'insertunorderedlist', '|', 'emoticons', 'image', 'link']
            });
            //初始化上传控件
            $(".upload-img").each(function () {
                $(this).InitSWFUpload({ filesize: "<%=siteConfig.imgsize %>", sendurl: "../../tools/upload_ajax.ashx", flashurl: "../../scripts/swfupload/swfupload.swf", filetypes: "*.<%=siteConfig.fileextension.Replace(",",";*.") %>" });
            });
            //设置封面图片的样式
            $(".photo-list ul li .img-box img").each(function () {
                if ($(this).attr("src") == $("#hidFocusPhoto").val()) {
                    $(this).parent().addClass("selected");
                }
            });
            //选择tab
            $(function () {
                if ("<%=select_tab_index%>" == 0) {
                    //设置点击后的切换样式
                    $("#a_item_base").parent().parent().find("li a").removeClass("selected");
                    $("#a_item_base").addClass("selected");
                    //根据参数决定显示内容
                    $(".tab-content").hide();
                    $(".tab-content").eq(0).show();
                }
                else {
                    $("#a_item_mortgages").parent().parent().find("li a").removeClass("selected");
                    $("#a_item_mortgages").addClass("selected");
                    //根据参数决定显示内容
                    $(".tab-content").hide();
                    $(".tab-content").eq(1).show();
                }

            });
        });
    </script>
</head>
<body class="mainbody">
    <form id="form1" runat="server">
        <!--导航栏-->
        <div class="location">
            <a href="loan_apply.aspx?channel_id=<%=this.channel_id %>" class="back"><i></i><span>返回列表页</span></a> <a href="../center.aspx" class="home"><i></i><span>首页</span></a>
            <i class="arrow"></i><a href="loan_apply.aspx?channel_id=<%=this.channel_id %>">
                <span>申请借款</span></a> <i class="arrow"></i><span>借款编辑</span>
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
                        <li><a href="javascript:;" id="a_item_base" onclick="tabs(this);" class="selected">基本信息</a></li>
                        <li><a href="javascript:;" id="a_item_mortgages" onclick="tabs(this);">标的信息</a></li>
                        <li><a href="javascript:;" onclick="tabs(this);">风控信息</a></li>
                        <li><a href="javascript:;" onclick="tabs(this);">SEO选项</a></li>
                    </ul>
                </div>
            </div>
        </div>
        <div class="tab-content">
            <dl>
                <dt>借款类别</dt>
                <dd>
                    <div class="rule-single-select">
                        <asp:DropDownList ID="ddlCategoryId" runat="server" datatype="*" sucmsg=" ">
                        </asp:DropDownList>
                    </div>
                </dd>
            </dl>
            <dl>
                <dt>借款主体</dt>
                <dd>
                    <div class="rule-multi-radio">
                        <asp:RadioButtonList ID="rbl_project_type" runat="server" RepeatDirection="Horizontal"
                            RepeatLayout="Flow" AutoPostBack="true" OnSelectedIndexChanged="rbl_project_type_SelectedIndexChanged">
                            <asp:ListItem Value="10" Selected="True">企业</asp:ListItem>
                            <asp:ListItem Value="20">个人</asp:ListItem>
                            <asp:ListItem Value="30">债权</asp:ListItem>
                        </asp:RadioButtonList>
                    </div>
                </dd>
            </dl>
            <dl>
                <dt>项目标题</dt>
                <dd>
                    <asp:TextBox ID="txtTitle" runat="server" CssClass="input normal" datatype="*2-100"
                        sucmsg=" " onBlur="changeProjectNo(this.value, txt_project_no)" />
                    <span class="Validform_checktip">*标题最多100个字符</span>
                </dd>
            </dl>
            <dl id="div_project_no">
                <dt>
                    <asp:Label ID="div_project_no_title" runat="server" Text="项目编号" /></dt>
                <dd>
                    <asp:TextBox ID="txt_project_no" runat="server" CssClass="input normal" datatype="*0-100"
                        sucmsg=" " />
                    <asp:Label ID="div_project_no_tip" runat="server" CssClass="Validform_checktip" />
                </dd>
            </dl>
            <dl id="div_project_amount">
                <dt>
                    <asp:Label ID="div_project_amount_title" runat="server" Text="项目金额" /></dt>
                <dd>
                    <asp:TextBox ID="txt_project_amount" runat="server" CssClass="input txt" datatype="/^(([1-9]{1}\d*)|([0]{1}))(\.(\d){1,4})?$/"
                        sucmsg=" "></asp:TextBox>
                    元
                <asp:Label ID="div_project_amount_tip" runat="server" CssClass="Validform_checktip" />
                </dd>
            </dl>
            <dl id="div_project_profit_rate">
                <dt>
                    <asp:Label ID="div_project_profit_rate_title" runat="server" Text="年化利率" /></dt>
                <dd>
                    <asp:TextBox ID="txt_project_profit_rate" runat="server" CssClass="input small" datatype="*"
                        sucmsg=" "></asp:TextBox>
                    %
                <asp:Label ID="div_project_profit_rate_tip" runat="server" CssClass="Validform_checktip" />
                </dd>
            </dl>
            <dl id="div_project_repayment_number">
                <dt>
                    <asp:Label ID="div_project_repayment_number_title" runat="server" Text="还款期限" /></dt>
                <dd>
                    <asp:TextBox ID="txt_project_repayment_number" runat="server" CssClass="input small"
                        datatype="n" sucmsg=" ">3</asp:TextBox>
                    <asp:Label ID="div_project_repayment_number_tip" runat="server" CssClass="Validform_checktip" />
                    <div class="rule-multi-radio">
                        <asp:RadioButtonList ID="txt_project_repayment_term" runat="server" RepeatDirection="Horizontal"
                            RepeatLayout="Flow">
                            <asp:ListItem Value="10">年</asp:ListItem>
                            <asp:ListItem Value="20" Selected="True">月</asp:ListItem>
                            <asp:ListItem Value="30">日</asp:ListItem>
                        </asp:RadioButtonList>
                    </div>
                </dd>
            </dl>
            <dl id="div_project_repayment_type">
                <dt>
                    <asp:Label ID="div_project_repayment_type_title" runat="server" Text="还款方式" /></dt>
                <dd>
                    <div class="rule-multi-radio">
                        <asp:RadioButtonList ID="txt_project_repayment_type" runat="server" RepeatDirection="Horizontal"
                            RepeatLayout="Flow">
                            <asp:ListItem Value="10" Selected="True">先息后本</asp:ListItem>
                            <asp:ListItem Value="20">等额本息</asp:ListItem>
                        </asp:RadioButtonList>
                    </div>
                    <asp:Label ID="div_project_repayment_type_tip" runat="server" CssClass="Validform_checktip" />
                </dd>
            </dl>
            <dl>
                <dt>排序数字</dt>
                <dd>
                    <asp:TextBox ID="txtSortId" runat="server" CssClass="input small" datatype="n" sucmsg=" ">99</asp:TextBox>
                    <span class="Validform_checktip">*数字，越小越向前</span>
                </dd>
            </dl>
            <dl>
                <dt>浏览次数</dt>
                <dd>
                    <asp:TextBox ID="txtClick" runat="server" CssClass="input small" datatype="n" sucmsg=" ">0</asp:TextBox>
                    <span class="Validform_checktip">点击浏览该信息自动+1</span>
                </dd>
            </dl>
            <dl>
                <dt>封面图片</dt>
                <dd>
                    <asp:TextBox ID="txtImgUrl" runat="server" CssClass="input normal upload-path" />
                    <div class="upload-box upload-img">
                    </div>
                </dd>
            </dl>
            <dl>
                <dt>发布时间</dt>
                <dd>
                    <div class="input-date">
                        <asp:TextBox ID="txtAddTime" runat="server" CssClass="input date" onfocus="WdatePicker({dateFmt:'yyyy-MM-dd HH:mm:ss'})"
                            datatype="/^\s*$|^\d{4}\-\d{1,2}\-\d{1,2}\s{1}(\d{1,2}:){2}\d{1,2}$/" errormsg="请选择正确的日期"
                            sucmsg=" " />
                        <i></i>
                    </div>
                    <span class="Validform_checktip">不选择默认当前发布时间</span>
                </dd>
            </dl>
        </div>
        <%-- 标的信息 --%>
        <div class="tab-content" style="display: none" id="div_mortgages_info">
            <dl>
                <dl>
                    <dt>选择借款人</dt>
                    <dd>
                        <div class="rule-single-select">
                            <asp:DropDownList ID="ddlLoaner" runat="server" datatype="*" sucmsg=" " AutoPostBack="true"
                                OnSelectedIndexChanged="ddlLoaner_SelectedIndexChanged">
                            </asp:DropDownList>
                        </div>
                    </dd>
                </dl>
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
            <dl>
                <dt>标的物信息</dt>
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
                                    <asp:CheckBox ID="chkId" CssClass="checkall" runat="server" Style="vertical-align: middle;" Checked='<%# Eval("check")%>' Enabled='<%# Eval("enable") %>' />
                                    <asp:HiddenField ID="hidId" Value='<%#Eval("id")%>' runat="server" />
                                </td>
                                <td><%# Eval("name")%></td>
                                <td><%# Eval("typeName")%></td>
                                <td><%# Eval("valuation")%></td>
                                <td title="<%# QueryUsingProject(((MortgageItem) Container.DataItem).id)%>"><%# Utils.GetLip2pEnumDes((Lip2pEnums.MortgageStatusEnum)Convert.ToByte(Eval("status")))%></td>
                            </tr>
                        </ItemTemplate>
                        <FooterTemplate>
                            <%#rptList.Items.Count == 0 ? "<tr><td align=\"center\" colspan=\"6\">暂无记录</td></tr>" : ""%>
                            </table>
                        </FooterTemplate>
                    </asp:Repeater>
                </dd>
            </dl>
        </div>
        <%-- 风控信息 --%>
        <div class="tab-content" style="display: none" id="div_risks_info">
            <dl>
                <dt>债权人</dt>
                <dd>
                    <div class="rule-single-select">
                        <asp:DropDownList ID="ddlCreditor" runat="server" datatype="*" sucmsg=" ">
                        </asp:DropDownList>
                    </div>
                </dd>
            </dl>
            <dl>
                <dt>债权人描述</dt>
                <dd>
                    <asp:TextBox ID="txtCreditorContent" runat="server" CssClass="input" TextMode="MultiLine"
                        datatype="*0-255" sucmsg=" ">债权人为广东汇诚创利电子商务有限公司资产部员工，由公司授权、代表广东汇诚创利电子商务有限公司全权处理与客户的的债权关系。
                    </asp:TextBox>
                    <span class="Validform_checktip"></span>
                </dd>
            </dl>
            <dl>
                <dt>借款人描述</dt>
                <dd>
                    <asp:TextBox ID="txtLoanerContent" runat="server" CssClass="input" TextMode="MultiLine"
                        datatype="*0-255" sucmsg=" "></asp:TextBox>
                    <span class="Validform_checktip"></span>
                </dd>
            </dl>
            <dl>
                <dt>风控描述</dt>
                <dd>
                    <textarea id="txtRiskContent" class="editor" style="visibility: hidden;" runat="server"></textarea>
                    <span class="Validform_checktip"></span>
                </dd>
            </dl>
            <dl>
                <dt>借款合同编号</dt>
                <dd>
                    <asp:TextBox ID="txtLoanAgreementNo" runat="server" CssClass="input normal" datatype="*0-100"
                        sucmsg=" " />
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
                                        <a href="javascript:;" onclick="setRemark(this);">描述</a> <a href="javascript:;" onclick="delImg(this);">删除</a> </li>
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
                                        <a href="javascript:;" onclick="setRemark(this);">描述</a> <a href="javascript:;" onclick="delImg(this);">删除</a> </li>
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
                                        <a href="javascript:;" onclick="setRemark(this);">描述</a> <a href="javascript:;" onclick="delImg(this);">删除</a> </li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                    </div>
                </dd>
                <input type="hidden" name="hid_photo_name" value="splitter" />
                <input type="hidden" name="hid_photo_remark" value="splitter" />
            </dl>
        </div>
        <div class="tab-content" style="display: none">
            <dl>
                <dt>SEO标题</dt>
                <dd>
                    <asp:TextBox ID="txtSeoTitle" runat="server" MaxLength="255" CssClass="input normal"
                        datatype="*0-100" sucmsg=" " />
                    <span class="Validform_checktip">255个字符以内</span>
                </dd>
            </dl>
            <dl>
                <dt>SEO关健字</dt>
                <dd>
                    <asp:TextBox ID="txtSeoKeywords" runat="server" CssClass="input" TextMode="MultiLine"
                        datatype="*0-255" sucmsg=" "></asp:TextBox>
                    <span class="Validform_checktip">以“,”逗号区分开，255个字符以内</span>
                </dd>
            </dl>
            <dl>
                <dt>SEO描述</dt>
                <dd>
                    <asp:TextBox ID="txtSeoDescription" runat="server" CssClass="input" TextMode="MultiLine"
                        datatype="*0-255" sucmsg=" "></asp:TextBox>
                    <span class="Validform_checktip">255个字符以内</span>
                </dd>
            </dl>
        </div>
        <!--/内容-->
        <!--工具栏-->
        <div class="page-footer">
            <div class="btn-list">
                <asp:Button ID="btnApproval" runat="server" Text="提交" CssClass="btn" OnClick="btnSubmit_Click" />
                <asp:Button ID="btnSave" runat="server" Text="保存" CssClass="btn"/>
                <input name="btnReturn" type="button" value="返回上一页" class="btn yellow" 
                    onclick="location.href='loan_apply.aspx?channel_id=<%=this.channel_id%>'" />
            </div>
            <div class="clear">
            </div>
        </div>
        <!--/工具栏-->
    </form>
</body>
</html>