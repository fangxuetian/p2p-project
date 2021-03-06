import "bootstrap-webpack";
import "../less/common.less";
import "../less/login.less";
import footerInit from "./footer.js";

/*rem的相对单位定义*/
$("html").css("font-size", $(window).width() * 0.9 / 20);

//获取url中的参数
function getUrlParam(name) {
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)"); //构造一个含有目标参数的正则表达式对象
    var r = window.location.search.substr(1).match(reg);  //匹配目标参数
    if (r != null) return unescape(r[2]); 
    return null; //返回参数值
}

//初始化验证表单
$(function () {
    $(window).load(function() {
       if (location.href.indexOf("url") != -1) {
           var RegUrl = getUrlParam('url')+"&id=" + getUrlParam('id');
           $("#registerBtn").attr("href", "/mobile/register.html?url=" + RegUrl);
       } 
    });

    //提交表单
    var btnSubmit = $("#btnSubmit");
    btnSubmit.bind("click", function() {
        if ($("#txtUserName").val() == "" || $("#txtPassword").val() == "") {
            $("#msgtips").show();
            $("#msgtips dd").text("请填写用户名和登录密码！");
            return false;
        }
        $.ajax({
                type: "POST",
                url: "/tools/submit_ajax.ashx?action=user_login",
                dataType: "json",
                data: {
                    "txtUserName": $("#txtUserName").val(),
                    "txtPassword": $("#txtPassword").val(),
                    "code": $("#code").val(),
                    "chkRemember": $("input[type=checkbox]").is(":checked")
                },
                timeout: 20000,
                beforeSend: function(XMLHttpRequest) {
                    btnSubmit.attr("disabled", true);
                    $("#msgtips").show();
                    $("#msgtips dd").text("正在登录，请稍候...");
                },
                success: function(data, textStatus) {
                    if (data.status == 1) {
                        if(document.URL !== "" && document.URL.indexOf("url") != -1) {
                            location.href = getUrlParam('url')+"&id=" + getUrlParam('id');
                        }
                        else if (typeof(data.url) == "undefined") {
                            location.href = $("#loginform").attr("data-turl");
                            //location.href = '/'
                        } else {
                            location.href = data.url;
                        }
                } else {
                    btnSubmit.attr("disabled", false);
                    $("#msgtips dd").text(data.msg);
                }
            },
            error: function(XMLHttpRequest, textStatus, errorThrown) {
                $("#msgtips dd").text("状态：" + textStatus + "；出错提示：" + errorThrown);
                btnSubmit.attr("disabled", false);
            }
        });
        return false;
    });
});