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
						autocomplete="off"
					>
					</v-text-field>
				</div>
			</div>
			<template v-if="belowElement == 'any-side-arrow'">
				<div class="mt-7 mb-2">Rodyklė į:</div>
				<div class="mb-n2">
					<v-radio-group
						v-model="arrows.below"
						class="ma-0 pa-0"
						hide-details
						row
					>
						<v-radio
							label="Į kairę"
							value="left"
							class="ma-0 pa-0 mr-3"
						></v-radio>
						<v-radio
							label="Į dešinę"
							value="right"
							class="ma-0 pa-0 mr-3"
						></v-radio>
						<v-radio
							label="Į abi puses"
							value="both"
							class="ma-0 pa-0 mr-3"
						></v-radio>
					</v-radio-group>
				</div>
			</template>
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
				inputVal: null,
				arrows: {
					below: null
				},
				arrowHeadWidth: 20,
				arrowHeadHeight: 16,
				overlapArrowLinesBy: 1
			};
			if (this.leftElement) {
				if (this.leftElement.substr(0, 6) == "arrow-") {
					data.arrows.left = this.leftElement.substr(6);
				}
			}
			if (this.rightElement) {
				if (this.rightElement.substr(0, 6) == "arrow-") {
					data.arrows.right = this.rightElement.substr(6);
				}
			}
			if (this.belowElement) {
				if (this.belowElement == "any-side-arrow") {
					data.arrows.below = "both";
				} else if (this.belowElement.substr(0, 6) == "arrow-") {
					data.arrows.below = this.belowElement.substr(6);
				} else {
					data.below = this.belowElement;
				}
			}
			if (this.aboveElement) {
				if (this.aboveElement.substr(0, 6) == "arrow-") {
					data.arrows.above = this.aboveElement.substr(6);
				} else {
					data.above = this.aboveElement;
				}
			}
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
			shape: String,
			textColor: String,
			outlineColor: String,
			outlineWidth: Number,
			fillColor: String,
			emptyText: String,
			leftElement: String,
			rightElement: String,
			belowElement: String,
			aboveElement: String,
			xPaddingMultiplyBy: Number,
			specificParams: Object
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
					var ctx = canvas.getContext("2d"),
						scale = this.shape == "circle" ? 1 : 0.8,
						textHeight = 30,
						strokeWidth = this.outlineWidth || 2,
						yPadding = 8 * scale, // Apskritimui neaktualu...
						xPadding = yPadding * (this.xPaddingMultiplyBy || 1),
						inputVal = this.inputVal;
					if (inputVal) {
						inputVal = inputVal.trim();
					}
					this.ctx = ctx;
					if (!inputVal) {
						inputVal = this.emptyText;
						if (!inputVal) {
							if (this.shape == "circle") {
								inputVal = "XX";
							} else {
								inputVal = "Tekstas";
							}
						}
					}
					if (this.shape == "circle") {
						if (inputVal.length == 1) { // Jei vienas simbolis — tekstas didelis
							textHeight = 40;
						} else if (inputVal.length == 2) { // Jei du simboliai — tekstas gan didelis, bet ne toks didelis kaip tuo atveju, jei simbolis yra vienas...
							textHeight = 38;
						}
						if (this.specificParams) {
							if (this.specificParams.triangles) {
								textHeight = 24;
							} else if (this.specificParams.below) {
								textHeight = 24;
							}
						}
					}
					textHeight = textHeight * scale;
					strokeWidth = strokeWidth * scale;
					var font = textHeight + "px Arial",
						smallerFont = (textHeight * 0.6) + "px Arial";
					ctx.font = font;
					ctx.textBaseline = "bottom";
					var textInfo = ctx.measureText(inputVal),
						canvasWidth = textInfo.width,
						canvasHeight = textHeight + 2 * (yPadding + strokeWidth),
						maxTextWidth,
						textOffsetX = 0,
						textOffsetY = 0,
						arrowWidth = 7 * scale,
						arrowHeight = 34 * scale,
						arrowSidePadding = 7 * scale,
						aboveTextHeight = 0,
						belowTextHeight = 0,
						triangleWidth = 12 * scale,
						triangleHeight = 8 * scale,
						triangleOffset = 2 * scale,
						textElements = [];
					if (this.shape == "circle") {
						canvasWidth = 70 * scale + 1; // Geriau nelyginis skaičius?
						canvasHeight = canvasWidth;
					} else {
						if (this.above) {
							var aboveElementTextInfo = ctx.measureText(this.above);
							aboveTextHeight = aboveElementTextInfo.actualBoundingBoxAscent + Math.abs(aboveElementTextInfo.actualBoundingBoxDescent);
							canvasHeight += aboveTextHeight + arrowSidePadding * 0.2;
							if (aboveElementTextInfo.width > textInfo.width) {
								canvasWidth = aboveElementTextInfo.width;
							}
						}
						if (this.below) {
							var belowElementTextInfo = ctx.measureText(this.below);
							belowTextHeight = belowElementTextInfo.actualBoundingBoxAscent + Math.abs(belowElementTextInfo.actualBoundingBoxDescent);
							textOffsetY = belowTextHeight + arrowSidePadding * 0.5; // Užrašo žemiau padding'as turi sutapti su rodyklės į viršų padding'u...
							canvasHeight += textOffsetY;
							if (belowElementTextInfo.width > textInfo.width) {
								canvasWidth = belowElementTextInfo.width;
							}
						}
						canvasWidth = canvasWidth + 2 * xPadding;
						var additionalItemSize = arrowWidth + 4 * arrowSidePadding;
						if (this.arrows.left) {
							if (this.arrows.left != "up") {
								additionalItemSize = arrowHeight + arrowSidePadding;
							}
							canvasWidth += additionalItemSize;
							textOffsetX = additionalItemSize;
						}
						if (this.arrows.right) {
							if (this.arrows.right != "up") {
								additionalItemSize = arrowHeight + arrowSidePadding;
							}
							canvasWidth += additionalItemSize;
							textOffsetX -= additionalItemSize;
						}
						if (this.arrows.below) {
							arrowWidth = 5 * scale;
							additionalItemSize = arrowWidth + 3 * arrowSidePadding;
							textOffsetY = additionalItemSize;
							canvasHeight += additionalItemSize;
						}
						if (this.arrows.above) {
							canvasHeight += additionalItemSize + arrowSidePadding * 0.5; // Užrašo žemiau padding'as turi sutapti su rodyklės į viršų padding'u...
						}
						canvasWidth += strokeWidth * 2;
					}
					if (this.data.type == "828") {
						if (this.inputVal) {
							var pattern = /^(.*)-(.*?)\s?h?$/,
								match = inputVal.match(pattern);
							if (match) {
								textElements = textElements.concat(this.getTimeTextElements(match[1]));
								textElements.push({
									el: "–",
									type: "divider"
								});
								textElements = textElements.concat(this.getTimeTextElements(match[2]));
								textElements.push({
									el: " h"
								});
							} else {
								textElements.push({
									el: inputVal
								});
							}
							canvasWidth = 0;
							textElements.forEach(function(textElement){
								if (textElement.el) {
									if (textElement.type == "m") {
										ctx.font = smallerFont;
									} else {
										ctx.font = font;
									}
									textInfo = ctx.measureText(textElement.el);
									textElement.textInfo = textInfo;
									canvasWidth += textInfo.width;
								}
							});
							canvasWidth += (strokeWidth + xPadding) * 2;
						}
					}
					canvas.width = canvasWidth;
					canvas.height = canvasHeight;
					ctx.font = font; // Svarbu iš naujo nustatyti!!!
					ctx.textAlign = "center";
					if (this.shape == "circle") {
						ctx.fillStyle = "white";
						ctx.strokeStyle = this.outlineColor || "black";
						maxTextWidth = canvas.width - 2 * strokeWidth - 2 * xPadding;
						if (this.specificParams && (this.specificParams.triangles == "horizontal")) {
							maxTextWidth -= 2 * triangleHeight;
						}
						ctx.lineWidth = strokeWidth;
						ctx.beginPath();
						ctx.arc(canvas.width / 2, canvas.height / 2, canvas.width / 2, 0, 2 * Math.PI, true);
						ctx.fill();
						ctx.beginPath();
						ctx.arc(canvas.width / 2, canvas.height / 2, canvas.width / 2 - strokeWidth / 2, 0, 2 * Math.PI, true);
						ctx.stroke();
					} else {
						ctx.fillStyle = this.outlineColor || "black";
						ctx.fillRect(0, 0, canvas.width, canvas.height);
						ctx.fillStyle = this.fillColor || "white";
						ctx.fillRect(strokeWidth, strokeWidth, canvas.width - 2 * strokeWidth, canvas.height - 2 * strokeWidth);
						ctx.lineWidth = arrowWidth;
						ctx.fillStyle = "black";
						var xCenter,
							yCenter,
							xStart,
							yStart,
							xEnd,
							yEnd,
							arrowAngle;
						if (this.arrows.left) {
							xCenter = strokeWidth + (xPadding + additionalItemSize) / 2;
							yCenter = canvas.height / 2;
							ctx.beginPath();
							if (this.arrows.left == "up") {
								xEnd = xCenter;
								yEnd = yCenter - arrowHeight / 2 + this.arrowHeadHeight * scale;
								ctx.moveTo(xCenter, yCenter + arrowHeight / 2);
								ctx.lineTo(xEnd, yEnd - this.overlapArrowLinesBy);
								arrowAngle = 180;
							} else {
								xEnd = xCenter - arrowHeight / 2 + this.arrowHeadHeight * scale;
								yEnd = yCenter;
								ctx.moveTo(xCenter + arrowHeight / 2, yCenter);
								ctx.lineTo(xEnd - this.overlapArrowLinesBy, yEnd);
								arrowAngle = - 90;
							}
							ctx.stroke();
							this.drawArrowHead(xEnd, yEnd, arrowAngle * Math.PI / 180, scale);
						}
						if (this.arrows.right) {
							xCenter = canvas.width - strokeWidth - (xPadding + additionalItemSize) / 2;
							yCenter = canvas.height / 2;
							ctx.beginPath();
							if (this.arrows.left == "up") {
								xEnd = xCenter;
								yEnd = yCenter - arrowHeight / 2 + this.arrowHeadHeight * scale;
								ctx.moveTo(xCenter, yCenter + arrowHeight / 2);
								ctx.lineTo(xCenter, yEnd - this.overlapArrowLinesBy);
								arrowAngle = 180;
							} else {
								xEnd = xCenter + arrowHeight / 2 - this.arrowHeadHeight * scale;
								yEnd = yCenter;
								ctx.moveTo(xCenter - arrowHeight / 2, yCenter);
								ctx.lineTo(xEnd + this.overlapArrowLinesBy, yEnd);
								arrowAngle = 90;
							}
							ctx.stroke();
							this.drawArrowHead(xEnd, yEnd, arrowAngle * Math.PI / 180, scale);
						}
						if (this.arrows.below) {
							ctx.beginPath();
							xStart = strokeWidth + xPadding;
							yStart = yEnd = canvas.height - strokeWidth - (yPadding + additionalItemSize) / 2;
							xEnd = canvas.width - strokeWidth - xPadding;
							if (this.arrows.below == "left" || this.arrows.below == "both") {
								xStart += this.arrowHeadHeight * scale;
							}
							if (this.arrows.below == "right" || this.arrows.below == "both") {
								xEnd -= this.arrowHeadHeight * scale;
							}
							ctx.moveTo(xStart, yStart);
							ctx.lineTo(xEnd, yEnd);
							ctx.stroke();
							if (this.arrows.below == "left" || this.arrows.below == "both") {
								this.drawArrowHead(xStart, yStart, - 90 * Math.PI / 180, scale);
							}
							if (this.arrows.below == "right" || this.arrows.below == "both") {
								this.drawArrowHead(xEnd, yEnd, + 90 * Math.PI / 180, scale);
							}
						}
						if (this.arrows.above) {
							xCenter = canvas.width / 2;
							yCenter = strokeWidth + yPadding + arrowHeight / 2;
							xEnd = xCenter;
							yEnd = yCenter - arrowHeight / 2 + this.arrowHeadHeight * scale;
							ctx.beginPath();
							ctx.moveTo(xCenter, yCenter + arrowHeight / 2);
							ctx.lineTo(xEnd, yEnd - this.overlapArrowLinesBy);
							ctx.stroke();
							this.drawArrowHead(xEnd, yEnd, Math.PI, scale);
						}
						if (this.above) {
							ctx.textBaseline = "middle";
							ctx.fillText(this.above, canvas.width / 2 + textOffsetX / 2, strokeWidth + yPadding + aboveTextHeight / 2, maxTextWidth);
						}
						if (this.below) {
							ctx.textBaseline = "middle";
							ctx.fillText(this.below, canvas.width / 2 + textOffsetX / 2, canvas.height - strokeWidth - yPadding - belowTextHeight / 2, maxTextWidth);
						}
					}
					if (this.inputVal) {
						ctx.fillStyle = this.textColor || "black";
					} else {
						ctx.fillStyle = "#b7b7b7";
					}
					var textY;
					if (this.shape == "circle") {
						if (textInfo.fontBoundingBoxAscent) {
							textY = canvas.height / 2 + (textInfo.fontBoundingBoxAscent - textHeight) / 2;
						} else {
							textY = canvas.height / 2; // Pvz. Firefox'ui...
						}
						ctx.textBaseline = "middle"; // Svarbu iš naujo nustatyti!!!
						if (this.data && (this.data.type == "317")) {
							var mTextOffset = 10 * scale; // FIXME! Dabar reikšmė nustatyta vizualiai... Reiktų paskaičiuoti matematiškai!
							ctx.fillText("m", canvas.width / 2, textY + mTextOffset, maxTextWidth);
							textY -= mTextOffset;
						}
						if (this.specificParams && this.specificParams.below) {
							if (this.specificParams.below == "wheels") {
								var wheelsOffset = 8 * scale;
								this.drawWheels(ctx, canvas, textY + 1.4 * wheelsOffset, scale);
								textY -= wheelsOffset;
							}
						}
					} else {
						if (textInfo.fontBoundingBoxAscent) {
							textY = canvas.height - strokeWidth - yPadding - textOffsetY + (textInfo.fontBoundingBoxAscent - textHeight);
						} else {
							textY = canvas.height - strokeWidth - yPadding - textOffsetY; // Pvz. Firefox'ui...
						}
						ctx.textBaseline = "bottom"; // Svarbu iš naujo nustatyti!!!
					}
					if (this.data.type == "828") {
						if (this.inputVal) {
							ctx.textAlign = "left";
							var textX = strokeWidth + xPadding,
								textElementY;
							textElements.forEach(function(textElement){
								if (textElement.el) {
									textElementY = textY;
									if (textElement.type == "m") {
										ctx.font = smallerFont;
										textElementY -= 12 * scale;
									} else {
										ctx.font = font;
									}
									ctx.fillText(textElement.el, textX, textElementY);
									if (textElement.type == "divider") {
										ctx.lineWidth = 1 * scale;
										var textXCenter = textX + textElement.textInfo.width / 2,
											signLength = 4 * scale;
										textElementY -= 30 * scale;
										ctx.moveTo(textXCenter - signLength, textElementY - signLength);
										ctx.lineTo(textXCenter + signLength, textElementY + signLength);
										ctx.stroke();
										ctx.moveTo(textXCenter + signLength, textElementY - signLength);
										ctx.lineTo(textXCenter - signLength, textElementY + signLength);
										ctx.stroke();
										ctx.beginPath();
										ctx.arc(textXCenter - signLength, textElementY - signLength, 2 * scale, 0, 2 * Math.PI, true);
										ctx.fill();
										ctx.beginPath();
										ctx.arc(textXCenter + signLength, textElementY - signLength, 2 * scale, 0, 2 * Math.PI, true);
										ctx.fill();
									}
									textX += textElement.textInfo.width;
								}
							});
						} else {
							ctx.fillText(inputVal, canvas.width / 2 + textOffsetX / 2, textY, maxTextWidth);
						}
					} else {
						ctx.fillText(inputVal, canvas.width / 2 + textOffsetX / 2, textY, maxTextWidth);
						if (this.specificParams) {
							if (this.shape == "circle") {
								if (this.specificParams.diagonalLines) {
									ctx.clip(); // Šitas metodas leidžia toliau brėžiamoms linijoms neišlįsti už ribų!? TODO: patikrinti, ar gerai veikia...
									ctx.lineWidth = 1.5 * scale;
									var offsets = [0, 10 * scale, 5 * scale, - 5 * scale, - 10 * scale];
									offsets.forEach(function(offset){
										ctx.beginPath();
										ctx.moveTo(canvas.width, 0 - offset);
										ctx.lineTo(0, canvas.height - offset);
										ctx.stroke();
									});
								}
								if (this.specificParams.triangles) {
									ctx.fillStyle = "black";
									if (this.specificParams.triangles == "vertical") {
										CommonHelper.drawArrowHead(canvas.width / 2, strokeWidth + triangleOffset, 0, triangleWidth, triangleHeight, ctx);
										CommonHelper.drawArrowHead(canvas.width / 2, canvas.height - strokeWidth - triangleOffset, Math.PI, triangleWidth, triangleHeight, ctx);
									} else if (this.specificParams.triangles == "horizontal") {
										CommonHelper.drawArrowHead(strokeWidth + triangleOffset, canvas.height / 2, Math.PI / 2, triangleWidth, triangleHeight, ctx);
										CommonHelper.drawArrowHead(canvas.width - strokeWidth - triangleOffset, canvas.height / 2, - Math.PI / 2, triangleWidth, triangleHeight, ctx);
									}
								}
							}
						}
					}
				}
			},
			drawArrowHead: function(xEnd, yEnd, angle, scale){
				CommonHelper.drawArrowHead(xEnd, yEnd, angle, this.arrowHeadWidth * scale, this.arrowHeadHeight * scale, this.ctx);
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
					if (["316"].indexOf(this.data.type) != -1) {
						if (value[value.length - 1] != "m") {
							value += "m";
						}
					} else if (["315"].indexOf(this.data.type) != -1) {
						if (value[value.length - 1] != "t") {
							value += "t";
						}
					}
				}
				return value;
			},
			drawWheels: function(ctx, canvas, y, scale){
				// Labai nesistengiau kažko ypatingo daryti, nes labai mažai tokių simbolių...
				ctx.save();
				ctx.strokeStyle = ctx.fillStyle = "black";
				ctx.beginPath();
				ctx.lineWidth = 3 * scale;
				var width = 34 * scale, // FIXME! Dabar iš oro paimtas, bet galima matematiškai apskaičiuoti pagal apskritimo parametrus ir pan.
					wheelHeight = 13 * scale;
				ctx.moveTo(canvas.width / 2 - width / 2, y);
				ctx.lineTo(canvas.width / 2 + width / 2, y);
				ctx.stroke();
				ctx.beginPath();
				ctx.moveTo(canvas.width / 2 - width / 2, y - wheelHeight / 2);
				ctx.lineTo(canvas.width / 2 - width / 2, y + wheelHeight / 2);
				ctx.stroke();
				ctx.beginPath();
				ctx.moveTo(canvas.width / 2 + width / 2, y - wheelHeight / 2);
				ctx.lineTo(canvas.width / 2 + width / 2, y + wheelHeight / 2);
				ctx.stroke();
				ctx.beginPath();
				ctx.arc(canvas.width / 2, y, 4 * scale, 0, 2 * Math.PI, true);
				ctx.fill();
				ctx.restore();
			},
			getTimeTextElements: function(el){
				var textElements = [];
				if (el) {
					el = el.trim();
					el = el.split(":");
					if (el.length == 2) {
						textElements.push({
							el: el[0],
							type: "h"
						});
						textElements.push({
							el: el[1],
							type: "m"
						});
					} else {
						textElements.push({
							el: el[0],
							type: "h"
						});
					}
				}
				return textElements;
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
			},
			arrows: {
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
	canvas {
		display: block;
	}
</style>