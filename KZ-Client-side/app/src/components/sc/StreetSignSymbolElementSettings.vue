<template>
	<v-dialog
		v-model="dialog"
		max-width="800"
		:scrollable="true"
	>
		<v-card>
			<v-card-title>
				<span>Nustatymai</span>
			</v-card-title>
			<v-card-text class="pb-0 pt-1" ref="content">
				<TemplatePicker
					:predefinedItems="backgroundColors"
					:onItemSelect="onBackgroundColorPick"
					:initialValue="backgroundColor"
					:returnCompleteInfo="true"
					:key="key"
					ref="backgroundColorTemplatePicker"
				/>
				<template v-if="alignmentPossible">
					<div class="mt-4">
						<MyHeading
							value="Horizontalus lygiavimas"
						/>
						<RadioButtonsGroup
							v-model="alignment"
							:items="alignmentItems"
							class="mt-2"
						/>
					</div>
				</template>
				<template v-if="rowsMergePossible">
					<div class="mt-4">
						<TemplatePicker
							:predefinedItems="rowsToMergeItems"
							:onItemSelect="onRowsToMergeItemPick"
							:initialValue="rowsToMerge"
							:returnCompleteInfo="true"
							:key="key"
							ref="rowsToMergeTemplatePicker"
						/>
					</div>
				</template>
				<template v-if="colsMergePossible">
					<div class="mt-4">
						<TemplatePicker
							:predefinedItems="colsToMergeItems"
							:onItemSelect="onColsToMergeItemPick"
							:initialValue="colsToMerge"
							:returnCompleteInfo="true"
							:key="key"
							ref="colsToMergeTemplatePicker"
						/>
					</div>
				</template>
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="save"
					outlined
					small
					v-if="setValueOnSaveOnly"
				>
					Išsaugoti
				</v-btn>
				<v-btn
					color="blue darken-1"
					text
					v-on:click="closeDialog"
					outlined
					small
				>
					Uždaryti
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import MyHeading from "../MyHeading";
	import RadioButtonsGroup from "../fields/RadioButtonsGroup";
	import TemplatePicker from "../TemplatePicker";
	import Vue from "vue";

	export default {
		data: function(){
			var data = {
				dialog: false,
				e: null,
				backgroundColors: null,
				backgroundColor: null,
				rowsToMergeItems: null,
				colsToMergeItems: null,
				rowsMergePossible: null,
				colsMergePossible: null,
				rowsToMerge: null,
				colsToMerge: null,
				alignmentPossible: null,
				key: 0,
				alignment: null,
				alignmentItems: [{
					title: "Kairiau",
					value: "left"
				},{
					title: "Centre",
					value: "center"
				},{
					title: "Dešiniau",
					value: "right"
				}],
				setValueOnSaveOnly: true
			};
			return data;
		},

		components: {
			MyHeading,
			RadioButtonsGroup,
			TemplatePicker
		},

		created: function(){
			this.$vBus.$on("show-symbol-element-settings", this.showDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-symbol-element-settings", this.showDialog);
		},

		methods: {
			showDialog: function(e){
				this.e = e;
				this.getData();
				this.dialog = true;
			},
			closeDialog: function(){
				this.dialog = false;
			},
			getData: function(){
				this.key += 1;
				this.backgroundColors = this.rowsToMergeItems = null;
				var backgroundColor = null,
					rowsToMerge = null,
					colsToMerge = null,
					rowsMergePossible,
					colsMergePossible,
					alignmentPossible,
					alignment;
				if (this.e) {
					console.log("TODO...", this.e, "NUSTATYTI CONTENT TIPĄ?..."); // Jei rodyklė į viršų, leisti ją padaryti didelę... Jei tekstas, tai leisti, kad jo dešinėje būtų "BY"?
					if (this.e.item) {
						backgroundColor = this.e.item.backgroundColor;
						rowsToMerge = this.e.item.rowsToMerge + "";
						colsToMerge = this.e.item.colsToMerge + "";
						rowsMergePossible = parseInt(this.e.rowsMergePossible);
						colsMergePossible = parseInt(this.e.colsMergePossible);
						alignmentPossible = this.e.alignmentPossible;
						alignment = this.e.item.alignment || "center";
					}
					var colors = ["yellow", "green", "red", "blue"],
						templates = [];
					colors.forEach(function(color){
						templates.push({
							symbol: {
								type: "color",
								value: CommonHelper.colors.sc[color]
							},
							id: color
						});
					});
					this.backgroundColors = [{
						title: "Fono spalva",
						groups: [{
							templates: templates
						}]
					}];
					if (rowsMergePossible) {
						var rowsToMergeTemplates = [];
						for (var i = 1; i <= rowsMergePossible; i++) {
							rowsToMergeTemplates.push({
								symbol: {
									type: "text",
									html: i
								},
								id: i + ""
							});
						}
						this.rowsToMergeItems = [{
							title: "Apjungiamų eilučių skaičius (nuo dabartinės žemyn)",
							groups: [{
								templates: rowsToMergeTemplates
							}]
						}];
					}
					if (colsMergePossible) {
						var colsToMergeTemplates = [];
						for (i = 1; i <= colsMergePossible; i++) {
							colsToMergeTemplates.push({
								symbol: {
									type: "text",
									html: i
								},
								id: i + ""
							});
						}
						this.colsToMergeItems = [{
							title: "Apjungiamų stulpelių skaičius (nuo dabartinės dešinėn)",
							groups: [{
								templates: colsToMergeTemplates
							}]
						}];
					}
				}
				var settingsCount = 1;
				if (rowsMergePossible) {
					settingsCount += 1;
				}
				if (colsMergePossible) {
					settingsCount += 1;
				}
				if (alignmentPossible) {
					settingsCount += 1;
				}
				this.backgroundColor = backgroundColor;
				this.rowsToMerge = rowsToMerge;
				this.colsToMerge = colsToMerge;
				this.rowsMergePossible = rowsMergePossible;
				this.colsMergePossible = colsMergePossible;
				this.alignmentPossible = alignmentPossible;
				this.alignment = alignment;
				if (settingsCount > 1) {
					this.setValueOnSaveOnly = true;
				} else {
					this.setValueOnSaveOnly = false;
				}
			},
			onBackgroundColorPick: function(template){
				if (!this.setValueOnSaveOnly) {
					if (this.e) {
						if (this.e.item) {
							var backgroundColor;
							if (template) {
								backgroundColor = template.id;
							}
							Vue.set(this.e.item, "backgroundColor", backgroundColor);
							if (this.e.onContentSet) {
								this.e.onContentSet(this.e.item);
							}
						}
					}
					if (this.dialog) {
						this.closeDialog();
					}
				}
			},
			onRowsToMergeItemPick: function(template){
				if (!this.setValueOnSaveOnly) {
					if (this.e) {
						if (this.e.item) {
							var rowsToMerge;
							if (template) {
								rowsToMerge = parseInt(template.id);
							}
							Vue.set(this.e.item, "rowsToMerge", rowsToMerge);
							if (this.e.onContentSet) {
								this.e.onContentSet(this.e.item);
							}
						}
					}
					if (this.dialog) {
						this.closeDialog();
					}
				}
			},
			onColsToMergeItemPick: function(template){
				if (!this.setValueOnSaveOnly) {
					if (this.e) {
						if (this.e.item) {
							var colsToMerge;
							if (template) {
								colsToMerge = parseInt(template.id);
							}
							Vue.set(this.e.item, "colsToMerge", colsToMerge);
							if (this.e.onContentSet) {
								this.e.onContentSet(this.e.item);
							}
						}
					}
					if (this.dialog) {
						this.closeDialog();
					}
				}
			},
			save: function(){
				if (this.e && this.e.item) {
					var properties = {};
					if (this.$refs.backgroundColorTemplatePicker) {
						properties.backgroundColor = this.$refs.backgroundColorTemplatePicker.activeTemplateItem;
					}
					if (this.alignmentPossible && this.alignment) {
						properties.alignment = this.alignment;
					}
					if (this.$refs.rowsToMergeTemplatePicker) {
						var rowsToMerge;
						if (this.$refs.rowsToMergeTemplatePicker.activeTemplateItem) {
							rowsToMerge = parseInt(this.$refs.rowsToMergeTemplatePicker.activeTemplateItem);
						}
						if (!isNaN(rowsToMerge)) {
							properties.rowsToMerge = rowsToMerge;
						} else {
							properties.rowsToMerge = null;
						}
					}
					if (this.$refs.colsToMergeTemplatePicker) {
						var colsToMerge;
						if (this.$refs.colsToMergeTemplatePicker.activeTemplateItem) {
							colsToMerge = parseInt(this.$refs.colsToMergeTemplatePicker.activeTemplateItem);
						}
						if (!isNaN(colsToMerge)) {
							properties.colsToMerge = colsToMerge;
						} else {
							properties.colsToMerge = null;
						}
					}
					for (var key in properties) {
						Vue.set(this.e.item, key, properties[key]);
					}
					if (this.e.onContentSet) {
						this.e.onContentSet(this.e.item);
					}
				}
				this.closeDialog();
			}
		}
	}
</script>