<template>
	<v-menu
		open-on-hover
		:close-on-content-click="false"
		tile
		right
		:eager="true"
		:nudge-top="0"
	>
		<template v-slot:activator="{on, attrs}">
			<v-btn
				icon
				x-small
				title="Nustatyti skiriamosios linijos savybes"
				class="divider-settings"
				color="primary lighten-1"
				v-on="on"
				v-bind="attrs"
			>
				<v-icon>
					mdi-cogs
				</v-icon>
			</v-btn>
		</template>
		<div class="pa-3 body-2">
			<div>Vertikalios linijos tipas:</div>
			<v-radio-group
				v-model="params.lineType"
				class="ma-0 pa-0"
				hide-details
				column
			>
				<v-radio
					label="Ištisinė"
					value="continuous"
					class="ma-0 pa-0 mt-1"
				></v-radio>
				<v-radio
					label="Punktyrinė"
					value="dashed"
					class="ma-0 pa-0 mt-1"
				></v-radio>
				<v-radio
					label="Punktyrinė, 40 % aukščio nuo apačios"
					value="dashed-40-perc"
					class="ma-0 pa-0 mt-1"
				></v-radio>
			</v-radio-group>
			<div class="mt-2">Tarpai tarp rodyklių:</div>
			<div>...</div>
		</div>
	</v-menu>
</template>

<script>
	import Vue from "vue";

	export default {
		data: function(){
			var data = {
				params: {
					lineType: "dashed-40-perc"
				}
			};
			return data;
		},

		props: {
			data: Object
		},

		watch: {
			params: {
				deep: true,
				immediate: true,
				handler: function(params){
					if (this.data) {
						Vue.set(this.data, "params", params);
					}
				}
			}
		}
	};
</script>

<style scoped>
	.divider-settings {
		position: absolute;
		z-index: 1;
		top: 50%;
		left: 50%;
		margin-left: -9px;
		margin-top: -9px;
	}
</style>