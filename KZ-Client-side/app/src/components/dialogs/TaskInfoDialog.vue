<template>
	<v-dialog
		persistent
		v-model="dialog"
		:max-width="fullDialog && formData && (formData != 'error') ? null : 900"
		:scrollable="Boolean(formData)"
		:content-class="fullDialog && formData && (formData != 'error') ? 'help-dialog' : null"
	>
		<v-card>
			<v-card-title class="d-flex">
				<span class="flex-grow-1">{{(e && e.title) ? e.title : 'Užduoties informacija'}}</span>
				<span v-if="e && e.properties && e.properties.URL">
					<a :href="(e.properties.Aplinka == 1 ? 'https://app.test.einpix.net/task/' : 'https://app.einpix.com/task/') + e.properties.URL" target="_blank">{{e.properties.URL}}</a>
				</span>
			</v-card-title>
			<v-card-text class="pb-1 pt-1" ref="content">
				<template v-if="formData">
					<template v-if="formData == 'error'">
						<v-alert
							dense
							type="error"
							class="ma-0 d-inline-block"
						>
							Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
						</v-alert>
					</template>
					<template v-else>
						<template v-if="e && e.complete">
							<MyHeading
								value="Užduoties informacija"
								:collapseAndExpandHandler="taskInformationCollapseAndExpandHandler"
								:initiallyCollapsed="false"
								class="black--text font-weight-bold pa-2"
							>
							</MyHeading>
							<v-expand-transition>
								<v-card
									v-show="!taskInformationCollapsed"
									flat
								>
									<div class="mt-2">
										<AttributesTable
											:data="formData"
											:editingActive="!Boolean(e && e.properties && ['4', '7'].indexOf(e.properties['Statusas']) != -1)"
											:markRequired="true"
											ref="form"
											class="popup task-popup task-attributes-table"
										/>
									</div>
								</v-card>
							</v-expand-transition>
							<MyHeading
								value="Užduoties nuotraukos ir brėžiniai"
								:collapseAndExpandHandler="taskAttachmentsCollapseAndExpandHandler"
								:initiallyCollapsed="false"
								class="black--text font-weight-bold pa-2 mt-3"
							>
							</MyHeading>
							<v-expand-transition>
								<v-card
									v-show="!taskAttachmentsCollapsed"
									flat
								>
									<div class="mt-2">
										<FeaturePhotosManager
											:e="{
												feature: e.feature,
												featureType: 'tasks',
												inline: true,
												// noSave: true
											}"
											ref="featurePhotosManager"
											:key="featurePhotosManagerKey"
										/>
									</div>
								</v-card>
							</v-expand-transition>
							<template v-if="e && e.properties && e.properties['Statusas'] != '0'">
								<MyHeading
									value="Rangovo IS esantys užduoties komentarai"
									:collapseAndExpandHandler="taskCommentsCollapseAndExpandHandler"
									:initiallyCollapsed="false"
									class="black--text font-weight-bold pa-2 mt-3"
								>
								</MyHeading>
								<v-expand-transition>
									<v-card
										v-show="!taskCommentsCollapsed"
										flat
									>
										<div class="mt-2">
											<TaskComments :feature="e.feature" />
										</div>
									</v-card>
								</v-expand-transition>
							</template>
							<template v-if="taskRemoteInformationNeeded">
								<MyHeading
									value="Rangovo IS esanti informacija apie užduotį"
									:collapseAndExpandHandler="taskRemoteInformationCollapseAndExpandHandler"
									:initiallyCollapsed="true"
									class="black--text font-weight-bold pa-2 mt-3"
								>
								</MyHeading>
								<v-expand-transition>
									<v-card
										v-show="!taskRemoteInformationCollapsed"
										flat
									>
										<div class="mt-2">
											Gal čia rodyti visą info, kuri yra rangovo IS? Čia turi būti ir `refresh`o mygtukas?.. Funkcionalumas reikalingas vizualiam inspect'inimui?.. Gal dar čia reikia ir `Sync` mygtuko, kuris iš mūsų siųstų notification'ą į rangovo IS?.. Skiltis rodoma tik tada, kai užduotis jau yra deleguota į rangovo IS...
										</div>
									</v-card>
								</v-expand-transition>
							</template>
						</template>
						<template v-else>
							<AttributesTable
								:data="formData"
								:editingActive="true"
								:markRequired="true"
								ref="form"
								class="popup task-popup"
							/>
						</template>
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
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5 d-flex justify-end">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="save"
					outlined
					small
					v-if="formData && formData != 'error'"
					:loading="saveInProgress"
				>
					{{(e && e.complete) ? "Išsaugoti užduoties informaciją" : "Išsaugoti"}}
				</v-btn>
				<template v-if="e && e.complete">
					<v-btn
						color="blue darken-1"
						text
						v-on:click="refresh"
						outlined
						small
					>
						<v-icon left>
							mdi-refresh
						</v-icon>
						Perkrauti
					</v-btn>
				</template>
				<v-btn
					color="blue darken-1"
					text
					v-on:click="exportData"
					outlined
					small
					v-if="e && e.complete"
				>
					<v-icon left>
						mdi-download
					</v-icon>
					Eksportuoti
				</v-btn>
				<v-btn
					color="blue darken-1"
					text
					v-on:click="closeDialog"
					outlined
					small
					:disabled="saveInProgress"
				>
					Uždaryti
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	import AttributesTable from "./../AttributesTable";
	import CommonHelper from "./../helpers/CommonHelper";
	import Feature from "ol/Feature";
	import FeaturePhotosManager from "../FeaturePhotosManager";
	import MyHeading from "../MyHeading";
	import MapHelper from "./../helpers/MapHelper";
	import TaskComments from "../TaskComments";
	import TaskHelper from "./../helpers/TaskHelper";

	export default {
		data: function(){
			var data = {
				saveInProgress: null,
				formData: null,
				dialog: false,
				fullDialog: false,
				e: null,
				taskInformationCollapsed: false,
				taskAttachmentsCollapsed: false,
				taskCommentsCollapsed: false,
				taskRemoteInformationCollapsed: false,
				taskRemoteInformationNeeded: false,
				getFreshDataEveryTime: true,
				modFormData: false,
				featurePhotosManagerKey: 0
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
			AttributesTable,
			FeaturePhotosManager,
			MyHeading,
			TaskComments
		},

		created: function(){
			this.$vBus.$on("show-task-info-dialog", this.showDialog);
			this.$vBus.$on("refresh-task-attachments-list", this.refreshTaskAttachmentsList);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-task-info-dialog", this.showDialog);
			this.$vBus.$off("refresh-task-attachments-list", this.refreshTaskAttachmentsList);
		},

		methods: {
			showDialog: function(e){
				var fullDialog = false;
				if (e && e.complete) {
					fullDialog = true;
				}
				this.getData(e);
				this.dialog = true;
				this.fullDialog = fullDialog;
				this.e = e;
				this.taskInformationCollapsed = false;
			},
			closeDialog: function(){
				this.dialog = false;
				if (this.$refs.content) {
					// Kad kitąkart atidarius dialog'ą nebūtų vizualių netikėtumų...
					this.$refs.content.scrollTop = 0;
					this.$refs.content.scrollLeft = 0;
				}
			},
			getData: function(e){
				this.formData = null;
				this.saveInProgress = false;
				var callback = function(){
					MapHelper.getTasksServiceCapabilities(this.myMap).then(function(){
						if (CommonHelper.layerIds["tasks"]) {
							var layerInfo = this.myMap.getLayerInfo("tasks");
							if (layerInfo && layerInfo.fields) {
								this.setFormData(layerInfo.fields, e);
							} else {
								this.formData = "error";
							}
						} else {
							this.formData = "error";
						}
					}.bind(this), function(){
						this.formData = "error";
					}.bind(this));
				}.bind(this);
				if (this.getFreshDataEveryTime) {
					if (e && e.properties) {
						var globalId = e.properties["GlobalID"];
						if (globalId) {
							TaskHelper.getTaskData(globalId, false, this.$vBus).then(function(taskData){
								if (taskData) {
									e.properties = taskData.attributes;
									callback();
									TaskHelper.logTaskViewAction(globalId).then(function(){
										// TODO: gal reiktų refresh'inti užduočių sąrašą?..
									}, function(){
										// ...
									});
								} else {
									this.formData = "error";
								}
							}.bind(this), function(){
								this.formData = "error";
							}.bind(this));
						} else {
							this.formData = "error";
						}
					} else {
						callback();
					}
				} else {
					callback();
				}
			},
			setFormData: function(fields, e){
				var callback = function(){
					var feature = new Feature();
					if (e && e.properties) {
						feature.setProperties(e.properties);
					}
					this.formData = {
						featureType: "tasks",
						feature: feature,
						origFeature: Boolean(e && e.origFeature)
					};
				}.bind(this);
				if (this.modFormData) {
					TaskHelper.getApproversList().then(function(list){
						if (list) {
							var listMod = [];
							list.forEach(function(item){
								listMod.push({
									name: item.email || (item.name + " (" + item.email + ")"),
									code: item.email
								});
							});
							fields.some(function(field){
								if (field.name == "uzsakovo_email") {
									field.domain = {
										codedValues: listMod
									};
								}
							});
						}
						callback();
					}, function(){
						callback();
					});	
				} else {
					callback();
				}
			},
			save: function(){
				// Iš pradžių tikriname ar yra neišsaugotų nuotraukų... Jei yra, tai pirmiausia jas išsaugome...
				var formData = this.$refs.form.getFormData();
				if (formData && formData != "invalid") {
					if (formData["Pabaigos_data"] && (formData["Pabaigos_data"] > Date.now())) {
						var doSave = function(){
							console.log("TODO: hmmm... gal reikia darkart gauti naujausią užduoties info iš serverio??? Ir saugoti duomenis tik tuo atveju, jei mūsų lokali pradinė info sutampa su ta, kuri yra serveryje?? Tikrinti pagal paskutinę redagavimo datą?.. Jei ji nesutampa, tai pranešti vartotojui ir paklausti ar tikrai vykdyti išsaugojimą??..");
							this.saveInProgress = true;
							var featureToSave = new Feature();
							featureToSave.setProperties(formData);
							if (this.formData.feature) {
								featureToSave.set("GlobalID", this.formData.feature.get("GlobalID")); // FIXME: ne'hardcode'inti...
								// featureToSave.set("OBJECTID", this.formData.feature.get("OBJECTID")); // FIXME: ne'hardcode'inti... To reikia, nes servise negalima daryti pakeitimų naudojant GlobalID...
							}
							if (!featureToSave.get("GlobalID")) { // FIXME: ne'hardcode'inti...
								featureToSave.set("Statusas", 0); // Atseit "Naujas"...
								if (this.e && this.e.origFeatureType && this.e.origFeature) {
									featureToSave.set("Objekto_GUID", this.e.origFeature.get("GlobalID"));
									// Hmm... O lygiagrečiai objekto tipo ar nereikia saugoti?..
								}
								if (this.$store.state.userData) {
									featureToSave.set("uzsakovo_email", this.$store.state.userData.email);
									featureToSave.set("Uzsakovo_vardas", this.$store.state.userData.clientName); // TODO!!!
									featureToSave.set("Uzsakovo_imone", this.$store.state.userData.clientEnterprise); // TODO!!!
									console.log("NUSTATYTI ir kitus užsakovo laukus?", this.$store.state.userData);
								}
								featureToSave.set("Aplinka", process.env.VUE_APP_TASKS_STATE == "prod" ? 2 : 1);
							}
							var dataToSave = [{
								featureType: this.formData.featureType,
								feature: featureToSave
							}];
							var errorCallback = function(){
								this.saveInProgress = false;
								this.$vBus.$emit("show-message", {
									type: "warning",
									message: "Atsiprašome, įvyko nenumatyta klaida... Užduotis nebuvo išsaugota"
								});
							}.bind(this);
							this.myMap.drawHelper.saveFeature(dataToSave).then(function(result){
								var successCallback = function(){
									this.saveInProgress = false;
									this.closeDialog();
									var globalId;
									if (result && result.length == 1) {
										if (result[0].addResults) {
											globalId = result[0].addResults[0].globalId;
											this.$store.commit("setActiveTask", { // FIXME! Gal tai overkill'as? Gal reiktų tyliai gauti naują užduoties info ir koreguoti this.e???
												globalId: globalId
											});
											TaskHelper.notifyAboutTaskChangeToEinpix(featureToSave.getProperties(), "new_task").then(function(){
												// ...
											}, function(reason){
												console.warn("Notification failed... Reason:", reason);
											});
											this.$vBus.$emit("refresh-tasks-search"); // Kad pridėtų įrašą į lentelę...
											TaskHelper.logTaskViewAction(globalId).then(function(){
												// TODO: gal reiktų refresh'inti užduočių sąrašą?..
											}, function(){
												// ...
											});
										} else if (result[0].updateResults) {
											// Čia taip būna kai pakoreguojame užduoties atributų formą?..
											globalId = result[0].updateResults[0].globalId;
											this.$store.commit("setActiveTask", { // FIXME! Gal tai overkill'as? Gal reiktų tyliai gauti naują užduoties info ir koreguoti this.e???
												globalId: globalId
											});
											TaskHelper.notifyAboutTaskChangeToEinpix(featureToSave.getProperties(), "update").then(function(){
												// ...
											}, function(reason){
												console.warn("Notification failed... Reason:", reason);
											});
											this.$vBus.$emit("refresh-tasks-search"); // FIXME! Gal tai overkill'as?.. Tereikia pakeisti konkretaus įrašo duomenis ir tiek?..
											TaskHelper.logTaskViewAction(globalId).then(function(){
												// TODO: gal reiktų refresh'inti užduočių sąrašą?..
											}, function(){
												// ...
											});
										}
									}
								}.bind(this);
								if (this.e.origFeatureType && this.e.origFeature) {
									var globalId;
									if (result && result.length == 1) {
										if (result[0].addResults) {
											globalId = result[0].addResults[0].globalId;
										}
									}
									if (globalId) {
										var dataToSave = [];
										if (this.e.origFeatureType == "verticalStreetSignsSupports") {
											dataToSave = [];
											if (this.e.origFeatureData && this.e.origFeatureData.additionalData) {
												if (this.e.origFeatureData.additionalData.supports && this.e.origFeatureData.additionalData.supports.length && this.e.origFeatureData.additionalData.supports.length == 1) {
													var feature;
													this.e.origFeatureData.additionalData.supports.forEach(function(f){
														feature = f.clone();
														feature.set(CommonHelper.taskFeatureActionFieldName, featureToSave.get(CommonHelper.taskFeatureActionFieldName));
														dataToSave.push({
															feature: feature,
															featureType: "verticalStreetSignsSupports"
														});
													});
													if (this.e.origFeatureData.additionalData.signs) {
														this.e.origFeatureData.additionalData.signs.forEach(function(f){
															feature = f.clone();
															feature.set(CommonHelper.taskFeatureActionFieldName, featureToSave.get(CommonHelper.taskFeatureActionFieldName));
															dataToSave.push({
																feature: feature,
																featureType: "verticalStreetSigns"
															});
														});
													}
												}
											}
										} else {
											var taskRelatedFeature = this.e.origFeature.clone();
											taskRelatedFeature.set(CommonHelper.taskFeatureActionFieldName, featureToSave.get(CommonHelper.taskFeatureActionFieldName)); // Čia nustatoma kas bus vykdoma su objektu: redagavimas ar demontavimas...
											dataToSave = [{
												feature: taskRelatedFeature,
												featureType: this.e.origFeatureType
											}];
										}
										var activeTaskFeature = new Feature();
										activeTaskFeature.set("GlobalID", globalId);
										this.myMap.drawHelper.saveFeature(dataToSave, {
											feature: activeTaskFeature
										}).then(function(){
											// Čia gan slidi vieta? Šioje vietoje nebūtinai gali būti sėkmė... Bet o ką daryti tada? Užduotis susikūrė, o su ja susijęs objektas į ją nebuvo perkeltas... Ką daryti?..
											successCallback();
										}, function(){
											errorCallback();
										});
									} else {
										errorCallback();
									}
								} else {
									successCallback();
								}
							}.bind(this), function(){
								errorCallback();
							}.bind(this));
						}.bind(this);
						if (this.$refs.featurePhotosManager && this.$refs.featurePhotosManager.isAnythingToSave()) {
							var photosTaskCallback = function(status, errors){
								if (status == "start") {
									this.saveInProgress = true;
								} else if (status == "end") {
									this.saveInProgress = false;
									if (!errors.length) {
										doSave();
									}
								}
							}.bind(this);
							this.$refs.featurePhotosManager.save(photosTaskCallback);
						} else {
							doSave();
						}
					} else {
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Turi būti nurodyta teisinga užduoties pabaigos data!"
						});
					}
				} else {
					this.$vBus.$emit("show-message", {
						type: "warning",
						message: "Yra formos pildymo klaidų!"
					});
				}
			},
			taskInformationCollapseAndExpandHandler: function(collapsed){
				this.taskInformationCollapsed = collapsed;
			},
			taskAttachmentsCollapseAndExpandHandler: function(collapsed){
				this.taskAttachmentsCollapsed = collapsed;
			},
			taskCommentsCollapseAndExpandHandler: function(collapsed){
				this.taskCommentsCollapsed = collapsed;
			},
			taskRemoteInformationCollapseAndExpandHandler: function(collapsed){
				this.taskRemoteInformationCollapsed = collapsed;
			},
			refresh: function(){
				if (this.$refs.content) {
					// Kad kitąkart atidarius dialog'ą nebūtų vizualių netikėtumų...
					this.$refs.content.scrollTop = 0;
					this.$refs.content.scrollLeft = 0;
				}
				this.$vBus.$emit("show-task-info-dialog", this.e);
			},
			refreshTaskAttachmentsList: function(){
				this.featurePhotosManagerKey += 1;
			},
			exportData: function(){
				if (this.e && this.e.properties) {
					var globalId = this.e.properties["GlobalID"];
					globalId = CommonHelper.stripGuid(globalId);
					if (globalId) {
						window.open(CommonHelper.webServicesRoot + "tasks/get-report?id=" + globalId, "_blank");
						// Gal naudoti lenteles? Pvz. https://github.com/simonbengtsson/jsPDF-AutoTable
						// Sunku, nes tekstą reikia nurodyti konkrečiomis koordinatėmis?..
						// https://stackoverflow.com/questions/24272058/word-wrap-in-generated-pdf-using-jspdf
					} else {
						// ...
					}
				} else {
					// ...
				}
			}
		}
	}
</script>

<style scoped>
	.task-attributes-table {
		max-width: 1200px;
	}
</style>