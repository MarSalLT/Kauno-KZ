<template>
	<OverMapButtonContent
		type="tasks-search"
		:title="title"
		:btn="btn"
		:onOpen="onOpen"
		ref="wrapper"
	>
		<template v-slot>
			<div class="body-2">
				<template v-if="loading">
					<v-progress-circular
						indeterminate
						color="primary"
						:size="30"
						width="2"
					></v-progress-circular>
				</template>
				<template v-else>
					<div>
						<template v-if="features">
							<template v-if="features == 'error'">
								<v-alert
									dense
									type="error"
									class="ma-0 d-inline-block"
								>
									Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
								</v-alert>
							</template>
							<template v-else>
								<div>
									Rezultatų sk.: {{features.length}}
								</div>
								<v-data-table
									:headers="listHeaders"
									:items="features"
									class="mt-3 mb-n2 rounded-0 simple-data-table search-content-table"
									must-sort
									:options="{sortBy: ['title'], sortDesc: [false]}"
									v-if="features.length"
								>
									<template v-slot:item="{item}">
										<tr
											:class="['clickable', activeTaskId == item.id ? 'primary lighten-4' : null]"
											v-on:click="onRowClick(item)"
										>
											<template v-for="(headerItem, j) in listHeaders">
												<td :class="['px-2 py-1 caption']" :key="j">
													<template v-if="headerItem.value == 'status'">
														<TaskStatusCode
															:code="item.statusCode"
															:title="item[headerItem.value]"
															:approved="item.appr"
														/>
													</template>
													<template v-else-if="headerItem.value == 'created'">
														<span class="text-no-wrap">{{item[headerItem.value]}}</span>
													</template>
													<template v-else>
														{{item[headerItem.value]}}
													</template>
												</td>
											</template>
										</tr>
									</template>
								</v-data-table>
							</template>
						</template>
						<v-divider :class="[features.length ? 'mt-4 mb-4' : 'mt-3 mb-4']"></v-divider>
						<div>
							<v-btn
								color="blue darken-1"
								text
								v-on:click="executeSearch"
								outlined
								small
							>
								<v-icon left size="18">mdi-reload</v-icon>
								Perkrauti
							</v-btn>
						</div>
					</div>
				</template>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import MapHelper from "../helpers/MapHelper";
	import OverMapButtonContent from "./OverMapButtonContent";
	import TaskStatusCode from "./TaskStatusCode";
	import TaskHelper from "../helpers/TaskHelper";

	export default {
		data: function(){
			var data = {
				title: null,
				btn: null,
				features: null,
				loading: null,
				listHeaders: [{
					value: "title",
					text: "Pavadinimas"
				},{
					value: "description",
					text: "Aprašymas"
				},{
					value: "created",
					text: "Sukurta"
				},{
					value: "status",
					text: "Būsena"
				}]
			};
			return data;
		},

		computed: {
			activeTaskId: {
				get: function(){
					var activeTaskId = null;
					if (this.$store.state.activeTask) {
						activeTaskId = this.$store.state.activeTask.globalId;
					}
					return activeTaskId;
				}
			}
		},

		components: {
			OverMapButtonContent,
			TaskStatusCode
		},

		created: function(){
			this.$vBus.$on("show-or-hide-tasks-search", this.showOrHide);
			this.$vBus.$on("refresh-tasks-search", this.executeSearchIfOpen);
			this.$vBus.$on("update-tasks-list-item", this.updateTasksListItem);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-or-hide-tasks-search", this.showOrHide);
			this.$vBus.$off("refresh-tasks-search", this.executeSearchIfOpen);
			this.$vBus.$off("update-tasks-list-item", this.updateTasksListItem);
		},

		methods: {
			showOrHide: function(e){
				this.title = e.title;
				this.btn = e.btn;
				this.$refs.wrapper.toggle(this.btn.$el.offsetLeft);
			},
			onOpen: function(){
				if (!this.searchExecuted) {
					this.executeSearch();
					this.searchExecuted = true;
				}
			},
			executeSearch: function(){
				this.loading = true;
				this.getFeatures().then(function(features){
					MapHelper.getTasksServiceCapabilities(this.$store.state.myMap).then(function(e){
						var fields = {};
						if (e) {
							if (CommonHelper.layerIds["tasks"]) {
								var layerInfo = e[CommonHelper.layerIds["tasks"][1]];
								if (layerInfo && layerInfo.fields) {
									layerInfo.fields.forEach(function(field){
										fields[field.name] = field;
									});
								}
							}
						}
						this.loading = false;
						var featuresMod = [],
							featureMod;
						features.forEach(function(feature){
							featureMod = this.getFeatureMod(feature, fields);
							featuresMod.push(featureMod);
						}.bind(this));
						this.features = featuresMod;
						this.fields = fields; // Prireiks update-tasks-list-item'ui...
					}.bind(this), function(){
						this.loading = false;
						this.features = "error";
					}.bind(this));
				}.bind(this), function(){
					this.loading = false;
					this.features = "error";
				}.bind(this));
			},
			executeSearchIfOpen: function(){
				if (this.$refs.wrapper.open) {
					this.executeSearch();
				}
			},
			getFeatures: function(){
				var promise = new Promise(function(resolve, reject){
					var url = CommonHelper.prependProxyIfNeeded(this.$store.getters.getServiceUrl("tasks"));
					if (url) {
						if (CommonHelper.layerIds["tasks"]) {
							url += "/" + CommonHelper.layerIds["tasks"][1] + "/query";
							var params = {
								f: "json",
								outFields: "*",
								returnGeometry: true,
								where: process.env.VUE_APP_TASKS_STATE == "prod" ? "Aplinka = 2" : "Aplinka = 1",
								outSR: 3346 // FIXME: ne'hardcode'inti...
							};
							CommonHelper.getFetchPromise(url, function(json){
								if (json.features) {
									json.features.forEach(function(feature){
										if (feature.geometry) {
											feature.geometry.spatialReference = json.spatialReference;
										}
									});
									return json.features;
								}
							}.bind(this), "POST", params).then(function(result){
								resolve(result);
							}, function(){
								reject();
							});
						} else {
							reject();
						}
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			},
			getFeatureMod: function(feature, fields){
				var globalIdFieldName = "GlobalID";
				var featureMod = {
					id: feature.attributes[globalIdFieldName],
					title: feature.attributes["Pavadinimas"] || "Be pavadinimo", // FIXME! Ne hardcode'inti!
					description: feature.attributes["Aprasymas"], // FIXME! Ne hardcode'inti!
					status: TaskHelper.getPrettyValue(feature, "Statusas", fields), // FIXME! Ne hardcode'inti!
					statusCode: feature.attributes["Statusas"],
					created: CommonHelper.getPrettyDate(feature.attributes["created_date"], false),
					appr: feature.attributes["Patvirtinimas"],
					feature: feature
				};
				return featureMod;
			},
			onRowClick: function(item){
				if (this.activeTaskId != item.id) {
					this.$store.commit("setActiveTask", null); // Gal be šito veiksmo galima apsieiti, bet taip saugiau?.. Bent jau išsisprendžia viena bėda... Pvz. aktyvioje užduotyje perstumiame normalųjų objektą... Ir tada šioje užduočių lentelėje spaudžiamr ant kito įrašo... Seniau likdavo kažkokių artefaktų? Dabar nebe...
					setTimeout(function(){
						this.$store.commit("setActiveTask", {
							globalId: item.id
						});
					}.bind(this), 0);
				}
			},
			updateTasksListItem: function(result){
				var itemGlobalId;
				if (result && result.feature && result.feature.attributes) {
					itemGlobalId = result.feature.attributes["GlobalID"];
				}
				if (itemGlobalId) {
					if (this.features) {
						if (Array.isArray(this.features)) {
							this.features.forEach(function(f){
								if (f.id == itemGlobalId) {
									var featureMod = this.getFeatureMod(result.feature, this.fields);
									f = Object.assign(f, featureMod);
								}
							}.bind(this));
						}
					}
				}
			}
		}
	}
</script>

<style scoped>
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