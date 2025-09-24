<template>
	<div class="d-flex align-center ml-1">
		<div class="mr-3">Tarpinės linijos tipas:</div>
		<v-select
			v-model="params.lineType"
			dense
			hide-details
			:items="lineTypes"
			item-value="id"
			item-text="title"
			clearable
			class="body-2 ma-0 mr-4"
		></v-select>
		<div class="mr-3">
			<template v-if="noSibling == 'left'">
				Nuo kairiojo krašto iki tarpinės linijos:
			</template>
			<template v-else>
				Nuo kairiojo simbolio dešiniojo krašto iki tarpinės linijos:
			</template>
		</div>
		<v-slider
			v-model="params.leftDistance"
			min="-50"
			max="40"
			step="2"
			thumb-label="always"
			:thumb-size="20"
			ticks
			:color="params.leftDistance < 0 ? 'red' : 'primary'"
			class="ma-0 pa-0 my-slider mr-2"
			hide-details
		></v-slider>
		<div class="mr-3">
			<template v-if="noSibling == 'right'">
				Nuo tarpinės linijos iki dešiniojo krašto:
			</template>
			<template v-else>
				Nuo tarpinės linijos iki dešiniojo simbolio kairiojo krašto:
			</template>
		</div>
		<v-slider
			v-model="params.rightDistance"
			min="-50"
			max="40"
			step="2"
			thumb-label="always"
			:thumb-size="20"
			ticks
			:color="params.rightDistance < 0 ? 'red' : 'primary'"
			class="ma-0 pa-0 my-slider"
			hide-details
		></v-slider>
	</div>
</template>

<script>
	import Vue from "vue";

	export default {
		data: function(){
			var leftDistance = 0,
				rightDistance = 0,
				lineType;
			if (this.data && this.data.params) {
				leftDistance = this.data.params.leftDistance;
				rightDistance = this.data.params.rightDistance;
				lineType = this.data.params.lineType;
			}
			var data = {
				lineTypes: [{
					id: "continuous",
					title: "Ištisinė"
				},{
					id: "continuous-50-perc",
					title: "Ištisinė, 50 %"
				},{
					id: "dashed",
					title: "Punktyrinė"
				},{
					id: "dashed-50-perc",
					title: "Punktyrinė, 50 %"
				},{
					id: "dashed-40-perc",
					title: "Punktyrinė, 40 %"
				},{
					id: "dashed-30-perc",
					title: "Punktyrinė, 30 %"
				},{
					id: "dashed-20-perc",
					title: "Punktyrinė, 20 %"
				},{
					id: "dashed-20-perc-from-top",
					title: "Punktyrinė, 20 % nuo v."
				}],
				params: {
					lineType: lineType,
					leftDistance: leftDistance,
					rightDistance: rightDistance
				}
			};
			return data;
		},

		props: {
			data: Object,
			noSibling: String
		},

		watch: {
			params: {
				deep: true,
				immediate: false,
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
	.v-select {
		width: 220px;
		min-width: 170px;
	}
	.my-slider {
		width: 150px;
	}
</style>