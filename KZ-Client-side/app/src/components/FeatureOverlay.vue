<template>
	<div ref="overlay" class="popup-wrapper">
		<template v-if="activeFeature">
			<div :class="['d-flex flex-column popup', activeFeature.content ? 'auto-width' : null]">
				<div class="header d-flex align-center pa-1">
					<div v-if="activeFeature.feature && activeFeature.feature.rawIdentifyData" class="body-2 text-truncate ml-1">{{activeFeature.feature.rawIdentifyData.layerName}}</div>
					<div class="d-flex align-center flex-grow-1 justify-end">
						<div>
							<v-btn
								icon
								small
								v-on:click="zoomTo"
								:disabled="!activeFeature.feature"
								title="Parodyti visą"
							>
								<v-icon size="18">mdi-magnify-plus</v-icon>
							</v-btn>
						</div>
						<ActiveFeatureSelector :overlay="overlay" />
						<div>
							<v-btn
								icon
								v-on:click="close"
								class="ml-1"
								small
							>
								<v-icon
									title="Uždaryti"
								>
									mdi-close
								</v-icon>
							</v-btn>
						</div>
					</div>
				</div>
				<div class="content pa-2">
					<template v-if="activeFeature.content">
						<span v-html="activeFeature.content"></span>
					</template>
					<template v-else>
						<FeaturePopupContent
							:data="{
								feature: activeFeature.queryResult.feature,
								featureType: 'general-object',
								showAttachments: !!(activeFeature.serviceId == 'accidents-stats')
							}"
							v-if="activeFeature.queryResult"
						>
						</FeaturePopupContent>
					</template>
				</div>
			</div>
		</template>
	</div>
</template>

<script>
	import ActiveFeatureSelector from "./ActiveFeatureSelector";
	import FeaturePopupContent from "./FeaturePopupContent";
	import Overlay from "ol/Overlay";

	export default {
		data: function(){
			var data = {
				overlay: null
			};
			return data;
		},

		computed: {
			activeFeature: {
				get: function(){
					return this.$store.state.activeFeatureInOverlay;
				}
			},
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		components: {
			ActiveFeatureSelector,
			FeaturePopupContent
		},

		methods: {
			createOverlay: function(){
				if (!this.overlay && this.$refs.overlay) {
					this.overlay = new Overlay({
						element: this.$refs.overlay,
						positioning: "bottom-center",
						offset: [0, -13],
						className: "popup-overlay ol-selectable"
					});
					this.myMap.map.addOverlay(this.overlay);
				}
			},
			showOverlay: function(activeFeature){
				if (activeFeature && this.overlay) {
					this.overlay.autoPan = {
						animation: {
							duration: 250
						},
						margin: 20
					};
					this.overlay.setPosition(activeFeature.coordinates);
				}
			},
			close: function(){
				if (this.activeFeature && this.activeFeature.onClose) {
					this.activeFeature.onClose();
				}
				this.$store.commit("setActiveFeatureInOverlay", null);
			},
			zoomTo: function(){
				this.myMap.zoomToFeatures([this.activeFeature.feature]);
			}
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
			activeFeature: {
				immediate: true,
				handler: function(activeFeature, prevActiveFeature){
					if (prevActiveFeature && prevActiveFeature.onClose) {
						prevActiveFeature.onClose(); // Reikalingas tais atvejais jei pvz. atsidarome app per koordinačių nuorodą ir po to vykdome identifikavimą juodųjų dėmių sluoksnyje... Kad pasišalintų koordinačių pin'as-taškas...
					}
					if (this.overlay) {
						var resetPosition = true;
						if (activeFeature && (activeFeature.coordinates == this.overlay.getPosition())) {
							resetPosition = false;
						}
						if (resetPosition) {
							this.overlay.setPosition(null);
							setTimeout(function(){
								this.showOverlay(activeFeature);
							}.bind(this), 0);
						}
					}
				}
			}
		}
	}
</script>

<style scoped>
	.popup-wrapper {
		max-height: 100%;
	}
	.popup {
		width: 500px;
		background-color: white;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3) !important;
		overflow: hidden;
		overflow-y: auto;
	}
	.auto-width.popup {
		width: auto;
	}
	.popup:after, .popup:before {
		top: 100%;
		border: solid transparent;
		content: " ";
		height: 0;
		width: 0;
		position: absolute;
		pointer-events: none;
	}
	.popup:after {
		border-top-color: white;
		border-width: 10px;
		left: 50%;
		margin-left: -10px;
	}
	.popup:before {
		border-top-color: #cccccc;
		border-width: 11px;
		left: 50%;
		margin-left: -11px;
	}
	.as-overlay.popup-wrapper {
		box-shadow: 0 2px 7px rgba(0, 0, 0, 0.4);
	}
	.header {
		border-bottom: 1px solid #cccccc;
	}
	.content {
		overflow: hidden;
		overflow-y: auto;
		width: 100%;
		max-height: 300px;
	}
</style>