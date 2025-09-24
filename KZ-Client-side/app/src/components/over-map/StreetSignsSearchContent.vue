<template>
	<OverMapButtonContent
		type="street-signs-search"
		:title="title"
		:btn="btn"
		:onOpen="onOpen"
		:wide="true"
		ref="wrapper"
	>
		<template v-slot>
			<div class="body-2">
				<MyForm
					:data="formData"
					id="form-data"
					:spacious="true"
					:onDataChange="onFormDataChange"
					ref="form"
					class="compact-form mt-n2"
				/>
				<div class="mt-2 d-flex justify-end">
					<v-btn
						text
						outlined
						small
						color="primary"
						v-on:click="exportSearchResults"
						class="mr-1"
						v-if="searchResults"
						title="Eksportuoti .csv"
					>
						<v-icon size="18">mdi-download</v-icon>
					</v-btn>
					<v-btn
						text
						outlined
						small
						color="primary"
						v-on:click="clearSearchResults"
						class="mr-1"
						v-if="searchResults"
					>
						<v-icon left size="18">mdi-delete</v-icon> Valyti paieškos rezultatus
					</v-btn>
					<v-btn
						small
						color="primary lighten-1"
						v-on:click="executeSearch"
						:loading="searchInProgress"
						:disabled="!executeSearchVisible"
					>
						Atlikti paiešką
					</v-btn>
				</div>
				<div
					v-if="searchResults"
					class="mt-3 pt-2 search-results"
				>
					Rezultatų sk.: {{searchResults.items.length}}
				</div>
				<v-data-table
					:headers="searchResultsHeaders"
					:items="searchResults.items"
					class="mt-2 flex-grow-1 rounded-0 simple-data-table search-content-table"
					must-sort
					:options="{sortBy: ['edited'], sortDesc: [true]}"
					v-if="searchResults"
				>
					<template v-slot:item="{item}">
						<tr
							:class="['clickable', activeFeatureGlobalId == item.id ? 'primary lighten-4' : null]"
							v-on:click="onRowClick(item)"
						>
							<template v-for="(headerItem, j) in searchResultsHeaders">
								<td :class="['px-2 py-1 caption', (headerItem.value == 'created') || (headerItem.value == 'edited') ? 'date' : null]" :key="j">{{item[headerItem.value]}}</td>
							</template>
						</tr>
					</template>
				</v-data-table>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import EsriJSON from "ol/format/EsriJSON";
	import MyForm from "./../MyForm";
	import OverMapButtonContent from "./OverMapButtonContent";

	export default {
		data: function(){
			var data = {
				formData: null,
				title: null,
				btn: null,
				executeSearchVisible: false,
				searchInProgress: false,
				formDataWithResults: null,
				searchResults: null,
				activeFeatureGlobalId: null,
				searchResultsHeaders: null,
				activeStreetSignsSearchType: null
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			userData: {
				get: function(){
					return this.$store.state.userData;
				}
			},
			activeFeature: {
				get: function(){
					return this.$store.state.activeFeature;
				}
			}
		},

		components: {
			MyForm,
			OverMapButtonContent
		},

		created: function(){
			this.$vBus.$on("show-or-hide-street-signs-search", this.showOrHide);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-or-hide-street-signs-search", this.showOrHide);
		},

		methods: {
			showOrHide: function(e){
				this.title = e.title;
				this.btn = e.btn;
				this.$refs.wrapper.toggle(this.btn.$el.offsetLeft);
			},
			onFormDataChange: function(formData){
				var formEmpty = true;
				if (formData) {
					for (var key in formData) {
						if (formData[key]) {
							formEmpty = false;
							break;
						}
					}
					this.activeStreetSignsSearchType = formData["street-signs-search-type"];
				}
				this.executeSearchVisible = !formEmpty;
			},
			onOpen: function(){
				this.setFormData();
			},
			setFormData: function(){
				var formData = [{
					fields: [{
						title: "Paieškos kriterijus",
						key: "street-signs-search-type",
						domain: {
							codedValues: [{
								code: "vertical-street-signs",
								name: "Vertikalieji KŽ"
							},{
								code: "horizontal-street-signs",
								name: "Horizontalieji KŽ"
							},{
								code: "infrastructure-objects",
								name: "Kelio infrastruktūros objektai"
							},{
								code: "unapproved-vertical",
								name: "Nepatvirtinti objektai"
							},{
								code: "vertical-street-signs-supports",
								name: "Ženklų tvirtinimo vietos ir šviesoforai"
							// },{
								// code: "otherPolylines-25,26,27,211",
								// name: "Kiti objektai: 2.5, 2.6, 2.7, 2.11"
							}]
						}
					}]
				}];
				if (this.additionalFormFields) {
					formData[0].fields = formData[0].fields.concat(this.additionalFormFields);
				}
				formData[0].fields = formData[0].fields.concat({
					type: "geom-advanced",
					key: "geom-advanced",
					title: "Paieškos aprėptis"
				});
				if (this.formDataWithResults) {
					formData[0].fields.forEach(function(field){
						field.value = this.formDataWithResults[field.key];
					}.bind(this));
				}
				this.formData = formData;
			},
			executeSearch: function(){
				var formData = this.$refs.form.getData(),
					searchType = formData["street-signs-search-type"];
				if (searchType) {
					if (this.userData) {
						this.searchInProgress = true;
						this.clearSearchResults();
						this.formDataWithResults = formData;
						this.getSearchResults({
							"street-signs-vertical": this.userData["vertical-street-signs-service-root"],
							"street-signs": this.userData["street-signs-service-root"]
						}, searchType, formData).then(function(searchResults){
							if (searchType == "otherPolylines-25,26,27,211") {
								this.searchResultsHeaders = [{
									value: "title",
									text: "Objekto ID"
								},{
									value: "type",
									text: "Objekto tipas"
								},{
									value: "created",
									text: "Sukurtas"
								},{
									value: "edited",
									text: "Redaguotas"
								}];
							} else if (searchType == "unapproved-vertical") {
								this.searchResultsHeaders = [{
									value: "title",
									text: "Objekto ID"
								},{
									value: "layer",
									text: "Objekto tipas"
								},{
									value: "created",
									text: "Sukurtas"
								},{
									value: "edited",
									text: "Redaguotas"
								}];
							} else if (searchType == "vertical-street-signs") {
								this.searchResultsHeaders = [{
									value: "title",
									text: "Objekto ID"
								},{
									value: "text",
									text: "Tekstas"
								},{
									value: "uniqueSymbol",
									text: "UNIK"
								},{
									value: "created",
									text: "Sukurtas"
								},{
									value: "edited",
									text: "Redaguotas"
								}];
							} else {
								this.searchResultsHeaders = [{
									value: "title",
									text: "Objekto ID"
								},{
									value: "created",
									text: "Sukurtas"
								},{
									value: "edited",
									text: "Redaguotas"
								}];
							}
							this.searchInProgress = false;
							this.searchResults = searchResults;
						}.bind(this), function(){
							this.searchInProgress = false;
						}.bind(this));
					}
				}
			},
			getSearchResults: function(serviceRoots, searchType, formData){
				var promise = new Promise(function(resolve, reject){
					var url,
						serviceId,
						where,
						layerDefs,
						layerIdMeta,
						layerInfo,
						layerId,
						verticalStreetSignsSupportsLayerIdMeta,
						verticalStreetSignsLayerIdMeta,
						typeSpl;
					if (searchType == "otherPolylines-25,26,27,211") {
						layerIdMeta = CommonHelper.layerIds["otherPolylines"];
						if (layerIdMeta) {
							serviceId = layerIdMeta[0];
							layerId = layerIdMeta[1];
							url = serviceRoots[serviceId];
							if (url) {
								url = CommonHelper.prependProxyIfNeeded(url) + "FeatureServer/" + layerId + "/query";
								where = "TIPAS IN (25, 26, 27, 211)";
							}
						}
					} else if (searchType == "unapproved-vertical") {
						verticalStreetSignsSupportsLayerIdMeta = CommonHelper.layerIds["verticalStreetSignsSupports"];
						verticalStreetSignsLayerIdMeta = CommonHelper.layerIds["verticalStreetSigns"];
						if (verticalStreetSignsSupportsLayerIdMeta && verticalStreetSignsLayerIdMeta) {
							if (verticalStreetSignsSupportsLayerIdMeta[0] == verticalStreetSignsLayerIdMeta[0]) {
								serviceId = verticalStreetSignsSupportsLayerIdMeta[0];
								url = serviceRoots[serviceId];
								if (url) {
									url = CommonHelper.prependProxyIfNeeded(url) + "FeatureServer/query";
									var extraWhere;
									if (CommonHelper.unapprovedFromUsers) {
										extraWhere = " OR last_edited_user IN ('" + CommonHelper.unapprovedFromUsers.join("','") + "')";
									}
									layerDefs = [{
										layerId: verticalStreetSignsSupportsLayerIdMeta[1],
										where: "(PATVIRTINIMAS = 0" + extraWhere + ")",
										outFields: "*"
									},{
										layerId: verticalStreetSignsLayerIdMeta[1],
										where: "(PATVIRTINTAS = 0" + extraWhere + ")",
										outFields: "*"
									}];
									if (formData["approver"]) {
										layerDefs.forEach(function(layerDef){
											layerDef.where += " AND TVIRTINTOJAS = " + formData["approver"];
										});
									}
								}
							}
						}
					} else if (searchType == "vertical-street-signs") {
						if (formData && formData["vertical-street-signs-type"]) {
							layerIdMeta = CommonHelper.layerIds["verticalStreetSigns"];
							layerInfo = this.myMap.getLayerInfo("verticalStreetSigns");
							if (layerIdMeta && layerInfo) {
								serviceId = layerIdMeta[0];
								layerId = layerIdMeta[1];
								url = serviceRoots[serviceId];
								if (url && layerInfo.typeIdField) {
									url = CommonHelper.prependProxyIfNeeded(url) + "FeatureServer/" + layerId + "/query";
									where = layerInfo.typeIdField + " = " + formData["vertical-street-signs-type"];
								}
							}
						} else {
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Nenurodytas KET numeris!"
							});
						}
					} else if (searchType == "horizontal-street-signs") {
						if (formData && formData["hor-type"]) {
							if (["horizontalPoints", "horizontalPolylines", "horizontalPolygons"].indexOf(formData["hor-type"]) != -1) {
								where = "";
								layerIdMeta = CommonHelper.layerIds[formData["hor-type"]];
								layerInfo = this.myMap.getLayerInfo(formData["hor-type"]);
								if (layerIdMeta && layerInfo) {
									serviceId = layerIdMeta[0];
									layerId = layerIdMeta[1];
									url = serviceRoots[serviceId];
									url = CommonHelper.prependProxyIfNeeded(url) + "FeatureServer/" + layerId + "/query";
								} else {
									this.$vBus.$emit("show-message", {
										type: "warning"
									});
								}
							} else {
								typeSpl = formData["hor-type"].split("-");
								if (typeSpl.length == 2) {
									layerIdMeta = CommonHelper.layerIds[typeSpl[0]];
									layerInfo = this.myMap.getLayerInfo(typeSpl[0]);
									if (layerIdMeta && layerInfo) {
										serviceId = layerIdMeta[0];
										layerId = layerIdMeta[1];
										url = serviceRoots[serviceId];
										if (url && layerInfo.typeIdField) {
											url = CommonHelper.prependProxyIfNeeded(url) + "FeatureServer/" + layerId + "/query";
											where = layerInfo.typeIdField + " = " + typeSpl[1];
										}
									}
								}
							}
							if (formData.wear) {
								var wear = formData.wear.split("—");
								if (wear.length == 2) {
									if (wear[0]) {
										if (where) {
											where += " AND ";
										}
										where += "NUSIDEVEJIMAS >= " + wear[0];
									}
									if (wear[1]) {
										if (where) {
											where += " AND ";
										}
										where += "NUSIDEVEJIMAS <= " + wear[1];
									}
								}
							} else {
								if (!where) {
									where = "1=1";
								}
							}
						} else {
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Nenurodytas objekto tipas!"
							});
						}
					} else if (searchType == "infrastructure-objects") {
						if (formData && formData["other-type"]) {
							typeSpl = formData["other-type"].split("-");
							if (typeSpl.length == 2) {
								layerIdMeta = CommonHelper.layerIds[typeSpl[0]];
								layerInfo = this.myMap.getLayerInfo(typeSpl[0]);
								if (layerIdMeta && layerInfo) {
									serviceId = layerIdMeta[0];
									layerId = layerIdMeta[1];
									url = serviceRoots[serviceId];
									if (url && layerInfo.typeIdField) {
										url = CommonHelper.prependProxyIfNeeded(url) + "FeatureServer/" + layerId + "/query";
										where = layerInfo.typeIdField + " = " + typeSpl[1];
									}
								}
							}
						} else {
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Nenurodytas objekto tipas!"
							});
						}
					} else if (searchType == "vertical-street-signs-supports") {
						if (formData && formData["vertical-street-sign-support-type"]) {
							layerIdMeta = CommonHelper.layerIds["verticalStreetSignsSupports"];
							layerInfo = this.myMap.getLayerInfo("verticalStreetSignsSupports");
							if (layerIdMeta && layerInfo) {
								serviceId = layerIdMeta[0];
								layerId = layerIdMeta[1];
								url = serviceRoots[serviceId];
								if (url && layerInfo.typeIdField) {
									url = CommonHelper.prependProxyIfNeeded(url) + "FeatureServer/" + layerId + "/query";
									where = layerInfo.typeIdField + " = '" + formData["vertical-street-sign-support-type"] + "'";
								}
							}
						} else {
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Nenurodytas objekto tipas!"
							});
						}
					}
					if (url && (where || layerDefs)) {
						var params = {
							f: "json"
						};
						if (where) {
							if (searchType == "vertical-street-signs" || searchType == "vertical-street-signs-supports") {
								if (searchType == "vertical-street-signs") {
									where += " AND NOT (STATUSAS = " + CommonHelper.verticalStreetSignDestroyedStatusValue + " AND PATVIRTINTAS = 1)"; // FIXME! Ar aktuali tokia sąlyga?..
								}
							}
							params.where = where;
							params.outFields = "*";
							params.orderByFields = "last_edited_date DESC";
						} else if (layerDefs) {
							params.layerDefs = JSON.stringify(layerDefs);
						}
						if (formData["geom-advanced"]) {
							var geomAdvanced = formData["geom-advanced"];
							if (this.myMap.zonesFeatures) {
								var zoneFeature;
								this.myMap.zonesFeatures.some(function(feature){
									if ((feature.attributes["OBJECTID"] + "") == geomAdvanced) {
										zoneFeature = feature;
										return true;
									}
								});
								if (zoneFeature) {
									if (zoneFeature.geometry && zoneFeature.geometry.rings) {
										params.geometryType = "esriGeometryPolygon";
										params.geometry = JSON.stringify(zoneFeature.geometry);
									}
								}
							}
						}
						CommonHelper.getFetchPromise(url, function(json){
							return json;
						}.bind(this), "POST", params).then(function(result){
							this.searchResultsRaw = result; // Prireiks objektų pavaizdavimui žemėlapyje...
							var searchResults = {
								items: [],
								serviceId: serviceId,
								layerId: layerId
							};
							var item;
							if (params.where) {
								if (result.features) {
									result.features.forEach(function(feature){
										item = {
											// TODO, FIXME! Turi imti iš serviso aprašymo!?
											id: feature.attributes[result.globalIdFieldName],
											serviceId: serviceId,
											layerId: layerId,
											feature: feature,
											title: feature.attributes[result.objectIdFieldName],
											created: CommonHelper.getPrettyDate(feature.attributes["created_date"]),
											edited: CommonHelper.getPrettyDate(feature.attributes["last_edited_date"])
										};
										if (searchType == "otherPolylines-25,26,27,211") {
											item.type = feature.attributes["TIPAS"]; // TODO, FIXME! Gauti tik jei toks laukas yra result'o field'ų masyve?..
										} else if (searchType == "vertical-street-signs") {
											item.text = feature.attributes[CommonHelper.symbolTextFieldName]; // TODO, FIXME! Gauti tik jei toks laukas yra result'o field'ų masyve?..
											item.uniqueSymbol = feature.attributes[CommonHelper.customSymbolIdFieldName] ? "+" : null
										}
										searchResults.items.push(item);
									});
								}
							} else if (params.layerDefs) {
								if (result.layers) {
									result.layers.forEach(function(layer){
										if (layer.features) {
											layer.features.forEach(function(feature){
												item = {
													// TODO, FIXME! Turi imti iš serviso aprašymo!?
													id: feature.attributes[layer.globalIdFieldName],
													title: feature.attributes[layer.objectIdFieldName],
													created: CommonHelper.getPrettyDate(feature.attributes["created_date"]),
													edited: CommonHelper.getPrettyDate(feature.attributes["last_edited_date"]),
													serviceId: serviceId,
													layerId: layer.id,
													feature: feature
												};
												if (searchType == "unapproved-vertical") {
													item.layer = layer.id;
													if (verticalStreetSignsSupportsLayerIdMeta) {
														if (item.layer == verticalStreetSignsSupportsLayerIdMeta[1]) {
															item.layer = "Tvirtinimo vieta";
														}
													}
													if (verticalStreetSignsLayerIdMeta) {
														if (item.layer == verticalStreetSignsLayerIdMeta[1]) {
															item.layer = "KŽ";
														}
													}
												}
												searchResults.items.push(item);
											});
										}
									});
								}
							}
							resolve(searchResults);
						}.bind(this), function(){
							reject();
						});
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			},
			clearSearchResults: function(){
				this.searchResults = null;
			},
			onRowClick: function(item){
				var activeFeatureGlobalId = null,
					featureData;
				if (item.id != this.activeFeatureGlobalId) {
					activeFeatureGlobalId = item.id;
				}
				this.activeFeatureGlobalId = activeFeatureGlobalId;
				if (activeFeatureGlobalId) {
					featureData = {
						globalId: item.id,
						serviceId: item.serviceId,
						layerId: item.layerId
					};
				}
				CommonHelper.routeTo({
					router: this.$router,
					vBus: this.$vBus,
					featureData: featureData
				});
			},
			exportSearchResults: function(){
				if (this.searchResults && this.searchResults.items) {
					var key,
						fields,
						csvContent = "data:text/csv;charset=utf-8,";
					if (this.searchResults.serviceId && Number.isInteger(parseInt(this.searchResults.layerId))) {
						if (CommonHelper.layerIds) {
							var layerDataToSearch = JSON.stringify([this.searchResults.serviceId, this.searchResults.layerId]);
							for (var k in CommonHelper.layerIds) {
								if (layerDataToSearch == JSON.stringify(CommonHelper.layerIds[k])) {
									key = k;
								}
							}
						}
					} else {
						console.log("No serviceId, layerId");
					}
					if (key) {
						fields = CommonHelper.importantFields[key];
					}
					if (!fields) {
						fields = ["OBJECTID"];
					}
					var fieldsForHeader = fields.slice(),
						fieldsForData = fields.slice();
					if (key) {
						var layerInfo = this.$store.state.myMap.getLayerInfo(key);
						if (layerInfo && layerInfo.objectIdField) {
							if (fieldsForHeader.indexOf(layerInfo.objectIdField) == -1) {
								fieldsForHeader.push(layerInfo.objectIdField);
								fieldsForData.push(layerInfo.objectIdField);
							}
						}
						if (key == "horizontalPolylines") {
							fieldsForHeader.some(function(f, i){
								if (f == CommonHelper.widthFieldName) {
										fieldsForHeader[i] = "PLOTIS";
									return true;
								}
							});
						}
					}
					fieldsForHeader.push("X");
					fieldsForHeader.push("Y");
					fieldsForHeader.push("Plotas");
					csvContent += fieldsForHeader.join(",") + "\r\n";
					var esriJsonFormat = new EsriJSON(),
						row;
					this.searchResults.items.forEach(function(result){
						row = [];
						fieldsForData.forEach(function(field){
							row.push(result.feature.attributes[field]);
						});
						if (result.feature.geometry.x) {
							row.push(result.feature.geometry.x.toFixed(2));
						} else {
							row.push(null);
						}
						if (result.feature.geometry.y) {
							row.push(result.feature.geometry.y.toFixed(2));
						} else {
							row.push(null);
						}
						if (key) {
							try {
								var area = CommonHelper.getPaintedArea({
									feature: esriJsonFormat.readFeature(result.feature),
									featureType: key
								}, this.myMap, false);
								row.push(area);
							} catch(err) {
								console.log("Area error", result); // ...
							}
						}
						csvContent += row.join(",") + "\r\n";
					}.bind(this));
					var encodedUri = encodeURI(csvContent),
						link = document.createElement("a");
					link.setAttribute("href", encodedUri);
					link.setAttribute("download", new Date().getTime() + ".csv");
					document.body.appendChild(link); 
					link.click();
				}
			}
		},

		watch: {
			activeFeature: {
				immediate: true,
				handler: function(activeFeature){
					if (activeFeature && activeFeature.globalId) {
						// Kažin ar yra tikimybė, kad tokie pat GlobalID būtų skirtinguose sluoksniuose, t. y. objektai su tokiu pačiu GlobalID skirtinguose sluoksniuose?
						// Jei taip, šita logika nėra `bulletproof`...
						this.activeFeatureGlobalId = "{" + activeFeature.globalId + "}";
					} else {
						this.activeFeatureGlobalId = null;
					}
				}
			},
			searchResults: {
				immediate: true,
				handler: function(searchResults){
					if (this.myMap) {
						if (searchResults) {
							this.myMap.searchResults = this.searchResultsRaw;
						} else {
							this.myMap.searchResults = null;
						}
					}
				}
			},
			activeStreetSignsSearchType: {
				immediate: true,
				handler: function(activeStreetSignsSearchType){
					var fields,
						field;
					if (activeStreetSignsSearchType == "vertical-street-signs") {
						this.additionalFormFields = [{
							title: "KET numeris",
							key: "vertical-street-signs-type",
							type: "street-sign-vertical"
						}];
					} else if (activeStreetSignsSearchType == "horizontal-street-signs") {
						var horizontalObjectsFormDataCodedValues = [{
							code: "horizontalPoints",
							name: "-Visi taškiniai horizontalaus ženklinimo KŽ-"
						},{
							code: "horizontalPolylines",
							name: "-Visi linijiniai horizontalaus ženklinimo KŽ-"	
						},{
							code: "horizontalPolygons",
							name: "-Visi plotiniai horizontalaus ženklinimo KŽ-"
						}];
						["horizontalPoints", "horizontalPolylines", "horizontalPolygons"].forEach(function(key){
							fields = this.myMap.getLayerFields(key);
							field = this.myMap.getLayerField(fields, "KET_NR");
							if (field && field.domain) {
								var groupValues = [];
								field.domain.codedValues.forEach(function(codedValue){
									groupValues.push({
										code: key + "-" + codedValue.code,
										name: codedValue.name
									});
								});
								CommonHelper.simpleSort(groupValues, "name");
								horizontalObjectsFormDataCodedValues = horizontalObjectsFormDataCodedValues.concat(groupValues);
							}
						}.bind(this));
						this.additionalFormFields = [{
							title: "Tipas",
							key: "hor-type",
							domain: {
								codedValues: horizontalObjectsFormDataCodedValues
							}
						},{
							title: "Nusidėvėjimas",
							key: "wear",
							type: "range",
							start: 0,
							end: 100
						}];
					} else if (activeStreetSignsSearchType == "infrastructure-objects") {
						var otherObjectsFormDataCodedValues = [];
						["otherPoints", "otherPolylines", "otherPolygons"].forEach(function(key){
							fields = this.myMap.getLayerFields(key);
							field = this.myMap.getLayerField(fields, "TIPAS");
							if (field && field.domain) {
								field.domain.codedValues.forEach(function(codedValue){
									otherObjectsFormDataCodedValues.push({
										code: key + "-" + codedValue.code,
										name: codedValue.name
									});
								});
							}
						}.bind(this));
						this.additionalFormFields = [{
							title: "Tipas",
							key: "other-type",
							domain: {
								codedValues: otherObjectsFormDataCodedValues
							}
						}];
					} else if (activeStreetSignsSearchType == "unapproved-vertical") {
						fields = this.myMap.getLayerFields("verticalStreetSigns");
						field = this.myMap.getLayerField(fields, "TVIRTINTOJAS");
						if (field && field.domain) {
							this.additionalFormFields = [{
								title: "Tvirtintojas",
								key: "approver",
								domain: {
									codedValues: field.domain.codedValues
								}
							}];
						}
					} else if (activeStreetSignsSearchType == "vertical-street-signs-supports") {
						fields = this.myMap.getLayerFields("verticalStreetSignsSupports");
						field = this.myMap.getLayerField(fields, "TVIRTINIMO_VIETA");
						if (field && field.domain) {
							this.additionalFormFields = [{
								title: "Tipas",
								key: "vertical-street-sign-support-type",
								domain: {
									codedValues: field.domain.codedValues
								}
							}];
						}
					} else {
						this.additionalFormFields = null;
					}
					this.setFormData();
				}
			}
		}
	}
</script>

<style scoped>
	.search-results {
		border-top: 1px solid #eeeeee;
	}
	.v-data-table {
		overflow: auto;
	}
	.clickable {
		cursor: pointer;
	}
	.v-data-table td {
		height: auto !important;
	}
	.v-data-table .date {
		width: 110px;
	}
</style>