<template>
	<div
		class="d-flex stop-event popup-wrapper"
		v-if="activeFeature"
	>
		<div
			class="stop-event action-buttons-wrapper"
			v-if="activeFeatureType != 'boundary'"
		>
			<div class="action-buttons white pa-2 d-flex flex-column overflow-y">
				<v-btn
					class="mb-2 justify-start"
					depressed
					v-on:click="toggleMaskEditing"
					v-if="activeFeatureType == 'image-mock-polygon'"
				>
					<v-icon
						title="Kirpti"
						color="primary"
					>
						{{maskEditingActive ? 'mdi-box-cutter-off' : 'mdi-box-cutter'}}
					</v-icon>
					<span class="caption ml-1">{{maskEditingActive ? 'Atšaukti kirpimą' : 'Kirpti'}}</span>
				</v-btn>
				<v-btn
					class="mb-2 justify-start"
					depressed
					v-on:click="clearMask"
					v-if="(activeFeatureType == 'image-mock-polygon') && maskFeaturesExist"
				>
					<v-icon
						title="Pašalinti kirpimą"
						color="error"
					>
						mdi-box-cutter-off
					</v-icon>
					<span class="caption ml-1">Pašalinti kirpimą</span>
				</v-btn>
				<v-btn
					class="justify-start"
					depressed
					v-on:click="destroyFeature(activeFeature)"
				>
					<v-icon
						title="Šalinti objektą"
						color="error"
					>
						mdi-delete
					</v-icon>
					<span class="caption ml-1">Šalinti</span>
				</v-btn>
			</div>
		</div>
		<div
			class="popup d-flex flex-column"
		>
			<div class="header d-flex align-center pa-1">
				<div class="flex-grow-1">
					<v-btn
						text
						class="body-2"
						small
						v-on:click="zoomTo"
					>
						<v-icon left size="18">mdi-magnify-plus</v-icon> Parodyti visą
					</v-btn>
				</div>
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
			<div class="content px-3" ref="content">
				<div class="my-3">
					<FeaturePopupContent :feature="activeFeature" :map="map" />
				</div>
			</div>
		</div>
	</div>
</template>

<script>
	import FeaturePopupContent from "./FeaturePopupContent";
	import {createEmpty, extend} from "ol/extent";
	import Vue from "vue";

	export default {
		data: function(){
			var data = {
				maskEditingActive: false,
				maskFeaturesExist: false
			};
			return data;
		},

		props: {
			activeFeature: Object,
			setActiveFeature: Function,
			destroyFeature: Function,
			addModInteraction: Function,
			removeModInteraction: Function,
			map: Object
		},

		computed: {
			activeFeatureType: {
				get: function(){
					var type = null;
					if (this.activeFeature) {
						type = this.activeFeature.get("type");
					}
					return type;
				}
			}
		},

		components: {
			FeaturePopupContent
		},

		methods: {
			zoomTo: function(){
				if (this.activeFeature) {
					this.zoomToFeatures([this.activeFeature]);
				}
			},
			close: function(){
				if (this.setActiveFeature) {
					this.setActiveFeature();
				}
			},
			zoomToFeatures: function(features, options){
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
						}
					}
				}
			},
			toggleMaskEditing: function(){
				this.maskEditingActive = !this.maskEditingActive;
			},
			clearMask: function(){
				if (this.activeFeature && this.activeFeature.clipFeatures) {
					Vue.set(this.activeFeature, "clipFeatures", null);
					this.maskFeaturesExist = false;
					this.activeFeature.changed();
				}
			},
			activateMaskGeometryEditing: function(){
				if (this.addModInteraction) {
					this.addModInteraction("mask-geometry-editing");
				}
			},
			deactivateMaskGeometryEditing: function(){
				if (this.removeModInteraction) {
					this.removeModInteraction();
				}
			},
		},

		watch: {
			maskEditingActive: {
				immediate: true,
				handler: function(maskEditingActive){
					if (maskEditingActive) {
						this.activateMaskGeometryEditing();
					} else {
						this.deactivateMaskGeometryEditing();
					}
				}
			},
			activeFeature: {
				immediate: true,
				handler: function(activeFeature){
					this.maskEditingActive = false;
					this.maskFeaturesExist = Boolean(activeFeature && activeFeature.clipFeatures && activeFeature.clipFeatures.length);
				}
			}
		}
	}
</script>

<style scoped>
	.popup-wrapper {
		max-height: 100%;
		overflow-y: auto;
		padding: 2px; /* Kitaip nusikerpa kraštinė, šešėlis */
	}
	.popup {
		width: 500px;
		background-color: white;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3) !important;
		overflow: hidden;
		overflow-y: auto;
	}
	.header {
		border-bottom: 1px solid #cccccc;
	}
	.content {
		overflow: hidden;
		overflow-y: auto;
		width: 100%;
	}
	.action-buttons-wrapper {
		margin-top: 37px;
	}
	.action-buttons {
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3) !important;
		border-top-left-radius: 10px;
		border-bottom-left-radius: 10px;
		max-height: 100%;
	}
</style>