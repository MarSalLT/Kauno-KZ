<template>
	<div
		class="stop-event action-buttons-wrapper"
		v-if="editingAllowed"
	>
		<div class="action-buttons white pa-2 d-flex flex-column overflow-y">
			<template v-if="mode == 'vertical-street-sign-addition'">
				<v-btn
					class="justify-start"
					depressed
					v-on:click="setMode()"
				>
					<v-icon
						title="Atšaukti KŽ pridėjimą"
						color="primary"
					>
						mdi-undo-variant
					</v-icon>
					<span class="caption ml-1">Atšaukti KŽ pridėjimą</span>
				</v-btn>
			</template>
			<template v-else-if="(mode == 'new-feature') || (mode == 'new-feature-vertical-sign')">
				<v-btn
					class="justify-start"
					depressed
					v-on:click="saveFeature('new')"
					:loading="saveInProgress"
				>
					<v-icon
						title="Išsaugoti objektą"
						color="success"
					>
						mdi-content-save
					</v-icon>
					<v-avatar
						color="success"
						size="18"
						v-if="activeTask"
						class="ml-1"
					>
						<span class="white--text caption">U</span>
					</v-avatar>
					<span class="caption ml-1">Išsaugoti</span>
				</v-btn>
				<template v-if="mode == 'new-feature-vertical-sign'">
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-on:click="destroyNewFeature"
						:disabled="saveInProgress"
					>
						<v-icon
							title="Grįžti į KŽ pridėjimą"
							color="primary"
						>
							mdi-undo-variant
						</v-icon>
						<span class="caption ml-1">Grįžti į KŽ pridėjimą</span>
					</v-btn>
				</template>
				<template v-else>
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-on:click="destroyNewFeature"
						:disabled="saveInProgress"
					>
						<v-icon
							title="Šalinti objektą"
							color="error"
						>
							mdi-delete
						</v-icon>
						<span class="caption ml-1">Šalinti</span>
					</v-btn>
				</template>
			</template>
			<template v-else>
				<v-btn
					class="justify-start"
					depressed
					v-on:click="toggleEditing"
					:disabled="(editingActive && saveInProgress) || destroyInProgress"
				>
					<v-icon
						title="Redaguoti objektą"
						color="primary"
					>
						{{editingActive ? 'mdi-pencil-off' : 'mdi-pencil'}}
					</v-icon>
					<span class="caption ml-1">{{editingActive ? 'Atšaukti redagavimą' : 'Redaguoti'}}</span>
				</v-btn>
				<template v-if="(['v', 'task-related-v'].indexOf(data.type) != -1)">
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-if="!editingActive && data.featureType == 'verticalStreetSignsSupports'"
						v-on:click="setMode('vertical-street-sign-addition')"
						:disabled="destroyInProgress"
					>
						<v-icon
							title="Pridėti KŽ"
							color="primary"
						>
							mdi-plus-circle
						</v-icon>
						<span class="caption ml-1">Pridėti KŽ</span>
					</v-btn>
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-if="!editingActive && (data.featureType == 'verticalStreetSignsSupports') && canVerticalStreetSignsSupportBeRemoved(data)"
						v-on:click="removeVerticalStreetSignsSupport"
						:loading="verticalStreetSignsSupportRemovalInProgress"
						:disabled="destroyInProgress"
					>
						<v-icon
							title="Panaikinti atramą"
							color="primary"
						>
							mdi-table-column-remove
						</v-icon>
						<span class="caption ml-1">Panaikinti atramą</span>
					</v-btn>
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-if="!editingActive"
						v-on:click="showPanorama"
						:disabled="destroyInProgress || (['task-related-v'].indexOf(data.type) != -1)"
					>
						<v-icon
							title="Rodyti panoraminę nuotrauką"
							color="primary"
						>
							mdi-panorama
						</v-icon>
						<span class="caption ml-1">Rodyti panoramą</span>
					</v-btn>
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-if="!activeTask && !editingActive"
						v-on:click="managePhotos"
						:disabled="destroyInProgress"
					>
						<v-icon
							title="Valdyti nuotraukas"
							color="primary"
						>
							mdi-camera
						</v-icon>
						<span class="caption ml-1">Valdyti nuotraukas</span>
					</v-btn>
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-if="!activeTask && !editingActive && needsApproval()"
						v-on:click="approve"
						:loading="approvalInProgress"
						:disabled="destroyInProgress"
					>
						<v-icon
							title="Patvirtinti"
							color="success"
						>
							mdi-check-outline
						</v-icon>
						<span class="caption ml-1">Patvirtinti</span>
					</v-btn>
					<v-btn
						class="mt-2 justify-start"
						depressed
						v-if="!activeTask && !editingActive && needsApproval('rejection')"
						v-on:click="reject"
						:loading="rejectionInProgress"
						:disabled="destroyInProgress"
					>
						<v-icon
							title="Atmesti"
							color="error"
						>
							mdi-close-outline
						</v-icon>
						<span class="caption ml-1">Atmesti</span>
					</v-btn>
				</template>
				<template v-else>
					<template v-if="['horizontalPolylines'].indexOf(data.featureType) != -1">
						<v-btn
							class="mt-2 justify-start"
							depressed
							v-if="!editingActive"
							v-on:click="toggleLineCutting"
						>
							<v-icon
								:title="lineCuttingActive ? 'Atšaukti linijos dalijimą' : 'Dalinti liniją'"
								color="primary"
							>
								mdi-content-cut
							</v-icon>
							<span class="caption ml-1">{{lineCuttingActive ? "Atšaukti linijos dalijimą" : "Dalinti liniją"}}</span>
						</v-btn>
						<v-btn
							class="mt-2 justify-start"
							depressed
							v-if="!editingActive"
							v-on:click="toggleLineJoining"
						>
							<v-icon
								:title="lineJoiningActive ? 'Atšaukti linijos sujungimą' : 'Sujungti liniją su...'"
								color="primary"
							>
								mdi-chart-timeline-variant
							</v-icon>
							<span class="caption ml-1">{{lineJoiningActive ? "Atšaukti linijos sujungimą" : "Sujungti liniją su..."}}</span>
						</v-btn>
					</template>
					<template v-if="data.type != 'task-related'">
						<template v-if="['horizontalPoints', 'horizontalPolylines', 'horizontalPolygons', 'vms-inventorization-l', 'vms-inventorization-p', 'otherPoints', 'otherPolylines', 'otherPolygons'].indexOf(data.featureType) != -1">
							<v-btn
								class="mt-2 justify-start"
								depressed
								v-if="!editingActive"
								v-on:click="managePhotos"
								:disabled="destroyInProgress"
							>
								<v-icon
									title="Valdyti nuotraukas"
									color="primary"
								>
									mdi-camera
								</v-icon>
								<span class="caption ml-1">Valdyti nuotraukas</span>
							</v-btn>
						</template>
					</template>
				</template>
				<v-btn
					class="mt-2 justify-start"
					depressed
					v-on:click="openTask"
					v-if="canHandleTasks && !activeTask && !editingActive && data.feature && data.feature.get('UZDUOTIES_GUID')"
				>
					<v-icon
						title="Atidaryti užduotį"
						color="primary"
					>
						mdi-text-box
					</v-icon>
					<span class="caption ml-1">Atidaryti užduotį</span>
				</v-btn>
				<v-btn
					class="mt-2 justify-start"
					depressed
					v-on:click="showNewTaskInfoDialog"
					v-if="canHandleTasks && !activeTask && !editingActive && (['vms-inventorization-l', 'vms-inventorization-p', 'vvt', 'userPoints'].indexOf(data.featureType) == -1)"
				>
					<v-icon
						title="Formuoti užduotį"
						color="primary"
					>
						mdi-text-box-plus
					</v-icon>
					<span class="caption ml-1">Formuoti užduotį</span>
				</v-btn>
				<v-btn
					class="mt-2 justify-start"
					depressed
					v-if="editingActive"
					v-on:click="saveFeature"
					:loading="saveInProgress"
				>
					<v-icon
						title="Išsaugoti objektą"
						color="success"
					>
						mdi-content-save
					</v-icon>
					<v-avatar
						color="success"
						size="18"
						v-if="activeTask && (['task-related', 'task-related-v'].indexOf(data.type) == -1)"
						class="ml-1"
					>
						<span class="white--text caption">U</span>
					</v-avatar>
					<span class="caption ml-1">Išsaugoti</span>
				</v-btn>
				<v-btn
					class="mt-2 justify-start"
					depressed
					v-if="!editingActive && (['vvt'].indexOf(data.featureType) == -1)"
					v-on:click="destroyFeature"
					:loading="destroyInProgress"
					:disabled="saveInProgress"
				>
					<v-icon
						title="Šalinti objektą"
						color="error"
					>
						mdi-delete
					</v-icon>
					<v-avatar
						color="success"
						size="18"
						v-if="activeTask && (['task-related', 'task-related-v'].indexOf(data.type) == -1)"
						class="ml-1"
					>
						<span class="white--text caption">U</span>
					</v-avatar>
					<span class="caption ml-1">{{activeTask && (['task-related', 'task-related-v'].indexOf(data.type) == -1) ? 'Demontuoti' : 'Šalinti'}}</span>
				</v-btn>
				<v-btn
					class="mt-2 justify-start"
					depressed
					v-if="(['vvt'].indexOf(data.featureType) != -1)"
					v-on:click="showPanorama"
					:disabled="destroyInProgress"
				>
					<v-icon
						title="Rodyti panoraminę nuotrauką"
						color="primary"
					>
						mdi-panorama
					</v-icon>
					<span class="caption ml-1">Rodyti panoramą</span>
				</v-btn>
			</template>
		</div>
	</div>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";
	import Feature from "ol/Feature";
	import Point from "ol/geom/Point";
	import TaskHelper from "./helpers/TaskHelper";
	import {getCenter as getExtentCenter} from "ol/extent";

	export default {
		data: function(){
			var editingAllowed = false,
				canHandleTasks = false;
			if (!this.data.historicMoment) { // Istorinių įrašų gi neleisime redaguoti :D
				if (this.$store.state.userData && this.$store.state.userData.permissions && this.data) {
					var userPermissions = this.$store.state.userData.permissions;
					if (["horizontalPoints", "horizontalPolylines", "horizontalPolygons"].indexOf(this.data.featureType) != -1) {
						if (userPermissions.indexOf("kz-horizontal-edit") != -1) {
							editingAllowed = true;
						}
					} else if (["verticalStreetSigns", "verticalStreetSignsSupports"].indexOf(this.data.featureType) != -1) {
						if (userPermissions.indexOf("kz-vertical-edit") != -1) {
							editingAllowed = true;
						}
					} else if (["otherPoints", "otherPolylines", "otherPolygons"].indexOf(this.data.featureType) != -1) {
						if (userPermissions.indexOf("kz-infra-edit") != -1) {
							editingAllowed = true;
						}
					} else if (["userPoints"].indexOf(this.data.featureType) != -1) {
						editingAllowed = true;
					}
					if ((userPermissions.indexOf("manage-tasks") != -1) || (userPermissions.indexOf("manage-tasks-test") != -1)) {
						canHandleTasks = true;
						if ((userPermissions.indexOf("manage-tasks-test") != -1) && ((this.data.type == "task-related") || this.$store.state.activeTask)) {
							editingAllowed = true; // VGTU atvejis...
						}
					}
				}
			}
			var data = {
				editingAllowed: editingAllowed,
				canHandleTasks: canHandleTasks,
				saveInProgress: false,
				destroyInProgress: false,
				approvalInProgress: false,
				rejectionInProgress: false,
				verticalStreetSignsSupportRemovalInProgress: false
			};
			return data;
		},

		props: {
			data: Object,
			setEditingActive: Function,
			setToggleLineCuttingActive: Function,
			setToggleLineJoiningActive: Function,
			getFeaturePopupContent: Function,
			mode: String,
			setMode: Function,
			editingActive: Boolean,
			lineCuttingActive: Boolean,
			lineJoiningActive: Boolean
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			activeTask: {
				get: function(){
					var activeTask = null;
					if (this.$store.state.activeTask) {
						var feature = this.$store.state.activeTask.feature;
						if (feature && feature != "error") {
							activeTask = this.$store.state.activeTask;
						}
					}
					return activeTask;
				}
			}
		},

		methods: {
			toggleEditing: function(){
				if (this.setEditingActive) {
					this.setEditingActive(!this.editingActive);
				}
			},
			toggleLineCutting: function(){
				if (!this.lineCuttingActive) {
					this.setToggleLineJoiningActive(false);
				}
				if (this.setToggleLineCuttingActive) {
					this.setToggleLineCuttingActive(!this.lineCuttingActive);
				}
			},
			toggleLineJoining: function(){
				if (!this.lineJoiningActive) {
					this.setToggleLineCuttingActive(false);
				}
				if (this.setToggleLineJoiningActive) {
					this.setToggleLineJoiningActive(!this.lineJoiningActive);
				}
			},
			saveFeature: function(type){
				var featurePopupContent = this.getFeaturePopupContent();
				if (featurePopupContent) {
					var formData = featurePopupContent.getFormData();
					if (formData && formData != "invalid") {
						var feature,
							featureType;
						if (type == "new") {
							if (featurePopupContent.activeNewFeatureData) {
								feature = featurePopupContent.activeNewFeatureData.feature;
								featureType = featurePopupContent.activeNewFeatureData.featureType;
							}
						} else {
							feature = this.data.feature;
							featureType = this.data.featureType;
						}
						if (feature && featureType) {
							var dataToSave = [];
							if (featureType == "verticalStreetSignsSupports" && feature.geometryModified) {
								if (this.data.origSignFeatures) {
									// !!! Čia aš jau nieko nesuprantu... Jei pastūmus vertikaliųjų KŽ tvirtinimo vietą į `applyEdits` nepaduosime KŽ masyvo, tai susijusių KŽ geometrija servise update'insis vis tiek??
									// WTF? Gal duomenų rinkinyje yra kažkokia topologijos taisyklė?.. Tada viskas lyg ir logiška!
									// Bet taip palikti rizikinga... Norime patys valdyti susijusių KŽ geometriją!!
									this.data.origSignFeatures.forEach(function(signFeature){
										dataToSave.push({
											feature: signFeature.clone(),
											featureType: "verticalStreetSigns"
										});
									});
								}
							}
							feature = feature.clone();
							feature.setProperties(formData);
							if (featureType == "vvt") {
								feature.layerId = parseInt(this.data.layerId);
							}
							this.myMap.setLayerForFeatureByFeatureType(feature, featureType); // Feature'io layer'io reikia metodui CommonHelper.adjustFeatureProperties()!
							CommonHelper.adjustFeatureProperties(feature, featureType); // Užtvirtinimui!
							if (featureType == "verticalStreetSigns") {
								if (feature.get("ATMESTA") != 1) { // Jei objektas nėra atmestas...
									if (feature.get("PASAL_DATA")) {
										feature.set("STATUSAS", CommonHelper.verticalStreetSignDestroyedStatusValue);
									}
								} else {
									feature.set("ATMESTA", 9); // Toks Kalvelio kvailas domenas...
								}
							}
							var dataItemToSave = {
								feature: feature,
								featureType: featureType
							};
							if (featureType == "vvt") {
								dataItemToSave.layerId = this.data.layerId;
							}
							dataToSave.push(dataItemToSave);
							this.saveInProgress = true;
							this.myMap.drawHelper.saveFeature(dataToSave, this.activeTask).then(function(result){
								this.saveInProgress = false;
								if (this.activeTask) {
									// Logika tokia: iš pradžių standartine procedūra išsisaugo kaip ir paprastas objektas išsisaugotų, o po to spec. modifikacijos...
									TaskHelper.saveFeature(dataToSave, this.activeTask, this.$store).then(function(){
										feature.isTasksRelated = true;
										this.myMap.setLayerForFeatureByFeatureType(feature, featureType);
										this.$store.commit("setActiveFeaturePreview", feature);
										CommonHelper.routeTo({
											router: this.$router,
											vBus: this.$vBus,
											feature: feature
										});
									}.bind(this), function(){
										this.$vBus.$emit("show-message", {
											type: "warning",
											message: "Atsiprašome, įvyko nenumatyta klaida sinchronizuojant užduoties objektus..."
										});
									}.bind(this));
								} else {
									if (result && result.featureDestroyApproved) {
										CommonHelper.routeTo({
											router: this.$router,
											vBus: this.$vBus
										});
									} else {
										// Dabar sėkmingai išsaugojus objektą vėl iš naujo bandoma atidaryti jo popup'ą... TODO, FIXME! O jei mums tai nebėra
										// aktualu? O kas bus, jei labai ilgai laukėme atsako ir nesulaukus jo uždarėme prieš tai buvusį popup'ą? Gaunasi, kad dabar mums per prievartą rodys
										// nebeaktualų popup'ą?..
										this.$store.commit("setActiveFeaturePreview", feature);
										CommonHelper.routeTo({
											router: this.$router,
											vBus: this.$vBus,
											feature: feature
										});
										// TODO... Jei tai vertikalus KŽ, tai atnaujinti jo unikalaus simbolio timestamp'ą? Ar ne?..
									}
								}
							}.bind(this), function(){
								this.saveInProgress = false;
								this.$vBus.$emit("show-message", {
									type: "warning",
									message: "Atsiprašome, įvyko nenumatyta klaida... Objektas nebuvo išsaugotas"
								});
							}.bind(this));
						} else {
							this.$vBus.$emit("show-message", {
								type: "warning"
							});
						}
					} else {
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Yra formos pildymo klaidų!"
						});
					}
				}
			},
			destroyFeature: function(){
				var title = "Ar tikrai šalinti objektą?",
					message = "Ar tikrai šalinti objektą? Pašalinus objektą nebebus galima jo atstatyti...",
					positiveActionTitle = "Šalinti objektą";
				if (this.activeTask) {
					if (["task-related", 'task-related-v'].indexOf(this.data.type) == -1) {
						title = "Ar tikrai demontuoti objektą?";
						message = "Ar tikrai demontuoti objektą?";
						positiveActionTitle = "Demontuoti objektą";
					} else {
						message = "Ar tikrai šalinti objektą?";
					}
				}
				this.$vBus.$emit("confirm", {
					title: title,
					message: message,
					positiveActionTitle: positiveActionTitle,
					negativeActionTitle: "Atšaukti",
					positive: function(){
						var feature = this.data.feature,
							featureType = this.data.featureType;
						if (feature && featureType) {
							if (featureType == "verticalStreetSignsSupports" && this.data.additionalData && this.data.additionalData.signs && this.data.additionalData.signs.length && !this.activeTask) {
								this.$vBus.$emit("show-message", {
									type: "warning",
									message: "Norint pašalinti tvirtinimo vietą, prieš tai reikia pašalinti visus susijusius KŽ!"
								});
							} else {
								this.destroyInProgress = true;
								if (this.activeTask && (["task-related", 'task-related-v'].indexOf(this.data.type) == -1)) {
									// TODO! Čia dar reikia tikrinti kokį objektą naikiname: originalaus sluoksnio, ar su užduotimi susijusio sluoksnio?..
									feature = feature.clone();
									feature.set(CommonHelper.taskFeatureActionFieldName, CommonHelper.taskFeatureActionValues["delete"]);
									var dataToSave = [{
										feature: feature,
										featureType: featureType
									}];
									this.myMap.drawHelper.saveFeature(dataToSave, this.activeTask).then(function(){
										this.destroyInProgress = false;
										// Logika tokia: iš pradžių standartine procedūra išsisaugo kaip ir paprastas objektas išsisaugotų, o po to spec. modifikacijos...
										TaskHelper.saveFeature(dataToSave, this.activeTask, this.$store).then(function(){
											feature.isTasksRelated = true;
											this.myMap.setLayerForFeatureByFeatureType(feature, featureType);
											this.$store.commit("setActiveFeaturePreview", feature);
											CommonHelper.routeTo({
												router: this.$router,
												vBus: this.$vBus,
												feature: feature
											});
										}.bind(this), function(){
											this.$vBus.$emit("show-message", {
												type: "warning",
												message: "Atsiprašome, įvyko nenumatyta klaida sinchronizuojant užduoties objektus..."
											});
										}.bind(this));
									}.bind(this), function(){
										this.destroyInProgress = false;
										this.$vBus.$emit("show-message", {
											type: "warning",
											message: "Atsiprašome, įvyko nenumatyta klaida... Objektas nebuvo išsaugotas"
										});
									}.bind(this));
								} else {
									var activeTask;
									if (["task-related", 'task-related-v'].indexOf(this.data.type) != -1) {
										if (this.activeTask) {
											activeTask = this.activeTask;	
										} else {
											// Taip niekada neturėtų nutikti!!! Nebeleidžiame tęsti, nes kitaip pašalintų objektą iš gerojo sluoksnio :) Kuriant funkcionalumą taip kartą nutiko :) Atsargiai!
											return;
										}
									}
									this.myMap.drawHelper.destroyFeature(feature, featureType, activeTask).then(function(){
										this.destroyInProgress = false;
										if (activeTask) {
											TaskHelper.onFeatureDestroy(this.activeTask, this.$store, feature, this.$router, this.$vBus);
										} else {
											// Tiesiog uždarome popup'ą...
											CommonHelper.routeTo({
												router: this.$router,
												vBus: this.$vBus
											});
											this.$vBus.$emit("show-message", {
												type: "success",
												message: "Objektas pašalintas sėkmingai!",
												timeout: 2000
											});
										}
									}.bind(this), function(){
										this.destroyInProgress = false;
										this.$vBus.$emit("show-message", {
											type: "warning",
											message: "Atsiprašome, įvyko nenumatyta klaida... Objektas nebuvo pašalintas"
										});
									}.bind(this));
								}
							}
						} else {
							this.$vBus.$emit("show-message", {
								type: "warning"
							});
						}
					}.bind(this)
				});
			},
			destroyNewFeature: function(){
				if (this.data.isNew) {
					CommonHelper.routeTo({
						router: this.$router,
						vBus: this.$vBus
					});
				} else {
					this.setMode("vertical-street-sign-addition");
				}
				if (this.myMap && this.myMap.drawLayer) {
					this.myMap.drawLayer.getSource().clear(true);
				}
			},
			needsApproval: function(mode){
				var needsApproval = false;
				if (this.$store.state.userData && this.$store.state.userData.permissions) {
					if (this.$store.state.userData.permissions.indexOf("approve") != -1) {
						if (this.data.feature.get("PATVIRTINTAS") === 0 || this.data.feature.get("PATVIRTINIMAS") === 0) {
							needsApproval = true;
						} else if (this.data.feature.get("ATMESTA")) {
							if (mode != "rejection") {
								needsApproval = true;
							}
						}
					}
				}
				return needsApproval;
			},
			approve: function(){
				this.$vBus.$emit("confirm", {
					title: "Ar tikrai patvirtinti objektą?",
					positiveActionTitle: "Patvirtinti objektą",
					negativeActionTitle: "Atšaukti",
					positive: function(){
						var feature = this.data.feature,
							featureType = this.data.featureType;
						if (feature && featureType) {
							this.approvalInProgress = true;
							feature = feature.clone();
							if (featureType == "verticalStreetSigns" || featureType == "verticalStreetSignsSupports") {
								if (feature.get("STATUSAS") != CommonHelper.verticalStreetSignDestroyedStatusValue) {
									if (feature.get("PASAL_DATA") && feature.get("ATMESTA") != 1) {
										feature.set("STATUSAS", 3);
									} else {
										feature.set("STATUSAS", 4);
									}
								}
							}
							feature.set("PATVIRTINTAS", 1);
							feature.set("PATVIRTINIMAS", 1);
							feature.set("ATMESTA", null);
							this.myMap.setLayerForFeatureByFeatureType(feature, featureType);
							var dataToSave = [{
								feature: feature,
								featureType: featureType
							}];
							this.myMap.drawHelper.saveFeature(dataToSave).then(function(result){
								this.approvalInProgress = false;
								if (result && result.featureDestroyApproved) {
									CommonHelper.routeTo({
										router: this.$router,
										vBus: this.$vBus
									});
								} else {
									this.$store.commit("setActiveFeaturePreview", feature);
									CommonHelper.routeTo({
										router: this.$router,
										vBus: this.$vBus,
										feature: feature
									});
									this.$vBus.$emit("show-message", {
										type: "success",
										message: "Objektas patvirtintas sėkmingai!",
										timeout: 2000
									});
								}
							}.bind(this), function(){
								this.approvalInProgress = false;
								this.$vBus.$emit("show-message", {
									type: "warning",
									message: "Atsiprašome, įvyko nenumatyta klaida... Objektas nebuvo nepatvirtintas..."
								});
							}.bind(this));
						} else {
							this.$vBus.$emit("show-message", {
								type: "warning"
							});
						}
					}.bind(this)
				});
			},
			reject: function(){
				this.$vBus.$emit("confirm", {
					title: "Ar tikrai atmesti objektą?",
					positiveActionTitle: "Atmesti objektą",
					negativeActionTitle: "Atšaukti",
					textarea: {
						placeholder: "Atmetimo priežastis"
					},
					positive: function(dialog){
						var positiveCallback = function(){
							var feature = this.data.feature,
								featureType = this.data.featureType;
							if (feature && featureType) {
								this.rejectionInProgress = true;
								feature = feature.clone();
								if (featureType == "verticalStreetSigns") {
									feature.set("ATMESTA", 1);
								} else if (featureType == "verticalStreetSignsSupports") {
									feature.set("ATMESTA", 1);
								}
								feature.set("PATVIRTINTAS", 1);
								feature.set("PATVIRTINIMAS", 1);
								if (dialog) {
									feature.set("ATMETIMO_PRIEZASTIS", dialog.message);
								}
								this.myMap.setLayerForFeatureByFeatureType(feature, featureType);
								var dataToSave = [{
									feature: feature,
									featureType: featureType
								}];
								this.myMap.drawHelper.saveFeature(dataToSave).then(function(result){
									this.rejectionInProgress = false;
									if (result && result.featureDestroyApproved) { // Hmmm...! Nukopijuota nuo "patvirtinimo"... Realiai niekada neturėtų būti "result.featureDestroyApproved"?..
										CommonHelper.routeTo({
											router: this.$router,
											vBus: this.$vBus
										});
									} else {
										this.$store.commit("setActiveFeaturePreview", feature);
										CommonHelper.routeTo({
											router: this.$router,
											vBus: this.$vBus,
											feature: feature
										});
										this.$vBus.$emit("show-message", {
											type: "success",
											message: "Objektas atmestas sėkmingai!",
											timeout: 2000
										});
									}
								}.bind(this), function(){
									this.rejectionInProgress = false;
									this.$vBus.$emit("show-message", {
										type: "warning",
										message: "Atsiprašome, įvyko nenumatyta klaida... Objektas nebuvo atmestas..."
									});
								}.bind(this));
							} else {
								this.$vBus.$emit("show-message", {
									type: "warning"
								});
							}
						}.bind(this);
						positiveCallback();
					}.bind(this)
				});
			},
			showPanorama: function(){
				var feature;
				if (this.data.additionalData && this.data.additionalData.supports && this.data.additionalData.supports.length && this.data.additionalData.supports.length == 1) {
					feature = this.data.additionalData.supports[0];
				} else if (["vvt"].indexOf(this.data.featureType) != -1) {
					if (this.data.feature.getGeometry().getType() == "Point") {
						feature = this.data.feature;
					} else {
						feature = new Feature({
							geometry: new Point(getExtentCenter(this.data.feature.getGeometry().getExtent()))
						});
					}
				}
				if (feature) {
					this.$vBus.$emit("show-point-panorama-viewer", {
						title: "Panoraminis vaizdas",
						feature: feature,
						source: "vms-2022",
						lookAt: true
					});
				}
			},
			managePhotos: function(){
				var feature,
					featureType;
				if (this.data.additionalData && this.data.additionalData.supports && this.data.additionalData.supports.length && this.data.additionalData.supports.length == 1) {
					feature = this.data.additionalData.supports[0];
					featureType = "verticalStreetSignsSupports";
				}
				if (!feature) {
					if (["horizontalPoints", "horizontalPolylines", "horizontalPolygons", "vms-inventorization-l", "vms-inventorization-p", "otherPoints", "otherPolylines", "otherPolygons"].indexOf(this.data.featureType) != -1) {
						feature = this.data.feature;
						featureType = this.data.featureType;
					}
				}
				if (feature) {
					this.$vBus.$emit("show-feature-photos-manager-dialog", {
						feature: feature,
						featureType: featureType
					});
				}
			},
			showNewTaskInfoDialog: function(){
				this.$vBus.$emit("show-task-info-dialog", {
					title: "Naujos užduoties objektui informacija",
					origFeature: this.data.feature,
					origFeatureType: this.data.featureType,
					origFeatureData: this.data
				});
			},
			openTask: function(){
				this.$store.commit("setActiveTask", {
					globalId: this.data.feature.get("UZDUOTIES_GUID")
				});
			},
			removeVerticalStreetSignsSupport: function(){
				this.$vBus.$emit("confirm", {
					title: "Ar tikrai panaikinti atramą?",
					positiveActionTitle: "Panaikinti atramą",
					negativeActionTitle: "Atšaukti",
					positive: function(){
						var feature = this.data.feature,
							featureType = this.data.featureType;
						if (feature && featureType) {
							if (featureType == "verticalStreetSignsSupports") {
								this.verticalStreetSignsSupportRemovalInProgress = true;
								feature = feature.clone();
								feature.set("STATUSAS", 3);
								feature.set("PATVIRTINTAS", 0);
								feature.set("PATVIRTINIMAS", 0);
								this.myMap.setLayerForFeatureByFeatureType(feature, featureType);
								var dataToSave = [{
									feature: feature,
									featureType: featureType
								}];
								this.myMap.drawHelper.saveFeature(dataToSave).then(function(){
									this.verticalStreetSignsSupportRemovalInProgress = false;
									this.$store.commit("setActiveFeaturePreview", feature);
									CommonHelper.routeTo({
										router: this.$router,
										vBus: this.$vBus,
										feature: feature
									});
									this.$vBus.$emit("show-message", {
										type: "success",
										message: "Objektas atmestas sėkmingai!",
										timeout: 2000
									});
								}.bind(this), function(){
									this.verticalStreetSignsSupportRemovalInProgress = false;
									this.$vBus.$emit("show-message", {
										type: "warning",
										message: "Atsiprašome, įvyko nenumatyta klaida... Atrama nebuvo panaikinta..."
									});
								}.bind(this));
							}
						} else {
							this.$vBus.$emit("show-message", {
								type: "warning"
							});
						}
					}.bind(this)
				});
			},
			canVerticalStreetSignsSupportBeRemoved: function(data){
				if (data && data.additionalData) {
					if (data.additionalData.signs && data.additionalData.signs.length) {
						data.additionalData.signs.forEach(function(){
							// Jei visi susiję KŽ yra -> {signFeature.get("STATUSAS") == 3 && signFeature.get("PATVIRTINTAS") == 1)}, tada irgi funkcionalumas galimas... Bet taip čia niekada nebus, nes tokie feature'ai jau buvo atfiltruoti...
						});
					} else {
						return true;
					}
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
		border-top-left-radius: 10px;
		border-bottom-left-radius: 10px;
		max-height: 100%;
	}
</style>