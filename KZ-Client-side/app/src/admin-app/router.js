import CommonHelper from "../components/helpers/CommonHelper";
import Vue from "vue";
import VueRouter from "vue-router";

Vue.use(VueRouter);

const router = new VueRouter({
	mode: "history",
	base: CommonHelper.adminPageBase,
	routes: [{
		path: "/sc/gallery",
		component: () => import("../components/sc/StreetSignsSymbolsGallery")
	},{
		path: "/sc/create",
		component: () => import("../components/sc/StreetSignSymbolCreator")
	},{
		path: "/sc/edit",
		component: () => import("../components/sc/StreetSignSymbolEditor")
	},{
		path: "/sc/elements-gallery",
		component: () => import("../components/sc/StreetSignsSymbolsElementsGallery")
	},{
		path: "/sc/element-create",
		component: () => import("../components/sc/StreetSignSymbolElementCreator")
	},{
		path: "/sc/element-edit",
		component: () => import("../components/sc/StreetSignSymbolElementEditor")
	}]
});

export default router;