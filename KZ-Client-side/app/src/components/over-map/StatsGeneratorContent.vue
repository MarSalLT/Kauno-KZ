<template>
	<OverMapButtonContent
		type="stats-generator"
		:title="title"
		:btn="btn"
		:onOpen="onOpen"
		ref="wrapper"
	>
		<template v-slot>
			<div class="body-2">
				<div class="mb-3">
					<strong>Statistikos skaičiavimo objektas:</strong>
				</div>
				<v-radio-group
					v-model="dataType"
					class="ma-0 pa-0 ml-5 body-2"
					hide-details
					column
				>
					<template v-for="(item, i) in dataTypeItems">
						<v-radio
							:label="item.title"
							:value="item.value"
							:key="i"
							:class="['ma-0 pa-0', i ? 'mt-2' : null]"
						></v-radio>
						<template v-if="item.subtypes">
							<div class="ml-9 mt-1 mb-n2" :key="i + '-s'">
								<v-radio-group
									v-model="dataSubtypes[item.value]"
									class="ma-0 pa-0 radio-group-row"
									hide-details
									row
								>
									<template v-for="(subtype, j) in item.subtypes">
										<v-radio
											:label="subtype.title"
											:value="subtype.value"
											:key="i + '-' + j"
											class="mr-2"
											:disabled="item.value != dataType"
										></v-radio>
									</template>
								</v-radio-group>
							</div>
						</template>
						<template v-if="item.additionalItems">
							<div class="ml-9 mt-2 mb-n1" :key="i + '-a'">
								<template v-for="(additionalItem, j) in item.additionalItems">
									<template v-if="additionalItem.domain">
										<div
											:key="i + '-' + j + '-a'"
											class="d-flex align-center compact-form"
										>
											<label :class="['mr-2 text-no-wrap', item.value == dataType ? null : 'grey--text']" :for="'stats-addit-' + j">
												{{additionalItem.title}}:
											</label>
											<SelectField
												v-model="additionalItems[additionalItem.key]"
												:id="'stats-addit-' + j"
												:items="additionalItem.domain.codedValues"
												:editable="item.value == dataType"
											/>
										</div>
									</template>
								</template>
							</div>
						</template>
					</template>
				</v-radio-group>
				<div class="mb-0 mt-4">
					<strong>Papildomi statistikos skaičiavimo kriterijai:</strong>
				</div>
				<div class="d-flex align-center">
					<MyForm
						:data="additionalSettingsFormData"
						id="form-data"
						:onDataChange="onAdditionalSettingsFormDataChange"
						ref="form"
						class="compact-form"
					/>
				</div>
				<div class="mt-3 d-flex justify-end">
					<v-btn
						small
						color="primary lighten-1"
						v-on:click="executeAnalysis"
						:loading="analysisInProgress"
					>
						Vykdyti statistikos skaičiavimą
					</v-btn>
				</div>
				<div v-if="result">
					<v-divider class="mt-5 mb-4"></v-divider>
					<strong>Rezultatas:</strong> <span v-html="result"></span>
				</div>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import MyForm from "../MyForm";
	import OverMapButtonContent from "./OverMapButtonContent";
	import SelectField from "../fields/SelectField";

	export default {
		data: function(){
			var geometryItems = [{
				title: "Taškiniai",
				value: "points"
			},{
				title: "Linijiniai",
				value: "lines"
			},{
				title: "Plotiniai",
				value: "polygons"
			}];
			var geometryItemsWithAll = geometryItems.slice();
			geometryItemsWithAll.unshift({
				title: "Visi",
				value: "all"
			})
			var data = {
				title: null,
				btn: null,
				analysisInProgress: false,
				dataType: "vertical",
				dataSubtypes: {
					horizontal: "all",
					infra: "points"
				},
				additionalItems: {},
				dataTypeItems: [{
					title: "Vertikalieji kelio ženklai",
					value: "vertical"
				},{
					title: "Horizontalusis ženklinimas",
					value: "horizontal",
					subtypes: geometryItemsWithAll,
					additionalItems: [{
						title: "Ženklinimo būdas",
						key: "marking",
						domain: {
							codedValues: [{
								code: "Antislydiminis plastikas",
								name: "Antislydiminis plastikas"
							},{
								code: "Asfaltas",
								name: "Asfaltas"
							},{
								code: "Dazai",
								name: "Dažai"
							},{
								code: "Metalas",
								name: "Metalas"
							},{
								code: "Plastikas",
								name: "Plastikas"
							},{
								code: "Plyteles",
								name: "Plytelės"
							},{
								code: "Termoplastas",
								name: "Termoplastas"
							},{
								code: "Kitas",
								name: "Kitas"
							}]
						}
					}]
				},{
					title: "Kelio infrastruktūros objektai",
					value: "infra",
					subtypes: geometryItems
				}],
				additionalSettingsFormData: null,
				savedAdditionalSettingsFormData: null,
				gpRoot: CommonHelper.statsGPUrl,
				result: null
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		components: {
			MyForm,
			OverMapButtonContent,
			SelectField
		},

		created: function(){
			this.$vBus.$on("show-or-hide-stats-generator", this.showOrHide);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-or-hide-stats-generator", this.showOrHide);
		},

		methods: {
			showOrHide: function(e){
				this.title = e.title;
				this.btn = e.btn;
				this.$refs.wrapper.toggle(this.btn.$el.offsetLeft);
			},
			onAdditionalSettingsFormDataChange: function(formData){
				this.savedAdditionalSettingsFormData = formData;
			},
			onOpen: function(){
				this.setFormData();
			},
			setFormData: function(){
				var values = this.savedAdditionalSettingsFormData || {};
				var additionalSettingsFormData = [{
					fields: [{
						type: "geom-advanced",
						key: "geom-advanced",
						title: "Paieškos aprėptis",
						additionalTopItem: {
							code: "all",
							name: "Visa teritorija"
						},
						useNameAsCode: true,
						value: values["geom-advanced"] || "all"
					},{
						type: "radio-button",
						key: "created-or-edited",
						title: "Ieškoti tik",
						items: [{
							title: "Sukurtus",
							value: "created"
						},{
							title: "Redaguotus",
							value: "edited"
						},{
							title: "Sukurtus arba redaguotus",
							value: "created-or-edited"
						}],
						value: values["created-or-edited"] || "created-or-edited",
						spaced: true
					},{
						title: "Data nuo",
						key: "date-from",
						type: "date",
						withTime: true,
						value: values["date-from"]
					}]
				}];
				this.additionalSettingsFormData = additionalSettingsFormData;
			},
			executeAnalysis: function(){
				this.analysisInProgress = true;
				var territoryKey = null,
					createdOrEdited,
					dateFrom;
				if (this.savedAdditionalSettingsFormData) {
					if (this.savedAdditionalSettingsFormData["geom-advanced"]) {
						if (this.savedAdditionalSettingsFormData["geom-advanced"] == "all") {
							territoryKey = "Visa teritorija";
						} else {
							territoryKey = this.savedAdditionalSettingsFormData["geom-advanced"];
						}
					}
					if (this.savedAdditionalSettingsFormData["created-or-edited"]) {
						var createdOrEditedDict = {
							"created": "Sukurtas",
							"edited": "Redaguotas",
							"created-or-edited": "Sukurtas arba redaguotas"
						};
						createdOrEdited = createdOrEditedDict[this.savedAdditionalSettingsFormData["created-or-edited"]];
					}
					if (this.savedAdditionalSettingsFormData["date-from"]) {
						dateFrom = parseInt(this.savedAdditionalSettingsFormData["date-from"]);
					}
				}
				var url,
					params = {
						f: "json",
						"teritory": territoryKey,
						"creation_type": createdOrEdited
					},
					jobRoot = this.gpRoot,
					resultType = "count";
				if (dateFrom) {
					// https://developers.arcgis.com/rest/services-reference/enterprise/gp-data-types.htm
					params["date"] = dateFrom;
				}
				var dataSubtypes = {
					points: "Taskai",
					lines: "Linijos",
					polygons: "Plotai",
					all: "Visi"
				};
				var dataSubtype = this.dataSubtypes[this.dataType];
				switch (this.dataType) {
					case "vertical":
						jobRoot += "KZTool/GPServer/";
						url = jobRoot + "Vertikaliųjų%20kelio%20ženklų%20kiekis";
						// TODO: nustatyti parametrus...
						break;
					case "horizontal":
						jobRoot += "HZTool/GPServer/";
						url = jobRoot + "Horizontaliųjų%20kelio%20ženklų%20plotas";
						params["input_feature"] = dataSubtypes[dataSubtype];
						if (this.additionalItems["marking"]) {
							params["marking"] = JSON.stringify([
								this.additionalItems["marking"]
							]);
						}
						resultType = "area";
						break;
					case "infra":
						jobRoot += "INFRTool/GPServer/";
						url = jobRoot + "Infrastruktūros%20objektų%20statistika";
						params["input_features"] = dataSubtypes[dataSubtype];
						if (dataSubtype == "lines" || dataSubtype == "polygons") {
							resultType = "area";
						}
						break;
				}
				url += "/submitJob";
				this.submitJob(url, params).then(function(jobId){
					var jobUrl = jobRoot + "jobs/" + jobId;
					var task = window.setInterval(function(){
						this.checkJob(jobUrl).then(function(response){
							if (["esriJobSucceeded", "esriJobFailed", "esriJobTimedOut"].indexOf(response.jobStatus) != -1 || response.error) {
								window.clearInterval(task);
								if (response.jobStatus == "esriJobSucceeded" && response.results) {
									this.getResults(jobUrl, response.results).then(function(result){
										var resultValue = result.value;
										if (resultType == "count") {
											resultValue += " vnt.";
										} else if (resultType == "area") {
											if (result.dataType == "GPDouble") {
												resultValue = resultValue.toFixed();
											} else {
												if (this.dataType == "horizontal") {
													if (params["marking"]) {
														if (resultValue) {
															resultValue = resultValue[this.additionalItems["marking"]];
															if (resultValue) {
																resultValue = resultValue.toFixed();
															}
														}
													} else {
														resultValue = resultValue.replace(",", ".");
														resultValue = parseFloat(resultValue).toFixed();
													}
												}
											}
											resultValue += " m<sup>2</sup>";
										}
										this.result = resultValue;
										this.analysisInProgress = false;
									}.bind(this), function(){
										this.analysisInProgress = false;
									}.bind(this));
								} else {
									this.analysisInProgress = false;
								}
							}
						}.bind(this), function(){
							window.clearInterval(task);	
						});
					}.bind(this), 2000);
				}.bind(this), function(){
					this.analysisInProgress = false;
				}.bind(this));
			},
			submitJob: function(url, params){
				this.result = null;
				var promise = new Promise(function(resolve, reject){
					if (url) {
						CommonHelper.getFetchPromise(url, function(json){
							return json;
						}.bind(this), "POST", params).then(function(result){
							if (result && result.jobId) {
								resolve(result.jobId);
							} else {
								reject();
							}
						}, function(){
							reject();
						});
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			},
			checkJob: function(url){
				var params = {
					f: "json"
				};
				var promise = new Promise(function(resolve, reject){
					CommonHelper.getFetchPromise(url, function(json){
						return json;
					}.bind(this), "POST", params).then(function(result){
						if (result) {
							resolve(result);
						} else {
							reject();
						}
					}, function(){
						reject();
					});
				}.bind(this));
				return promise;
			},
			getResults: function(jobUrl, res){
				var promise = new Promise(function(resolve, reject){
					var resultParam = res.count || res.statistic;
					if (res && resultParam) {
						var url = jobUrl + "/" + resultParam.paramUrl;
						var params = {
							f: "json"
						};
						CommonHelper.getFetchPromise(url, function(json){
							return json;
						}.bind(this), "POST", params).then(function(result){
							if (result) {
								if (result.value && result.value.error) {
									reject();
								} else {
									resolve(result);
								}
							} else {
								reject();
							}
						}, function(){
							reject();
						});
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			}
		}
	}
</script>