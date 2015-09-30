import $ from "jquery";
import "bootstrap-webpack";
import "../less/head.less";
import "../less/invest_detail.less";
import "../less/footerSmall.less";
import "visualnav";

import header from "./header.js";

$(function () {
    $(window).scroll(function () {
        var scrollTop = $(window).scrollTop();
        var rightNav = $(".project-content-right ul");
        if (scrollTop >= 560) {
            rightNav.removeClass("notScroll");
            rightNav.addClass("scrolled");
        } else {
            rightNav.removeClass("scrolled");
            rightNav.addClass("notScroll");
        }
    });

    header.setHeaderHighlight(1);

    $("#sidemenu").visualNav({
        selectedClass : "active",
        selectedAppliedTo : 'a',
        animationTime     : 600,
    });
});