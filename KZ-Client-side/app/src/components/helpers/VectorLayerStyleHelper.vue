<script>
	import CommonHelper from "./CommonHelper";
	import LineString from "ol/geom/LineString";
	import StreetSignsLayerStyleHelper from "./StreetSignsLayerStyleHelper";
	import {Circle as CircleStyle, Icon, Fill, RegularShape, Stroke, Style, Text} from "ol/style";
	import {intersect} from "mathjs";

	export default {
		getTaskColor: function(feature){
			var action = feature.get(CommonHelper.taskFeatureActionFieldName),
				color = "rgba(211, 211, 211, 0.3)";
			if (Number.isInteger(parseInt(action))) {
				if (action == CommonHelper.taskFeatureActionValues["add"]) {
					color = "rgba(0, 255, 0, 0.3)";
				} else if (action == CommonHelper.taskFeatureActionValues["delete"]) {
					color = "rgba(255, 0, 0, 0.3)";
				} else if (action == CommonHelper.taskFeatureActionValues["update"]) {
					color = "rgba(255, 255, 0, 0.3)";
				}
			}
			return color;
		},

		getVectorLayerStyle: function(service, layerInfo, myMap){
			var specificRenderingType = service.specificRenderingType,
				style = this.getCustomStyle(service, layerInfo, myMap, specificRenderingType);
			if (!style) {
				style = this.getDefaultStyle(service, layerInfo, myMap, specificRenderingType);
			}
			return style;
		},

		getCustomStyle: function(service, layerInfo, myMap, specificRenderingType){
			if (service.id == "street-signs") {
				var style = StreetSignsLayerStyleHelper.getStyle(layerInfo, myMap, specificRenderingType);
				return style;
			}
		},

		getDefaultStyle: function(service, layerInfo, myMap, specificRenderingType){
			var style;
			if (layerInfo.drawingInfo) {
				var renderer = layerInfo.drawingInfo.renderer,
					labelingInfo = layerInfo.drawingInfo.labelingInfo;
				if (renderer) {
					if (renderer.type == "uniqueValue") {
						if (renderer.field1) {
							var scaleToResolution = false;
							if (service.id == "street-signs") {
								scaleToResolution = true;
							}
							var styles = this.getStylesDict(renderer, scaleToResolution, labelingInfo),
								defaultStyle,
								rotationField,
								scaleType = "basic",
								defaultColor = "rgba(255, 0, 0, 0.5)";
							if (service.id == "street-signs") {
								scaleType = "fluid";
							} else if (service.id == "street-signs-vertical") {
								// scaleType = null; // Sprendimas specialiai Kaunui dėl serviso `bug`, kai gavosi per dideli imageData simboliai... Bet šis sprendimas mums nebeleidžia pritaikyti simboliams scale'inimo, nes nedraugauja "width" ir "height" su setScale() metodu...
							}
							if (layerInfo.geometryType == "esriGeometryPoint") {
								defaultStyle = new Style({
									image: new CircleStyle({
										radius: 5,
										fill: new Fill({
											color: defaultColor
										})	
									})
								});
								if (renderer.visualVariables) {
									renderer.visualVariables.forEach(function(visualVariable){
										if (visualVariable.type == "rotationInfo") {
											rotationField = visualVariable.valueExpression.replace("$feature.", "");
										}
									});
								}
							} else if (layerInfo.geometryType == "esriGeometryPolyline") {
								defaultStyle = new Style({
									stroke: new Stroke({
										width: 2,
										color: defaultColor
									})
								});
							} else if (layerInfo.geometryType == "esriGeometryPolygon") {
								defaultStyle = new Style({
									fill: new Fill({
										color: defaultColor
									})
								});
							}
							if (renderer.defaultSymbol) {
								defaultStyle = this.getStyleObject(renderer.defaultSymbol);
							}
							style = function(feature, resolution){
								var skipFeature = CommonHelper.checkIfSkipFeature(feature, myMap, layerInfo);
								if (!skipFeature) {
									var featureStyle,
										customStyle = false,
										extraPointBeneath;
									if (service.id == "street-signs-vertical") { // Tiksliau būtų sukonkretinti sąlygą...
										var uniqueSymbolId = feature.get(CommonHelper.customSymbolIdFieldName);
										if (uniqueSymbolId) {
											if (resolution < 0.05) { // ! Unikalius simbolius rodome tik stambiausiame mastelyje?! TODO: padaryti kažkur checkbox'ą, kur pasirenki kaip rodyti?
												// 1) Visada unifikuotais
												// 2) Visada unikaliais
												// 3) Stambiausiame m. — unikaliais, smulkesniuose — unifikuotais!
												featureStyle = new Style({
													image: new Icon({
														src: CommonHelper.getUniqueSymbolSrc(uniqueSymbolId, feature.get("unique-symbol-timestamp") || service.timestamp)
													})
												});
												customStyle = true;
												extraPointBeneath = true; // Tarkime, kad piešinukas serveryje nėra pasiekiamas... Kad kažką rodytų bent jau `vietoj jo`...
											}
										}
									}
									if (!customStyle) {
										var vals = [];
										if (renderer.field1) {
											vals.push(feature.get(renderer.field1));
										}
										if (renderer.field2) {
											vals.push(feature.get(renderer.field2));
										}
										if (renderer.field3) {
											vals.push(feature.get(renderer.field3));
										}
										featureStyle = styles[vals.join(",")];
										if (featureStyle) {
											if (featureStyle.fnc) {
												return featureStyle.fnc(feature, resolution, specificRenderingType);
											}
										} else {
											featureStyle = defaultStyle;
										}
									}
									this.modFeatureStyle(featureStyle, feature, resolution, rotationField, scaleType, customStyle);
									if (service.id == "street-signs-vertical") {
										featureStyle = this.getModFeatureStyleVertical(featureStyle, feature, resolution);
									}
									if (extraPointBeneath) {
										if (!Array.isArray(featureStyle)) {
											featureStyle = [featureStyle];
										}
										featureStyle.unshift(
											new Style({
												image: new CircleStyle({
													radius: 5,
													fill: new Fill({
														color: "red"
													})	
												})
											})
										);
									}
									if (specificRenderingType) {
										if (specificRenderingType == "task") {
											if (!Array.isArray(featureStyle)) {
												featureStyle = [featureStyle];
											}
											featureStyle.unshift(
												new Style({
													image: new CircleStyle({
														radius: 0.6 / resolution,
														fill: new Fill({
															color: this.getTaskColor(feature)
														})
													}),
													stroke: new Stroke({ // Pasikartojantis stilius kaip ir addSpecificRenderingIfNeeded() metode... TODO: šitą reikia push'inti?..
														width: StreetSignsLayerStyleHelper.getLineWidth(resolution) * 2,
														color: this.getTaskColor(feature)
													})
												})
											);
										}
									}
								}
								return featureStyle;
							}.bind(this);
						}
					} else if (renderer.type == "simple") {
						// Kol kas aktualu tik "kelių" servisui...
						if (renderer.symbol) {
							if (layerInfo.geometryType == "esriGeometryPolygon") {
								style = new Style({
									fill: this.getFill(renderer.symbol.style, renderer.symbol.color),
									stroke: this.getStroke(renderer.symbol.outline)
								});
							} else if (layerInfo.geometryType == "esriGeometryPolyline") {
								style = new Style({
									stroke: this.getStroke(renderer.symbol)
								});
							} else if (layerInfo.geometryType == "esriGeometryPoint") {
								style = new Style({
									image: new CircleStyle({
										radius: renderer.symbol.size * 1.5,
										fill: new Fill({
											color: this.getProperColor(renderer.symbol.color)
										}),
										stroke: this.getStroke(renderer.symbol.outline)
									})
								});
							}
						}
					}
				}
			}
			return style;
		},

		getStylesDict: function(renderer, scaleToResolution, labelingInfo){
			var styles = {};
			renderer.uniqueValueInfos.forEach(function(uniqueValueInfo){
				if (uniqueValueInfo.symbol) {
					styles[uniqueValueInfo.value] = this.getStyleObject(uniqueValueInfo.symbol, scaleToResolution, labelingInfo);
				}
			}.bind(this));
			return styles;
		},

		getStyleObject: function(symbol, scaleToResolution, labelingInfo){
			var style;
			switch (symbol.type) {
				case "esriPMS":
					style = new Style({
						image: new Icon({
							src: "data:" + symbol.contentType + ";base64," + symbol.imageData,
							width: symbol.width,
							height: symbol.height
						})
					});
					if (labelingInfo) {
						// TODO...
					}
					break;
				case "esriSFS":
					style = new Style({
						fill: this.getFill(symbol.style, symbol.color),
						stroke: this.getStroke(symbol.outline)
					});
					if (labelingInfo) {
						// TODO...
					}
					break;
				case "esriSLS":
					if (scaleToResolution) {
						style = {
							fnc: function(feature, resolution, specificRenderingType){
								var featureStyle = new Style({
									stroke: this.getStroke(symbol, resolution),
									text: this.getText(labelingInfo, feature, resolution)
								});
								if (specificRenderingType) {
									if (specificRenderingType == "task") {
										if (!Array.isArray(featureStyle)) {
											featureStyle = [featureStyle];
										}
										featureStyle.push(
											new Style({
												stroke: new Stroke({ // Pasikartojantis stilius kaip ir addSpecificRenderingIfNeeded() metode...
													width: StreetSignsLayerStyleHelper.getLineWidth(resolution) * 2,
													color: this.getTaskColor(feature)
												})
											})
										);
									}
								}
								return featureStyle;
							}.bind(this)
						};
					} else {
						if (labelingInfo) {
							style = {
								fnc: function(feature, resolution){
									var featureStyle = new Style({
										stroke: this.getStroke(symbol),
										text: this.getText(labelingInfo, feature, resolution)
									});
									return featureStyle;
								}.bind(this)
							};
						} else {
							style = new Style({
								stroke: this.getStroke(symbol)
							});
						}
					}
					break;
				case "esriSMS":
					if (symbol.style == "esriSMSCircle") {
						style = new Style({
							image: new CircleStyle({
								radius: symbol.size / 4 * 3,
								fill: this.getFill("esriSFSSolid", symbol.color),
								stroke: this.getStroke(symbol.outline)
							})
						});
						if (labelingInfo) {
							// TODO...
						}
					}
					break;
			}
			return style;
		},

		getFill: function(style, color){
			var fill;
			if (style == "esriSFSSolid") {
				fill = new Fill({
					color: this.getProperColor(color)
				});
			} else {
				fill = new Fill({
					color: this.getFillColorPattern(style, "rgba(" + this.getProperColor(color).join(",") + ")")
				});
			}
			return fill;
		},

		getStroke: function(outline, resolution){
			var stroke;
			if (outline) {
				stroke = new Stroke({
					width: resolution ? (outline.width / resolution / 16) : outline.width,
					color: this.getProperColor(outline.color),
					lineDash: outline.style == "esriSLSDash" ? [10, 10] : null, // TODO! Dar būna ir daugiau tipų...
					lineCap: "butt"
				});
			}
			return stroke;
		},

		getProperColor: function(color){
			if (color && color.length == 4) {
				color = color.slice();
				color[3] = color[3] / 256;
			}
			return color;
		},

		getFillColorPattern: function(style, color){
			// https://openlayers.org/en/latest/examples/canvas-gradient-pattern.html
			var canvas = document.createElement("canvas"),
				context = canvas.getContext("2d"),
				dim = 12;
			canvas.width = dim;
			canvas.height = dim;
			context.strokeStyle = color;
			context.beginPath();
			if (style == "esriSFSForwardDiagonal" || style == "esriSFSDiagonalCross") {
				context.moveTo(0, 0);
				context.lineTo(dim, dim);
			}
			if (style == "esriSFSBackwardDiagonal" || style == "esriSFSDiagonalCross") {
				context.moveTo(0, dim);
				context.lineTo(dim, 0);
			}
			context.lineWidth = 1;
			context.stroke();
			return context.createPattern(canvas, "repeat");
		},

		modFeatureStyle: function(style, feature, resolution, rotationField, scaleType, customStyle){
			var image = style.getImage();
			if (image) {
				if (rotationField) {
					var rotation = feature.get(rotationField) || 0;
					style.getImage().setRotation(rotation * Math.PI / 180);
				}
				if (scaleType == "basic") {
					if (resolution > 0.03) {
						image.setScale(0.6);
					} else {
						image.setScale(1);
					}
				} else if (scaleType == "fluid") {
					image.setScale(0.04 / resolution);
				}
				if (customStyle) {
					image.setScale(0.01 / resolution);
				}
			}
		},

		offsetLineString: function(geom, dist, simple){
			// https://stackoverflow.com/questions/57421223/openlayers-3-offset-stroke-style
			// Dabar šio algoritmo nesuprantu!!! TODO!!! Išmokti, suprasti!!!
			var coords = [],
				counter = 0,
				intersection;
			geom.forEachSegment(function(from, to) {
				var angle = Math.atan2(to[1] - from[1], to[0] - from[0]);
				var newFrom = [
					Math.sin(angle) * dist + from[0],
					-Math.cos(angle) * dist + from[1]
				];
				var newTo = [
					Math.sin(angle) * dist + to[0],
					-Math.cos(angle) * dist + to[1]
				];
				coords.push(newFrom);
				coords.push(newTo);
				if (!simple) {
					if (coords.length > 2) {
						intersection = intersect(coords[counter], coords[counter + 1], coords[counter + 2], coords[counter + 3]);
						coords[counter + 1] = (intersection) ? intersection : coords[counter + 1];
						coords[counter + 2] = (intersection) ? intersection : coords[counter + 2];
						counter += 2;
					}
				}
			});
			// Čia jau mano išmislas imti tik unikalius taškus, o pasikartojančius išmesti:
			var newCoords = [],
				lastI = coords.length - 1;
			newCoords.push(coords[0]);
			for (var i = 1; i < lastI; i += 2) {
				newCoords.push(coords[i]);
			}
			newCoords.push(coords[lastI]);
			return newCoords;
		},

		densifyCoordinates: function(geom, distanceBetweenPoints, includeEndingPoints){
			// !!! TODO! Šito savo algoritmo neperžiūrėjau!!!
			var newCoordinates = [];
			if (geom.getType() == "LineString") {
				var length = geom.getLength(),
					angle;
				if (length > distanceBetweenPoints * 2) {
					var pointsCount = Math.floor(length / distanceBetweenPoints),
						delta = (length - pointsCount * distanceBetweenPoints) / 2,
						segmentsData = [],
						segmDist = 0,
						segmLength,
						d;
					geom.forEachSegment(function(from, to){
						angle = Math.atan2(to[0] - from[0], to[1] - from[1]) + Math.PI;
						segmLength = new LineString([to, from]).getLength();
						segmentsData.push({
							a: angle,
							length: segmLength,
							fromDist: segmDist,
							toDist: segmDist + segmLength,
							fromCoordinate: from.slice()
						});
						segmDist += segmLength;
					}.bind(this));
					var pointDistance,
						segmentDataItem;
					for (var i = 0; i <= pointsCount; i++) {
						pointDistance = delta + i * distanceBetweenPoints;
						for (var j = 0; j < segmentsData.length; j++) {
							segmentDataItem = segmentsData[j];
							if (segmentDataItem.fromDist <= pointDistance && segmentDataItem.toDist > pointDistance) {
								d = pointDistance - segmentDataItem.fromDist;
								newCoordinates.push({
									c: [segmentDataItem.fromCoordinate[0] - d * Math.sin(segmentDataItem.a), segmentDataItem.fromCoordinate[1] - d * Math.cos(segmentDataItem.a)],
									a: segmentDataItem.a
								});
								break;
							}
						}
					}
					if (includeEndingPoints) {
						var origCoordinates = geom.getCoordinates();
						if (delta > 0.3) {
							// !! Dabar nėra "a" reikšmės???
							newCoordinates.unshift({
								c: origCoordinates[0]
							});
							newCoordinates.push({
								c: origCoordinates[origCoordinates.length - 1]
							});
						} else {
							// !! Dabar neteisinga "a" reikšmė???
							newCoordinates[0].c = origCoordinates[0];
							newCoordinates[newCoordinates.length - 1].c = origCoordinates[origCoordinates.length - 1];
						}
					}
				} else {
					geom.forEachSegment(function(from, to){
						angle = Math.atan2(to[0] - from[0], to[1] - from[1]) + Math.PI;
						if (!i) {
							newCoordinates.push({
								c: from.slice(),
								a: angle
							});
						}
						newCoordinates.push({
							c: to.slice(),
							a: angle
						});
						i += 1;
					}.bind(this));
				}
			}
			return newCoordinates;
		},

		getModFeatureStyleVertical: function(featureStyle, feature, resolution){
			var unapproved = false;
			if (feature.get("PATVIRTINTAS") === 0 || feature.get("PATVIRTINIMAS") === 0) { // 0 = Nepatvirtintas
				unapproved = true;
			}
			if (unapproved) {
				var src = require("@/assets/q/red.png");
				if (!feature.get("PASAL_DATA")) {
					if (feature.get("ATMESTA") == 1) {
						src = require("@/assets/q/blue.png");
					} else if (feature.get("STATUSAS") == 1) {
						src = require("@/assets/q/green.png");
					} else if (feature.get("STATUSAS") == 4) {
						src = require("@/assets/q/orange.png");
					}
				}
				var scale = 0.5;
				if (resolution > 0.1) {
					scale = scale / 10 / resolution;
				}
				featureStyle = [
					featureStyle,
					new Style({
						image: new Icon({
							src: src,
							imgSize: [40, 40],
							scale: scale,
							anchor: [-15, 15],
							anchorXUnits: "pixels",
							anchorYUnits: "pixels"
						})
					})
				];
			} else {
				if (feature.get("ATMESTA")) {
					featureStyle = [
						featureStyle,
						new Style({
							image: new RegularShape({
								stroke: new Stroke({
									color: "red",
									width: 3
								}),
								points: 4,
								radius: 16,
								radius2: 0,
								angle: Math.PI / 4
							})
						}),
						new Style({
							image: new CircleStyle({
								radius: 10,
								fill: new Fill({
									color: "rgba(255, 0, 0, 0.3)"
								})
							})
						})
					];
				} else if (feature.get("STATUSAS") == CommonHelper.verticalStreetSignDestroyedStatusValue) {
					// http://localhost:3001/admin/?t=v&l=0&id=56BE31C3-9A41-4952-83BE-3B84E5BA1988
					featureStyle = [
						featureStyle,
						new Style({
							image: new RegularShape({
								stroke: new Stroke({
									color: "red",
									width: 4
								}),
								points: 4,
								radius: 20,
								radius2: 0,
								angle: Math.PI / 4
							})
						})
					];
				}
			}
			return featureStyle
		},

		getInventorizationLTextStyle: function(feature, field){
			var text = feature.get(field);
			if (text) {
				return new Text({
					text: text,
					font: "14px Arial",
					overflow: true,
					fill: new Fill({
						color: "red"
					}),
					stroke: new Stroke({
						color: "#fff",
						width: 5
					}),
					placement: "line",
					textBaseline: "top"
				});
			}
		},

		getText: function(labelingInfo, feature, resolution){
			var text;
			if (labelingInfo) {
				if (Array.isArray(labelingInfo)) {
					// Problemėlė?? OL vienam simboliui tik vieną tekstą galima priskirti??... ESRI bent kelis?.. Sprendimas: elementui nustatyti kelis simbolius...
					// https://developers.arcgis.com/javascript/latest/sample-code/labels-multiple-classes/
					var labelInfo = labelingInfo[0]; // Deja, kol kas imsime tik pirmąjį narį...
					if (labelInfo) {
						if (labelInfo.labelExpressionInfo) {
							// http://localhost:3001/admin/?t=vvt&l=7&id=4550
							// Pagal rezoliuciją reikia atsekti mastelį?..
							// 1 = 0.026458386250105836 / 100
							var fieldName = this.getLabelingInfoFieldFromExpression(labelInfo.labelExpressionInfo),
								textValue = feature.get(fieldName);
							if (textValue) {
								console.log("LI", textValue, resolution, labelInfo.minScale, labelInfo.maxScale); // TODO!!
							}
						}
					}
				}
			}
			return text;
		},

		getLabelingInfoFieldFromExpression: function(labelExpressionInfo){
			var field;
			if (labelExpressionInfo && labelExpressionInfo.expression) {
				var match = labelExpressionInfo.expression.match(/^\$feature\.(.*)/);
				if (match) {
					field = match[1];
				} else {
					match = labelExpressionInfo.expression.match(/DomainName\(\$feature, '(.*)'\)/);
					if (match) {
						field = match[1];
					}
				}
			}
			return field;
		}
	}
</script>