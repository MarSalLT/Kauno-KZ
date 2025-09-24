<template>
	<div>
		<template v-if="dataReady">
			<v-alert
				dense
				type="error"
				class="ma-0 mb-7"
				v-if="subtype == '529'"
			>
				Atsiprašome. Funkcionalumas kuriamas...
			</v-alert>
			<div>
				<label for="symbol-text" class="mb-1 d-block">Tekstas simbolio viduje:</label>
				<div>
					<v-text-field
						v-model="inputVal"
						id="symbol-text"
						dense
						hide-details
						class="body-2 ma-0"
						clearable
					>
					</v-text-field>
				</div>
			</div>
			<div class="mt-7 mb-3">Sugeneruotas simbolio piešinukas:</div>
			<canvas ref="canvas"></canvas>
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
	// !!! Šis komponentas buvo vienas iš pirmųjų, bet po to nutariau jo nebenaudoti, nes elegantiškiau pasirodė naudoti tiesiog piešinuką...

	import CommonHelper from "../../helpers/CommonHelper";
	import StreetSignsSymbolsManagementHelper from "../../helpers/StreetSignsSymbolsManagementHelper";

	export default {
		data: function(){
			var data = {
				dataReady: false,
				inputVal: null,
				test: false
			};
			if (this.data.mode == "edit") {
				if (this.data.data) {
					var initialParams = JSON.parse(this.data.data);
					if (initialParams) {
						data.inputVal = initialParams.val;
					}
				}
			}
			return data;
		},

		props: {
			data: Object,
			subtype: String
		},

		mounted: function(){
			if (this.data && this.data.vss) {
				if (this.data.textValue) {
					this.inputVal = this.data.textValue;
					this.dataReady = true;
				} else {
					StreetSignsSymbolsManagementHelper.findFeature({
						globalId: this.data.vss
					}, this.$store).then(function(feature){
						if (feature.attributes) {
							this.inputVal = feature.attributes[CommonHelper.symbolTextFieldName];
						}
						this.dataReady = true;
					}.bind(this), function(){
						// TODO, FIXME! Pranešti, kad įvyko klaida?.. Ar nelabai net svarbu?..
						this.dataReady = true;
					}.bind(this));
				}
			} else {
				this.dataReady = true;
			}
		},

		methods: {
			renderCanvas: function(){
				var canvas = this.$refs.canvas;
				if (canvas) {
					var scale = 0.9; // Nes kitaip piešinukas fiziškai per aukštas žemėlapiui...
					var elements = [{
						items: [{
							type: "text",
							value: "P", // Testuoti su Šj
							height: 70
						}],
						bottomMargin: - 8 // To prireikė, nes "P" raidė kažkaip aukščiau... Ir nelabai pavyko paskaičiuoti tikro jos aukščio... Ir offset'inti... BIG FIXME! Reiktų padaryti normaliai, kad nereikėtų šito hardcode'inti...
					}];
					if (this.subtype == "529") {
						elements = elements.concat([{
							items: [{
								type: "text",
								value: this.inputVal || "Tekstas",
								height: 10,
								shape: {
									type: "clock",
									fillColor: "white",
									align: "right"
								},
								// textColor: "black"
							}]
						}]);
					} else if (this.subtype == "530") {
						elements = elements.concat([{
							items: [{
								type: "text",
								value: this.inputVal || "Tekstas",
								height: 12,
								shape: {
									type: "rectangle",
									fillColor: "white",
									padding: 2 * scale
								},
								textColor: "black"
							}]
						}]);
					} else if (this.subtype == "531") {
						elements = elements.concat([{
							items: [{
								type: "text",
								value: "Rezervuota".toUpperCase(),
								height: 8
							}],
							bottomMargin: 3
						},{
							items: [{
								type: "arrow",
								side: "up",
								paddingRight: 6 * scale,
								width: 4 * scale,
								height: 12 * scale,
								size: 8 * scale
							},{
								type: "text",
								value: this.inputVal || "Tekstas",
								height: 10
							},{
								type: "arrow",
								side: "up",
								paddingLeft: 6 * scale,
								width: 4 * scale,
								height: 12 * scale,
								size: 8 * scale
							}]
						}]);
					}
					var ctx = canvas.getContext("2d"),
						strokeWidth = 2 * scale,
						padding = 8 * scale,
						textInfo,
						maxGroupWidth = 0,
						canvasHeight = 0,
						groupY = strokeWidth + padding,
						groupElementsWidth,
						groupElementsHeight,
						elementY,
						elementX;
					ctx.textBaseline = "middle";
					elements.forEach(function(group){
						groupElementsWidth = 0;
						groupElementsHeight = 0;
						if (group.items) {
							group.items.forEach(function(element){
								if (element.type == "text") {
									element.scaledHeight = element.height * scale;
									element.font = element.scaledHeight + "px Arial";
									ctx.font = element.font;
									textInfo = ctx.measureText(element.value);
									element.textInfo = textInfo;
									groupElementsWidth += textInfo.width;
									element.actualHeight = element.scaledHeight;
									if (element.actualHeight > groupElementsHeight) {
										groupElementsHeight = element.actualHeight;
									}
								} else if (element.type == "arrow") {
									groupElementsWidth += (element.width || 0) + (element.paddingLeft || 0) + (element.paddingRight || 0);
									if (element.height > groupElementsHeight) {
										groupElementsHeight = element.height;
									}
								}
							});
							if (groupElementsWidth > maxGroupWidth) {
								maxGroupWidth = groupElementsWidth;
							}
							canvasHeight += groupElementsHeight;
							if (group.bottomMargin) {
								group.scaledBottomMargin = group.bottomMargin * scale;
								canvasHeight += group.scaledBottomMargin;
							}
						}
						group.groupElementsWidth = groupElementsWidth;
						group.groupElementsHeight = groupElementsHeight;
					});
					canvas.width = maxGroupWidth + (strokeWidth + padding) * 2;
					canvas.height = canvasHeight + (strokeWidth + padding) * 2;
					ctx.fillStyle = "white";
					ctx.fillRect(0, 0, canvas.width, canvas.height);
					ctx.fillStyle = CommonHelper.colors.sc.blue;
					ctx.fillRect(strokeWidth, strokeWidth, canvas.width - 2 * strokeWidth, canvas.height - 2 * strokeWidth);
					ctx.fillStyle = ctx.strokeStyle = "white";
					ctx.textBaseline = "middle";
					elements.forEach(function(group){
						if (group.items) {
							elementX = (canvas.width - group.groupElementsWidth) / 2;
							group.items.forEach(function(element){
								if (element.type == "text") {
									if (element.shape) {
										if (element.shape.type == "rectangle") {
											ctx.fillRect(elementX - element.shape.padding, groupY - element.shape.padding, element.textInfo.width + element.shape.padding * 2, element.scaledHeight + element.shape.padding * 2 - 1 * scale); // FIXME! To paskutinio hardcode'into skaičiaus neturėtų būti?.. Jį dabar naudoju dėl to, kad sunku teksto aukštį paskaičiuoti ir apačioje lieka balto ploto...
										}
									}
									if (element.textColor) {
										ctx.fillStyle = element.textColor;
									}
									ctx.textAlign = "left";
									ctx.font = element.font;
									elementY = groupY + element.actualHeight / 2;
									ctx.fillText(element.value, elementX, elementY);
									if (this.test) {
										ctx.save();
										ctx.beginPath();
										ctx.lineWidth = 1;
										ctx.setLineDash([3, 3]);
										ctx.strokeStyle = "yellow";
										ctx.moveTo(0, elementY);
										ctx.lineTo(canvas.width, elementY);
										ctx.stroke();
										ctx.restore();
									}
									elementY += element.actualHeight / 2;
									if (this.test) {
										ctx.save();
										ctx.beginPath();
										ctx.lineWidth = 2;
										ctx.strokeStyle = "green";
										ctx.moveTo(0, elementY);
										ctx.lineTo(canvas.width, elementY);
										ctx.stroke();
										ctx.restore();
									}
									elementX += element.textInfo.width;
								} else if (element.type == "arrow") {
									elementX += (element.paddingLeft || 0);
									ctx.lineWidth = element.width;
									ctx.beginPath();
									ctx.moveTo(elementX + element.width / 2, groupY + element.size);
									ctx.lineTo(elementX + element.width / 2, groupY + element.height);
									ctx.stroke();
									CommonHelper.drawArrowHead(elementX + element.width / 2, groupY + element.size, Math.PI, element.size, element.size, ctx);
									elementX += element.width;
									elementX += (element.paddingRight || 0);
								}
							}.bind(this));
						}
						groupY += group.groupElementsHeight;
						if (group.scaledBottomMargin) {
							groupY += group.scaledBottomMargin;
						}
					}.bind(this));
				}
			},
			getDataUrl: function(){
				var canvas = this.$refs.canvas;
				if (canvas) {
					return canvas.toDataURL();
				}
			},
			getAllData: function(){
				var data = {
					val: this.inputVal,
					dataURL: this.getDataUrl()
				};
				return data;
			}
		},

		watch: {
			dataReady: {
				immediate: true,
				handler: function(dataReady){
					if (dataReady) {
						setTimeout(function(){
							this.renderCanvas();
						}.bind(this), 0);
					}
				}
			},
			inputVal: {
				immediate: true,
				handler: function(){
					this.renderCanvas();
				}
			}
		}
	};
</script>

<style scoped>
	.v-text-field {
		width: 400px;
	}
	canvas {
		display: block;
	}
</style>