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
					mdi-text-box-multiple
				</v-icon>
			</v-btn>
		</v-badge>
		<v-dialog
			persistent
			v-model="dialog"
			:max-width="1800"
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
									<template v-slot:item="{item}">
										<tr
											:class="['clickable', item.unseen ? 'unseen error lighten-5' : null]"
											v-on:click="onRowClick(item)"
										>
											<template v-for="(headerItem, j) in listHeaders">
												<td :class="['px-2 py-1 caption']" :key="j">
													<template v-if="headerItem.value == 'Statusas'">
														<TaskStatusCode
															:code="item['Statusas']"
															:title="item.statusAlias || item['Statusas']"
															:approved="item['Patvirtinimas']"
														/>
													</template>
													<template v-else-if="['created_date', 'last_edited_date', 'Pabaigos_data'].indexOf(headerItem.value) != -1">
														<span class="text-no-wrap">{{item[headerItem.value]}}</span>
													</template>
													<template v-else-if="['Pavadinimas'].indexOf(headerItem.value) != -1">
														<span :title="item[headerItem.value]">{{item.titleShort || item[headerItem.value]}}</span>
													</template>
													<template v-else-if="headerItem.value == 'URL'">
														<span>{{item[headerItem.value]}}</span>
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
	import MapHelper from "../helpers/MapHelper";
	import TaskHelper from "../helpers/TaskHelper";
	import TaskStatusCode from "../over-map/TaskStatusCode";

	export default {
		data: function(){
			var data = {
				title: "Užduočių sąrašas",
				loading: true,
				count: "?",
				list: null,
				dialog: false,
				listHeaders: [{
					value: "URL",
					text: "Einpix nr.",
					width: 50
				},{
					value: "Pavadinimas",
					text: "Pavadinimas",
					width: 200
				},{
					value: "Statusas",
					text: "Būsena",
					width: 50
				},{
					value: "Uzduoties_tipas",
					text: "Kodas",
					width: 50
				},{
					value: "Teritorija",
					text: "Teritorija",
					width: 50
				},{
					value: "type",
					text: "Tipas",
					width: 50
				},{
					value: "Svarba",
					text: "Svarba",
					width: 50
				},{
					value: "created_date",
					text: "Sukūrimo data",
					width: 60
				},{
					value: "last_edited_date",
					text: "Paskutinis redagavimas",
					width: 60
				},{
					value: "Pabaigos_data",
					text: "Užbaigimo data",
					width: 60
				},{
					value: "uzsakovo_email",
					text: "Užsakovas",
					width: 50
				},{
					value: "rangovo_email",
					text: "Vykdytojas",
					width: 50
				},{
					value: "last_time_opened",
					text: "Paskutinį kartą žiūrėta",
					width: 50
				}],
				listOptions: {
					sortBy: ["last_edited_date"],
					sortDesc: [true],
					mustSort: true,
					itemsPerPage: 50
				},
				search: null
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
			TaskStatusCode
		},

		mounted: function(){
			this.getList();
		},

		methods: {
			getList: function(){
				var doGetList = function(){
					this.loading = true;
					this.list = null;
					this.getTasks().then(function(list){
						this.loading = false;
						var importantItemsCount = 0;
						if (list) {
							list.forEach(function(item){
								if (item.unseen) {
									importantItemsCount += 1;
								}
							});
						}
						this.list = list;
						this.count = importantItemsCount += "";
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
				}.bind(this);
				if (this.fields) {
					doGetList();
				} else {
					this.loading = true;
					MapHelper.getTasksServiceCapabilities(this.myMap).then(function(){
						if (CommonHelper.layerIds["tasks"]) {
							var layerInfo = this.myMap.getLayerInfo("tasks");
							if (layerInfo && layerInfo.fields) {
								var fields = {};
								layerInfo.fields.forEach(function(field){
									fields[field.name] = field;
								});
								this.fields = fields;
								doGetList();
							} else {
								this.loading = false;
							}
						} else {
							this.loading = false;
						}
					}.bind(this), function(){
						this.loading = false;
						this.list = "error";
						this.count = "!";
					}.bind(this));
				}
			},
			getTasks: function(){
				var promise = new Promise(function(resolve, reject){
					TaskHelper.getTasksList().then(function(list){
						resolve(this.modList(list));
					}.bind(this), function(){
						reject();
					}.bind(this));
				}.bind(this));
				return promise;
			},
			modList: function(list){
				if (list) {
					list.forEach(function(item){
						var unseen = true;
						if (item["Objekto_GUID"]) {
							item.type = "Objektinė";
						} else {
							item.type = "Kompleksinė";	
						}
						if (item["Pabaigos_data"]) {
							item["Pabaigos_data"] -= 2 * 60 * 60;
						}
						if (item["Pabaigos_data"]) {
							item["Pabaigos_data"] = item["Pabaigos_data"] * 1000;
						}
						if (item["lastCommentTime"]) {
							if (item["last_edited_date"] && (item["lastCommentTime"] > item["last_edited_date"])) {
								item["last_edited_date"] = item["lastCommentTime"];
							}
						}
						if (item["last_time_opened"]) {
							if (item["last_edited_date"] && (item["last_time_opened"] > (item["last_edited_date"] - 5))) { // 5 sek. paklaidą uždedame!
								unseen = false;
							}
							item["last_time_opened"] = CommonHelper.getPrettyDate(item["last_time_opened"] * 1000, true);
						}
						item["created_date"] = item["created_date"] * 1000;
						item["last_edited_date"] = item["last_edited_date"] * 1000;
						item.unseen = unseen;
						item.statusAlias = this.getPrettyValue(item, "Statusas", this.fields);
						item["Uzduoties_tipas"] = this.getPrettyValue(item, "Uzduoties_tipas", this.fields);
						item["Teritorija"] = this.getPrettyValue(item, "Teritorija", this.fields);
						item["Svarba"] = this.getPrettyValue(item, "Svarba", this.fields);
						item["created_date"] = this.getPrettyValue(item, "created_date", this.fields);
						item["last_edited_date"] = this.getPrettyValue(item, "last_edited_date", this.fields);
						item["Pabaigos_data"] = this.getPrettyValue(item, "Pabaigos_data", this.fields);
						item.titleShort = item["Pavadinimas"];
						if (item.titleShort) {
							if (item.titleShort.length > 30) {
								item.titleShort = item.titleShort.substring(0, 30) + "...";
							}
						}
					}.bind(this));
				}
				return list;
			},
			onRowClick: function(item){
				this.dialog = false;
				this.$store.commit("setActiveTask", {
					globalId: item["GlobalID"]
				});
			},
			getPrettyValue: function(item, attr, fields){
				var val = item[attr];
				if (fields) {
					var field = fields[attr];
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
	.clickable {
		cursor: pointer;
	}
	.v-text-field {
		max-width: 500px;
	}
</style>