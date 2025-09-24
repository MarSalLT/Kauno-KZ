<template>
	<div
		v-if="canHandleTasks"
		class="d-flex stop-event popup-wrapper"
	>
		<v-btn
			fab
			small
			title="Inicijuoti naują užduotį"
			:elevation="2"
			v-if="!activeTaskMainData"
			color="success"
			v-on:click="showTaskInfoDialog()"
		>
			<v-icon color="white">mdi-text-box-plus</v-icon>
		</v-btn>
		<div
			class="popup d-flex flex-column"
			v-if="activeTaskMainData"
		>
			<div class="header d-flex align-center pa-1 success white--text">
				<span class="ml-2"><strong class="mr-1">UŽDUOTIS:</strong> {{activeTaskMainData == "error" ? "" : (activeTaskMainData.title || "Be pavadinimo")}}</span>
				<div class="d-flex flex-grow-1 justify-end">
					<v-btn
						icon
						v-on:click="expanded = !expanded"
						:title="expanded ? 'Suskleisti' : 'Išskleisti'"
						class="ml-1"
						small
						color="white"
					>
						<template v-if="expanded">
							<v-icon>mdi-menu-down</v-icon>
						</template>
						<template v-else>
							<v-icon>mdi-menu-up</v-icon>
						</template>
					</v-btn>
					<v-btn
						icon
						v-on:click="refresh"
						title="Perkrauti"
						class="ml-1"
						small
						color="white"
					>
						<v-icon>mdi-refresh</v-icon>
					</v-btn>
					<v-btn
						icon
						v-on:click="zoomToExtent(true)"
						title="Artinti prie aprėpties"
						class="ml-1"
						small
						color="white"
					>
						<v-icon>mdi-magnify-plus</v-icon>
					</v-btn>
					<v-btn
						icon
						v-on:click="unsetActiveTask"
						title="Uždaryti"
						class="ml-1"
						small
						color="white"
					>
						<v-icon>
							mdi-close
						</v-icon>
					</v-btn>
				</div>
			</div>
			<v-card
				v-show="expanded"
				flat
				class="overflow-y"
			>
				<div class="content pa-3 d-flex flex-column">
					<template v-if="activeTaskMainData == 'error'">
						<v-alert
							dense
							type="error"
							class="ma-0 d-inline-block"
						>
							Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
						</v-alert>
					</template>
					<template v-else>
						<div v-if="activeTaskMainData.description" class="body-2 mb-1"><span class="font-weight-medium">Aprašymas:</span> {{activeTaskMainData.description}}</div>
						<div v-if="activeTask.status" class="body-2"><span class="font-weight-medium">Būsena:</span> {{activeTask.status || "—"}}</div>
						<div v-if="activeTaskMainData['Objekto_GUID']" class="body-2 mt-1"><span class="font-weight-medium">Susijusio KŽ objekto GUID:</span> <span class="success white--text">{{activeTaskMainData['Objekto_GUID']}}</span></div>
						<template v-if="activeTask.initialData == 'loading'">
							<v-progress-circular
								indeterminate
								color="primary"
								:size="25"
								width="2"
								class="my-2"
							></v-progress-circular>
						</template>
						<template v-if="activeTaskFeatures && activeTaskFeatures.length">
							<v-data-table
								:headers="listHeaders"
								:items="activeTaskFeatures"
								:options="listOptions"
								class="mt-2 mb-n1 rounded-0 my-table search-content-table"
							>
								<template v-slot:item="{item}">
									<tr
										:class="['clickable']"
										v-on:click="onRowClick(item)"
									>
										<template v-for="(headerItem, j) in listHeaders">
											<td :class="['px-2 py-1 caption', ['action', 'remove', 'manage-photos'].indexOf(headerItem.value) != -1 ? 'text-center' : null]" :key="j">
												<template v-if="headerItem.value == 'action'">
													<v-avatar
														color="primary"
														size="20"
														:title="getActionTitle(item.action)"
														:class="item.action"
													></v-avatar>
												</template>
												<template v-else-if="headerItem.value == 'featureType'">
													{{getFeatureTypePrettyName(item.featureType)}}
												</template>
												<template v-else-if="headerItem.value == 'symbol'">
													<ESRISymbol
														:descr="item.symbolDescr"
														:dark="item.dark"
														small
													/>
												</template>
												<template v-else-if="headerItem.value == 'remove'">
													<MyButton
														:descr="{
															color: 'error',
															title: 'Šalinti objektą',
															icon: 'mdi-delete',
															onClick: deleteFeature,
															item: item
														}"
													/>
												</template>
												<template v-else-if="headerItem.value == 'manage-photos'">
													<TaskFeatureAttachmentsButton
														:data="item"
														:activeTask="activeTask"
														v-if="item.action != 'delete'"
													/>
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
				</div>
			</v-card>
		</div>
		<NewTaskInitiatorActionButtons
			v-if="activeTask && activeTask.feature && (activeTask.feature != 'error')"
			:feature="activeTask.feature"
			:taskStatusCode="taskStatusCode"
			:expanded="expanded"
		/>
		<TaskDelegationDialog />
		<TaskInfoDialog />
		<TaskAttachmentsTransferDialog />
	</div>
</template>

<script>
	import CommonHelper from "../components/helpers/CommonHelper";
	import EsriJSON from "ol/format/EsriJSON";
	import ESRISymbol from "./ESRISymbol";
	import MapHelper from "../components/helpers/MapHelper";
	import MyButton from "./MyButton";
	import NewTaskInitiatorActionButtons from "./NewTaskInitiatorActionButtons";
	import TaskAttachmentsTransferDialog from "./dialogs/TaskAttachmentsTransferDialog";
	import TaskDelegationDialog from "./dialogs/TaskDelegationDialog";
	import TaskFeatureAttachmentsButton from "./TaskFeatureAttachmentsButton";
	import TaskHelper from "./helpers/TaskHelper";
	import TaskInfoDialog from "./dialogs/TaskInfoDialog";
	import VectorLayer from "ol/layer/VectorImage";
	import VectorSource from "ol/source/Vector";
	import {Fill, Stroke, Style} from "ol/style";

	export default {
		data: function(){
			var canHandleTasks = false;
			if (this.$store.state.userData && this.$store.state.userData.permissions) {
				if ((this.$store.state.userData.permissions.indexOf("manage-tasks") != -1) || (this.$store.state.userData.permissions.indexOf("manage-tasks-test") != -1)) { // TODO: tik "approve"...
					canHandleTasks = true;
				}
			}
			var data = {
				type: "task-initiator",
				canHandleTasks: canHandleTasks,
				isLoading: null,
				listHeaders: this.getListHeaders(),
				listOptions: {
					sortBy: ["featureType"],
					sortDesc: [false],
					mustSort: true,
					itemsPerPage: 10
				},
				expanded: false
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			activeTask: {
				get: function(){
					return this.$store.state.activeTask;
				}
			},
			activeTaskMainData: {
				get: function(){
					var activeTask = null;
					if (this.activeTask) {
						var feature = this.activeTask.feature;
						if (feature) {
							if (feature == "error") {
								activeTask = "error";
							} else {
								activeTask = feature.getProperties() || {};
								// BIG FIXME! Čia neturi būti hardcode'intų laukų... Kažkaip kitaip sužaisti?..
								activeTask.title = feature.get("Pavadinimas");
								activeTask.description = feature.get("Aprasymas");
								activeTask.globalId = feature.get("GlobalID");
							}
						}
					}
					return activeTask;
				}
			},
			activeTaskFeatures: {
				get: function(){
					var activeTaskFeatures = this.$store.state.activeTaskFeatures;
					if (activeTaskFeatures) {
						var activeTaskFeaturesMod = [],
							items;
						for (var layerKey in activeTaskFeatures) {
							items = activeTaskFeatures[layerKey];
							if (items) {
								items.forEach(function(item){
									item.symbolDescr = this.getSymbolDescr(item);
									if (item.featureType == "horizontalPoints") {
										item.dark = true;
									}
									activeTaskFeaturesMod.push(item);
								}.bind(this));
							}
						}
						activeTaskFeatures = activeTaskFeaturesMod;
					}
					return activeTaskFeatures;
				}
			},
			taskStatusCode: {
				get: function(){
					var taskStatusCode;
					if (this.activeTask && this.activeTask.feature) {
						if (this.activeTask.feature != "error") {
							var properties = this.activeTask.feature.getProperties();
							if (properties) {
								taskStatusCode = properties["Statusas"]; // FIXME!!! Ne'hardcode'inti...
							}
						}
					}
					return taskStatusCode;
				}
			}
		},

		components: {
			ESRISymbol,
			MyButton,
			NewTaskInitiatorActionButtons,
			TaskAttachmentsTransferDialog,
			TaskDelegationDialog,
			TaskFeatureAttachmentsButton,
			TaskInfoDialog
		},

		created: function(){
			this.$vBus.$on("update-tasks-list-item", this.updateTasksListItem);
		},

		beforeDestroy: function(){
			this.$vBus.$off("update-tasks-list-item", this.updateTasksListItem);
		},

		methods: {
			showTaskInfoDialog: function(taskInfo){
				if (taskInfo) {
					// ...
				} else {
					this.$vBus.$emit("show-task-info-dialog", {
						title: "Naujos užduoties informacija"
					});
				}
			},
			unsetActiveTask: function(){
				this.$store.commit("setActiveTask", null);
			},
			findActiveTaskIfNeeded: function(){
				this.isLoading = false;
				if (this.activeTask && !this.activeTask.feature) {
					if (this.activeTask.globalId && this.myMap && CommonHelper.layerIds["tasks"]) {
						MapHelper.getTasksServiceCapabilities(this.myMap).then(function(e){
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
							if (!this.activeTaskGettingCounter) {
								this.activeTaskGettingCounter = 0;
							}
							this.activeTaskGettingCounter += 1;
							var activeTaskGettingCounter = this.activeTaskGettingCounter;
							this.isLoading = true;
							TaskHelper.getTaskData(this.activeTask.globalId, false, this.$vBus).then(function(taskData){
								if (activeTaskGettingCounter == this.activeTaskGettingCounter) {
									var feature;
									if (taskData) {
										var esriJsonFormat = new EsriJSON();
										feature = esriJsonFormat.readFeature(taskData);
									}
									this.setActiveTaskFeature(feature, fields);
								}
							}.bind(this), function(){
								if (activeTaskGettingCounter == this.activeTaskGettingCounter) {
									this.setActiveTaskFeature();
								}
							}.bind(this));
						}.bind(this), function(){
							this.setActiveTaskFeature();
						}.bind(this));
					}
				}
			},
			setActiveTaskFeature: function(feature, fields){
				var activeTask = this.activeTask;
				if (activeTask) {
					activeTask = Object.assign({}, activeTask);
					activeTask.feature = feature || "error";
					if (feature && fields) {
						activeTask.status = TaskHelper.getPrettyValue({attributes: feature.getProperties()}, "Statusas", fields);
					}
					this.$store.commit("setActiveTask", activeTask);
					this.expanded = true;
				}
			},
			drawActiveTaskExtentFeature: function(){
				if (this.myMap) {
					if (!this.activeTaskLayer) {
						this.activeTaskLayer = new VectorLayer({
							source: new VectorSource(),
							zIndex: 1001,
							style: new Style({
								fill: new Fill({
									color: "rgba(255, 255, 255, 0)"
								}),
								stroke: new Stroke({
									color: "blue",
									width: 4
								})
							})
						});
						this.myMap.map.addLayer(this.activeTaskLayer);
					} else {
						this.activeTaskLayer.getSource().clear(true);
					}
					var activeTask = this.activeTask;
					if (activeTask && activeTask.feature) {
						this.activeTaskLayer.getSource().addFeature(activeTask.feature);
						if (activeTask.initialData != "loaded") {
							this.zoomToExtent();
						}
					}
				}
			},
			zoomToExtent: function(manual){
				// Kol kas zoom'iname prie bendro extent'o visų su užduotimi susijusių objektų...
				if (this.myMap) {
					var features = [];
					if (this.activeTaskFeatures) {
						this.activeTaskFeatures.forEach(function(item){
							if (item.feature) {
								features.push(item.feature);
							}
						});
					}
					if (this.activeTask && this.activeTask.feature) {
						features.push(this.activeTask.feature);
					}
					var emptyExtentCallback;
					if (manual) {
						emptyExtentCallback = function(){
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Aprėpties nėra!"
							});
						}.bind(this);
					}
					this.myMap.zoomToFeatures(features, null, emptyExtentCallback);
				}
			},
			getActionTitle: function(action){
				var titles = {
					add: "Naujo sukūrimas",
					update: "Egzistuojančio redagavimas",
					delete: "Pašalinimas"
				};
				return titles[action];
			},
			getFeatureTypePrettyName: function(featureType){
				return CommonHelper.getPrettyName(null, featureType);
			},
			onRowClick: function(item){
				var layerIdMeta = CommonHelper.layerIds[item.featureType];
				if (layerIdMeta) {
					var layerId = layerIdMeta[1],
						type = "task-related";
					if (layerIdMeta[0] == "street-signs-vertical") {
						type += "-v";
					}
					CommonHelper.routeTo({ // Bet nelabai tai veikia... Net nerealizuota...
						featureData: {
							type: type,
							globalId: item.feature.get("GlobalID"), // TODO: ne'hardcode'inti lauko...
							layerId: layerId
						},
						router: this.$router,
						vBus: this.$vBus
					});
				}
			},
			getSymbolDescr: function(item){
				var symbolDescr,
					layerInfo = this.$store.state.myMap.getLayerInfo(item.featureType);
				if (layerInfo) {
					var renderer = layerInfo.drawingInfo.renderer,
						symbols = {};
					if (renderer) {
						if (renderer.type == "uniqueValue") {
							renderer.uniqueValueInfos.forEach(function(uniqueValueInfo){
								symbols[uniqueValueInfo.value] = uniqueValueInfo.symbol;
							});
						}
					}
					symbolDescr = symbols[item.feature.get(layerInfo.typeIdField)];
					if (item.feature.get(CommonHelper.customSymbolIdFieldName)) {
						console.log("TODO: unique symbol..."); // TODO: kaip `VerticalStreetSignSelector.vue`...
					}
				}
				return symbolDescr;
			},
			deleteFeature: function(item, btn){
				this.$vBus.$emit("confirm", {
					title: "Ar tikrai šalinti objektą?",
					message: "Ar tikrai šalinti objektą?",
					positiveActionTitle: "Šalinti objektą",
					negativeActionTitle: "Atšaukti",
					positive: function(){
						btn.loading = true;
						this.$store.state.myMap.drawHelper.destroyFeature(item.feature, item.featureType, true).then(function(){
							btn.loading = false;
							TaskHelper.onFeatureDestroy(this.activeTask, this.$store, item.feature, this.$router, this.$vBus);
						}.bind(this), function(){
							btn.loading = false;
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Atsiprašome, įvyko nenumatyta klaida... Objektas nebuvo pašalintas"
							});
						}.bind(this));
					}.bind(this)
				});
			},
			getListHeaders: function(){
				var listHeaders = [{
					value: "symbol",
					text: "Objektas",
					sortable: false
				},{
					value: "featureType",
					text: "Tipas"
				},{
					value: "action",
					text: "Veiksmas"
				}];
				if (this.$store.state.userData && (this.$store.state.userData.role != "tasks-tester")) {
					if (this.taskStatusCode != "4") {
						listHeaders.push({
							value: "remove",
							text: "Šalinti",
							sortable: false
						});
					} else {
						if (!this.$store.state.hideNewUnderConstructionFunctionality) {
							listHeaders.push({
								value: "manage-photos",
								text: "Valdyti nuotraukas",
								sortable: false
							});
						}
					}
				}
				return listHeaders;
			},
			refresh: function(){
				if (this.$store.state.activeTask && this.$store.state.activeTask.globalId) {
					this.$store.commit("setActiveTask", {
						globalId: this.$store.state.activeTask.globalId
					});
				}
			},
			updateTasksListItem: function(result){
				var itemGlobalId;
				if (result && result.feature && result.feature.attributes) {
					itemGlobalId = result.feature.attributes["GlobalID"];
				}
				if (itemGlobalId) {
					if (this.$store.state.activeTask && result.feature) {
						if (this.$store.state.activeTask.feature) {
							if (this.$store.state.activeTask.feature.get("GlobalID") == result.feature.attributes["GlobalID"]) {
								this.$store.state.activeTask.feature.setProperties(result.feature.attributes);
							}
						}
					}
				}
			}
		},

		watch: {
			activeTask: {
				immediate: true,
				handler: function(activeTask, prevActiveTask){
					this.drawActiveTaskExtentFeature();
					this.findActiveTaskIfNeeded();
					this.$store.commit("setMapNewItemState", (activeTask && activeTask.feature) ? this.type : null);
					if (!activeTask && prevActiveTask) {
						// Uždarome popup'ą... Nes gi jis galėjo būti susijęs su užduotimi... Jei paliks kaboti, o bus susijęs su užduotimi, bus nesąmonė...
						CommonHelper.routeTo({
							router: this.$router,
							vBus: this.$vBus
						});
					}
				}
			},
			"activeTask.initialData": {
				immediate: true,
				handler: function(activeTaskInitialData){
					if (activeTaskInitialData == "loaded") {
						this.zoomToExtent();
						if (this.activeTask && this.activeTask.feature && this.activeTask.feature.get("Objekto_GUID") && this.activeTaskFeatures) {
							this.activeTaskFeatures.some(function(activeTaskFeature){
								if (activeTaskFeature.feature && (activeTaskFeature.feature.get(CommonHelper.taskFeatureOriginalGlobalIdFieldName) == this.activeTask.feature.get("Objekto_GUID"))) {
									this.onRowClick(activeTaskFeature);
									return true;
								}
							}.bind(this));
						} else {
							// Aktualu uždaryti popup'ą?.. Reikalinga, jei kartais buvome atsidarę kokio objekto popup'ą... Tada aktyvavome kokią užduotį... Ir pasirodo, kad popup'o objektas dalyvauja užduotyje... Susimaišo viskas... Blogai!
							// TODO: galima būtų uždaryti ne visais atvejais, o tik jei užduotis turi bent vieną susijusį objektą?...
							CommonHelper.routeTo({
								router: this.$router,
								vBus: this.$vBus
							});
						}
					}
				}
			},
			myMap: {
				immediate: true,
				handler: function(myMap){
					if (myMap) {
						this.findActiveTaskIfNeeded();
					}
				}
			},
			taskStatusCode: {
				immediate: true,
				handler: function(){
					this.listHeaders = this.getListHeaders();
				}
			}
		}
	}
</script>

<style scoped>
	.popup-wrapper {
		max-height: 100%;
	}
	.popup {
		width: 500px;
		background-color: white;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3) !important;
		overflow: hidden;
		overflow-y: auto;
		z-index: 1;
	}
	.header {
		border-bottom: 1px solid #cccccc;
	}
	.content {
		overflow: hidden;
		overflow-y: auto;
		width: 100%;
	}
	.clickable {
		cursor: pointer;
	}
	.v-avatar {
		border: 1px solid #ababab !important;
	}
	.add.v-avatar {
		background-color: green !important;
	}
	.update.v-avatar {
		background-color: yellow !important;
	}
	.delete.v-avatar {
		background-color: red !important;
	}
</style>