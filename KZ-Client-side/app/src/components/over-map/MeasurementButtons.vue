<template>
	<div class="stop-event">
		<v-btn-toggle
			v-model="type"
			dense
		>
			<v-tooltip bottom>
				<template v-slot:activator="{on, attrs}">
					<v-btn
						small
						value="length"
						v-bind="attrs"
						v-on="on"
						:color="type == 'length' ? 'orange' : 'rgba(255, 255, 255, 0.8)'"
					>
						<v-icon>mdi-vector-line</v-icon>
					</v-btn>
				</template>
				<span>Matuoti atstumą</span>
			</v-tooltip>
			<v-tooltip bottom>
				<template v-slot:activator="{on, attrs}">
					<v-btn
						small
						value="area"
						v-bind="attrs"
						v-on="on"
						:color="type == 'area' ? 'orange' : 'rgba(255, 255, 255, 0.8)'"
					>
						<v-icon>mdi-vector-polygon</v-icon>
					</v-btn>
				</template>
				<span>Matuoti plotą</span>
			</v-tooltip>
		</v-btn-toggle>
	</div>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import Draw from "ol/interaction/Draw";
	import Overlay from "ol/Overlay";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import {Circle as CircleStyle, Fill, Stroke, Style} from "ol/style";
	import {LineString, Polygon} from "ol/geom";
	import {unByKey} from "ol/Observable";

	export default {
		data: function(){
			var data = {
				type: null
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

		methods: {
			addInteraction: function(type){
				this.$vBus.$emit("deactivate-interactions", "measurement");
				if (!this.overlays) {
					this.overlays = [];
				}
				var geometryType;
				if (type == "length") {
					geometryType = "LineString";
				} else if (type == "area") {
					geometryType = "Polygon";
				}
				if (type) {
					if (!this.vectorLayer) {
						this.vectorLayer = new VectorLayer({
							source: new VectorSource(),
							zIndex: 1001,
							style: new Style({
								fill: new Fill({
									color: "rgba(255, 255, 255, 0.2)"
								}),
								stroke: new Stroke({
									color: "#ffcc33",
									width: 2
								}),
								image: new CircleStyle({
									radius: 7,
									fill: new Fill({
										color: "#ffcc33"
									})
								})
							})
						});
						this.myMap.map.addLayer(this.vectorLayer);
					}
					if (!this.drawInteraction) {
						this.drawInteraction = new Draw({
							type: geometryType,
							source: this.vectorLayer.getSource(),
							stopClick: true
						});
						var sketch,
							listener;
						this.drawInteraction.on("drawstart", function(evt){
							sketch = evt.feature;
							var tooltipCoord = evt.coordinate;
							listener = sketch.getGeometry().on("change", function(evt){
								var geom = evt.target,
									output;
								if (geom instanceof Polygon) {
									output = this.formatArea(geom);
									tooltipCoord = geom.getInteriorPoint().getCoordinates();
								} else if (geom instanceof LineString) {
									output = CommonHelper.formatLength(geom);
									tooltipCoord = geom.getLastCoordinate();
								}
								this.measureTooltipElement.innerHTML = output;
								this.measureTooltip.setPosition(tooltipCoord);
							}.bind(this));
						}.bind(this));
						this.drawInteraction.on("drawend", function(){
							this.measureTooltipElement.className = "ol-tooltip ol-tooltip-static stop-event";
							this.measureTooltip.setOffset([0, -7]);
							sketch = null;
							this.measureTooltipElement = null;
							this.createMeasureTooltip();
							unByKey(listener);
						}.bind(this));
						this.createMeasureTooltip();
						this.myMap.addInteraction(this.drawInteraction);
					}
				}
			},
			removeInteraction: function(){
				if (this.vectorLayer) {
					this.myMap.map.removeLayer(this.vectorLayer);
					this.vectorLayer = null;
				}
				if (this.drawInteraction) {
					this.myMap.removeInteraction(this.drawInteraction);
					this.drawInteraction = null;
				}
				if (this.overlays) {
					this.overlays.forEach(function(overlay){
						this.myMap.map.removeOverlay(overlay);
					}.bind(this));
					this.overlays = null;
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
				this.overlays.push(this.measureTooltip);
			},
			formatLength: function(line){
				var length = line.getLength(),
					output;
				if (length > 100) {
					output = Math.round((length / 1000) * 100) / 100 + " " + "km";
				} else {
					output = Math.round(length * 100) / 100 + " " + "m";
				}
				return output;
			},
			formatArea: function(polygon){
				var area = polygon.getArea(),
					output;
				if (area > 10000) {
					output = Math.round((area / 1000000) * 100) / 100 + " " + "km<sup>2</sup>";
				} else {
					output = Math.round(area * 100) / 100 + " " + "m<sup>2</sup>";
				}
				return output;
			},
			deactivateInteraction: function(type){
				if (type != "measurement") {
					this.type = null;
				}
			}
		},

		watch: {
			type: {
				immediate: true,
				handler: function(type){
					// https://openlayers.org/en/latest/examples/measure.html
					this.removeInteraction();
					if (type) {
						this.addInteraction(type);
					}
				}
			}
		}
	}
</script>

<style scoped>
	.v-btn-toggle, .v-item--active.v-btn:before {
		background-color: transparent !important;
	}
	.v-btn {
		opacity: 1 !important;
	}
</style>