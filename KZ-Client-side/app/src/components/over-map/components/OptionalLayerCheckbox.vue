<template>
	<div class="layer-container mt-1 pa-1">
		<div class="d-flex align-center">
			<div class="d-flex flex-grow-1">
				<v-checkbox
					:label="service.title"
					class="ma-0 pa-0"
					hide-details
					v-model="checked"
					v-if="optionalService"
					:disabled="loading"
				></v-checkbox>
				<div class="ml-3 d-none">
					<v-btn
						icon
						height="24"
						width="24"
						:disabled="loading"
					>
						<v-icon title="Nustatymai" small>mdi-cog</v-icon>
					</v-btn>
				</div>
			</div>
			<div class="ml-3 settings" v-if="checked">
				<v-btn
					icon
					height="20"
					width="20"
					v-on:click="settingsVisible = !settingsVisible"
				>
					<v-icon
						title="Nustatymai"
						small
					>
						{{settingsVisible ? "mdi-chevron-up" : "mdi-chevron-down"}}
					</v-icon>
				</v-btn>
			</div>
		</div>
		<div class="ml-8 mr-8 mt-1" v-if="checked && settingsVisible">
			<div class="d-flex align-center">
				<span class="mr-3">Peršviečiamumas:</span>
				<v-slider
					v-model="opacity"
					max="100"
					min="0"
					class="ma-0 pa-0 mr-2"
					hide-details
				></v-slider>
			</div>
			<OptionalLayerSublayersTree
				:service="service"
				v-if="service.advancedDetails"
			/>
		</div>
	</div>
</template>

<script>
	import OptionalLayerSublayersTree from "./OptionalLayerSublayersTree";

	export default {
		data: function(){
			var optionalService = this.service,
				callback = optionalService.callback;
			optionalService.callback = function(status){
				if (!status) {
					this.checked = false;
				} else {
					var opacity = optionalService.layer.get("opacity");
					this.opacity = (1 - opacity || 0) * 100; // FIXME! Ar gerai nustatomas? Nepatikrinau...
				}
				if (callback) {
					callback(status);
				}
				this.loading = false;
			}.bind(this);
			var data = {
				checked: Boolean(optionalService.active),
				optionalService: optionalService,
				loading: false,
				settingsVisible: false,
				opacity: 0
			};
			return data;
		},

		components: {
			OptionalLayerSublayersTree
		},

		props: {
			service: Object,
			addLayer: Function,
			removeLayer: Function
		},

		watch: {
			checked: {
				immediate: true,
				handler: function(checked){
					if (checked) {
						this.addLayer(this.optionalService);
						this.loading = true; // Reikalingas tam, kad neatžymėtume besikraunančio sluoksnio... Kitaip susijauks reikaliukai...
					} else {
						this.removeLayer(this.optionalService);
					}
				}
			},
			opacity: {
				immediate: true,
				handler: function(opacity){
					if (this.service.layer) {
						this.service.layer.set("opacity", 1 - opacity / 100);
					}
				}
			}
		}
	}
</script>

<style scoped>
	.layer-container {
		border: 1px solid #dddddd;
		background-color: #fbfbfb;
	}
</style>