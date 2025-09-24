const pages = {
	admin: {
		entry: "src/admin-app/main.js",
		template: "public/index.html",
		filename: "admin.html",
		title: "Kauno miesto techninių eismo reguliavimo priemonių IS"
	}
};
const { defineConfig } = require("@vue/cli-service");
module.exports = defineConfig({
	transpileDependencies: [
		"vuetify"
	],
	productionSourceMap: false,
	pages: pages,
	devServer: {
		port: 3001,
		proxy: {
			"^/kauno_eop_is/(web-services|Proxy)/": {
				target: "https://localhost:44397/",
				changeOrigin: true
			}
		}
	},
	chainWebpack: config => {
		if (process.env.NODE_ENV === "production") {
			Object.keys(pages).forEach(page => {
				config.plugins.delete(`html-${page}`);
				config.plugins.delete(`preload-${page}`);
				config.plugins.delete(`prefetch-${page}`);
			});
		}
	},
	configureWebpack: {
		output: {
			chunkFilename: (pathData) => {
				if (["chunk-vendors", "chunk-common"].indexOf(pathData.chunk.name) != -1) {
					return "js/[name].js";
				}
				return "js/[id].[contenthash].js";
			},
		}
	},
	css: {
		extract: false
	},
	outputDir: "../../KZ-Server-side/KZ/Scripts",
	filenameHashing: false,
	publicPath: process.env.NODE_ENV === "production" ? process.env.VUE_APP_ROOT + "/Scripts/" : "/"
});