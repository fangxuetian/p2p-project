﻿import "bootstrap-webpack";
import "../less/head.less";
import "../less/usercenter.less";
import "../less/footerSmall.less";


import React from "react"
import MyTransaction from "../containers/mytransaction.jsx"
import RechargePage from "../containers/recharge.jsx"

$(function(){
    //点击导航加载相应内容
    var $mainContent = $("div.content-body");
    var basePath = $mainContent.data("templateskin");
    var aspxPath = $mainContent.data("aspx-path");
    var $nav = $(".outside-ul li");

    $("#tradeDetails").click(function(){
        $nav.removeClass("nav-active");
        $(this).parent().addClass("nav-active");

        React.render(<MyTransaction aspxPath={aspxPath} />, $mainContent[0]);
    });

    //加载我要充值内容
    $("#recharge").click(function(){
        $nav.removeClass("nav-active");
        $(this).parent().addClass("nav-active");

        React.render(<RechargePage templateBasePath={basePath} />, $mainContent[0]);
    });

    //加载我要提现内容
    $("#withdraw").click(function(){
        $nav.removeClass("nav-active");
        $(this).parent().addClass("nav-active");
        $mainContent.load(basePath + "/_withdraw.html",function(){
            //提现银行卡选择
            var $card = $(".ul-withdraw .card");
            $card.click(function(){
                $card.find("img").remove();
                var img = document.createElement("img");
                img.src = basePath + "/imgs/usercenter/withdraw-icons/selected.png";
                this.appendChild(img);
            });
        })
    });
});