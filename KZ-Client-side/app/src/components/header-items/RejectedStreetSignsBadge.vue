<template>
	<div>
		<v-badge
			color="error"
			overlap
			:offset-x="10"
			:offset-y="14"
			:content="count"
			:value="!loading"
		>
			<v-btn
				icon
				small
				:title="title"
				v-on:click="dialog = true"
				:loading="loading"
			>
				<v-icon
					color="error darken-2"
					size="24"
				>
					mdi-bell-alert
				</v-icon>
			</v-btn>
		</v-badge>
		<v-dialog
			persistent
			v-model="dialog"
			max-width="1200"
			:scrollable="!loading"
		>
			<v-card>
				<v-card-title>
					<span>{{title}}</span>
				</v-card-title>
				<v-card-text class="pb-1 pt-1" ref="content">
					<template v-if="list">
						<template v-if="list == 'error'">
							<v-alert
								dense
								type="error"
								class="ma-0 d-inline-block"
							>
								Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
							</v-alert>
						</template>
						<template v-else>
							<template v-if="list.length">
								<v-text-field
									v-model="search"
									prepend-icon="mdi-magnify"
									label="Filtras"
									single-line
									hide-details
									class="ma-0 mb-3 pa-0"
								></v-text-field>
								<v-data-table
									:headers="listHeaders"
									:items="list"
									:options="listOptions"
									:mobile-breakpoint="0"
									:search="search"
								>
									<template v-slot:[`item.title`]="{item}">
										<div class="d-flex align-center">
											<template v-if="item.iconSrc">
												<span class="mr-2"><v-img :src="item.iconSrc"></v-img></span>
											</template>
											{{item.title}}
										</div>
									</template>
									<template v-slot:[`item.locationDescr`]="{item}">
										<v-btn
											icon
											v-on:click="zoomTo(item)"
										>
											<v-icon
												size="20"
												color="primary"
												title="Rodyti žemėlapyje"
											>
												mdi-map-marker
											</v-icon>
										</v-btn>
									</template>
								</v-data-table>
							</template>
							<template v-else>
								Įrašų nėra
							</template>
						</template>
					</template>
					<template v-else>
						<div>
							<v-progress-circular
								indeterminate
								color="primary"
								:size="30"
								width="2"
							></v-progress-circular>
						</div>
					</template>
				</v-card-text>
				<v-card-actions class="mx-2 pb-5 pt-5">
					<v-btn
						color="blue darken-1"
						text
						v-on:click="getList"
						outlined
						small
						:disabled="loading"
					>
						<v-icon left size="18">mdi-reload</v-icon>
						Perkrauti
					</v-btn>
					<v-btn
						color="blue darken-1"
						text
						v-on:click="dialog = false"
						outlined
						small
					>
						Atšaukti
					</v-btn>
				</v-card-actions>
			</v-card>
		</v-dialog>
	</div>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";

	export default {
		data: function(){
			var data = {
				title: "Atmestų objektų sąrašas",
				loading: true,
				count: "?",
				list: null,
				dialog: false,
				listHeaders: [{
					value: "title",
					text: "Objekto ID"
				},{
					value: "type",
					text: "Objekto tipas"
				},{
					value: "locationDescr",
					text: "Objekto vieta žemėlapyje",
					align: "center",
					sortable: false
				},{
					value: "created",
					text: "Sukurtas"
				},{
					value: "edited",
					text: "Redaguotas"
				},{
					value: "lastEditor",
					text: "Paskutinis redagavo"
				},{
					value: "approver",
					text: "Tvirtintojas"
				}],
				listOptions: {
					sortBy: ["edited"],
					sortDesc: [true],
					mustSort: true,
					itemsPerPage: 50
				},
				search: null
			};
			return data;
		},

		computed: {
			userData: {
				get: function(){
					return this.$store.state.userData;
				}
			}
		},

		mounted: function(){
			this.getList();
		},

		methods: {
			getList: function(){
				this.loading = true;
				this.list = null;
				this.getUnapprovedStreetSigns().then(function(list){
					this.loading = false;
					this.list = list;
					this.count = list.length += "";
					if (this.$refs.content) {
						setTimeout(function(){
							this.$refs.content.scrollTop = 0;
							this.$refs.content.scrollLeft = 0;
						}.bind(this), 0);
					}
				}.bind(this), function(){
					this.loading = false;
					this.list = "error";
					this.count = "!";
				}.bind(this));
			},
			getUnapprovedStreetSigns: function(){
				var promise = new Promise(function(resolve, reject){
					if (this.userData) {
						var verticalStreetSignsSupportsLayerIdMeta = CommonHelper.layerIds["verticalStreetSignsSupports"],
							verticalStreetSignsLayerIdMeta = CommonHelper.layerIds["verticalStreetSigns"];
						if (verticalStreetSignsSupportsLayerIdMeta && verticalStreetSignsLayerIdMeta) {
							if (verticalStreetSignsSupportsLayerIdMeta[0] == verticalStreetSignsLayerIdMeta[0]) {
								var serviceId = verticalStreetSignsSupportsLayerIdMeta[0];
								if (serviceId == "street-signs-vertical") {
									var url = this.userData["vertical-street-signs-service-root"];
									if (url) {
										url = CommonHelper.prependProxyIfNeeded(url) + "FeatureServer/query";
										var layerDefs = [{
											layerId: verticalStreetSignsSupportsLayerIdMeta[1],
											where: "ATMESTA = 1",
											outFields: "*"
										},{
											layerId: verticalStreetSignsLayerIdMeta[1],
											where: "ATMESTA = 1",
											outFields: "*"
										}];
										var params = {
											f: "json",
											layerDefs: JSON.stringify(layerDefs)
										};
										CommonHelper.getFetchPromise(url, function(json){
											return json;
										}.bind(this), "POST", params).then(function(result){
											var list = this.getListFromResult(result, serviceId, {
												verticalStreetSignsSupportsLayerIdMeta: verticalStreetSignsSupportsLayerIdMeta,
												verticalStreetSignsLayerIdMeta: verticalStreetSignsLayerIdMeta
											});
											resolve(list);
										}.bind(this), function(){
											reject();
										});
									} else {
										reject();
									}
								} else {
									reject();
								}
							} else {
								reject();
							}
						} else {
							reject();
						}
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			},
			getListFromResult: function(result, serviceId, meta){
				var list = [];
				if (result.layers) {
					result.layers.forEach(function(layer){
						if (layer.features) {
							var layerInfosFields = {},
								layerInfo,
								layerIdMeta,
								fields = {},
								item,
								iconSrc;
							["verticalStreetSignsSupports", "verticalStreetSigns"].forEach(function(featureType){
								layerIdMeta = CommonHelper.layerIds[featureType];
								if (layerIdMeta) {
									layerInfo = this.$store.state.myMap.getLayerInfo(featureType);
									if (layerInfo) {
										if (!layerInfosFields[layerIdMeta[0]]) {
											layerInfosFields[layerIdMeta[0]] = {};
										}
										if (!layerInfosFields[layerIdMeta[0]][layerIdMeta[1]]) {
											fields = {};
											if (layerInfo.fields) {
												layerInfo.fields.forEach(function(field){
													fields[field.name] = field;
												});
											}
											layerInfosFields[layerIdMeta[0]][layerIdMeta[1]] = fields;
										}
									}
								}
							}.bind(this));
							layer.features.forEach(function(feature){
								if (feature.attributes) {
									// TODO, FIXME! Dabar šis paveiksliuko nustatymo kodas dubliuojasi su VectorLayerStyleHelper'io kodu... Reikia pataisyti!
									/*
									iconSrc = require("@/assets/q/red.png");
									if (!feature.attributes["PASAL_DATA"]) {
										if (feature.attributes["STATUSAS"] == 1) {
											iconSrc = require("@/assets/q/green.png");
										} else if (feature.attributes["STATUSAS"] == 4) {
											iconSrc = require("@/assets/q/orange.png");
										}
									}
									*/
									item = {
										id: feature.attributes[layer.globalIdFieldName],
										serviceId: serviceId,
										layerId: layer.id,
										feature: feature,
										title: feature.attributes[layer.objectIdFieldName],
										type: layer.id,
										created: this.getPrettyValue(feature, "created_date", layer.id, serviceId, layerInfosFields),
										edited: this.getPrettyValue(feature, "last_edited_date", layer.id, serviceId, layerInfosFields),
										lastEditor: CommonHelper.getLastEditor(feature),
										approver: this.getPrettyValue(feature, "TVIRTINTOJAS", layer.id, serviceId, layerInfosFields),
										iconSrc: iconSrc
									};
									if (meta) {
										if (meta.verticalStreetSignsSupportsLayerIdMeta) {
											if (item.type == meta.verticalStreetSignsSupportsLayerIdMeta[1]) {
												item.type = "Tvirtinimo vieta";
											}
										}
										if (meta.verticalStreetSignsLayerIdMeta) {
											if (item.type == meta.verticalStreetSignsLayerIdMeta[1]) {
												item.type = "KŽ";
												if (feature.attributes["KET_NR"]) {
													item.type += " (KET NR: " + feature.attributes["KET_NR"] + ")";
												}
											}
										}	
									}
									list.push(item);
								}
							}.bind(this));
						}
					}.bind(this));
				}
				return list;
			},
			zoomTo: function(item){
				CommonHelper.routeTo({
					router: this.$router,
					vBus: this.$vBus,
					featureData: {
						globalId: item.id,
						serviceId: item.serviceId,
						layerId: item.layerId
					}
				});
				this.dialog = false;
			},
			getPrettyValue: function(feature, attr, layerId, serviceId, fields){
				var val = feature.attributes[attr];
				if (fields) {
					var field;
					if (fields[serviceId]) {
						if (fields[serviceId][layerId]) {
							field = fields[serviceId][layerId][attr];
						}
					}
					if (field) {
						if (field.type == "esriFieldTypeDate") {
							val = CommonHelper.getPrettyDate(val, true);
						} else {
							if (field.domain) {
								var codedValues = field.domain.codedValues;
								if (codedValues) {
									codedValues.some(function(codedValue){
										if (codedValue.code == val) {
											val = codedValue.name;
											return true;
										}
									});
								}
							}
						}
					}
				}
				return val;
			}
		}
	}
</script>

<style scoped>
	.v-text-field {
		max-width: 500px;
	}
	.v-image {
		width: 16px;
		height: 16px;
	}
</style>