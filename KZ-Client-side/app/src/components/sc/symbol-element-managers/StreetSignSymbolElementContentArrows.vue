<template>
	<div>
		<div v-if="!noForm">
			<div class="mb-3 font-weight-medium" v-if="!compact">Rodyklės parametrai:</div>
			<div :class="[compact ? null : 'ml-6']">
				<div class="d-flex align-center" v-if="roundaboutAvailable || arrowDownAvailable || arrow522Available">
					<div>Rodyklės tipas:</div>
					<v-radio-group
						v-model="params.mainType"
						class="ma-0 pa-0 ml-3"
						hide-details
						row
					>
						<v-radio
							:label="arrowDownAvailable ? 'Į viršų' : 'Paprasta'"
							value="simple"
							class="ma-0 pa-0"
						></v-radio>
						<v-radio
							label="Žemyn"
							value="down"
							class="ma-0 pa-0 ml-3"
							v-if="arrowDownAvailable"
						></v-radio>
						<v-radio
							label="522 rodyklė"
							value="522"
							class="ma-0 pa-0 ml-3"
							v-if="arrow522Available"
						></v-radio>
						<v-radio
							label="Žiedinės sankryžos rodyklė"
							value="round"
							class="ma-0 pa-0 ml-3"
							v-if="roundaboutAvailable"
						></v-radio>
					</v-radio-group>
				</div>
				<div :class="[(roundaboutAvailable || arrowDownAvailable || arrow522Available) ? 'mt-3' : null]">
					<template v-if="dataType == 'shuttle'">
						<v-checkbox
							v-model="params.simple.shuttle"
							label="Eismo juosta maršrutiniam transportui"
							class="ma-0 pa-0 d-inline-block"
							hide-details
						></v-checkbox>
					</template>
					<template v-else-if="params.mainType == 'simple'">
						<div class="mb-2">Rodyklės:</div>
						<div class="d-inline-flex">
							<template v-for="(arrowParams, i) in params.simple.arrows">
								<div
									:class="['pr-6 mr-5', additionalArrowsAvailable ? 'divider' : (i == params.simple.arrows.length - 1) ? null : 'divider']"
									:key="i"
								>
									<ArrowControls
										:model="arrowParams"
									/>
								</div>
							</template>
							<div class="d-flex flex-column ml-2" v-if="additionalArrowsAvailable">
								<v-btn
									text
									outlined
									small
									color="primary"
									class="mb-1"
									v-on:click="addSimpleArrow('left')"
									:disabled="params.simple.simpleArrowAdditionalLeft"
								>
									Pridėti `kairiau`
								</v-btn>
								<v-btn
									text
									outlined
									small
									color="primary"
									class="mb-1"
									v-on:click="addSimpleArrow('right')"
									:disabled="params.simple.simpleArrowAdditionalRight"
								>
									Pridėti `dešiniau`
								</v-btn>
								<v-btn
									text
									outlined
									small
									color="primary"
									class="mb-1"
									v-on:click="addSimpleArrow('up')"
									:disabled="params.simple.simpleArrowAdditionalUpUnavailable"
								>
									Pridėti `į viršų`
								</v-btn>
								<v-btn
									text
									outlined
									small
									color="primary"
									class="mb-1 d-none"
									v-on:click="addSimpleArrow('down')"
									:disabled="params.simple.simpleArrowAdditionalDown"
								>
									Pridėti `žemyn`
								</v-btn>
							</div>
						</div>
					</template>
					<template v-else-if="params.mainType == 'round'">
						<div>
							<v-checkbox
								v-model="params.round.closedCircle"
								label="Uždaras apskritimas"
								:value="true"
								class="ma-0 pa-0 d-inline-block"
								hide-details
							></v-checkbox>
						</div>
						<div class="mt-3">
							<RoundaboutArrowsSelector
								:onRoundaboutArrowsSelect="onRoundaboutArrowsSelect"
								:selectedArray="params.round.selectedArrows"
							/>
						</div>
						<div class="mt-4">
							<div>
								<v-checkbox
									v-model="params.round.beforeToLeft"
									label="Iki žiedinės sankryžos rodyklė į kairę"
									:value="true"
									class="ma-0 pa-0 d-inline-block"
									hide-details
								></v-checkbox>
							</div>
							<div>
								<v-checkbox
									v-model="params.round.beforeToRight"
									label="Iki žiedinės sankryžos rodyklė į dešinę"
									:value="true"
									class="ma-0 pa-0 d-inline-block"
									hide-details
								></v-checkbox>
							</div>
						</div>
					</template>
				</div>
			</div>
		</div>
		<template>
			<div :class="[
				'font-weight-medium',
				dataType == 'shuttle' ? 'pt-5' : (params.mainType == 'down' ? null : 'pt-5'),
				compact ? 'mb-2' : 'mb-4']"
			>Sugeneruotas rodyklės piešinukas:</div>
			<div :class="[compact ? null : 'ml-8']">
				<canvas ref="canvas" :class="['mr-2 d-inline-block', darkArrows ? 'bordered' : null]"></canvas>
				<canvas ref="canvasToSave" :class="['mr-2', test ? 'd-inline-block' : 'd-none']"></canvas>
			</div>
		</template>
	</div>
</template>

<script>
	import ArrowControls from "./ArrowControls";
	import CommonHelper from "../../helpers/CommonHelper";
	import LineString from "ol/geom/LineString";
	import RoundaboutArrowsSelector from "./RoundaboutArrowsSelector";
	import Vue from "vue";

	export default {
		data: function(){
			var data = {
				alreadyMounted: false,
				params: {
					mainType: "simple",
					round: {
						closedCircle: false
					},
					simple: {
						arrows: [{
							type: "left"
						},{
							type: "up",
							params: {
								checked: true
							}
						},{
							type: "right"
						}],
						simpleArrowAdditionalLeft: false,
						simpleArrowAdditionalRight: false,
						simpleArrowAdditionalDown: false,
						simpleArrowAdditionalUpUnavailable: false
					}
				},
				arrowParams: {
					arrowWidth: 14,
					arrowHeadWidth: 34,
					arrowHeadHeight: 36
				},
				roundaboutAvailable: true,
				arrowDownAvailable: false,
				arrow522Available: false,
				additionalArrowsAvailable: true,
				roundaboutRadius: 26,
				roundaboutStrokeWidth: 14,
				roundaboutArrowBodyHeight: 30,
				neckRadius: 20,
				padding: 30, // Testavimui galima naudoti `0`!
				test: false,
				defaultRoundaboutDownLineHeight: 34,
				overlapArrowLinesBy: 1 // Naudojamas, nes kartais pasimatydavo mažas mažytis linijos trūkis pvz. ten kur susiliesdavo rodykės koto galas su rodyklės galva ir pan.
			};
			if (this.data && (this.data.mode == "element-edit")) {
				this.restoreData(data, this.data);
			}
			data.roundaboutDownLineHeight = data.defaultRoundaboutDownLineHeight;
			data.arrowParams.x = data.roundaboutDownLineHeight + 2 * data.roundaboutRadius + data.roundaboutArrowBodyHeight - data.roundaboutStrokeWidth / 2;
			this.determineIfSimpleArrowAdditionalUpUnavailable(data.params);
			if (this.type) {
				var newArrows = [],
					restrictArrows = [];
				if (this.type == "508") {
					restrictArrows.push("up");
				} else if (this.type == "509") {
					restrictArrows.push("right");
				} else if (this.type == "510") {
					restrictArrows.push("left");
				} else if (this.type == "511") {
					restrictArrows = restrictArrows.concat(["up", "right"]);
				} else if (this.type == "512") {
					restrictArrows = restrictArrows.concat(["left", "up"]);
				} else if (["517", "518"].indexOf(this.type) != -1) {
					restrictArrows = restrictArrows.concat(["up"]);
					data.arrowDownAvailable = true;
				} else if (this.type == "522") {
					data.arrow522Available = true;
				} else if (["523", "524"].indexOf(this.type) != -1) {
					// TODO: o gal to atsisakyti? Ir sukurti naują tipą pvz. "up"? Kuris būtų kaip "down" ar "522"...
					restrictArrows = restrictArrows.concat(["up"]);
					data.arrowDownAvailable = true;
				}
				if (restrictArrows.length) {
					data.roundaboutAvailable = false;
					data.additionalArrowsAvailable = false;
					restrictArrows.forEach(function(restrictArrow){
						if (data.params.simple) {
							var arrowAvailable = false;
							if (data.params.simple.arrows) {
								if (this.data && (this.data.mode == "element-edit")) {
									// ...
								} else {
									data.params.simple.arrows = [];
								}
								data.params.simple.arrows.forEach(function(arrow){
									if (arrow.type == restrictArrow) {
										arrowAvailable = true;
										newArrows.push(arrow);
									}
								});
							}
							if (!arrowAvailable) {
								newArrows.push({
									type: restrictArrow,
									params: {
										checked: true
									}
								});
							}
						}
					}.bind(this));
					data.params.simple.arrows = newArrows;
				}
			}
			return data;
		},

		props: {
			data: Object,
			compact: Boolean,
			refItem: Object,
			modRefItem: Function,
			noForm: Boolean,
			type: String,
			darkArrows: Boolean,
			dataType: String
		},

		components: {
			ArrowControls,
			RoundaboutArrowsSelector
		},

		mounted: function(){
			this.alreadyMounted = true;
			this.renderCanvas();
		},

		methods: {
			renderCanvas: function(callback){
				var canvas = this.$refs.canvas;
				if (canvas) {
					this.canvas = canvas;
					this.ctx = canvas.getContext("2d");
					this.setCanvasSize();
					var strokeColor = "white",
						backgroundColor = "#444444",
						x,
						y,
						xEnd,
						yEnd;
					if (this.darkArrows) {
						strokeColor = "black";
						backgroundColor = "white";
					}
					this.canvas.style.background = backgroundColor;
					this.ctx.fillStyle = backgroundColor;
					this.ctx.strokeStyle = strokeColor;
					this.ctx.fillStyle = strokeColor;
					if (this.params.mainType == "round") {
						var centerX = this.canvas.width / 2,
							centerY = this.canvas.height - this.roundaboutDownLineHeight - this.roundaboutRadius,
							sAngle = 0.5 * Math.PI,
							eAngle;
						if (this.params.round.closedCircle) {
							sAngle = 0;
							eAngle = 2 * Math.PI;
						} else {
							if (this.params.round.selectedArrows && this.params.round.selectedArrows.length) {
								// Mano kampas nurodytas nuo apačios einant prieš laikrodžio rodyklę, tad čia prireiks šiek tiek matematikos...
								eAngle = 2.5 * Math.PI - Math.max(...this.params.round.selectedArrows) * Math.PI / 180;
							} else {
								eAngle = sAngle;
							}
						}
						this.ctx.lineCap = "butt";
						// Brėžiame apskritimo dalį...
						this.ctx.lineWidth = this.roundaboutStrokeWidth;
						this.ctx.beginPath();
						this.ctx.arc(centerX, centerY, this.roundaboutRadius, sAngle, eAngle, true);
						this.ctx.stroke();
						if (this.params.round.closedCircle || (this.params.round.selectedArrows && this.params.round.selectedArrows.length) || this.params.round.beforeToLeft || this.params.round.beforeToRight) {
							// Brėžiame apatinį kotą iki apskritimo...
							this.ctx.lineWidth = this.arrowParams.arrowWidth;
							this.ctx.beginPath();
							this.ctx.moveTo(this.canvas.width / 2, this.canvas.height);
							this.ctx.lineTo(this.canvas.width / 2, this.canvas.height - this.roundaboutDownLineHeight - this.roundaboutStrokeWidth / 2);
							this.ctx.stroke();
						}
						// Brėžiame rodykles...
						if (this.params.round.selectedArrows) {
							this.ctx.lineCap = "butt";
							// https://www.w3schools.com/tags/canvas_arc.asp
							// Pvz. pagal mano logiką yra 90 laipsnių, o pagal canvas'o logiką — 0
							// 90 laipsnių -> 0
							// 180 laipsnių -> 1.5 * PI
							// 270 laipsnių -> 1 * PI
							// 45 laipsniai -> 0.25 * PI
							this.params.round.selectedArrows.forEach(function(angle){
								// Pagal kampą reikia rasti pradinį tašką, nuo kurio brėžiama rodyklė...
								angle = angle * Math.PI / 180;
								x = centerX + (this.roundaboutRadius - this.roundaboutStrokeWidth / 2) * Math.sin(angle);
								y = centerY + (this.roundaboutRadius - this.roundaboutStrokeWidth / 2) * Math.cos(angle);
								xEnd = x + this.roundaboutArrowBodyHeight * Math.sin(angle);
								yEnd = y + this.roundaboutArrowBodyHeight * Math.cos(angle);
								this.ctx.beginPath();
								this.ctx.moveTo(x, y);
								this.ctx.lineTo(xEnd + this.overlapArrowLinesBy * Math.sin(angle), yEnd + this.overlapArrowLinesBy * Math.cos(angle));
								this.ctx.stroke();
								this.drawArrowHead(xEnd, yEnd, angle);
							}.bind(this));
						}
						if (this.params.round.beforeToLeft) {
							this.ctx.beginPath();
							x = centerX;
							y = this.canvas.height - 30;
							xEnd = centerX - this.roundaboutArrowBodyHeight;
							this.ctx.moveTo(x, y);
							this.ctx.lineTo(xEnd, y);
							this.ctx.stroke();
							this.drawArrowHead(xEnd, y, - 90 * Math.PI / 180);
						}
						if (this.params.round.beforeToRight) {
							this.ctx.beginPath();
							x = centerX;
							y = this.canvas.height - 30;
							xEnd = centerX + this.roundaboutArrowBodyHeight;
							this.ctx.moveTo(x, y);
							this.ctx.lineTo(xEnd, y);
							this.ctx.stroke();
							this.drawArrowHead(xEnd, y, 90 * Math.PI / 180);
						}
					} else if (this.params.mainType == "simple") {
						if (this.params.simple.arrows) {
							var upArrowParamsList = [];
							this.params.simple.arrows.forEach(function(item){
								if (item.params) {
									if (item.params.checked) {
										if (item.type == "up") {
											upArrowParamsList.push(item.params);
										} else if ((item.type == "left") || (item.type == "right")) {
											this.drawSimpleSideArrow(item.type, item.params);
										} else if (item.type == "down") {
											this.drawSimpleDownArrow();
										}
									}
								}
							}.bind(this));
							if (upArrowParamsList) {
								upArrowParamsList.forEach(function(upArrowParams){
									this.drawSimpleUpArrow(upArrowParams);
								}.bind(this));
							}
						}
					} else if (this.params.mainType == "down") {
						this.drawSimpleDownArrow();
					} else if (this.params.mainType == "522") {
						this.draw522Arrow();
					}
					var arrowsCanvas;
					if (this.test || (this.refItem && this.modRefItem) || callback) {
						arrowsCanvas = this.renderCanvasToSave(false); // Jei `false`, tai vaizdas gražesnis, bet lėčiau piešiamas... Jei `true`, tai vaizdas nelabai gražus, bet piešiasi greitai...
					}
					var imageDimensions;
					if (this.refItem) {
						if (this.modRefItem) {
							imageDimensions = {
								width: this.$refs.canvasToSave.width,
								height: this.$refs.canvasToSave.height
							};
							this.modRefItem(this.refItem, this.getAllData(), imageDimensions, arrowsCanvas);
						}
					}
					if (callback) {
						imageDimensions = {
							width: this.$refs.canvasToSave.width,
							height: this.$refs.canvasToSave.height
						};
						callback(this.getAllData(), imageDimensions, arrowsCanvas);
					}
				}
			},
			drawArrowHead: function(xEnd, yEnd, angle){
				CommonHelper.drawArrowHead(xEnd, yEnd, angle, this.arrowParams.arrowHeadWidth, this.arrowParams.arrowHeadHeight, this.ctx, this.test);
			},
			onRoundaboutArrowsSelect: function(selectedArray){
				Vue.set(this.params.round, "selectedArrows", selectedArray);
			},
			setCanvasSize: function(){
				if (this.params.mainType == "round") {
					var roundaboutDownLineHeight = this.defaultRoundaboutDownLineHeight;
					if (this.params.round.selectedArrows && this.params.round.selectedArrows.length) {
						var min = Math.min(...this.params.round.selectedArrows),
							max = Math.max(...this.params.round.selectedArrows);
						if ((max > 270) || (min < 90)) {
							roundaboutDownLineHeight += 20;
						}
					}
					if (this.params.round.beforeToLeft || this.params.round.beforeToRight) {
						roundaboutDownLineHeight += 40;
					}
					this.roundaboutDownLineHeight = roundaboutDownLineHeight;
					this.canvas.width = 2 * (this.roundaboutRadius + this.roundaboutArrowBodyHeight + this.arrowParams.arrowHeadHeight - this.roundaboutStrokeWidth / 2 + this.padding);
					this.canvas.height = this.roundaboutDownLineHeight + 2 * this.roundaboutRadius + this.roundaboutArrowBodyHeight + this.arrowParams.arrowHeadHeight - this.roundaboutStrokeWidth / 2 + this.padding;
				} else {
					this.canvas.width = 2 * (this.arrowParams.x + this.arrowParams.arrowHeadHeight + this.padding);
					this.canvas.height = this.arrowParams.x + this.arrowParams.arrowHeadHeight + this.padding;
				}
			},
			addSimpleArrow: function(type){
				var arrows = this.params.simple.arrows;
				if (arrows) {
					arrows = arrows.slice();
					var newArrow = {
						type: type
					};
					if (type == "up") {
						newArrow.secondary = true;
					}
					arrows.push(newArrow);
					this.params.simple.arrows = arrows;
					if (type == "left") {
						this.params.simple.simpleArrowAdditionalLeft = true;
					} else if (type == "right") {
						this.params.simple.simpleArrowAdditionalRight = true;
					} else if (type == "down") {
						this.params.simple.simpleArrowAdditionalDown = true;
					}
					this.determineIfSimpleArrowAdditionalUpUnavailable(this.params);
				}
			},
			drawSimpleUpArrow: function(params){
				var x = this.canvas.width / 2,
					yEnd = this.canvas.height - this.arrowParams.x,
					multiplyBy,
					xWithOffset,
					extraDataForRestricted = {};
				this.ctx.lineWidth = this.arrowParams.arrowWidth;
				if (params.rootType == "straight" || this.dataType == "shuttle") {
					this.ctx.beginPath();
					this.ctx.moveTo(x, this.canvas.height);
					this.ctx.lineTo(x, yEnd - this.overlapArrowLinesBy);
					this.ctx.stroke();
					this.drawArrowHead(x, yEnd, 180 * Math.PI / 180);
				} else if (params.rootType == "with-curve") {
					if (params.rootCurveYProportions && params.rootCurveYProportions.length == 2) {
						multiplyBy = 1;
						if (params.rootCurveSide == "right") {
							multiplyBy = - 1;
						}
						if (params.rootDeltaX) {
							x += this.arrowParams.x * params.rootDeltaX * multiplyBy;
						}
						xWithOffset = x - multiplyBy * params.curveDeltaX * this.arrowParams.x;
						var curvePoints = [
							[x, this.canvas.height],
							[x, this.canvas.height - params.rootCurveYProportions[0] * this.arrowParams.x],
							[xWithOffset, this.canvas.height - params.rootCurveYProportions[1] * this.arrowParams.x],
							[xWithOffset, yEnd - this.overlapArrowLinesBy]
						];
						if (params.skipFirstPoint) {
							curvePoints.shift();
							this.ctx.rect(0, 0, this.canvas.width / 2 - 6, this.canvas.height); // Jei šito nebus, tai dešinėje negražus kraštas liks...
							this.ctx.clip();
						}
						this.ctx.beginPath();
						var rounded = true; // Vietinis config'as...
						if (rounded) {
							var neckRadius = 7, // Šiaip, `iš akies` tokį pasirinkau...
								curvePointsA = [],
								curvePoint = {},
								prevCurvePoint,
								nextCurvePoint;
							curvePoints.forEach(function(point, i){
								curvePoint = {
									p: point,
									a: []
								};
								prevCurvePoint = curvePoints[i - 1];
								nextCurvePoint = curvePoints[i + 1];
								if (prevCurvePoint && nextCurvePoint) {
									var angle1 = Math.atan2(point[1] - prevCurvePoint[1], prevCurvePoint[0] - point[0]),
										angle2 = Math.atan2(point[1] - nextCurvePoint[1], nextCurvePoint[0] - point[0]);
									curvePoint.a.push([point[0] + neckRadius * Math.cos(angle1), point[1] - neckRadius * Math.sin(angle1)]);
									curvePoint.a.push([point[0] + neckRadius * Math.cos(angle2), point[1] - neckRadius * Math.sin(angle2)]);
								}
								curvePointsA.push(curvePoint);
							}.bind(this));
							curvePointsA.forEach(function(point, i){
								if (point.p) {
									if (i) {
										if (point.a && point.a.length) {
											this.ctx.lineTo(point.a[0][0], point.a[0][1]);
											this.ctx.quadraticCurveTo(point.p[0], point.p[1], point.a[1][0], point.a[1][1]);
										} else {
											this.ctx.lineTo(point.p[0], point.p[1]);
										}
									} else {
										this.ctx.moveTo(point.p[0], point.p[1]);
									}
								}
								this.ctx.stroke();
							}.bind(this));
						} else {
							curvePoints.forEach(function(point, i){
								if (i) {
									this.ctx.lineTo(point[0], point[1]);
								} else {
									this.ctx.moveTo(x, this.canvas.height);
								}
							}.bind(this));
							this.ctx.stroke();
						}
						this.drawArrowHead(xWithOffset, yEnd, 180 * Math.PI / 180);
						extraDataForRestricted.curvePoints = curvePoints;
					}
				} else if (params.rootType == "with-top-arc") {
					multiplyBy = 1;
					if (params.rootTopArcSide == "right") {
						multiplyBy = - 1;
					}
					this.ctx.beginPath();
					this.ctx.moveTo(x, this.canvas.height);
					xWithOffset = x - multiplyBy * params.topArcDeltaX * this.arrowParams.x;
					var arcPoints = [
						[x, this.canvas.height - params.topArcDeltaY * this.arrowParams.x],
						[xWithOffset, yEnd]
					];
					this.ctx.lineTo(arcPoints[0][0], arcPoints[0][1]);
					this.ctx.quadraticCurveTo(arcPoints[1][0], arcPoints[0][1], arcPoints[1][0], arcPoints[1][1]);
					this.ctx.stroke();
					this.drawArrowHead(xWithOffset, yEnd, 180 * Math.PI / 180);
					extraDataForRestricted.arcPointStart = arcPoints[0];
					extraDataForRestricted.arcPointEnd = arcPoints[1];
					extraDataForRestricted.controlPoint = [arcPoints[1][0], arcPoints[0][1]];
				}
				if (params.restricted) {
					this.drawRestrictedSign(x, params, "up", extraDataForRestricted);
				}
				this.drawShuttleIfNeeded(x, "up");
			},
			drawSimpleSideArrow: function(side, params){
				var multiplyBy = 1,
					dX,
					dY;
				if (side == "right") {
					multiplyBy = - 1;
				}
				dX = multiplyBy * this.arrowParams.x * params.deltaX;
				dY = this.arrowParams.x * params.deltaY;
				var x = this.canvas.width / 2,
					y = this.canvas.height,
					xEnd,
					yEnd = this.canvas.height - dY;
				this.ctx.lineWidth = this.arrowParams.arrowWidth;
				this.ctx.lineCap = "butt";
				if (params.headAngle == 0) {
					this.ctx.lineCap = "round"; // FIMXE! Gal to vengti ir render'inimo situaciją taisyti naudojant `overlapArrowLinesBy`?
				}
				this.ctx.beginPath();
				this.ctx.moveTo(x, y);
				xEnd = x - dX;
				var bendPoint,
					curvePoint1,
					curvePoint2,
					controlPoint,
					tempPointsCoordinates = [],
					extraDataForRestricted = {};
				if (params.sideArrowType == "smooth" && (params.headAngle == 90 || params.headAngle == 135)) {
					if (params.headAngle == 90) {
						controlPoint = [x, yEnd];
						curvePoint2 = [xEnd, yEnd]; // TODO: gal reikia pakoreguoti atsižvelgiant į `overlapArrowLinesBy`?
						this.ctx.quadraticCurveTo(controlPoint[0], controlPoint[1], curvePoint2[0], curvePoint2[1]);
						this.ctx.stroke();
						if (this.test) {
							tempPointsCoordinates.push(controlPoint);
						}
					} else if (params.headAngle == 135) {
						controlPoint = [x, yEnd + this.arrowParams.x * params.deltaX];
						curvePoint2 = [xEnd, yEnd]; // TODO: gal reikia pakoreguoti atsižvelgiant į `overlapArrowLinesBy`?
						this.ctx.quadraticCurveTo(controlPoint[0], controlPoint[1], curvePoint2[0], curvePoint2[1]);
						this.ctx.stroke();
						if (this.test) {
							tempPointsCoordinates.push(controlPoint);
						}
					}
					extraDataForRestricted.arcPointStart = [x, y];
					extraDataForRestricted.arcPointEnd = curvePoint2;
					extraDataForRestricted.controlPoint = controlPoint;
				} else {
					var headAngle = params.headAngle * Math.PI / 180,
						points = [{
							point: [x, y]
						}];
					if (params.neckRounded) {
						var neckRadius = this.neckRadius;
						if (params.headAngle == 45) { // PV/PR
							bendPoint = [x, yEnd];
							curvePoint1 = [bendPoint[0], bendPoint[1] + neckRadius];
							curvePoint2 = [ // BIG FIXME! Peržiūrėti formulę. Čia gali būti didelė NSMN!
								bendPoint[0] - multiplyBy * neckRadius * Math.cos(headAngle),
								bendPoint[1] + neckRadius * Math.sin(headAngle)
							];
							// FIXME! Neaišku, ar geros controlPoint'o koordinatės...
							controlPoint = [
								bendPoint[0],
								bendPoint[1]
							];
							points.push({
								curve: {
									start: curvePoint1,
									end: curvePoint2,
									control: controlPoint
								}
							});
							yEnd += this.arrowParams.x * params.deltaX;
							points.push({
								point: [xEnd - multiplyBy * this.overlapArrowLinesBy * Math.sin(headAngle), yEnd + this.overlapArrowLinesBy * Math.cos(headAngle)]
							});
							if (this.test) {
								tempPointsCoordinates.push(bendPoint);
								tempPointsCoordinates.push(curvePoint1);
								tempPointsCoordinates.push(curvePoint2);
							}
						} else if (params.headAngle == 135) { // ŠV/ŠR
							bendPoint = [x, yEnd + this.arrowParams.x * params.deltaX];
							curvePoint1 = [bendPoint[0], bendPoint[1] + neckRadius];
							curvePoint2 = [ // BIG FIXME! Peržiūrėti formulę. Čia gali būti didelė NSMN!
								bendPoint[0] + multiplyBy * neckRadius * Math.cos(headAngle),
								bendPoint[1] - neckRadius * Math.sin(headAngle)
							];
							// FIXME! Neaišku, ar geros controlPoint'o koordinatės...
							controlPoint = [
								bendPoint[0],
								bendPoint[1]
							];
							points.push({
								curve: {
									start: curvePoint1,
									end: curvePoint2,
									control: controlPoint
								}
							});
							points.push({
								point: [xEnd - multiplyBy * this.overlapArrowLinesBy * Math.sin(headAngle), yEnd + this.overlapArrowLinesBy * Math.cos(headAngle)]
							});
							if (this.test) {
								tempPointsCoordinates.push(bendPoint);
								tempPointsCoordinates.push(curvePoint1);
								tempPointsCoordinates.push(curvePoint2);
							}
						} else {
							if (params.headAngle == 0) {
								// TODO, FIXME! Čia tų reikšmių ne'hardcode'inti, o rasti bendrą tendenciją, formulę...
								if (params.deltaX < 0.40) {
									if (params.deltaX < 0.20) {
										neckRadius = 5;
									} else {
										neckRadius = this.neckRadius / 2; // Nes posūkis labai arti ir reikia sumažinti atkarpą tarp lankų...
									}
								}
							}
							points.push({
								curve: {
									start: [x, yEnd + neckRadius - this.overlapArrowLinesBy],
									end: [x - multiplyBy * neckRadius, yEnd],
									control: [x, yEnd]
								}
							});
							if (params.headAngle == 0) {
								points.push({
									curve: {
										start: [xEnd + multiplyBy * neckRadius, yEnd],
										end: [xEnd, yEnd + neckRadius],
										control: [xEnd, yEnd]
									}
								});
								yEnd += this.arrowParams.x * params.deltaYNegative;
								points.push({
									point: [xEnd, yEnd]
								});
							} else {
								points.push({
									point: [xEnd - multiplyBy * this.overlapArrowLinesBy, yEnd]
								});
							}
						}
					} else {
						// TODO! Čia dar gi nėra this.overlapArrowLinesBy!
						if (params.headAngle == 45) { // PV/PR
							points.push({
								point: [x, yEnd]
							});
							yEnd += this.arrowParams.x * params.deltaX;
							points.push({
								point: [xEnd - multiplyBy * this.overlapArrowLinesBy * Math.sin(headAngle), yEnd + this.overlapArrowLinesBy * Math.cos(headAngle)]
							});
						} else if (params.headAngle == 135) { // ŠV/ŠR
							points.push({
								point: [x, yEnd + this.arrowParams.x * params.deltaX]
							});
							points.push({
								point: [xEnd - multiplyBy * this.overlapArrowLinesBy * Math.sin(headAngle), yEnd + this.overlapArrowLinesBy * Math.cos(headAngle)]
							});
						} else {
							points.push({
								point: [x, yEnd]
							});
							points.push({
								point: [xEnd, yEnd]
							});
							if (params.headAngle == 0) { // T. y. jei rodyklė yra P, tai dar brėžiame liniją žemyn...
								yEnd += this.arrowParams.x * params.deltaYNegative;
								points.push({
									point: [xEnd, yEnd]
								});
							}
						}
					}
					if (points.length) {
						points.forEach(function(point, i){
							if (!i) {
								this.ctx.moveTo(point[0], point[1]);
							} else {
								if (point.point) {
									this.ctx.lineTo(point.point[0], point.point[1]);
								} else if (point.curve) {
									this.ctx.lineTo(point.curve.start[0], point.curve.start[1]);
									this.ctx.quadraticCurveTo(point.curve.control[0], point.curve.control[1], point.curve.end[0], point.curve.end[1]);
								}
							}
						}.bind(this));
						this.ctx.stroke();
					}
					extraDataForRestricted.points = points;
				}
				tempPointsCoordinates.forEach(function(coordinate){
					this.ctx.save();
					this.ctx.lineWidth = 1;
					this.ctx.strokeStyle = "blue";
					this.ctx.beginPath();
					this.ctx.arc(coordinate[0], coordinate[1], 3, 0, 2 * Math.PI, true);
					this.ctx.stroke();
					this.ctx.restore();
				}.bind(this));
				this.drawArrowHead(xEnd, yEnd, (params.headAngle * multiplyBy * -1) * Math.PI / 180);
				if (params.restricted) {
					this.drawRestrictedSign(x, params, "side", extraDataForRestricted);
				}
			},
			drawSimpleDownArrow: function(){
				var x = this.canvas.width / 2,
					yEnd = this.arrowParams.x;
				this.ctx.lineWidth = this.arrowParams.arrowWidth;
				this.ctx.beginPath();
				this.ctx.moveTo(x, 0);
				this.ctx.lineTo(x, yEnd + this.overlapArrowLinesBy);
				this.ctx.stroke();
				this.drawArrowHead(x, yEnd, Math.PI / 180);
				this.drawShuttleIfNeeded(x, "down");
			},
			draw522Arrow: function(){
				this.drawSimpleUpArrow({
					rootType: "with-curve",
					rootCurveSide: "left",
					rootCurveYProportions: [0.4, 0.7],
					curveDeltaX: 0.45,
					skipFirstPoint: true
				});
			},
			drawRestrictedSign: function(x, params, type, extraData){
				this.ctx.lineCap = "butt";
				var y,
					restrictedSignPointCoordinates;
				if (type == "up") {
					var totalDistance,
						distanceFrom,
						quadraticBezierLength;
					if (params.rootType == "straight") {
						y = this.canvas.height - this.arrowParams.x * params.restrictedDistanceFrom;
					} else if (params.rootType == "with-curve") {
						if (extraData.curvePoints) {
							restrictedSignPointCoordinates = this.getRestrictedSignPointCoordinates(extraData.curvePoints, params.restrictedDistanceFrom);
							x = restrictedSignPointCoordinates.x;
							y = restrictedSignPointCoordinates.y;
						}
					} else if (params.rootType == "with-top-arc") {
						// https://stackoverflow.com/questions/9194558/center-point-on-html-quadratic-curve
						var straightLineDistance = new LineString([[x, this.canvas.height], extraData.arcPointStart]).getLength();
						totalDistance = straightLineDistance;
						quadraticBezierLength = this.quadraticBezierLength(
							{x: extraData.arcPointStart[0], y: extraData.arcPointStart[1]},
							{x: extraData.arcPointEnd[0], y: extraData.arcPointEnd[1]},
							{x: extraData.controlPoint[0], y: extraData.controlPoint[1]}
						);
						var quadraticBezierExists = false;
						if (quadraticBezierLength) {
							quadraticBezierExists = true;
							totalDistance += quadraticBezierLength;
						} else {
							totalDistance += new LineString([extraData.arcPointStart, extraData.arcPointEnd]).getLength();
						}
						distanceFrom = totalDistance * params.restrictedDistanceFrom;
						if (!quadraticBezierExists || (distanceFrom <= straightLineDistance)) {
							y = this.canvas.height - distanceFrom;
						} else {
							restrictedSignPointCoordinates = this.getQuadraticXY(
								(distanceFrom - straightLineDistance) / (totalDistance - straightLineDistance),
								extraData.arcPointStart[0],
								extraData.arcPointStart[1],
								extraData.controlPoint[0],
								extraData.controlPoint[1],
								extraData.arcPointEnd[0],
								extraData.arcPointEnd[1]
							);
							x = restrictedSignPointCoordinates.x;
							y = restrictedSignPointCoordinates.y;
						}
					}
				} else if (type == "side") {
					var pointsMod = [];
					if ([0].indexOf(params.headAngle) == -1) {
						if (params.sideArrowType == "smooth" && (params.headAngle == 90 || params.headAngle == 135)) {
							quadraticBezierLength = this.quadraticBezierLength(
								{x: extraData.arcPointStart[0], y: extraData.arcPointStart[1]},
								{x: extraData.arcPointEnd[0], y: extraData.arcPointEnd[1]},
								{x: extraData.controlPoint[0], y: extraData.controlPoint[1]}
							);
							if (quadraticBezierLength) {
								// BIG FIXME! Man rodos, kad čia nelabai sąmoningai paskaičiuoja taško vietą...
								distanceFrom = quadraticBezierLength * params.restrictedDistanceFrom;
								restrictedSignPointCoordinates = this.getQuadraticXY(
									distanceFrom / quadraticBezierLength,
									extraData.arcPointStart[0],
									extraData.arcPointStart[1],
									extraData.controlPoint[0],
									extraData.controlPoint[1],
									extraData.arcPointEnd[0],
									extraData.arcPointEnd[1]
								);
								x = restrictedSignPointCoordinates.x;
								y = restrictedSignPointCoordinates.y;
							} else {
								pointsMod.push(extraData.arcPointStart);
								pointsMod.push(extraData.arcPointEnd);
								restrictedSignPointCoordinates = this.getRestrictedSignPointCoordinates(pointsMod, params.restrictedDistanceFrom);
								x = restrictedSignPointCoordinates.x;
								y = restrictedSignPointCoordinates.y;
							}
						} else {
							if (params.neckRounded) {
								if (extraData.points) {
									// TODO! Gal kažką protingesnio sugalvoti? Nes dabar tokia pati logika, kaip ir neapvalinto kaklo atveju...
									extraData.points.forEach(function(point){
										if (point.point) {
											pointsMod.push(point.point);
										} else if (point.curve) {
											pointsMod.push(point.curve.control);
										}
									});
									restrictedSignPointCoordinates = this.getRestrictedSignPointCoordinates(pointsMod, params.restrictedDistanceFrom);
									x = restrictedSignPointCoordinates.x;
									y = restrictedSignPointCoordinates.y;
								}
							} else {
								if (extraData.points) {
									extraData.points.forEach(function(point){
										pointsMod.push(point.point);
									});
									restrictedSignPointCoordinates = this.getRestrictedSignPointCoordinates(pointsMod, params.restrictedDistanceFrom);
									x = restrictedSignPointCoordinates.x;
									y = restrictedSignPointCoordinates.y;
								}
							}
						}
					}
				}
				if (y) {
					this.ctx.save();
					if (params.restrictedCargo) {
						this.ctx.fillStyle = "white";
						this.ctx.beginPath();
						this.ctx.arc(x, y, this.arrowParams.arrowWidth, 0, 2 * Math.PI, true);
						this.ctx.fill();
						this.ctx.lineWidth = 2;
						this.ctx.strokeStyle = "red";
						this.ctx.arc(x, y, this.arrowParams.arrowWidth, 0, 2 * Math.PI, true);
						this.ctx.stroke();
						this.ctx.beginPath();
						this.ctx.strokeStyle = this.ctx.fillStyle = "black";
						this.ctx.fillRect(x - 2, y - 5, 10, 7);
						this.ctx.lineWidth = 1;
						this.ctx.beginPath();
						this.ctx.arc(x - 5, y + 4, 2, 0, 2 * Math.PI, true);
						this.ctx.stroke();
						this.ctx.beginPath();
						this.ctx.arc(x + 6, y + 4, 2, 0, 2 * Math.PI, true);
						this.ctx.stroke();
						this.ctx.moveTo(x - 2, y + 4);
						this.ctx.lineTo(x + 3, y + 4);
						this.ctx.stroke();
						this.ctx.fillRect(x - 8, y - 3, 5, 4);
					} else {
						this.ctx.fillStyle = "red";
						this.ctx.beginPath();
						this.ctx.arc(x, y, this.arrowParams.arrowWidth, 0, 2 * Math.PI, true);
						this.ctx.fill();
						this.ctx.lineWidth = 1;
						this.ctx.strokeStyle = "white";
						this.ctx.arc(x, y, this.arrowParams.arrowWidth, 0, 2 * Math.PI, true);
						this.ctx.stroke();
						this.ctx.beginPath();
						this.ctx.lineWidth = 7;
						this.ctx.moveTo(x - 10, y);
						this.ctx.lineTo(x + 10, y);
						this.ctx.stroke();
					}
					this.ctx.restore();
				}
			},
			drawShuttleIfNeeded: function(x, arrowDirection){
				if (this.params.simple.shuttle) {
					var y = this.canvas.height - this.arrowParams.x / 2;
					if (arrowDirection == "down") {
						y = this.arrowParams.x / 2;
					}
					this.ctx.save();
					// Gal būtų galima panaudoti -> require("@/assets/custom-sc-elements/bus.png")
					this.ctx.fillStyle = CommonHelper.colors.sc.blue;
					this.ctx.beginPath();
					this.ctx.rect(x - 25, y - 13, 50, 26);
					this.ctx.fill();
					this.ctx.fillStyle = "white";
					this.ctx.beginPath();
					this.ctx.moveTo(x - 19, y - 4);
					this.ctx.lineTo(x - 17, y - 7);
					this.ctx.lineTo(x + 17, y - 7);
					this.ctx.lineTo(x + 19, y - 4);
					this.ctx.lineTo(x + 19, y + 6);
					this.ctx.lineTo(x - 19, y + 6);
					this.ctx.closePath();
					this.ctx.fill();
					this.ctx.fillStyle = CommonHelper.colors.sc.blue;
					this.ctx.beginPath();
					this.ctx.arc(x - 12, y + 6, 5, 0, 2 * Math.PI, true);
					this.ctx.fill();
					this.ctx.beginPath();
					this.ctx.arc(x + 12, y + 6, 5, 0, 2 * Math.PI, true);
					this.ctx.fill();
					this.ctx.fillStyle = "white";
					this.ctx.beginPath();
					this.ctx.arc(x - 12, y + 6, 3, 0, 2 * Math.PI, true);
					this.ctx.fill();
					this.ctx.beginPath();
					this.ctx.arc(x + 12, y + 6, 3, 0, 2 * Math.PI, true);
					this.ctx.fill();
					this.ctx.fillStyle = CommonHelper.colors.sc.blue;
					this.ctx.beginPath();
					this.ctx.arc(x - 12, y + 6, 1, 0, 2 * Math.PI, true);
					this.ctx.fill();
					this.ctx.beginPath();
					this.ctx.arc(x + 12, y + 6, 1, 0, 2 * Math.PI, true);
					this.ctx.fill();
					this.ctx.beginPath();
					this.ctx.rect(x - 9, y - 4, 5, 4);
					this.ctx.fill();
					this.ctx.beginPath();
					this.ctx.rect(x - 3, y - 4, 5, 4);
					this.ctx.fill();
					this.ctx.beginPath();
					this.ctx.rect(x + 4, y - 4, 6, 4);
					this.ctx.fill();
					this.ctx.restore();
				}
			},
			renderCanvasToSave: function(returnCanvasCopy){
				var boundingBox = this.contextBoundingBox(this.ctx),
					destCanvas = this.$refs.canvasToSave;
				if (boundingBox.w > 0 && boundingBox.h > 0) {
					destCanvas.width = boundingBox.w;
					if (this.params.mainType == "522") {
						boundingBox.h += (this.ctx.canvas.height - 1 - boundingBox.maxY); // Čia toks nelabai gražus sprendimas? Bet tas `522` toks išskirtinis... Pati vektorinė matoma figūra prasideda ne nuo apačios...
					}
					destCanvas.height = boundingBox.h;
				} else {
					destCanvas.width = 0;
					destCanvas.height = 0;
				}
				destCanvas.style.background = "red";
				var destCtx = destCanvas.getContext("2d"),
					maxHeight = this.arrowParams.x + this.arrowParams.arrowHeadHeight,
					doScaleIfNeeded = true;
				if (doScaleIfNeeded && (destCanvas.height > maxHeight)) {
					var scaleRatio = maxHeight / destCanvas.height;
					destCanvas.width = boundingBox.w * scaleRatio;
					destCanvas.height = boundingBox.h * scaleRatio;
					destCtx.drawImage(this.canvas, - boundingBox.x * scaleRatio, - boundingBox.y * scaleRatio, this.canvas.width * scaleRatio, this.canvas.height * scaleRatio);
				} else {
					destCtx.drawImage(this.canvas, - boundingBox.x, - boundingBox.y);
				}
				if (returnCanvasCopy) {
					// Mintis kažką tokio naudoti: https://stackoverflow.com/questions/4405336/how-to-copy-contents-of-one-canvas-to-another-canvas-locally
					var canvasCopy = document.createElement("canvas");
					canvasCopy.width = destCanvas.width;
					canvasCopy.height = destCanvas.height;
					var canvasCopyCtx = canvasCopy.getContext("2d");
					canvasCopyCtx.drawImage(destCanvas, 0, 0, canvasCopy.width, canvasCopy.height); // Hmmm... Gal čia reiktų sumažinti piešinuką iki tokio dydžio, kad būtų 1:1 tinkamas StreetSignSymbolContent5XXInteractive'ui?..
					// Gal čia dar kas https://stackoverflow.com/questions/17861447/html5-canvas-drawimage-how-to-apply-antialiasing (?)
					return canvasCopy;
				}
			},
			getDataUrl: function(){
				if (!this.test && !this.modRefItem) {
					this.renderCanvasToSave();
				}
				var canvas = this.$refs.canvasToSave;
				if (canvas) {
					return canvas.toDataURL();
				}
			},
			getAllData: function(){
				var data = {},
					params;
				if (this.params.mainType == "simple") {
					// TODO? Galima būtų saugoti ne visą info, o tik tų rodyklių info, kurios yra pažymėtos arba buvo pažymėtos/atžymėtos? Bet realiai didelės naudos nebūtų...
					params = this.params.simple;
				} else if (this.params.mainType == "round") {
					params = this.params.round;
				} else if (this.params.mainType == "down") {
					params = {};
					if (this.dataType == "shuttle") {
						if (this.params.simple) {
							params.shuttle = this.params.simple.shuttle;
						}
					}
				} else if (this.params.mainType == "522") {
					params = {};
				}
				if (params) {
					data.params = params;
					data.dataURL = this.getDataUrl();
					data.subtype = this.getSubtype(this.params.mainType, params);
				}
				return data;
			},
			getSubtype: function(mainType, params){
				var subtype = mainType;
				if (subtype != "round") {
					if (params.arrows) {
						var arrowTypes = [];
						params.arrows.forEach(function(arrow){
							if (arrow.params && arrow.params.checked) {
								if (arrowTypes.indexOf(arrow.type) == -1) {
									arrowTypes.push(arrow.type);
								}
							}
						});
						if (arrowTypes.length) {
							arrowTypes.sort();
							subtype = arrowTypes.join("-");
						}
					}
				}
				return subtype;
			},
			contextBoundingBox: function(ctx, alphaThreshold){
				// !!! https://stackoverflow.com/questions/9852159/calculate-bounding-box-of-arbitrary-pixel-based-drawing
				if (alphaThreshold === undefined) {
					alphaThreshold = 5;
				}
				var w = ctx.canvas.width,
					h = ctx.canvas.height,
					data = ctx.getImageData(0, 0, w, h).data,
					minX = w,
					maxX = 0,
					minY = h,
					maxY = 0;
				for (var y = 0; y < h; y++) {
					for (var x = 0; x < w; x++) {
						if (data[y * w * 4 + x * 4 + 3]) {
							minX = Math.min(minX, x);
							maxX = Math.max(maxX, x);
							minY = Math.min(minY, y);
							maxY = y;
							x = maxX;
						}
					}
				}
				return {
					x: minX,
					y: minY,
					maxX: maxX,
					maxY: maxY,
					w: maxX - minX + 1,
					h: maxY - minY + 1
				};
			},
			determineIfSimpleArrowAdditionalUpUnavailable: function(params){
				var upArrowsCount = 0;
				if (params.simple.arrows) {
					params.simple.arrows.forEach(function(item){
						if (item.type == "up") {
							upArrowsCount += 1;
						}
					});
				}
				params.simple.simpleArrowAdditionalUpUnavailable = (upArrowsCount >= 3);
			},
			quadraticBezierLength: function(p0, p1, p2){
				// https://gist.github.com/tunght13488/6744e77c242cc7a94859
				var ax = p0.x - 2 * p1.x + p2.x;
				var ay = p0.y - 2 * p1.y + p2.y;
				var bx = 2 * p1.x - 2 * p0.x;
				var by = 2 * p1.y - 2 * p0.y;
				var A = 4 * (ax * ax + ay * ay);
				var B = 4 * (ax * bx + ay * by);
				var C = bx * bx + by * by;
				var Sabc = 2 * Math.sqrt(A+B+C);
				var A_2 = Math.sqrt(A);
				var A_32 = 2 * A * A_2;
				var C_2 = 2 * Math.sqrt(C);
				var BA = B / A_2;
				return (A_32 * Sabc + A_2 * B * (Sabc - C_2) + (4 * C * A - B * B) * Math.log((2 * A_2 + BA + Sabc) / (BA + C_2))) / (4 * A_32);
			},
			getQuadraticXY: function(t, sx, sy, cp1x, cp1y, ex, ey){
				// http://www.independent-software.com/determining-coordinates-on-a-html-canvas-bezier-curve.html
				return {
					x: (1 - t) * (1 - t) * sx + 2 * (1 - t) * t * cp1x + t * t * ex,
					y: (1 - t) * (1 - t) * sy + 2 * (1 - t) * t * cp1y + t * t * ey
				};
			},
			getRestrictedSignPointCoordinates: function(points, restrictedDistanceFrom){
				var totalDistance = new LineString(points).getLength(),
					distanceFrom = totalDistance * restrictedDistanceFrom,
					distanceToThisSegment = 0,
					distanceBetweenPoints,
					coordinates = {};
				for (var i = 0; i < points.length - 1; i++) {
					distanceBetweenPoints = new LineString([points[i], points[i + 1]]).getLength();
					if (distanceFrom >= distanceToThisSegment && distanceFrom <= (distanceToThisSegment + distanceBetweenPoints)) {
						// Turime du taškus, tarp kurių reikia rasti norimo atstumo taško koordinates...
						var d = distanceFrom - distanceToThisSegment,
							a = Math.atan2(points[i + 1][0] - points[i][0], points[i + 1][1] - points[i][1]);
						coordinates.x = points[i][0] + d * Math.sin(a);
						coordinates.y = points[i][1] + d * Math.cos(a);
						break;
					}
					distanceToThisSegment += distanceBetweenPoints;
				}
				return coordinates;
			},
			restoreData: function(data, myData){
				data.params.mainType = myData.subtype;
				if (["round", "down", "522"].indexOf(data.params.mainType) == -1) {
					data.params.mainType = "simple";
				}
				if (myData.data) {
					var initialParams = JSON.parse(myData.data);
					if (initialParams) {
						if (myData.subtype == "round") {
							data.params.round = initialParams.params;
						} else if (["down", "522"].indexOf(data.params.mainType) != -1) {
							if (this.dataType == "shuttle") {
								data.params.simple = initialParams.params;
							}
						} else {
							data.params.simple = initialParams.params;
						}
					}
				}
			},
			setData: function(data){
				// TODO... Gal nustatyti visą pradinį originalųjį `this.data`?.. Gal jis kartais buvo perrašytas?..
				this.restoreData(this, data);
			}
		},

		watch: {
			params: {
				deep: true,
				immediate: true,
				handler: function(){
					if (this.alreadyMounted) {
						this.renderCanvas();
					}
				}
			}
		}
	};
</script>

<style scoped>
	canvas {
		vertical-align: bottom;
	}
	.divider {
		border-right: 1px dashed #aaaaaa;
	}
	canvas.bordered {
		border: 1px solid #dddddd;
	}
</style>