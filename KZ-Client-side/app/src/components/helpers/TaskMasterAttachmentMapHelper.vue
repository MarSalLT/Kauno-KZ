<script>
	import CommonHelper from "./CommonHelper";
	import LinearRing from "ol/geom/LinearRing";
	import LineString from "ol/geom/LineString";
	import Polygon from "ol/geom/Polygon";
	import Text from "ol/style/Text";
	import {Circle as CircleStyle, Fill, Stroke, Style, RegularShape} from "ol/style";
	import {getCenter as getExtentCenter, getTopLeft} from "ol/extent";

	var webServicesRoot = CommonHelper.webServicesRoot + "attachment/";

	export default {
		extendBeyond: 100000,

		sizes: {
			"a3-landscape": {
				width: 4200,
				height: 2970
			},
			"a4-portrait": {
				width: 2100,
				height: 2970
			}
		},

		highlightedStrokeColor: "rgba(52, 212, 255, 1)",

		getBoundaryGeometry: function(extentCenter, frameSizeType){
			var size = this.sizes[frameSizeType];
			if (size) {
				var geometry = new LineString([
					[extentCenter[0] - size.width / 2, extentCenter[1] - size.height / 2],
					[extentCenter[0] + size.width / 2, extentCenter[1] - size.height / 2],
					[extentCenter[0] + size.width / 2, extentCenter[1] + size.height / 2],
					[extentCenter[0] - size.width / 2, extentCenter[1] + size.height / 2],
					[extentCenter[0] - size.width / 2, extentCenter[1] - size.height / 2]
				]);
				return geometry;
			}
		},

		getBoundaryOutsideGeometry: function(map, coordinates){
			var extent = map.getView().getProjection().getExtent();
			var geometry = new Polygon([[
				[extent[0] - this.extendBeyond, extent[1] - this.extendBeyond],
				[extent[2] + this.extendBeyond, extent[1] - this.extendBeyond],
				[extent[2] + this.extendBeyond, extent[3] + this.extendBeyond],
				[extent[0] - this.extendBeyond, extent[3] + this.extendBeyond],
				[extent[0] - this.extendBeyond, extent[1] - this.extendBeyond]
			]]);
			geometry.appendLinearRing(new LinearRing(coordinates));
			return geometry;
		},

		getFeaturesLayerStyle: function(map){
			var that = this;
			var fnc = function(feature, resolution) {
				var type = feature.get("type"),
					active = feature.get("active"),
					zIndex = feature.get("z-index") || 0,
					strokeWidth = feature.get("stroke-width") || 0,
					style,
					stroke;
				if (type != "boundary") {
					strokeWidth = strokeWidth / resolution;
				}
				if (strokeWidth) {
					stroke = new Stroke({
						color: feature.get("stroke-color"),
						width: strokeWidth,
						lineCap: "butt"
					});
				}
				var activeStroke = that.getActiveStroke(strokeWidth);
				var fill = new Fill({
					color: feature.get("fill-color")
				});
				switch (type) {
					case "boundary-outside":
						style = [
							new Style({
								fill: new Fill({
									color: "rgba(240, 240, 240, 0.8)"
								}),
								stroke: new Stroke({
									color: "red",
									width: 1
								})
							})
						];
						break;
					case "image-mock-polygon":
						style = [
							new Style({
								fill: new Fill({
									color: "rgba(255, 255, 255, 0.01)" // Beveik nematomas uŽpildas...
								}),
								zIndex: zIndex
							})
						];
						if (feature.get("img")) {
							// FIXME! Kažkaip gaunasi, kad piešinukas yra virš kraštinės... Reiktų pataisyti... Kažkas su jo zIndex reikšme ne taip?..
							style.unshift(new Style({
								renderer: function(coordinates, state){
									var ctx = state.context,
										c = coordinates[0],
										w = new LineString([c[1], c[2]]).getLength(),
										h = new LineString([c[0], c[1]]).getLength();
									ctx.save();
									var x = (c[2][0] + c[0][0]) / 2,
										y = (c[2][1] + c[0][1]) / 2;
									if (feature.rotationInfo) {
										// https://riptutorial.com/html5-canvas/example/19532/rotate-an-image-or-path-around-it-s-centerpoint
										// Įdomus pvz.: https://codepen.io/mike-000/pen/ExaMmQK
										// Čia dar šis-tas: https://jsfiddle.net/18e4qpma/3/
										// https://www.w3schools.com/graphics/game_rotation.asp
										ctx.translate(x, y);
										ctx.rotate(- feature.rotationInfo.angle);
										ctx.translate(- x, - y);
									}
									if (feature.clipFeatures) {
										feature.clipFeatures.forEach(function(clipFeature){
											ctx.beginPath();
											var clipFeatureCoordinates = clipFeature.getGeometry().getCoordinates()[0];
											clipFeatureCoordinates.forEach(function(coord, i){
												coord = map.getPixelFromCoordinate(coord);
												if (i) {
													ctx.lineTo(coord[0], coord[1]);
												} else {
													ctx.moveTo(coord[0], coord[1]);
												}
											});
											ctx.closePath();
										});
										ctx.clip("evenodd");
									}
									x -= w / 2;
									y -= h / 2;
									ctx.drawImage(feature.get("img"), x, y, w, h);
									ctx.restore();
									if (active && activeStroke) {
										ctx.beginPath();
										ctx.lineWidth = activeStroke.getWidth();
										ctx.strokeStyle = activeStroke.getColor();
										c.forEach(function(coord, i){
											if (i) {
												ctx.lineTo(coord[0], coord[1]);
											} else {
												ctx.moveTo(coord[0], coord[1]);
											}
										});
										ctx.closePath();
										ctx.stroke();
									}
									if (stroke) {
										ctx.beginPath();
										ctx.lineWidth = stroke.getWidth();
										ctx.strokeStyle = "rgba(" + stroke.getColor().join(",") + ")";
										c.forEach(function(coord, i){
											if (i) {
												ctx.lineTo(coord[0], coord[1]);
											} else {
												ctx.moveTo(coord[0], coord[1]);
											}
										});
										ctx.closePath();
										ctx.stroke();
									}
								}.bind(this),
								zIndex: zIndex
							}));
						} else {
							style.unshift(new Style({
								fill: new Fill({
									color: that.getUnavailableImageFillColorPattern()
								}),
								zIndex: zIndex
							}));
							if (active) {
								style.unshift(new Style({
									stroke: activeStroke,
									zIndex: zIndex
								}));
							}
						}
						break;
					case "point":
						var image,
							size = (feature.get("point-size") || 10) / resolution * 10;
						if (feature.get("point-type") == "rectangle") {
							image = new RegularShape({
								fill: fill,
								radius: size * 1.3,
								points: 4,
								angle: Math.PI / 4
							});
						} else {
							image = new CircleStyle({
								radius: size,
								fill: fill
							});
						}
						style = [
							new Style({
								image: image,
								zIndex: zIndex
							})
						];
						if (active) {
							style.unshift(new Style({
								image: new RegularShape({
									stroke: activeStroke,
									radius: size + 80 / resolution,
									points: 4,
									angle: Math.PI / 4
								}),
								zIndex: zIndex
							}));
						}
						break;
					case "text":
						style = that.getTextStyle(feature, resolution, active);
						break;
					case "arrow":
						style = that.getArrowStyle(feature, resolution, active);
						break;
					default:
						style = [
							new Style({
								stroke: stroke,
								fill: fill,
								zIndex: zIndex
							})
						];
						if (active) {
							style.unshift(new Style({
								stroke: activeStroke,
								zIndex: zIndex
							}));
						}
					}
				return style;
			}
			return fnc;
		},

		getRectangleGeometryFunction(){
			var fnc = function(coordinates, opt_geometry){
				var geometry = opt_geometry;
				if (geometry) {
					if (coordinates.length) {
						if (coordinates.length == 2) {
							geometry.setCoordinates([[
								coordinates[0],
								[coordinates[0][0], coordinates[1][1]],
								coordinates[1],
								[coordinates[1][0], coordinates[0][1]],
								coordinates[0]
							]]);
						} else {
							geometry.setCoordinates([coordinates]);
						}
					} else {
						geometry.setCoordinates([]);
					}
				} else {
					geometry = new Polygon([coordinates]);
				}
				return geometry;
			};
			return fnc;
		},

		getTextStyle: function(feature, resolution, active){
			var textValue = feature.get("text-value") || "Tekstas";
			// Čia HIT-detection'o problema? -> https://github.com/openlayers/openlayers/issues/3657
			// https://github.com/openlayers/openlayers/issues/8136
			var style = [
				new Style({
					text: new Text({
						text: textValue,
						fill: new Fill({
							color: feature.get("fill-color")
						}),
						font: Math.round(((feature.get("text-size") || 10) / resolution * 10)) + "px Arial",
						backgroundStroke: active ? new Stroke({
							color: this.highlightedStrokeColor,
							width: 1,
							lineCap: "butt"
						}) : null,
						backgroundFill: new Fill({
							color: [255, 255, 255, 0.2] || [255, 255, 255, 0.01] // Puikesniam HIT-detection'ui aktualu?..
						}),
						rotation: - (feature.get("rotation-angle") || 0) * Math.PI / 180
					}),
					zIndex: feature.get("z-index") || 0
				})
			];
			return style;
		},

		getArrowStyle: function(feature, resolution, active, highlighted){
			var zIndex = feature.get("z-index") || 0,
				stroke,
				strokeWidth = (feature.get("stroke-width") || 0) / resolution,
				activeStroke = this.getActiveStroke(strokeWidth);
			if (highlighted) {
				if (strokeWidth < 2) {
					strokeWidth = 2;
				}
			}
			if (strokeWidth) {
				stroke = new Stroke({
					color: highlighted ? this.highlightedStrokeColor : feature.get("stroke-color"),
					width: strokeWidth,
					lineCap: "butt",
					lineJoin: "miter"
				});
			}
			var geometryFunc = function(f){
				if (f.styleGeom && !f.ignoreStyleGeom) {
					return f.styleGeom;
				}
				var arrowHeadCoordinates = this.getArrowHeadCoordinates(f);
				if (arrowHeadCoordinates) {
					var g;
					if (f.get("arrow-head-filled")) {
						g = new Polygon([arrowHeadCoordinates]);
					} else {
						g = new LineString(arrowHeadCoordinates);
					}
					f.styleGeom = g;
					return g;
				}
			}.bind(this);
			var style = [
				new Style({
					stroke: stroke,
					fill: feature.get("arrow-head-filled") ? new Fill({
						color: feature.get("stroke-color")
					}) : null,
					geometry: geometryFunc,
					zIndex: zIndex
				}),
				new Style({
					stroke: stroke,
					zIndex: zIndex
				}),
			];
			if (active) {
				style.unshift(new Style({
					stroke: activeStroke,
					zIndex: zIndex
				}));
				style.unshift(new Style({
					stroke: activeStroke,
					geometry: geometryFunc,
					zIndex: zIndex
				}));
			}
			return style;
		},

		getArrowHeadCoordinates: function(feature){
			var coordinates = feature.getGeometry().getCoordinates();
			if (coordinates.length > 1) {
				var lastCoordinateI = coordinates.length - 1,
					lineAngle = Math.atan2(coordinates[lastCoordinateI][0] - coordinates[lastCoordinateI - 1][0], coordinates[lastCoordinateI][1] - coordinates[lastCoordinateI - 1][1]) + Math.PI,
					arrowLength = feature.get("arrow-head-size"),
					arrowAngle = 30 * Math.PI / 180,
					arrowHeadCoordinates = [];
				arrowHeadCoordinates.push([coordinates[lastCoordinateI][0] + arrowLength * Math.sin(lineAngle + arrowAngle), coordinates[lastCoordinateI][1] + arrowLength * Math.cos(lineAngle + arrowAngle)]);
				arrowHeadCoordinates.push(coordinates[lastCoordinateI]);
				arrowHeadCoordinates.push([coordinates[lastCoordinateI][0] + arrowLength * Math.sin(lineAngle - arrowAngle), coordinates[lastCoordinateI][1] + arrowLength * Math.cos(lineAngle - arrowAngle)]);
				return arrowHeadCoordinates;
			}
		},

		getActiveStroke: function(strokeWidth){
			var activeStroke = new Stroke({
				color: this.highlightedStrokeColor,
				width: strokeWidth ? (strokeWidth + 3) : 1,
				lineCap: "butt"
			});
			return activeStroke;
		},

		rotateImageMockPolygon: function(feature, rotationAngle){
			// FIXME! Gal čia galima kažką protingiau sugalvoti? Dabar gan kebli logika dėl to "origGeometry", "origGeometryRotationAngle"...
			if (!feature.origGeometry) {
				feature.origGeometry = feature.getGeometry().clone();
				feature.origGeometryRotationAngle = feature.get("rotation-angle") || 0;
			}
			var center = getExtentCenter(feature.origGeometry.getExtent()),
				angle = (rotationAngle || 0) * Math.PI / 180;
			feature.rotationInfo = {
				center: center,
				angle: angle
			};
			var g = feature.origGeometry.clone(),
				rotateBy = angle - (feature.origGeometryRotationAngle || 0) * Math.PI / 180;
			g.rotate(rotateBy, center);
			feature.setGeometry(g);
		},

		getMasterAttachmentSrc: function(boundaryLayer, featuresLayer, map){
			if (map) {
				var canvas = document.createElement("canvas"),
					ctx = canvas.getContext("2d"),
					boundaryTopLeft;
				if (boundaryLayer) {
					boundaryLayer.getSource().getFeatures().some(function(feature){
						if (feature.get("type") == "boundary") {
							boundaryTopLeft = getTopLeft(feature.getGeometry().getExtent());
							var frameSize = this.sizes[feature.get("frame-size")];
							if (frameSize) {
								canvas.width = frameSize.width;
								canvas.height = frameSize.height;
							}
							return true;
						}
					}.bind(this));
				}
				if (canvas.width && canvas.height) {
					var mapExtent = map.getView().getProjection().getExtent(),
						topLeft = getTopLeft(mapExtent);
					if (topLeft && boundaryTopLeft) {
						ctx.fillStyle = "white";
						ctx.fillRect(0, 0, canvas.width, canvas.height);
						if (featuresLayer) {
							var featureType,
								geometry;
							var features = featuresLayer.getSource().getFeatures().slice();
							features.sort(function(a, b){
								if (a.get("z-index") <= b.get("z-index")) {
									return -1;
								}
								if (a.get("z-index") > b.get("z-index")) {
									return 1;
								}
								return 0;
							});
							features.forEach(function(feature){
								featureType = feature.get("type");
								geometry = feature.getGeometry().clone();
								// !!! https://stackoverflow.com/questions/63638347/inverting-the-y-axis
								if (geometry) {
									ctx.lineWidth = (feature.get("stroke-width") || 0) * 1;
									ctx.strokeStyle = this.getColor(feature.get("stroke-color"));
									ctx.fillStyle = this.getColor(feature.get("fill-color"));
									ctx.lineCap = "butt";
									ctx.lineJoin = "round";
									var coord;
									switch (featureType) {
										case "image-mock-polygon":
											var img = feature.get("img");
											if (img) {
												ctx.save();
												var newC = [];
												geometry.getCoordinates()[0].forEach(function(coord){
													newC.push(this.adjustCanvasCoordinate(coord, boundaryTopLeft));
												}.bind(this));
												var w = new LineString([newC[1], newC[2]]).getLength(),
													h = new LineString([newC[0], newC[1]]).getLength();
												var x = (newC[2][0] + newC[0][0]) / 2,
													y = (newC[2][1] + newC[0][1]) / 2;
												if (feature.get("rotation-angle")) {
													ctx.translate(x, y);
													ctx.rotate(- feature.get("rotation-angle") * Math.PI / 180);
													ctx.translate(- x, - y);
												}
												if (feature.clipFeatures) {
													feature.clipFeatures.forEach(function(clipFeature){
														ctx.beginPath();
														var clipFeatureCoordinates = clipFeature.getGeometry().getCoordinates()[0];
														clipFeatureCoordinates.forEach(function(coord, i){
															coord = this.adjustCanvasCoordinate(coord, boundaryTopLeft);
															if (i) {
																ctx.lineTo(coord[0], coord[1]);
															} else {
																ctx.moveTo(coord[0], coord[1]);
															}
														}.bind(this));
														ctx.closePath();
													}.bind(this));
													ctx.clip("evenodd");
												}
												x -= w / 2;
												y -= h / 2;
												ctx.drawImage(img, x, y, w, h);
												ctx.restore();
											}
											geometry.getCoordinates().forEach(function(coordinates){
												ctx.beginPath();
												coordinates.forEach(function(coord, i){
													coord = this.adjustCanvasCoordinate(coord, boundaryTopLeft);
													if (i) {
														ctx.lineTo(coord[0], coord[1]);
													} else {
														ctx.moveTo(coord[0], coord[1]);
													}
												}.bind(this));
												ctx.closePath();
												ctx.stroke();
											}.bind(this));
											break;
										case "point":
											coord = this.adjustCanvasCoordinate(feature.getGeometry().getCoordinates(), boundaryTopLeft);
											var size = (feature.get("point-size") || 10) * 10;
											if (feature.get("point-type") == "rectangle") {
												ctx.beginPath();
												ctx.rect(coord[0] - size, coord[1] - size, size * 2, size * 2);
												ctx.fill();
											} else {
												ctx.beginPath();
												ctx.arc(coord[0], coord[1], size, 0, 2 * Math.PI, false);
												ctx.fill();
												// ctx.stroke(); // Kol kas nėra tokio funkcionalumo...
											}
											break;
										case "text":
											ctx.save();
											ctx.textAlign = "center";
											ctx.textBaseline = "middle";
											coord = this.adjustCanvasCoordinate(feature.getGeometry().getCoordinates(), boundaryTopLeft);
											if (feature.get("rotation-angle")) {
												ctx.translate(coord[0], coord[1]);
												ctx.rotate(- feature.get("rotation-angle") * Math.PI / 180);
												ctx.translate(- coord[0], - coord[1]);
											}
											ctx.font = Math.round(((feature.get("text-size") || 10) * 10)) + "px Arial";
											ctx.fillText(feature.get("text-value") || "Tekstas", coord[0], coord[1]);
											ctx.restore();
											break;
										case "line":
											ctx.beginPath();
											geometry.getCoordinates().forEach(function(coord, i){
												coord = this.adjustCanvasCoordinate(coord, boundaryTopLeft);
												if (i) {
													ctx.lineTo(coord[0], coord[1]);
												} else {
													ctx.moveTo(coord[0], coord[1]);
												}
											}.bind(this));
											ctx.stroke();
											break;
										case "polygon":
										case "rectangle":
											geometry.getCoordinates().forEach(function(coordinates){
												ctx.beginPath();
												coordinates.forEach(function(coord, i){
													coord = this.adjustCanvasCoordinate(coord, boundaryTopLeft);
													if (i) {
														ctx.lineTo(coord[0], coord[1]);
													} else {
														ctx.moveTo(coord[0], coord[1]);
													}
												}.bind(this));
												ctx.closePath();
												ctx.fill();
												ctx.stroke();
											}.bind(this));
											break;
										case "circle":
											ctx.beginPath();
											coord = this.adjustCanvasCoordinate(geometry.getCenter(), boundaryTopLeft);
											ctx.arc(coord[0], coord[1], geometry.getRadius(), 0, 2 * Math.PI, false);
											ctx.fill();
											ctx.stroke();
											break;
										case "arrow":
											ctx.lineJoin = "miter";
											ctx.fillStyle = ctx.strokeStyle;
											ctx.beginPath();
											geometry.getCoordinates().forEach(function(coord, i){
												coord = this.adjustCanvasCoordinate(coord, boundaryTopLeft);
												if (i) {
													ctx.lineTo(coord[0], coord[1]);
												} else {
													ctx.moveTo(coord[0], coord[1]);
												}
											}.bind(this));
											ctx.stroke();
											ctx.beginPath();
											var arrowHeadCoordinates = this.getArrowHeadCoordinates(feature);
											if (arrowHeadCoordinates) {
												arrowHeadCoordinates.forEach(function(coord, i){
													coord = this.adjustCanvasCoordinate(coord, boundaryTopLeft);
													if (i) {
														ctx.lineTo(coord[0], coord[1]);
													} else {
														ctx.moveTo(coord[0], coord[1]);
													}
												}.bind(this));
											}
											if (feature.get("arrow-head-filled")) {
												ctx.closePath();
												ctx.stroke();
												ctx.fill();
											} else {
												ctx.stroke();
											}
											break;
									}
								}
							}.bind(this));
						}
						ctx.strokeStyle = "black";
						ctx.lineWidth = 10;
						ctx.strokeRect(0, 0, canvas.width, canvas.height);
						var src = canvas.toDataURL("image/jpeg", 0.95);
						return src;
					}
				}
			}
		},

		adjustCanvasCoordinate: function(coordinate, boundaryTopLeft){
			if (boundaryTopLeft) {
				coordinate[0] = coordinate[0] - boundaryTopLeft[0];
				coordinate[1] = boundaryTopLeft[1] - coordinate[1];
			}
			return coordinate;
		},

		getColor: function(color){
			var colorNew;
			if (color) {
				if (Array.isArray(color)) {
					colorNew = "rgba(" + color.join(",") + ")";
				}
			}
			return colorNew;
		},

		getAttachmentContentData: function(id){
			var promise = new Promise(function(resolve, reject){
				if (id) {
					var url = webServicesRoot + "get-data?&id=" + id;
					CommonHelper.getFetchPromise(url).then(function(result){
						if (result) {
							resolve(result);
						} else {
							reject();
						}
					}, function(){
						reject();
					});
				} else {
					reject();
				}
			}.bind(this));
			return promise;
		},

		saveAttachmentContentData: function(attachmentId, contentData){
			var params = {
				attachmentId: attachmentId,
				data: JSON.stringify(contentData)
			};
			var promise = new Promise(function(resolve, reject){
				if (attachmentId && contentData) {
					var url = webServicesRoot + "save-data";
					CommonHelper.getFetchPromise(url, null, "POST", params).then(function(result){
						if (result) {
							resolve(result);
						} else {
							reject();
						}
					}, function(){
						reject();
					});
				} else {
					reject();
				}
			}.bind(this));
			return promise;
		},

		getUnavailableImageFillColorPattern: function(){
			var canvas = document.createElement("canvas"),
				context = canvas.getContext("2d"),
				dim = 12,
				color = "red";
			canvas.width = dim;
			canvas.height = dim;
			context.strokeStyle = color;
			context.beginPath();
			context.moveTo(0, 0);
			context.lineTo(dim, dim);
			context.moveTo(0, dim);
			context.lineTo(dim, 0);
			context.lineWidth = 1;
			context.stroke();
			return context.createPattern(canvas, "repeat");
		}
	}
</script>