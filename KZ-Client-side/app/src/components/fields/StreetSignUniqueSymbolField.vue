<template>
	<div>
		<v-text-field
			v-model="inputVal"
			dense
			hide-details
			:outlined="outlined"
			:id="id"
			full-width
			:class="['body-2 ma-0', simple ? null : 'plain']"
			clearable
			ref="field"
			:error="error"
			:disabled="!editable"
			:readonly="readonly"
		>
			<template v-slot:append>
				<template v-if="inputVal">
					<v-btn
						icon
						height="24"
						width="24"
						v-on:click.stop="showStreetSignSymbolEditorDialog"
						:disabled="!editable"
					>
						<v-icon title="Redaguoti unikalų simbolį" small>mdi-pencil</v-icon>
					</v-btn>
				</template>
				<template v-else>
					<v-btn
						icon
						height="24"
						width="24"
						v-on:click.stop="createUniqueSymbolMagically"
						:disabled="!editable"
					>
						<v-icon title="Sukurti unikalų simbolį automatiškai!" small>mdi-auto-fix</v-icon>
					</v-btn>
				</template>
				<v-btn
					icon
					height="24"
					width="24"
					v-on:click.stop="showPicker"
					:disabled="!editable"
				>
					<v-icon title="Pasirinkti unikalų simbolį" small>mdi-cog</v-icon>
				</v-btn>
			</template>
		</v-text-field>
		<v-dialog
			v-model="dialog"
			max-width="1200"
			:scrollable="Boolean(list)"
		>
			<v-card>
				<v-card-title>
					<span>Unikalaus simbolio pasirinkimas</span>
				</v-card-title>
				<v-card-text class="pb-0" ref="content">
					<template v-if="list">
						<template v-if="list == 'error'">
							<v-alert
								dense
								type="error"
								class="ma-0 d-inline-block"
							>
								Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
							</v-alert>
						</template>
						<template v-else-if="list == 'no-type'">
							<v-alert
								dense
								type="error"
								class="ma-0 d-inline-block"
							>
								Nenurodytas KET numeris!
							</v-alert>
						</template>
						<template v-else>
							<div class="d-flex">
								<div class="mr-3">
									<template v-if="list.length">
										<StreetSignUniqueSymbolPicker
											:list="list"
											:onItemSelect="onItemSelect"
											:initialValue="inputVal"
										/>
									</template>
									<template v-else>
										Sukurtų unikalių simbolių nėra...
									</template>
									<div :class="[list.length ? 'mt-4' : 'mt-3']">
										<div v-if="inputVal" class="d-none">
											<a
												href="#"
												v-on:click.stop.prevent="showStreetSignSymbolEditorDialog(inputVal)"
												class="mr-1"
											>
												Redaguoti aktyvų unikalų simbolį
											</a>
										</div>
										<div class="d-flex align-center">
											<a
												href="#"
												v-on:click.stop.prevent="showStreetSignSymbolCreatorDialog"
												class="mr-1"
											>
												Atidaryti kelio ženklo unikalaus simbolio kūrimo aplinką
											</a>
											<v-btn
												icon
												color="primary"
												small
												v-on:click="showStreetSignSymbolCreator"
												title="Pereiti į kelio ženklo unikalaus simbolio kūrimo aplinką naujame lange"
											>
												<v-icon size="18">mdi-open-in-new</v-icon>
											</v-btn>
										</div>
									</div>
								</div>
								<div
									class="photos-carousel flex-shrink-0 mt-1"
									v-if="list.length"
								>
									<FeaturePhotosCarousel
										v-if="activeFeature && activeFeature.additionalData && activeFeature.additionalData.supports"
										:feature="activeFeature.additionalData.supports[0]"
										featureType="verticalStreetSignsSupports"
										:historicMoment="activeFeature.historicMoment"
									/>
								</div>
							</div>
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
				<v-card-actions class="mx-2 pb-5 pt-5">
					<v-btn
						color="blue darken-1"
						text
						v-on:click="getList"
						outlined
						small
						:disabled="!Boolean(list)"
						v-if="type"
					>
						<v-icon left size="18">mdi-reload</v-icon>
						Perkrauti
					</v-btn>
					<v-btn
						color="blue darken-1"
						text
						v-on:click="closePicker"
						outlined
						small
					>
						Uždaryti
					</v-btn>
				</v-card-actions>
			</v-card>
		</v-dialog>
	</div>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import FeaturePhotosCarousel from "../FeaturePhotosCarousel";
	import StreetSignUniqueSymbolPicker from "../sc/StreetSignUniqueSymbolPicker";
	import StreetSignsSymbolsManagementHelper from "../helpers/StreetSignsSymbolsManagementHelper";

	export default {
		data: function(){
			var data = {
				dialog: false,
				outlined: !this.simple,
				type: null,
				list: null
			};
			return data;
		},

		props: {
			value: String,
			id: String,
			simple: Boolean,
			error: Boolean,
			editable: Boolean,
			readonly: Boolean
		},

		computed: {
			inputVal: {
				get: function(){
					return this.value;
				},
				set: function(val){
					this.$emit("input", val);
				}
			},
			activeFeature: {
				get: function(){
					return this.$store.state.activeFeature;
				}
			}
		},

		components: {
			FeaturePhotosCarousel,
			StreetSignUniqueSymbolPicker
		},

		methods: {
			getParentFormData: function(){
				var formData;
				if (this.$parent && this.$parent.$parent && this.$parent.$parent.getFormData) { // TODO, FIXME!!! Tai tikrai gan negražus sprendimas... Iš field'o gauti jo tėvo info...
					// Kitas variantas būtų komponente `AttributesTable` koreguoti metodo streetSignFeatureNrChanged() funkcionalumą...
					formData = this.$parent.$parent.getFormData(true);
				}
				return formData;
			},
			showPicker: function(){
				var parentFormData = this.getParentFormData();
				if (parentFormData) {
					this.type = parentFormData["KET_NR"]; // FIXME! Turbūt nėra gerai čia turėti fiksuotą lauko pavadinimą...
				}
				this.getList();
				this.dialog = true;
			},
			closePicker: function(){
				this.dialog = false;
				this.$refs.field.blur(); // Nes kažkodėl fokusuojasi?..
			},
			getList: function(){
				if (this.type) {
					this.list = null;
					StreetSignsSymbolsManagementHelper.getUniqueSymbols({
						type: this.type
					}).then(function(list){
						if (process.env.VUE_APP_SC_TYPE != "test") {
							if (list) {
								list.forEach(function(item){
									item.id = "{" + item.id.toUpperCase() + "}";
								});
							}
						}
						this.list = list;
					}.bind(this), function(){
						this.list = "error";
					}.bind(this));
				} else {
					this.list = "no-type";
				}
			},
			onItemSelect: function(item){
				this.inputVal = item;
				if (this.dialog) {
					this.closePicker();
				}
			},
			getVerticalStreetSignsSupportObjectId: function(){
				var objectId;
				if (this.activeFeature && this.activeFeature.additionalData && this.activeFeature.additionalData.supports && this.activeFeature.additionalData.supports.length && this.activeFeature.additionalData.supports.length == 1) {
					var feature = this.activeFeature.additionalData.supports[0],
						layerInfo = this.$store.state.myMap.getLayerInfo("verticalStreetSignsSupports");
					if (layerInfo) {
						if (layerInfo.objectIdField) {
							objectId = CommonHelper.stripGuid(feature.get(layerInfo.objectIdField));
						}
					}
				}
				return objectId;
			},
			getVerticalStreetSignGlobalId: function(){
				var globalId;
				if (this.activeFeature) {
					globalId = CommonHelper.stripGuid(this.activeFeature.globalId);
				}
				return globalId;
			},
			showStreetSignSymbolCreator: function(){
				var url = this.$router.history.base + "/sc/create?type=" + this.type + "&vsss=" + this.getVerticalStreetSignsSupportObjectId() + "&vss=" + this.getVerticalStreetSignGlobalId();
				window.open(url, "_blank");
			},
			showStreetSignSymbolCreatorDialog: function(e){
				var parentFormData = this.getParentFormData(),
					textValue;
				if (parentFormData) {
					textValue = parentFormData[CommonHelper.symbolTextFieldName];
				}
				this.$vBus.$emit("show-street-sign-symbol-creator-dialog", {
					type: this.type + "",
					vsss: this.getVerticalStreetSignsSupportObjectId() + "",
					vss: this.getVerticalStreetSignGlobalId() + "",
					textValue: textValue,
					trySuggestingExistingSymbolByValue: e.trySuggestingExistingSymbolByValue,
					onSave: function(val){
						if (e && e.setValue) {
							this.inputVal = val;
						}
						this.getList();
					}.bind(this)
				});
			},
			createUniqueSymbolMagically: function(){
				var parentFormData = this.getParentFormData(),
					type;
				if (parentFormData) {
					if (parentFormData["KET_NR"]) {
						type = parentFormData["KET_NR"] + "";
					}
				}
				if (type) {
					if (CommonHelper.scUniqueVeryBasicContent.indexOf(type) != -1) {
						var trulyMagically = false;
						if (trulyMagically) {
							// Neišbaigta... Buvo mintis vieno mygtuko paspaudimu sukurti simbolį... BET...
							var textValue = parentFormData[CommonHelper.symbolTextFieldName];
							if (textValue) {
								// !!!
								// Atsiklausti ar tikrai kurti?
								// TODO... Turime tipą... Galime atsekti jo turinio generatorių?... Galima būtų padaryti kažką panašaus kaip 5XX rodyklės kūrime (kai tik pasikrauna simbolis ir iš aprašomosios info sugeneruojamas rodyklės piešinukas)?..
								console.log("createUniqueSymbolMagically", type, textValue); // TEMP...
								this.$vBus.$emit("show-message", {
									type: "warning",
									message: "Funkcionalumas kuriamas..."
								});
							} else {
								this.$vBus.$emit("show-message", {
									type: "warning",
									message: "Neįvestas joks tekstas ant ženklo!"
								});
							}
						} else {
							this.type = type;
							this.showStreetSignSymbolCreatorDialog({
								setValue: true,
								trySuggestingExistingSymbolByValue: true
							});
						}
					} else {
						this.type = type;
						this.showStreetSignSymbolCreatorDialog({
							setValue: true
						});
					}
				} else {
					this.$vBus.$emit("show-message", {
						type: "warning",
						message: "Nenurodytas KET numeris!"
					});
				}
			},
			showStreetSignSymbolEditorDialog: function(){
				this.$vBus.$emit("show-street-sign-symbol-creator-dialog", {
					id: this.value,
					mode: "edit",
					vsss: this.getVerticalStreetSignsSupportObjectId() + "",
					vss: this.getVerticalStreetSignGlobalId() + "",
					onSave: function(){
						this.tryRefreshingFeatureSymbol();
					}.bind(this)
				});
			},
			tryRefreshingFeatureSymbol: function(){
				if (this.activeFeature && this.activeFeature.feature) {
					var origFeature = null,
						serviceId = this.activeFeature.type == "v" ? "street-signs-vertical" : "street-signs",
						layerId = parseInt(this.activeFeature.layerId);
					this.$store.state.myMap.map.getLayers().forEach(function(layer){
						if (layer.service) {
							if (layer.service.id == serviceId && layer.getLayers) {
								layer.getLayers().forEach(function(l){
									if (layerId === l.layerId) {
										if (l.objectIdField) {
											origFeature = l.getSource().getFeatureById(this.activeFeature.feature.get(l.objectIdField));
										}
									}
								}.bind(this));
							}
						}
					}.bind(this));
					if (origFeature) {
						origFeature.set("unique-symbol-timestamp", Date.now()); // TODO... Hmmm... Gal prie didesnio vieneto pridėti? Sluoksnio?.. Nes jei refresh'insime sluoksnį, tai tooltip'e esantis piešinukas jau senas bus?
						origFeature.changed(); // Gal padės?..
						console.log("origFeature changed", Date.now()); // TEMP...
					}
				}
				this.$vBus.$emit("unique-symbol-timestamp-changed");
			}
		},

		watch: {
			dialog: {
				immediate: false,
				handler: function(open){
					if (open) {
						if (this.$refs.content) {
							setTimeout(function(){
								this.$refs.content.scrollTop = 0;
							}.bind(this), 0);
						}
					}
				}
			}
		}
	}
</script>

<style scoped>
	.photos-carousel {
		width: 200px;
	}
</style>