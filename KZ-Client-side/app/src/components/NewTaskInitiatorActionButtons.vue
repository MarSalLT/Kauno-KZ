<template>
	<div
		:class="['stop-event action-buttons-wrapper', expanded ? null : 'd-none']"
	>
		<div class="action-buttons white pa-2 d-flex flex-column overflow-y">
			<template v-if="completeInfoInOnePlace">
				<v-btn
					class="justify-start"
					depressed
					v-on:click="showCompleteInfo"
				>
					<v-icon
						title="Peržiūrėti/Redaguoti informaciją"
						color="primary"
					>
						mdi-view-list-outline
					</v-icon>
					<span class="caption ml-1">{{((taskStatusCode == '4' || taskStatusCode == '2') && (apprState == 1)) ? "Peržiūrėti visą informaciją" : "Peržiūrėti/Redaguoti visą informaciją"}}</span>
				</v-btn>
			</template>
			<template v-else>
				<v-btn
					class="justify-start"
					depressed
					v-on:click="editInfo"
				>
					<v-icon
						title="Redaguoti informaciją"
						color="primary"
					>
						mdi-pencil
					</v-icon>
					<span class="caption ml-1">Redaguoti informaciją</span>
				</v-btn>
				<v-btn
					class="mt-2 justify-start"
					depressed
					v-on:click="manageAttachments"
				>
					<v-icon
						title="Valdyti nuotraukas, brėžinius"
						color="primary"
					>
						mdi-image
					</v-icon>
					<span class="caption ml-1">Valdyti nuotraukas, brėžinius</span>
				</v-btn>
			</template>
			<v-btn
				class="mt-2 justify-start"
				depressed
				:color="geomDrawingButtonActive ? 'orange' : null"
				v-on:click="toggleTaskGeomDrawing"
				v-if="!((taskStatusCode == '4' || taskStatusCode == '2') && (apprState == 1))"
			>
				<v-icon
					title="Nurodyti aprėptį"
					color="primary"
				>
					mdi-rectangle-outline
				</v-icon>
				<span class="caption ml-1">Nurodyti aprėptį</span>
			</v-btn>
			<v-btn
				class="mt-2 justify-start"
				depressed
				v-on:click="cancelTask"
				v-if="taskStatusCode != '0' && taskStatusCode != '4' && taskStatusCode != '7' && taskStatusCode != '2'"
			>
				<v-icon
					title="Atšaukti užduotį"
					color="error lighten-1"
				>
					mdi-stop-circle
				</v-icon>
				<span class="caption ml-1">Atšaukti užduotį</span>
			</v-btn>
			<template v-if="(taskStatusCode == '4') && (apprState != 1)">
				<v-btn
					class="mt-2 justify-start"
					depressed
					v-on:click="approveTask"
				>
					<v-icon
						title="Patvirtinti užduotį"
						color="primary"
					>
						mdi-check-circle
					</v-icon>
					<span class="caption ml-1">Patvirtinti užduotį</span>
				</v-btn>
				<v-btn
					class="mt-2 justify-start"
					depressed
					v-on:click="rejectTask"
				>
					<v-icon
						title="Atmesti užduotį"
						color="error"
					>
						mdi-minus-circle
					</v-icon>
					<span class="caption ml-1">Atmesti užduotį</span>
				</v-btn>
			</template>
			<template v-else>
				<template v-if="taskStatusCode == '0'">
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-on:click="delegateTask"
					>
						<v-icon
							title="Deleguoti užduotį"
							color="success"
						>
							mdi-send
						</v-icon>
						<span class="caption ml-1">Deleguoti užduotį</span>
					</v-btn>
				</template>
				<template v-if="!taskStatusCode || (taskStatusCode == '0')">
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-on:click="destroyTask"
						:loading="destroyInProgress"
					>
						<v-icon
							title="Šalinti užduotį"
							color="error"
						>
							mdi-delete
						</v-icon>
						<span class="caption ml-1">Šalinti užduotį</span>
					</v-btn>
				</template>
			</template>
		</div>
	</div>
</template>

<script>
	import Draw from "ol/interaction/Draw";
	import TaskHelper from "./helpers/TaskHelper";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import {Fill, Stroke, Style} from "ol/style";

	export default {
		data: function(){
			var data = {
				destroyInProgress: false,
				geomDrawingButtonActive: false,
				completeInfoInOnePlace: true
			};
			return data;
		},

		props: {
			feature: Object,
			taskStatusCode: String,
			expanded: Boolean
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			apprState: {
				get: function(){
					var apprState = false;
					if (this.feature) {
						apprState = this.feature.get("Patvirtinimas");
					}
					return apprState;
				}
			}
		},

		created: function(){
			this.$vBus.$on("deactivate-interactions", this.deactivateInteraction);
		},

		beforeDestroy: function(){
			this.onGeomDrawingButtonDeactivate();
			this.$vBus.$off("deactivate-interactions", this.deactivateInteraction);
		},

		methods: {
			showCompleteInfo: function(){
				var properties = this.feature.getProperties();
				var title;
				if (properties) {
					title = properties["Pavadinimas"];
					if (title) {
						title = "Užduotis: " + title;
					}
				}
				this.$vBus.$emit("show-task-info-dialog", {
					properties: properties,
					feature: this.feature,
					title: title,
					complete: true
				});
			},
			editInfo: function(){
				this.$vBus.$emit("show-task-info-dialog", {
					properties: this.feature.getProperties()
				});
			},
			manageAttachments: function(){
				this.$vBus.$emit("show-feature-photos-manager-dialog", {
					feature: this.feature,
					featureType: "tasks"
				});
			},
			destroyTask: function(){
				// TODO... Gal neleisti pašalinti užduoties, jei su ja yra susijusių objektų?.. O gal šalinant užduotį pašalinti ir susijusius objektus...
				this.$vBus.$emit("confirm", {
					title: "Ar tikrai šalinti užduotį?",
					message: "Ar tikrai šalinti užduotį? Pašalinus užduotį nebebus galima jos atstatyti...",
					positiveActionTitle: "Šalinti užduotį",
					negativeActionTitle: "Atšaukti",
					positive: function(){
						this.destroyInProgress = true;
						this.$store.state.myMap.drawHelper.destroyFeature(this.feature, "tasks").then(function(){
							this.destroyInProgress = false;
							this.$store.commit("setActiveTask", null);
							this.$vBus.$emit("refresh-tasks-search"); // FIXME! Gal tai overkill'as?.. Tereikia pašalinti konkretų įrašą ir tiek?..
						}.bind(this), function(){
							this.destroyInProgress = false;
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Atsiprašome, įvyko nenumatyta klaida... Užduotis nebuvo pašalinta"
							});
						}.bind(this));
					}.bind(this)
				});
			},
			delegateTask: function(){
				this.$vBus.$emit("delegate-task", {
					feature: this.feature
				});
			},
			toggleTaskGeomDrawing: function(){
				if (this.myMap) {
					if (this.geomDrawingButtonActive) {
						this.geomDrawingButtonActive = false;
					} else {
						this.geomDrawingButtonActive = true;
					}
				}
			},
			deactivateInteraction: function(type){
				if (type != "task-geom") {
					this.geomDrawingButtonActive = false;
				}
			},
			onGeomDrawingButtonActivate: function(){
				this.$vBus.$emit("deactivate-interactions", "task-geom");
				if (!this.vectorLayer) {
					this.vectorLayer = new VectorLayer({
						source: new VectorSource(),
						zIndex: 1001,
						style: new Style({
							fill: new Fill({
								color: "rgba(255, 255, 255, 0.2)"
							}),
							stroke: new Stroke({
								color: "blue",
								width: 4,
								lineDash: [10, 10]
							})
						})
					});
					this.myMap.map.addLayer(this.vectorLayer);
				}
				if (!this.drawInteraction) {
					this.drawInteraction = new Draw({
						type: "Polygon",
						source: this.vectorLayer.getSource(),
						stopClick: true
					});
					this.drawInteraction.on("drawend", function(e){
						this.$vBus.$emit("confirm", {
							title: "Ar tvirtinate užduoties geometriją?",
							message: "Ar tvirtinate užduoties geometriją?",
							positiveActionTitle: "Tvirtinti užduoties geometriją",
							negativeActionTitle: "Atšaukti",
							delayedPositive: true,
							positive: function(dialog){
								dialog.loading = true;
								var feature = this.feature.clone();
								feature.setGeometry(e.feature.getGeometry());
								var dataToSave = [{
									featureType: "tasks",
									feature: feature
								}];
								this.myMap.drawHelper.saveFeature(dataToSave).then(function(result){
									if (result && result.length == 1) {
										if (result[0].updateResults) {
											dialog.dialog = false;
											this.deactivateInteraction();
											this.$store.commit("setActiveTask", { // FIXME! Tai labai didelis overkill'as... Reiktų tiesiog tyliai nustatyti geometriją ir tiek...
												globalId: result[0].updateResults[0].globalId
											});
											return;
										}
									}
									dialog.loading = false;
									this.$vBus.$emit("show-message", {
										type: "warning",
										message: "Atsiprašome, įvyko nenumatyta klaida... Objektas nebuvo išsaugotas"
									});
								}.bind(this), function(){
									dialog.loading = false;
									this.$vBus.$emit("show-message", {
										type: "warning",
										message: "Atsiprašome, įvyko nenumatyta klaida... Objektas nebuvo išsaugotas"
									});
								}.bind(this));
							}.bind(this),
							negative: function(){
								this.deactivateInteraction();
							}.bind(this)
						});
					}.bind(this));
					this.myMap.addInteraction(this.drawInteraction);
				}
			},
			onGeomDrawingButtonDeactivate: function(){
				if (this.vectorLayer) {
					this.myMap.map.removeLayer(this.vectorLayer);
					this.vectorLayer = null;
				}
				if (this.drawInteraction) {
					this.myMap.removeInteraction(this.drawInteraction);
					this.drawInteraction = null;
				}
			},
			approveTask: function(){
				this.$vBus.$emit("confirm", {
					title: "Ar tikrai patvirtinti užduotį?",
					positiveActionTitle: "Patvirtinti užduotį",
					negativeActionTitle: "Atšaukti",
					delayedPositive: true,
					positive: function(dialog){
						dialog.loading = true;
						TaskHelper.approveTask(this.feature).then(function(){
							this.onTaskApproveOrRejectSuccess(dialog, "approve");
						}.bind(this), function(){
							dialog.loading = false;
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Atsiprašome, įvyko nenumatyta klaida... Užduotis nebuvo patvirtinta"
							});
						}.bind(this));
					}.bind(this)
				});
			},
			rejectTask: function(){
				this.$vBus.$emit("confirm", {
					title: "Ar tikrai atmesti užduotį?",
					positiveActionTitle: "Atmesti užduotį",
					negativeActionTitle: "Atšaukti",
					delayedPositive: true,
					textarea: {
						placeholder: "Atmetimo priežastis"
					},
					positive: function(dialog){
						dialog.loading = true;
						TaskHelper.rejectTask(this.feature, dialog.message).then(function(){
							this.onTaskApproveOrRejectSuccess(dialog, "reject");
						}.bind(this), function(){
							dialog.loading = false;	
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Atsiprašome, įvyko nenumatyta klaida... Užduotis nebuvo atmesta"
							});
						}.bind(this));
					}.bind(this)
				});
			},
			onTaskApproveOrRejectSuccess: function(dialog, action){
				var actionName,
					taskStarted = Date.now();
				if (action == "approve") {
					actionName = "patvirtinti";
				} else if (action == "reject") {
					actionName = "atmesti";
				}
				var task = window.setInterval(function(){
					var timeElapsed = Date.now() - taskStarted;
					if (timeElapsed > 30000) {
						window.clearInterval(task);
						dialog.loading = false;
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Atsiprašome, įvyko nenumatyta klaida... Užduoties " + actionName + " nepavyko. Pabandykite dar kartą...",
							timeout: 10000
						});
					} else {
						this.getTaskData(this.feature).then(function(taskData){
							console.log(taskData);
							var taskGlobalId = taskData["GlobalID"];
							if (action == "approve") {
								if ((taskData["Statusas"] == "2") && (taskData["Patvirtinimas"] == 1)) {
									// Clear interval immediately to prevent multiple triggers
									window.clearInterval(task);
									setTimeout(function(){
										dialog.loading = false;
										dialog.dialog = false;
										this.$vBus.$emit("show-message", {
											type: "success",
											message: "Užduotis patvirtinta sėkmingai!",
											timeout: 3000
										});
										TaskHelper.logTaskViewAction(taskGlobalId).then(function(){
											// TODO: gal reiktų refresh'inti užduočių sąrašą?..
										}, function(){
											// ...
										});
										try {
											this.myMap.map.getLayers().forEach(function(layer){
												if (layer.service && (layer.service.id == "street-signs-vertical" || layer.service.id == "street-signs")) {
													layer.service.timestamp = Date.now();
													if (layer.getLayers && typeof layer.getLayers === 'function') {
														layer.getLayers().forEach(function(l){
															if (l && l.getSource && typeof l.getSource === 'function') {
																try {
																	l.getSource().refresh();
																} catch (e) {
																	console.warn("Could not refresh layer source:", e);
																}
															}
														});
													}
												}
											});
										} catch (e) {
											console.warn("Error refreshing map layers:", e);
										}
									}.bind(this), 15000);
								}
							} else if (action == "reject") {
								if (taskData["Statusas"] == "6") {
									window.clearInterval(task);
									dialog.loading = false;
									dialog.dialog = false;
									this.$vBus.$emit("show-message", {
										type: "success",
										message: "Užduotis atmesta sėkmingai!",
										timeout: 3000
									});
									console.log("REEE");
									TaskHelper.logTaskViewAction(taskGlobalId).then(function(){
										// TODO: gal reiktų refresh'inti užduočių sąrašą?..
									}, function(){
										// ...
									});

								}
							}
						}.bind(this), function(){
							// ...
						});
					}
				}.bind(this), 3000);
			},
			getTaskData: function(feature){
				var promise = new Promise(function(resolve, reject){
					if (feature) {
						var taskGlobalId = feature.get("GlobalID");
						if (taskGlobalId) {
							TaskHelper.getTaskData(taskGlobalId, false, this.$vBus).then(function(taskData){
								if (taskData && taskData.attributes) {
									resolve(taskData.attributes);
								} else {
									reject();
								}
							}.bind(this), function(){
								reject();
							}.bind(this));
						} else {
							reject();
						}
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			},
			cancelTask: function(){
				this.$vBus.$emit("confirm", {
					title: "Ar tikrai atšaukti užduotį?",
					message: "Ar tikrai norite atšaukti užduotį? Atšaukus užduotį ji bus negrįžtamai nutraukta.",
					positiveActionTitle: "Atšaukti užduotį",
					negativeActionTitle: "Grįžti",
					delayedPositive: true,
					positive: function(dialog){
						dialog.loading = true;
						var feature = this.feature.clone();
						feature.set("Statusas", "7");
						var dataToSave = [{
							featureType: "tasks",
							feature: feature
						}];
						this.myMap.drawHelper.saveFeature(dataToSave).then(function(result){
							if (result && result.length == 1) {
								if (result[0].updateResults) {
									var globalId = result[0].updateResults[0].globalId;
									this.$store.commit("setActiveTask", { // FIXME! Gal tai overkill'as? Gal reiktų tyliai gauti naują užduoties info ir koreguoti this.e???
										globalId: globalId
									});
									TaskHelper.notifyAboutTaskChangeToEinpix(feature.getProperties(), "cancel_task").then(function(){
										// ...
									}, function(reason){
										console.warn("Notification failed... Reason:", reason);
									});
									dialog.dialog = false;
									this.$vBus.$emit("show-message", {
										type: "success",
										message: "Užduotis atšaukta sėkmingai! (BET JOS ATNAUJINIMAS PILNAI... NEĮGYVENDINTAS...)",
										timeout: 5000
									});
									return;
								}
							}
							dialog.loading = false;
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Atsiprašome, įvyko nenumatyta klaida... Užduotis nebuvo atšaukta"
							});
						}.bind(this), function(){
							dialog.loading = false;
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Atsiprašome, įvyko nenumatyta klaida... Užduotis nebuvo atšaukta"
							});
						}.bind(this));
					}.bind(this)
				});
			}
		},

		watch: {
			geomDrawingButtonActive: {
				immediate: true,
				handler: function(geomDrawingButtonActive){
					if (geomDrawingButtonActive) {
						this.onGeomDrawingButtonActivate();
					} else {
						this.onGeomDrawingButtonDeactivate();
					}
				}
			},
			"$store.state.activeTask": {
				immediate: true,
				handler: function(){
					this.geomDrawingButtonActive = false;
				}
			}
		}
	}
</script>

<style scoped>
	.action-buttons-wrapper {
		margin-top: 37px;
	}
	.action-buttons {
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3) !important;
		border-top-right-radius: 10px;
		border-bottom-right-radius: 10px;
		max-height: 100%;
	}
</style>