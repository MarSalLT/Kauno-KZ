<template>
	<div v-if="(items && items.length > 1)" class="wrapper d-flex">
		<v-btn
			icon
			v-on:click="prev"
			small
		>
			<v-icon
				title="Ankstesnis"
			>
				mdi-chevron-left
			</v-icon>
		</v-btn>
		<v-select
			dense
			hide-details
			:items="items"
			v-model="selected"
			item-value="key"
			item-text="name"
			class="ma-0 body-2"
			append-icon=""
		></v-select>
		<v-btn
			icon
			v-on:click="next"
			small
		>
			<v-icon
				title="Kitas"
			>
				mdi-chevron-right
			</v-icon>
		</v-btn>
	</div>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";

	export default {
		data: function(){
			var data = {
				loading: true,
				items: null,
				selected: null
			};
			return data;
		},

		props: {
			overlay: Object
		},

		computed: {
			singleClickFeatures: {
				get: function(){
					if (this.overlay) {
						return this.$store.state.singleClickFeatures4Overlay;
					}
					return this.$store.state.singleClickFeatures;
				}
			}
		},

		methods: {
			prev: function(){
				var selected = this.selected - 1;
				if (selected < 0) {
					selected = this.items.length - 1;
				}
				this.selected = selected;
			},
			next: function(){
				var selected = this.selected + 1;
				if (selected == this.items.length) {
					selected = 0;
				}
				this.selected = selected;
			}
		},

		watch: {
			singleClickFeatures: {
				immediate: true,
				handler: function(singleClickFeatures){
					var items = [],
						selected;
					if (singleClickFeatures && singleClickFeatures.length > 1) {
						var activeFeature = this.$store.state.activeFeature;
						if (this.overlay) {
							activeFeature = this.$store.state.activeFeatureInOverlay;
						}
						singleClickFeatures.forEach(function(singleClickFeature, i){
							items.push({
								key: i,
								name: (i + 1) + " " + "iš" + " " + singleClickFeatures.length
							});
							if (activeFeature) {
								if (singleClickFeature.layer && (singleClickFeature.layer.layerId == activeFeature.layerId)) {
									if (singleClickFeature == activeFeature.feature) {
										selected = i;
									} else {
										var singleClickFeatureId = CommonHelper.stripGuid(singleClickFeature.get(singleClickFeature.layer.globalIdField) || singleClickFeature.get(singleClickFeature.layer.objectIdField));
										if (singleClickFeatureId == activeFeature.globalId) {
											selected = i;
										}
									}
								}
							}
						}.bind(this));
					}
					this.items = items;
					this.selected = selected;
				}
			},
			selected: {
				handler: function(selected){
					if (this.singleClickFeatures) {
						var newActiveFeature = this.singleClickFeatures[selected];
						if (this.overlay) {
							if (newActiveFeature) {
								if (newActiveFeature.fromIdentifyAction) {
									this.$store.commit("setActiveFeatureInOverlay", {
										queryResult: {
											feature: newActiveFeature
										},
										noZoom: true,
										feature: newActiveFeature,
										serviceId: newActiveFeature.layer.serviceId,
										layerId: newActiveFeature.layer.layerId,
										coordinates: this.overlay.getPosition()
									});
								}
							}
						} else {
							if (newActiveFeature) {
								if (newActiveFeature.fromIdentifyAction) {
									// BIG TODO! Reikia sprendimo su unikalia nuoroda!..
									this.$store.commit("setActiveFeature", {
										queryResult: {
											feature: newActiveFeature
										},
										noZoom: true,
										feature: newActiveFeature,
										serviceId: newActiveFeature.layer.serviceId,
										layerId: newActiveFeature.layer.layerId
									});
								} else {
									CommonHelper.routeTo({
										router: this.$router,
										vBus: this.$vBus,
										feature: newActiveFeature
									});
								}
							}
						}
					}
				}
			}
		}
	};
</script>

<style scoped>
	.wrapper {
		background-color: #f5f5f5;
	}
	.v-select {
		max-width: 48px;
	}
</style>