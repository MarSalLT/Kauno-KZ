<template>
	<v-dialog
		persistent
		v-model="dialog"
		max-width="1200"
		scrollable
	>
		<v-card>
			<v-card-title>
				<span>Užduoties objekto nuotraukų valdymas</span>
			</v-card-title>
			<v-card-text class="pt-4 pb-2">
				<template v-if="data">
					<template v-if="data == 'error'">
						<v-alert
							dense
							type="error"
							class="ma-0 d-inline-block"
						>
							Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
						</v-alert>
					</template>
					<template v-else>
						<template v-if="data.photos">
							<div class="mb-3"><strong>Esamos KŽ nuotraukos:</strong></div>
							<template v-if="data.photos.length">
								<PhotosRow
									:photos.sync="data.photos"
									:getKey="getKey"
									actions="edit"
									:onPhotoRemove="onPhotoRemove"
									:onPhotoMod="onPhotoMod"
									class="mb-5"
								/>
							</template>
							<template v-else>
								<div>Nuotraukų nėra</div>
							</template>
						</template>
						<template v-if="data.taskPhotos">
							<div class="mb-3 mt-5"><strong>Užduoties nuotraukos:</strong></div>
							<template v-if="data.taskPhotos.length">
								<PhotosRow
									:photos.sync="data.taskPhotos"
									:getKey="getKey"
									:transferPhoto="transferPhoto"
									actions="edit"
									type="transfer"
									class="mb-5"
								/>
							</template>
							<template v-else>
								<div>Nuotraukų nėra</div>
							</template>
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
					outlined
					small
					:disabled="!(photosCase && (photosCase.add || photosCase.remove || photosCase.mod))"
					v-if="data && data.photos != 'error'"
					v-on:click="save"
					:loading="saveInAction"
				>
					Išsaugoti
				</v-btn>
				<v-btn
					color="blue darken-1"
					text
					v-on:click="closeDialog"
					outlined
					small
				>
					Uždaryti
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import PhotoHelper from "../feature-photos-manager/PhotoHelper";
	import PhotosHelper from "../feature-photos-manager/PhotosHelper";
	import PhotosRow from "../feature-photos-manager/PhotosRow";
	import TaskHelper from "../helpers/TaskHelper";
	import Vue from "vue";

	export default {
		data: function(){
			var data = {
				dialog: false,
				data: null,
				key: 0,
				photosCase: null,
				saveInAction: null
			};
			return data;
		},

		components: {
			PhotosRow
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		created: function(){
			this.$vBus.$on("show-task-attachments-transfer-dialog", this.showDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-task-attachments-transfer-dialog", this.showDialog);
		},

		methods: {
			showDialog: function(e){
				this.e = e;
				this.getData(e);
				this.dialog = true;
			},
			closeDialog: function(){
				this.dialog = false;
			},
			getData: function(e){
				this.data = null;
				// Iš dalies vadovautasi kapinių app "app\src\components\dialogs\ReportPhotosApprovalDialog.vue"
				// Dar geras pavyzdys yra KŽ modulis "app\src\components\AttachmentTransferButton.vue"
				// Produkcijoje testuoti (vertikalųjį KŽ) -> UŽDUOTIS: Drujos g. apipaišytas pėsčiųjų perėjos ženklas
				// Produkcijoje testuoti (kitą KŽ) -> UŽDUOTIS: Vytauto Nasvyčio g.
				if (e.feature) {
					this.saveInAction = false;
					this.photosCase = {
						add: 0,
						remove: 0,
						photosToRemove: [],
						mod: 0
					};
					var taskFeatureOriginalGlobalId = e.feature.feature.get(CommonHelper.taskFeatureOriginalGlobalIdFieldName);
					this.getOrigFeature(e.feature.feature, taskFeatureOriginalGlobalId, e.feature.featureType, e.feature.action, e).then(function(feature){
						if (e.feature.featureType == "verticalStreetSigns") {
							// Prie vertikalaus KŽ attachment'ų neprikabinėjame... Ieškome tvirtinimo vietos...
							this.getOrigFeature(feature, feature.attributes["GUID"], "verticalStreetSignsSupports").then(function(supportFeature){
								this.getPhotos(supportFeature, "verticalStreetSignsSupports");
							}.bind(this), function(){
								this.data = "error";
							}.bind(this));
						} else {
							this.getPhotos(feature, e.feature.featureType);
						}
					}.bind(this), function(){
						this.data = "error";
					}.bind(this));
				} else {
					this.data = "error";
				}
			},
			getOrigFeature: function(feature, taskFeatureOriginalGlobalId, featureType, action, e){
				var promise = new Promise(function(resolve, reject){
					var layerIdMeta = CommonHelper.layerIds[featureType];
					if (layerIdMeta) {
						var serviceUrl = this.$store.getters.getServiceUrl(layerIdMeta[0]);
						if (serviceUrl) {
							serviceUrl += "/" + layerIdMeta[1];
							serviceUrl = CommonHelper.prependProxyIfNeeded(serviceUrl.replace("/FeatureServer", "/MapServer") + "/query");
							var params;
							if ((action == "update") || !action) {
								if (taskFeatureOriginalGlobalId) {
									params = {
										f: "json",
										outFields: "*",
										where: "GlobalID = '" + taskFeatureOriginalGlobalId + "'"
									};
									CommonHelper.getFetchPromise(serviceUrl, function(json){
										return json;
									}.bind(this), "POST", params).then(function(result){
										if (result.features && result.features.length == 1) {
											resolve(result.features[0]);
										} else {
											reject();
										}
									}.bind(this), function(){
										reject();
									}.bind(this));
								} else {
									reject();
								}
							} else if ((action == "add") && e) {
								params = {
									f: "json",
									outFields: "*",
									where: "UZDUOTIES_OBJEKTO_GUID" + " = '" + feature.get("GlobalID") + "'"
								};
								CommonHelper.getFetchPromise(serviceUrl, function(json){
									return json;
								}.bind(this), "POST", params).then(function(result){
									if (result.features && result.features.length == 1) {
										resolve(result.features[0]);
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
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			},
			getPhotos: function(feature, featureType){
				this.refFeatureData = {
					feature: feature,
					featureType: featureType
				}
				CommonHelper.getPhotos({
					featureObjectId: feature.attributes["OBJECTID"], // FIXME: ne'hardcode'inti...
					featureType: featureType,
					store: this.$store
				}).then(function(photos){
					photos.forEach(function(photo){
						photo.key = this.getKey();
					}.bind(this));
					this.data = {
						photos: photos
					}
					CommonHelper.getPhotos({
						feature: this.e.task.feature,
						featureType: "tasks",
						store: this.$store
					}).then(function(photos){
						photos.forEach(function(photo){
							photo.key = this.getKey();
							photo.keywords = "Užduoties priedas"; // Svarbu anuliuoti "main", nes kitaip PhotosRow'e bus specifiškas (šiuo atveju nepageidaujamas) funkcionalumas...
							photo.canBeTransferred = true;
							photo.brandNew = true;
						}.bind(this));
						TaskHelper.getComments(this.e.task.feature.getProperties()).then(function(result){
							if (result.comments) {
								result.comments.forEach(function(comment){
									if (comment.attachments) {
										comment.attachments.forEach(function(attachment){
											if (attachment.type == "image") {
												attachment.src = CommonHelper.webServicesRoot + "tasks/get-task-comment-attachment-from-einpix?url=" + attachment.url + "&token=" + result.token;
												if (attachment.original_filename && attachment.original_extension) {
													attachment.name = attachment.original_filename + "." + attachment.original_extension;
												}
												attachment.key = this.getKey();
												attachment.contentType = attachment.mimetype;
												attachment.isImage = true;
												attachment.noCache = true;
												attachment.keywords = "Užduoties komentaro priedas";
												attachment.canBeTransferred = true;
												attachment.brandNew = true;
												photos.push(attachment);
											}
										}.bind(this));
									}
								}.bind(this));
							}
							if (this.data) {
								Vue.set(this.data, "taskPhotos", photos);
							}
						}.bind(this), function(){
							// ...
						}.bind(this));
					}.bind(this), function(){
						// ...
					}.bind(this));
				}.bind(this), function(){
					this.data = "error";
				}.bind(this));
			},
			getKey: function(){
				this.key += 1;
				return this.key;
			},
			transferPhoto: function(photo){
				photo.canBeTransferred = false;
				photo = Object.assign({}, photo);
				photo.key = this.getKey();
				PhotoHelper.getExif(photo).then(function(e){
					PhotoHelper.getProperSrc(e).then(function(src){
						src = PhotoHelper.prependExif(src, e.exif);
						PhotoHelper.getFile(src, photo).then(function(file){
							photo.file = file; // Tai svarbus žingsnis, nes gi prieš tai turėjome tik nuorodą!
						}.bind(this), function(err){
							console.error(err);
						});
					}.bind(this));
				}.bind(this));
				var photos = this.data.photos || [];
				photos = photos.slice();
				photos.push(photo);
				Vue.set(this.data, "photos", photos);
				this.photosCase.add += 1;
			},
			onPhotoRemove: function(photo){
				if (photo.brandNew) {
					this.photosCase.add -= 1;
				} else {
					this.photosCase.remove += 1;
					this.photosCase.photosToRemove.push(photo);
				}
				if (this.data.taskPhotos) {
					this.data.taskPhotos.forEach(function(taskPhoto){
						if (taskPhoto.id == photo.id) {
							taskPhoto.canBeTransferred = true;
						}
					});
				}
			},
			onPhotoMod: function(){
				this.photosCase.mod += 1;
			},
			save: function(){
				if (this.data && this.data.photos && this.refFeatureData) {
					var valid = true;
					this.data.photos.some(function(photo){
						if (photo.brandNew && !photo.file) {
							valid = false;
							return true;
						}
					});
					if (valid) {
						var layerInfo = this.myMap.getLayerInfo(this.refFeatureData.featureType) || {},
							objectIdField = layerInfo.objectIdField || "OBJECTID";
						var photosTaskCallback = function(status, errors){
							this.photosTaskCallback(status, errors);
						}.bind(this);
						PhotosHelper.tryExecutingPhotosTask(
							this.refFeatureData.feature.attributes[objectIdField],
							this.refFeatureData.featureType,
							this.data.photos,
							this.photosCase,
							photosTaskCallback,
							this.$vBus
						);
					}
				}
			},
			photosTaskCallback: function(status, errors){
				if (status == "start") {
					this.saveInAction = true;
				} else if (status == "end") {
					this.saveInAction = false;
					if (errors.length) {
						this.getData(this.e);
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Išsaugant nuotraukas buvo klaidų..."
						});
					} else {
						this.closeDialog();
					}
				}
			}
		}
	}
</script>