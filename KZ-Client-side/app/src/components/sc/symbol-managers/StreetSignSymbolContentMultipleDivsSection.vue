<template>
	<div>
		<table>
			<template v-for="(row, i) in rows">
				<tr :key="i">
					<template v-for="(element, j) in row">
						<td
							:key="i + '-' + j"
							class="pa-0"
							:rowspan="element.rowsToMerge || 1"
							:colspan="element.colsToMerge || 1"
							v-if="!element.skip"
						>
							<div
								class="d-flex content-wrapper"
								:style="{
									'min-height': ((i + (element.rowsToMerge || 1) >= rows.length) ? (rows.length - i) : (element.rowsToMerge || 1)) * 70 + 'px',
									'min-width': ((j + (element.colsToMerge || 1) >= row.length) ? (row.length - j) : (element.colsToMerge || 1)) * 115 + 'px'
								}"
							>
								<div v-if="advanced && test">
									{{i}},{{j}}, MR: {{(element.rowsToMerge || 1)}}, MC: {{(element.colsToMerge || 1)}}
								</div>
								<div class="flex-grow-1 d-flex align-center justify-center">
									<template v-if="element.type">
										<div class="px-3 pr-2 py-3 flex-grow-1 text-left">
											<template v-if="element.type == 'text'">
												<v-text-field
													v-model="element.value"
													dense
													hide-details
													class="body-2 ma-0 mt-5"
													clearable
													:placeholder="element.placeholder"
												>
												</v-text-field>
											</template>
											<template v-else-if="element.type.rawElementData">
												<ESRISymbol
													:descr="element.type.symbol"
													small
												/>
											</template>
											<template v-else>
												<v-icon size="28">{{getIcon(element.type)}}</v-icon>
											</template>
										</div>
									</template>
									<template v-else>
										<div class="d-flex align-center justify-center">
											<v-btn
												icon
												color="primary lighten-2"
												title="Nurodyti turinį"
												v-on:click="selectContent(i, j)"
												class="add-content"
											>
												<v-icon size="20">mdi-image-plus</v-icon>
											</v-btn>
										</div>
									</template>
								</div>
								<div v-if="(element.type || advanced) && !plain">
									<div :class="['my-card-actions d-flex justify-center pa-1', advanced && !element.type ? 'absolute' : null]">
										<v-btn
											icon
											color="primary"
											small
											title="Parinkti nustatymus"
											v-on:click="showSettings(i, j)"
										>
											<v-icon size="20">mdi-cog</v-icon>
										</v-btn>
										<v-btn
											icon
											color="error"
											small
											title="Pašalinti turinį"
											v-on:click="removeContent(i, j)"
											v-if="element.type"
										>
											<v-icon size="20">mdi-image-minus</v-icon>
										</v-btn>
									</div>
								</div>
								<template v-if="element.backgroundColor">
									<span class="d-inline-block color-symbol rounded-circle" :style="{backgroundColor: getBackgroundColor(element.backgroundColor)}"></span>
								</template>
							</div>
						</td>
					</template>
				</tr>
			</template>
		</table>
		<div class="mt-3" v-if="rows.length < maxRowsCount">
			<v-btn
				color="blue darken-1"
				text
				small
				v-on:click="addRow"
				outlined
				class="mr-1"
			>
				Pridėti eilutę
			</v-btn>
		</div>
	</div>
</template>

<script>
	import CommonHelper from "../../helpers/CommonHelper";
	import ESRISymbol from "../../ESRISymbol";
	import Vue from "vue";

	export default {
		props: {
			data: Object,
			advanced: Boolean,
			sectionId: Number,
			rows: Array,
			test: Boolean,
			getEmptyRow: Function,
			modSection: Function,
			colCount: Number,
			updateRows: Function,
			getBackgroundColor: Function,
			maxRowsCount: Number,
			plain: Boolean
		},

		components: {
			ESRISymbol
		},

		methods: {
			addRow: function(){
				var rows = JSON.parse(JSON.stringify(this.rows));
				rows.push(this.getEmptyRow(this.colCount));
				this.updateRows(rows, this.sectionId);
			},
			selectContent: function(i, j){
				var item = this.rows[i][j];
				if (this.data.type == "622") {
					var directSelect = true;
					if (directSelect) {
						Vue.set(item, "type", "text");
						this.onContentSet();
					} else {
						this.$vBus.$emit("show-symbol-element-item-selector", {
							type: this.data.type,
							item: item,
							onContentSet: this.onContentSet
						});
					}
				} else {
					this.$vBus.$emit("show-symbol-element-item-selector", {
						type: this.data.type,
						item: item,
						onContentSet: this.onContentSet
					});
				}
			},
			removeContent: function(i, j){
				var rows = JSON.parse(JSON.stringify(this.rows));
				rows[i][j] = {};
				this.modSection(rows);
				this.updateRows(rows, this.sectionId);
			},
			showSettings: function(i, j){
				var item = this.rows[i][j],
					rowsMergePossible = item.type == "text" ? 0 : this.rows.length,
					colsMergePossible = 0,
					alignmentPossible = false;
				if (this.data.type == "604") {
					rowsMergePossible = this.rows.length;
					colsMergePossible = this.colCount - j;
					alignmentPossible = true;
				}
				this.$vBus.$emit("show-symbol-element-settings", {
					type: this.data.type,
					item: item,
					onContentSet: this.onContentSet,
					rowsMergePossible: rowsMergePossible,
					colsMergePossible: colsMergePossible,
					alignmentPossible: alignmentPossible
				});
			},
			onContentSet: function(){
				var rows = JSON.parse(JSON.stringify(this.rows));
				this.modSection(rows);
				this.updateRows(rows, this.sectionId);
			},
			getIcon: function(type){
				return CommonHelper.getIcon(type);
			}
		}
	};
</script>

<style scoped>
	table {
		border-spacing: 0;
		border-collapse: collapse;
	}
	table, td {
		border-color: #bebebe !important;
	}
	td {
		text-align: center;
		vertical-align: top;
		position: relative;
		border: 1px solid;
	}
	.no-content {
		vertical-align: middle;
	}
	.content-wrapper {
		position: relative;
	}
	.add-content {
		opacity: 0.2;
	}
	td:hover .add-content {
		opacity: 1;
	}
	.my-card-actions {
		background-color: rgba(255, 255, 255, 0.5);
	}
	.v-text-field {
		width: 150px;
	}
	.color-symbol {
		width: 20px;
		height: 20px;
		position: absolute;
		right: 7px;
		bottom: 7px;
	}
	canvas {
		border: 1px solid #a3a3a3;
	}
	.d-invisible {
		visibility: hidden;
		border-width: 0;
	}
	/*
	.absolute {
		position: absolute;
		right: 0;
		top: 0;
	}
	*/
</style>