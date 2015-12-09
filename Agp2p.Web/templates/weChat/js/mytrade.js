﻿import "bootstrap-webpack";
import "../less/footer.less";
import "../less/trade-record.css";
import template from "lodash/string/template"

/*rem的相对单位定义*/
var viewportWidth = $(window).width();
var fontSizeUnit = viewportWidth / 20;
$("html").css("font-size", fontSizeUnit);

var templateSettings = {
    evaluate: /@\{([\s\S]+?)\}@/g, // 求值 @{ ... }@
    interpolate: /@\{=([\s\S]+?)\}@/g // 直接输出 @{= ...}@
};

var render = template($("#record-item").html(), templateSettings);
function loadData(pageIndex, callback) {
    var pageSize = 15;
    var loadingHint = $("#loading-hint");
    var emptyBox = $("div.loading-hint");
    loadingHint.text("加载中...");
    loadingHint.show();
    emptyBox.hide();
    $.ajax({
        type: "POST",
        url: reqFilePath + "/AjaxQueryTransactionHistory",
        data: JSON.stringify({ pageIndex, pageSize, type: 0, startTime: "", endTime: "" }),
        contentType: "application/json",
        dataType: "json",
        success: function (msg) {
            var ls = JSON.parse(msg.d).data;
            $("#pending").append(render({ items: ls }));
            if (ls.length < pageSize) {
                $(window).unbind('scroll');
            }
            loadingHint.hide();
            if (pageIndex === 0 && ls.length === 0) {
                emptyBox.show();
            } else {
                emptyBox.hide();
            }
            callback(true);
        }
    }).fail(function () {
        loadingHint.text("加载失败");
        callback(false);
    });
}
$(function() {
    $("#pending").on("click", ".record-cell", function() {
        $(".remark[data-id=" + $(this).attr("data-id") + "]").toggle();
    });
    var processing = false;
    var loadedPage = 0;
    loadData(loadedPage, function(succ) {
        if (succ) loadedPage += 1;
    });

    $(window).scroll(function() {
        if (processing) return;

        if (($(document).height() - $(window).height())*0.7 <= $(window).scrollTop()) {
            processing = true; //sets a processing AJAX request flag

            loadData(loadedPage, function(succ) {
                if (succ) loadedPage += 1;
                processing = false;
            });
        }
    });
});