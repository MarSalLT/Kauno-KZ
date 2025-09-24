<template>
	<div>
		<template v-if="dataReady">
			<div class="mb-3 my-title">Simbolio komponentai:</div>
			<div class="d-inline-flex float-left">
				<div class="d-inline-flex symbol-wrapper pa-1 rounded">
					<div
						class="d-inline-flex symbol-elements-wrapper"
					>
						<template v-for="(item, i) in elements">
							<template v-if="(maxElementsLength == 1) && elements[0] && elements[0].selected">
								<div
									:key="i + '-b-divider'"
									class="d-inline-block divider ml-6"
									tile
									flat
								>
									<v-avatar
										size="20"
										class="primary darken-1 divider-avatar"
									>
										<span>A</span>
									</v-avatar>
								</div>
							</template>
							<v-card
								:key="i"
								:min-width="width"
								:min-height="height"
								class="element-card d-inline-block"
								tile
								flat
							>
								<v-card-text :class="['fill-height pa-2 pb-0', item.rawElementData && item.rawElementData.subtype == 'down' ? 'pt-0' : null]">
									<v-row
										:align="item.selected ? (item.rawElementData && item.rawElementData.subtype == 'down' ? 'start' : 'end') : 'center'"
										justify="center"
										class="fill-height"
										no-gutters
									>
										<template v-if="item.selected">
											<v-img
												:src="getItemSrc(item.selected)"
												:width="item.width || 0"
												:height="item.height || 0"
												contain
												class="d-inline-block"
											>
												<template v-slot:placeholder>
													<v-row
														class="fill-height"
														align="center"
														justify="center"
														no-gutters
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
										</template>
										<template v-else>
											<v-btn
												icon
												color="primary lighten-3"
												large
												title="Pasirinkti piešinuką"
												v-on:click="selectImage(item)"
											>
												<v-icon>mdi-image-plus</v-icon>
											</v-btn>
										</template>
									</v-row>
								</v-card-text>
								<v-card-actions class="my-card-actions d-flex justify-center">
									<template v-if="item.selected">
										<v-btn
											icon
											color="success"
											small
											title="Kurti naują šio piešinuko pagrindu"
											disabled
										>
											<v-icon size="20">mdi-square-edit-outline</v-icon>
										</v-btn>
										<v-btn
											icon
											color="error"
											small
											title="Pašalinti piešinuką"
											v-on:click="removeImage(item)"
										>
											<v-icon size="20">mdi-image-minus</v-icon>
										</v-btn>
									</template>
									<template v-else>
										<v-btn
											icon
											color="error"
											small
											title="Šalinti"
											v-on:click="deleteItem(item)"
											:disabled="elements.length <= 2"
										>
											<v-icon size="20">mdi-close-circle</v-icon>
										</v-btn>
									</template>
								</v-card-actions>
							</v-card>
							<template v-if="(maxElementsLength == 1) && elements[0] && elements[0].selected">
								<div
									:key="i + '-a-divider'"
									class="d-inline-block divider mr-6"
									tile
									flat
								>
									<v-avatar
										size="20"
										class="primary darken-1 divider-avatar"
									>
										<span>B</span>
									</v-avatar>
								</div>
							</template>
							<template v-else>
								<div
									v-if="i < elements.length - 1"
									:key="i + '-divider'"
									class="d-inline-block divider"
									tile
									flat
								>
									<template v-if="dividerSettingsMenu">
										<DividerSettingsMenu
											v-if="elements[i + 1] && elements[i + 1].selected"
										/>
									</template>
									<template v-else>
										<v-avatar
											size="20"
											class="primary darken-1 divider-avatar"
											v-if="elements[i + 1] && elements[i + 1].selected"
										>
											<span>{{i + 1}}</span>
										</v-avatar>
									</template>
								</div>
							</template>
						</template>
					</div>
				</div>
				<div
					class="d-inline-block pa-1 additional ml-2"
					v-if="elements.length < maxElementsLength"
				>
					<v-card
						:width="width"
						:height="height"
						class="new-element-card d-inline-block"
						tile
						flat
					>
						<v-row
							align="center"
							justify="center"
							class="fill-height"
							no-gutters
						>
							<v-btn
								icon
								large
								color="primary"
								v-on:click="addElement"
								title="Pridėti komponentą"
								:disabled="elements.length >= maxElementsLength"
							>
								<v-icon>
									mdi-plus-circle-outline
								</v-icon>
							</v-btn>
						</v-row>
					</v-card>
				</div>
			</div>
			<template v-if="elementsWithContent.length">
				<template v-if="!dividerSettingsMenu && dividers.length">
					<div class="pt-5 mb-3 my-title">Tarpinės linijos, tarpai tarp simbolio komponentų:</div>
					<template v-for="(item, i) in dividers">
						<div :key="dividersKey + '-' + i" class="d-flex align-center mt-3">
							<v-avatar
								size="20"
								class="primary darken-1 mr-2 white--text"
							>
								<span>{{item.i}}</span>
							</v-avatar>
							<div>
								<DividerSettings :data="item.data" :noSibling="item.noSibling" />
							</div>
						</div>
					</template>
				</template>
				<div class="pt-5 mb-3 my-title">Sugeneruotas simbolio piešinukas:</div>
				<v-progress-circular
					indeterminate
					color="primary"
					:size="30"
					width="2"
					v-if="!canvasDataReady"
				></v-progress-circular>
				<canvas ref="canvas" :class="[canvasDataReady ? 'd-block' : 'd-none']"></canvas>
			</template>
		</template>
		<template v-else-if="dataLoadError">
			<v-alert
				dense
				type="error"
				class="ma-0 d-inline-block"
			>
				Atsiprašome, įvyko nenumatyta klaida gaunant simbolio komponentų aprašomąją informaciją... Pabandykite vėliau...
			</v-alert>
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

<script>
	import CommonHelper from "../../helpers/CommonHelper";
	import DividerSettings from "./DividerSettings";
	import DividerSettingsMenu from "./DividerSettingsMenu";
	import StreetSignsSymbolsManagementHelper from "../../helpers/StreetSignsSymbolsManagementHelper";
	import Vue from "vue";

	export default {
		data: function(){
			var initialLength = 1,
				elements = [];
			if (this.maxElementsLength > initialLength) {
				initialLength = 2;
			}
			for (var i = 0; i < initialLength; i++) {
				elements.push({});
			}
			var data = {
				dataReady: false,
				dataLoadError: null,
				elements: elements,
				dividers: [],
				width: 120,
				height: 150,
				topPadding: 18,
				canvasDataReady: false,
				dividerSettingsMenu: false, // Tokia buvo pradinė mintis, bet atsisakiau... Ten niekas neveikia, nebaigta...
				elementsWithContent: [],
				dividersKey: 0,
				now: Date.now()
			};
			if (this.data.mode == "edit") {
				if (this.data.data) {
					var initialParams = JSON.parse(this.data.data);
					if (initialParams) {
						if (initialParams.elements) {
							data.elements = initialParams.elements;
						}
					}
				}
			}
			data.elements.forEach(function(element){
				this.modItem(element);
			}.bind(this));
			return data;
		},

		props: {
			data: Object,
			maxElementsLength: Number
		},

		mounted: function(){
			if (this.data.mode == "edit") {
				StreetSignsSymbolsManagementHelper.getUniqueSymbolsElements().then(function(items){
					var itemsDict = {};
					items.forEach(function(item){
						itemsDict[item.id] = item;
					});
					this.elements.forEach(function(element){
						if (element.selected) {
							element.rawElementData = itemsDict[element.selected];
						}
					});
					this.dataLoadError = true;
					this.dataReady = true;
					setTimeout(function(){
						this.renderCanvas();
					}.bind(this), 0);
				}.bind(this), function(){
					this.dataLoadError = true;
				}.bind(this));
			} else {
				this.dataReady = true;
			}
		},

		components: {
			DividerSettings,
			DividerSettingsMenu
		},

		methods: {
			addElement: function(){
				var elements = this.elements.slice();
				elements.push({});
				this.elements = elements;
			},
			selectImage: function(item){
				this.$vBus.$emit("show-symbol-element-item-selector", {
					type: this.data.type,
					item: item,
					modItem: this.modItem
				});
			},
			removeImage: function(item){
				delete item.image;
				Vue.set(item, "selected", null);
				Vue.set(item, "width", null);
				Vue.set(item, "height", null);
				delete item.prevDividerParams; // Kadangi divider'į pašalinome, panaikiname ir už'cache'uotą išsaugotą info... Nes gi įkėlus naują piešinuką bus nauja pradžia!
				Vue.set(item, "divider", null);
				Vue.set(item, "dividers", null);
				delete item.rawElementData; // Analogiška situacija, kaip ir su divider'iu...
				this.dividersKey += 1; // Be šito lyg ir būtų šakės... Tarkime, turime tris elementus su piešinukais... Ir tada pašaliname antro elemento piešinuką... Matome, kad tinkamai neatsinaujina divider'ių control'ai...
			},
			deleteItem: function(item){
				var index = this.elements.indexOf(item);
				this.elements.splice(index, 1);
			},
			getItemSrc: function(id){
				var src;
				if (id) {
					src = CommonHelper.getUniqueSymbolElementSrc(id, this.now);
				}
				return src;
			},
			renderCanvas: function(){
				this.canvasDataReady = false;
				var canvas = this.$refs.canvas;
				if (canvas) {
					var elements = this.elements;
					if (elements) {
						var imagesCount = 0,
							imagesLoadedCount = 0;
						elements.forEach(function(element){
							if (element.selected) {
								imagesCount += 1;
							}
							if (element.image) {
								imagesLoadedCount += 1;
							}
						}.bind(this));
						if (imagesCount) {
							if (imagesLoadedCount == imagesCount) {
								this.renderCanvasWithImages()
							} else {
								elements.forEach(function(element){
									if (element.selected) {
										var image = element.image;
										if (!image) {
											image = new Image();
											image.crossOrigin = "Anonymous"; // To nenaudojant būna klaida canvas.toDataURL() atveju -> "DOMException: Failed to execute 'toDataURL' on 'HTMLCanvasElement': Tainted canvases may not be exported."
											image.onload = function(){
												imagesLoadedCount += 1;
												if (imagesLoadedCount == imagesCount) {
													this.renderCanvasWithImages();
												}
											}.bind(this);
											image.onerror = function(){
												imagesLoadedCount += 1;
												if (imagesLoadedCount == imagesCount) {
													this.renderCanvasWithImages();
												}
											}.bind(this);
											image.src = this.getItemSrc(element.selected);
											element.image = image;
										}
									}
								}.bind(this));
							}
						} else {
							this.renderCanvasWithImages();
						}
					}
				}
			},
			renderCanvasWithImages: function(){
				// FIXME! Dėl padding'ų funkcionalumą dar reiktų peržiūrėti...
				this.canvasDataReady = true;
				var image,
					totalImagesWidth = 0,
					imagesCount = 0,
					paddings = {},
					dividerData,
					sidePaddings = 0,
					topPadding = this.topPadding,
					lastIWithImage,
					dividersData = {},
					item;
				this.elements.forEach(function(item, i){
					paddings[i] = {};
					dividerData = this.getDividerData(i);
					if (dividerData && dividerData.params) {
						if (paddings[lastIWithImage]) {
							paddings[lastIWithImage].rightSidePadding = dividerData.params.leftDistance || 0;
						}
						paddings[i].leftSidePadding = dividerData.params.rightDistance || 0;
						dividersData[i] = dividerData;
					}
					image = item.image;
					if (image) {
						totalImagesWidth += image.width;
						imagesCount += 1;
						lastIWithImage = i;
					}
				}.bind(this));
				if (paddings[0]) {
					paddings[0].leftSidePadding = topPadding; // Pats pirmas padding'as...
				}
				if (paddings[lastIWithImage]) {
					paddings[lastIWithImage].rightSidePadding = topPadding; // Pats paskutinis padding'as...
					if (this.elements[lastIWithImage] && this.elements[lastIWithImage].selected == "522-arrow") {
						paddings[lastIWithImage].rightSidePadding = 0;
					}
				}
				this.elements.forEach(function(item, i){
					if (item.image) {
						sidePaddings += paddings[i].leftSidePadding;
						sidePaddings += paddings[i].rightSidePadding;
					}
				}.bind(this));
				var canvas = this.$refs.canvas;
				if (imagesCount) {
					var ctx = canvas.getContext("2d"),
						scale = 0.4 || 0.5,
						strokeWidthScaled = 6 * scale,
						dividerWidth = 4;
					if (this.maxElementsLength == 1) {
						item = this.elements[0];
						var canvasWidth = totalImagesWidth * scale + 2 * strokeWidthScaled;
						if (item) {
							if (item.dividers) {
								item.dividers.forEach(function(divider){
									canvasWidth += (divider.params.leftDistance + dividerWidth + divider.params.rightDistance) * scale;
								});
							}
						}
						canvas.width = canvasWidth;
					} else {
						canvas.width = (totalImagesWidth + sidePaddings + (imagesCount - 1) * dividerWidth) * scale + 2 * strokeWidthScaled;
					}
					canvas.height = (StreetSignsSymbolsManagementHelper.maxSymbolElementHeight + topPadding) * scale + 2 * strokeWidthScaled;
					ctx.lineCap = "butt";
					ctx.fillStyle = "white";
					ctx.fillRect(0, 0, canvas.width, canvas.height);
					ctx.fillStyle = CommonHelper.colors.sc.blue;
					ctx.fillRect(strokeWidthScaled, strokeWidthScaled, canvas.width - 2 * strokeWidthScaled, canvas.height - 2 * strokeWidthScaled);
					var imageX = strokeWidthScaled,
						imageY,
						firstImageExists = false;
					if (this.maxElementsLength == 1) {
						item = this.elements[0];
						image = item.image;
						if (image && item.dividers) {
							// FIXME: daugoka pasikartojančio kodo, kuris kartojasi su this.elements ciklu...
							imageX += item.dividers[0].params.leftDistance * scale;
							imageX += (dividerWidth / 2) * scale;
							this.drawDivider(canvas, scale, ctx, imageX, item.dividers[0], dividerWidth, strokeWidthScaled);
							imageX += (dividerWidth / 2) * scale;
							imageX += item.dividers[0].params.rightDistance * scale;
							if (image.width && image.height) {
								imageY = canvas.height - image.height * scale - strokeWidthScaled;
								if (item.rawElementData && (item.rawElementData.subtype == "down")) {
									imageY = strokeWidthScaled;
								}
								ctx.drawImage(image, imageX, imageY, image.width * scale, image.height * scale);
							}
							imageX += image.width * scale;
							imageX += item.dividers[1].params.leftDistance * scale;
							imageX += (dividerWidth / 2) * scale;
							this.drawDivider(canvas, scale, ctx, imageX, item.dividers[1], dividerWidth, strokeWidthScaled);
						}
					} else {
						this.elements.forEach(function(item, i){
							image = item.image;
							if (image) {
								var leftSidePadding = paddings[i].leftSidePadding,
									rightSidePadding = paddings[i].rightSidePadding;
								if (firstImageExists) {
									imageX += (dividerWidth / 2) * scale;
									dividerData = dividersData[i];
									this.drawDivider(canvas, scale, ctx, imageX, dividerData, dividerWidth, strokeWidthScaled);
									imageX += (dividerWidth / 2) * scale;
								}
								imageX += leftSidePadding * scale;
								if (image.width && image.height) {
									imageY = canvas.height - image.height * scale - strokeWidthScaled;
									if (item.rawElementData && (item.rawElementData.subtype == "down")) {
										imageY = strokeWidthScaled;
									}
									ctx.drawImage(image, imageX, imageY, image.width * scale, image.height * scale);
									if (item.rawElementData) {
										if (item.rawElementData.subtype == "up") {
											// Gauname rodyklės galvų skaičių... Ir jų viduryje brėžiame tarpines linijas...
											if (item.rawElementData.data) {
												var itemData = JSON.parse(item.rawElementData.data);
												if (itemData) {
													var dividerData = {
														params: {
															lineType: "dashed-20-perc-from-top"
														}
													};
													var upArrowsCount = 0;
													if (itemData.params && itemData.params.arrows) {
														itemData.params.arrows.forEach(function(arrow){
															if ((arrow.type == "up") && arrow.params && arrow.params.checked) {
																upArrowsCount += 1;
																if (arrow.params.rootDeltaX) {
																	dividerData.params.lineType = "dashed-20-perc";
																}
															}
														});
													}
													if (upArrowsCount > 1) {
														var d = image.width * scale / (upArrowsCount - 1) / 2;
														for (var j = 0; j < upArrowsCount - 1; j++) {
															// BIG FIXME! Dabar punktyrinė juosta nelabai tikslioje vietoje brėžiama...
															this.drawDivider(canvas, scale, ctx, imageX + d + j * 2 * d, dividerData, dividerWidth, strokeWidthScaled);
														}
													}
												}
											}
										}
									}
								}
								imageX += (image.width + rightSidePadding) * scale;
								firstImageExists = true;
							}
						}.bind(this));
					}
					ctx.strokeStyle = "black";
					ctx.lineWidth = 2 * scale;
					ctx.setLineDash([]);
					ctx.strokeRect(0, 0, canvas.width, canvas.height);
				} else {
					canvas.width = 0;
					canvas.height = 0;
				}
			},
			getDataUrl: function(){
				var canvas = this.$refs.canvas;
				if (canvas) {
					return canvas.toDataURL();
				}
			},
			getAllData: function(){
				var elements = [];
				this.elements.forEach(function(element){
					if (element.selected) {
						var dividerParams;
						if (element.dividers) {
							dividerParams = [];
							element.dividers.forEach(function(divider){
								dividerParams.push(divider.params);
							});
						} else if (element.divider) {
							dividerParams = element.divider.params;
						}
						element = {
							selected: element.selected
						};
						if (dividerParams) {
							element.prevDividerParams = dividerParams;
						}
					} else {
						element = {};
					}
					elements.push(element);
				});
				var data = {
					elements: elements,
					dataURL: this.getDataUrl()
				};
				return data;
			},
			modItem: function(item){
				if (item.selected) {
					var scale = 0.75;
					var image = new Image();
					image.onload = function(){
						// FIXME! Šitų set'ų minusas tas, kad CANVAS'as bus perpieštas dažniau nei reikia...
						Vue.set(item, "width", image.width * scale);
						Vue.set(item, "height", image.height * scale);
					}.bind(this);
					image.onerror = function(){
						// ...
					}.bind(this);
					image.src = this.getItemSrc(item.selected);
				}
				if (item.prevDividerParams) {
					if (this.maxElementsLength == 1) {
						if (item.prevDividerParams) {
							item.dividers = [];
							item.prevDividerParams.forEach(function(dividerParams){
								item.dividers.push({
									params: dividerParams
								});
							});
						}
					} else {
						item.divider = {
							params: item.prevDividerParams
						};
					}
				}
			},
			getDividerData: function(i){
				var dividerData;
				if (this.dividers) {
					this.dividers.some(function(item){
						if (item.i == i) {
							dividerData = item.data;
							return true;
						}
					});
				}
				return dividerData;
			},
			drawDivider: function(canvas, scale, ctx, imageX, dividerData, dividerWidth, strokeWidthScaled){
				if (dividerData && dividerData.params) {
					ctx.strokeStyle = "white";
					ctx.lineWidth = dividerWidth * scale;
					ctx.beginPath();
					ctx.moveTo(imageX, canvas.height - strokeWidthScaled);
					ctx.setLineDash([10 * scale, 7 * scale]);
					switch (dividerData.params.lineType) {
						case "dashed-50-perc":
						case "dashed-40-perc":
						case "dashed-30-perc":
						case "dashed-20-perc":
							var percentage = parseInt(dividerData.params.lineType.replace("dashed-", "").replace("-perc", ""));
							ctx.lineTo(imageX, canvas.height - strokeWidthScaled - ((canvas.height - 2 * strokeWidthScaled) * percentage / 100));
							ctx.stroke();
							break;
						case "dashed-20-perc-from-top":
							ctx.moveTo(imageX, strokeWidthScaled);
							ctx.lineTo(imageX, strokeWidthScaled + (canvas.height - 2 * strokeWidthScaled) * 0.2); // Apatinis linijos taškas...
							ctx.stroke();
							break;
						case "dashed":
							ctx.lineTo(imageX, strokeWidthScaled);
							ctx.stroke();
							break;
						case "continuous":
							ctx.setLineDash([]);
							ctx.lineTo(imageX, strokeWidthScaled);
							ctx.stroke();
							break;
						case "continuous-50-perc":
							ctx.setLineDash([]);
							ctx.lineTo(imageX, canvas.height - strokeWidthScaled - ((canvas.height - 2 * strokeWidthScaled) * 50 / 100));
							ctx.stroke();
							break;
					}
				}
			}
		},

		watch: {
			elements: {
				deep: true,
				immediate: true,
				handler: function(elements){
					var elementsWithContent = [],
						dividers = [];
					if (elements) {
						if (this.maxElementsLength == 1) {
							if (elements.length == 1) {
								var item = elements[0];
								if (item && item.selected) {
									elementsWithContent.push(item);
									if (!item.dividers) {
										item.dividers = [{
											params: {
												leftDistance: this.topPadding,
												rightDistance: this.topPadding,
												lineType: "dashed-40-perc"
											}
										},{
											params: {
												leftDistance: this.topPadding,
												rightDistance: this.topPadding,
												lineType: "dashed-40-perc"
											}
										}];
									}
									item.dividers.forEach(function(divider, i){
										dividers.push({
											i: i == 0 ? "A" : "B",
											data: divider,
											noSibling: i == 0 ? "left" : "right"
										});
									});
								}
							}
						} else {
							elements.forEach(function(item, i){
								if (item.selected) {
									elementsWithContent.push(item);
									if (this.maxElementsLength != 1) {
										if (item && item.selected) {
											if (i) { // Atseit divider'į gali turėti tik tas item'as, kuris nėra pirmas...
												if (!item.divider) {
													// Čia nustatome default'ines divider'io savybes...
													item.divider = {
														params: {
															leftDistance: this.topPadding,
															rightDistance: this.topPadding,
															lineType: "dashed-40-perc"
														}
													};
												}
												dividers.push({
													i: i,
													data: item.divider
												});
											}
										}
									}
								}
							}.bind(this));
						}
					}
					this.elementsWithContent = elementsWithContent;
					this.dividers = dividers;
					// this.renderCanvas(); // Nebekviečiame čia, nes perduodame tą veiksmą `dividers` watcher'iui...
				}
			},
			dividers: {
				deep: true,
				immediate: false,
				handler: function(){
					this.renderCanvas();
				}
			}
		}
	};
</script>

<style scoped>
	.symbol-elements-wrapper, .element-card {
		background-color: #0b4c9d;
	}
	.new-element-card {
		background-color: transparent;
	}
	.symbol-wrapper {
		border: 1px solid #111111;
	}
	.additional {
		border: 1px dashed grey;
	}
	.divider {
		/*
		border-left: 3px dashed white;
		height: 120px;
		margin-top: 80px;
		*/
		border-left: 3px solid #1e5598;
		position: relative;
	}
	.my-card-actions {
		position: absolute;
		top: -1px;
		right: -1px;
	}
	.my-title {
		clear: both;
	}
	.v-image {
		flex: none !important; /* To reikia IE? Esmė tame, kad komponente norime rodyti piešinuką 1:1 dydžio, bet nenurodome nei jo pločio, nei jo aukščio... */
	}
	.divider-avatar {
		position: absolute;
		z-index: 1;
		bottom: 5%;
		left: 50%;
		margin-left: -10px;
	}
</style>