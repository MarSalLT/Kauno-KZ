<template>
	<v-dialog
		persistent
		v-model="dialog"
		scrollable
		content-class="sc-dialog"
	>
		<v-card>
			<v-card-title>
				<span>{{e && e.masterAttachment ? "Užduoties priedas: Brėžinys/schema" : "Užduoties priedas"}}</span>
			</v-card-title>
			<v-card-text class="pb-0">
				<template v-if="e">
					<div class="interactive-image-wrapper">
						<template v-if="e.masterAttachment">
							<TaskMasterAttachmentManager
								:task="e.task"
								:attachment="e.attachment"
								:onMapLoad="onInteractiveImageLoad"
								:key="key"
								ref="taskMasterAttachmentManager"
							/>
						</template>
						<template v-else-if="e.src">
							<InteractiveImage
								:photo="e"
								:onInteractiveImageLoad="onInteractiveImageLoad"
								:dark="true"
								:key="key"
								ref="interactiveImage"
							/>
						</template>
					</div>
				</template>
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="save"
					outlined
					small
					:loading="saveInProgress"
					v-if="map"
				>
					Išsaugoti
				</v-btn>
				<v-btn
					color="blue darken-1"
					text
					v-on:click="save('pdf')"
					outlined
					small
					:loading="saveInProgress"
					v-if="map && e && e.masterAttachment"
				>
					Išsaugoti kaip PDF
				</v-btn>
				<v-btn
					color="blue darken-1"
					text
					v-on:click="dialog = false"
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
	import CommonHelper from "../helpers/CommonHelper";
	import InteractiveImage from "../sc/InteractiveImage";
	import PhotoHelper from "../feature-photos-manager/PhotoHelper";
	import PhotosHelper from "../feature-photos-manager/PhotosHelper";
	import TaskHelper from "../helpers/TaskHelper";
	import TaskMasterAttachmentManager from "../TaskMasterAttachmentManager";
	import TaskMasterAttachmentMapHelper from "../helpers/TaskMasterAttachmentMapHelper";
	import { jsPDF } from "jspdf";

	export default {
		data: function(){
			var data = {
				dialog: false,
				e: null,
				key: 0,
				saveInProgress: false,
				map: null
			};
			return data;
		},

		components: {
			InteractiveImage,
			TaskMasterAttachmentManager
		},

		created: function(){
			this.$vBus.$on("task-attachment-new", this.showDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("task-attachment-new", this.showDialog);
		},

		methods: {
			showDialog: function(e){
				this.e = e;
				this.map = null;
				this.key += 1;
				this.dialog = true;
			},
			onInteractiveImageLoad: function(map){
				this.map = map;
			},
			save: function(type){
				var name = "Brezinys-schema",
					src = this.e.src,
					contentType;
				if (this.e.attachment) {
					contentType = this.e.attachment.contentType;
				}
				var initSave = function(src, contentType, contentData){
					if (src) {
						this.saveInProgress = true;
						var imageType;
						if ((contentType == "image/png") || (src.substring(0, 14) == "data:image/png")) {
							name += ".png";
							imageType = "PNG";
						} else if ((contentType == "image/jpeg") || (src.substring(0, 15) == "data:image/jpeg")) {
							name += ".jpg";
							imageType = "JPEG";
						} else {
							console.log("Different content type?", this.e); // TODO...
						}
						var keywords;
						if (this.e.tempAttachment) {
							keywords = "temp";
						} else if (this.e.masterAttachment) {
							keywords = "main";
						} else {
							if (this.e.attachment) {
								keywords = this.e.attachment.keywords;
							}
						}
						var doSave = function(){
							PhotoHelper.getFile(src, {name: name}).then(function(file){
								var doSaveWithFile = function(file){
									file.key = "new";
									if (this.e.id) {
										var attachmentGlobalId;
										if (this.e.attachment) {
											attachmentGlobalId = this.e.attachment.globalId;
										}
										TaskMasterAttachmentMapHelper.saveAttachmentContentData(attachmentGlobalId, contentData).then(function(){
											PhotosHelper.replacePhoto(this.e.id, "tasks", {
												file: file,
												id: this.e.attachmentId
											}, keywords).then(function(){
												this.saveInProgress = false;
												this.dialog = false;
												TaskHelper.notifyAboutTaskChangeToEinpix(this.e.task.feature.getProperties(), "attachment_edit");
												this.$vBus.$emit("refresh-feature-photos-manager-dialog");
											}.bind(this), function(){
												this.saveInProgress = false;
												this.$vBus.$emit("show-message", {
													type: "warning"
												});
											}.bind(this));
										}.bind(this), function(){
											this.saveInProgress = false;
											this.$vBus.$emit("show-message", {
												type: "warning",
												message: "Atsiprašome... Nepavyko išsaugoti priedo aprašomosios info..."
											});
										}.bind(this));
									} else {
										if (this.e.task) {
											PhotosHelper.addPhoto(this.e.task.feature.get("OBJECTID"), "tasks", {file: file}, keywords).then(function(response){ // FIXME... Ne'hardcode'inti "OBJECTID"...
												var onSuccess = function(){
													this.saveInProgress = false;
													this.dialog = false;
													TaskHelper.notifyAboutTaskChangeToEinpix(this.e.task.feature.getProperties(), "attachment_edit");
													this.$vBus.$emit("refresh-feature-photos-manager-dialog");
												}.bind(this);
												if (this.e.masterAttachment) {
													if (response && response.addAttachmentResult) {
														CommonHelper.getPhotos({
															featureObjectId: this.e.task.feature.get("OBJECTID"),
															feature: this.e.task.feature,
															featureType: "tasks",
															justImages: false, // `false`, nes gi gali būti ir pdf!!!
															store: this.$store
														}).then(function(images){
															var attachmentGlobalId;
															if (images) {
																images.some(function(image){
																	if (image.id == response.addAttachmentResult.objectId) {
																		attachmentGlobalId = image.globalId;
																		return true;
																	}
																});
															}
															if (attachmentGlobalId) {
																TaskMasterAttachmentMapHelper.saveAttachmentContentData(attachmentGlobalId, contentData).then(function(){
																	onSuccess();
																}.bind(this), function(){
																	onSuccess();
																}.bind(this));
															} else {
																onSuccess();
															}
														}.bind(this), function(){
															onSuccess();
														}.bind(this));
													} else {
														onSuccess();
													}
												} else {
													onSuccess();
												}
											}.bind(this), function(){
												this.saveInProgress = false;
												this.$vBus.$emit("show-message", {
													type: "warning"
												});
											}.bind(this));
										}
									}
								}.bind(this);
								if (type == "pdf") {
									// Naudoti https://www.npmjs.com/package/jspdf
									var img = new Image();
									img.setAttribute("crossOrigin", "anonymous");
									img.onload = function(){
										var doc = new jsPDF({
											orientation: "l",
											unit: "pt",
											format: [this.width, this.height]
										});
										doc.addImage(file.src, imageType, 0, 0, this.width, this.height);
										// doc.save(name + ".pdf"); // Lokaliai testavimui...
										file = new File([doc.output("blob")], "Brezinys-schema.pdf", {
											type: "application/pdf",
										});
										doSaveWithFile(file);
									};
									img.src = file.src;
								} else {
									doSaveWithFile(file);
								}
							}.bind(this), function(err){
								console.error(err);
							});
						}.bind(this);
						doSave();
					}
				}.bind(this);
				if (this.e.masterAttachment) {
					var taskMasterAttachmentManager = this.$refs.taskMasterAttachmentManager;
					if (taskMasterAttachmentManager && taskMasterAttachmentManager.getAllData) {
						var dataPromise = taskMasterAttachmentManager.getAllData();
						if (dataPromise) {
							dataPromise.then(function(data){
								// Šiame etape atskiras servisas jau būna sugeneravęs mums PDF, turime aprašomąją brėžinio/schemos info...
								// Dabar reikia atsekti vartotojo įkeltas tas tarpines nuotraukas ir išsaugoti jas... (IŠSAUGOTI TIK TAS, KURIOS BUVO NAUDOTOS KURIANT BRĖŽINIO/SCHEMOS TURINĮ)? Ar visas išsaugoti?..
								var images = [];
								if (data.images) {
									data.images.forEach(function(image){
										if (image.brandNew) { // FIXME! Išsaugot ne visus naujai įkeltus, o tik tokius, kurie buvo panaudoti??...
											images.push(image);
										}
									});
								}
								this.saveInProgress = true;
								var taskId = this.e.id;
								if (!taskId) {
									// Čia šito prireikė 2022.08.19... Justinas pastebėjo... Kad yra klaida kai kuri naują schemą/brėžinį, o ne kai redaguoji jau esamą...
									if (this.e.task && this.e.task.feature) {
										taskId = this.e.task.feature.get("OBJECTID");
									}
								}
								if (taskId) {
									PhotosHelper.tryExecutingPhotosTask(taskId, "tasks", images, {photosToRemove: []}, function(status, errors, res){
										if (status == "end") {
											this.saveInProgress = false;
											if (res && res.length) {
												if (data.data && data.data.features) {
													data.data.features.forEach(function(feature){
														if ((feature.type == "image-mock-polygon") && !feature["attachment-id"]) {
															res.some(function(r){
																if (r.task && (r.task.action == "add") && r.task.photo && (r.task.photo.key == feature["img-key"])) {
																	if (r.res) {
																		feature["attachment-id"] = r.res.addAttachmentResult.objectId;
																		delete feature["img-key"];
																	}
																	return true;
																}
															});
														}
													});
												}
											}
											initSave(data.dataURL, null, data.data);
										}
									}.bind(this), this.$vBus);
								} else {
									this.saveInProgress = false;
									if (data.data && data.data.features) {
										data.data.features.forEach(function(feature){
											if ((feature.type == "image-mock-polygon") && !feature["attachment-id"]) {
												delete feature["img-key"];
											}
										});
									}
									initSave(data.dataURL, null, data.data);
								}
							}.bind(this));
						}
					}
				} else {
					initSave(src, contentType);
				}
			}
		}
	}
</script>

<style scoped>
	.interactive-image-wrapper {
		height: 100%;
		position: relative;
	}
	.interactive-image-controls {
		position: absolute;
		top: 3px;
		right: 3px;
		bottom: 3px;
	}
</style>