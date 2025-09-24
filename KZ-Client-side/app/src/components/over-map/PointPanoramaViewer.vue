<template>
	<OverMapButton
		:title="title"
		icon="mdi-google-street-view"
		:clickCallback="onClick"
		:activeChangeCallback="onActiveChange"
		ref="btn"
	/>
</template>

<script>
	import Draw from "ol/interaction/Draw";
	import OverMapButton from "./OverMapButton";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import {Circle as CircleStyle, Fill, Style} from "ol/style";

	export default {
		data: function(){
			var data = {
				title: "Rasti panoraminį vaizdą"
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
						this.addInteraction();
					} else {
						this.removeInteraction();
					}
					this.$vBus.$emit("show-or-hide-panorama-viewer-settings", {
						btn: this.$refs.btn,
						state: active
					});
				}
			},
			addInteraction: function(){
				this.$vBus.$emit("deactivate-interactions", "panorama-viewer");
				if (!this.vectorLayer) {
					this.vectorLayer = new VectorLayer({
						source: new VectorSource(),
						zIndex: 1002,
						style: new Style({
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
						type: "Point",
						source: this.vectorLayer.getSource(),
						stopClick: true
					});
					this.drawInteraction.on("drawend", function(e){
						this.$vBus.$emit("show-point-panorama-viewer", {
							title: "Panoraminis vaizdas",
							feature: e.feature,
							source: this.$refs.btn.source
						});
						this.deactivateInteraction();
					}.bind(this));
					this.myMap.addInteraction(this.drawInteraction);
					this.myMap.helpMessage = "Spustelkite vietoje, kuriai rodyti panoraminę nuotrauką";
					this.myMap.addPointerMoveHandler();
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
					this.myMap.removePointerMoveHandler();
				}
			},
			deactivateInteraction: function(type){
				if (type != "panorama-viewer") {
					if (this.$refs.btn) {
						this.$refs.btn.needsAttention = this.$refs.btn.active = false;
					}
				}
			}
		}
	}
</script>