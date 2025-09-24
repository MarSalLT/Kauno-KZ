<template>
	<div
		:class="['stop-event menu-wrapper pa-1', visible ? '' : 'd-none']"
		ref="wrapper"
	>
		<template v-if="visible && e && e.coordinates">
			<div class="pa-2 mr-2">
				<div class="body-2">
					Taško koordinatės: <span><span class="font-weight-bold">{{e.coordinates[0].toFixed()}}</span>, <span class="font-weight-bold">{{e.coordinates[1].toFixed()}}</span></span>
				</div>
				<div class="mt-1">
					<div class="d-none">
						<v-btn
							depressed
							small
							left
						>
							<v-icon
								small
							>
								mdi-share-variant
							</v-icon>
							Dalintis
						</v-btn>
					</div>
					<div class="d-flex align-center popup">
						<v-text-field
							v-model="linkVal"
							dense
							hide-details
							full-width
							class="body-2 ma-0 plain"
							readonly
						>
						</v-text-field>
						<v-btn
							icon
							height="24"
							width="24"
							v-on:click="copyLink"
						>
							<v-icon title="Kopijuoti" small>mdi-content-copy</v-icon>
						</v-btn>
					</div>
				</div>
			</div>
		</template>
		<v-btn
			icon
			v-on:click.stop="hideContextMenu"
			class="close-button"
			small
		>
			<v-icon
				title="Uždaryti"
			>
				mdi-close
			</v-icon>
		</v-btn>
	</div>
</template>

<script>
	import Feature from "ol/Feature";
	import Overlay from "ol/Overlay";
	import Point from "ol/geom/Point";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import {Circle as CircleStyle, Fill, Style} from "ol/style";

	export default {
		data: function(){
			var data = {
				visible: false,
				e: null,
				linkVal: null
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
			this.$vBus.$on("set-context-menu-state", this.setContextMenuState);
		},

		beforeDestroy: function(){
			this.$vBus.$off("set-context-menu-state", this.setContextMenuState);
		},

		methods: {
			createOverlay: function(){
				if (!this.overlay) {
					this.overlay = new Overlay({
						element: this.$refs.wrapper,
						positioning: "top-left",
						offset: [2, 0],
						className: "popup-overlay"
					});
					this.overlay.on("change:position", function(){
						var position = this.overlay.get("position");
						if (position) {
							this.showFeature(position);
						} else {
							this.hideFeature();
						}
					}.bind(this));
					this.myMap.map.addOverlay(this.overlay);
				}
			},
			showOverlay: function(coordinates){
				if (coordinates) {
					this.overlay.autoPan = false;
					this.overlay.setPosition(coordinates);
				}
			},
			setContextMenuState: function(e){
				var visible = false,
					linkVal;
				if (e && (e.state == "visible") && e.coordinates) {
					visible = true;
					linkVal = window.location.origin + process.env.VUE_APP_ROOT + "/?coordinates=" + e.coordinates[0].toFixed() + "," + e.coordinates[1].toFixed() + "," + this.myMap.map.getView().getZoom();
					setTimeout(function(){
						this.showOverlay(e.coordinates);
					}.bind(this), 0);
				} else {
					if (this.overlay) {
						this.overlay.setPosition(null);
					}
				}
				this.e = e;
				this.visible = visible;
				this.linkVal = linkVal;
			},
			hideContextMenu: function(){
				this.$vBus.$emit("set-context-menu-state", {
					state: "hidden"
				});
				this.hideFeature();
			},
			copyLink: function(){
				navigator.clipboard.writeText(this.linkVal);
				this.$vBus.$emit("show-message", {
					type: "success",
					message: "Nuoroda nukopijuota"
				});
			},
			showFeature: function(position){
				this.hideFeature();
				if (!this.vectorLayer) {
					this.vectorLayer = new VectorLayer({
						source: new VectorSource(),
						zIndex: 2000,
						style: new Style({
							image: new CircleStyle({
								radius: 5,
								fill: new Fill({
									color: "red"
								})
							})
						})
					});
					this.myMap.map.addLayer(this.vectorLayer);
				}
				var feature = new Feature();
				feature.setGeometry(new Point(position));
				this.vectorLayer.getSource().addFeature(feature);
			},
			hideFeature: function(){
				if (this.vectorLayer) {
					this.myMap.map.removeLayer(this.vectorLayer);
					this.vectorLayer = null;
				}
			},
		},

		watch: {
			myMap: {
				immediate: true,
				handler: function(myMap){
					if (myMap) {
						this.createOverlay();
					}
				}
			},
			visible: {
				immediate: true,
				handler: function(visible){
					if (this.myMap) {
						if (visible) {
							// this.myMap.map.on("pointerdown", this.hideContextMenu);
							// this.myMap.map.on("movestart", this.hideContextMenu);
						} else {
							// this.myMap.map.un("pointerdown", this.hideContextMenu);
							// this.myMap.map.un("movestart", this.hideContextMenu);
						}
					}
				}
			}
		}
	}
</script>

<style scoped>
	.menu-wrapper {
		background-color: white;
		border-radius: 5px;
		min-width: 300px;
	}
	.close-button {
		position: absolute;
		top: 0;
		right: 0;
		margin-top: 2px;
		margin-right: 2px;
	}
</style>