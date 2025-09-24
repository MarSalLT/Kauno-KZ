<template>
	<OverMapButtonContent
		:title="title"
		:btn="btn"
		:onClose="onClose"
		:noContentPadding="true"
		:fixedHeight="350"
		ref="wrapper"
	>
		<template v-slot>
			<div
				class="body-2 panorama-content fill-height"
				id="mly"
				ref="pano"
				:key="key"
			>
				<template v-if="panoramaFeature">
					<template v-if="panoramaFeature == 'error' || panoramaFeature == 'no-features'">
						<div class="pa-3">
							<template v-if="panoramaFeature == 'error'">
								<v-alert
									dense
									type="error"
									class="ma-0"
								>
									Atsiprašome, įvyko nenumatyta klaida...
								</v-alert>
							</template>
							<template v-else-if="panoramaFeature == 'no-features'">
								<v-alert
									dense
									type="info"
									class="ma-0"
								>
									<template v-if="searchRadius">
										{{searchRadius}} metrų spinduliu panoraminių vaizdų nėra...
									</template>
									<template v-else>
										Netoliese esančių panoraminių vaizdų nėra...
									</template>
								</v-alert>
							</template>
						</div>
					</template>
				</template>
				<v-progress-circular
					indeterminate
					color="primary"
					:size="30"
					width="2"
					class="ma-3"
					v-if="loading"
				></v-progress-circular>
			</div>
			<div
				class="panorama-content-temp"
				ref="panoTemp"
				:key="key + '-t'"
			>
			</div>
		</template>
		<template v-slot:buttons>
			<div v-if="panoramaFeature && panoramaFeature != 'error' && panoramaFeature != 'no-features'">
				<v-btn
					v-on:click="exportPanorama4ActiveTask"
					:width="28"
					:height="28"
					min-width="auto"
					class="ml-2 rounded-circle"
					small
					color="success"
					title="Eksportuoti panoramą kaip piešinuką aktyviai užduočiai"
					:loading="exportInProgress"
					v-if="activeTask && (['vms', 'vms-2022'].indexOf(panoramaFeature.get('sourceType')) != -1)"
				>
					<v-icon color="white" small>mdi-camera</v-icon>
				</v-btn>
				<v-btn
					icon
					v-on:click="toggleSize"
					class="ml-2"
					small
					:title="maximized ? 'Sumažinti' : 'Padidinti'"
				>
					<v-icon>
						{{maximized ? 'mdi-fullscreen-exit' : 'mdi-fullscreen'}}
					</v-icon>
				</v-btn>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import Circle from "ol/geom/Circle";
	import CommonHelper from "../helpers/CommonHelper";
	import Feature from "ol/Feature";
	import MapHelper from "../helpers/MapHelper";
	import OverMapButtonContent from "./OverMapButtonContent";
	import * as PhotoSphereViewer from "photo-sphere-viewer";
	import Point from "ol/geom/Point";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import {Circle as CircleStyle, Fill, Stroke, Style, Text} from "ol/style";
	import EsriJSON from "ol/format/EsriJSON";
	import {fromCircle} from "ol/geom/Polygon";
	import LineString from "ol/geom/LineString";
	import MVT from "ol/format/MVT";
	import VectorTileLayer from "ol/layer/VectorTile";
	import VectorTileSource from "ol/source/VectorTile";
	import {get as getProjection} from "ol/proj";
	import {transformExtent} from "ol/proj";
	import "photo-sphere-viewer/dist/photo-sphere-viewer.css";

	export default {
		data: function(){
			var data = {
				title: null,
				btn: null,
				feature: null,
				lookAt: null,
				maximized: false,
				panoramaFeature: null,
				searchRadius: 20,
				loading: false,
				highlightedFeature: null,
				mapillaryV: "4",
				key: 0,
				exportInProgress: false
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			activeTask: {
				get: function(){
					var activeTask = null;
					if (this.$store.state.activeTask) {
						var feature = this.$store.state.activeTask.feature;
						if (feature && feature != "error") {
							activeTask = this.$store.state.activeTask;
						}
					}
					return activeTask;
				}
			}
		},

		components: {
			OverMapButtonContent
		},

		created: function(){
			this.$vBus.$on("show-point-panorama-viewer", this.show);
			window.addEventListener("resize", this.onWindowResize);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-point-panorama-viewer", this.show);
			window.removeEventListener("resize", this.onWindowResize);
		},

		methods: {
			show: function(e){
				this.title = e.title;
				this.btn = e.btn;
				this.feature = e.feature;
				this.lookAt = Boolean(e.lookAt);
				this.panoramaFeature = null; // Anuliuojame, jei buvo seniau...
				this.$refs.wrapper.open = true;
				this.maximized = false;
				if (e.feature) {
					this.getAndShowPointPanorama(e.feature, e.source);
				} else if (e.results) {
					console.log("TODO... Convert to possibleFeatures", e.results); // TODO...
					var possibleFeatures = []; // TODO: sukurti iš e.results
					this.showResults(possibleFeatures, null, e.source);
				}
			},
			toggleSize: function(){
				this.maximized = !this.maximized;
			},
			getAndShowPointPanorama: function(feature, source){
				this.loading = true;
				this.key += 1;
				if (this.vectorLayer) {
					this.vectorLayer.getSource().clear(true);
				}
				var params;
				if (source == "mapillary") {
					this.myMap.showPointProgress(feature.getGeometry().getCoordinates());
					if (this.mapillaryV == "4") {
						var url = "https://tiles.mapillary.com/maps/vtp/mly1_public/2/{z}/{x}/{y}?access_token=" + CommonHelper.mapillaryV4ClientToken,
							z = 14,
							format = new MVT();
						var layer = new VectorTileLayer({
							source: new VectorTileSource({
								format: format,
								url: url
							})
						});
						var pointCoordinates = feature.getGeometry().getCoordinates(),
							extent = transformExtent([
								pointCoordinates[0] - this.searchRadius,
								pointCoordinates[1] - this.searchRadius,
								pointCoordinates[0] + this.searchRadius,
								pointCoordinates[1] + this.searchRadius
							], this.myMap.map.getView().getProjection().getCode(), "EPSG:3857");
						var tileGrid = layer.getSource().getTileGridForProjection(getProjection("EPSG:3857")),
							tileRange = tileGrid.getTileRangeForExtentAndZ(extent, z),
							urls = [];
						for (var x = tileRange.minX; x <= tileRange.maxX; x++) {
							for (var y = tileRange.minY; y <= tileRange.maxY; y++) {
								urls.push({
									x: x,
									y: y,
									url: url.replace("{z}", z).replace("{x}", x).replace("{y}", y),
									extent: tileGrid.getTileCoordExtent([z, x, y])
								});
							}
						}
						var promises = [];
						urls.forEach(function(item){
							promises.push(this.getMapillaryV4Promise(item.url));
						}.bind(this));
						promises = Promise.allSettled(promises);
						promises.then(function(values){
							var allFulfilled = true,
								allFeatures = [];
							values.forEach(function(v, i){
								if (v.status == "fulfilled") {
									// Testavimui, analizei: http://localhost:3001/admin/experimental
									var features = format.readFeatures(v.value, {
										extent: urls[i].extent
									});
									if (features) {
										allFeatures = allFeatures.concat(features);
									}
								} else {
									allFulfilled = false;
								}
							}.bind(this));
							if (allFulfilled) {
								allFeatures = this.filterMapillaryV4Features(allFeatures, feature);
								this.showResults(allFeatures, feature, source);
							} else {
								this.showResults("error");
							}
						}.bind(this));
					}
				} else if (source == "vms" || source == "vms-2022") {
					var optionalServiceId;
					if (source == "vms") {
						optionalServiceId = "pano";
					} else if (source == "vms-2022") {
						optionalServiceId = "pano-2022";
					}
					var mainServices = MapHelper.getMainServices(),
						queryUrl,
						where;
					if (mainServices && mainServices.optional) {
						mainServices.optional.some(function(optionalService){
							if (optionalService.id == optionalServiceId) {
								queryUrl = optionalService.url;
								if (queryUrl) {
									if (optionalService.showLayers) {
										queryUrl += "/" + optionalService.showLayers[0] + "/query";
										if (optionalService.layerDefs) {
											if (optionalService.layerDefs.layerId == optionalService.showLayers[0]) {
												where = optionalService.layerDefs.where;
											}
										}
									}
								}
							}
						});
					}
					if (queryUrl) {
						this.myMap.showPointProgress(feature.getGeometry().getCoordinates());
						var esriJsonFormat = new EsriJSON(),
							circleGeometry = new Circle(feature.getGeometry().getCoordinates(), this.searchRadius);
						circleGeometry = fromCircle(circleGeometry, 32);
						var queryFeature = new Feature();
						queryFeature.setGeometry(circleGeometry);
						queryFeature = esriJsonFormat.writeFeatureObject(queryFeature, {
							featureProjection: this.myMap.map.getView().getProjection()
						});
						params = {
							f: "json",
							where: where || "1=1",
							outFields: "*",
							geometry: JSON.stringify(queryFeature.geometry),
							geometryType: "esriGeometryPolygon"
						};
						CommonHelper.getFetchPromise(queryUrl, function(json){
							return json;
						}.bind(this), "POST", params).then(function(json){
							var esriJsonFormat = new EsriJSON(),
								possibleFeatures = esriJsonFormat.readFeatures(json);
							this.showResults(possibleFeatures, feature, source);
						}.bind(this), function(){
							this.showResults("error");
						}.bind(this));
					}
				}
			},
			onClose: function(){
				if (this.myMap) {
					this.myMap.hidePointProgress();
				}
				if (this.vectorLayer) {
					this.vectorLayer.getSource().clear(true);
				}
				this.removeVectorLayerInteractivity();
			},
			showResults: function(possibleFeatures, sourceFeature, source){
				this.myMap.hidePointProgress();
				if (possibleFeatures == "error") {
					this.panoramaFeature = "error";
					this.loading = false;
				} else {
					if (!this.vectorLayer) {
						this.vectorLayer = new VectorLayer({
							source: new VectorSource(),
							zIndex: 1002,
							style: function(feature){
								var type = feature.get("type"),
									style;
								if (type == "coverage") {
									if (feature.getGeometry().getType() == "Circle") {
										style = new Style({
											fill: new Fill({
												color: "rgba(255, 255, 0, 0.05)"
											}),
											stroke: new Stroke({
												width: 2,
												color: "black"
											})
										});
									} else if (feature.getGeometry().getType() == "Point") {
										style = new Style({
											image: new CircleStyle({
												radius: 4,
												fill: new Fill({
													color: "black"
												}),
												stroke: new Stroke({
													width: 2,
													color: "white"
												})
											})
										});
									}
								} else {
									if (feature.get("active")) {
										style = [];
										style.push(
											new Style({
												text: new Text({
													text: "\uf1eb",
													scale: 1.2,
													font: "normal 18px FontAwesome",
													offsetY: -10,
													rotation: feature.get("angle") * Math.PI / 180,
													fill: new Fill({
														color: "red"
													}),
													stroke: new Stroke({
														color: "red",
														width: 3
													})
												})
											})
										);
										style.push(
											new Style({
												image: new CircleStyle({
													radius: 7,
													stroke: new Stroke({
														width: 2,
														color: "black"
													}),
													fill: new Fill({
														color: "#05cb63"
													})
												})
											})
										);
									} else {
										style = new Style({
											image: new CircleStyle({
												radius: 7,
												fill: new Fill({
													color: "#05cb63"
												})
											})
										});
									}
								}
								return style;
							}
						});
						this.myMap.map.addLayer(this.vectorLayer);
					}
					if (sourceFeature) {
						var coverageFeature = new Feature(),
							showCoverageCenter = !this.lookAt; // Jei panoramą iškvietėme iš popup'o, tai to centro simboliuko nerodome...
						coverageFeature.setGeometry(new Circle(sourceFeature.getGeometry().getCoordinates(), this.searchRadius));
						coverageFeature.set("type", "coverage");
						this.vectorLayer.getSource().addFeature(coverageFeature);
						if (showCoverageCenter) {
							sourceFeature.set("type", "coverage");
							this.vectorLayer.getSource().addFeature(sourceFeature);
						}
					}
					possibleFeatures.forEach(function(possibleFeature){
						possibleFeature.set("sourceType", source);
					});
					this.vectorLayer.getSource().addFeatures(possibleFeatures);
					if (possibleFeatures.length) {
						var activeFeature;
						if (source == "vms" || source == "vms-2022") {
							if (sourceFeature) {
								activeFeature = this.vectorLayer.getSource().getClosestFeatureToCoordinate(sourceFeature.getGeometry().getCoordinates(), function(f){
									return !(f.get("type") == "coverage");
								});
							} else {
								console.log("GET?"); // TODO...
							}
						} else {
							activeFeature = possibleFeatures[0];
						}
						this.setActiveFeature(activeFeature);
						this.addVectorLayerInteractivity();
					} else {
						this.panoramaFeature = "no-features";
						this.loading = false;
					}
				}
			},
			setActiveFeature: function(feature){
				feature = feature.clone();
				feature.set("active", true);
				this.panoramaFeature = feature;
			},
			onWindowResize: function(){
				this.resizeMly();
			},
			resizeMly: function(){
				setTimeout(function(){
					if (this.mly) {
						this.mly.resize();
					}
					if (this.viewer) {
						this.viewer.updateSize();
					}
					if (this.pSViewer) {
						this.pSViewer.resize();
					}
				}.bind(this), 0);
			},
			addVectorLayerInteractivity: function(){
				if (!this.mapClickListenerHandler) {
					this.mapClickListenerHandler = this.myMap.map.on("singleclick", this.mapClickListener);
				}
				if (!this.mapPointerMoveListenerHandler) {
					this.mapPointerMoveListenerHandler = this.myMap.map.on("pointermove", this.mapPointerMoveListener);
				}
			},
			removeVectorLayerInteractivity: function(){
				if (this.mapClickListenerHandler) {
					this.myMap.map.un("singleclick", this.mapClickListener);
					this.mapClickListenerHandler = null;
				}
				if (this.mapPointerMoveListenerHandler) {
					this.myMap.map.un("pointermove", this.mapPointerMoveListener);
					this.mapPointerMoveListenerHandler = null;
				}
			},
			mapClickListener: function(e){
				if (!this.myMap.interactivityDisabled) {
					var feature = this.getFeature(e.pixel);
					if (feature) {
						this.setActiveFeature(feature);
					}
				}
			},
			mapPointerMoveListener: function(e){
				if (!this.myMap.interactivityDisabled) {
					var r;
					if (!e.dragging) {
						r = this.getFeature(e.pixel);
					}
					if (r != this.highlightedFeature) {
						this.highlightedFeature = r;
					}
				}
			},
			getFeature: function(pixel){
				var feature = this.myMap.map.forEachFeatureAtPixel(pixel, function(feature, layer){
					if (layer && (layer == this.vectorLayer) && (feature.get("type") != "coverage")) {
						return feature;
					}
				}.bind(this), {hitTolerance: 5});
				return feature;
			},
			setPanoramaFeatureAngle: function(panoramaFeature, position){
				var anglePossible = false;
				if (anglePossible) {
					panoramaFeature.set("angle", 180 / Math.PI * position.longitude); // BIG TODO! Dar reikia pridėti pradinį taško bearing'ą? O koks jis???
				}
			},
			bearingToBasic: function(desiredBearing, nodeBearing) {
				// Paimta iš čia: https://github.com/mapillary/mapillary-js/issues/278
				var basic = (desiredBearing - nodeBearing) / 360 + 0.5;
				return this.wrap(basic, 0, 1);
			},
			wrap: function wrap(value, min, max) {
				// Paimta iš čia: https://github.com/mapillary/mapillary-js/issues/278
				var interval = (max - min);
				while (value > max || value < min) {
					if (value > max) {
						value = value - interval;
					} else if (value < min) {
						value = value + interval;
					}
				}
				return value;
			},
			getMapillaryV4Promise: function(url){
				var promise = new Promise(function(resolve, reject){
					CommonHelper.getFetchPromise(url, function(response){
						resolve(response);
					}, "GET", null, null, true).then(function(result){
						resolve(result);
					}.bind(this), function(){
						reject();
					}.bind(this));
				}.bind(this));
				return promise;
			},
			filterMapillaryV4Features: function(features, sourceFeature){
				var filteredFeatures = [],
					point,
					tempLineString,
					newFeature;
				features.forEach(function(feature){
					if (feature.getGeometry().getType() == "Point" && feature.get("is_pano")) {
						// https://openlayers.org/en/latest/apidoc/module-ol_render_Feature-RenderFeature.html
						if (feature.getFlatCoordinates) {
							point = new Point(feature.getFlatCoordinates()).transform("EPSG:3857", "EPSG:3346");
							tempLineString = new LineString([point.getCoordinates(), sourceFeature.getGeometry().getCoordinates()]);
							if (tempLineString.getLength() <= this.searchRadius) {
								newFeature = new Feature();
								newFeature.setProperties(feature.getProperties());
								newFeature.setGeometry(point);
								filteredFeatures.push(newFeature);
							}
						}
					}
				}.bind(this));
				return filteredFeatures;
			},
			exportPanorama4ActiveTask: function(){
				// https://github.com/mistic100/Photo-Sphere-Viewer/issues/193
				// https://github.com/mistic100/Photo-Sphere-Viewer/issues/462
				// !!! https://www.geoarchief.nl/psv/geoarchive_grab.html
				if (this.pSViewer && this.pSViewer.renderer && this.pSViewer.renderer.renderer) {
					var sourceCanvas = this.pSViewer.renderer.renderer.domElement;
					if (sourceCanvas.width > 0) {
						var resizeIfNeeded = true; // Lokalus config'as...
						if (resizeIfNeeded) {
							if (this.$refs.panoTemp) {
								this.exportInProgress = true;
								var width = this.$refs.pano.clientWidth,
									height = this.$refs.pano.clientHeight,
									cfg = this.pSViewer.config,
									destWidth = 1500,
									scaleRatio = destWidth / width;
								this.$refs.panoTemp.style.width = (width * scaleRatio) + "px";
								this.$refs.panoTemp.style.height = (height * scaleRatio) + "px";
								cfg.container = this.$refs.panoTemp;
								var tempPSViewer = new PhotoSphereViewer.Viewer(cfg);
								tempPSViewer.on("ready", function(){
									tempPSViewer.rotate(this.pSViewer.getPosition());
									tempPSViewer.zoom(this.pSViewer.getZoomLevel());
									tempPSViewer.renderer.render();
									this.$vBus.$emit("task-attachment-new", {
										src: tempPSViewer.renderer.renderer.domElement.toDataURL(),
										task: this.activeTask,
										tempAttachment: true
									});
									this.exportInProgress = false;
								}.bind(this));
							}
						} else {
							this.pSViewer.renderer.render();
							this.$vBus.$emit("task-attachment-new", {
								src: sourceCanvas.toDataURL(),
								task: this.activeTask,
								tempAttachment: true
							});
						}
					}
				}
			}
		},

		watch: {
			maximized: {
				immediate: true,
				handler: function(maximized){
					if (this.$refs.wrapper) {
						this.$refs.wrapper.maximized = maximized;
						this.resizeMly();
					}
				}
			},
			panoramaFeature: {
				immediate: true,
				handler: function(panoramaFeature, oldPanoramaFeature){
					this.nodeOnceChanged = false;
					if (this.vectorLayer && oldPanoramaFeature && (oldPanoramaFeature instanceof Feature)) {
						if (this.vectorLayer.getSource().hasFeature(oldPanoramaFeature)) {
							this.vectorLayer.getSource().removeFeature(oldPanoramaFeature);
						}
					}
					if (this.mly) {
						if (this.mly.remove) {
							this.mly.remove();
						} else {
							console.log("MLY REMOVE?", this.mly);
						}
						this.mly = null;
					}
					if (this.viewer) {
						this.viewer.destroy();
						this.viewer = null;
					}
					if (this.pSViewer) {
						this.pSViewer.destroy();
						this.pSViewer = null;
					}
					if (panoramaFeature && panoramaFeature != "error" && panoramaFeature != "no-features") {
						var featureSourceType = panoramaFeature.get("sourceType");
						if (featureSourceType == "mapillary") {
							if (!this.mly) {
								this.key += 1;
								setTimeout(function(){
									if (this.mapillaryV == "4") {
										// https://github.com/mapillary/mapillary-js
										var imageId = panoramaFeature.get("id");
										if (imageId && window.mapillary) {
											var { Viewer } = window.mapillary;
											this.mly = new Viewer({
												accessToken: CommonHelper.mapillaryV4ClientToken,
												container: "mly",
												imageId: imageId + "",
												component: {
													cover: false
												}
											});
											// this.mly.setFilter(["==", "ownerId", CommonHelper.mapillaryOrganizationId]); // BIG FIXME! Reikia kažkaip filtruoti, nes dabar rodo rodyklytes į visų vartotojų panoramas?
											// Gal pasitarnautų "creatorId", "creatorUsername", "ownerId"??? VMS nuotraukose šios reikšmės lyg ir tuščios...
											this.mly.on(
												"load",
												function(){
													this.loading = false;
												}.bind(this)
											);
											this.mly.on(
												"image", // "Fired every time the viewer navigates to a new image."
												function(e){
													panoramaFeature.setGeometry(new Point([e.image.computedLngLat.lng, e.image.computedLngLat.lat]).transform("EPSG:4326", "EPSG:3346"));
													if (!this.nodeOnceChanged) {
														this.nodeOnceChanged = true;
														var desiredBearing;
														if (this.lookAt) {
															var featureCoordinates = this.feature.getGeometry().getCoordinates(),
																panoramaFeatureCoordinates = panoramaFeature.getGeometry().getCoordinates();
															desiredBearing = Math.atan2(
																panoramaFeatureCoordinates[0] - featureCoordinates[0],
																panoramaFeatureCoordinates[1] - featureCoordinates[1]
															);
															desiredBearing = desiredBearing * 180 / Math.PI + 180;
														} else {
															desiredBearing = e.image.computedCompassAngle + 180;
														}
														this.mly.setCenter([this.bearingToBasic(desiredBearing, e.image.computedCompassAngle), 0.5]);
													}
												}.bind(this)
											);
											this.mly.on(
												"bearing",
												function(e){
													panoramaFeature.set("angle", e.bearing);
												}.bind(this)
											);
										}
									}
								}.bind(this), 0);
							}
						} else if (featureSourceType == "vms" || featureSourceType == "vms-2022") {
							// FIXME! Prie panoramos kelio pridedamas Data.now() suknisa eksportuojamo paveikslėlio pavadinimą?..
							// Eksportavimo logiką galima pažiūrėti faile -> app\node_modules\photo-sphere-viewer\dist\photo-sphere-viewer.js
							// Ta logika yra labai paprasta -> `link.download = this.psv.config.panorama;`
							// Gali tekti kurti savo mygtuką?.. https://photo-sphere-viewer.js.org/guide/navbar.html#custom-buttons
							var panoramaRoot = "https://vppub.blob.core.windows.net/pano/" || "https://zemelapiai.vplanas.lt/isorei/kz/";
							/*
							if (window.location.search == "?mode=pano-test") {
								panoramaRoot = "https://vppub.blob.core.windows.net/pano/";
							}
							*/
							var panoramaPath = panoramaRoot + panoramaFeature.get("FolderName") + "/" + panoramaFeature.get("ImageName") + ".jpg?" + Date.now();
							if (location.hostname == "localhost" || window.location.search == "?mode=test") {
								// Testavimui:
								// "@/assets/pano_000014_000047.jpg"
								// "@/assets/003483-20201002-121227-000001233-Motion.jpg"
								panoramaPath = require("@/assets/nauj-test.jpg");
							}
							setTimeout(function(){
								var useMarzipano = false;
								if (useMarzipano) {
									// TODO! Dabar reikalas neišbaigtas!!!
									var Marzipano = require("marzipano");
									this.viewer = new Marzipano.Viewer(this.$refs.pano);
									var source = Marzipano.ImageUrlSource.fromString(panoramaPath),
										width = 8000;
									var geometry = new Marzipano.EquirectGeometry([{
										width: width
									}]);
									var limiter = Marzipano.RectilinearView.limit.traditional(width, 100 * Math.PI / 180);
									var view = new Marzipano.RectilinearView({
										yaw: Math.PI
									}, limiter);
									var scene = this.viewer.createScene({
										source: source,
										geometry: geometry,
										view: view
									});
									scene.addEventListener("viewChange", function(){
										// Gal čia kas?..
									});
									scene.switchTo();
									this.loading = false;
								} else {
									this.pSViewer = new PhotoSphereViewer.Viewer({
										container: this.$refs.pano,
										panorama: panoramaPath,
										lang: {
											autorotate: "Automatinis sukimasis",
											zoom: "Zoom", // FIXME: i18n
											zoomOut: "Tolinti",
											zoomIn: "Artinti",
											download: "Atsisiųsti",
											fullscreen: "Per visą ekraną",
											menu: "Meniu",
											twoFingers: ["Use two fingers to navigate"], // FIXME: i18n
											loadError : "Nuotrauka negali būti pakrauta..."
										},
										loadingTxt: "Kraunasi...",
										sphereCorrection: {
											pan: 180 * (Math.PI / 180),
											tilt: 0,
											roll: 0
										}
									});
									this.pSViewer.on("ready", function(){
										this.loading = false;
									}.bind(this));
									var initialPosition = this.pSViewer.getPosition();
									console.log("Koks pradinis pasukimo kampas?", panoramaFeature); // TODO! Išsiaiškinti kuris atributas yra aktualus...
									this.setPanoramaFeatureAngle(panoramaFeature, initialPosition);
									this.pSViewer.on("position-updated", function(e, position){
										this.setPanoramaFeatureAngle(panoramaFeature, position);
									}.bind(this));
								}
							}.bind(this), 0);
						}
						this.vectorLayer.getSource().addFeature(panoramaFeature);
					}
				}
			},
			highlightedFeature: {
				immediate: true,
				handler: function(highlightedFeature){
					var visualize = false;
					if (visualize) {
						console.log("HF", highlightedFeature); // TODO...
					}
				}
			}
		}
	}
</script>

<style scoped>
	.panorama-content {
		min-width: 450px;
		width: 100%;
		position: relative;
	}
	.v-progress-circular {
		position: absolute;
	}
	.panorama-content-temp {
		position: absolute;
	}
</style>