<template>
	<div class="d-flex flex-column align-end pa-1">
		<div>
			<v-btn-toggle
				v-model="activeButton"
				dense
			>
				<template v-for="(button, i) in buttons">
					<v-tooltip bottom :key="i">
						<template v-slot:activator="{on, attrs}">
							<v-btn
								small
								:value="button.value"
								v-bind="attrs"
								v-on="on"
								:disabled="button.value == 'vertical-street-sign'"
							>
								<template v-if="button.title">
									{{button.title}}
								</template>
								<template v-else-if="button.icon">
									<v-icon>{{button.icon}}</v-icon>
								</template>
							</v-btn>
						</template>
						<span>{{button.tooltip}}</span>
					</v-tooltip>
				</template>
			</v-btn-toggle>
		</div>
		<div :class="['mt-1 overflow-y slot-wrapper', activeFeature ? 'd-none' : null]">
			<slot></slot>
		</div>
		<FeaturePopup
			:activeFeature="activeFeature"
			:setActiveFeature="setActiveFeature"
			:destroyFeature="destroyFeature"
			:addModInteraction="addModInteraction"
			:removeModInteraction="removeModInteraction"
			:map="map"
			class="mt-1"
			ref="featurePopup"
		/>
	</div>
</template>

<script>
	import {Draw, Snap} from "ol/interaction";
	import Circle from "ol/geom/Circle";
	import Feature from "ol/Feature";
	import FeatureInteraction from "./FeatureInteraction.js";
	import FeaturePopup from "./FeaturePopup";
	import LineString from "ol/geom/LineString";
	import Overlay from "ol/Overlay";
	import Point from "ol/geom/Point";
	import Polygon from "ol/geom/Polygon";
	import TaskMasterAttachmentMapHelper from "../helpers/TaskMasterAttachmentMapHelper";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import {Circle as CircleStyle, Fill, Stroke, Style} from "ol/style";
	import {getCenter as getExtentCenter} from "ol/extent";
	import {fromExtent} from "ol/geom/Polygon";

	export default {
		data: function(){
			var data = {
				buttons: [{
					value: "point",
					icon: "mdi-circle-small",
					tooltip: "Sukurti taškinį objektą"
				},{
					value: "line",
					icon: "mdi-vector-line",
					tooltip: "Brėžti liniją"
				},{
					value: "polygon",
					icon: "mdi-vector-polygon",
					tooltip: "Brėžti plotinį objektą"
				},{
					value: "rectangle",
					icon: "mdi-rectangle-outline",
					tooltip: "Brėžti stačiakampį"
				},{
					value: "circle",
					icon: "mdi-circle-outline",
					tooltip: "Brėžti apskritimą"
				},{
					value: "text",
					title: "Tekstas",
					tooltip: "Sukurti tekstinį objektą"
				},{
					value: "arrow",
					title: "Rodyklė",
					tooltip: "Brėžti rodyklę"
				},{
					value: "vertical-street-sign",
					title: "Vertikalusis KŽ",
					tooltip: "Sukurti vertikaliojo kelio ženklo objektą"
				}],
				activeButton: null,
				activeFeature: null,
				highlightedFeature: null,
				featuresLayerZIndex: 1
			};
			return data;
		},

		props: {
			map: Object,
			contentData: Object,
			tooltipRef: HTMLDivElement,
			images: Array,
			task: Object
		},

		components: {
			FeaturePopup
		},

		mounted: function(){
			if (this.map) {
				this.createBoundary();
				this.createFeaturesLayer(); // LAIKINAI užkomentuojame...
				this.createProgressIndicator(); // Indikatorius naudojamas nuotraukų įkėlimo į žemėlapį metu...
				var initialFeatures = [];
				if (this.contentData && this.contentData.features) {
					this.contentData.features.forEach(function(f){
						if (f.type == "boundary") {
							if (this.boundaryLayer) {
								this.boundaryLayer.getSource().clear(true);
								this.addBoundaryFeatureToBoundaryLayer(f["frame-size"]);
							}
						} else {
							var feature = new Feature();
							if (f.geometry && f.geometry.type && f.geometry.coordinates) {
								switch (f.geometry.type) {
									case "Point":
										feature.setGeometry(new Point(f.geometry.coordinates));
										break;
									case "LineString":
										feature.setGeometry(new LineString(f.geometry.coordinates));
										break;
									case "Polygon":
										feature.setGeometry(new Polygon(f.geometry.coordinates));
										break;
									case "Circle":
										if (f.geometry.radius) {
											feature.setGeometry(new Circle(f.geometry.coordinates, f.geometry.radius));
										}
										break;
								}
							}
							delete f.geometry;
							feature.setProperties(f);
							if (feature.get("type") == "image-mock-polygon") {
								var attachmentId = feature.get("attachment-id");
								if (this.images && (Array.isArray(this.images))) {
									var matchedImage;
									this.images.some(function(image){
										if (image.id == attachmentId) {
											matchedImage = image;
											return true;
										}
									});
									if (matchedImage) {
										var img = new Image(),
											that = this;
										img.onload = function(){
											feature.set("img", img);
											feature.set("attachment-id", attachmentId);
											setTimeout(function(){
												that.featuresLayer.changed(); // Čia šioks toks hack'as... Esmė, kad kažkodėl iškart nuotrauka nenusipiešia poligone? Nusipieštų jei pvz. ant poligono užvestume pelės kursoriumi...
											}.bind(this), 0);
										}
										img.src = matchedImage.src;
										if (feature.get("rotation-angle")) {
											TaskMasterAttachmentMapHelper.rotateImageMockPolygon(feature, feature.get("rotation-angle"));
										}
										if (f.clipFeatures) {
											feature.clipFeatures = [];
											f.clipFeatures.forEach(function(clipFeature){
												clipFeature = new Feature(new Polygon(clipFeature));
												feature.clipFeatures.push(clipFeature);
											});
										}
										feature.set("img", img);
									} else {
										console.log("TODO! Kažkaip simbolizuoti tą poligoną! Gal įsitrižomis raudonomis linijomis? Popup'e pateikti info, kad nėra nuotraukos?.."); // TODO...
									}
								}
							}
							initialFeatures.push(feature);
						}
					}.bind(this));
				}
				if (this.featuresLayer) {
					this.featuresLayer.getSource().addFeatures(initialFeatures);
				}
				this.makeMapInteractive();
			}
		},

		methods: {
			createBoundary: function(){
				if (!this.boundaryLayer) {
					this.boundaryLayer = new VectorLayer({
						source: new VectorSource(),
						style: TaskMasterAttachmentMapHelper.getFeaturesLayerStyle(),
						zIndex: this.featuresLayerZIndex + 1
					});
					this.map.addLayer(this.boundaryLayer);
				}
				this.addBoundaryFeatureToBoundaryLayer();
			},
			addBoundaryFeatureToBoundaryLayer: function(frameSizeType){
				if (!frameSizeType) {
					frameSizeType = "a3-landscape";
				}
				if (this.boundaryLayer) {
					var extent = this.map.getView().getProjection().getExtent(),
						extentCenter = [(extent[0] + extent[2]) / 2, (extent[1] + extent[3]) / 2];
					var feature = new Feature({
						geometry: TaskMasterAttachmentMapHelper.getBoundaryGeometry(extentCenter, frameSizeType),
						type: "boundary" // `Custom`, simbolizacijai...
					});
					feature.set("frame-size", frameSizeType);
					feature.set("stroke-width", 4);
					var boundaryOutsideFeature = new Feature({
						geometry: TaskMasterAttachmentMapHelper.getBoundaryOutsideGeometry(this.map, feature.getGeometry().getCoordinates()),
						type: "boundary-outside", // `Custom`, simbolizacijai...
						ignore: true
					})
					feature.boundaryOutsideFeature = boundaryOutsideFeature;
					this.boundaryLayer.getSource().addFeatures([boundaryOutsideFeature, feature]);
					setTimeout(function(){
						this.map.getView().fit(feature.getGeometry().getExtent(), {
							padding: [30, 30, 30, 30]
						});
					}.bind(this), 0);
					this.boundaryFeature = feature;
				}
			},
			createFeaturesLayer: function(){
				if (!this.featuresLayer) {
					this.featuresLayer = new VectorLayer({
						source: new VectorSource(),
						style: TaskMasterAttachmentMapHelper.getFeaturesLayerStyle(this.map),
						zIndex: this.featuresLayerZIndex,
						renderBuffer: 500 // FIXME! Ar bus neigiamų pasekmių, jei padidinsime?> Pagal nutylėjimą yra 100... Viliamės, kad šitos reikšmės padidinimas padės išgauti geresnį tekstinių simbolių hit-detection'ą...
					});
					this.map.addLayer(this.featuresLayer);
				}
			},
			addInteraction: function(type){
				if (!this.drawInteraction && this.map) {
					this.createFeaturesLayer();
					var drawType;
					switch (type) {
						case "point":
						case "text":
							drawType = "Point";
							break;
						case "line":
							drawType = "LineString";
							break;
						case "polygon":
							drawType = "Polygon";
							break;
						case "rectangle":
							drawType = "Rectangle";
							break;
						case "circle":
							drawType = "Circle";
							break;
						case "arrow":
							drawType = "Arrow";
							break;
					}
					if (drawType) {
						this.activeFeature = null;
						if (drawType == "Rectangle") {
							this.drawInteraction = new Draw({
								type: "LineString",
								source: this.featuresLayer.getSource(),
								geometryFunction: TaskMasterAttachmentMapHelper.getRectangleGeometryFunction(),
								maxPoints: 2,
								stopClick: true
							});
						} else if (drawType == "Arrow") {
							this.drawInteraction = new Draw({
								type: "LineString",
								source: this.featuresLayer.getSource(),
								maxPoints: 2,
								stopClick: true
							});
						} else {
							var drawStyle;
							if (type == "text") {
								// Čia įdomus pvz. -> https://openlayers.org/en/latest/examples/custom-hit-detection-renderer.html
								drawStyle = function(feature, resolution){
									return TaskMasterAttachmentMapHelper.getTextStyle(feature, resolution);
								};
							}
							this.drawInteraction = new Draw({
								type: drawType,
								source: this.featuresLayer.getSource(),
								stopClick: true,
								style: drawStyle
							});
						}
						if (this.drawInteraction) {
							this.drawInteraction.on("drawend", function(e){
								e.feature.set("type", type);
								e.feature.set("z-index", this.getNextFeatureZIndex());
								var color = [0, 0, 0, 0.7],
									invisibleColor = [255, 255, 255, 0.01];
								if (type == "circle" || type == "rectangle") {
									e.feature.set("fill-color", invisibleColor);
								} else if (type == "polygon") {
									e.feature.set("fill-color", [0, 0, 0, 0.3]);
								} else {
									if ((type != "line") || (type != "arrow")) {
										e.feature.set("fill-color", color);
									}
								}
								e.feature.set("stroke-color", color);
								if (type == "arrow") {
									e.feature.set("stroke-width", 8);
									e.feature.set("arrow-head-size", Math.round(e.feature.getGeometry().getLength() * 0.2));
								} else {
									e.feature.set("stroke-width", 5);
								}
								if (type == "point") {
									e.feature.set("point-type", "circle");
									e.feature.set("point-size", 10);
								}
								this.setActiveFeature(e.feature);
								this.activeButton = null;
							}.bind(this));
							this.map.addInteraction(this.drawInteraction);
							if (!this.snapInteraction) {
								this.snapInteraction = new Snap({
									source: this.featuresLayer.getSource() // Hmmmmm... O gal šio snap'inimo ir nereikia?.. Ar aktualu snap'inti vieną objektą prie kito?..
								});
								this.map.addInteraction(this.snapInteraction);
							}
							if (this.boundaryLayer) {
								if (!this.boundarySnapInteraction) {
									this.boundarySnapInteraction = new Snap({
										source: this.boundaryLayer.getSource()
									});
									this.map.addInteraction(this.boundarySnapInteraction);
								}
							}
						}
					}
				}
			},
			removeInteraction: function(){
				if (this.drawInteraction) {
					if (this.map) {
						this.map.removeInteraction(this.drawInteraction);
					}
					this.drawInteraction = null;
				}
				if (this.snapInteraction) {
					if (this.map) {
						this.map.removeInteraction(this.snapInteraction);
					}
					this.snapInteraction = null;
				}
				if (this.boundarySnapInteraction) {
					if (this.map) {
						this.map.removeInteraction(this.boundarySnapInteraction);
					}
					this.boundarySnapInteraction = null;
				}
			},
			makeMapInteractive: function(){
				this.highlightedFeatureVectorLayer = this.getHighlightedFeatureVectorLayer("rgba(52, 212, 255, $opacity)");
				this.map.addLayer(this.highlightedFeatureVectorLayer);
				this.map.on("pointermove", function(e){
					// console.log("POINTER", e.coordinate); // TESTAVIMUI...
					var r;
					if (!e.dragging) {
						r = this.getFeature(e.pixel, "pointermove");
					}
					if (r != this.highlightedFeature) {
						this.highlightedFeature = r;
					}
					if (r) {
						this.showTooltip(e.pixel, r);
					} else {
						this.hideTooltip();
					}
				}.bind(this));
				this.map.on("singleclick", function(e){
					var feature = this.getFeature(e.pixel);
					if (feature) {
						this.setActiveFeature(feature);
					} else {
						this.setActiveFeature(); // Nežinau ar iš `usability` pusės tai yra gerai... Bet taip pvz. ESRI daro ir lyg visai patogu...
					}
				}.bind(this));
			},
			getHighlightedFeatureVectorLayer: function(color){
				if (!color) {
					color = "rgba(255, 0, 0, $opacity)";
				}
				var layer = new VectorLayer({
					source: new VectorSource(),
					style: function(feature, resolution){
						// Hmmmm... TODO? O gal simbolizacija turi būti panaši kaip ir aktyvaus ("active") simbolio?..
						var type = feature.get("type"),
							style;
						if (type == "text") {
							style = TaskMasterAttachmentMapHelper.getTextStyle(feature, resolution, true); // FIXME? Reiktų patobulinti... Dabar nelabai sąmoningai, nes uždeda ta patį tekstą ant kito teksto... O gal ir ok? :)
						} else if (type == "arrow") {
							style = TaskMasterAttachmentMapHelper.getArrowStyle(feature, resolution, null, true);
						} else {
							var strokeWidth = (feature.get("stroke-width") || 1) / resolution;
							if (strokeWidth < 2) {
								strokeWidth = 2;
							}
							style = new Style({
								stroke: new Stroke({
									color: color.replace("$opacity", 1),
									width: strokeWidth,
									lineCap: "butt"
								}),
								fill: new Fill({
									color: color.replace("$opacity", type == "image-mock-polygon" ? 0.05 : 0.4)
								}),
								image: new CircleStyle({
									radius: 10,
									fill: new Fill({
										color: color.replace("$opacity", 0.7)
									})
								})
							});
						}
						return style;
					},
					zIndex: this.featuresLayerZIndex + 2
				});
				return layer;
			},
			getFeature: function(pixel){
				var feature = this.map.forEachFeatureAtPixel(pixel, function(feature, layer){
					if (layer && ((layer == this.featuresLayer) || (layer == this.boundaryLayer))) {
						if (!feature.get("ignore")) {
							return feature;
						}
					}
				}.bind(this), {hitTolerance: 5});
				return feature;
			},
			setActiveFeature: function(feature){
				this.activeFeature = feature;
			},
			destroyFeature: function(feature){
				if (feature) {
					if (this.featuresLayer) {
						var source = this.featuresLayer.getSource();
						if (source) {
							this.$vBus.$emit("confirm", {
								title: "Ar tikrai šalinti objektą?",
								message: "Ar tikrai šalinti objektą?",
								positiveActionTitle: "Šalinti objektą",
								negativeActionTitle: "Atšaukti",
								positive: function(){
									source.removeFeature(feature);
									this.setActiveFeature();
								}.bind(this)
							});
						}
					}
				}
			},
			addImage: function(src, attachmentId, key){
				if (this.boundaryFeature) {
					// Esmė tokia, kad pridedamą nuotrauką/piešinuką keliame į patį rėmelio centrą... TODO: O gal tiesiog į matomo vaizdo centrą kelti??? Jei taip, tai ir extent'o keisti nereiktų...
					var boundaryExtent = this.boundaryFeature.getGeometry().getExtent(),
						boundaryCenter = getExtentCenter(boundaryExtent);
					this.showProgressIndicator(boundaryCenter);
					this.map.getView().fit(boundaryExtent, { // To reikia, nes jei būsime kažkur stambiame mastelyje, kur nors šone, tai galime net nepamatyti kaip prisidės piešinukas...
						padding: [30, 30, 30, 30]
					});
					var img = new Image(),
						that = this;
					img.onload = function(){
						// TODO: reiktų kažkaip protingai paskaičiuoti extent'ą?... Kad tilptų į rėmelio ribas, bet kad nebūtų per mažas...
						var extent = [
							boundaryCenter[0] - this.width / 2,
							boundaryCenter[1] - this.height / 2,
							boundaryCenter[0] + this.width / 2,
							boundaryCenter[1] + this.height / 2
						];
						var feature = new Feature({
							geometry: fromExtent(extent),
							type: "image-mock-polygon" // `Custom`, simbolizacijai...
						});
						feature.set("z-index", that.getNextFeatureZIndex());
						feature.set("stroke-color", [0, 0, 0, 0.7]);
						feature.set("stroke-width", 5);
						feature.set("img", img);
						if (key) {
							feature.set("img-key", key);
						}
						feature.set("attachment-id", attachmentId);
						that.featuresLayer.getSource().addFeature(feature);
						that.hideProgressIndicator();
						that.setActiveFeature(feature);
					}
					img.onerror = function(){
						that.hideProgressIndicator();
					}
					img.src = src;
				}
			},
			createProgressIndicator: function(){
				var div = document.createElement("div");
				div.className = "identify-progress";
				this.overlay = new Overlay({
					element: div,
					position: null,
					positioning: "center-center",
					stopEvent: false
				});
				this.map.addOverlay(this.overlay);
			},
			showProgressIndicator: function(coordinate){
				if (this.overlay) {
					this.overlay.setPosition(coordinate);
					this.overlay.element.style.display = "";
				}
			},
			hideProgressIndicator: function(){
				if (this.overlay) {
					this.overlay.element.style.display = "none";
				}
			},
			showAdditionalMenu: function(){
				console.log("showAdditionalMenu"); // Jei turime reikalų su tekstu, tai būtų galima pateikti kažkokį input'ą, į kurį būtų galima įvesti tkesto reikšmę dar nesukūrus pačio objekto?..
			},
			hideAdditionalMenu: function(){
				console.log("hideAdditionalMenu");
			},
			getNextFeatureZIndex: function(){
				var zIndex = 0;
				if (this.featuresLayer) {
					this.featuresLayer.getSource().getFeatures().forEach(function(feature){
						var zI = parseInt(feature.get("z-index"));
						if (Number.isInteger(zI)) {
							if (zI > zIndex) {
								zIndex = zI;
							}
						}
					});
				}
				zIndex += 1;
				return zIndex;
			},
			showTooltip: function(c, feature){
				var tooltip = this.tooltipRef;
				if (tooltip) {
					var tooltipContent = "<span>" + feature.get("type") + "</span>";
					tooltipContent = null; // LAIKINAI! Kol turinys nesugalvotas...
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
				if (this.tooltipRef) {
					Object.assign(this.tooltipRef.style, {
						display: "none"
					});
				}
			},
			activateInteraction: function(feature){
				this.deactivateInteraction();
				if (!this.featureInteraction) {
					this.featureInteraction = new FeatureInteraction({
						feature: feature,
						map: this.map
					});
					this.map.addInteraction(this.featureInteraction);
				}
			},
			deactivateInteraction: function(){
				if (this.featureInteraction) {
					this.featureInteraction.destroy();
					this.map.removeInteraction(this.featureInteraction);
					this.featureInteraction = null;
				}
				this.removeModInteraction();
			},
			getDataUrl: function(){
				var promise = new Promise(function(resolve, reject){
					var src = TaskMasterAttachmentMapHelper.getMasterAttachmentSrc(this.boundaryLayer, this.featuresLayer, this.map);
					if (src) {
						resolve(src);
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			},
			getAllData: function(){
				var d = {
					features: []
				};
				if (this.boundaryLayer) {
					this.boundaryLayer.getSource().getFeatures().forEach(function(feature){
						if (feature.get("type") == "boundary") {
							d.features.push(this.getFeatureToSave(feature));
						}
					}.bind(this));
				}
				if (this.featuresLayer) {
					this.featuresLayer.getSource().getFeatures().forEach(function(feature){
						d.features.push(this.getFeatureToSave(feature));
					}.bind(this));
				}
				var promise = new Promise(function(resolve){
					this.getDataUrl().then(function(url){
						var data = {
							dataURL: url,
							data: d,
							images: this.images,
							task: this.task
						};
						resolve(data);
					}.bind(this));
				}.bind(this));
				return promise;
			},
			getFeatureToSave: function(feature){
				var geom = feature.getGeometry();
				if (geom) {
					var f = feature.clone(),
						g;
					switch (geom.getType()) {
						case "Point":
						case "LineString":
						case "Polygon":
							g = {
								type: geom.getType(),
								coordinates: geom.getCoordinates()
							};
							break;
						case "Circle":
							g = {
								type: geom.getType(),
								coordinates: geom.getCenter(),
								radius: geom.getRadius()
							};
							break;
					}
					f = feature.getProperties();
					delete f.geometry;
					delete f.active;
					delete f.img;
					if (feature.get("type") == "boundary") {
						delete f["stroke-width"];
					} else {
						f.geometry = g;
					}
					if (feature.clipFeatures) {
						f.clipFeatures = [];
						feature.clipFeatures.forEach(function(clipFeature){
							f.clipFeatures.push(clipFeature.getGeometry().getCoordinates());
						});
					}
					return f;
				}
			},
			addModInteraction: function(type){
				if (type == "mask-geometry-editing") {
					if (!this.modInteraction) {
						if (!this.tempFeaturesLayer) {
							this.tempFeaturesLayer = new VectorLayer({
								source: new VectorSource(),
								zIndex: this.featuresLayerZIndex,
								style: null // Kad objektai būtų nematomi...
							});
							this.map.addLayer(this.tempFeaturesLayer);
						}
						this.modInteraction = new Draw({
							type: "Polygon",
							source: this.tempFeaturesLayer.getSource(),
							stopClick: true
						});
						this.modInteraction.on("drawend", function(e){
							if (this.activeFeature) {
								if (!this.activeFeature.clipFeatures) {
									this.activeFeature.clipFeatures = [];
								} else {
									this.activeFeature.clipFeatures = []; // Ar tinkamas funkcionalumas?..
								}
								if (this.activeFeature.rotationInfo) {
									e.feature.getGeometry().rotate(- this.activeFeature.rotationInfo.angle, getExtentCenter(this.activeFeature.getGeometry().getExtent()));
								}
								this.activeFeature.clipFeatures.push(e.feature);
							}
							if (this.$refs.featurePopup) {
								this.$refs.featurePopup.maskEditingActive = false;
								this.$refs.featurePopup.maskFeaturesExist = true;
							}
						}.bind(this));
						if (this.map) {
							this.map.addInteraction(this.modInteraction);
							if (this.featureInteraction) {
								this.featureInteraction.pause();
							}
						}
					}
				}
			},
			removeModInteraction: function(){
				if (this.modInteraction) {
					if (this.map) {
						this.map.removeInteraction(this.modInteraction);
						if (this.featureInteraction) {
							this.featureInteraction.resume();
						}
					}
					this.modInteraction = null;
				}
			}
		},

		watch: {
			activeButton: {
				immediate: false,
				handler: function(type){
					this.removeInteraction();
					this.hideAdditionalMenu();
					if (type) {
						if (type == "text") {
							this.showAdditionalMenu(type);
						}
						this.addInteraction(type);
					}
				}
			},
			highlightedFeature: {
				immediate: true,
				handler: function(highlightedFeature){
					if (this.highlightedFeatureVectorLayer) {
						this.highlightedFeatureVectorLayer.getSource().clear(true);
						if (highlightedFeature) {
							this.highlightedFeatureVectorLayer.getSource().addFeature(highlightedFeature);
						}
					}
				}
			},
			activeFeature: {
				immediate: false,
				handler: function(activeFeature, previousActiveFeature){
					this.deactivateInteraction();
					if (activeFeature) {
						activeFeature.set("active", true);
						this.activateInteraction(activeFeature);
					}
					if (previousActiveFeature) {
						previousActiveFeature.set("active", false);
					}
				}
			}
		}
	}
</script>

<style scoped>
	.slot-wrapper {
		max-height: 100%;
	}
</style>