import Vue from "vue";
import Vuex from "vuex";
import CommonHelper from "../components/helpers/CommonHelper";

Vue.use(Vuex);

const store = new Vuex.Store({
	state: {
		layersInfoDict: null,
		mapServicesFailedToLoad: null,
		myMap: null,
		testDelay: window.appEnv == "development" ? 200 : 0,
		title: window.title,
		userData: null,
		activeFeature: null,
		activeFeatureInOverlay: null,
		activeFeatureInOverlayPreview: null,
		activeFeaturePreview: null,
		scItem: null,
		newItemState: null,
		activeTask: null,
		activeTaskFeatures: null,
		activeTaskFeaturesGrouped: null,
		sc5XXInteractive: true,
		singleClickFeatures: null,
		singleClickFeatures4Overlay: null,
		hideNewUnderConstructionFunctionality: false,
		activeAction: null,
		mapLayersListInMenu: false,
		additionalLayersIdentifyResultInMapOverlay: true,
		initialCoordinates: null
	},

	mutations: {
		resetFailedMapServices: function(state){ // Kaip "Kapinių APP"
			state.mapServicesFailedToLoad = null;
		},
		setMapServiceAsFailed: function(state, service){ // Kaip "Kapinių APP"
			var mapServicesFailedToLoad;
			if (state.mapServicesFailedToLoad) {
				mapServicesFailedToLoad = state.mapServicesFailedToLoad.slice();
			} else {
				mapServicesFailedToLoad = [];
			}
			mapServicesFailedToLoad.push(service);
			state.mapServicesFailedToLoad = mapServicesFailedToLoad;
		},
		setMyMap: function(state, myMap){ // Kaip "Kapinių APP"
			state.myMap = myMap;
			state.activeFeaturePreview = null;
			if (!myMap) {
				state.activeTask = state.activeTaskFeatures = state.activeTaskFeaturesGrouped = null;
			}
		},
		setUserData: function(state, userData){ // Kaip "Kapinių APP"
			state.userData = userData;
		},
		setLayersInfoDict: function(state, layersInfoDict){
			state.layersInfoDict = layersInfoDict;
		},
		setActiveFeature: function(state, e){
			if (!CommonHelper.isActiveFeatureTheSameAsActiveFeaturePreview(state.activeFeaturePreview, e)) {
				state.activeFeaturePreview = null;
			}
			if (!CommonHelper.isActiveFeatureInSingleClickFeatures(state.singleClickFeatures, e)) {
				state.singleClickFeatures = null;
			}
			state.activeFeature = e;
			if (e) {
				state.activeAction = null;
			}
		},
		setActiveFeatureInOverlay: function(state, e){
			if (!CommonHelper.isActiveFeatureTheSameAsActiveFeaturePreview(state.activeFeatureInOverlayPreview, e)) {
				state.activeFeatureInOverlayPreview = null;
			}
			if (!CommonHelper.isActiveFeatureInSingleClickFeatures(state.singleClickFeatures4Overlay, e)) {
				state.singleClickFeatures4Overlay = null;
			}
			state.activeFeatureInOverlay = e;
		},
		setActiveFeaturePreview: function(state, feature){
			state.activeFeaturePreview = feature;
		},
		setActiveFeatureInOverlayPreview: function(state, feature){
			state.activeFeatureInOverlayPreview = feature;
		},
		setSCItem: function(state, scItem){
			state.scItem = scItem;
		},
		setMapNewItemState: function(state, newItemState){
			state.newItemState = newItemState;
		},
		setActiveTask: function(state, activeTask){
			state.activeTask = activeTask;
			var activeTaskFeatures = CommonHelper.getActiveTaskFeatures(activeTask, state.myMap);
			state.activeTaskFeatures = activeTaskFeatures;
			var activeTaskFeaturesGrouped = {};
			if (state.myMap) {
				for (var key in activeTaskFeatures) {
					if (activeTaskFeatures[key]) {
						var featureId;
						activeTaskFeatures[key].forEach(function(item){
							if (item.action == "update" || item.action == "delete") {
								featureId = item.feature.get(CommonHelper.taskFeatureOriginalGlobalIdFieldName);
								if (featureId) {
									activeTaskFeaturesGrouped[featureId] = item;
								}
							}
						});
					}
				}
			}
			state.activeTaskFeaturesGrouped = activeTaskFeaturesGrouped;
		},
		setSingleClickFeatures: function(state, features){
			state.singleClickFeatures = features;
		},
		setSingleClickFeatures4Overlay: function(state, features){
			state.singleClickFeatures4Overlay = features;
		},
		setActiveAction: function(state, e){
			state.activeAction = e;
		},
		setInitialCoordinates: function(state, e){
			state.initialCoordinates = e;
		}
	},

	getters: {
		getServiceUrl: (state) => (serviceId) => {
			var userData = state.userData,
				url;
			if (userData) {
				if (serviceId == "street-signs") {
					url = userData["street-signs-service-root"] + "FeatureServer";
				} else if (serviceId == "street-signs-vertical") {
					url = userData["vertical-street-signs-service-root"] + "FeatureServer";
				} else if ((serviceId == "vms-inventorization-l") || (serviceId == "vms-inventorization-p")) {
					if (serviceId == "vms-inventorization-l") {
						url = userData["vms-inventorization-polylines-service-root"];
					} else if (serviceId == "vms-inventorization-p") {
						url = userData["vms-inventorization-points-service-root"];
					}
					if (url) {
						url += "FeatureServer";
					}
				} else if (serviceId == "tasks") {
					url = userData["tasks-service-root"] + "FeatureServer";
				} else if (serviceId == "vvt") {
					url = userData["vvt-service-root"] + "FeatureServer";
				} else if (serviceId == "social-mobility") {
					url = userData["social-mobility"] + "MapServer";
				}
			}
			if (serviceId == "waze") {
				url = "https://arcgis.sisp.lt/arcgis/rest/services/Waze/Waze_public/FeatureServer";
			}
			return url;
		}
	}
});

export default store;