<template>
	<div>
		<div class="mb-2">Rodyklės sankryžoje:</div>
		<table class="my-table">
			<tr v-for="i in grid.rows" :key="i">
				<td
					v-for="j in grid.cols"
					:key="i + '-' + j"
					:class="['pa-4', cells[i + ',' + j] ? 'clickable' : null, selected[i + ',' + j] ? 'selected' : null]"
					v-on:click="selectCell(i, j)"
				>
					<template v-if="cells[i + ',' + j]">
						<v-icon class="arrow-icon">{{cells[i + ',' + j].icon}}</v-icon>
					</template>
					<template v-else>
						<span class="arrow-icon"></span>
					</template>
				</td>
			</tr>
		</table>
	</div>
</template>

<script>
	import Vue from "vue";

	export default {
		data: function(){
			var data = {
				grid: {
					rows: 3,
					cols: 3
				},
				cells: {
					"1,1": {
						angle: 225,
						icon: "mdi-arrow-top-left"
					},
					"1,2": {
						angle: 180,
						icon: "mdi-arrow-up"
					},
					"1,3": {
						angle: 135,
						icon: "mdi-arrow-top-right"
					},
					"2,1": {
						angle: 270,
						icon: "mdi-arrow-left"
					},
					"2,3": {
						angle: 90,
						icon: "mdi-arrow-right"
					},
					"3,1": {
						angle: 315,
						icon: "mdi-arrow-bottom-left"
					},
					"3,3": {
						angle: 45,
						icon: "mdi-arrow-bottom-right"
					}
				},
				selected: {}
			};
			var selected = {};
			if (this.selectedArray) {
				var anglesAndCellKeysMap = {},
					cell,
					property;
				for (property in data.cells) {
					cell = data.cells[property];
					anglesAndCellKeysMap[cell.angle] = property;
				}
				this.selectedArray.forEach(function(selectedItem){
					selected[anglesAndCellKeysMap[selectedItem]] = true;
				});
			}
			data.selected = selected;
			return data;
		},

		props: {
			onRoundaboutArrowsSelect: Function,
			selectedArray: Array
		},

		methods: {
			selectCell: function(i, j){
				var key = i + "," + j;
				if (this.cells[key]) {
					Vue.set(this.selected, key, !this.selected[key]);
				}
			}
		},

		watch: {
			selected: {
				deep: true,
				immediate: true,
				handler: function(selected){
					var selectedArray = [];
					for (var property in selected) {
						if (selected[property]) {
							selectedArray.push(this.cells[property].angle);
						}
					}
					if (this.onRoundaboutArrowsSelect) {
						this.onRoundaboutArrowsSelect(selectedArray);
					}
				}
			}
		}
	};
</script>

<style scoped>
	table {
		width: auto !important;
		border-top: 1px solid #d4d4d4 !important;
	}
	td {
		border-right: 1px solid #d4d4d4 !important;
		border-bottom: 1px solid #d4d4d4 !important;
	}
	td:first-child {
		border-left: 1px solid #d4d4d4 !important;
	}
	.selected {
		background-color: #dcdcdc;
	}
	.clickable {
		cursor: pointer;
	}
	.clickable:hover {
		background-color: #cccccc !important;
	}
	.arrow-icon {
		width: 28px;
		height: 28px;
	}
</style>