<template>
	<OverMapButton
		:title="title"
		icon="mdi-blur"
		:clickCallback="onClick"
		:activeChangeCallback="onActiveChange"
		ref="btn"
	/>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import Draw from "ol/interaction/Draw";
	import OverMapButton from "./OverMapButton";
	import Select from "ol/interaction/Select";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import EsriJSON from "ol/format/EsriJSON";
	import {Point, Polygon, LineString} from "ol/geom";
	import {Circle as CircleStyle, Fill, Stroke, Style} from "ol/style";
	import {booleanContains, polygon as turfPolygon, lineString as turfLineString} from "@turf/turf";

	export default {
		data: function(){
			var data = {
				title: "Pateikti pažymėtų objektų statistiką",
				type: null,
				selectedFeatures: null
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		created: function(){
			this.$vBus.$on("deactivate-interactions", this.deactivateInteraction);
		},

		beforeDestroy: function(){
			this.$vBus.$off("deactivate-interactions", this.deactivateInteraction);
		},

		components: {
			OverMapButton
		},

		methods: {
			onClick: function(){
				this.$refs.btn.needsAttention = this.$refs.btn.active = !this.$refs.btn.active;
			},
			onActiveChange: function(active){ // T. y. kviečiamas, kai keičiasi mygtuko "active" reikšmė, t. y. mygtukas yra toggle'inamas...
				if (this.$refs.btn) {
					this.$refs.btn.needsAttention = this.$refs.btn.active = active;
					if (active) {
						// this.addInteraction();
					} else {
						// this.removeInteraction();
					}
					this.$vBus.$emit("show-or-hide-painted-area-summary-settings", {
						btn: this.$refs.btn,
						state: active,
						btnComponent: this
					});
				}
			},
			addDrawInteraction: function(){
				this.$vBus.$emit("deactivate-interactions", "painted-area-summary");
				if (!this.vectorLayer) {
					this.vectorLayer = new VectorLayer({
						source: new VectorSource(),
						zIndex: 1002,
						style: new Style({
							stroke: new Stroke({
								width: 3,
								color: "black"
							}),
							fill: new Fill({
								color: "rgba(255, 255, 255, 0.2)"
							})
						})
					});
					this.myMap.map.addLayer(this.vectorLayer);
				}
				if (!this.analysisResultsVectorLayer) {
					this.analysisResultsVectorLayer = new VectorLayer({
						source: new VectorSource(),
						zIndex: 1003,
						style: function(feature){
							var color = feature.get("out") ? "red" : "green";
							return new Style({
								stroke: new Stroke({
									color: color,
									width: 2
								}),
								fill: new Fill({
									color: color
								}),
								image: new CircleStyle({
									radius: 5,
									fill: new Fill({
										color: color
									})
								})
							});
						}
					});
					this.myMap.map.addLayer(this.analysisResultsVectorLayer);
				}
				if (!this.interaction) {
					this.interaction = new Draw({
						type: "Polygon",
						stopClick: true
					});
					this.interaction.on("drawend", function(e){
						this.boundsFeature = e.feature;
						this.vectorLayer.getSource().clear();
						this.vectorLayer.getSource().addFeature(e.feature);
						if (this.analysisResultsVectorLayer) {
							this.analysisResultsVectorLayer.getSource().clear();
						}
						this.$vBus.$emit("show-point-painted-area-summary", {
							title: this.title,
							feature: e.feature,
							source: this.$refs.btn.source
						});
						this.$vBus.$emit("on-painted-area-summary-bounds", e);
					}.bind(this));
					this.myMap.addInteraction(this.interaction);
					this.myMap.helpMessage = "Pažymėkite teritorijos ribas";
					this.myMap.addPointerMoveHandler();
				}
			},
			addSelectInteraction: function(){
				this.$vBus.$emit("deactivate-interactions", "painted-area-summary");
				if (!this.interaction) {
					this.interaction = new Select({
						multi: true
					});
					this.myMap.addInteraction(this.interaction);
				}
			},
			removeInteraction: function(){
				if (this.vectorLayer) {
					this.myMap.map.removeLayer(this.vectorLayer);
					this.vectorLayer = null;
				}
				if (this.analysisResultsVectorLayer) {
					this.myMap.map.removeLayer(this.analysisResultsVectorLayer);
					this.analysisResultsVectorLayer = null;
				}
				if (this.interaction) {
					this.myMap.removeInteraction(this.interaction);
					this.interaction = null;
					this.myMap.removePointerMoveHandler();
				}
			},
			deactivateInteraction: function(type){
				if (type != "painted-area-summary") {
					if (this.$refs.btn) {
						this.$refs.btn.needsAttention = this.$refs.btn.active = false;
					}
				}
			},
			selectFeaturesByExtent: function(){
				// Galime kreiptis į /identify...
				var feature = this.boundsFeature;
				var selectType = "local";
				if (selectType == "service-query") {
					var esriJsonFormat = new EsriJSON();
					var queryFeature = esriJsonFormat.writeFeatureObject(feature, {
						featureProjection: this.myMap.map.getView().getProjection()
					});
					var url = this.$store.getters.getServiceUrl("street-signs");
					var layerDefs = [{
						layerId: 2, // Horizantalūs_KZ_T
						where: "1=1",
						outFields: "*"
					},{
						layerId: 3, // Horizantalūs_KZ_L
						where: "1=1",
						outFields: "*"
					},{
						layerId: 4, // Horizantalūs_KZ_P
						where: "1=1",
						outFields: "*"
					}];
					var params = {
						f: "json",
						layerDefs: JSON.stringify(layerDefs),
						geometry: JSON.stringify(queryFeature.geometry) // Neveikia??? Nepanaudojamas parametras?.. Dėl to atsisakau funkcionalumo...
					};
					url = CommonHelper.prependProxyIfNeeded(url) + "/query";
					CommonHelper.getFetchPromise(url, function(json){
						return json;
					}.bind(this), "POST", params).then(function(result){
						console.log("RESULT...", result); // TODO...
					}.bind(this), function(){
						console.log("ERR"); // TODO...
					});
					// Deja, nepasiteisino... Nes neveikia `query` parametras?..
				} else if (selectType == "local") {
					var selectedFeatures = [];
					// https://openlayers.org/en/latest/examples/box-selection.html
					this.myMap.map.getLayers().forEach(function(layer){
						if (layer.service) {
							if (layer.service.id == "street-signs") {
								if (layer.getLayers) {
									layer.getLayers().forEach(function(l){
										if (l.getLayers) {
											// ...
										} else {
											l.getSource().forEachFeatureIntersectingExtent(feature.getGeometry().getExtent(), function(f){
												selectedFeatures.push(f);
											});
										}
									});
								}
							}
						}
					});
					this.selectedFeatures = selectedFeatures;
				}
			},
			executeAnalysis: function(){
				if (this.analysisResultsVectorLayer) {
					this.analysisResultsVectorLayer.getSource().clear();
				}
				// https://developers.arcgis.com/rest/services-reference/enterprise/cut.htm
				this.selectFeaturesByExtent();
				if (this.selectedFeatures) {
					var f = {
						points: [],
						polylines: [],
						polygons: []
					};
					var g;
					this.selectedFeatures.forEach(function(feature){
						g = feature.getGeometry();
						if (g instanceof Polygon) {
							f.polygons.push(feature);
						} else if (g instanceof LineString) {
							f.polylines.push(feature);
						} else if (g instanceof Point) {
							if (this.boundsFeature.getGeometry().intersectsCoordinate(g.getCoordinates())) {
								f.points.push(feature);
							}
						}
					}.bind(this));
					var promises = [];
					if (f.polylines.length) {
						promises.push(this.getCutPromise(f.polylines, "esriGeometryPolyline", this.boundsFeature));
					}
					if (f.polygons.length) {
						promises.push(this.getCutPromise(f.polygons, "esriGeometryPolygon", this.boundsFeature));
					}
					promises = Promise.allSettled(promises);
					promises.then(function(values){
						var allFulfilled = true,
							newG = {};
						values.forEach(function(v, i){
							if (v.status == "fulfilled") {
								if (i == 0) {
									newG.polylines = v.value;
								} else if (i == 1) {
									newG.polygons = v.value;
								}
							} else {
								allFulfilled = false;
							}
						});
						if (allFulfilled) {
							var result = {
								polylines: [],
								polygons: [],
								points: f.points
							};
							if (newG.polylines && newG.polylines.geometries) {
								newG.polylines.geometries.forEach(function(g, i){
									result.polylines.push({
										g: g,
										f: f.polylines[newG.polylines.cutIndexes[i]]
									});
								});
							}
							if (newG.polygons && newG.polygons.geometries) {
								newG.polygons.geometries.forEach(function(g, i){
									result.polygons.push({
										g: g,
										f: f.polygons[newG.polygons.cutIndexes[i]]
									});
								});
							}
							result = this.renderAreaSummaryAnalysisResults(result);
							this.$vBus.$emit("on-painted-area-summary-analysis-results", result);
						} else {
							this.$vBus.$emit("on-painted-area-summary-analysis-results", "error");
						}
					}.bind(this), function(){
						this.$vBus.$emit("on-painted-area-summary-analysis-results", "error");
					}.bind(this));
				}
			},
			getCutPromise: function(features, geometryType, boundsFeature){
				var esriJsonFormat = new EsriJSON();
				var t = esriJsonFormat.writeFeaturesObject(features, {
					featureProjection: this.myMap.map.getView().getProjection()
				});
				var targetGeometries = [];
				t.features.forEach(function(f){
					targetGeometries.push(f.geometry);
				});
				var target = {
					geometryType: geometryType,
					geometries: targetGeometries
				}
				var cutter = esriJsonFormat.writeFeatureObject(boundsFeature, {
					featureProjection: this.myMap.map.getView().getProjection()
				});
				cutter = {
					paths: cutter.geometry.rings
				};
				// https://developers.arcgis.com/rest/services-reference/enterprise/cut.htm
				var url = CommonHelper.prependProxyIfNeeded(CommonHelper.cutUrl);
				var params = {
					target: JSON.stringify(target),
					cutter: JSON.stringify(cutter),
					sr: this.myMap.map.getView().getProjection().getCode().replace("EPSG:", ""),
					f: "json"
				};
				var promise = new Promise(function(resolve, reject){
					CommonHelper.getFetchPromise(url, function(json){
						resolve(json);
					}, "POST", params).then(function(result){
						resolve(result);
					}.bind(this), function(){
						reject();
					}.bind(this));
				}.bind(this));
				return promise;
			},
			renderAreaSummaryAnalysisResults: function(result){
				var selectedFeatures = [],
					selectedData = [],
					esriJsonFormat = new EsriJSON(),
					boundsFeatureGeometry = this.boundsFeature.getGeometry().clone();
				boundsFeatureGeometry.scale(1.01);
				var boundsTurfGeom = turfPolygon(boundsFeatureGeometry.getCoordinates()),
					attributes;
				if (result.polygons) {
					result.polygons.forEach(function(item){
						if (item.f) {
							attributes = Object.assign({}, item.f.getProperties());
							delete attributes.geometry;
							var feature = esriJsonFormat.readFeature({
								geometry: item.g,
								attributes: attributes
							});
							if (booleanContains(boundsTurfGeom, turfPolygon(feature.getGeometry().getCoordinates()))) {
								selectedFeatures.push(feature);
								selectedData.push({
									feature: feature,
									featureType: "horizontalPolygons"
								});
							} else {
								feature.set("out", true);
								selectedFeatures.push(feature);
							}
						}
					});
				}
				if (result.polylines) {
					result.polylines.forEach(function(item){
						if (item.f) {
							attributes = Object.assign({}, item.f.getProperties());
							delete attributes.geometry;
							var feature = esriJsonFormat.readFeature({
								geometry: item.g,
								attributes: attributes
							});
							if (booleanContains(boundsTurfGeom, turfLineString(feature.getGeometry().getCoordinates()))) {
								selectedFeatures.push(feature);
								selectedData.push({
									feature: feature,
									featureType: "horizontalPolylines"
								});
							} else {
								feature.set("out", true);
								selectedFeatures.push(feature);
							}
						}
					});
				}
				if (result.points) {
					result.points.forEach(function(feature){
						selectedFeatures.push(feature);
						selectedData.push({
							feature: feature,
							featureType: "horizontalPoints"
						});
					});
				}
				if (this.analysisResultsVectorLayer) {
					this.analysisResultsVectorLayer.getSource().addFeatures(selectedFeatures);
				}
				return selectedData;
			}
		},

		watch: {
			type: {
				immediate: true,
				handler: function(type){
					this.removeInteraction();
					if (type == "extent") {
						this.addDrawInteraction();
					} else if (type == "select") {
						this.addSelectInteraction();
					}
				}
			}
		}
	}
</script>