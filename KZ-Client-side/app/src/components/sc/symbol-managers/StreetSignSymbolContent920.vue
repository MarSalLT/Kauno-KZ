<template>
	<div>
		<template v-if="dataReady">
			<div v-if="types && types.length">
				<div>
					<div class="mb-1">Elementų išdėstymas:</div>
					<v-radio-group
						v-model="m.type"
						class="ma-0 pa-0"
						hide-details
						row
					>
						<template v-for="(type, i) in types">
							<v-radio
								:label="type.title"
								:value="type.id"
								:key="i"
								:class="['ma-0 pa-0 mr-4']"
								:disabled="Boolean(type.id != 'blue-backgr')"
							>
							</v-radio>
						</template>
					</v-radio-group>
				</div>
			</div>
			<div v-if="m.type">
				<div v-if="arrows && arrows.length" class="mt-4">
					<label for="symbol-type" class="mb-1 d-block">Rodyklės kryptis:</label>
					<v-radio-group
						v-model="m.arrow"
						id="symbol-type"
						class="ma-0 pa-0"
						hide-details
						row
					>
						<template v-for="(arrow, i) in arrows">
							<v-radio
								:value="arrow.id"
								:key="i"
								:class="['ma-0 pa-0 mr-4']"
							>
								<template v-slot:label>
									<v-icon
										size="26"
									>
										{{arrow.id}}
									</v-icon>
								</template>
							</v-radio>
						</template>
					</v-radio-group>
				</div>
				<template v-if="['blue-backgr', 'white-backgr'].indexOf(m.type) != -1">
					<div class="mt-4">
						<label for="symbol-text" class="mb-1 d-block">Atstumo reikšmė:</label>
						<div>
							<v-text-field
								v-model="m.inputVal"
								placeholder="Pvz.: 100 m"
								id="symbol-text"
								dense
								hide-details
								class="body-2 ma-0"
								clearable
							>
							</v-text-field>
						</div>
					</div>
					<template v-if="m.type == 'blue-backgr'">
						<div v-if="zones && zones.length" class="mt-4">
							<label for="symbol-zone" class="mb-1 d-block">Zona:</label>
							<v-radio-group
								v-model="m.zone"
								id="symbol-zone"
								class="ma-0 pa-0"
								hide-details
								row
							>
								<template v-for="(zone, i) in zones">
									<v-radio
										:label="zone.title"
										:value="zone.id"
										:key="i"
										:class="['ma-0 pa-0 mr-4']"
									>
									</v-radio>
								</template>
							</v-radio-group>
						</div>
					</template>
				</template>
			</div>
			<div v-if="m.type">
				<div class="mt-7 mb-3">Sugeneruotas simbolio piešinukas:</div>
				<canvas ref="canvas" class="d-block"></canvas>
			</div>
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
				types: [{
					id: "blue-backgr",
					title: "Mėlyname fone (P, rodyklė, tekstas, zona)"
				},{
					id: "white-backgr",
					title: "Baltame fone (P, rodyklė, tekstas)"
				},{
					id: "blue-backgr-simple",
					title: "Mėlyname fone (P, rodyklė)"
				}],
				arrows: [{
					id: "mdi-arrow-up-thick"
				},{
					id: "mdi-arrow-down-thick"
				},{
					id: "mdi-arrow-left-thick"
				},{
					id: "mdi-arrow-right-thick"
				},{
					id: "mdi-arrow-left-top-bold"	
				},{
					id: "mdi-arrow-right-top-bold"
				},{
					id: "mdi-arrow-left-bottom-bold"
				},{
					id: "mdi-arrow-right-bottom-bold"
				}],
				zones: [{
					id: "red",
					title: "Raudonoji zona"
				},{
					id: "green",
					title: "Žalioji zona"
				/*
				},{
					id: "blue",
					title: "Mėlynoji zona"
				*/
				},{
					id: "yellow",
					title: "Geltonoji zona"
				}],
				m: {
					type: "blue-backgr",
					arrow: "mdi-arrow-up-thick",
					inputVal: null,
					zone: "red"
				}
			};
			if (this.data.mode == "edit") {
				if (this.data.data) {
					var initialParams = JSON.parse(this.data.data);
					if (initialParams) {
						if (initialParams.m) {
							data.m = Object.assign(data.m, initialParams.m);
						}
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
					this.m.inputVal = this.data.textValue;
					this.dataReady = true;
				} else {
					StreetSignsSymbolsManagementHelper.findFeature({
						globalId: this.data.vss
					}, this.$store).then(function(feature){
						if (feature.attributes) {
							this.m.inputVal = feature.attributes[CommonHelper.symbolTextFieldName];
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
					var ctx = canvas.getContext("2d"),
						scale = 1.2;
					if (this.m.type == "blue-backgr") {
						canvas.width = 94 * scale;
						canvas.height = 56 * scale;
						ctx.fillStyle = CommonHelper.colors.sc.blue;
						ctx.fillRect(0, 0, canvas.width, canvas.height);
						ctx.font = "bold " + (65 * scale) + "px Arial";
						ctx.textBaseline = "top";
						ctx.fillStyle = "white";
						ctx.fillText("P", 2 * scale, 1 * scale);
						ctx.fillRect(canvas.width - 44 * scale, 1 * scale, 43 * scale, 35 * scale);
						ctx.font = (6 * scale) + "px Arial";
						ctx.fillText("Bilietų automatas".toUpperCase(), 23 * scale, 39 * scale);
						if (this.m.zone && this.m.zone != "blue") {
							ctx.fillStyle = CommonHelper.colors.sc[this.m.zone];
							ctx.fillRect(23 * scale, 47 * scale, canvas.width - 24 * scale, canvas.height - 48 * scale);
							ctx.font = (6 * scale) + "px Arial";
							ctx.fillStyle = this.m.zone == "yellow" ? "black" : "white";
							ctx.textAlign = "center";
							ctx.fillText(this.getZoneText(), (47 * scale + canvas.width - 24 * scale) / 2, 49 * scale);
						}
						ctx.lineWidth = 4 * scale;
						if (this.m.inputVal) {
							ctx.fillStyle = "black";
							ctx.fillRect(canvas.width - 43 * scale, 26 * scale, 41 * scale, 9 * scale);
							ctx.fillStyle = "white";
							ctx.fillText(this.m.inputVal, canvas.width - 22 * scale, 28 * scale, 32 * scale);
						} else {
							ctx.lineWidth = 5 * scale;
						}
						ctx.strokeStyle = ctx.fillStyle = CommonHelper.colors.sc.blue;
						ctx.beginPath();
						var whiteRectCenter = {
							x: canvas.width - 22 * scale,
							y: 19 * scale
						};
						if (this.m.inputVal) {
							whiteRectCenter.y -= 5 * scale;
						}
						var arrowHeadAngle = 0,
							arrowX,
							arrowY;
						switch (this.m.arrow) {
							case "mdi-arrow-up-thick":
								arrowX = whiteRectCenter.x; // Balto stačiakampio vidurys
								arrowHeadAngle = 1;
								arrowY = whiteRectCenter.y - (this.m.inputVal ? -1 : 0) * scale;
								ctx.moveTo(arrowX, arrowY);
								ctx.lineTo(arrowX, whiteRectCenter.y + (this.m.inputVal ? 9 : 12) * scale);
								break;
							case "mdi-arrow-down-thick":
								arrowX = whiteRectCenter.x;
								arrowHeadAngle = 0;
								arrowY = whiteRectCenter.y + (this.m.inputVal ? 0 : 1) * scale;
								ctx.moveTo(arrowX, arrowY);
								ctx.lineTo(arrowX, whiteRectCenter.y - (this.m.inputVal ? 9 : 12) * scale);
								break;
							case "mdi-arrow-left-thick":
								arrowX = whiteRectCenter.x - 5 * scale;
								arrowY = whiteRectCenter.y;
								arrowHeadAngle = 1.5;
								ctx.moveTo(arrowX, arrowY);
								ctx.lineTo(whiteRectCenter.x + 14 * scale, arrowY);
								break;
							case "mdi-arrow-right-thick":
								arrowX = whiteRectCenter.x + 4 * scale;
								arrowY = whiteRectCenter.y;
								arrowHeadAngle = -1.5;
								ctx.moveTo(arrowX, arrowY);
								ctx.lineTo(whiteRectCenter.x - 15 * scale, arrowY);
								break;
							case "mdi-arrow-left-top-bold":
								arrowX = whiteRectCenter.x - 2 * scale;
								arrowY = whiteRectCenter.y - 4 * scale;
								arrowHeadAngle = 1.5;
								ctx.moveTo(arrowX, arrowY);
								ctx.lineTo(whiteRectCenter.x + 10 * scale, arrowY);
								ctx.lineTo(whiteRectCenter.x + 10 * scale, arrowY + 14 * scale);
								break;
							case "mdi-arrow-right-top-bold":
								arrowX = whiteRectCenter.x + 2 * scale;
								arrowY = whiteRectCenter.y - 4 * scale;
								arrowHeadAngle = -1.5;
								ctx.moveTo(arrowX, arrowY);
								ctx.lineTo(whiteRectCenter.x - 10 * scale, arrowY);
								ctx.lineTo(whiteRectCenter.x - 10 * scale, arrowY + 14 * scale);
								break;
							case "mdi-arrow-left-bottom-bold":
								arrowX = whiteRectCenter.x - 2 * scale;
								arrowY = whiteRectCenter.y + 4 * scale;
								arrowHeadAngle = 1.5;
								ctx.moveTo(arrowX, arrowY);
								ctx.lineTo(whiteRectCenter.x + 10 * scale, arrowY);
								ctx.lineTo(whiteRectCenter.x + 10 * scale, arrowY - 14 * scale);
								break;
							case "mdi-arrow-right-bottom-bold":
								arrowX = whiteRectCenter.x + 2 * scale;
								arrowY = whiteRectCenter.y + 4 * scale;
								arrowHeadAngle = -1.5;
								ctx.moveTo(arrowX, arrowY);
								ctx.lineTo(whiteRectCenter.x - 10 * scale, arrowY);
								ctx.lineTo(whiteRectCenter.x - 10 * scale, arrowY - 14 * scale);
								break;
						}
						ctx.stroke();
						CommonHelper.drawArrowHead(
							arrowX,
							arrowY,
							arrowHeadAngle * Math.PI,
							(this.m.inputVal ? 12 : 14) * scale,
							(this.m.inputVal ? 10 : 12) * scale,
							ctx
						);
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
					m: this.m,
					dataURL: this.getDataUrl()
				};
				return data;
			},
			getZoneText: function(){
				var texts = {
					"red": "Raudonoji zona",
					"green": "Žalioji zona",
					"blue": "Mėlynoji zona",
					"yellow": "Geltonoji zona"
				}
				var text = texts[this.m.zone];
				if (text) {
					text = text.toUpperCase();
				}
				return text;
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
			m: {
				deep: true,
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
</style>