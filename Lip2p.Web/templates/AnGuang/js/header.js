﻿var $ = require("jquery");

module.exports = {
    setHeaderHighlight: function (index) {
        $("ul.in-header > li:nth(" + index + ") > a:first").addClass("nav-active");
    }
}