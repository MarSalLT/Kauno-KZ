<template>
	<v-btn-toggle
		v-model="activeTemplateItem"
		dense
		:group="true"
		class="d-block"
	>
		<template v-if="items">
			<template v-for="(item, i) in items">
				<div
					:key="i"
					:class="i ? 'mt-5' : null"
				>
					<MyHeading
						:value="item.title"
						v-if="item.title"
					/>
					<div v-if="item.groups">
						<template v-if="filterNeeded">
							<div class="mt-3">
								<v-text-field
									v-model="filterValue"
									label="Filtras"
									dense
									single-line
									hide-details
									autocomplete="off"
								>
									<template v-slot:append>
										<v-btn
											icon
											height="20"
											width="20"
											:class="['mt-1', filterValue ? null : 'invisible']"
											v-on:click="filterValue = null"
										>
											<v-icon
												title="Valyti"
												small
											>
												mdi-close-circle
											</v-icon>
										</v-btn>
									</template>
								</v-text-field>
							</div>
						</template>
						<template v-for="(group, j) in item.groups">
							<div
								v-if="group.name && (!filterNeeded || group.templates.length)"
								:key="j + '-name'"
								class="mt-4 font-weight-bold body-2"
							>
								{{group.name}}
							</div>
							<div
								:key="j + '-items'"
								v-if="!filterNeeded || group.templates.length"
								:class="[group.itemsBottom ? 'd-flex align-end' : null]"
							>
								<template v-if="group.templates.length">
									<template v-for="(template, k) in group.templates">
										<div :key="j + '-' + k" :class="[item.list ? 'd-block mt-3' : 'd-inline-block mr-2 mt-2']">
											<template v-if="item.list">
												<div class="font-weight-bold body-2 mb-1">{{template.name}}</div>
											</template>
											<TemplatePickerButton
												:template="template"
												:dark="item.dark"
												:noTitle="Boolean(item.list)"
											/>
										</div>
									</template>
								</template>
								<template v-else>
									<div class="mt-2 body-2">Nėra...</div>
								</template>
							</div>
						</template>
					</div>
				</div>
			</template>
		</template>
	</v-btn-toggle>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";
	import MyHeading from "./MyHeading";
	import TemplatePickerButton from "./TemplatePickerButton";

	export default {
		data: function(){
			var data = {
				items: null,
				activeTemplateItem: this.initialValue,
				filterValue: null
			};
			return data;
		},

		props: {
			featureTypes: Array,
			onItemSelect: Function,
			initialValue: String,
			returnCompleteInfo: Boolean,
			filterNeeded: Boolean,
			predefinedItems: Array,
			layersInfo: Object,
			showOnly: Object,
			templatePickerConfig: Object
		},

		components: {
			MyHeading,
			TemplatePickerButton
		},

		mounted: function(){
			this.setItems();
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		methods: {
			setItems: function(){
				this.templates = {};
				if (this.featureTypes) {
					var items = [],
						layerInfo,
						item,
						template;
					this.featureTypes.forEach(function(featureType){
						layerInfo = null;
						if (this.myMap) {
							layerInfo = this.myMap.getLayerInfo(featureType);
						} else {
							if (this.layersInfo) {
								layerInfo = this.layersInfo[featureType];
							}
						}
						if (layerInfo) {
							item = {
								groups: [],
								title: CommonHelper.getPrettyName(layerInfo.name, featureType)
							};
							if (featureType == "horizontalPoints") {
								item.dark = true;
							} else if (["verticalStreetSignsSupports", "otherPoints", "otherPolylines", "otherPolygons"].indexOf(featureType) != -1) {
								item.list = true;
							}
							var templates = [];
							if (layerInfo.templates && layerInfo.templates.length) {
								// Bookmarks'ų sluoksnyje taip...
								layerInfo.templates.forEach(function(templateData, i){
									template = JSON.parse(JSON.stringify(templateData));
									if (this.templatePickerConfig && this.templatePickerConfig.noTemplateNames) {
										template.name = null;
									}
									if (layerInfo.drawingInfo) {
										template.symbol = layerInfo.drawingInfo.renderer.symbol;
									}
									if (this.returnCompleteInfo) {
										template.id = featureType + "-" + i;
										this.templates[template.id] = {
											template: template,
											featureType: featureType
										};
									}
									templates.push(template);
								}.bind(this));
								item.groups.push({
									templates: templates
								});
								item.list = true;
							} else if (layerInfo.types && layerInfo.types.length) {
								var symbols = {};
								if (layerInfo.drawingInfo) {
									var renderer = layerInfo.drawingInfo.renderer;
									if (renderer.type == "uniqueValue") {
										renderer.uniqueValueInfos.forEach(function(uniqueValueInfo){
											symbols[uniqueValueInfo.value] = uniqueValueInfo.symbol;
										});
									}
								}
								layerInfo.types.forEach(function(typeData){
									typeData.templates.forEach(function(templateData){
										template = JSON.parse(JSON.stringify(templateData));
										template.id = typeData.id + "";
										template.symbol = symbols[template.id];
										if (this.returnCompleteInfo) {
											template.id = featureType + "-" + typeData.id;
											this.templates[template.id] = {
												id: typeData.id,
												template: template,
												featureType: featureType
											};
										}
										templates.push(template);
									}.bind(this));
								}.bind(this));
								if (featureType == "verticalStreetSigns") {
									var nameFirstChar,
										itemsGroups = {},
										skip,
										showOnly;
									if (this.showOnly) {
										showOnly = this.showOnly[featureType];
									}
									templates.forEach(function(template){
										skip = false;
										nameFirstChar = template.name.substring(0, 1);
										if (!itemsGroups[nameFirstChar]) {
											itemsGroups[nameFirstChar] = {
												templates: [],
												name: this.getVerticalStreetSignsGroupName(nameFirstChar)
											}
										}
										if (showOnly) {
											if (showOnly.all.indexOf(template.name) == -1) {
												skip = true;
											} else {
												if (showOnly.available.indexOf(template.name) == -1) {
													template.disabled = true;
												}
											}
										}
										if (this.filterNeeded) {
											if (this.filterValue) {
												if (template.name.indexOf(this.filterValue) == -1) {
													skip = true;
												}
											}
										}
										if (!skip) {
											itemsGroups[nameFirstChar].templates.push(template);
										}
									}.bind(this));
									for (var property in itemsGroups) {
										if (showOnly) {
											if (itemsGroups[property].templates.length) {
												item.groups.push(itemsGroups[property]);
											}
										} else {
											item.groups.push(itemsGroups[property]);
										}
									}
								} else {
									item.groups.push({
										templates: templates
									});
								}
							}
							items.push(item);
						}
					}.bind(this));
					this.items = items;
				} else if (this.predefinedItems) {
					if (this.returnCompleteInfo) {
						this.predefinedItems.forEach(function(item){
							if (item.groups) {
								item.groups.forEach(function(group){
									if (group.templates) {
										group.templates.forEach(function(template){
											this.templates[template.id] = template;
										}.bind(this));
									}
								}.bind(this));
							}
						}.bind(this));
					}
					this.items = this.predefinedItems;
				}
			},
			getVerticalStreetSignsGroupName: function(key){
				var names = {
					"1": "Įspėjamieji kelio ženklai",
					"2": "Pirmumo kelio ženklai",
					"3": "Draudžiamieji kelio ženklai",
					"4": "Nukreipiamieji kelio ženklai",
					"5": "Nurodomieji kelio ženklai",
					"6": "Informaciniai kelio ženklai",
					"7": "Paslaugų kelio ženklai",
					"8": "Papildomos lentelės",
					"9": "Kiti kelio ženklai"
				};
				return names[key];
			}
		},

		watch: {
			activeTemplateItem: {
				immediate: false,
				handler: function(activeTemplateItem){
					if (this.onItemSelect) {
						if (this.returnCompleteInfo) {
							var template;
							if (activeTemplateItem && this.templates) {
								template = this.templates[activeTemplateItem];
							}
							activeTemplateItem = template;
						}
						this.onItemSelect(activeTemplateItem);
					}
				}
			},
			filterValue: {
				immediate: false,
				handler: function(){
					this.setItems();
				}
			}
		}
	};
</script>