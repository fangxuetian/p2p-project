﻿import "bootstrap-webpack!./bootstrap.config.js";
import "../less/head.less";
import "../less/coop.less";
import "../less/footerSmall.less";
import alert from "../components/tips_alert.js";

import header from "./header.js";

window['$'] = $;

$(function () {
    header.setHeaderHighlight(5);

    //data-toggle 初始化
    $('[data-toggle="popover"]').popover();

    $("a.rongzi").hover(function(){
        $("a.teamwork").css("background","#1478d2");
    },function(){
        $("a.teamwork").css("background","#1e94ff");
    });

    $("#giving").click(function() {
        $.ajax({
            type: "post",
            url: "/aspx/main/coop.aspx/Apply",
            dataType: "json",
            contentType: "application/json",
            data: JSON.stringify({
                userName: $("#accountname").val(),
                mobile: $("#phone").val(),
                type:"银票融资"
            }),
            success: function(data) {
                var r = JSON.parse(data.d);
                if(r.status == 1){
                    //提交成功
                    alert("提交申请成功!");
                } else {
                    alert(r.msg);
                }
            },
            error: function(xhr, status, err){
                alert("处理请求失败!请联系客服.");
            }
        });      
    });
});