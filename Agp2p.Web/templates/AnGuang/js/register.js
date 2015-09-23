﻿import "bootstrap-webpack";
import "../less/head.less";
import "../less/register.less";
import "../less/footerSmall.less";

import $ from "jquery";

$(function(){
    // 刷新图文验证码
    var $picCode = $(".pic-code-wrap img");
    var originSrc = $picCode[0].getAttribute("src");
    $picCode.click(function(){
        this.setAttribute("src", originSrc + "?r=" + Math.random());
    });

    // 获取短信验证码
    $("#get-sms-btn").click(function(){
        var phone = $("#account").val();
        var picCode = $("#pic-code").val();
        $.ajax({
            url: "/tools/submit_ajax.ashx?action=user_register_smscode",
            type: "post",
            dataType: "json",
            data: {
                mobile: phone,
                txtPicCode: picCode
            },
            success: function (data) {
                alert(data.msg);
            },
            error: function (data) {
                alert("操作超时，请重试");
            }
        });
    });

    // 注册
    var $register = $("#registerBtn");
    $register.click(function(){
        $.ajax({
            url: "/tools/submit_ajax.ashx?action=user_register",
            type: "post",
            dataType: "json",
            data: {
                txtSMSCode: $("#sms-code").val(),
                txtMobile: $("#account").val(),
                txtPassword: $("#psw").val(),
                txtPassword1: $("#psw2").val(),
                txtInviteNo: $("#recommended").val()
            },
            success: function(data){
                alert(data.msg);
            },
            error: function(data){
                alert("操作超时，请重试");
            }
        });
    });
});
