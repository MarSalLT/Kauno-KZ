<template>
	<div class="wrapper d-flex">
		<template v-if="loading">
			<v-progress-circular
				indeterminate
				color="primary"
				:size="40"
				width="2"
				class="ma-3"
			></v-progress-circular>
		</template>
		<template v-else>
			<template v-if="layersInfoDict">
				<template v-if="layersInfoDict == 'error'">
					<div>
						<v-alert
							dense
							type="error"
							class="d-inline-block ma-2"
						>
							<div>Atsiprašome, šiuo metu kelio ženklų duomenys nepasiekiami...</div>
							<div class="mt-2">
								<v-btn
									color="error darken-1"
									class="body-2"
									small
									tile
									v-on:click="getMainServicesData"
								>
									<v-icon left size="18">mdi-repeat</v-icon> Bandyti iš naujo
								</v-btn>
							</div>
						</v-alert>
					</div>
				</template>
				<template v-else>
					<div v-if="!$store.state.mapLayersListInMenu" class="left-side-wrapper" ref="leftSideWrapper">
						<div class="layers-list-wrapper">
							<MapLayersList />
						</div>
						<div class="collapse d-none" v-on:click="toggleLeftSideWrapper"></div>
					</div>
					<div class="map-wrapper">
						<MyMap
							:maxScale="100"
						/>
						<div id="map-tooltip" ref="tooltip" class="pa-1 body-2"></div>
						<div class="top-start d-flex flex-column stop-event">
							<div class="d-flex stop-event">
								<MapLayersSwitcher v-if="$store.state.mapLayersListInMenu" />
								<StreetSignsFilter :class="$store.state.mapLayersListInMenu ? 'ml-2' : null" v-if="!basicViewer" />
								<StreetSignsSearch class="ml-2" v-if="!basicViewer" />
								<TasksSearch class="ml-2" v-if="!basicViewer && canHandleTasks" />
								<StatsGenerator class="ml-2" v-if="!basicViewer" />
								<StreetSignsReloader class="ml-2" v-if="!basicViewer" />
								<MapLayersLegend class="ml-2" />
								<AddressSearch class="ml-2" />
								<MapExporter class="ml-2" />
								<PointPanoramaViewer class="ml-2" />
								<PaintedAreaSummary class="ml-2" v-if="!basicViewer" />
								<FileUpload class="ml-2" v-if="!basicViewer" />
								<UserPointsManager class="ml-2" v-if="!basicViewer" />
								<MeasurementButtons class="ml-2" />
								<MapExporter4ActiveTask class="ml-2" />
							</div>
							<div class="flex-grow-1 mt-1 stop-event buttons-widgets-container">
								<StreetSignsFilterContent />
								<StreetSignsSearchContent />
								<MapLayersLegendContent />
								<TasksSearchContent />
								<StatsGeneratorContent />
								<PointPanoramaViewerSettings />
								<PaintedAreaSummarySettings />
								<FileUploadContent />
								<UserPointsManagerContent />
							</div>
						</div>
						<div class="top-end pa-1 d-flex flex-column align-end stop-event">
							<FeaturePopup />
							<NewFeatureDrawingManager v-if="!basicViewer" />
						</div>
						<div class="bottom-start stop-event align-end d-flex">
							<PointPanoramaViewerContent class="mr-1" />
							<NewTaskInitiator v-if="!basicViewer" />
						</div>
						<FeatureHistoryDialog />
						<ConfirmationDialog />
						<FeaturePhotosManagerDialog />
						<StreetSignSymbolCreatorDialog />
						<StreetSignSymbolElementItemSelector />
						<StreetSignSymbolElementSettings />
						<TaskAttachmentEditorDialog />
						<FeatureOverlay />
						<ContextMenu />
					</div>
				</template>
			</template>
			<template v-else>
				<v-alert
					dense
					type="error"
					class="d-inline-block ma-2"
				>
					Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
				</v-alert>
			</template>
		</template>
	</div>
</template>

<script>
	import AddressSearch from "./over-map/AddressSearch";
	import Circle from "ol/geom/Circle";
	import Collection from "ol/Collection";
	import CommonHelper from "./helpers/CommonHelper";
	import ConfirmationDialog from "./dialogs/ConfirmationDialog";
	import ContextMenu from "./ContextMenu";
	import Feature from "ol/Feature";
	import FeatureHistoryDialog from "./dialogs/FeatureHistoryDialog";
	import FeatureOverlay from "./FeatureOverlay";
	import FeaturePhotosManagerDialog from "./dialogs/FeaturePhotosManagerDialog";
	import FeaturePopup from "./FeaturePopup";
	import FileUpload from "./over-map/FileUpload";
	import FileUploadContent from "./over-map/FileUploadContent";
	import GeometryCollection from "ol/geom/GeometryCollection";
	import LayerGroup from "ol/layer/Group";
	import LineString from "ol/geom/LineString";
	import MapExporter from "./over-map/MapExporter";
	import MapExporter4ActiveTask from "./over-map/MapExporter4ActiveTask";
	import MapHelper from "./helpers/MapHelper";
	import MapLayersLegend from "./over-map/MapLayersLegend";
	import MapLayersLegendContent from "./over-map/MapLayersLegendContent";
	import MapLayersList from "./MapLayersList";
	import MapLayersSwitcher from "./over-map/MapLayersSwitcher";
	import MeasurementButtons from "./over-map/MeasurementButtons";
	import MultiPoint from "ol/geom/MultiPoint";
	import MyMap from "./MyMap";
	import NewFeatureDrawingManager from "./NewFeatureDrawingManager";
	import NewTaskInitiator from "./NewTaskInitiator";
	import PaintedAreaSummary from "./over-map/PaintedAreaSummary";
	import PaintedAreaSummarySettings from "./over-map/PaintedAreaSummarySettings";
	import Point from "ol/geom/Point";
	import PointPanoramaViewer from "./over-map/PointPanoramaViewer";
	import PointPanoramaViewerContent from "./over-map/PointPanoramaViewerContent";
	import PointPanoramaViewerSettings from "./over-map/PointPanoramaViewerSettings";
	import StatsGenerator from "./over-map/StatsGenerator";
	import StatsGeneratorContent from "./over-map/StatsGeneratorContent";
	import StreetSignSymbolCreatorDialog from "./dialogs/StreetSignSymbolCreatorDialog";
	import StreetSignSymbolElementItemSelector from "./sc/StreetSignSymbolElementItemSelector";
	import StreetSignSymbolElementSettings from "./sc/StreetSignSymbolElementSettings";
	import StreetSignsFilter from "./over-map/StreetSignsFilter";
	import StreetSignsFilterContent from "./over-map/StreetSignsFilterContent";
	import StreetSignsReloader from "./over-map/StreetSignsReloader";
	import StreetSignsSearch from "./over-map/StreetSignsSearch";
	import StreetSignsSearchContent from "./over-map/StreetSignsSearchContent";
	import TaskAttachmentEditorDialog from "./dialogs/TaskAttachmentEditorDialog";
	import TaskHelper from "./helpers/TaskHelper";
	import TasksSearch from "./over-map/TasksSearch";
	import TasksSearchContent from "./over-map/TasksSearchContent";
	import UserPointsManager from "./over-map/UserPointsManager";
	import UserPointsManagerContent from "./over-map/UserPointsManagerContent";
	import VectorImageLayer from "ol/layer/VectorImage";
	import VectorLayer from "ol/layer/Vector";
	import VectorLayerStyleHelper from "./helpers/VectorLayerStyleHelper";
	import VectorSource from "ol/source/Vector";
	import Vue from "vue";
	import {Circle as CircleStyle, Fill, Stroke, Style} from "ol/style";

	export default {
		data: function(){
			var data = {
				loading: true,
				layersInfoDict: null,
				minZoomLevel4Interaction: CommonHelper.pointsZoomThreshold + 1, // T. y. vos tik pradedame matyti taškinius objektus, aktyvuojame interaktyvumą...
				highlightedFeature: null,
				basicViewer: false,
				canHandleTasks: false
			};
			if (this.$store.state.userData) {
				if (this.$store.state.userData.role == "street-viewer") {
					data.basicViewer = true;
				}
				if (this.$store.state.userData.permissions) {
					if ((this.$store.state.userData.permissions.indexOf("manage-tasks") != -1) || (this.$store.state.userData.permissions.indexOf("manage-tasks-test") != -1)) { // TODO: tik "approve"...
						data.canHandleTasks = true;
					}
				}
			}
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			userData: {
				get: function(){
					return this.$store.state.userData;
				}
			},
			activeTaskFeatures: {
				get: function(){
					return this.$store.state.activeTaskFeatures;
				}
			}
		},

		components: {
			AddressSearch,
			ConfirmationDialog,
			ContextMenu,
			FeatureHistoryDialog,
			FeaturePhotosManagerDialog,
			FeatureOverlay,
			FeaturePopup,
			FileUpload,
			FileUploadContent,
			MapExporter,
			MapExporter4ActiveTask,
			MapLayersLegend,
			MapLayersLegendContent,
			MapLayersList,
			MapLayersSwitcher,
			MeasurementButtons,
			MyMap,
			NewFeatureDrawingManager,
			NewTaskInitiator,
			PaintedAreaSummary,
			PaintedAreaSummarySettings,
			PointPanoramaViewer,
			PointPanoramaViewerContent,
			PointPanoramaViewerSettings,
			StatsGenerator,
			StatsGeneratorContent,
			StreetSignSymbolElementItemSelector,
			StreetSignSymbolElementSettings,
			StreetSignsFilter,
			StreetSignsFilterContent,
			StreetSignsReloader,
			StreetSignsSearch,
			StreetSignsSearchContent,
			StreetSignSymbolCreatorDialog,
			TaskAttachmentEditorDialog,
			TasksSearch,
			TasksSearchContent,
			UserPointsManager,
			UserPointsManagerContent
		},

		mounted: function(){
			this.getMainServicesData();
		},

		created: function(){
			this.$vBus.$on("redraw-active-feature-additional-features", this.redrawActiveFeatureAdditionalFeatures);
			this.$vBus.$on("draw-temp-circles", this.drawTempCircles);
		},

		beforeDestroy: function(){
			this.$vBus.$off("redraw-active-feature-additional-features", this.redrawActiveFeatureAdditionalFeatures);
			this.$vBus.$off("draw-temp-circles", this.drawTempCircles);
		},

		methods: {
			getMainServicesData: function(){
				if (this.userData && this.userData["street-signs-service-root"] && this.userData["vertical-street-signs-service-root"]) {
					this.loading = true;
					var promises = [];
					promises.push(MapHelper.getServiceCapabilities(CommonHelper.prependProxyIfNeeded(this.userData["street-signs-service-root"] + "FeatureServer/layers"), "esri"));
					if (this.userData["street-signs-service-root"] != this.userData["vertical-street-signs-service-root"]) {
						promises.push(MapHelper.getServiceCapabilities(CommonHelper.prependProxyIfNeeded(this.userData["vertical-street-signs-service-root"] + "FeatureServer/layers"), "esri"));
					}
					promises = Promise.allSettled(promises);
					promises.then(function(values){
						var allFulfilled = true,
							content,
							layersInfoDict = {},
							layerInfoDict = {};
						values.forEach(function(v, i){
							if (v.status == "fulfilled") {
								content = v.value.content;
								if (content && content.layers) {
									content.layers.forEach(function(layer){
										layerInfoDict[layer.id] = layer;
									});
								}
								if (i == 0) {
									layersInfoDict["street-signs"] = layerInfoDict;
								} else if (i == 1) {
									layersInfoDict["street-signs-vertical"] = layerInfoDict;
								}
							} else {
								allFulfilled = false;
							}
						});
						if (allFulfilled) {
							this.loading = false;
							if (!layersInfoDict["street-signs-vertical"]) {
								layersInfoDict["street-signs-vertical"] = layersInfoDict["street-signs"];
							}
							this.modLayersInfoDict(layersInfoDict);
							this.$store.commit("setLayersInfoDict", layersInfoDict);
							this.layersInfoDict = layersInfoDict;
						} else {
							this.loading = false;
							this.layersInfoDict = "error";
						}
					}.bind(this), function(){
						this.loading = false;
						this.layersInfoDict = "error";
					}.bind(this));
				} else {
					this.loading = false;
				}
			},
			modLayersInfoDict: function(layersInfoDict){
				var layerIdMeta,
					layerInfo;
				["horizontalPolylines", "horizontalPolygons"].forEach(function(key){
					layerIdMeta = CommonHelper.layerIds[key];
					if (layerIdMeta) {
						if (layersInfoDict[layerIdMeta[0]]) {
							layerInfo = layersInfoDict[layerIdMeta[0]][layerIdMeta[1]];
							if (layerInfo) {
								MapHelper.modDateFields(layerInfo);
								if (layerInfo.types && layerInfo.types.length) {
									var renderer = layerInfo.drawingInfo.renderer;
									if (renderer.type == "uniqueValue") {
										renderer.uniqueValueInfos.forEach(function(uniqueValueInfo){
											this.modSymbol(uniqueValueInfo.symbol, uniqueValueInfo.value, key);
										}.bind(this));
									}
								}
							}
						}
					}
				}.bind(this));
				layerIdMeta = CommonHelper.layerIds["verticalStreetSigns"];
				if (layerIdMeta) {
					if (layersInfoDict[layerIdMeta[0]]) {
						layerInfo = layersInfoDict[layerIdMeta[0]][layerIdMeta[1]];
						if (layerInfo) {
							MapHelper.modDateFields(layerInfo);
							if (layerInfo.fields) {
								layerInfo.fields.some(function(field){
									if (field.name == "ZENKLU_GRUPE") {
										if (field.domain) {
											if (field.domain.codedValues) {
												field.domain.codedValues.push({
													code: 5,
													name: "Grupės patikimai nustatyti pagal ženklo dydį nepavyko"
												});
												field.domain.codedValues.push({
													code: 6,
													name: "Ženklui nėra įstatyme numatytos grupės pagal ženklo dydį"
												});
											}
										}
										return true;
									}
								});
							}
						}
					}
				}
			},
			modSymbol: function(symbol, value, layerKey){
				if (layerKey == "horizontalPolygons" && value == 133) {
					// Skip'iname...
				} else {
					symbol.altSrc = require("@/assets/alt-map-symbols/" + layerKey + "/" + value + ".gif");
				}
			},
			makeMyMapInteractive: function(){
				this.addPointerMoveHandler();
				this.addClickHandler();
				this.createContextMenu();
			},
			addPointerMoveHandler: function(){
				this.highlightedFeatureVectorLayer = this.getHighlightedFeatureVectorLayer(null, null, null, 1001);
				this.myMap.map.addLayer(this.highlightedFeatureVectorLayer);
				this.myMap.map.on("pointermove", function(e){
					if (e.originalEvent.target && e.originalEvent.target.tagName && (e.originalEvent.target.tagName.toUpperCase() == "CANVAS")) {
						if (!this.myMap.interactivityDisabled) {
							var currentZoomLevel = this.myMap.map.getView().getZoom();
							var r;
							if (!e.dragging) {
								var features = this.getFeatures(e.pixel, currentZoomLevel);
								r = features[0];
							}
							if (r != this.highlightedFeature) {
								this.highlightedFeature = r;
							}
							if (r) {
								if (Number.isInteger(currentZoomLevel)) {
									this.showTooltip(e.pixel, r);
								}
							} else {
								this.hideTooltip();
							}
						}
					}
				}.bind(this));
			},
			addClickHandler: function(){
				this.myMap.map.on("singleclick", function(e){
					if (!this.myMap.interactivityDisabled) {
						var currentZoomLevel = this.myMap.map.getView().getZoom(),
							features = this.getFeatures(e.pixel, currentZoomLevel, true),
							feature = features[0];
						if (feature && feature.layer) {
							this.$store.commit("setSingleClickFeatures", null);
							if (feature.layer.isTasksRelatedLayer) {
								this.$store.commit("setActiveFeaturePreview", feature);
								CommonHelper.routeTo({
									router: this.$router,
									vBus: this.$vBus,
									feature: feature
								});
							} else {
								this.$store.commit("setSingleClickFeatures", features);
								this.$store.commit("setActiveFeaturePreview", feature);
								CommonHelper.routeTo({
									router: this.$router,
									vBus: this.$vBus,
									feature: feature
								});
							}
						} else {
							var doGeneralIdentify = true;
							if (this.userData && (this.userData.role == "street-viewer")) {
								doGeneralIdentify = false;
								var services = MapHelper.getMainServices();
								if (services.optional) {
									services.optional.forEach(function(service){
										if (service.id == "pano-2022") {
											feature = this.myMap.map.forEachFeatureAtPixel(e.pixel, function(feature, layer){
												if (layer) { // TODO! Toks variantas netinka... Reikia įsitikinti, kad sluoksnis yra panoramos taškų sluoksnis?..
													return feature;
												}
											}.bind(this), {hitTolerance: 5});
											if (!feature) {
												var doIdentify = false; // Lokalus config'as...
												if (doIdentify) {
													var callback = function(results){
														// Daryti panašiai kaip kapinių APP setFeatures4ActiveTomb()?..
														// Kol kas primityviai...
														this.$vBus.$emit("show-point-panorama-viewer", {
															title: "Panoraminis vaizdas",
															results: results,
															source: "vms-2022"
														});
													}.bind(this);
													this.identifyFeature(e.coordinate, service, "visible:0", callback);
												} else {
													var feature = new Feature();
													feature.setGeometry(new Point(e.coordinate));
													this.$vBus.$emit("show-point-panorama-viewer", {
														title: "Panoraminis vaizdas",
														feature: feature,
														source: "vms-2022"
													});
												}
											}
										}
									}.bind(this));
								}
							}
							if (doGeneralIdentify) {
								if (currentZoomLevel >= (this.minZoomLevel4Interaction - 3)) { // Leidžiame click'inti iš aukščiau (proto ribose)
									this.$store.commit("setActiveAction", {
										action: "identify",
										coordinate: e.coordinate,
										mapZoom: this.myMap.map.getView().getZoom()
									});
								}
							}
						}
					}
				}.bind(this));
			},
			getFeatures: function(pixel, currentZoomLevel, onClick){
				var features = [];
				if (onClick) {
					this.myMap.map.forEachFeatureAtPixel(pixel, function(feature, layer){
						if (layer && layer.get("opacity")) { // TODO: tikrinti tėvinio sluoksnio peršviečiamumą!
							if (["street-signs", "street-signs-vertical", "user-points"].indexOf(layer.parentServiceId) != -1) {
								if (currentZoomLevel >= this.minZoomLevel4Interaction) {
									feature.layer = layer; // Nelabai geras sprendimas?! Kažkaip visgi reikia žinoti aktyvų sluoksnį...
									features.push(feature);
								}
							} else if (layer.id == "search-results") { // Leidžiame click'inti ant paieškos rezultato...
								feature.layer = layer;
								features.push(feature); // BIG TODO, FIXME! Vis tiek pilnai dar nefunkcionuoja...
							} else if ((["vms-inventorization-l", "vms-inventorization-p", "vvt"].indexOf(layer.parentServiceId) != -1) || layer.clickable) {
								feature.layer = layer;
								features.push(feature);
							}
						}
					}.bind(this), {hitTolerance: 5});
				} else {
					if (currentZoomLevel >= this.minZoomLevel4Interaction) {
						this.myMap.map.forEachFeatureAtPixel(pixel, function(feature, layer){
							if (layer && layer.get("opacity")) { // TODO: tikrinti tėvinio sluoksnio peršviečiamumą!
								if ((["street-signs", "street-signs-vertical", "vvt", "user-points"].indexOf(layer.parentServiceId) != -1) || layer.clickable) {
									feature.layer = layer; // Nelabai geras sprendimas?! Kažkaip visgi reikia žinoti aktyvų sluoksnį...
									features.push(feature);
								}
							}
						}.bind(this), {hitTolerance: 5});
					}
				}
				return features;
			},
			getHighlightedFeatureVectorLayer: function(color, bolder, showVertices, zIndex){
				if (!color) {
					color = "rgba(255, 0, 0, $opacity)";
				}
				var layer = new VectorLayer({
					source: new VectorSource(),
					style: function(feature){
						var styles = [new Style({
							stroke: new Stroke({
								color: color.replace("$opacity", 1),
								width: bolder ? 3 : 1
							}),
							fill: new Fill({
								color: color.replace("$opacity", 0.4)
							}),
							image: new CircleStyle({
								radius: 10,
								fill: new Fill({
									color: color.replace("$opacity", 0.7)
								})
							})
						})];
						if (showVertices) {
							if (feature.getGeometry().getType() == "LineString") {
								styles.push(new Style({
									image: new CircleStyle({
										radius: 5,
										fill: new Fill({
											color: color.replace("$opacity", 1)
										}),
										stroke: new Stroke({
											color: "white",
											width: 2
										})
									}),
									geometry: function(feature){
										var coordinates = feature.getGeometry().getCoordinates();
										return new MultiPoint(coordinates);
									}
								}));
							}
						}
						return styles;
					},
					zIndex: zIndex || 1000
				});
				return layer;
			},
			showTooltip: function(c, feature){
				var tooltip = this.$refs.tooltip;
				if (tooltip) {
					var content = "Informacijos nėra";
					if (feature.layer && feature.layer.typeIdField) {
						content = feature.get(feature.layer.typeIdField);
						if (content) {
							if (feature.layer.typesCodedValues) {
								content = feature.layer.typesCodedValues[content] || content;
								if (feature.layer.typeField && feature.layer.typeField.alias) {
									content = feature.layer.typeField.alias + ": " + content;
								}
							}
							if (feature.layer.parentServiceId == "street-signs-vertical") {
								var customSymbolId = feature.get(CommonHelper.customSymbolIdFieldName);
								if (customSymbolId) {
									var timestamp = feature.get("unique-symbol-timestamp");
									if (!timestamp) {
										timestamp = feature.layer.timestamp;
									}
									content += '<img src="' + CommonHelper.getUniqueSymbolSrc(customSymbolId, timestamp) + '"></img>';
								}
							}
						}
					}
					if (feature.layer) {
						if (feature.layer.parentServiceId == "user-points") {
							// console.log("GET NAME?", feature); // TODO...
						}
					}
					var tooltipContent = "<span>" + content + "</span>";
					if (tooltipContent) {
						if (tooltip.activeFeature != feature) { // Čia toks kaip ir minimalus apsisaugojimas nuo bereikalingo turinio render'inimo?.. [FIXME! Ar jis tikrai veikia, kai redaguojame unikalų simbolį?]
							tooltip.innerHTML = tooltipContent;
							tooltip.activeFeature = feature
						}
						Object.assign(tooltip.style, {
							left: (c[0] + 20) + "px",
							top: c[1] + "px",
							display: "block"
						});
					} else {
						this.hideTooltip();
					}
				}
			},
			hideTooltip: function(){
				if (this.$refs.tooltip) {
					Object.assign(this.$refs.tooltip.style, {
						display: "none"
					});
				}
			},
			getAndShowActiveFeature: function(activeFeature){
				if (activeFeature.queryResult) {
					this.showActiveFeaturePreview(activeFeature);
					if (!activeFeature.noZoom) {
						if (!CommonHelper.isActiveFeatureTheSameAsActiveFeaturePreview(this.$store.state.activeFeaturePreview, activeFeature)) {
							this.myMap.zoomToFeatures([activeFeature.feature]);
						}
					}
				} else {
					if (!activeFeature.featureDescr) {
						if (!this.activeFeatureGettingCounter) {
							this.activeFeatureGettingCounter = 0;
						}
						this.activeFeatureGettingCounter += 1;
						var activeFeatureGettingCounter = this.activeFeatureGettingCounter;
						this.$store.state.myMap.findFeature(activeFeature).then(function(e){
							if (activeFeatureGettingCounter == this.activeFeatureGettingCounter) {
								this.setFeature4ActiveFeature(activeFeature, e.feature, e.additional, e.fields);
							} else {
								// Taip gali būti, jei greitai click'iname žemėlapyje...
							}
						}.bind(this), function(){
							if (activeFeatureGettingCounter == this.activeFeatureGettingCounter) {
								this.setFeature4ActiveFeature(activeFeature, "error");
							}
						}.bind(this));
					} else {
						if (activeFeature.featureDescr != "error") {
							this.showActiveFeaturePreview({
								feature: activeFeature.feature,
								data: activeFeature
							}); // Hmmm... Realiai jei jau egzistuoja "activeFeaturePreview", galime to nedaryti? Dabar tiesiog yra mintis
							// rodyti naujausią "feature"... Nes gal per tą kažkokį laiką pasikeitė objekto geometrija??? Jei tarkime žemėlapyje objektai pasikrovė prieš kokią valandą, o tik
							// dabar click'iname...
							// Kita vertus gausis nesąmonė, jei jau pasikeitusi žemėlapyje esančio objekto geometrija arba atributinė info, pvz. tipas!!!
							// Gal reiktų tikrinti kiek šis "feature" atitinka žemėlapyje esantį??...
							if (!activeFeature.isNew) { // Pridėjus naują objektą jį centruoti neaktualu, nes gi ir taip žinome kur jis yra!!!
								if (!CommonHelper.isActiveFeatureTheSameAsActiveFeaturePreview(this.$store.state.activeFeaturePreview, activeFeature)) {
									this.myMap.zoomToFeatures([activeFeature.feature]);
								}
							}
						}
					}
				}
			},
			showActiveFeaturePreview: function(e){
				if (!this.activeFeaturePreviewVectorLayer) {
					this.activeFeaturePreviewVectorLayer = this.getHighlightedFeatureVectorLayer("rgba(52, 212, 255, $opacity)", true, true);
					this.myMap.map.addLayer(this.activeFeaturePreviewVectorLayer);
				} else {
					this.activeFeaturePreviewVectorLayer.getSource().clear(true);
				}
				this.activeFeaturePreviewVectorLayer.getSource().addFeature(e.feature);
				this.showActiveFeatureAdditionalFeatures(e.data);
				if (e.feature.refreshAdditionalFeatures) {
					// BIG TODO, FIXME!!! ŠITA SKILTIS HACK'iŠKA!!!
					// Gal net kol kas (2021.05.30) hack'iškiausia visoje app...
					// Jei jos nebus, tai tiesiog 2 atvejais nebus braižomos pagalbinės linijos...
					if (this.$store.state.activeFeature) {
						if (e.feature.refreshAdditionalFeatures == "street-sign-vertical") {
							if (this.$store.state.activeFeature.additionalData) {
								var data = {
									type: "v",
									additionalData: {}
								};
								data.additionalData.supports = this.$store.state.activeFeature.additionalData.supports;
								data.additionalData.signs = this.$store.state.activeFeature.additionalData.signs;
								data.additionalData.newSignFeature = e.feature;
								this.showActiveFeatureAdditionalFeatures(data);
							}
						} else {
							this.showActiveFeatureAdditionalFeatures(this.$store.state.activeFeature);
						}
					}
					delete e.feature.refreshAdditionalFeatures; // Kintamasis savo misiją atliko, jį naikiname...
				}
			},
			removeActiveFeaturePreview: function(){
				if (this.activeFeaturePreviewVectorLayer) {
					this.activeFeaturePreviewVectorLayer.getSource().clear(true);
				}
				this.removeActiveFeatureAdditionalFeatures();
			},
			showActiveFeatureInOverlayPreview: function(e){
				if (!this.activeFeatureInOverlayPreviewVectorLayer) {
					this.activeFeatureInOverlayPreviewVectorLayer = this.getHighlightedFeatureVectorLayer("rgba(255, 255, 127, $opacity)", true, true);
					this.myMap.map.addLayer(this.activeFeatureInOverlayPreviewVectorLayer);
				} else {
					this.activeFeatureInOverlayPreviewVectorLayer.getSource().clear(true);
				}
				if (!e.noPreview) {
					this.activeFeatureInOverlayPreviewVectorLayer.getSource().addFeature(e.feature);
				}
			},
			removeActiveFeatureInOverlayPreview: function(){
				if (this.activeFeatureInOverlayPreviewVectorLayer) {
					this.activeFeatureInOverlayPreviewVectorLayer.getSource().clear(true);
				}
			},
			setFeature4ActiveFeature: function(activeFeature, featureDescr, additionalData, fields){
				activeFeature = Object.assign({}, activeFeature);
				activeFeature.featureDescr = featureDescr;
				if (additionalData) {
					activeFeature.additionalData = {}; // Kol kas aktualu tik vertikaliųjų KŽ sluoksnių objektams!
					var feature;
					if (additionalData.supports) {
						activeFeature.additionalData.supports = [];
						additionalData.supports.forEach(function(featureDescr){
							feature = MapHelper.createFeature(featureDescr, true);
							feature.layerId = featureDescr.layerId;
							activeFeature.additionalData.supports.push(feature);
						});
					}
					if (additionalData.signs) {
						activeFeature.additionalData.signs = [];
						additionalData.signs.forEach(function(featureDescr){
							feature = MapHelper.createFeature(featureDescr, true);
							if (featureDescr.isTasksRelated) {
								feature.isTasksRelated = true;
							}
							feature.layerId = featureDescr.layerId;
							activeFeature.additionalData.signs.push(feature);
						});
					}
				}
				if (activeFeature.featureDescr != "error") {
					activeFeature.feature = MapHelper.createFeature(activeFeature.featureDescr, activeFeature.type == "v");
					activeFeature.featureType = CommonHelper.getFeatureType(activeFeature);
					if (fields) {
						activeFeature.fields = fields;
					}
					// Dar čia būtų aktualu activeFeature.feature'iui nustatyti layer'į pvz. taip:
					// this.myMap.setLayerForFeature(activeFeature.feature, activeFeature.type == "v" ? "street-signs-vertical" : "street-signs");
					// Bėda ta, kad jei tai pats pradinis objektas, su kurio URL atidarėme `app`, tai tie sluoksniai dar gali būti net nepakrauti???... Šitą reiktų išsiaiškinti... TODO!
				}
				this.$store.commit("setActiveFeature", activeFeature);
			},
			showActiveFeatureAdditionalFeatures: function(e){
				// Aktualu vertikaliajam ženklinimui! Rodyti atramas ir KŽ jungiančias linijas... Senoje APP versijoje buvo aktualus metodas drawTempFeatures()!
				this.removeActiveFeatureAdditionalFeatures();
				var additionalFeatures = [];
				if (e && (["v", "task-related-v"].indexOf(e.type) != -1)) {
					if (e.additionalData) {
						var supportFeature;
						if (e.additionalData.supports && e.additionalData.supports.length == 1) {
							supportFeature = e.additionalData.supports[0];
						}
						if (supportFeature) {
							if (e.additionalData.signs) {
								var geometries = [];
								e.additionalData.signs.forEach(function(signFeature){
									geometries.push(new LineString(
										[
											supportFeature.getGeometry().getCoordinates(),
											signFeature.getGeometry().getCoordinates()
										]
									));
									geometries.push(signFeature.getGeometry());
								});
								geometries.push(supportFeature.getGeometry());
								if (e.additionalData.newSignFeature) {
									geometries.push(new LineString(
										[
											supportFeature.getGeometry().getCoordinates(),
											e.additionalData.newSignFeature.getGeometry().getCoordinates()
										]
									));
								}
								var feature = new Feature({
									geometry: new GeometryCollection(geometries)
								});
								additionalFeatures.push(feature);
							}
						}
					}
				}
				if (additionalFeatures.length) {
					if (!this.activeFeatureAdditionalFeaturesLayer) {
						this.activeFeatureAdditionalFeaturesLayer = new VectorLayer({
							source: new VectorSource(),
							style: function(){
								return new Style({
									stroke: new Stroke({
										color: "rgba(52, 212, 255, 1)", // Seniau būdavo raudonos linijos...
										width: 2
									}),
									image: new CircleStyle({
										radius: 4,
										fill: new Fill({
											color: "rgba(52, 212, 255, 1)"
										})
									})
								});
							},
							zIndex: 31 // Gan svarbu, kad būtų žemiau nei vertikalusis ženklinimas... TODO...
						});
						this.myMap.map.addLayer(this.activeFeatureAdditionalFeaturesLayer);
					}
					console.log("SET additionalFeatures Z INDEX");
					this.activeFeatureAdditionalFeaturesLayer.getSource().addFeatures(additionalFeatures);
				}
			},
			removeActiveFeatureAdditionalFeatures: function(){
				if (this.activeFeatureAdditionalFeaturesLayer) {
					this.activeFeatureAdditionalFeaturesLayer.getSource().clear(true);
				}
			},
			redrawActiveFeatureAdditionalFeatures: function(data){
				this.showActiveFeatureAdditionalFeatures(data);
			},
			drawTempCircles: function(supportFeature){
				if (this.tempCirclesFeaturesLayer) {
					this.tempCirclesFeaturesLayer.getSource().clear(true);
				}
				if (supportFeature) {
					if (!this.tempCirclesFeaturesLayer) {
						this.tempCirclesFeaturesLayer = new VectorLayer({
							source: new VectorSource(),
							style: function(){
								return new Style({
									stroke: new Stroke({
										color: "rgba(255, 0, 0, 0.7)",
										width: 2,
										lineDash: [3, 3],
										lineCap: "butt"
									})
								});
							},
							zIndex: 30 // Gan svarbu, kad būtų žemiau nei vertikalusis ženklinimas... TODO...
						});
						this.myMap.map.addLayer(this.tempCirclesFeaturesLayer);
					}
					console.log("SET CIRCLES Z INDEX");
					var step = 1.25,
						circlesRadiuses = [step, 2 * step, 3 * step, 4 * step, 5 * step],
						circlesFeatures = [],
						circleFeature;
					circlesRadiuses.forEach(function(circleRadius){
						circleFeature = new Feature(new Circle(supportFeature.getGeometry().getCoordinates(), circleRadius));
						circlesFeatures.push(circleFeature);
					}.bind(this));
					this.tempCirclesFeaturesLayer.getSource().addFeatures(circlesFeatures);
				}
			},
			getLayerKey: function(serviceId, layerId){
				var key;
				if (CommonHelper.layerIds) {
					var layerDataToSearch = JSON.stringify([serviceId, layerId]);
					for (var k in CommonHelper.layerIds) {
						if (layerDataToSearch == JSON.stringify(CommonHelper.layerIds[k])) {
							key = k;
						}
					}
				}
				return key;
			},
			identifyFeature: function(coordinate, service, layers, callback){
				if (service.url) {
					this.myMap.showPointProgress(coordinate);
					// https://developers.arcgis.com/rest/services-reference/enterprise/identify-map-service-.htm
					var url = service.url + "/identify",
						params = this.myMap.getIdentifyParams(coordinate, layers);
					CommonHelper.getFetchPromise(url, function(json){
						return json;
					}.bind(this), "POST", params).then(function(json){
						if (json) {
							if (json.error) {
								// ...
							} else {
								if (json.results) {
									if (callback) {
										callback(json.results);
									}
								}
							}
						}
						this.myMap.hidePointProgress();
					}.bind(this), function(){
						this.myMap.hidePointProgress();
					}.bind(this));
				}
			},
			getAndShowActiveAction: function(activeAction){
				if (activeAction && (activeAction.action == "identify")) {
					if (!activeAction.promises) {
						var promises = [],
							services = [];
						this.myMap.map.getLayers().forEach(function(layer){ // TODO... Rikiuoti pagal z-index'ą, jei dabar nesurikiuota...
							if (layer.service && layer.service.clickable) {
								var promise = this.myMap.findFeature({
									service: layer.service,
									coordinates: activeAction.coordinate,
									z: activeAction.z
								});
								promises.push(promise);
								services.push(layer.service);
							}
						}.bind(this));
						if (promises.length) {
							if (!this.$store.state.additionalLayersIdentifyResultInMapOverlay) {
								// this.$store.commit("setActiveFeature", null); // Neaišku dėl `usability`... Ar ok deaktyvuoti esamą objektą?.. Ar realiai jis turi būti deaktyvuojamas tik, jei identifikavimo veiksmas sėkmingai grąžino rezultatą?.. Neaiškūs naudotojo lūkesčiai
							}
							this.myMap.showPointProgress(activeAction.coordinate);
							promises = Promise.allSettled(promises);
							activeAction.promises = promises;
							promises.then(function(e){
								var count = 0,
									features = [];
								if (e) {
									e.forEach(function(item, i){
										if (item.value) {
											item.service = services[i];
											if (Array.isArray(item.value)) {
												item.value.forEach(function(record){
													var feature = MapHelper.createFeature(record);
													feature.layer = {
														layerId: record.layerId,
														serviceId: item.service.id,
														service: item.service
													};
													feature.rawIdentifyData = record;
													feature.fromIdentifyAction = true;
													features.push(feature);
												});
												count += item.value.length;
											}
										}
									});
									e.count = count;
								}
								Vue.set(activeAction, "res", e);
								if (features.length) {
									// BIG TODO! Reikia sprendimo su unikalia nuoroda!..
									var uppermostFeature = features[0];
									if (this.$store.state.additionalLayersIdentifyResultInMapOverlay) {
										this.$store.commit("setSingleClickFeatures4Overlay", features); // Nelabai suveikia? Gal nes ir pats popup'as nepersipiešia?..
										this.$store.commit("setActiveFeatureInOverlayPreview", uppermostFeature);
									} else {
										this.$store.commit("setSingleClickFeatures", features); // Nelabai suveikia? Gal nes ir pats popup'as nepersipiešia?..
										this.$store.commit("setActiveFeaturePreview", uppermostFeature);
									}
									this.$store.commit(this.$store.state.additionalLayersIdentifyResultInMapOverlay ? "setActiveFeatureInOverlay" : "setActiveFeature", {
										queryResult: {
											feature: uppermostFeature
										},
										noZoom: true,
										feature: uppermostFeature,
										serviceId: uppermostFeature.layer.serviceId,
										layerId: uppermostFeature.layer.layerId,
										coordinates: activeAction.coordinate
									});
								} else {
									if (!this.$store.state.additionalLayersIdentifyResultInMapOverlay) {
										this.$store.commit("setActiveAction", null); // Vėl `usability` klausimas... Ar ok, kad popup'as pradings..
										/*
										this.$vBus.$emit("show-message", {
											type: "info",
											message: "Identifikavimo rezultatų nėra"
										});
										*/
									}
								}
								this.myMap.hidePointProgress();
							}.bind(this), function(){
								// ...
							});
						} else {
							this.$store.commit("setActiveAction", null);
						}
					} else {
						console.log("ZOOM?..", activeAction); // ...
					}
				}
			},
			toggleLeftSideWrapper: function(){
				if (this.$refs.leftSideWrapper) {
					// FIXME... Prastas sprendimas
					this.$refs.leftSideWrapper.style.width = this.$refs.leftSideWrapper.clientWidth ? 0 : "500px";
					setTimeout(function(){
						// TODO...
						// this.myMap.map.updateSize();
					}.bind(this), 300);
				}
			},
			createContextMenu: function(){
				this.myMap.map.getViewport().addEventListener("contextmenu", function(evt){
					evt.preventDefault();
					var coordinates = this.myMap.map.getEventCoordinate(evt);
					this.$vBus.$emit("set-context-menu-state", {
						state: "visible",
						coordinates: coordinates
					});
				}.bind(this));
			},
			showInitialFeature: function(e){
				if (!this.initialFeatureVectorLayer) {
					this.initialFeatureVectorLayer = this.getHighlightedFeatureVectorLayer(null, null, null, 2000);
					this.myMap.map.addLayer(this.initialFeatureVectorLayer);
				}
				var feature = new Feature();
				feature.setGeometry(new Point([parseFloat(e[0]), parseFloat(e[1])]));
				var coordinates = feature.getGeometry().getCoordinates();
				this.initialFeatureVectorLayer.getSource().addFeature(feature);
				this.$store.commit("setActiveFeatureInOverlay", {
					queryResult: {
						feature: feature
					},
					noZoom: true,
					feature: feature,
					coordinates: coordinates,
					noPreview: true,
					onClose: function(){
						if (this.initialFeatureVectorLayer) {
							this.myMap.map.removeLayer(this.initialFeatureVectorLayer);
							this.initialFeatureVectorLayer = null;
						}
					}.bind(this),
					content: '<span class="body-2">Taško koordinatės: <span><span class="font-weight-bold">' + coordinates[0].toFixed() + '</span>, <span class="font-weight-bold">' + coordinates[1].toFixed() + '</span></span></span>'
				});
				this.myMap.map.getView().setCenter(coordinates);
				this.myMap.map.getView().setZoom(parseInt(e[2]));
			}
		},

		watch: {
			myMap: {
				immediate: true,
				handler: function(myMap){
					if (myMap) {
						this.makeMyMapInteractive();
						var initialCoordinates = this.$store.state.initialCoordinates;
						if (initialCoordinates) {
							initialCoordinates = initialCoordinates.split(",");
							if (initialCoordinates.length == 3) {
								this.showInitialFeature(initialCoordinates);
							}
						}
						if (this.initialActiveFeature) {
							var activeFeature = this.initialActiveFeature;
							this.initialActiveFeature = null;
							this.getAndShowActiveFeature(activeFeature);
						}
						if (this.initialActiveAction) {
							var activeAction = this.initialActiveAction;
							this.initialActiveAction = null;
							this.getAndShowActiveAction(activeAction);
						}
						// Laikinai...
						if (location.hostname == "localhost" && window.location.search == "?mode=test") {
							this.$vBus.$emit("task-attachment-new", {
								task: {
									globalId: "{8C499E65-04BA-47FF-ADCF-D5C28E7855A5}",
									objectId: 4802,
									feature: new Feature({
										globalId: "{8C499E65-04BA-47FF-ADCF-D5C28E7855A5}",
										OBJECTID: 4802
									})
								},
								masterAttachment: true
							});
						}
					}
				}
			},
			highlightedFeature: {
				immediate: true,
				handler: function(highlightedFeature){
					// Dabar highlight'inimas vyksta viršuje sukuriant laikiną `feature`... O gal būtų galima to atsisakyti? Reiktų aktyviam feature'iui pakeisti kažkokį atributą,
					// pagal kurį keistųsi jo render'inimas...
					if (this.highlightedFeatureVectorLayer) {
						this.highlightedFeatureVectorLayer.getSource().clear(true);
						if (highlightedFeature) {
							this.highlightedFeatureVectorLayer.getSource().addFeature(highlightedFeature);
						}
					}
				}
			},
			"$store.state.activeFeature": {
				immediate: true,
				handler: function(activeFeature){
					if (this.myMap) {
						if (activeFeature) {
							if (!CommonHelper.isActiveFeatureTheSameAsActiveFeaturePreview(this.$store.state.activeFeaturePreview, activeFeature)) {
								this.removeActiveFeaturePreview();
							} else {
								if (activeFeature.type == "v" && (activeFeature.layerId == this.myMap.getLayerId("verticalStreetSigns"))) {
									activeFeature.verticalStreetSignsSupportGlobalId = CommonHelper.stripGuid(this.$store.state.activeFeaturePreview.get("GUID")); // Naudojame, kad nereiktų daryti papildomos
									// užklausos tik tam, kad gauti vertikalaus kelio ženklo atramos GlobalID!!! Aišku, teoriškai įmanoma bėda, jei objektus esame pakrovę seniai ir pvz.
									// kitas vartotojas yra pakeitęs kelio ženklo atramos GlobalID... Kita vertus, tai yra mažai tikėtina!
								}
								this.removeActiveFeatureAdditionalFeatures(); // Aktualu kai pvz. click'iname žemėlapyje ant to paties vertikalaus KŽ objekto... Kad tam kartui išsivalytų
								// laikinos linijos...
							}
							this.getAndShowActiveFeature(activeFeature);
						} else {
							if (this.activeFeatureGettingCounter) {
								// Pvz. paspaudėme ant objekto... Tada prasidėjo "query", atsidarė popup'as... Ir uždarėme popup'ą specialiai... Gi nenorime, kad pavėlavęs "query"
								// rezultatas rodytų mums tą popup'ą...
								this.activeFeatureGettingCounter += 1;
							}
							this.removeActiveFeaturePreview();
						}
					} else {
						this.initialActiveFeature = activeFeature;
					}
				}
			},
			"$store.state.activeFeatureInOverlay": {
				immediate: true,
				handler: function(activeFeature){
					if (this.myMap) {
						if (activeFeature) {
							if (!CommonHelper.isActiveFeatureTheSameAsActiveFeaturePreview(this.$store.state.activeFeatureInOverlayPreview, activeFeature)) {
								this.removeActiveFeatureInOverlayPreview();
							}
							this.showActiveFeatureInOverlayPreview(activeFeature);
						}
					}
				}
			},
			"$store.state.activeFeaturePreview": {
				immediate: true,
				handler: function(activeFeature){
					if (activeFeature) {
						this.showActiveFeaturePreview({
							feature: activeFeature
						});
					} else {
						this.removeActiveFeaturePreview();
					}
				}
			},
			"$store.state.activeFeatureInOverlayPreview": {
				immediate: true,
				handler: function(activeFeature){
					if (activeFeature) {
						this.showActiveFeatureInOverlayPreview({
							feature: activeFeature
						});
					} else {
						this.removeActiveFeatureInOverlayPreview();
					}
				}
			},
			"$store.state.myMap.interactionsCount": {
				immediate: true,
				handler: function(interactionsCount){
					if (interactionsCount) {
						this.highlightedFeature = null; // Jei to nepadarysime, tai ir kabos žemėlapyje highlight'intas `feature` su savo tooltip'u...
						this.hideTooltip();
					}
				}
			},
			"$store.state.activeTask": {
				immediate: true,
				handler: function(activeTask){
					if (activeTask && activeTask.feature && activeTask.feature != "error") {
						// Čia būtų galima prie žemėlapio pridėti sluoksnių grupę, kurioje būtų visi sluoksniai, susiję su užduotimi?
						// Sluoksniuose turėtų būti sąlyga, kad krautų tik tuos objektus, kurių užduoties GUID lygus aktyvios užduoties GlobalID?
						// Bet mes dabar pasirenkame kiek kitokį sprendimą...
						// Rankiniu būdu susirinksime visus susijusius objektus ir juos pridėsime į žemėlapį vienu ypu...
						if (!activeTask.data && !activeTask.initialData) {
							TaskHelper.getRelatedFeatures(activeTask, this.$store); // Kai įsivykdo, jau pats nustato susijusius geografinius objektus... Tai savo ruožtu aktyvuoja `activeTaskFeatures` persiskaičiavimą... O tas veiksmas savo ruožtu aktyvuoja žemėlapio objektų persipiešimą?...
						}
					}
				}
			},
			activeTaskFeatures: {
				immediate: true,
				handler: function(activeTaskFeatures){
					if (this.myMap && this.myMap.map) {
						this.myMap.map.getLayers().forEach(function(layer){
							if (layer.service) {
								if (layer.service.id == "street-signs" || layer.service.id == "street-signs-vertical") {
									if (layer.getSource) {
										layer.getSource().changed(); // Liepia persipiešti sluoksniui...
									} else {
										if (layer.getLayers) {
											layer.getLayers().forEach(function(l){
												if (l.getSource) {
													l.getSource().changed(); // Liepia persipiešti sluoksniui...
												}
											});
										}
									}
								}
							}
						});
						if (this.taskLayer) {
							this.myMap.map.removeLayer(this.taskLayer);
							this.taskLayer = null;
						}
						var features,
							key,
							mode; // Galima naudoti -> "test"
						if (mode == "test") { // Čia toks pradinis variantas, kurio esmė tiesiog pavaizduoti objektus žemėlapyje, nesigilinant į jų vizualizaciją...
							if (activeTaskFeatures) {
								if (!this.taskLayer) {
									this.taskLayer = this.getHighlightedFeatureVectorLayer("rgba(0, 255, 0, $opacity)", true); // Tik bėda su zIndex'u dabar :) Jis toks pat, kaip ir tikrojo sluoksnio...
									this.myMap.map.addLayer(this.taskLayer);
								}
								for (key in activeTaskFeatures) {
									features = activeTaskFeatures[key];
									if (features) {
										features.forEach(function(item){
											if (item.feature) {
												this.taskLayer.getSource().addFeature(item.feature);
											}
										}.bind(this));
									}
								}
							}
						} else {
							if (activeTaskFeatures) {
								if (!this.taskLayer) {
									this.taskLayer = new LayerGroup();
									this.taskLayer.isTasksRelatedLayer = true; // Prireiks DrawHelper'io getOrigFeature() metode...
									this.taskSublayers = {};
									var taskSublayer,
										taskSublayers = [],
										layerInfo;
									for (key in activeTaskFeatures) {
										taskSublayer = new VectorImageLayer({
											source: new VectorSource(),
											zIndex: 30
										});
										var layerIdMeta = CommonHelper.layerIds[key];
										if (layerIdMeta) {
											layerInfo = this.myMap.getLayerInfo(key) || {};
											taskSublayer.setStyle(VectorLayerStyleHelper.getVectorLayerStyle({id: layerIdMeta[0], specificRenderingType: "task"}, layerInfo));
											taskSublayer.isTasksRelatedLayer = true;
											// FIXME! Dabar gan pasikartojantis kodas... Reikia prie sluoksnių prikabinti šiokių tokių savybių... Aktualu :hover tooltip'ui, click'inimui...
											// O gal tą visą info susirinkti iš analogiškų sluoksnių?..
											taskSublayer.parentServiceId = layerIdMeta[0];
											taskSublayer.layerId = layerIdMeta[1]; // Dabar originalaus sluoksnio layerId! Ar gerai? Ar blogai?..
											if (CommonHelper.layerIds.tasksRelated) {
												// taskSublayer.layerId = CommonHelper.layerIds.tasksRelated[key]; // Teisingas layerId'as aktualus findFeature() metodui!
												// Hmmm... Bet kitur gal kas nors susiknisa? Ar ne?
												// Jooo... Visą grandinę reiktų keisti... Pvz. getFeatures() metodą...
											}
											taskSublayer.typeIdField = layerInfo.typeIdField;
											taskSublayer.typesCodedValues = {};
											taskSublayer.globalIdField = layerInfo.globalIdField;
											taskSublayer.objectIdField = layerInfo.objectIdField;
											taskSublayer.rotationField = MapHelper.getRotationField(layerInfo);
											if (layerInfo.types) {
												layerInfo.types.forEach(function(type){
													taskSublayer.typesCodedValues[type.id] = type.name;
												});
											}
											if (layerInfo.fields) {
												layerInfo.fields.some(function(field){
													if (field.name == layerInfo.typeIdField) {
														taskSublayer.typeField = field;
														return true;
													}
												});
											}
											this.taskSublayers[key] = taskSublayer;
											taskSublayers.push(taskSublayer);
										}
									}
									this.taskLayer.setLayers(new Collection(taskSublayers));
									this.myMap.map.addLayer(this.taskLayer);
								}
								for (key in activeTaskFeatures) {
									features = activeTaskFeatures[key];
									if (features) {
										features.forEach(function(item){
											if (item.feature) {
												taskSublayer = this.taskSublayers[key];
												if (taskSublayer) {
													item.feature.setId(item.feature.get(taskSublayer.objectIdField)); // Reikalingas DrawerHelper'io getOrigFeature() metode ieškant dominančio objekto sluoksnio šaltinyje!
													taskSublayer.getSource().addFeature(item.feature);
												}
											}
										}.bind(this));
									}
								}
							}
						}
					}
				}
			},
			"$store.state.activeAction": {
				immediate: true,
				handler: function(activeAction){
					if (this.myMap) {
						if (activeAction) {
							this.getAndShowActiveAction(activeAction);
						} else {
							this.myMap.hidePointProgress();
						}
					} else {
						this.initialActiveAction = activeAction;
					}
				}
			}
		}
	};
</script>

<style scoped>
	.wrapper, .map-wrapper {
		height: 100%;
		width: 100%;
		overflow: hidden;
		position: relative;
	}
	.left-side-wrapper {
		height: 100%;
		border-right: 1px solid #efefef;
		width: 500px;
		transition: width 0.2s ease-in-out;
		position: relative;
		z-index: 1;
	}
	.layers-list-wrapper {
		overflow: auto;
		height: 100%;
	}
	.top-start {
		position: absolute;
		top: 0.5em;
		bottom: 0.5em;
		left: 50px;
		right: 50px;
		overflow: hidden;
		z-index: 3;
	}
	.top-end {
		position: absolute;
		top: 0;
		bottom: 0;
		width: 100%;
		overflow: hidden;
		z-index: 3;
	}
	.bottom-start {
		position: absolute;
		top: 0.5em;
		bottom: 0.5em;
		left: 0.5em;
		right: 0.5em;
		z-index: 4;
	}
	.buttons-widgets-container {
		overflow-x: hidden;
		overflow-y: auto;
		position: relative;
	}
	#map-tooltip {
		position: absolute;
		z-index: 1;
		background-color: white;
		pointer-events: none;
		display: none;
		border: 1px solid #cccccc;
	}
	.collapse {
		position: absolute;
		width: 12px;
		height: 100px;
		background-color: #555555;
		top: 50%;
		margin-top: -50px;
		right: -12px;
		cursor: pointer;
	}
	.collapse:hover {
		background-color: #222222;
	}
</style>