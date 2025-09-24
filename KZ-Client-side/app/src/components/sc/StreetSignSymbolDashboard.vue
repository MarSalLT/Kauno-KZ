<template>
	<div class="wrapper full-height pa-2 d-flex">
		<div :class="['flex-grow-1', symbolFromImageOnly ? 'fill-height d-flex' : null]">
			<template v-if="data">
				<template v-if="data.error">
					<div>
						<v-alert
							dense
							type="error"
							class="ma-0 body-2"
						>
							Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
						</v-alert>
					</div>
				</template>
				<template v-else>
					<div class="d-flex flex-column full-height flex-grow-1">
						<div class="flex-grow-1 content">
							<template v-if="mode == 'element-create' || mode == 'element-edit'">
								<div class="pa-4">
									<template v-if="data.type == 'arrows'">
										<StreetSignSymbolElementContentArrows
											:data="data"
											ref="dataContent"
										/>
									</template>
									<template v-else>
										{{mode}}
										{{data}}
									</template>
								</div>
							</template>
							<template v-else>
								<template v-if="symbolFromImageOnly || (contentMode == 'image')">
									<StreetSignSymbolContentFromImage
										:data="data"
										ref="dataContent"
									/>
								</template>
								<template v-else>
									<div class="pa-4 content">
										<div class="d-flex">
											<StreetSignSymbolContentGeneral
												:data="data"
												ref="dataContent"
												:class="[featurePhotosNeeded && supportFeatureObjectId ? 'mr-10' : 'flex-grow-1']"
											/>
											<FeaturePhotosCarousel
												v-if="featurePhotosNeeded && supportFeatureObjectId"
												:featureObjectId="supportFeatureObjectId"
												:asMaps="true"
												featureType="verticalStreetSignsSupports"
												class="mt-n1 photos-carousel"
											/>
										</div>
									</div>
								</template>
							</template>
						</div>
						<div class="mt-2 mr-2 mx-4 py-3 mb-n1 mode-selector" v-if="areMultipleModesAvailable()">
							<div class="d-flex align-center justify-start">
								<span class="pr-2">Simbolį kurti:</span>
								<v-radio-group
									v-model="contentMode"
									class="ma-0 pa-0"
									hide-details
									row
								>
									<template v-for="(item, i) in contentModes">
										<v-radio
											:label="item.title"
											:value="item.value"
											:key="i"
											class="ma-0 pa-0 mr-3"
										></v-radio>
									</template>
								</v-radio-group>
							</div>
						</div>
					</div>
				</template>
			</template>
		</div>
		<div class="mt-n2 mr-n2 flex-shrink-0 overflow-y">
			<div class="d-flex justify-end">
				<v-btn
					icon
					large
					v-on:click="save"
					class="mr-1"
					:loading="saveInProgress"
					v-if="!data.error"
					:disabled="!saveIsPossible"
				>
					<v-icon
						title="Išsaugoti"
						size="34"
					>
						mdi-content-save
					</v-icon>
				</v-btn>
				<v-btn
					icon
					large
					v-on:click="close"
					:disabled="saveInProgress"
				>
					<v-icon
						title="Uždaryti"
						size="34"
					>
						mdi-close
					</v-icon>
				</v-btn>
			</div>
			<div
				class="mt-1 ml-1 mr-2 pt-2 d-flex flex-column align-end preview-wrapper"
				v-if="previewImg"
				
			>
				<div class="mb-1">Išsaugota:</div>
				<div :class="[mode == 'element-edit' ? 'img-wrapper pa-1 pb-0' : null]">
					<v-img
						:src="previewImg.src + '?t=' + now"
						:width="previewImg.width"
						:height="previewImg.height"
						:contain="true"
						:key="now"
					>
						<template v-slot:placeholder>
							<v-row
								class="fill-height ma-0 full-height"
								align="center"
								justify="center"
							>
								<v-progress-circular
									indeterminate
									color="primary"
									:size="25"
									width="2"
								></v-progress-circular>
							</v-row>
						</template>
					</v-img>
				</div>
			</div>
		</div>
	</div>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import FeaturePhotosCarousel from "../FeaturePhotosCarousel";
	import StreetSignSymbolContentFromImage from "./symbol-managers/StreetSignSymbolContentFromImage";
	import StreetSignSymbolContentGeneral from "./symbol-managers/StreetSignSymbolContentGeneral";
	import StreetSignSymbolElementContentArrows from "./symbol-element-managers/StreetSignSymbolElementContentArrows";
	import StreetSignsSymbolsManagementHelper from "../helpers/StreetSignsSymbolsManagementHelper";

	export default {
		data: function(){
			var onClosePath;
			if (this.mode) {
				if (this.mode == "create") {
					onClosePath = "/sc/create";
				} else if (this.mode == "edit") {
					onClosePath = "/sc/gallery";
				} else if (this.mode == "element-create") {
					onClosePath = "/sc/element-create";
				} else if (this.mode == "element-edit") {
					onClosePath = "/sc/elements-gallery";
				}
			}
			var supportFeatureObjectId;
			if (this.data.vsss) {
				supportFeatureObjectId = this.data.vsss;
			}
			var symbolFromImageOnly = (CommonHelper.scUniqueContent.indexOf(this.data.type) == -1),
				saveIsPossible = true,
				contentMode = "controls";
			if (this.mode == "edit") {
				try {
					var initialParams = JSON.parse(this.data.data);
					if (symbolFromImageOnly) {
						if (initialParams) {
							if (!initialParams.supportFeatureObjectId) {
								saveIsPossible = false;
							}
						} else {
							saveIsPossible = false;
						}
					} else {
						if (initialParams && initialParams.fromImage) {
							contentMode = "image";
						}
					}
				} catch {
					// ...
				}
			}
			var data = {
				onClosePath: onClosePath,
				saveInProgress: false,
				previewImg: null,
				now: Date.now(),
				supportFeatureObjectId: supportFeatureObjectId,
				symbolFromImageOnly: symbolFromImageOnly,
				saveIsPossible: saveIsPossible,
				contentMode: contentMode,
				contentModes: [{
					title: "Pasirenkant simbolio komponentus",
					value: "controls"
				},{
					title: "Iškerpant nuotraukoje",
					value: "image"
				}],
				featurePhotosNeeded: true
			};
			if (CommonHelper.sc5XXContent) {
				if (this.$store.state.sc5XXInteractive && (CommonHelper.sc5XXContent.indexOf(this.data.type) != -1)) {
					data.featurePhotosNeeded = false;
				}
			}
			if (this.data.id) {
				var imageDimensions = StreetSignsSymbolsManagementHelper.getImageDimensions(
					this.data,
					this.mode == "element-edit" ? "elements" : null,
					this.mode == "element-edit" ? null : 35
				);
				data.previewImg = {
					width: imageDimensions.width,
					height: imageDimensions.height
				};
				if (this.mode == "element-edit") {
					data.previewImg.src = CommonHelper.getUniqueSymbolElementSrc(this.data.id);
				} else if (this.mode == "edit") {
					data.previewImg.src = CommonHelper.getUniqueSymbolSrc(this.data.id);
				}
			}
			return data;
		},

		props: {
			data: Object,
			mode: String,
			onClose: Function,
			onSave: Function
		},

		components: {
			FeaturePhotosCarousel,
			StreetSignSymbolContentFromImage,
			StreetSignSymbolContentGeneral,
			StreetSignSymbolElementContentArrows
		},

		methods: {
			close: function(){
				if (this.onClose) {
					this.onClose();
				} else {
					this.$router.push({
						path: this.onClosePath
					});
				}
			},
			save: function(){
				if (this.$refs.dataContent) {
					if (this.$refs.dataContent.getAllData) {
						var data = this.$refs.dataContent.getAllData();
						if (data) {
							var category = "symbols";
							if (this.mode == "element-create" || this.mode == "element-edit") {
								category = "elements";
							}
							if (data.dataURL && (data.dataURL != "data:,")) {
								var params = {
									category: category,
									type: this.data.type
								}
								if (this.data.id) {
									params.id = this.data.id;
								}
								params.dataURL = data.dataURL;
								delete data.dataURL;
								if (data.subtype) {
									params.subtype = data.subtype;
									delete data.subtype;
								}
								params.data = JSON.stringify(data);
								this.saveInProgress = true;
								StreetSignsSymbolsManagementHelper.saveData(params).then(function(response){
									this.saveInProgress = false;
									var routerParams = {
										path: "/sc/" + (category == "elements" ? "element-" : "") + "edit",
										query: {
											id: response.id
										}
									};
									this.$vBus.$emit("sc-gallery-item-updated", category);
									if (this.onSave) {
										if (process.env.VUE_APP_SC_TYPE != "test") {
											response.id = "{" + response.id.toUpperCase() + "}";
										}
										this.onSave(response.id);
									} else {
										this.$router.push(routerParams).catch(function(){
											this.$vBus.$emit("route-to", routerParams);
										}.bind(this));
									}
								}.bind(this), function(){
									this.saveInProgress = false;
									this.$vBus.$emit("show-message", {
										type: "warning",
										message: "Atsiprašome, įvyko nenumatyta klaida... Duomenys nebuvo išsaugoti"
									});
								}.bind(this));
							} else {
								this.$vBus.$emit("show-message", {
									type: "warning",
									message: "Netinkamas piešinukas!"
								});
							}
						} else {
							this.$vBus.$emit("show-message", {
								type: "warning"
							});
						}
					} else {
						this.$vBus.$emit("show-message", {
							type: "warning"
						});
					}
				} else {
					this.$vBus.$emit("show-message", {
						type: "warning"
					});
				}
			},
			areMultipleModesAvailable: function(){
				var available = false;
				if ((this.mode == "create") && (this.data.category != "elements")) {
					if (CommonHelper.scUniqueAdvancedContent.indexOf(this.data.type) != -1) {
						available = true;
					} else {
						// available = true; // Justinui prireikė galimybės visiems :)
					}
				}
				return available;
			}
		}
	};
</script>

<style scoped>
	.wrapper {
		border: 1px solid #cecece;
		background-color: #f4f4f4;
		position: relative;
		overflow-x: auto;
	}
	.content {
		overflow: auto;
	}
	.preview-wrapper {
		border-top: 1px dashed #cccccc;
	}
	.img-wrapper {
		background-color: #444444;
	}
	.photos-carousel {
		width: 320px;
	}
	.mode-selector {
		border-top: 1px dashed #a7a7a7;
	}
</style>