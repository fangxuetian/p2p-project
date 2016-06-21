import "bootstrap-webpack!./bootstrap.config.js";
import "../less/head.less";
import "../less/about_more.less";
import "../less/footerSmall.less";

import header from "./header.js";
window['$'] = $;

$(function(){
    

    //加入我们 招聘列表开关样式
    var $office = $(".join-us-wrap .content-body .office > ul > li");
    $office.click(function() {
        var index = $office.index(this);
        $(this).find("i").toggleClass("glyphicon-menu-up");
        $(".office-detail").eq(index).toggle();
    });
    $('[data-toggle="popover"]').popover();
});