<template>
	<div>
		<MyHeading
			value="Kelio ženklai atramoje:"
		>
			<template v-slot:additional>
				<v-btn
					icon
					small
					class="ml-1"
					v-on:click="dialog = true"
					v-if="items && items.length"
				>
					<v-icon
						title="Rodyti išsamią informaciją"
						:size="18"
					>
						mdi-format-list-bulleted
					</v-icon>
				</v-btn>
			</template>
		</MyHeading>
		<div class="mt-1">
			<template v-if="items && items.length">
				<v-data-table
					:headers="listHeaders"
					:items="items"
					hide-default-footer
					hide-default-header
					disable-pagination
					disable-sort
					class="simple-data-table vertical-street-signs-selector"
				>
					<template v-slot:header="{props}">
						<tr>
							<template v-for="header in props.headers">
								<th
									:key="header.value"
									:class="['body-2 px-2 py-1 font-weight-medium bolder-background', header.align == 'center' ? 'text-center' : 'text-left']"
								>
									{{header.text}}
								</th>
							</template>
						</tr>
					</template>
					<template v-slot:item="{item}">
						<tr
							:class="['clickable', activeFeatureGlobalId == item.id ? 'primary lighten-4' : null]"
							v-on:click="onRowClick(item)"
						>
							<td class="pa-1">
								<ESRISymbol
									:descr="item.symbolDescr"
									:small="true"
									:key="symbolsKey"
								/>
								<template v-if="item.isTasksRelated">
									<v-avatar
										color="success"
										size="15"
										class="ml-1"
									>
										<span class="white--text caption">U</span>
									</v-avatar>
								</template>
							</td>
							<td class="px-2 py-2">
								{{item.streetSignNumber}}
							</td>
							<td class="px-2 py-2">
								<span v-html="item.rotationAngle"></span>
							</td>
						</tr>
					</template>
				</v-data-table>
			</template>
			<template v-else>
				<div class="pa-1 bordered-container body-2">
					Susietų kelio ženklų nėra
				</div>
			</template>
		</div>
		<v-dialog
			v-model="dialog"
			scrollable
		>
			<v-card>
				<v-card-title>
					<span>Išsami kelio ženklų atramoje informacija</span>
				</v-card-title>
				<v-card-text class="pb-0">
					<template v-if="advancedListItems">
						<div class="table-wrapper" ref="dialogContent">
							<table class="body-2 my-table advanced-list-items-table">
								<template v-for="(item, i) in advancedListItems">
									<tr :key="i">
										<template v-if="item.title">
											<th class="px-2 py-1 font-weight-medium bolder-background">
												{{item.title}}
											</th>
										</template>
										<template v-for="(valueItems, j) in item.values">
											<td
												class="px-2 py-1"
												:key="j"
											>
												<template v-if="valueItems.symbolDescr">
													<ESRISymbol
														:descr="valueItems.symbolDescr"
														:small="true"
														:alignStart="true"
													/>
												</template>
												<template v-else>
													{{valueItems.valuePretty}}
												</template>
											</td>
										</template>
									</tr>
								</template>
							</table>
						</div>
					</template>
				</v-card-text>
				<v-card-actions class="mx-2 pb-5 pt-5">
					<v-btn
						color="blue darken-1"
						text
						v-on:click="dialog = false"
						outlined
						small
					>
						Uždaryti
					</v-btn>
				</v-card-actions>
			</v-card>
		</v-dialog>
	</div>
</template>

<script>
	import CommonHelper from "../components/helpers/CommonHelper";
	import ESRISymbol from "./ESRISymbol";
	import MyHeading from "./MyHeading";

	export default {
		data: function(){
			var data = {
				listHeaders: [{
					value: "icon",
					text: "Ženklas",
					align: "center"
				},{
					value: "streetSignNumber",
					text: "KET nr.",
					align: "center"
				},{
					value: "rotationAngle",
					text: "Pasukimo kampas",
					align: "center"
				}],
				activeFeatureGlobalId: null,
				dialog: false,
				advancedListItems: [],
				symbolsKey: 0
			};
			if (this.data.featureType == "verticalStreetSigns") {
				data.activeFeatureGlobalId = "{" + this.data.globalId + "}";
			}
			return data;
		},

		props: {
			data: Object
		},

		components: {
			ESRISymbol,
			MyHeading
		},

		created: function(){
			this.$vBus.$on("street-sign-feature-rotation-changed", this.streetSignFeatureRotationChanged);
			this.$vBus.$on("unique-symbol-timestamp-changed", this.uniqueSymbolTimestampChanged);
		},

		beforeDestroy: function(){
			this.$vBus.$off("street-sign-feature-rotation-changed", this.streetSignFeatureRotationChanged);
			this.$vBus.$off("unique-symbol-timestamp-changed", this.uniqueSymbolTimestampChanged);
		},

		computed: {
			items: {
				get: function(){
					var items = [];
					if (this.data && this.data.additionalData && this.data.additionalData.signs) {
						var layerInfo = this.$store.state.myMap.getLayerInfo("verticalStreetSigns");
						if (layerInfo) {
							var renderer = layerInfo.drawingInfo.renderer,
								rotationField,
								symbols = {};
							if (renderer) {
								if (renderer.visualVariables) {
									renderer.visualVariables.forEach(function(visualVariable){
										if (visualVariable.type == "rotationInfo") {
											rotationField = visualVariable.valueExpression.replace("$feature.", "");
										}
									});
								}
								if (renderer.type == "uniqueValue") {
									renderer.uniqueValueInfos.forEach(function(uniqueValueInfo){
										symbols[uniqueValueInfo.value] = uniqueValueInfo.symbol;
									});
								}
							}
							var item;
							this.data.additionalData.signs.forEach(function(feature){
								if ((feature.get("STATUSAS") == CommonHelper.verticalStreetSignDestroyedStatusValue) && (feature.get("PATVIRTINTAS") == 1) && !feature.get("ATMESTA")) {
									// Ženklas yra panaikintas ir patvirtintas... Jį ignoruojame...
								} else {
									item = {
										streetSignNumber: feature.get(layerInfo.typeIdField) || "—",
										id: feature.get(layerInfo.globalIdField),
										symbolDescr: symbols[feature.get(layerInfo.typeIdField)]
									};
									if (feature.get(CommonHelper.customSymbolIdFieldName)) {
										item.symbolDescr = Object.assign({}, item.symbolDescr);
										item.symbolDescr.altSrc = CommonHelper.getUniqueSymbolSrc(feature.get(CommonHelper.customSymbolIdFieldName), Date.now());
										item.symbolDescr.altSrcWithoutTimestamp = CommonHelper.getUniqueSymbolSrc(feature.get(CommonHelper.customSymbolIdFieldName));
										item.symbolDescr.width = "auto";
										item.symbolDescr.height = "30px";
									}
									feature.symbolDescr = item.symbolDescr; // Prireiks KŽ sąrašui dialog'e...
									item.rotationAngle = this.getAngleValue(rotationField ? feature.get(rotationField) : null);
									if (feature.isTasksRelated) {
										item.isTasksRelated = true;
										item.taskFeatureAction = feature.get(CommonHelper.taskFeatureActionFieldName);
										console.log("TODO: dar nustatyti redagavimo veiksmą, kurį irgi pateikti lentelėje...", item.taskFeatureAction); // TODO!
										if (!item.id) {
											console.log("PROBLEMA??!"); // Taip būdavo kartais...
										}
									}
									items.push(item);
								}
							}.bind(this));
						}
					}
					return items;
				}
			}
		},

		methods: {
			onRowClick: function(item){
				var activeFeatureGlobalId = null;
				if (item.id != this.activeFeatureGlobalId) {
					activeFeatureGlobalId = item.id;
				}
				var feature;
				if (this.data && this.data.additionalData) {
					if (activeFeatureGlobalId) {
						if (this.data.additionalData.signs) {
							var layerInfo = this.$store.state.myMap.getLayerInfo("verticalStreetSigns");
							if (layerInfo) {
								this.data.additionalData.signs.some(function(f){
									if (f.get(layerInfo.globalIdField) == activeFeatureGlobalId) {
										feature = f;
										return true;
									}
								});
							}
						}
					} else {
						if (this.data.additionalData.supports && this.data.additionalData.supports.length == 1) {
							feature = this.data.additionalData.supports[0];
						}
					}
				}
				this.$store.state.myMap.setLayerForFeature(feature, "street-signs-vertical");
				if (feature && feature.layer) {
					this.activeFeatureGlobalId = activeFeatureGlobalId;
					this.$store.commit("setActiveFeaturePreview", feature);
					CommonHelper.routeTo({
						router: this.$router,
						vBus: this.$vBus,
						feature: feature
					});
				} else {
					this.$vBus.$emit("show-message", {
						type: "warning"
					});
				}
			},
			getAdvancedListItems: function(){
				var items = [],
					featureFields = CommonHelper.importantFields["verticalStreetSigns"];
				if (featureFields) {
					if (this.data && this.data.additionalData && this.data.additionalData.signs) {
						var myMap = this.$store.state.myMap,
							layerFields = myMap.getLayerFields("verticalStreetSigns"),
							field,
							item;
						if (layerFields) {
							var firstItem = {
								title: "Simbolis",
								values: []
							};
							this.data.additionalData.signs.forEach(function(feature){
								firstItem.values.push({
									symbolDescr: feature.symbolDescr
								});
							});
							items.push(firstItem);
							featureFields.forEach(function(key){
								field = myMap.getLayerField(layerFields, key);
								if (field) {
									item = {
										title: myMap.getFieldTitle(field),
										values: []
									};
									this.data.additionalData.signs.forEach(function(feature){
										item.values.push(myMap.getValueItems(field, feature.get(key)));
									});
									items.push(item);
								}
							}.bind(this));
						}
					}
				}
				this.advancedListItems = items;
			},
			streetSignFeatureRotationChanged: function(e){
				if (e && e.feature) {
					var feature = e.feature;
					if (this.items && feature.origRotation) {
						var featureGlobalId;
						if (feature.layer && feature.layer.globalIdField) {
							featureGlobalId = feature.get(feature.layer.globalIdField);
						}
						if (featureGlobalId) {
							this.items.some(function(item){
								if (item.id == featureGlobalId) {
									item.rotationAngle = this.getAngleValue(feature.get(feature.origRotation.field));
									return true;
								}
							}.bind(this));
						}
					}
				}
			},
			getAngleValue: function(value){
				if (value || value === 0) {
					value = value.toFixed() + "&deg;";
				} else {
					value = "—";
				}
				return value;
			},
			uniqueSymbolTimestampChanged: function(){
				if (this.items) {
					this.items.forEach(function(item){
						if (item.symbolDescr && item.symbolDescr.altSrc) {
							if (item.symbolDescr.altSrcWithoutTimestamp) {
								item.symbolDescr.altSrc = item.symbolDescr.altSrcWithoutTimestamp + "?=" + Date.now();
							}
						}
					}.bind(this));
				}
				this.symbolsKey += 1;
			}
		},

		watch: {
			dialog: {
				immediate: true,
				handler: function(open){
					if (open) {
						this.getAdvancedListItems();
						if (this.$refs.dialogContent) {
							setTimeout(function(){
								this.$refs.dialogContent.scrollTop = 0;
								this.$refs.dialogContent.scrollLeft = 0;
							}.bind(this), 0);
						}
					}
				}
			}
		}
	};
</script>

<style scoped>
	.clickable {
		cursor: pointer;
	}
	.v-data-table td {
		width: 33%;
		text-align: center;
		height: auto !important;
		position: relative;
	}
	.advanced-list-items-table {
		width: auto;
	}
	.v-avatar {
		position: absolute;
		top: 3px;
		right: 3px;
	}
</style>