<template>
	<v-card v-if="e" :outlined="Boolean(e.inline)">
		<v-card-title v-if="!e.inline">
			<span>{{e.featureType == "tasks" ? "Objekto failų valdymas" : "Objekto nuotraukų valdymas"}}</span>
		</v-card-title>
		<v-card-text :class="e.inline ? 'pa-2' : 'pb-0 pt-1'">
			<template v-if="photos">
				<template v-if="photos == 'error'">
					<v-alert
						dense
						type="error"
						class="ma-0 d-inline-block"
					>
						Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
					</v-alert>
				</template>
				<template v-else>
					<PhotosRow
						:photos.sync="photos"
						:maxPhotosCount="e.featureType == 'tasks' ? 20 : null"
						actions="edit"
						:getKey="getKey"
						:onPhotoAdd="onPhotoAdd"
						:onPhotoRemove="onPhotoRemove"
						:onPhotoMod="onPhotoMod"
						:canPhotoBeEdited="canPhotoBeEdited"
						:featureType="e.featureType"
						:small="e.small"
					/>
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
		<v-card-actions
			:class="e.inline ? 'ml-2 mr-2 mb-2 pa-0' : 'mx-2 pb-5 pt-5'"
			v-if="onClose || (photos && (photos != 'error') && !e.noSave)"
		>
			<v-btn
				color="blue darken-1"
				text
				v-on:click="save()"
				outlined
				small
				:loading="saveInProgress"
				:disabled="!(photosCase && (photosCase.add || photosCase.remove || photosCase.mod))"
				v-if="!e.noSave"
			>
				Išsaugoti
			</v-btn>
			<v-btn
				color="blue darken-1"
				text
				v-on:click="onClose"
				outlined
				small
				v-if="onClose"
			>
				Uždaryti
			</v-btn>
		</v-card-actions>
	</v-card>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";
	import PhotosHelper from "./feature-photos-manager/PhotosHelper";
	import PhotosRow from "./feature-photos-manager/PhotosRow";
	import TaskHelper from "./helpers/TaskHelper";

	export default {
		data: function(){
			var data = {
				photos: null,
				saveInProgress: null,
				photosCase: null,
				key: 0,
				date: Date.now()
			};
			return data;
		},

		props: {
			e: Object,
			onClose: Function,
			onPhotosCase: Function
		},

		components: {
			PhotosRow
		},

		mounted: function(){
			this.getData();
		},

		created: function(){
			if (this.e && this.e.inline) {
				this.$vBus.$on("refresh-feature-photos-manager-dialog", this.getData);
			}
		},

		beforeDestroy: function(){
			if (this.e && this.e.inline) {
				this.$vBus.$off("refresh-feature-photos-manager-dialog", this.getData);
			}
		},

		methods: {
			getData: function(){
				this.photos = null;
				this.saveInProgress = false;
				this.photosCase = {
					add: 0,
					remove: 0,
					photosToRemove: [],
					mod: 0
				};
				if (this.e) {
					if (this.e.feature && this.e.featureType) {
						CommonHelper.getPhotos({
							feature: this.e.feature,
							featureType: this.e.featureType,
							store: this.$store
						}).then(function(photos){
							photos.forEach(function(photo){
								photo.key = this.getKey();
							}.bind(this));
							this.photos = photos;
						}.bind(this), function(){
							this.photos = "error";
						}.bind(this));
					} else if (this.e.customUpload) {
						this.photos = []; // Tokio atvejo prireikė užduoties komentarams...
					} else {
						this.photos = "error";
					}
				} else {
					this.photos = "error";
				}
			},
			save: function(customPhotosTaskCallback){
				if (this.e.feature && this.e.featureType) {
					var layerInfo = this.$store.state.myMap.getLayerInfo(this.e.featureType) || {},
						objectIdField = layerInfo.objectIdField || "OBJECTID";
					var photosTaskCallback = function(status, errors){
						this.photosTaskCallback(status, errors);
						if (customPhotosTaskCallback) {
							customPhotosTaskCallback(status, errors);
						}
					}.bind(this);
					PhotosHelper.tryExecutingPhotosTask(
						this.e.feature.get(objectIdField),
						this.e.featureType,
						this.photos,
						this.photosCase,
						photosTaskCallback,
						this.$vBus
					);
				}
			},
			getKey: function(){
				this.key += 1;
				return this.date + "-" + this.key;
			},
			onPhotoAdd: function(){
				this.photosCase.add += 1;
			},
			onPhotoRemove: function(photo){
				if (photo.brandNew) {
					this.photosCase.add -= 1;
				} else {
					this.photosCase.remove += 1;
					this.photosCase.photosToRemove.push(photo);
				}
			},
			onPhotoMod: function(){
				this.photosCase.mod += 1;
			},
			canPhotoBeEdited: function(){
				return !this.saveInProgress;
			},
			photosTaskCallback: function(status, errors){
				if (status == "start") {
					this.saveInProgress = true;
				} else if (status == "end") {
					this.saveInProgress = false;
					if (errors.length) {
						this.getData(); // Perkrauname nuotraukas... Kad vartotojas matytų kas išsisaugojo, o kas ne...
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Išsaugant nuotraukas buvo klaidų..."
						});
					} else {
						this.$vBus.$emit("feature-photos-modified", this.e);
						if (this.e.featureType == "tasks") {
							this.getData(); // Užduočių atveju nėra iškart matomos galerijos, tad vartotojui reiktų vėl parodyti visą attachment'ų sąrašą... Kad vizualiai įsitikintų, kad kažkas pasikeitė...
							TaskHelper.notifyAboutTaskChangeToEinpix(this.e.feature.getProperties(), "photo_upload");
						} else {
							this.onClose();
						}
					}
				}
			},
			isAnythingToSave: function(){
				return Boolean(this.photosCase && (this.photosCase.add || this.photosCase.remove || this.photosCase.mod));
			}
		},

		watch: {
			photosCase: {
				deep: true,
				immediate: true,
				handler: function(){
					if (this.onPhotosCase) {
						this.onPhotosCase();
					}
				}
			}
		}
	}
</script>