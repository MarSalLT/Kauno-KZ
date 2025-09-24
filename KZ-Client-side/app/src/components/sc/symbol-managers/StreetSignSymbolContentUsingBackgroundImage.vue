<template>
	<div>
		<template v-if="dataReady">
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
			<v-progress-circular
				indeterminate
				color="primary"
				:size="30"
				width="2"
				v-if="!canvasDataReady"
			></v-progress-circular>
			<canvas ref="canvas" :class="[canvasDataReady ? 'd-block' : 'd-none']"></canvas>
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
	import StreetSignsSymbolsManagementHelper from "../../helpers/StreetSignsSymbolsManagementHelper";

	export default {
		data: function(){
			var data = {
				dataReady: false,
				inputVal: null,
				canvasDataReady: false
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
			data: Object
		},

		mounted: function(){
			if (this.data && this.data.vss) {
				if (this.data.textValue) {
					this.inputVal = this.modInputVal(this.data.textValue);
					this.dataReady = true;
				} else {
					StreetSignsSymbolsManagementHelper.findFeature({
						globalId: this.data.vss
					}, this.$store).then(function(feature){
						if (feature.attributes) {
							this.inputVal = this.modInputVal(feature.attributes[CommonHelper.symbolTextFieldName]);
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
					this.canvasDataReady = false;
					if (this.image) {
						this.renderCanvasWithImage();
					} else {
						var image = new Image();
						image.crossOrigin = "Anonymous";
						image.onload = function(){
							this.renderCanvasWithImage();
						}.bind(this);
						image.onerror = function(){
							this.renderCanvasWithImage();
						}.bind(this);
						image.src = require("@/assets/custom-sc-background-images/" + this.data.type + "-resized.png");
						this.image = image;
					}
				}
			},
			renderCanvasWithImage: function(){
				this.canvasDataReady = true;
				if (this.image.width && this.image.height) {
					var canvas = this.$refs.canvas,
						ctx = canvas.getContext("2d"),
						inputVal = this.inputVal,
						canvasWidth = 70;
					if (["117", "118"].indexOf(this.data.type) != -1) {
						canvasWidth = 85;
					} else if (["318", "319"].indexOf(this.data.type) != -1) {
						canvasWidth = 85;
					}
					if (!inputVal) {
						inputVal = "XX";
					}
					canvas.width = canvasWidth;
					canvas.height = (this.image.height / this.image.width) * canvasWidth;
					if (this.inputVal) {
						ctx.fillStyle = "black";
						if (this.data.type == "304" || this.data.type == "531") {
							ctx.fillStyle = "white";
						}
					} else {
						ctx.fillStyle = "#b7b7b7";
					}
					ctx.drawImage(this.image, 0, 0, canvas.width, canvas.height);
					// https://newspaint.wordpress.com/2014/05/22/writing-rotated-text-on-a-javascript-canvas/
					var textHeight,
						font;
					textHeight = 20;
					font = textHeight + "px Arial";
					ctx.font = font;
					ctx.textAlign = "center";
					if (this.data.type == "117") {
						ctx.save();
						ctx.translate(canvas.width / 2, canvas.height / 2);
						ctx.rotate(Math.PI / 7.5);
						ctx.fillText(inputVal, 3, 11, 34);
						ctx.restore;
					} else if (this.data.type == "118") {
						ctx.save();
						ctx.translate(canvas.width / 2, canvas.height / 2);
						ctx.rotate(- Math.PI / 7.5);
						ctx.fillText(inputVal, - 6, 11, 34);
						ctx.restore;
					} else if (this.data.type == "304") {
						textHeight = 14;
						font = textHeight + "px Arial";
						ctx.font = font;
						ctx.fillText(inputVal, canvas.width / 2 + 8, canvas.height / 2 - 1, 25);
					} else if (this.data.type == "318") {
						textHeight = 14;
						font = textHeight + "px Arial";
						ctx.font = font;
						ctx.fillText(inputVal, canvas.width / 2 + 1, canvas.height / 2 + 16, 22);
						ctx.fillText("m", canvas.width / 2 + 1, canvas.height / 2 + 29, 22);
					} else if (this.data.type == "319") {
						textHeight = 17;
						font = textHeight + "px Arial";
						ctx.font = font;
						ctx.fillText(inputVal, canvas.width / 2 + 4, canvas.height / 2 - 10, 35);
					} else if (this.data.type == "529") {
						textHeight = 14;
						font = textHeight + "px Arial";
						ctx.font = font;
						ctx.fillText(inputVal, canvas.width / 2 + 9, canvas.height / 2 + 32, 20);
					} else if (this.data.type == "530") {
						textHeight = 13;
						font = textHeight + "px Arial";
						ctx.font = font;
						ctx.fillText(inputVal, canvas.width / 2, canvas.height / 2 + 34, 46);
					} else if (this.data.type == "531") {
						textHeight = 12;
						font = textHeight + "px Arial";
						ctx.font = font;
						ctx.fillText(inputVal, canvas.width / 2, canvas.height / 2 + 41, 30);
					} else if (this.data.type == "546") {
						textHeight = 12;
						font = textHeight + "px Arial";
						ctx.font = font;
						ctx.fillText(inputVal, canvas.width / 2, canvas.height / 2 + 35, 25);
					}
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
			},
			modInputVal: function(value){
				if (this.data) {
					if (["117", "118"].indexOf(this.data.type) != -1) {
						if (value[value.length - 1] != "%") {
							value += "%";
						}
					} else if (["319", "546"].indexOf(this.data.type) != -1) {
						if (value[value.length - 1] != "m") {
							value += "m";
						}
					}
				}
				return value;
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