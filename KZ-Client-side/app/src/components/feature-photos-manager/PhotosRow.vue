<template>
	<v-fade-transition
		group
		tag="div"
		:class="asColumn ? '' : 'd-flex mt-n4 mr-n4 flex-wrap'"
	>
		<template v-for="(photo, i) in photos">
			<v-card
				:key="photo.key"
				:width="width"
				:height="height"
				:class="['image-card', asColumn ? (i ? 'mt-1' : null) : 'mt-4 mr-4']"
				tile
				outlined
			>
				<template v-if="photo.isImage">
					<v-row
						no-gutters
						justify="center"
						class="full-height"
					>
						<v-tooltip
							left
							:disabled="!Boolean(tooltip)"
						>
							<template v-slot:activator="{on, attrs}">
								<v-img
									:src="photo.src + (photo.brandNew || photo.rotated || photo.noCache ? '' : '?key=' + photo.key)"
									:max-width="width"
									:max-height="height"
									contain
									v-on:click="showGallery(photo)"
									:class="((photo.brandNew || photo.rotated) && !onClick) ? null : 'clickable-image'"
									v-bind="attrs"
									v-on="on"
								>
									<template v-slot:placeholder>
										<v-row
											class="fill-height ma-0 full-height"
											align="center"
											justify="center"
										>
											<v-progress-circular
												indeterminate
												color="grey"
												:size="25"
												width="2"
											></v-progress-circular>
										</v-row>
									</template>
								</v-img>
								<span v-if="hoverIcon" class="hover-icon rounded-1">
									<v-icon
										size="24"
										color="primary"
									>
										{{hoverIcon}}
									</v-icon>
								</span>
							</template>
							<span>{{tooltip}}</span>
						</v-tooltip>
					</v-row>
				</template>
				<template v-else>
					<v-row
						class="fill-height ma-0 full-height"
						align="center"
						justify="center"
					>
						<template v-if="photo.brandNew">
							<template v-if="photo.file && photo.file.name">
								{{photo.file.name}}
							</template>
							<template v-else>
								Naujas failas
							</template>
						</template>
						<template v-else>
							<v-btn
								color="blue darken-1"
								text
								v-on:click="openFile(photo)"
								outlined
								small
								class="link-button"
							>
								{{photo.name ? photo.name : "Nuoroda į failą"}}
							</v-btn>
						</template>
					</v-row>
				</template>
				<div class="actions py-1" v-if="actions">
					<template v-if="actions == 'select'">
						<v-btn
							small
							color="primary"
							v-if="onSelect"
							v-on:click="onSelect(photo)"
						>
							Pasirinkti
						</v-btn>
					</template>
					<template v-else-if="actions == 'edit'">
						<template v-if="photo.keywords == 'main'">
							<v-btn
								color="primary"
								title="Redaguoti"
								class="pa-0 mr-2"
								fab
								x-small
								v-on:click.stop="editTaskAttachment(photo)"
							>
								<v-icon dark>mdi-pencil</v-icon>
							</v-btn>
						</template>
						<template v-else>
							<v-btn
								color="primary"
								title="Pasukti kairėn"
								class="pa-0 mr-2"
								fab
								x-small
								v-on:click.stop="rotatePhoto(photo, 'cc')"
								v-if="photo.isImage && (type != 'transfer')"
							>
								<v-icon dark>mdi-rotate-left</v-icon>
							</v-btn>
							<v-btn
								color="primary"
								title="Pasukti dešinėn"
								class="pa-0 mr-2"
								fab
								x-small
								v-on:click.stop="rotatePhoto(photo, 'c')"
								v-if="photo.isImage && (type != 'transfer')"
							>
								<v-icon dark>mdi-rotate-right</v-icon>
							</v-btn>
							<v-btn
								color="success"
								title="Perkelti"
								class="pa-0 mr-2"
								fab
								x-small
								v-on:click.stop="transferPhoto(photo)"
								v-if="transferPhoto && photo.canBeTransferred"
							>
								<v-icon dark>mdi-image-move</v-icon>
							</v-btn>
						</template>
						<v-btn
							color="error"
							:title="photo.isImage ? 'Šalinti nuotrauką' : 'Šalinti'"
							class="pa-0"
							fab
							x-small
							v-on:click="removePhoto(photo)"
							v-if="type != 'transfer'"
						>
							<v-icon dark>{{photo.brandNew ? "mdi-delete" : "mdi-delete-forever"}}</v-icon>
						</v-btn>
					</template>
				</div>
				<template v-if="photo.keywords && !hideKeywords">
					<div class="keywords pa-1 ma-1">{{photo.keywords == "temp" ? "Laikinas" : (photo.keywords == "main" ? "Brėžinys/schema" : photo.keywords)}}</div>
				</template>
			</v-card>
		</template>
		<template v-if="onPhotoAdd">
			<v-card
				key="new"
				:width="width"
				:height="height"
				:class="['image-card drop-zone', asColumn ? (photos.length ? 'mt-1' : null) : 'mt-4 mr-4']"
				tile
				outlined
				tabindex="0"
			>
				<FileDropZone
					:addPhotos="addPhotos"
					:disabled="maxCountReached"
					:accept="(['tasks', 'comments'].indexOf(featureType) != -1) ? 'image/*,.pdf,.doc,.docx,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document' : null"
					:small="small"
				/>
			</v-card>
			<template v-if="(featureType == 'tasks')">
				<v-card
					key="new-main-attachment"
					:width="width"
					:height="height"
					class="mt-4 mr-4 image-card"
					tile
					outlined
					tabindex="0"
				>
					<MasterAttachment
						:addPhotos="addPhotos"
						:disabled="maxCountReached"
					/>
				</v-card>
			</template>
		</template>
	</v-fade-transition>
</template>

<script>
	import FileDropZone from "./FileDropZone";
	import MasterAttachment from "./MasterAttachment";
	import PhotoHelper from "./PhotoHelper";

	export default {
		data: function(){
			var data = {
				maxCount: this.maxPhotosCount || 5,
				maxCountReached: false,
				width: 300,
				height: 225
			};
			if (this.small) {
				var scaleFactor = 0.7;
				data.width = data.width * scaleFactor;
				data.height = data.height * scaleFactor;
			}
			return data;
		},

		props: {
			photos: Array,
			maxPhotosCount: Number,
			getKey: Function,
			onPhotoAdd: Function,
			onPhotoRemove: Function,
			onPhotoMod: Function,
			transferPhoto: Function,
			canPhotoBeEdited: Function,
			featureType: String,
			actions: String,
			onSelect: Function,
			asColumn: Boolean,
			small: Boolean,
			onClick: Function,
			hideKeywords: Boolean,
			hoverIcon: String,
			tooltip: String,
			type: String
		},

		components: {
			FileDropZone,
			MasterAttachment
		},

		mounted: function(){
			this.checkIfMaxCountReached(this.photos);
		},

		methods: {
			addPhotos: function(files){
				if (!this.canPhotoBeEdited || (this.canPhotoBeEdited && this.canPhotoBeEdited())) {
					if (!this.maxCountReached) {
						if (files && files.length) {
							var file = files[0],
								fileSize = file.size / 1024 / 1024,
								maxSize = 20;
							if (fileSize > maxSize) {
								this.$vBus.$emit("show-message", {
									type: "warning",
									message: "Failas negali viršyti " + maxSize + " MB!"
								});
							} else {
								var reader = new FileReader();
								reader.addEventListener("load", function(e){
									var photo = {
										src: e.target.result,
										brandNew: true,
										key: this.getKey(),
										file: file
									};
									if (file.type.substr(0, 6) == "image/") {
										photo.isImage = true;
										PhotoHelper.getExif(photo).then(function(e){
											PhotoHelper.getProperSrc(e).then(function(src){
												src = PhotoHelper.prependExif(src, e.exif);
												PhotoHelper.getFile(src, photo).then(function(file){
													photo.src = src; // TAIP TURĖTŲ BŪTI?.. Kitu atveju grybauja pvz. su PA030591.JPG
													photo.file = file; // !!! Naudojame ne originalų failą, o modifikuotą (sumažintą!!)
													var photos = this.photos.slice();
													photos.push(photo);
													if (this.onPhotoAdd) {
														this.onPhotoAdd();
													}
													this.checkIfMaxCountReached(photos);
													this.$emit("update:photos", photos);
												}.bind(this), function(err){
													console.error(err);
												});
											}.bind(this));
										}.bind(this));
									} else {
										var photos = this.photos.slice();
										photos.push(photo);
										if (this.onPhotoAdd) {
											this.onPhotoAdd();
										}
										this.checkIfMaxCountReached(photos);
										this.$emit("update:photos", photos);
									}
								}.bind(this), false);
								reader.readAsDataURL(file);
							}
						}
					}
				}
			},
			removePhoto: function(photo){
				if (!this.canPhotoBeEdited || (this.canPhotoBeEdited && this.canPhotoBeEdited())) {
					var photos = this.photos.slice();
					photos.splice(photos.indexOf(photo), 1);
					if (this.onPhotoRemove) {
						this.onPhotoRemove(photo);
					}
					this.checkIfMaxCountReached(photos);
					this.$emit("update:photos", photos);
				}
			},
			rotatePhoto: function(photo, code){
				if (!this.canPhotoBeEdited || (this.canPhotoBeEdited && this.canPhotoBeEdited())) {
					PhotoHelper.getExif(photo).then(function(e){
						PhotoHelper.getProperSrc(e, code).then(function(src){
							src = PhotoHelper.prependExif(src, e.exif);
							PhotoHelper.getFile(src, photo).then(function(file){
								var photos = this.photos.slice();
								photo.src = src;
								photo.file = file;
								photo.rotated = true;
								photos.splice(photos.indexOf(photo), 1, photo);
								if (!photo.brandNew) {
									if (this.onPhotoMod) {
										this.onPhotoMod(photo);
									}
								}
								this.checkIfMaxCountReached(photos);
								this.$emit("update:photos", photos);
							}.bind(this), function(err){
								console.error(err);
							});
						}.bind(this));
					}.bind(this));
				}
			},
			editTaskAttachment: function(attachment){
				if (!this.canPhotoBeEdited || (this.canPhotoBeEdited && this.canPhotoBeEdited())) {
					var activeTask = this.$store.state.activeTask;
					if (activeTask && activeTask.feature) {
						this.$vBus.$emit("task-attachment-new", {
							src: attachment.src,
							id: activeTask.feature.get("OBJECTID"), // TODO: ne'harcode'inti...
							attachmentId: attachment.id,
							attachment: attachment,
							masterAttachment: Boolean(attachment.keywords == "main"),
							task: activeTask // Reikalingas tik dešinėms nuotraukoms gauti...
						});
					}
				}
			},
			checkIfMaxCountReached: function(photos){
				if (photos) {
					if (this.maxCount && (photos.length >= this.maxCount)) {
						this.maxCountReached = true;
					} else {
						this.maxCountReached = false;
					}
				}
			},
			showGallery: function(photo){
				if (photo) {
					if (this.onClick) {
						this.onClick(photo);
					} else {
						if (!photo.brandNew && !photo.rotated) {
							window.open(photo.src, "_blank");
						}
					}
				}
			},
			openFile: function(file){
				if (file && !file.brandNew) {
					window.open(file.src, "_blank");
				}
			}
		}
	}
</script>

<style scoped>
	.image-card {
		box-sizing: content-box;
	}
	.actions {
		position: absolute;
		bottom: 0;
		background-color: rgba(255, 255, 255, 0.3);
		text-align: center;
		width: 100%;
	}
	.clickable-image.v-image {
		cursor: pointer;
	}
	.keywords {
		position: absolute;
		top: 0;
		left: 0;
		background-color: rgba(255, 255, 255, 0.3);
		border: 1px solid #dddddd;
	}
	.hover-icon {
		position: absolute;
		top: 50%;
		left: 50%;
		margin-top: -12px;
		margin-left: -12px;
		background-color: white;
		display: none;
		pointer-events: none;
	}
	.image-card:hover .hover-icon {
		display: block;
	}
</style>