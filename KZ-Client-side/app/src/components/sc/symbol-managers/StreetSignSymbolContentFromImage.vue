<template>
	<div class="d-flex flex-grow-1 full-height">
		<template v-if="selectedImage">
			<div class="flex-grow-1 image-wrapper mr-2 d-flex flex-column">
				<div class="stepper-wrapper pa-2 d-flex align-center" v-if="stepperNeeded">
					<div class="flex-grow-1">
						<v-stepper flat v-model="activeStep">
							<v-stepper-header class="justify-start stepper-header">
								<v-stepper-step
									editable
									:step="1"
									class="pa-1 mr-1"
								>
									Simbolio iškirpimas
								</v-stepper-step>
								<v-stepper-step
									editable
									:step="2"
									class="pa-1 mr-1"
								>
									Iškirpto simbolio apdorojimas
								</v-stepper-step>
							</v-stepper-header>
						</v-stepper>
					</div>
					<div v-if="!(images && images.length == 1)" class="ml-3">
						<v-btn
							color="error"
							title="Uždaryti"
							class="pa-0"
							fab
							x-small
							:width="22"
							:height="22"
							v-on:click="removeImage"
						>
							<v-icon dark>mdi-close</v-icon>
						</v-btn>
					</div>
				</div>
				<div class="flex-grow-1 pa-1 overflow-y">
					<v-tabs-items v-model="activeStep" class="fill-height overflow-y">
						<v-tab-item :value="1" class="fill-height">
							<InteractiveImage
								:photo="selectedImage"
								:onInteractiveImageLoad="onInteractiveImageLoad"
								ref="interactiveImage"
							/>
							<div class="top-start d-flex stop-event align-start">
								<v-tooltip bottom>
									<template v-slot:activator="{on, attrs}">
										<v-btn
											:width="40"
											:height="40"
											min-width="auto"
											:elevation="2"
											color="error"
											v-on:click="removeImage"
											v-bind="attrs"
											v-on="on"
											class="rounded-circle mr-2"
											v-if="!stepperNeeded"
											:disabled="Boolean(imageBoundariesFeature)"
										>
											<v-icon color="white">mdi-image-remove</v-icon>
										</v-btn>
									</template>
									<span>Pašalinti nuotrauką</span>
								</v-tooltip>
								<v-tooltip bottom>
									<template v-slot:activator="{on, attrs}">
										<v-btn
											:width="40"
											:height="40"
											min-width="auto"
											:elevation="2"
											:color="imageClipActive ? 'primary darken-1' : 'primary'"
											v-on:click="toggleImageClip"
											v-bind="attrs"
											v-on="on"
											class="rounded-circle"
											:disabled="Boolean(imageBoundariesFeature)"
										>
											<v-icon color="white">mdi-pencil</v-icon>
										</v-btn>
									</template>
									<span>Aktyvuoti simbolio ribų braižymą</span>
								</v-tooltip>
							</div>
							<div class="top-end d-flex flex-column align-end stop-event">
								<div class="flex-grow-1 overflow-y pa-1">
									<InteractiveImagePopup
										:image="selectedImage"
										:supportFeatureObjectId="supportFeatureObjectId"
										:feature="imageBoundariesFeature"
										:initialImageParams="initialImageParams"
										:onClose="unsetImageBoundariesFeature"
										v-if="imageBoundariesFeature"
										ref="dataContent"
									/>
								</div>
							</div>
						</v-tab-item>
						<v-tab-item :value="2" class="fill-height">
							<div class="pa-2">Kuriama...</div>
						</v-tab-item>
					</v-tabs-items>
				</div>
			</div>
		</template>
		<template v-else>
			<div>
				<template v-if="imageManipulationsImpossible">
					<v-alert
						dense
						type="warning"
						class="ma-0 d-inline-block"
					>
						Simbolio redagavimas negalimas...
					</v-alert>
				</template>
				<template v-else>
					<div class="d-flex">
						<template v-if="supportFeatureObjectId">
							<div class="mb-2 mr-2">
								<template v-if="images">
									<template v-if="images == 'error'">
										<v-alert
											dense
											type="error"
											class="ma-0"
										>
											Atsiprašome, įvyko nenumatyta klaida...
										</v-alert>
									</template>
									<template v-else>
										<template v-if="images.length">
											<PhotosRow
												:photos.sync="images"
												:getKey="getKey"
												actions="select"
												:onSelect="selectImage"
											>
											</PhotosRow>
										</template>
										<template v-else>
											<div>Nėra nuotraukų...</div>
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
							</div>
						</template>
						<v-card
							width="300"
							height="225"
							class="image-card drop-zone"
							tile
							outlined
							tabindex="0"
						>
							<FileDropZone
								:addPhotos="addImages"
							/>
						</v-card>
					</div>
				</template>
			</div>
		</template>
	</div>
</template>

<script>
	// Testavimui -> http://localhost:3001/admin/sc/create?type=932&vsss=4318&vss=6C96540F-47FA-4E54-BC9E-58CC1FBE6AE9

	import CommonHelper from "../../helpers/CommonHelper";
	import Feature from "ol/Feature";
	import FileDropZone from "../../feature-photos-manager/FileDropZone";
	import InteractiveImage from "../InteractiveImage";
	import InteractiveImagePopup from "../InteractiveImagePopup";
	import PhotosRow from "../../feature-photos-manager/PhotosRow";
	import Polygon from "ol/geom/Polygon";
	import VectorLayer from "ol/layer/VectorImage";
	import VectorSource from "ol/source/Vector";
	import {Draw} from "ol/interaction";

	export default {
		data: function(){
			var supportFeatureObjectId;
			if (this.data.vsss) {
				supportFeatureObjectId = this.data.vsss;
			}
			var data = {
				supportFeatureObjectId: supportFeatureObjectId,
				selectedImage: null,
				images: null,
				key: 0,
				activeStep: null,
				imageClipActive: false,
				stepperNeeded: false, // Prieš tai galvojau, kad reikia funkcionalumą skaidyti į žingsnius, bet po to apsigalvojau...
				imageBoundariesFeature: null,
				initialParams: null,
				imageManipulationsImpossible: false,
				interactiveImageLoadCounter: 0,
				initialImageParams: null
			};
			if (this.data.mode == "edit") {
				data.imageManipulationsImpossible = true;
				if (this.data.data) {
					data.initialParams = JSON.parse(this.data.data);
					if (data.initialParams) {
						if (data.initialParams.supportFeatureObjectId) {
							data.supportFeatureObjectId = data.initialParams.supportFeatureObjectId;
							data.imageManipulationsImpossible = false;
						}
						if (data.initialParams.imageClipCoordinates) {
							data.imageBoundariesFeature = new Feature({
								geometry: new Polygon([data.initialParams.imageClipCoordinates])
							});
							data.initialImageParams = data.initialParams.imageParams; // FIXME! Paduoti kažką konkrečiau...
						}
					}
				}
			}
			return data;
		},

		props: {
			data: Object
		},

		components: {
			FileDropZone,
			InteractiveImage,
			InteractiveImagePopup,
			PhotosRow
		},

		mounted: function(){
			if (this.supportFeatureObjectId) {
				CommonHelper.getPhotos({
					featureObjectId: this.supportFeatureObjectId,
					featureType: "verticalStreetSignsSupports",
					store: this.$store
				}).then(function(photos){
					photos.forEach(function(photo, i){
						photo.key = i;
					}.bind(this));
					this.images = photos;
					if (this.initialParams) {
						if (this.initialParams.imageGlobalId) {
							var selectedImage;
							photos.some(function(photo){
								if (photo.globalId == this.initialParams.imageGlobalId) {
									selectedImage = photo;
									return true;
								}
							}.bind(this));
							if (selectedImage) {
								this.selectedImage = selectedImage;
							} else {
								console.warn("Nuotrauka nebeegzistuoja? O gal persiskaičiavo `atramų` OBJECTID ir šios nuotraukos yra visai ne tos, kurių reikia?.."); // BIG TODO...
							}
						}
					} else {
						if (photos.length == 1) {
							// this.selectedImage = photos[0]; // Buvo aktualu, kai neleidome vartotojui įkelti savo nuotraukų?..
						}
					}
				}.bind(this), function(){
					this.images = "error";
				}.bind(this));
			}
		},

		methods: {
			getAllData: function(){
				var data;
				if (this.$refs.dataContent && this.$refs.dataContent.getAllData) {
					data = this.$refs.dataContent.getAllData();
				}
				return data;
			},
			addImages: function(files){
				if (files && files.length) {
					var file = files[0],
						fileSize = file.size / 1024 / 1024,
						maxSize = 10;
					if (fileSize > maxSize) {
						// ...
					} else {
						var reader = new FileReader();
						reader.addEventListener("load", function(e){
							var image = {
								src: e.target.result,
								file: file
							};
							this.selectedImage = image;
						}.bind(this), false);
						reader.readAsDataURL(file);
					}
				}
			},
			removeImage: function(){
				this.selectedImage = null;
				this.imageBoundariesFeature = null;
			},
			getKey: function(){
				return this.key;
			},
			selectImage: function(image){
				this.selectedImage = image;
			},
			toggleImageClip: function(){
				if (this.imageClipActive) {
					this.removeImageInteraction();
				} else {
					this.addImageInteraction();
				}
				this.imageClipActive = !this.imageClipActive;
			},
			addImageInteraction: function(){
				if (this.$refs.interactiveImage) {
					var map = this.$refs.interactiveImage.map;
					if (map) {
						if (!this.imageBoundariesDrawVectorLayer) {
							this.imageBoundariesDrawVectorLayer = new VectorLayer({
								source: new VectorSource(),
								zIndex: 2
							});
							map.addLayer(this.imageBoundariesDrawVectorLayer);
						}
						if (!this.drawInteraction) {
							this.drawInteraction = new Draw({
								type: "Polygon",
								source: this.imageBoundariesDrawVectorLayer.getSource(),
								stopClick: true,
								// maxPoints: 4
							});
							this.drawInteraction.on("drawend", function(e){
								var feature = e.feature;
								this.toggleImageClip();
								if (feature.getGeometry().getCoordinates()[0].length == 5) {
									this.imageBoundariesFeature = feature;
									this.initialImageParams = null;
								} else {
									this.$vBus.$emit("show-message", {
										type: "warning",
										message: "Nubrėžkite keturkampį!"
									});
								}
							}.bind(this));
							map.addInteraction(this.drawInteraction);
						}
						this.imageBoundariesFeature = null;
					}
				}
			},
			removeImageInteraction: function(){
				if (this.$refs.interactiveImage) {
					var map = this.$refs.interactiveImage.map;
					if (map) {
						if (this.imageBoundariesDrawVectorLayer) {
							map.removeLayer(this.imageBoundariesDrawVectorLayer);
							this.imageBoundariesDrawVectorLayer = null;
						}
						if (this.drawInteraction) {
							map.removeInteraction(this.drawInteraction);
							this.drawInteraction = null;
						}
					}
				}
			},
			showImageBoundariesFeature: function(feature){
				this.removeImageBoundariesFeature();
				if (this.$refs.interactiveImage) {
					var map = this.$refs.interactiveImage.map;
					if (map) {
						if (!this.imageBoundariesVectorLayer) {
							this.imageBoundariesVectorLayer = new VectorLayer({
								source: new VectorSource(),
								zIndex: 1
							});
							map.addLayer(this.imageBoundariesVectorLayer);
						}
						this.imageBoundariesVectorLayer.getSource().addFeature(feature);
						// TODO: dabar turi leisti modifikuoti kampų koordinates? T. y. stumdai kampuose esančius kažkokius taškus ir adjust'inasi stačiakampis?..
					}
				}
			},
			removeImageBoundariesFeature: function(){
				if (this.imageBoundariesVectorLayer) {
					this.imageBoundariesVectorLayer.getSource().clear(true);
					this.imageBoundariesVectorLayer = null;
				}
			},
			unsetImageBoundariesFeature: function(){
				this.imageBoundariesFeature = null;
			},
			onInteractiveImageLoad: function(){
				if (this.initialParams) {
					if (this.initialParams.imageGlobalId == this.selectedImage.globalId) {
						if (this.initialParams.imageClipCoordinates) {
							if (!this.interactiveImageLoadCounter) {
								this.imageBoundariesFeature = new Feature({
									geometry: new Polygon([this.initialParams.imageClipCoordinates])
								});
							}
						}
					}
				}
				this.interactiveImageLoadCounter += 1;
			}
		},

		watch: {
			selectedImage: {
				immediate: true,
				handler: function(selectedImage){
					if (selectedImage) {
						this.activeStep = 1;
					}
				}
			},
			imageBoundariesFeature: {
				immediate: true,
				handler: function(imageBoundariesFeature){
					if (imageBoundariesFeature) {
						this.showImageBoundariesFeature(imageBoundariesFeature);
					} else {
						this.removeImageBoundariesFeature();
					}
				}
			}
		}
	};
</script>

<style scoped>
	.image-wrapper {
		height: 100% !important;
		border: 2px dotted #cccccc;
		position: relative;
	}
	.v-stepper {
		background-color: transparent !important;
	}
	.stepper-header {
		height: auto !important;
	}
	.stepper-wrapper {
		border-bottom: 2px dotted #cccccc;
		background-color: #e1e1e1;
	}
	.top-start {
		position: absolute;
		top: 0;
		bottom: 10px;
		left: 0;
		right: 10px;
		overflow: hidden;
		padding: 8px 0 0 50px;
	}
	.top-end {
		position: absolute;
		top: 0;
		bottom: 0;
		width: 100%;
		overflow: hidden;
	}
	.actions {
		background-color: rgba(255, 255, 255, 0.3);
	}
	.v-btn--disabled {
		cursor: default;
	}
	.drop-zone {
		box-sizing: content-box;
	}
</style>