var CommonsChunkPlugin = require("webpack/lib/optimize/CommonsChunkPlugin");
var path = require('path');
var webpack = require("webpack");

module.exports = {
    cache: true,
    entry: {
        // including bootstrap
        index: "./js/index.js",
        login: "./js/login.js",
        register: "./js/register.js",
        project: "./js/project.js",
        projects: "./js/projects.js",
        aboutus: "./js/aboutus.js",
        article_show: "./js/article_show.js",
        article_list: "./js/article_list.js",
        safe_defence: "./js/safe_defence.js",
        sitemap: "./js/sitemap.js",
        404: "./js/404.js",
        agreement: "./js/register-agreement.js",
        risk: "./js/risk-tips.js",
        help: "./js/help.js",
        // including bootstrap, react
        usercenter: "./js/usercenter.jsx",
        forgot_password: "./js/forgot_password.js",
    },
    output: {
        path: path.join(__dirname, './build'),
        filename: "[name].bundle.js",
        publicPath: "/templates/AnGuang/build/"
    },
    plugins: [
        // CommonsChunkPlugin 能将公共的模块抽出到单独的 js，再由页面单独引用。参考 https://webpack.github.io/docs/optimization.html
        new CommonsChunkPlugin("react.bundle.js", ["usercenter", "forgot_password"]),
        new CommonsChunkPlugin("commons.bundle.js", ["react.bundle.js", "index", "login",
            "register", "project", "projects", "aboutus", "article_show","article_list","safe_defence","sitemap","404"]),
        new webpack.ProvidePlugin({
            $: "jquery",
            jQuery: "jquery",
            "window.jQuery": "jquery",
            "root.jQuery": "jquery"
        }),
        new webpack.DefinePlugin({
            TEMPLATE_PATH : JSON.stringify("/templates/AnGuang"),
            USER_CENTER_ASPX_PATH : JSON.stringify("/aspx/main/usercenter.aspx")
        })
    ],
    module: {
        loaders: [
            { test: /clipboard.*?js$/, loader: 'babel?cacheDirectory' }, // only use by clipboard.js
            { test: /\.js$/, exclude: /node_modules/, loader: 'babel?cacheDirectory' },
            { test: /\.jsx$/, exclude: /node_modules/, loader: 'babel?cacheDirectory' },
            { test: /\.css$/, loader: "style!css!autoprefixer" },
            { test: /\.png$/, loader: "url?limit=100000" },
            { test: /\.jpg$/, loader: "file" },
            { test: /\.less$/, loader: "style!css!autoprefixer!less" },

            // **IMPORTANT** This is needed so that each bootstrap js file required by
            // bootstrap-webpack has access to the jQuery object
            //{ test: /bootstrap\/js\//, loader: 'imports?jQuery=jquery' },

            // Needed for the css-loader when [bootstrap-webpack](https://github.com/bline/bootstrap-webpack)
            // loads bootstrap's css.
            { test: /\.woff(\?v=\d+\.\d+\.\d+)?$/,   loader: "url?limit=10000&mimetype=application/font-woff" },
            { test: /\.woff2(\?v=\d+\.\d+\.\d+)?$/,   loader: "url?limit=10000&mimetype=application/font-woff2" },
            { test: /\.ttf(\?v=\d+\.\d+\.\d+)?$/,    loader: "url?limit=10000&mimetype=application/octet-stream" },
            { test: /\.eot(\?v=\d+\.\d+\.\d+)?$/,    loader: "file" },
            { test: /\.svg(\?v=\d+\.\d+\.\d+)?$/,    loader: "url?limit=10000&mimetype=image/svg+xml" }
        ]
    }
};