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
	import CommonHelper from "../../helpers/CommonHelper";
	import StreetSignsSymbolsManagementHelper from "../../helpers/StreetSignsSymbolsManagementHelper";

	export default {
		data: function(){
			var data = {
				dataReady: false,
				inputVal: null
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
					// TODO: gal reiktų elementų koordinates skaičiuoti kažkaip reliatyviau... Nes dabar gan absoliutinės koordinatės... T. y., vienur pakeiti kažką, tai reikia žiūrėti ar kitur nepersistūmė...
					var scale = 1,
						ctx = canvas.getContext("2d"),
						strokeWidth = 1 * scale,
						inputVal = this.inputVal;
					canvas.width = 68 * scale;
					canvas.height = 94 * scale;
					ctx.fillStyle = "black";
					ctx.fillRect(0, 0, canvas.width, canvas.height);
					ctx.fillStyle = "white";
					ctx.fillRect(strokeWidth, strokeWidth, canvas.width - 2 * strokeWidth, canvas.height - 2 * strokeWidth);
					var greyColor = "#bbbbbb";
					if (this.subtype == "540" || this.subtype == "543") {
						ctx.save();
						// -
						ctx.beginPath();
						ctx.fillStyle = this.subtype == "543" ? greyColor : CommonHelper.colors.sc.red;
						ctx.arc(canvas.width / 2, canvas.height / 2, 24 * scale, 0, 2 * Math.PI, true);
						ctx.fill();
						// -
						ctx.beginPath();
						ctx.fillStyle = "white";
						ctx.arc(canvas.width / 2, canvas.height / 2, 22 * scale, 0, 2 * Math.PI, true);
						ctx.fill();
						// -
						ctx.beginPath();
						ctx.fillStyle = this.subtype == "543" ? greyColor : CommonHelper.colors.sc.blue;
						ctx.arc(canvas.width / 2, canvas.height / 2, 21 * scale, 0, 2 * Math.PI, true);
						ctx.fill();
						// -
						ctx.clip();
						// -
						ctx.lineWidth = 4 * scale;
						ctx.beginPath();
						ctx.strokeStyle = "white";
						ctx.moveTo(0, 0);
						ctx.lineTo(canvas.width, canvas.height);
						ctx.stroke();
						// -
						ctx.lineWidth = 2 * scale;
						ctx.beginPath();
						ctx.strokeStyle = this.subtype == "543" ? greyColor : CommonHelper.colors.sc.red;
						ctx.moveTo(0, 0);
						ctx.lineTo(canvas.width, canvas.height);
						ctx.stroke();
						// -
						ctx.restore();
					} else if (this.subtype == "541") {
						ctx.save();
						// -
						var rectDim = 44 * scale;
						ctx.rect(canvas.width / 2 - rectDim / 2 , canvas.height / 2 - rectDim / 2, rectDim, rectDim);
						ctx.fillStyle = CommonHelper.colors.sc.blue;
						ctx.fill();
						ctx.font = "bold " + (40 * scale) + "px Arial";
						ctx.fillStyle = "white";
						ctx.textAlign = "center";
						ctx.textBaseline = "middle";
						ctx.fillText("P", canvas.width / 2, canvas.height / 2 + 3 * scale, canvas.width);
						// -
						ctx.restore();
					} else {
						ctx.beginPath();
						ctx.strokeStyle = this.subtype == "545" ? CommonHelper.colors.sc.grey : CommonHelper.colors.sc.red;
						ctx.lineWidth = 2.5 * scale;
						ctx.arc(canvas.width / 2, canvas.height / 2 + 2 * scale, 24 * scale, 0, 2 * Math.PI, true);
						ctx.stroke();
					}
					ctx.font = "bold " + (11 * scale) + "px Arial";
					ctx.fillStyle = "black";
					ctx.textAlign = "center";
					ctx.textBaseline = "middle";
					ctx.fillText("ZONA", canvas.width / 2, 13 * scale, canvas.width);
					if (this.subtype == "540" || this.subtype == "543" || this.subtype == "541") {
						if (!inputVal) {
							if (this.subtype != "543") {
								inputVal = "XX";
								ctx.fillStyle = "#b7b7b7";
							}
						}
						if (inputVal) {
							ctx.fillText(inputVal, canvas.width / 2, 83 * scale, canvas.width - 10 * scale);
						}
					} else {
						ctx.font = (26 * scale) + "px Arial";
						if (this.subtype == "545") {
							ctx.fillStyle = CommonHelper.colors.sc.grey;
						}
						if (!inputVal) {
							inputVal = "XX";
							ctx.fillStyle = "#b7b7b7";
						}
						ctx.fillText(inputVal, canvas.width / 2, canvas.height / 2 + 4 * scale, 36 * scale);
					}
					if (this.subtype == "543" || this.subtype == "545") {
						ctx.strokeStyle = "#444444";
						ctx.lineWidth = 1.5 * scale;
						var offsets = [0, 10 * scale, 5 * scale, - 5 * scale, - 10 * scale]; // 10 * scale, 5 * scale, - 5 * scale, - 10 * scale
						offsets.forEach(function(offset){
							ctx.beginPath();
							ctx.moveTo(canvas.width, 0 - offset + 10);
							ctx.lineTo(0 - 10, canvas.height - offset + 0);
							ctx.stroke();
						});
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