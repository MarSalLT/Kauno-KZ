<template>
	<div ref="map" class="map-cont">
		<template v-if="loading">
			<v-progress-circular
				indeterminate
				color="primary"
				:size="40"
				width="2"
				class="ma-3"
			></v-progress-circular>
		</template>
		<template v-if="layersLoadingCounter">
			<v-progress-linear
				indeterminate
				color="primary"
				height="4"
				class="layer-loading-indicator"
			></v-progress-linear>
		</template>
	</div>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";
	import DrawHelper from "./helpers/DrawHelper.js";
	import EsriJSON from "ol/format/EsriJSON";
	import LineString from "ol/geom/LineString";
	import Map from "ol/Map";
	import MapHelper from "./helpers/MapHelper";
	import Overlay from "ol/Overlay";
	import Point from "ol/geom/Point";
	import proj4 from "proj4";
	import TileLayer from "ol/layer/Tile";
	import {unByKey} from "ol/Observable";
	import VectorLayer from "ol/layer/VectorImage";
	import VectorLayerStyleHelper from "./helpers/VectorLayerStyleHelper";
	import VectorSource from "ol/source/Vector";
	import View from "ol/View";
	import {createEmpty, extend} from "ol/extent";
	import {Draw, Snap} from "ol/interaction";
	import {getCenter as getExtentCenter} from "ol/extent";
	import {register} from "ol/proj/proj4";
	import {transformExtent} from "ol/proj";
	import {Circle as CircleStyle, Fill, Stroke, Style} from "ol/style";
	import "ol/ol.css";

	export default {
		data: function(){
			var data = {
				loading: true,
				layersLoadingCounter: 0,
				layersFilter: null,
				uploadedFiles: null,
				interactionsCount: 0,
				searchResults: null,
				additionalLayersInfoDict: {},
				pointerMoveHandlersCount: 0
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			activeTaskFeaturesGrouped: { // FIXME! Architektūriškai gan blogai, nes šiaip MyMap turėtų būti abstraktesnis ir nieko apie kažkokius projektus nežinoti...
				get: function(){
					return this.$store.state.activeTaskFeaturesGrouped;
				}
			},
			userData: {
				get: function(){
					return this.$store.state.userData;
				}
			}
		},

		mounted: function(){
			proj4.defs("EPSG:3346", "+proj=tmerc +lat_0=0 +lon_0=24 +k=0.9998 +x_0=500000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs");
			register(proj4);
			var mapViewConfig = {
				projection: "EPSG:" + MapHelper.getProjectionCode(),
				resolutions: MapHelper.getResolutions(this.maxScale),
				enableRotation: false,
				constrainResolution: true,
				smoothResolutionConstraint: false,
				zoomFactor: 8
			};
			this.map = new Map({
				target: this.$refs.map,
				view: new View(mapViewConfig)
			});
			this.drawHelper = new DrawHelper({
				myMap: this,
				$vBus: this.$vBus,
				$store: this.$store
			});
			this.map.getLayers().on("add", function(e){
				this.onLayerAdd(e.element);
			}.bind(this));
			this.map.once("postrender", function(){
				var extent = this.getFullExtent();
				if (extent) {
					this.map.getView().fit(new Point(getExtentCenter(extent)), {
						maxZoom: CommonHelper.streetSignsZoomThreshold + 1
					});
				}
				this.$store.commit("setMyMap", this);
				this.loading = false;
			}.bind(this));
		},

		created: function(){
			this.$vBus.$on("update-map-size", this.updateMapSize);
		},

		beforeDestroy: function(){
			this.$vBus.$off("update-map-size", this.updateMapSize);
		},

		destroyed: function(){
			this.map = this.possibleFeaturesLayer = this.activeFeaturesLayer = null;
			this.$store.commit("setMyMap", null);
		},

		props: {
			maxScale: Number
		},

		methods: {
			getFullExtent: function(){
				var extent = MapHelper.getFullExtent();
				if (extent) {
					extent = transformExtent([extent.xmin, extent.ymin, extent.xmax, extent.ymax], "EPSG:" + extent.spatialReference.wkid, this.map.getView().getProjection().getCode());
				}
				return extent;
			},
			addLayer: function(service, layersGroup){
				var serviceAdditionId;
				if (service.base) {
					serviceAdditionId = this.getBaseServiceAdditionId();
					if (this.baseLayer) {
						if (this.map) {
							this.map.removeLayer(this.baseLayer);
						}
						this.baseLayer = null;
					}
				}
				this.layersLoadingCounter += 1;
				MapHelper.getLayer(service, null, this, this.userData).then(function(layer){
					if (service.base && serviceAdditionId != this.baseServiceAdditionId) {
						// Taip nutinka kai greit einame per MapLayersSwitcher'io radio mygtukus...
						console.log("BAD ID", serviceAdditionId, this.baseServiceAdditionId);
					} else {
						var status;
						if (layer) {
							if (this.map) {
								this.setLayerFilter(layer, this.layersFilter); // Šito reikia jei išjungiame sluoksnį, pritaikome filtrą, įjungiame sluoksnį...
								if (layersGroup) {
									layersGroup.getLayers().push(layer);
								} else {
									this.map.addLayer(layer);
								}
								status = true;
								if (service.base) {
									this.baseLayer = layer;
								}
							}
						} else {
							console.warn("No layer", service);
						}
						if (service.callback) {
							service.callback(status);
						}
					}
					this.layersLoadingCounter -= 1;
				}.bind(this), function(){
					if (service.callback) {
						service.callback(false);
					}
					console.warn("Layer retrieval failed", service);
					this.layersLoadingCounter -= 1;
					if (service.significant) {
						this.$store.commit("setMapServiceAsFailed", service);
					} else {
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Deja, sluoksnis `" + service.title + "` negali būti pakrautas"
						});
					}
				}.bind(this));
			},
			removeLayer: function(service){
				if (service.layer) {
					if (this.map) {
						this.map.removeLayer(service.layer);
					}
				} else {
					console.warn("No layer to remove", service);
				}
			},
			onLayerAdd: function(layer){
				if (layer.getSource) {
					var source = layer.getSource();
					if (layer instanceof TileLayer) {
						source.on("tileloadstart", function(){
							this.layersLoadingCounter += 1;
						}.bind(this));
						source.on("tileloadend", function(){
							this.layersLoadingCounter -= 1;
						}.bind(this));
						source.on("tileloaderror", function(){
							this.layersLoadingCounter -= 1;
						}.bind(this));
					} else {
						source.on("imageloadstart", function(){
							this.layersLoadingCounter += 1;
						}.bind(this));
						source.on("imageloadend", function(){
							this.layersLoadingCounter -= 1;
						}.bind(this));
						source.on("imageloaderror", function(){
							this.layersLoadingCounter -= 1;
						}.bind(this));
					}
				} else {
					if (layer.getLayers) { // Darome prielaidą, kad tai yra LayerGroup'as
						layer.getLayers().forEach(function(l){
							this.onLayerAdd(l);
						}.bind(this));
						layer.getLayers().on("add", function(e){
							this.onLayerAdd(e.element);
						}.bind(this));
					}
				}
			},
			getBaseServiceAdditionId: function(){
				if (!this.baseServiceAdditionId) {
					this.baseServiceAdditionId = 0;
				}
				this.baseServiceAdditionId += 1;
				return this.baseServiceAdditionId;
			},
			zoomToFeatures: function(features, options, emptyExtentCallback){
				if (this.map) {
					if (features.length) {
						var extent = createEmpty(),
							extentNotEmpty = false;
						features.forEach(function(feature){
							if (feature.getGeometry()) {
								extend(extent, feature.getGeometry().getExtent());
								extentNotEmpty = true;
							}
						});
						if (extentNotEmpty) {
							this.map.getView().fit(extent, options);
						} else {
							if (emptyExtentCallback) {
								emptyExtentCallback();
							}
						}
					} else {
						if (emptyExtentCallback) {
							emptyExtentCallback();
						}
					}
				}
			},
			addPointerMoveHandler: function(){
				this.pointerMoveHandlersCount += 1;
				if (!this.helpTooltipElement) {
					this.helpTooltipElement = document.createElement("div");
					this.helpTooltipElement.className = "ol-tooltip hidden";
					this.helpTooltip = new Overlay({
						element: this.helpTooltipElement,
						offset: [15, 0],
						positioning: "center-left",
					});
					this.map.addOverlay(this.helpTooltip);
				}
				this.map.on("pointermove", this.pointerMoveHandler);
			},
			removePointerMoveHandler: function(){
				if (this.pointerMoveHandlersCount) {
					this.pointerMoveHandlersCount -= 1;
				}
				if (!this.pointerMoveHandlersCount) {
					if (this.helpTooltipElement) {
						this.helpTooltipElement.classList.add("d-none");
					}
					this.map.un("pointermove", this.pointerMoveHandler);
				}
			},
			pointerMoveHandler: function(e){
				if (e.dragging) {
					return;
				}
				if (this.helpTooltipElement && this.helpMessage) {
					this.helpTooltipElement.innerHTML = this.helpMessage;
					this.helpTooltip.setPosition(e.coordinate);
					this.helpTooltipElement.classList.remove("d-none");
				}
			},
			getLayerInfo: function(key, layerId){
				var layersInfoDict = this.$store.state.layersInfoDict;
				if (layersInfoDict) {
					var layerIdMeta = CommonHelper.layerIds[key];
					if (layerIdMeta) {
						var v = layersInfoDict[layerIdMeta[0]];
						if (!v) {
							v = this.additionalLayersInfoDict[layerIdMeta[0]];
						}
						if (v) {
							var layerInfo = v[layerIdMeta[1]];
							if (layerInfo) {
								return layerInfo;
							}
						}
					} else {
						if (this.additionalLayersInfoDict[key]) {
							return this.additionalLayersInfoDict[key][layerId];
						}
					}
				}
			},
			getLayerFields: function(key, layerId){
				var fields = [],
					layerInfo = this.getLayerInfo(key, layerId);
				if (layerInfo) {
					fields = layerInfo.fields;
				}
				return fields;
			},
			getLayerField: function(fields, fieldKey){
				var field;
				if (fields) {
					fields.some(function(f){
						if (f.name == fieldKey) {
							field = JSON.parse(JSON.stringify(f));
							if (field.domain && field.domain.codedValues) {
								field.domain.codedValues.forEach(function(codedValue){
									codedValue.code += "";
								});
							}
							return true;
						}
					});
				}
				return field;
			},
			getLayerId: function(key){
				var layerIdMeta = CommonHelper.layerIds[key];
				if (layerIdMeta) {
					return layerIdMeta[1];
				}
			},
			setLayersFilter: function(layersFilter){
				this.layersFilter = layersFilter;
			},
			setUploadedFiles: function(uploadedFiles){
				this.uploadedFiles = uploadedFiles;
			},
			setLayerFilter: function(layer, layersFilter){
				if (layersFilter && layer.service) {
					if (layer.service.id == "street-signs-vertical" || layer.service.id == "street-signs") {
						layer.getLayers().forEach(function(l){
							if (l.getSource) {
								this.setLayerFilterInner(l, layersFilter);
							} else {
								l.getLayers().forEach(function(la){
									if (la.getSource) {
										this.setLayerFilterInner(la, layersFilter);
									}
								}.bind(this));
							}
						}.bind(this));
					}
				}
			},
			setLayerFilterInner: function(layer, layersFilter){
				var oldWhere = layer.where || "",
					newWhere = [];
				if (layer.parentServiceId == CommonHelper.layerIds["verticalStreetSigns"][0] && layer.layerId == CommonHelper.layerIds["verticalStreetSigns"][1]) {
					if (layersFilter["vertical-nr"]) {
						newWhere.push("KET_NR = " + parseInt(layersFilter["vertical-nr"])); // O gal "KET_NR LIKE '%" + layersFilter["vertical-nr"] + "%'"?
					}
					if (layersFilter["vertical-type"]) {
						newWhere.push("TIPAS = '" + layersFilter["vertical-type"] + "'");
					}
				} else if (layer.parentServiceId == "street-signs" && CommonHelper.layerIds["horizontalLayerIds"].indexOf(layer.layerId) != -1) {
					if (layersFilter["horizontal-nr"]) {
						newWhere.push("KET_NR = " + parseInt(layersFilter["horizontal-nr"])); // O gal "KET_NR LIKE '%" + layersFilter["horizontal-nr"] + "%'"?
					}
				} else if (layer.parentServiceId == "street-signs" && CommonHelper.layerIds["otherLayerIds"].indexOf(layer.layerId) != -1) {
					if (layersFilter["other-type"]) {
						newWhere.push("TIPAS = " + parseInt(layersFilter["other-type"]));
					}
				}
				console.log("WHERE", newWhere);
				layer.where = newWhere.join(" AND ");
				var historicMoment = layersFilter["historic-moment"],
					oldHistoricMoment = layer.historicMoment;
				layer.historicMoment = historicMoment;
				if ((oldWhere != layer.where) || (oldHistoricMoment != layer.historicMoment)) {
					layer.getSource().refresh();
				}
			},
			addInteraction: function(interaction){
				this.interactionsCount += 1;
				this.map.addInteraction(interaction);
				this.syncInteractivity();
			},
			removeInteraction: function(interaction){
				this.interactionsCount -= 1;
				this.map.removeInteraction(interaction);
				this.syncInteractivity();
			},
			syncInteractivity: function(){
				if (this.interactivityDisabledTimeout) {
					clearTimeout(this.interactivityDisabledTimeout);
					this.interactivityDisabledTimeout = null;
				}
				if (this.interactionsCount) {
					this.interactivityDisabled = true;
				} else {
					// Jei tai kviečiama PointPanoramaViewer'iui ir panoramos ieškojome click'indami ant kažkokio "feature", tai suveiks žemėlapio singleclick'as!!! Labai blogai! FIXME!!!
					this.interactivityDisabledTimeout = setTimeout(function(){
						this.interactivityDisabled = false;	
					}.bind(this), 300);
				}
			},
			showPointProgress: function(coordinate){
				if (!this.pointProgressOverlay) {
					var div = document.createElement("div");
					div.className = "point-progress";
					this.pointProgressOverlay = new Overlay({
						element: div,
						position: null,
						positioning: "center-center",
						stopEvent: false
					});
					this.map.addOverlay(this.pointProgressOverlay);
				}
				if (this.pointProgressOverlay) {
					this.pointProgressOverlay.setPosition(coordinate);
					this.pointProgressOverlay.element.style.display = "";
				}
			},
			hidePointProgress: function(){
				if (this.pointProgressOverlay) {
					this.pointProgressOverlay.element.style.display = "none";
				}
			},
			findFeature: function(e){
				// Šis metodas pasidarė griozdiškas po `vms-inventorization` atsiradimo... FIXME!
				var promise = new Promise(function(resolve, reject){
					if (e.globalId) {
						var serviceId = "street-signs",
							layerId = parseInt(e.layerId),
							verticalAdvanced,
							customServiceUrl;
						if (e.type == "v") {
							serviceId = "street-signs-vertical";
						} else if ((e.type == "vms-inventorization-l") || (e.type == "vms-inventorization-p")) {
							serviceId = e.type;
							if (!customServiceUrl) {
								reject();
							}
						} else if (e.type == "tasks") {
							customServiceUrl = this.$store.getters.getServiceUrl("tasks");
						} else if (e.type == "task-related" || e.type == "task-related-v") {
							var solutionExists = true;
							// Gal čia dar reiktų kokių modifikacijų? Bet jei reikės, tai vadinasi kažkur logikos spragos...
							// Jo... reikės...
							// Kadangi StreetSignsMap'e padarėme, kad kiekvienas taskSublayer'is turėtų originalaus sluoksnio layerId, čia reikia atsekti aktualųjį užduočių serviso sluoksnio layerId!!!
							if (e.type == "task-related") {
								serviceId = "street-signs";
							} else if (e.type == "task-related-v") {
								serviceId = "street-signs-vertical";
							}
							if (CommonHelper.layerIds.tasksRelated) {
								var layerIdMeta,
									featureType;
								for (var key in CommonHelper.layerIds) {
									layerIdMeta = CommonHelper.layerIds[key];
									if (layerIdMeta[0] == serviceId && layerIdMeta[1] == layerId) {
										featureType = key;
										break;
									}
								}
								if (featureType) {
									layerId = CommonHelper.layerIds.tasksRelated[featureType];
								}
							}
							// -----------------------------------------------
							// Sprendimas komplikuojasi vertikaliųjų KŽ atveju... Ypač jei ieškome užduoties sluoksnyje esančio KŽ, kurio atrama yra ne užduoties sluoksnyje, o originaliame...
							// Dabar logika tokia: kiek galima viską susirenkame iš lokalių užduoties objektų... Ir jei ko trūksta, tai kreipiamės į originalųjį sluoksnį...
							// Viskas būtų paprasčiau, jei su užduotimis susijusių atramų ir vertikaliųjų KŽ sluoksniai būtų "street-signs-vertical" servise...
							// Kas kaltas?
							// Kad užduočių kūrimo logika buvo lipdoma "ant viršaus"... Apie užduotis ir jų objektus reikėjo galvoti iš pat pradžių...
							// ----------------------------------------------
							if (featureType) {
								if (this.$store.state.activeTaskFeatures) {
									if (this.$store.state.activeTaskFeatures[featureType]) {
										var matchedF;
										this.$store.state.activeTaskFeatures[featureType].some(function(f){
											if (f.feature) {
												if (f.feature.get("GlobalID") == "{" + e.globalId + "}") {
													matchedF = f;
													return true;
												}
											}
										});
										if (matchedF) {
											if (["verticalStreetSigns", "verticalStreetSignsSupports"].indexOf(featureType) != -1) {
												// Gali būti sudėtinga dėl šito -> `jei ieškome užduoties sluoksnyje esančio KŽ, kurio atrama yra ne užduoties sluoksnyje, o originaliame...`...
												// ------------------------------------------------------ Experimental, pradžia...
												// Viskas būtų žymiai paprasčiau, jei užduočių sluoksniai būtų ne atskirame servise, o tame pačiame, kaip ir originalūs objektai? Jei taip būtų, tai užtektų naudoti tą pačią /find užklausą, bet tik pakoreguoti sluoksnių id, kuriuose ieškoti?..
												var result;
												if (featureType == "verticalStreetSignsSupports") {
													// Šitas atvejis lengviau realizuojamas...
													result = {
														additional: {
															supports: [matchedF.feature],
															signs: []
														},
														feature: matchedF.feature
													};
													if (this.$store.state.activeTaskFeatures["verticalStreetSigns"]) { // FIXME... Čia pasikartojantis kodas su tuo, kuris yra CommonHelper.getFetchPromise'e...
														this.$store.state.activeTaskFeatures["verticalStreetSigns"].forEach(function(f){
															if (f.feature) {
																if ([matchedF.feature.get("GlobalID"), matchedF.feature.get(CommonHelper.taskFeatureOriginalGlobalIdFieldName)].indexOf(f.feature.get("GUID")) != -1) { // FIXME! Ne'hardcode'inti "GlobalID", "GUID"...
																	result.additional.signs.push(this.mockFindResultFromFeature(f, "verticalStreetSigns"));
																}
															}
														}.bind(this));
													}
												} else if (featureType == "verticalStreetSigns") {
													// Iš pradžių atramos objekto ieškome tarp užduoties objektų? Jei randame — valio. Jei nerandame, tai kreipiamės į originalųjį servisą su /find užklausa?..
													var matchedVerticalStreetSignsSupportFeature;
													if (this.$store.state.activeTaskFeatures["verticalStreetSignsSupports"]) {
														this.$store.state.activeTaskFeatures["verticalStreetSignsSupports"].some(function(f){
															if (f.feature && matchedF.feature) {
																if ([f.feature.get("GlobalID"), f.feature.get(CommonHelper.taskFeatureOriginalGlobalIdFieldName)].indexOf(matchedF.feature.get("GUID")) != -1) {
																	matchedVerticalStreetSignsSupportFeature = f;
																	return true;
																}
															}
														}.bind(this));
													}
													if (matchedVerticalStreetSignsSupportFeature) {
														// Atramos objektą radome... Dabar reikia rasti visus jo kelio ženklus?.. Jie visi gi turėtų būti "vietiniai", objektų masyve...
														result = {
															additional: {
																supports: [matchedVerticalStreetSignsSupportFeature.feature],
																signs: []
															},
															feature: matchedF.feature
														};
														if (this.$store.state.activeTaskFeatures["verticalStreetSigns"]) {
															this.$store.state.activeTaskFeatures["verticalStreetSigns"].forEach(function(f){
																if (f.feature) {
																	if ([matchedVerticalStreetSignsSupportFeature.feature.get("GlobalID"), matchedVerticalStreetSignsSupportFeature.feature.get(CommonHelper.taskFeatureOriginalGlobalIdFieldName)].indexOf(f.feature.get("GUID")) != -1) {
																		result.additional.signs.push(this.mockFindResultFromFeature(f, "verticalStreetSigns"));
																	}
																}
															}.bind(this));
														}
													} else {
														solutionExists = false; // Nes išeiname už "užduoties objekto ribų..."
														// Toliau bandysime pagal turimą atramos GlobalID gauti reikiamą info...
														serviceId = "street-signs-vertical";
														layerId = this.getLayerId("verticalStreetSignsSupports");
														e.matchedF = matchedF;
														e.pseudoFind = {
															type: "v",
															layerId: layerId,
															globalId: CommonHelper.stripGuid(matchedF.feature.get("GUID"))
														};
													}
													// Šis užkomentuotas sprendimas buvo laikinas...
													/*
													if (!matchedVerticalStreetSignsSupportFeature) {
														result = { // Dabar sprendimas laikinas...
															feature: matchedF.feature
														};
													}
													*/
												}
												if (solutionExists) {
													if (result) {
														resolve(result);
													} else {
														reject();
													}
												}
												// ------------------------------------------------------ Experimental, pabaiga...
											} else {
												resolve({
													feature: matchedF.feature
												});
											}
										} else {
											reject();
										}
									} else {
										reject();
									}
								} else {
									// FIXME... Ateityje pakoreguoti taip, kad gautų objektą, pagal jį gautų užduotį... Aktyvuotų užduotį ir aktyvuotų objektą?.. Daugokai reikalų :)
									reject();
									this.$vBus.$emit("show-message", {
										type: "warning",
										message: "Deja, nėra aktyvios užduoties... Objektas negali būti parodytas..."
									});
									CommonHelper.routeTo({
										router: this.$router,
										vBus: this.$vBus
									});
								}
							} else {
								reject();
							}
							if (solutionExists) {
								return;	
							}
						} else if (["vvt", "waze"].indexOf(e.type) != -1) {
							serviceId = e.type;
							customServiceUrl = this.$store.getters.getServiceUrl(e.type);
							if (!customServiceUrl) {
								reject();
							}
						}
						var serviceUrl,
							params;
						if (customServiceUrl) {
							var tokenPromise = new Promise(function(resolve){
								resolve();
							}.bind(this));
							if (["vms-inventorization-l", "vms-inventorization-p", "vvt"].indexOf(e.type) != -1) { // FIXME! Funkcionalumas pasidarė hack'iškas... Reikia pagal tipą gauti serviso objektą ir tikrinti ar jis turi reikšmę "securedWithToken"...
								tokenPromise = CommonHelper.getToken({
									url: customServiceUrl
								});
							}
							customServiceUrl = CommonHelper.prependProxyIfNeeded(customServiceUrl);
							tokenPromise.then(function(token){
								MapHelper.getServiceCapabilities(customServiceUrl + "/layers", "esri", token).then(function(layersInfo){
									var layersInfoDict = {};
									if (layersInfo && layersInfo.content && layersInfo.content.layers) {
										layersInfo.content.layers.forEach(function(layerInfo){
											layersInfoDict[layerInfo.id] = layerInfo;
										});
									}
									this.populateLayersInfoDict(serviceId, layersInfoDict); // Realiai šito prireikia, kai atidarome app turint nuorodą, pvz. http://localhost:3001/admin/?t=vms-inventorization-p&l=0&id=237d9975-6d41-4093-a181-a236b0fcd163
									var layerId4Query = layerId;
									if (e.historic) {
										if (e.type == "vvt") {
											layerId4Query += 14; // FIXME! Ne hardcode'inti...
										}
										customServiceUrl = customServiceUrl.replace("/FeatureServer", "/MapServer");
									}
									serviceUrl = customServiceUrl + "/" + layerId4Query + "/query";
									params = {
										f: "json",
										outFields: "*",
										returnGeometry: true,
										where: "GlobalID = '" + e.globalId + "'",
										outSR: 3346 // FIXME: ne hardcode'inti...
									};
									if (token) {
										params.token = token;
									}
									if (layersInfoDict[layerId]) {
										if (/^\d+$/.test(e.globalId)) {
											params.where = layersInfoDict[layerId].objectIdField + " = " + e.globalId;
										} else {
											params.where = layersInfoDict[layerId].globalIdField + " = '" + e.globalId + "'";
										}
									}
									CommonHelper.getFetchPromise(serviceUrl, function(json){
										var result;
										if (e.historic) {
											result = json.features;
										} else {
											var feature;
											if (json.features && json.features.length == 1) {
												feature = json.features[0];
											}
											if (feature) {
												result = {
													feature: feature
												};
											}
										}
										return result;
									}.bind(this), "POST", params).then(function(result){
										if (result) {
											resolve(result);
										} else {
											reject();
										}
									}.bind(this), function(){
										reject();
									});
								}.bind(this), function(){
									reject();
								});
							}.bind(this), function(){
								reject();
							});
						} else {
							serviceUrl = this.$store.getters.getServiceUrl(serviceId);
							if ((e.type == "task-related" || e.type == "task-related-v") && !e.pseudoFind) {
								serviceUrl = this.$store.getters.getServiceUrl("tasks");
							}
							if (serviceUrl) {
								var verticalStreetSignsSupportsLayerId,
									verticalStreetSignsLayerId;
								if (serviceId == "street-signs-vertical") {
									verticalStreetSignsSupportsLayerId = this.getLayerId("verticalStreetSignsSupports");
									verticalStreetSignsLayerId = this.getLayerId("verticalStreetSigns");
									if ((e.type == "task-related" || e.type == "task-related-v") && !e.pseudoFind) {
										if (CommonHelper.layerIds.tasksRelated) {
											verticalStreetSignsSupportsLayerId = CommonHelper.layerIds.tasksRelated["verticalStreetSignsSupports"];
											verticalStreetSignsLayerId = CommonHelper.layerIds.tasksRelated["verticalStreetSigns"];
										}
									}
									if (layerId == verticalStreetSignsSupportsLayerId) {
										verticalAdvanced = true;
									} else {
										if (e.verticalStreetSignsSupportGlobalId) {
											verticalAdvanced = true;
										}
									}
								}
								if (e.historic || verticalAdvanced) {
									serviceUrl = serviceUrl.replace("/FeatureServer", "/MapServer");
								}
								serviceUrl = CommonHelper.prependProxyIfNeeded(serviceUrl);
								params = {
									f: "json",
									outFields: "*",
									returnGeometry: true
								};
								if (e.historicMoment) {
									params.historicMoment = e.historicMoment;
								}
								if (verticalAdvanced) {
									serviceUrl += "/find";
									params.searchText = "{" + (e.verticalStreetSignsSupportGlobalId || e.globalId) + "}";
									params.searchFields = ["GlobalID", "GUID"].join(","); // FIXME! Imti iš "layerInfo"?!!
									params.layers = [verticalStreetSignsSupportsLayerId, verticalStreetSignsLayerId].join(",");
									params.contains = false;
									params.returnFieldName = true;
									params.returnUnformattedValues = true;
									if (e.pseudoFind) {
										params.searchText = "{" + e.pseudoFind.globalId + "}";
										// FIXME! Dar būtų gan logiška pagal e.pseudoFind'o turinį nurodyti params.layers reikšmę? Ar dar ką nors?.. Teoriškai tai būtų universalesnis sprendimas...
									}
								} else {
									if (Number.isInteger(layerId)) {
										var realLayerId = layerId;
										if (e.historic) {
											if (e.type == "vvt") {
												console.log("VVT?", e);
											} else {
												realLayerId += CommonHelper.historyLayerIdOffset;
											}
										}
										serviceUrl += "/" + realLayerId + "/";
										serviceUrl += "query";
										params.where = "GlobalID = '" + e.globalId + "'"; // Laukas gali vadintis ir kitaip? Nebūtinai "GlobalID"? Šio lauko pavadinimą reikia imti iš "layerInfo"?..
										if (/^\d+$/.test(e.globalId)) {
											var layerInfo = this.myMap.getLayerInfo(CommonHelper.getFeatureType(e));
											if (layerInfo) {
												params.where = layerInfo.objectIdField + " = " + e.globalId; // Sprendimas `Bookmarks'ų" sluoksniui...
											}
										}
									} else {
										reject();
									}
								}
								CommonHelper.getFetchPromise(serviceUrl, function(json){
									var result;
									if (e.historic) {
										result = json.features;
									} else if (verticalAdvanced) {
										if (json.results) {
											result = {
												additional: {
													supports: [],
													signs: []
												}
											};
											var skipFeature;
											json.results.forEach(function(item){
												skipFeature = false;
												if ((serviceId == "street-signs-vertical") && item.attributes && (item.attributes["STATUSAS"] == CommonHelper.verticalStreetSignDestroyedStatusValue) && (item.attributes["PATVIRTINTAS"] == 1) && !item.attributes["ATMESTA"]) {
													// Ženklas yra panaikintas ir patvirtintas... Jį ignoruojame...
													// TODO: grąžinti kažkokiame kitame masyve?..
												} else {
													if (item.layerId == verticalStreetSignsSupportsLayerId) {
														result.additional.supports.push(item);
													} else if (item.layerId == verticalStreetSignsLayerId) {
														if (this.activeTaskFeaturesGrouped) {
															var substituteItem = this.activeTaskFeaturesGrouped[item.attributes["GlobalID"]]; // FIXME! Imti iš "layerInfo"?!!
															if (substituteItem && substituteItem.feature) {
																skipFeature = true;
															}
														}
														if (!skipFeature) {
															result.additional.signs.push(item);
														}
													}
													if (item.layerId == layerId && CommonHelper.stripGuid(item.attributes["GlobalID"]) == e.globalId) { // FIXME! Imti iš "layerInfo"?!!
														result.feature = item;
													}
												}
											}.bind(this));
											if (e.matchedF) {
												result.feature = e.matchedF.feature; // Klaida, jei šis nurodomas?..
											}
											if (this.$store.state.activeTaskFeatures) {
												// Čia dar įdomi situacija! Jei tarkime prie originalios atramos užduotyje tiesiog pridedame naują KŽ...
												// Tai tą KŽ reikia įkišti į result.additional.signs!
												if (result.additional.supports && result.additional.supports.length && result.additional.supports.length == 1) {
													var supportFeatureGlobalId = result.additional.supports[0].attributes["GlobalID"]; // FIXME! Ne hardcode'inti reikšmės...
													if (this.$store.state.activeTaskFeatures["verticalStreetSigns"]) {
														this.$store.state.activeTaskFeatures["verticalStreetSigns"].forEach(function(activeTaskFeature){
															if (activeTaskFeature.feature) {
																if (activeTaskFeature.feature.get("GUID") == supportFeatureGlobalId) {
																	// Jei taip yra, vadinasi su užduotimi susijęs vert. KŽ yra susijęs su šia atrama...
																	result.additional.signs.push(this.mockFindResultFromFeature(activeTaskFeature, "verticalStreetSigns"));
																}
															}
														}.bind(this));
													}
												}
											}
											if (result.feature) {
												var key = "PADETIS_NUO_VIRSAUS"; // FIXME! Imti iš "layerInfo"?!!
												result.additional.signs.sort(function(a, b){
													if (a.attributes[key] < b.attributes[key]) {
														return -1;
													}
													if (a.attributes[key] > b.attributes[key]) {
														return 1;
													}
													return 0;
												});
												result.results = json.results;
											} else {
												result = null;
											}
										}
									} else {
										var feature;
										if (json.features && json.features.length == 1) {
											feature = json.features[0];
											if (serviceId == "street-signs-vertical") {
												if (feature && feature.attributes && (feature.attributes["STATUSAS"] == CommonHelper.verticalStreetSignDestroyedStatusValue) && (feature.attributes["PATVIRTINTAS"] == 1) && !feature.attributes["ATMESTA"]) {
													// Aktualu pvz. jei turime panaikinto objekto nuorodą: http://localhost:3001/admin/?t=v&l=0&id=56BE31C3-9A41-4952-83BE-3B84E5BA1988
													// Dabar apsimetame, kad toks objektas neegzistuoja...
													console.warn("Objektas panaikintas... Iš kur nuoroda?");
													feature = null;
												}
											}
										}
										if (feature) {
											result = {
												feature: feature
											};
										}
									}
									return result;
								}.bind(this), "POST", params).then(function(result){
									if (result) {
										if (serviceId == "street-signs-vertical") {
											if (verticalAdvanced) {
												resolve(result);
											} else {
												if (result.feature && (layerId == verticalStreetSignsLayerId)) {
													var verticalStreetSignsSupportGlobalId = CommonHelper.stripGuid(result.feature.attributes["GUID"]); // FIXME! Imti iš "layerInfo"?!!
													if (verticalStreetSignsSupportGlobalId) {
														var newE = {
															type: "v",
															layerId: layerId,
															globalId: e.globalId,
															verticalStreetSignsSupportGlobalId: verticalStreetSignsSupportGlobalId,
															historicMoment: e.historicMoment
														};
														this.findFeature(newE).then(function(result){
															resolve(result);
														}.bind(this), function(){
															reject();
														}.bind(this));
													} else {
														reject();
													}
												} else {
													reject();
												}
											}
										} else {
											resolve(result);
										}
									} else {
										reject();
									}
								}.bind(this), function(){
									reject();
								});
							} else {
								reject();
							}
						}
					} else if (e.service && e.coordinates) {
						var queryUrl = e.service.url;
						if (queryUrl && e.service.showLayers) {
							// Juodųjų dėmių sluoksnio atveju tokio gali prireikti...
							/*
							MapHelper.getServiceCapabilities(queryUrl + "/layers", "esri").then(function(layersInfo){
								console.log("LAYERS INFO...", layersInfo);
							});
							*/
							queryUrl += "/identify";
							var sourceParamsLayers;
							if (e.service.layer && e.service.layer.getSource()) {
								var sourceParams = e.service.layer.getSource().getParams();
								if (sourceParams) {
									sourceParamsLayers = sourceParams.layers;
									if (sourceParamsLayers) {
										sourceParamsLayers = sourceParamsLayers.replace("show:", "visible:");
									}
								}
							}
							params = this.getIdentifyParams(e.coordinates, sourceParamsLayers || ("visible:" + e.service.showLayers.join(",")), e.service);
							params.returnUnformattedValues = false;
							params.returnFieldName = false;
							CommonHelper.getFetchPromise(queryUrl, function(json){
								if (json) {
									if (json.error) {
										// ...
									} else {
										return json.results;
									}
								}
							}.bind(this), "POST", params).then(function(result){
								if (result) {
									resolve(result);
								} else {
									reject();
								}
							}.bind(this), function(){
								reject();
							});
						} else {
							reject();
						}
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			},
			getFieldTitle: function(field){
				var title = field.alias || field.name;
				var titles = {
					"Patvirtinimas": "Patvirtintas",
					"Plotas": "Plotis",
					"Editorius": "Objektą redagavo",
					"last_edited_user": "Objektą redagavo (sisteminis)",
					"PAV": "Gatvės pavadinimas",
					"L_nusidevejimas": "Linijų nusidėvėjimas",
					"S_nusidevejimas": "Simbolių nusidėvėjimas",
					"korys": "Korys",
					"L_Nusidevejimas": "Linijų nusidėvėjimas",
					"Nusidevėjimas": "Nusidėvėjimas"
				};
				return titles[title] || title;
			},
			getValueItems: function(field, value, dateFieldsTimeReference){
				var valueStr,
					valuePretty = value;
				if (field) {
					if (field.domain && field.domain.codedValues) {
						valueStr = null;
						if (value || (value === 0)) {
							valueStr = value + "";
						}
						field.domain.codedValues.some(function(codedValue){
							if (codedValue.code == valueStr) {
								valuePretty = codedValue.name;
								return true;
							}
						});
					} else if (field.type == "esriFieldTypeDouble") {
						if (valuePretty) {
							valuePretty = valuePretty.toFixed(2);
						}
					} else if (field.type == "esriFieldTypeDate") {
						valuePretty = CommonHelper.getPrettyDate(value, true, dateFieldsTimeReference);
					}
				}
				if (valuePretty !== 0) {
					if (!valuePretty || (valuePretty == " ")) {
						valuePretty = "—";
					}
				}
				var valueItems = {
					value: value,
					valuePretty: valuePretty,
					rawField: field // Reikalinga redagavimo režimui...
				};
				return valueItems;
			},
			setLayerForFeatureByFeatureType: function(feature, featureType){
				if (featureType == "vvt") {
					this.setLayerForFeature(feature, "vvt");
				} else {
					var layerIdMeta = CommonHelper.layerIds[featureType];
					if (layerIdMeta) {
						feature.layerId = layerIdMeta[1];
						this.setLayerForFeature(feature, layerIdMeta[0]);
					}
				}
			},
			setLayerForFeature: function(feature, serviceId){
				this.$store.state.myMap.map.getLayers().forEach(function(layer){
					if (layer.service) {
						if (layer.service.id == serviceId && layer.getLayers) {
							layer.getLayers().forEach(function(l){
								if (feature.layerId == l.layerId) {
									feature.layer = l;
								}
							}.bind(this));
						}
					}
				}.bind(this));
				if (feature.isTasksRelated) {
					// Jei turime reikalų su objektu, kuris susijęs su užduotimi, randame specifinį jo sluoksnį...
					this.$store.state.myMap.map.getLayers().forEach(function(layer){
						if (layer.isTasksRelatedLayer) {
							if (layer.getLayers) {
								layer.getLayers().forEach(function(l){
									if (feature.layerId == l.layerId) {
										feature.layer = l;
									}
								}.bind(this));
							}
						}
					}.bind(this));
				}
			},
			createDrawInteraction: function(e){
				var type,
					drawInteraction;
				if (e && e.template) {
					var types = {
						"esriFeatureEditToolPoint": "Point",
						"esriFeatureEditToolLine": "LineString",
						"esriFeatureEditToolPolygon": "Polygon"
					};
					type = types[e.template.drawingTool];
				}
				if (type) {
					if (!this.drawLayer) {
						this.drawLayer = new VectorLayer({
							source: new VectorSource(),
							zIndex: 999
						});
						this.map.addLayer(this.drawLayer);
					}
					var layerIdMeta = CommonHelper.layerIds[e.featureType];
					if (layerIdMeta) {
						this.drawLayer.setStyle(VectorLayerStyleHelper.getVectorLayerStyle({id: layerIdMeta[0]}, this.getLayerInfo(e.featureType) || {}));
					}
					drawInteraction = new Draw({
						type: type,
						source: this.drawLayer.getSource(),
						stopClick: true
					});
					this.addInteraction(drawInteraction);
					var snapInteractionNeeded = this.isSnapInteractionNeeded(e);
					if (snapInteractionNeeded) {
						this.addSnapInteraction(snapInteractionNeeded);
					}
					this.addMeasurementInteractionIfNeeded(type, drawInteraction, "draw");
				}
				return drawInteraction;
			},
			populateLayersInfoDict: function(serviceId, layersInfoDict){
				if (serviceId) {
					if (["vms-inventorization-l", "vms-inventorization-p", "vvt", "waze"].indexOf(serviceId) != -1) {
						if (!this.additionalLayersInfoDict[serviceId]) {
							this.additionalLayersInfoDict[serviceId] = layersInfoDict;
						}
					} else if (serviceId == "tasks") {
						if (!this.additionalLayersInfoDict[serviceId]) {
							for (var key in layersInfoDict) {
								MapHelper.modDateFields(layersInfoDict[key]);
							}
							this.additionalLayersInfoDict[serviceId] = layersInfoDict;
						}
					}
				}
			},
			getZones: function(){
				var promise = new Promise(function(resolve, reject){
					if (this.zonesFeatures) {
						resolve(this.zonesFeatures);
					} else {
						var mainServices = MapHelper.getMainServices();
						if (mainServices && mainServices.optional) {
							var zonesService;
							mainServices.optional.some(function(service){
								if (service.id == "zones") {
									zonesService = service;
									return true;
								}
							});
							if (zonesService && zonesService.showLayers) {
								var url = CommonHelper.prependProxyIfNeeded(zonesService.url + "/" + zonesService.showLayers[0] + "/query");
								var params = {
									f: "json",
									returnGeometry: true,
									outFields: "*",
									where: "1=1",
									outSR: 3346 // FIXME: ne hardcode'inti...
								};
								CommonHelper.getFetchPromise(url, function(json){
									if (json.features) {
										json.features.forEach(function(feature){
											feature.geometry.spatialReference = json.spatialReference;
										});
										this.zonesFeatures = json.features;
										return json.features;
									}
								}.bind(this), "POST", params).then(function(result){
									resolve(result);
								}, function(){
									reject();
								});
							} else {
								reject();
							}
						} else {
							reject();
						}
					}
				}.bind(this));
				return promise;
			},
			mockFindResultFromFeature: function(activeTaskFeature, featureType){
				var esriJsonFormat = new EsriJSON();
				var f = esriJsonFormat.writeFeatureObject(activeTaskFeature.feature, {
					featureProjection: this.map.getView().getProjection()
				});
				f.isTasksRelated = true;
				// Hmmmm... Čia buvau prikabinęs layerId, kuris yra task'o servise, bet labiau prireikė originalaus sluoksnio atitikmens layerId!
				/*
				if (CommonHelper.layerIds.tasksRelated) {
					// FIXME! O gal tai anksčiau jau turėjo būti nustatyta?..
					f.layerId = CommonHelper.layerIds.tasksRelated[featureType];
				}
				*/
				var layerIdMeta = CommonHelper.layerIds[featureType];
				if (layerIdMeta) {
					f.layerId = layerIdMeta[1];
				}
				return f;
			},
			addSnapInteraction: function(featureType){
				if (featureType) {
					if (!this.snapInteractions) {
						var layerIdMeta = CommonHelper.layerIds[featureType];
						if (layerIdMeta) {
							this.snapInteractions = [];
							var source;
							this.map.getLayers().forEach(function(layer){
								if (layer.service && (layer.service.id == layerIdMeta[0])) {
									if (layer.getLayers) {
										layer.getLayers().forEach(function(l){
											if (l.layerId == layerIdMeta[1]) {
												source = l.getSource();
												if (source) {
													var snapInteraction = new Snap({
														source: source
													});
													this.map.addInteraction(snapInteraction);
													this.snapInteractions.push(snapInteraction);
												}
											}
										}.bind(this));
									}
								}
							}.bind(this));
						}
					}
				}
			},
			removeSnapInteraction: function(){
				if (this.snapInteractions) {
					this.snapInteractions.forEach(function(snapInteraction){
						this.map.removeInteraction(snapInteraction);
					}.bind(this));
					this.snapInteractions = null;
				}
			},
			isSnapInteractionNeeded: function(e, editing){
				var snapInteractionNeeded = false,
					snapInteractionFeatureType = "horizontalPolylines";
				if (e.featureType == "horizontalPolylines") {
					snapInteractionNeeded = true;
				} else if (e.featureType == "otherPolygons") {
					var type;
					if (editing) {
						if (e.featureDescr && e.featureDescr.attributes) {
							type = e.featureDescr.attributes["TIPAS"]; // FIXME: ne'hardcode'inti lauko pavadinimo...
						}
					} else {
						type = e.id;
					}
					if (e.isNew) {
						snapInteractionNeeded = true; // Kažko tai nesuveikia... FIXME!..
					} else {
						if (type == 2) { // FIXME: ne'hardcode'inti reikšmės...
							snapInteractionNeeded = true;
							snapInteractionFeatureType = "otherPolygons";
						} else if (type == 3) { // FIXME: ne'hardcode'inti reikšmės...
							snapInteractionNeeded = true;
							snapInteractionFeatureType = "horizontalPolylines";
						}
					}
				}
				if (snapInteractionNeeded) {
					snapInteractionNeeded = snapInteractionFeatureType;
				}
				return snapInteractionNeeded;
			},
			addMeasurementInteractionIfNeeded: function(geometryType, interaction, eventType){
				if (geometryType == "LineString") {
					var sketch,
						listener;
					interaction.on(eventType + "start", function(evt){
						sketch = evt.feature;
						if (eventType == "modify") {
							if (evt.features) {
								sketch = evt.features.getArray()[0];
							}
						}
						if (sketch) {
							var tooltipCoord = evt.coordinate;
							listener = sketch.getGeometry().on("change", function(evt){
								var geom = evt.target,
									output;
								if (geom instanceof LineString) {
									output = CommonHelper.formatLength(geom);
									tooltipCoord = geom.getLastCoordinate(); // Modifikuojant geometriją geriau būtų imti dabartinę koordinatę?..
									if (this.measureTooltipElement) {
										this.measureTooltipElement.innerHTML = output;
									}
									if (this.measureTooltip) {
										this.measureTooltip.setPosition(tooltipCoord);
									}
								}
							}.bind(this));
						}
					}.bind(this));
					interaction.on(eventType + "end", function(){
						sketch = null;
						unByKey(listener);
					}.bind(this));
					this.createMeasureTooltip();
				}
			},
			createMeasureTooltip: function(){
				if (this.measureTooltipElement) {
					this.measureTooltipElement.parentNode.removeChild(this.measureTooltipElement);
				}
				this.measureTooltipElement = document.createElement("div");
				this.measureTooltipElement.className = "ol-tooltip ol-tooltip-measure";
				this.measureTooltip = new Overlay({
					element: this.measureTooltipElement,
					offset: [0, -15],
					positioning: "bottom-center",
					stopEvent: false
				});
				this.myMap.map.addOverlay(this.measureTooltip);
			},
			removeMeasureTooltip: function(){
				if (this.measureTooltip) {
					this.myMap.map.removeOverlay(this.measureTooltip);
					this.measureTooltip = null;
				}
			},
			updateMapSize: function(){
				if (this.map) {
					this.map.updateSize();
				}
			},
			getIdentifyParams: function(coordinate, layers, service){
				var params = {
					f: "json",
					tolerance: 5,
					geometry: coordinate.join(","),
					geometryType: "esriGeometryPoint",
					sr: MapHelper.getProjectionCode(),
					mapExtent: this.myMap.map.getView().calculateExtent(),
					imageDisplay: this.myMap.map.getSize(),
					returnFieldName: true,
					returnUnformattedValues: true,
					layers: layers
				};
				if (service && service.token) {
					params.token = service.token;
				}
				return params;
			}
		},

		watch: {
			layersFilter: {
				handler: function(layersFilter){
					this.myMap.map.getLayers().forEach(function(layer){
						this.setLayerFilter(layer, layersFilter);
					}.bind(this));
				}
			},
			searchResults: {
				handler: function(searchResults){
					if (this.searchResultsVectorLayer) {
						this.searchResultsVectorLayer.getSource().clear(true);
					}
					if (searchResults) {
						// TODO, FIXME! Kai daug objektų, tai labai lėtas render'inimas gaunasi??... Kai daug objektų, atsisakyti dinaminio stiliaus? Gal tai padėtų???
						if (!this.searchResultsVectorLayer) {
							var color = "#00ff00";
							this.searchResultsVectorLayer = new VectorLayer({
								source: new VectorSource(),
								zIndex: 40,
								style: function(feature, resolution){
									var lineWidth = 6,
										radius = 10;
									lineWidth = lineWidth / resolution / 16;
									if (resolution > 0.2) {
										lineWidth = 3;
										radius = 5;
									}
									return [
										new Style({
											stroke: new Stroke({
												width: lineWidth,
												color: color,
												lineCap: "butt"
											}),
											image: new CircleStyle({
												radius: radius,
												fill: new Fill({
													color: color
												})
											})
										})
									];
								}.bind(this)
							});
							this.searchResultsVectorLayer.id = "search-results";
							this.map.addLayer(this.searchResultsVectorLayer);
						}
						var esriJsonFormat = new EsriJSON();
						if (searchResults.layers) {
							searchResults.layers.forEach(function(layer){
								this.searchResultsVectorLayer.getSource().addFeatures(esriJsonFormat.readFeatures(layer));
							}.bind(this));
						} else {
							this.searchResultsVectorLayer.getSource().addFeatures(esriJsonFormat.readFeatures(searchResults));
						}
					}
				}
			}
		}
	}
</script>

<style scoped>
	.map-cont {
		width: 100%;
		height: 100%;
		overflow: hidden; /* Be šito IE lygtais akimirkai pasirodo vertikalus scrollbar'as? */
	}
	.layer-loading-indicator {
		position: absolute;
		z-index: 1;
	}
</style>