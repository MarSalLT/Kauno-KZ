<template>
	<div class="popup d-flex flex-column overflow-y">
		<div class="header d-flex justify-end pa-1" v-if="onClose">
			<div>
				<v-btn
					icon
					v-on:click="onClose"
					class="ml-1"
					small
					v-if="onClose"
				>
					<v-icon
						title="Uždaryti"
					>
						mdi-close
					</v-icon>
				</v-btn>
			</div>
		</div>
		<div class="pa-2 overflow-y">
			<div>Simbolio plotis:</div>
			<v-slider
				v-model="imageParams.width"
				min="10"
				max="400"
				step="5"
				:thumb-size="30"
				ticks
				class="ma-0 pa-0 my-slider"
				hide-details
			></v-slider>
			<div class="mt-2">Simbolio aukštis:</div>
			<v-slider
				v-model="imageParams.height"
				min="10"
				:max="maxImageHeight"
				step="2"
				:thumb-size="30"
				ticks
				class="ma-0 pa-0 my-slider"
				hide-details
			></v-slider>
			<div class="mt-2">Simbolio ryškumas:</div>
			<v-slider
				v-model="imageBrightness"
				min="0"
				max="255"
				step="2"
				:thumb-size="30"
				ticks
				class="ma-0 pa-0 my-slider"
				hide-details
			></v-slider>
			<div class="mb-1 mt-2">Sugeneruotas simbolio piešinukas:</div>
			<div class="canvas-wrapper">
				<v-progress-circular
					indeterminate
					color="primary"
					:size="30"
					width="2"
					v-if="!canvasDataReady"
					class="progress-indicator"
				></v-progress-circular>
				<canvas ref="canvas" :class="['d-block', canvasDataReady ? null : 'd-invisible']"></canvas>
			</div>
		</div>
	</div>
</template>

<script>
	export default {
		data: function(){
			var maxImageHeight = 100,
				imageDimensions = this.getDestinationDimensions(this.getClipCoordinates());
			var data = {
				canvasDataReady: false,
				imageParams: {
					width: imageDimensions[0] * (maxImageHeight / imageDimensions[1]),
					height: maxImageHeight
				},
				imageBrightness: 100,
				img: null,
				maxImageHeight: maxImageHeight
			};
			if (this.initialImageParams) {
				data.imageParams = Object.assign({}, this.initialImageParams);
				data.imageBrightness = this.initialImageParams.brightness;
			}
			return data;
		},

		props: {
			image: Object,
			supportFeatureObjectId: String,
			feature: Object,
			initialImageParams: Object,
			onClose: Function
		},

		mounted: function(){
			// Pvz.:
			// http://evanw.github.io/glfx.js/demo/#perspective
			// https://github.com/wanadev/perspective.js/
			// Grynai ko reikia:
			// https://stackoverflow.com/questions/30565987/cropping-images-with-html5-canvas-in-a-non-rectangular-shape-and-transforming
			// http://www.recompile.in/2019/11/creating-irregular-crop-window-on-image.html
			// COOOOOL -> https://bl.ocks.org/mbostock/10571478
			this.renderCanvas();
		},

		methods: {
			renderCanvas: function(){
				// ! Algoritmas imtas iš čia -> https://stackoverflow.com/questions/30565987/cropping-images-with-html5-canvas-in-a-non-rectangular-shape-and-transforming
				if (this.$refs.canvas) {
					if (this.img) {
						this.renderCanvasWithImage();
					} else {
						this.canvasDataReady = false;
						var img = new Image();
						img.setAttribute("crossOrigin", "anonymous");
						img.onload = function(){
							this.img = img;
							this.renderCanvasWithImage();
							this.canvasDataReady = true;
						}.bind(this);
						img.src = this.image.src;
					}
				}
			},
			renderCanvasWithImage: function(){
				var img = this.img,
					clipCoordinates = this.getClipCoordinates(),
					tempSourceCanvas = document.createElement("canvas"),
					tempDestCanvas = this.$refs.tempDestCanvas || document.createElement("canvas");
				var anchors = {
					TL: {x: clipCoordinates[0][0], y: img.height - clipCoordinates[0][1]},
					TR: {x: clipCoordinates[1][0], y: img.height - clipCoordinates[1][1]},
					BR: {x: clipCoordinates[2][0], y: img.height - clipCoordinates[2][1]},
					BL: {x: clipCoordinates[3][0], y: img.height - clipCoordinates[3][1]}
				}
				var destinationDimensions = [this.imageParams.width, this.imageParams.height];
				var unwarped = {
					TL: {x: 0, y: 0},
					TR: {x: destinationDimensions[0], y: 0},
					BR: {x: destinationDimensions[0], y: destinationDimensions[1]},
					BL: {x: 0, y: destinationDimensions[1]}
				}
				tempSourceCanvas.width = tempDestCanvas.width = img.width;
				tempSourceCanvas.height = tempDestCanvas.height = img.height;
				tempDestCanvas.width = 700;
				tempSourceCanvas.getContext("2d").drawImage(img, 0, 0);
				tempDestCanvas.width = destinationDimensions[0];
				tempDestCanvas.height = destinationDimensions[1];
				this.$refs.canvas.width = destinationDimensions[0];
				this.$refs.canvas.height = destinationDimensions[1] - 2;
				this.tempDestCanvas = tempDestCanvas;
				this.unwarp(anchors, unwarped, tempSourceCanvas.getContext("2d"), tempDestCanvas.getContext("2d"), img);
				this.$refs.canvas.getContext("2d").drawImage(this.tempDestCanvas, 0 , 0, destinationDimensions[0], destinationDimensions[1] - 1, 0, - 1, destinationDimensions[0], destinationDimensions[1]);
				this.setImageData();
				this.setImageBrightness();
			},
			unwarp: function(anchors, unwarped, sourceContext, destContext, img){
				this.mapTriangle(destContext, anchors.TL, anchors.BR, anchors.BL, unwarped.TL, unwarped.BR, unwarped.BL, img);
				destContext.translate(- 1, 1);
				this.mapTriangle(destContext, anchors.TL, anchors.TR, anchors.BR, unwarped.TL, unwarped.TR, unwarped.BR, img);
			},
			mapTriangle: function(ctx, p0, p1, p2, p_0, p_1, p_2, img){
				var x0 = p_0.x, y0 = p_0.y,
					x1 = p_1.x, y1 = p_1.y,
					x2 = p_2.x, y2 = p_2.y,
					u0 = p0.x, v0 = p0.y,
					u1 = p1.x, v1 = p1.y,
					u2 = p2.x, v2 = p2.y;
				ctx.save();
				ctx.beginPath();
				ctx.moveTo(x0, y0);
				ctx.lineTo(x1, y1);
				ctx.lineTo(x2, y2);
				ctx.closePath();
				ctx.clip();
				var delta = u0 * v1 + v0 * u2 + u1 * v2 - v1 * u2 - v0 * u1 - u0 * v2,
					delta_a = x0 * v1 + v0 * x2 + x1 * v2 - v1 * x2 - v0 * x1 - x0 * v2,
					delta_b = u0 * x1 + x0 * u2 + u1 * x2 - x1 * u2 - x0 * u1 - u0 * x2,
					delta_c = u0 * v1 * x2 + v0 * x1 * u2 + x0 * u1 * v2 - x0 * v1 * u2 - v0 * u1 * x2 - u0 * x1 * v2,
					delta_d = y0 * v1 + v0 * y2 + y1 * v2 - v1 * y2 - v0 * y1 - y0 * v2,
					delta_e = u0 * y1 + y0 * u2 + u1 * y2 - y1 * u2 - y0 * u1 - u0 * y2,
					delta_f = u0 * v1 * y2 + v0 * y1 * u2 + y0 * u1 * v2 - y0 * v1 * u2 - v0 * u1 * y2 - u0 * y1 * v2;
				ctx.transform(
					delta_a / delta, delta_d / delta,
					delta_b / delta, delta_e / delta,
					delta_c / delta, delta_f / delta
				);
				ctx.drawImage(img, 0, 0);
				ctx.restore();
			},
			getClipCoordinates: function(){
				var clipCoordinates = this.feature.getGeometry().getCoordinates()[0];
				clipCoordinates = clipCoordinates.slice();
				if (clipCoordinates.length == 5) {
					clipCoordinates.pop();
				}
				clipCoordinates = this.sortCoordinates(clipCoordinates);
				return clipCoordinates;
			},
			sortCoordinates: function(coordinates){
				var topLeft, topRight, bottomRight, bottomLeft;
				coordinates.sort(function(a, b){
					if (a[0] > b[0]) {
						return 1;
					}
					if (a[0] < b[0]) {
						return -1;
					}
					return 0;
				});
				if (coordinates[0][1] > coordinates[1][1]) {
					topLeft = coordinates[0];
					bottomLeft = coordinates[1];
				} else {
					topLeft = coordinates[1];
					bottomLeft = coordinates[0];
				}
				if (coordinates[2][1] > coordinates[3][1]) {
					topRight = coordinates[2];
					bottomRight = coordinates[3];
				} else {
					topRight = coordinates[3];
					bottomRight = coordinates[2];
				}
				coordinates = [topLeft, topRight, bottomRight, bottomLeft];
				return coordinates;
			},
			getDistance: function(p1, p2){
				var distance = 0,
					dx = p2[0] - p1[0],
					dy = p2[1] - p1[1];
				distance = Math.sqrt(dx * dx + dy * dy);
				return distance;
			},
			getDestinationDimensions: function(clipCoordinates){
				var width = (this.getDistance(clipCoordinates[0], clipCoordinates[1]) + this.getDistance(clipCoordinates[2], clipCoordinates[3])) / 2,
					height = (this.getDistance(clipCoordinates[1], clipCoordinates[2]) + this.getDistance(clipCoordinates[3], clipCoordinates[0])) / 2;
				var dimensions = [width, height];
				return dimensions;
			},
			setImageData: function(){
				if (this.$refs.canvas) {
					var ctx = this.$refs.canvas.getContext("2d"),
						iD = ctx.getImageData(0, 0, this.$refs.canvas.width, this.$refs.canvas.height);
					this.imageData = iD.data;
				}
			},
			setImageBrightness: function(){
				if (this.$refs.canvas && this.imageData) {
					// https://www.qoncious.com/questions/changing-brightness-image-html5-canvas
					var ctx = this.$refs.canvas.getContext("2d"),
						iD = ctx.getImageData(0, 0, this.$refs.canvas.width, this.$refs.canvas.height),
						dA = this.imageData,
						dM = iD.data,
						brightnessMul = this.imageBrightness / 100,
						bRed,
						bGreen,
						bBlue;
					for (var i = 0; i < dA.length; i += 4) {
						var red = dA[i]; // Extract original red color [0 to 255]
						var green = dA[i + 1]; // Extract green
						var blue = dA[i + 2]; // Extract blue
						bRed = brightnessMul * red;
						bGreen = brightnessMul * green;
						bBlue = brightnessMul * blue;
						dM[i] = bRed;
						dM[i + 1] = bGreen;
						dM[i + 2] = bBlue;
					}
					ctx.putImageData(iD, 0, 0);
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
					dataURL: this.getDataUrl(),
					fromImage: true
				};
				if (this.supportFeatureObjectId && this.image && this.image.globalId) {
					data.supportFeatureObjectId = this.supportFeatureObjectId;
					data.imageGlobalId = this.image.globalId;
					data.imageClipCoordinates = this.getClipCoordinates();
					data.imageParams = Object.assign({}, this.imageParams);
					data.imageParams.brightness = this.imageBrightness;
				}
				return data;
			}
		},

		watch: {
			imageParams: {
				deep: true,
				immediate: false,
				handler: function(){
					this.renderCanvas();
				}
			},
			imageBrightness: {
				immediate: false,
				handler: function(){
					this.setImageBrightness();
				}
			}
		}
	}
</script>

<style scoped>
	.popup {
		width: 500px;
		max-height: 100%;
		background-color: white;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3) !important;
		overflow: hidden;
		overflow-y: auto;
	}
	.header {
		border-bottom: 1px solid #cccccc;
	}
	.my-slider {
		max-width: 300px;
	}
	canvas {
		max-width: 100%;
	}
	.canvas-wrapper {
		position: relative;
	}
	.progress-indicator {
		position: absolute;
	}
	.d-invisible {
		visibility: hidden;
	}
</style>