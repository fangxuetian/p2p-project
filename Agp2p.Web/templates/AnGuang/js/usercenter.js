import "bootstrap-webpack";
import "../less/head.less";
import "../less/usercenter.less";
import "../less/footerSmall.less";

import $ from "jquery";
import header from "./header.js";
import "../less/bootstrap-datetimepicker.css";
import "./bootstrap-datetimepicker.js";
import  "./bootstrap-datetimepicker.zh-CN.js";
$(function(){
    //�������������Ӧ����
    var $mainContent = $("div.content-body");
    var basePath = $mainContent.data("templateskin");

    $("#tradeDetails").click(function(){
        $(this).parent().addClass("nav-active");
        $mainContent.load(basePath + "/_tradeDetails.html", function () {
            //��������
            $(".form_date").datetimepicker({
                language: 'zh-CN',
                format: 'yyyy-mm-dd',
                weekStart: 1,
                todayBtn: true,
                todayHighlight: 1,
                startView: 2,
                forceParse: 0,
                showMeridian: 1,
                autoclose: 1,
                minView: 2
            });

            //������ϸ ������ŷ�ת
            $(".detailRow").click(function(){
                $(this).next("tr").toggle();
                $(this).find(".glyphicon-triangle-bottom").toggleClass("glyphicon-triangle-top");
            });
        });
    });
});