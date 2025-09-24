<template>
	<v-dialog
		v-model="dialog"
		max-width="800"
		:scrollable="Boolean(items)"
	>
		<v-card>
			<v-card-title>
				<span>Simbolio elemento pasirinkimas</span>
			</v-card-title>
			<v-card-text class="pb-0 pt-1" ref="content">
				<template v-if="e && e.type == 'XXX'">
					<div>TODO...</div>
				</template>
				<template v-else>
					<template v-if="items">
						<template v-if="items == 'error'">
							<v-alert
								dense
								type="error"
								class="ma-0 d-inline-block"
							>
								Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
							</v-alert>
						</template>
						<template v-else>
							<template v-if="items.length">
								<TemplatePicker
									:predefinedItems="items"
									:onItemSelect="onTemplatePick"
									:initialValue="inputVal"
									:returnCompleteInfo="true"
									ref="templatePicker"
									:key="key"
								/>
							</template>
							<template v-else>
								<div>
									Elementų nėra...
								</div>
							</template>
							<v-divider class="my-4" />
							<div>Neradote tinkamo simbolio elemento? Simbolių elementus galite kurti patys...</div>
						</template>
					</template>
					<template v-else>
						<v-progress-circular
							indeterminate
							color="primary"
							:size="30"
							width="2"
						></v-progress-circular>
					</template>
				</template>
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="closeDialog"
					outlined
					small
				>
					Uždaryti
				</v-btn>
				<v-btn
					color="blue darken-1"
					text
					v-on:click="showStreetSignSymbolElementCreator"
					outlined
					small
					disabled
				>
					Atidaryti simbolio elemento (rodyklės) kūrimo aplinką
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import StreetSignsSymbolsManagementHelper from "../helpers/StreetSignsSymbolsManagementHelper";
	import TemplatePicker from "../TemplatePicker";
	import Vue from "vue";

	export default {
		data: function(){
			var data = {
				dialog: false,
				e: null,
				items: null,
				value: null,
				key: 0
			};
			return data;
		},

		components: {
			TemplatePicker
		},

		created: function(){
			this.$vBus.$on("show-symbol-element-item-selector", this.showDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-symbol-element-item-selector", this.showDialog);
		},

		computed: {
			inputVal: {
				get: function(){
					return this.value;
				},
				set: function(val){
					this.$emit("input", val);
				}
			}
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
				this.items = null;
				var value = null;
				if (this.e) {
					if (this.e.item) {
						value = this.e.item.selected;
					}
					if (["604", "622", "623"].indexOf(this.e.type) != -1) {
						this.items = this.getItems(this.e.type);
					} else {
						var query = this.getQuery(this.e.type);
						StreetSignsSymbolsManagementHelper.getUniqueSymbolsElements(query).then(function(items){
							var now = Date.now(),
								itemsGrouped = [];
							if (items.length) {
								var gr = {},
									symbol,
									imageDimensions;
								items.forEach(function(item){
									if (item.subtype) {
										if (!gr[item.subtype]) {
											gr[item.subtype] = [];
										}
										imageDimensions = StreetSignsSymbolsManagementHelper.getImageDimensions(item, "elements");
										symbol = {
											altSrc: CommonHelper.getUniqueSymbolElementSrc(item.id, now),
											width: imageDimensions.width,
											height: imageDimensions.height,
											type: "sc-element"
										};
										if (item.subtype == "down") {
											symbol.fromTop = true;
										}
										gr[item.subtype].push({
											id: item.id,
											symbol: symbol,
											rawElementData: item
										});
									}
								}.bind(this));
								if (["517", "518"].indexOf(this.e.type) == -1) {
									delete gr["down"];
								}
								if (this.e.type == "522") {
									// gr["left"].push(StreetSignsSymbolsManagementHelper.getRawElementData("522-arrow")); // Nebeaktualu... Ai, ir šiaip nelogiška buvo :) Nes rodyklė gi į viršų, o ne į kairę...
								}
								if (gr["up"]) {
									if (["513", "514"].indexOf(this.e.type) != -1) {
										// ...
									} else if (["515", "516"].indexOf(this.e.type) != -1) {
										// ...
									} else {
										var upItemsFiltered = [];
										gr["up"].forEach(function(upItem){
											if (upItem.rawElementData) {
												if (upItem.rawElementData.data) {
													var upItemData = JSON.parse(upItem.rawElementData.data);
													if (upItemData) {
														var upArrowsCount = 0;
														if (upItemData.params && upItemData.params.arrows) {
															upItemData.params.arrows.forEach(function(arrow){
																if ((arrow.type == "up") && arrow.params && arrow.params.checked) {
																	upArrowsCount += 1;
																}
															});
														}
														if (upArrowsCount == 1) {
															upItemsFiltered.push(upItem);
														}
													}
												}
											}
										});
										// Pateikiame tik tuos, kurie turi tik vieną rodyklę į viršų!!
										gr["up"] = upItemsFiltered;
									}
								}
								var keys = [];
								for (var key in gr) {
									keys.push(key);
								}
								keys.forEach(function(key){
									if (gr[key]) {
										itemsGrouped.push({
											key: key,
											title: this.getGroupTitle(key),
											groups: [{
												templates: gr[key] || [],
												itemsBottom: true
											}]
										});
									}
								}.bind(this));
								this.sortGroups(itemsGrouped);
							}
							this.items = itemsGrouped;
						}.bind(this), function(){
							this.items = "error";
						}.bind(this));
					}
				}
				this.value = value;
			},
			onTemplatePick: function(template){
				if (this.e) {
					if (this.e.item) {
						if (["604", "622", "623"].indexOf(this.e.type) != -1) {
							var type;
							if (template) {
								type = template.id;
							}
							Vue.set(this.e.item, "type", type);
							if (type != "text") {
								Vue.set(this.e.item, "backgroundColor", "blue");
								if (template.rawElementData) {
									template = StreetSignsSymbolsManagementHelper.getRawElementData(template.id, true);
									Vue.set(this.e.item, "type", template); // Iš naujo nustatome type'o reikšmę, nes mums reikia turėti daugiau info, nei tik kodinį pavadinimą...
								}
							}
							if (this.e.onContentSet) {
								this.e.onContentSet(this.e.item);
							}
						} else {
							if (this.e.onContentSet) {
								this.e.onContentSet(template);
							} else {
								var selected;
								if (template) {
									selected = template.id;
									this.e.item.rawElementData = template.rawElementData;
								}
								Vue.set(this.e.item, "selected", selected); // Kad būtų viskas reaktyvu...
								if (this.e.modItem) {
									this.e.modItem(this.e.item);
								}
							}
						}
					}
				}
				if (this.dialog) {
					this.closeDialog();
				}
			},
			getQuery: function(type){
				var query;
				if (type == "507") {
					query = {
						type: "arrows"
					};
				} else if (type == "508") {
					query = {
						type: "arrows",
						subtype: "up"
					};
				} else if (type == "509") {
					query = {
						type: "arrows",
						subtype: "right"
					};
				} else if (type == "510") {
					query = {
						type: "arrows",
						subtype: "left"
					};
				} else if (type == "511") {
					query = {
						type: "arrows",
						subtype: "right-up"
					};
				} else if (type == "512") {
					query = {
						type: "arrows",
						subtype: "left-up"
					};
				} else if (["517", "518"].indexOf(type) != -1) {
					query = {
						type: "arrows",
						subtype: ["up", "down"].join(",")
					};
				}
				return query;
			},
			getGroupTitle: function(key){
				var titles = {
					"round": "Žiedinės sankryžos rodyklės",
					"simple": "Paprastos sankryžos rodyklės",
					"up": "Rodyklės tiesiai",
					"left": "Rodyklės į kairę",
					"right": "Rodyklės į dešinę",
					"left-up": "Rodyklės į kairę, tiesiai",
					"right-up": "Rodyklės tiesiai, į dešinę",
					"left-right": "Rodyklės į šonus",
					"down": "Žemyn"
				}
				return titles[key] || key;
			},
			sortGroups: function(groups){
				groups.sort(function(a, b){
					if (a.title < b.title) {
						return -1;
					}
					if (a.title > b.title) {
						return 1;
					}
					return 0;
				});
			},
			getItems: function(type){
				var templates = [{
					symbol: {
						type: "text",
						html: "Tekstas"
					},
					id: "text"
				}];
				if (["604", "623"].indexOf(type) != -1) {
					var icons = [{
						id: "arrow-up",
						value: CommonHelper.getIcon("arrow-up")
					},{
						id: "arrow-left",
						value: CommonHelper.getIcon("arrow-left")
					},{
						id: "arrow-right",
						value: CommonHelper.getIcon("arrow-right")
					},{
						id: "arrow-top-left",
						value: CommonHelper.getIcon("arrow-top-left")
					},{
						id: "arrow-top-right",
						value: CommonHelper.getIcon("arrow-top-right")
					}];
					icons.forEach(function(icon){
						templates.push({
							symbol: {
								type: "icon",
								value: icon.value
							},
							id: icon.id
						});
					});
				}
				var items = [{
					groups: [{
						templates: templates
					}]
				}];
				if (["604"].indexOf(type) != -1) {
					items.push({
						title: "Specialūs elementai",
						groups: [{
							templates: [
								StreetSignsSymbolsManagementHelper.getRawElementData("circle-test")
							]
						}]
					});
				}
				return items;
			},
			showStreetSignSymbolElementCreator: function(){
				console.log("showStreetSignSymbolElementCreator");
				// Dialog'e turi rodyti http://localhost:3001/admin/sc/element-create?type=arrows turinį?..
				// Sunkesnė užduotis: reikia perduoti esamo simbolio turinį?..
				// Gali būti dar tik naujai kuriamas simbolis ir niekur neišsaugotas... Reikia perduoti visą jo JSON?..
				// Hmmm... Tai jau šitas selector'ius turi turėti visą simbolio info?..
				this.$vBus.$emit("show-street-symbol-element-creator-dialog", {
					type: "arrows"
				});
				this.$vBus.$emit("show-message", {
					type: "warning",
					message: "Atsiprašome, kuriama..."
				});
			}
		},

		watch: {
			items: {
				immediate: true,
				handler: function(items){
					if (items) {
						if (this.$refs.content) {
							setTimeout(function(){
								this.$refs.content.scrollTop = 0;
							}.bind(this), 0);
						}
					}
				}
			}
		}
	}
</script>