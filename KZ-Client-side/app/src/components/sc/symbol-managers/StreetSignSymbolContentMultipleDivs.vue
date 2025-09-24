<template>
	<div>
		<template v-if="dataReady">
			<div>
				<div class="mb-3">Simbolio komponentai:</div>
				<template v-for="(sectionRows, i) in sections">
					<StreetSignSymbolContentMultipleDivsSection
						:data="data"
						:advanced="advanced"
						:sectionId="i"
						:rows="sectionRows"
						:test="test"
						:getEmptyRow="getEmptyRow"
						:modSection="modSection"
						:colCount="colCount"
						:updateRows="updateRows"
						:getBackgroundColor="getBackgroundColor"
						:maxRowsCount="maxRowsCount"
						:plain="plain"
						:key="i"
						:class="i ? 'mt-4' : null"
					/>
				</template>
				<div class="mt-3" v-if="advanced">
					<v-divider class="my-4" />
					<v-btn
						color="blue darken-1"
						text
						small
						v-on:click="addSection"
						outlined
						class="mr-1"
						disabled
					>
						Pridėti skiltį
					</v-btn>
				</div>
			</div>
			<div :class="[noContent ? 'd-none' : 'mt-7 mb-3']">Sugeneruotas simbolio piešinukas:</div>
			<canvas ref="canvas" :class="['d-block', noContent ? 'd-invisible' : null]"></canvas>
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
	import StreetSignSymbolContentMultipleDivsSection from "./StreetSignSymbolContentMultipleDivsSection";
	import StreetSignsSymbolsManagementHelper from "../../helpers/StreetSignsSymbolsManagementHelper";
	import Vue from "vue";

	export default {
		data: function(){
			var sections = [],
				colCount = this.advanced ? 4 : 3,
				maxRowsCount = 6,
				plain = false,
				initialRows = 1,
				sectionRows = [],
				cents102050Case = false;
			if (["840", "921", "922", "923", "924"].indexOf(this.data.type) != -1) {
				cents102050Case = true;
			}
			if (this.data.type == "829" || cents102050Case) {
				colCount = 2;
				maxRowsCount = 2;
				plain = true;
				initialRows = 2;
			} else if (this.data.type == "623") {
				colCount = 4;
			} else if (this.data.type == "622") {
				maxRowsCount = 7;
			}
			for (var i = 1; i <= initialRows; i++) {
				sectionRows.push(this.getEmptyRow(colCount));
			}
			if (this.data.type == "622") {
				if (sectionRows[0] && sectionRows[0][0]) {
					sectionRows[0][0].type = "text";
				}
			} else if (this.data.type == "829" || cents102050Case) {
				sectionRows.forEach(function(row){
					row.forEach(function(cell){
						cell.type = "text";
					});
				});
			}
			sections.push(sectionRows);
			var data = {
				dataReady: false,
				colCount: colCount,
				sections: sections,
				test: false,
				overlapArrowLinesBy: 1,
				noContent: true,
				maxRowsCount: maxRowsCount,
				plain: plain,
				cents102050Case: cents102050Case
			};
			if (this.data.mode == "edit") {
				if (this.data.data) {
					var initialParams = JSON.parse(this.data.data);
					if (initialParams) {
						if (initialParams.sections) {
							initialParams.sections.forEach(function(section){
								section.forEach(function(row){
									row.forEach(function(element){
										if (element.type && element.type != "text") {
											element.type = StreetSignsSymbolsManagementHelper.getRawElementData(element.type, true);
										}
									});
								});
								this.modSection(section);
							}.bind(this));
							data.sections = initialParams.sections;
						}
					}
				}
			}
			if (this.data.type == "829" || cents102050Case) {
				if (data.sections[0]) {
					data.sections[0].forEach(function(row){
						row.forEach(function(cell, i){
							if (i === 0) {
								cell.placeholder = "Dienos";
								cell.valueType = "d";
								cell.alignment = "right";
								if (cents102050Case) {
									cell.alignment = "left";
								}
							} else if (i === 1) {
								cell.placeholder = "Valandos";
								cell.valueType = "h";
								cell.alignment = "left";
							}
						}.bind(this));
					}.bind(this));
				}
			}
			return data;
		},

		props: {
			data: Object,
			advanced: Boolean
		},

		components: {
			StreetSignSymbolContentMultipleDivsSection
		},

		mounted: function(){
			if (this.data && this.data.vss) {
				if (this.sections && this.sections[0] && this.sections[0][0] && this.sections[0][0][0] && this.sections[0][0][0].type == "text") {
					var sections = this.sections.slice();
					if (this.data.textValue) {
						this.setInitValue(sections[0], this.data.textValue);
						this.dataReady = true;
					} else {
						StreetSignsSymbolsManagementHelper.findFeature({
							globalId: this.data.vss
						}, this.$store).then(function(feature){
							if (feature.attributes) {
								this.setInitValue(sections[0], feature.attributes[CommonHelper.symbolTextFieldName]);
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
			} else {
				this.dataReady = true;
			}
		},

		methods: {
			getEmptyRow: function(colCount){
				var row = [];
				for (var i = 1; i <= colCount; i++) {
					row.push({});
				}
				return row;
			},
			addSection: function(){
				this.$set(this.sections, this.sections.length, [this.getEmptyRow(this.colCount)]);
			},
			modSection: function(section){
				var skipCells = {};
				section.forEach(function(row, i){
					row.forEach(function(element, j){
						if (skipCells[i] && skipCells[i][j]) {
							element.skip = true;
						} else {
							element.skip = false;
							if (element.rowsToMerge > 1) {
								for (var k = 1; k < element.rowsToMerge; k++) {
									if (!skipCells[i + k]) {
										skipCells[i + k] = {};
									}
									skipCells[i + k][j] = true;
								}
							}
							if (element.colsToMerge > 1) {
								var mLength = element.rowsToMerge > 1 ? element.rowsToMerge : 1;
								for (var l = 1; l < element.colsToMerge; l++) {
									for (var m = 0; m < mLength; m++) {
										if (!skipCells[i]) {
											skipCells[i] = {};
										}
										skipCells[i + m][j + l] = true;
									}
								}
							}
						}
					});
				}.bind(this));
				if (this.test) {
					var newElements = JSON.parse(JSON.stringify(section));
					newElements.forEach(function(row, i){
						row.forEach(function(element, j){
							console.log("ROW: ", i, "COL: ", j, "SKIP: ", element.skip);
						});
					}.bind(this));
				}
			},
			updateRows: function(rows, sectionId){
				this.$set(this.sections, sectionId, rows);
			},
			renderCanvas: function(){
				var canvas = this.$refs.canvas;
				if (canvas) {
					var ctx = canvas.getContext("2d"),
						scale = this.cents102050Case ? 0.65 : 1,
						strokeWidth = 0 * scale,
						padding = 3 * scale,
						canvasHeight = 0,
						rowWidth,
						rowHeight,
						textInfo,
						maxRowWidth = 0,
						elementsToDraw = [],
						colsWidth = {},
						colWidth,
						defaultRowHeight = 35 * scale,
						spaceBetweenCells = 3 * scale,
						rowHasContent,
						font = "bold " + (20 * scale) + "px Arial",
						cellXPadding = 20 * scale,
						backgroundColor = this.advanced ? CommonHelper.colors.sc.blue : "white",
						textColor = this.advanced ? "white" : "black",
						smallerFont;
					if (this.data.type == "829" || this.cents102050Case) {
						spaceBetweenCells = 0;
						cellXPadding = 5 * scale;
						var textHeight = (30 * 0.8) * scale;
						font = textHeight + "px Arial"; // Išlaikomas toks pat aukštis kaip ir `828` atveju...
						smallerFont = (textHeight * 0.7) + "px Arial";
						strokeWidth = 2 * scale;
						defaultRowHeight = textHeight + 2 * (3 * scale + strokeWidth);
						if (this.cents102050Case) {
							defaultRowHeight = textHeight;
							cellXPadding = 3 * scale;
						}
					}
					ctx.font = font;
					this.sections[0].forEach(function(row, i){
						rowWidth = 0;
						rowHeight = 0;
						rowHasContent = false;
						row.forEach(function(cell, j){
							colWidth = 0;
							if (!cell.skip) {
								if (cell.type == "text") {
									if (cell.value) {
										if (this.data.type == "829") {
											if (cell.valueType == "h") {
												ctx.font = smallerFont;
											} else {
												ctx.font = font;
											}
										}
										textInfo = ctx.measureText(this.modCellValue(cell.value));
										colWidth = textInfo.width + 2 * cellXPadding;
										if ((this.data.type == "829" || this.cents102050Case) && j) {
											colWidth -= cellXPadding; // Esmė, kad besiribojantys elementai neturi kraštinių, tad tarpas tarp jų neturi būti toks jau didelis...
										}
										rowHasContent = true;
									}
								} else {
									if (cell.type) {
										if (cell.type.rawElementData) {
											colWidth = cell.type.symbol.width * (defaultRowHeight / cell.type.symbol.height) * scale;
										} else {
											// Atseit jei tipas nėra text'as ir neturi `rawElementData`, tai bus paprasta vektorinė rodyklė...
											colWidth = 40 * scale; // Atseit rodyklės elemento plotis... FIXME! Paskaičiuoti pagal rodyklės tipą?..
										}
										rowHasContent = true;
									}
								}
							}
							if (!colsWidth[j] || colsWidth[j] < colWidth) {
								colsWidth[j] = colWidth;
							}
							rowWidth += colsWidth[j];
						}.bind(this));
						if (!rowHasContent) {
							// Jei eilutėje nėra turinio, bet kuris nors stulpelis dalyvauja `merge`, tai eilutę reikia įtraukti...
							rowHasContent = this.isRowMergedWithPreviousRow(0, i);
						}
						if (rowHasContent) {
							rowHeight = defaultRowHeight; // Dabar fiksuotas... Ar gerai taip?..
							if (rowWidth > maxRowWidth) {
								maxRowWidth = rowWidth;
							}
							elementsToDraw.push(row);
						}
						canvasHeight += rowHeight;
					}.bind(this));
					var colsCount = 0;
					for (var key in colsWidth) {
						if (colsWidth[key]) {
							colsCount += 1;
						}
					}
					if (maxRowWidth && canvasHeight) {
						canvas.width = maxRowWidth + (strokeWidth + padding) * 2 + (colsCount - 1) * spaceBetweenCells;
						canvas.height = canvasHeight + (elementsToDraw.length - 1) * spaceBetweenCells + (strokeWidth + padding) * 2;
						if (this.cents102050Case) {
							if (canvas.width < 200 * scale) {
								canvas.width = 200 * scale;
							}
							canvas.height = 102 * scale; // Realiai `76` centams + `XX` tekstui
						}
					} else {
						if (this.cents102050Case) {
							canvas.width = 156 * scale;
							canvas.height = 76 * scale;
						} else {
							canvas.width = 0;
							canvas.height = 0;
						}
					}
					var bottomColoredTableHeight = 35;
					if (canvas.width) {
						this.noContent = false;
						ctx.fillStyle = backgroundColor;
						if (["921", "922", "923", "924"].indexOf(this.data.type) != -1) {
							canvas.height += bottomColoredTableHeight * scale;
						}
					} else {
						ctx.fillStyle = "gray";
						this.noContent = true;
					}
					ctx.fillRect(0, 0, canvas.width, canvas.height);
					if (this.data.type == "829" || this.cents102050Case) {
						ctx.fillStyle = "black";
						ctx.fillRect(0, 0, canvas.width, canvas.height);
						ctx.fillStyle = "white";
						ctx.fillRect(strokeWidth, strokeWidth, canvas.width - 2 * strokeWidth, canvas.height - 2 * strokeWidth);
						if (["921", "922", "923", "924"].indexOf(this.data.type) != -1) {
							// Čia kuriamas tas apatinis spalvotas "X zonos" stačiakampis...
							ctx.save();
							ctx.beginPath();
							ctx.fillStyle = this.get92XBottomColoredTableColor();
							ctx.fillRect(0, canvas.height - bottomColoredTableHeight * scale, canvas.width, bottomColoredTableHeight * scale);
							ctx.beginPath();
							ctx.strokeStyle = "black";
							ctx.lineWidth = strokeWidth;
							ctx.moveTo(strokeWidth / 2, canvas.height - bottomColoredTableHeight * scale);
							ctx.lineTo(canvas.width - strokeWidth / 2, canvas.height - bottomColoredTableHeight * scale);
							ctx.lineTo(canvas.width - strokeWidth / 2, canvas.height - strokeWidth / 2);
							ctx.lineTo(strokeWidth / 2, canvas.height - strokeWidth / 2);
							ctx.closePath();
							ctx.stroke();
							ctx.fillStyle = this.data.type == "924" ? "black" : "white";
							ctx.textAlign = "center";
							ctx.textBaseline = "middle";
							ctx.font = (26 * scale) + "px Arial";
							ctx.fillText(this.get92XBottomColoredTableText(), canvas.width / 2, canvas.height - bottomColoredTableHeight / 2 * scale);
							ctx.restore();
						}
					}
					if (this.cents102050Case) {
						ctx.save();
						ctx.textAlign = "center";
						ctx.textBaseline = "middle";
						var centX = canvas.width - scale * 38,
							centY = scale * 38;
						ctx.beginPath();
						ctx.arc(centX, centY, 28 * scale, 0, 2 * Math.PI, true);
						ctx.fillStyle = "white";
						ctx.fill();
						ctx.stroke();
						ctx.fillStyle = "black";
						ctx.font = 34 * scale + "px Arial";
						ctx.fillText("50", centX, centY + 3 * scale, 28 * scale);
						centX = canvas.width - 86 * scale;
						ctx.beginPath();
						ctx.arc(centX, centY, 24 * scale, 0, 2 * Math.PI, true);
						ctx.fillStyle = "white";
						ctx.fill();
						ctx.stroke();
						ctx.fillStyle = "black";
						ctx.font = 26 * scale + "px Arial";
						ctx.fillText("20", centX, centY + 2 * scale, 24 * scale);
						centX = canvas.width - 126 * scale;
						ctx.beginPath();
						ctx.arc(centX, centY, 20 * scale, 0, 2 * Math.PI, true);
						ctx.fillStyle = "white";
						ctx.fill();
						ctx.stroke();
						ctx.fillStyle = "black";
						ctx.font = 18 * scale + "px Arial";
						ctx.fillText("10", centX, centY + 2 * scale, 20 * scale);
						ctx.restore();
					}
					var elementX,
						elementY,
						cellHeight,
						rowsToMerge,
						colsToMerge,
						arrowLength = 28 * scale,
						arrowHeadHeight = 14 * scale;
					ctx.textBaseline = "middle";
					ctx.textAlign = "center";
					ctx.font = font;
					ctx.lineWidth = 7 * scale;
					if (this.cents102050Case) {
						if (elementsToDraw.length == 2) {
							// Nelabai gražus sprendimas... Esmė, kad reikia sumažinti elementus... Realiai kelio ženkluose taip ir yra, kad tekstai per dvi eilutes yra mažesnio šrifto...
							font = smallerFont;
							defaultRowHeight = (textHeight * 0.7);
							for (var k in colsWidth) {
								if (colsWidth[k]) {
									colsWidth[k] *= 0.7;
								}
							}
						}
					}
					elementsToDraw.forEach(function(row, i){
						elementX = strokeWidth + padding;
						row.forEach(function(cell, j){
							colWidth = colsWidth[j]; // TODO! Ne visada taip?..
							if (!cell.skip) {
								ctx.fillStyle = cell.backgroundColor ? this.getBackgroundColor(cell.backgroundColor) : backgroundColor;
								elementY = strokeWidth + padding + i * (defaultRowHeight + spaceBetweenCells);
								if (this.cents102050Case) {
									// Stebėjimais parinkta Y vieta. Esmė ta, kad data/laikas būtų kairėje apačioje.
									if (elementsToDraw.length == 1) {
										elementY += 66 * scale;
									} else if (elementsToDraw.length == 2) {
										elementY += 59 * scale;
									}
								}
								cellHeight = defaultRowHeight;
								if (cell.rowsToMerge) {
									rowsToMerge = parseInt(cell.rowsToMerge);
									if (rowsToMerge > 1) {
										if ((i + rowsToMerge) > elementsToDraw.length) {
											rowsToMerge = elementsToDraw.length - i;
										}
										cellHeight += (defaultRowHeight + spaceBetweenCells) * (rowsToMerge - 1);
									}
								}
								if (cell.colsToMerge) {
									colsToMerge = parseInt(cell.colsToMerge);
									if (colsToMerge > 1) {
										if ((j + colsToMerge) > row.length) {
											colsToMerge = row.length - i;
										}
										colWidth = 0;
										for (var colI = j; colI < j + colsToMerge; colI++) {
											colWidth += colsWidth[colI];
											if (colsWidth[colI + 1] && ((colI + 1) < (j + colsToMerge))) {
												colWidth += spaceBetweenCells;
											}
										}
									}
								}
								ctx.fillRect(elementX, elementY, colWidth, cellHeight);
								ctx.fillStyle = ctx.strokeStyle = (cell.backgroundColor && cell.backgroundColor != "yellow") ? "white" : textColor; // Tekstui, rodyklėms...
								if (cell.type == "text") {
									if (cell.value) {
										ctx.textAlign = "center";
										var textX = elementX + colWidth / 2;
										if (cell.alignment == "left") {
											ctx.textAlign = "left";
											textX = elementX + cellXPadding;
											if ((this.data.type == "829" || this.cents102050Case) && colWidth) {
												textX -= cellXPadding; // Esmė, kad besiribojantys elementai neturi kraštinių, tad tarpas tarp jų neturi būti toks jau didelis...
											}
										} else if (cell.alignment == "right") {
											ctx.textAlign = "right";
											textX = elementX + colWidth - cellXPadding;
										}
										var textY = elementY + cellHeight / 2;
										if (!this.cents102050Case && (cell.valueType == "h") && smallerFont) {
											ctx.font = smallerFont;
											textY += 1 * scale;
										} else {
											ctx.font = font;
										}
										var maxWidth;
										if (this.cents102050Case) {
											// TODO: galima nustatyi kokį nors `maxWidth`? Ar neaktualu?..
										}
										ctx.fillText(this.modCellValue(cell.value), textX, textY, maxWidth);
									}
								} else if (cell.type) {
									// BIG TODO: atsižvelgti į cell.alignment'us!!!
									if (cell.type.rawElementData) {
										if (cell.type.symbol) {
											if (cell.type.symbol.altSrc) {
												var currentElementX = elementX + colWidth / 2 - cell.type.symbol.width * scale / 2, // Reikia, nes piešinukas pasikrauna asinchroniškai...
													currentElementY = elementY + cellHeight / 2 - cell.type.symbol.height * scale / 2, // Reikia, nes piešinukas pasikrauna asinchroniškai...
													currentCell = cell;
												if (this.images && this.images[currentCell.type.symbol.altSrc]) {
													ctx.drawImage(this.images[currentCell.type.symbol.altSrc], currentElementX, currentElementY, currentCell.type.symbol.width * scale, currentCell.type.symbol.height * scale);
												} else {
													var image = new Image();
													image.crossOrigin = "Anonymous";
													image.onload = function(){
														if (!this.images) {
															this.images = {};
														}
														this.images[image.src] = image;
														ctx.drawImage(image, currentElementX, currentElementY, currentCell.type.symbol.width * scale, currentCell.type.symbol.height * scale);
													}.bind(this);
													image.onerror = function(){
														// ...
													}.bind(this);
													image.src = currentCell.type.symbol.altSrc;
												}
											}
										}
									} else {
										var arrowAngle = this.getArrowAngle(cell.type);
										var arrowCenterCoordinates = {
											x: elementX + colWidth / 2,
											y: elementY + cellHeight / 2
										};
										var arrowCoordinates = [{
											x: arrowCenterCoordinates.x + Math.cos(arrowAngle) * arrowLength / 2,
											y: arrowCenterCoordinates.y + Math.sin(arrowAngle) * arrowLength / 2
										}, {
											x: arrowCenterCoordinates.x - Math.cos(arrowAngle) * (arrowLength / 2 - arrowHeadHeight),
											y: arrowCenterCoordinates.y - Math.sin(arrowAngle) * (arrowLength / 2 - arrowHeadHeight)
										}];
										ctx.beginPath();
										ctx.moveTo(arrowCoordinates[0].x, arrowCoordinates[0].y);
										ctx.lineTo(arrowCoordinates[1].x, arrowCoordinates[1].y);
										ctx.stroke();
										var arrowHeadAngle = this.getArrowHeadAngle(cell.type);
										CommonHelper.drawArrowHead(arrowCoordinates[1].x + this.overlapArrowLinesBy * Math.cos(arrowAngle), arrowCoordinates[1].y + this.overlapArrowLinesBy * Math.sin(arrowAngle), arrowHeadAngle, 18 * scale, arrowHeadHeight, ctx);
									}
								}
							}
							colWidth = colsWidth[j]; // Būtina nustatyti iš naujo, nes merge'inami stulpeliai sujaukia situaciją...
							elementX += colWidth;
							if (colWidth) {
								elementX += spaceBetweenCells;
							}
						}.bind(this));
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
				var sections = JSON.parse(JSON.stringify(this.sections));
				if (sections) {
					sections.forEach(function(section){
						section.forEach(function(row){
							row.forEach(function(element){
								delete element.skip;
							});
						});
						section.forEach(function(row){
							row.forEach(function(element){
								if (element.type && element.type != "text") {
									if (element.type.rawElementData) {
										element.type = element.type.id;
									}
								}
								if (!element.rowsToMerge) {
									delete element.rowsToMerge;
								}
								if (!element.colsToMerge) {
									delete element.colsToMerge;
								}
								delete element.placeholder;
								delete element.valueType;
							});
						});
					});
				}
				var data = {
					sections: sections,
					dataURL: this.getDataUrl()
				};
				return data;
			},
			isRowMergedWithPreviousRow: function(sectionId, rowI){
				var rowsInMerge = {},
					rowsToMerge;
				this.sections[sectionId].forEach(function(row, i){
					row.forEach(function(element){
						if (element.rowsToMerge) {
							rowsToMerge = parseInt(element.rowsToMerge);
							for (var k = i; k < i + rowsToMerge; k++) {
								rowsInMerge[k] = true;
							}
						}
					});
				}.bind(this));
				return Boolean(rowsInMerge[rowI]);
			},
			getArrowAngle: function(key){
				var arrowAngle = 0;
				switch (key) {
					case "arrow-left":
						arrowAngle = 0; // Galva: - 90
						break;
					case "arrow-top-left":
						arrowAngle = 45; // Galva: - 135
						break;
					case "arrow-up":
						arrowAngle = 90; // Galva: 180
						break;
					case "arrow-top-right":
						arrowAngle = 135; // Galva: 135
						break;
					case "arrow-right":
						arrowAngle = 180; // Galva: 90
						break;
				}
				arrowAngle = arrowAngle * Math.PI / 180;
				return arrowAngle;
			},
			getArrowHeadAngle: function(key){
				var arrowAngle = 0;
				switch (key) {
					case "arrow-left":
						arrowAngle = - 90;
						break;
					case "arrow-top-left":
						arrowAngle = - 135;
						break;
					case "arrow-up":
						arrowAngle = 180;
						break;
					case "arrow-top-right":
						arrowAngle = 135;
						break;
					case "arrow-right":
						arrowAngle = 90;
						break;
				}
				arrowAngle = arrowAngle * Math.PI / 180;
				return arrowAngle;
			},
			modCellValue: function(value){
				if (this.data.type == "829" || this.cents102050Case) {
					return value;
				}
				if (value) {
					value = value.toUpperCase();
				}
				return value;
			},
			getBackgroundColor: function(backgroundColor){
				return CommonHelper.colors.sc[backgroundColor] || backgroundColor;
			},
			setInitValue: function(section, val){
				if (this.cents102050Case && val) {
					// TODO: dar galima ir detaliau padaryti... Išskaidyti į dienas ir valandas...
					var v = val.split(";");
					if (v.length == 2 && section.length == 2 && section[1][0]) {
						Vue.set(section[0][0], "value", v[0]);
						Vue.set(section[1][0], "value", v[1]);
					} else {
						Vue.set(section[0][0], "value", val);
					}
				} else {
					Vue.set(section[0][0], "value", val);
				}
			},
			get92XBottomColoredTableColor: function(){
				var texts = {
					"921": CommonHelper.colors.sc.red,
					"922": CommonHelper.colors.sc.green,
					"923": CommonHelper.colors.sc.blue,
					"924": CommonHelper.colors.sc.yellow
				}
				return texts[this.data.type];
			},
			get92XBottomColoredTableText: function(){
				var texts = {
					"921": "R zona",
					"922": "Ž zona",
					"923": "M zona",
					"924": "G zona"
				}
				return texts[this.data.type];
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
			sections: {
				deep: true,
				immediate: true,
				handler: function(){
					this.renderCanvas();
				}
			}
		}
	};
</script>