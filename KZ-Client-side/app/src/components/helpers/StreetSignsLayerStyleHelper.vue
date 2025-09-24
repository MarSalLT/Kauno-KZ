<script>
	import CommonHelper from "./CommonHelper";
	import GeometryCollection from "ol/geom/GeometryCollection";
	import LineString from "ol/geom/LineString";
	import MultiPoint from "ol/geom/MultiPoint";
	import Point from "ol/geom/Point";
	import VectorLayerStyleHelper from "./VectorLayerStyleHelper";
	import {Circle as CircleStyle, Fill, RegularShape, Stroke, Style} from "ol/style";

	var test = false;

	export default {
		fakeColor: test ? "green" : "#ffffff01",
		thinLineWidth: 2,
		whiteColor: "white",
		widthFieldName: CommonHelper.widthFieldName,
		yellowColor: "yellow",

		getStyle: function(layerInfo, myMap, specificRenderingType){
			var style,
				layerIdMeta;
			for (var key in CommonHelper.layerIds) {
				layerIdMeta = CommonHelper.layerIds[key];
				if (layerIdMeta[0] == "street-signs" && layerIdMeta[1] == layerInfo.id) {
					style = this.getStyleByKey(key, layerInfo, myMap, specificRenderingType);
					break;
				}
			}
			return style;
		},

		getStyleByKey: function(key, layerInfo, myMap, specificRenderingType){
			var style;
			if (key == "horizontalPolylines" || key == "horizontalPolygons") {
				style = this.getAdvancedStyle(layerInfo, key, myMap, specificRenderingType);
			}
			return style;
		},

		getAdvancedStyle: function(layerInfo, key, myMap, specificRenderingType){
			var style;
			if (layerInfo.drawingInfo && layerInfo.drawingInfo.renderer) {
				var rendererField = layerInfo.drawingInfo.renderer.field1;
				if (rendererField) {
					var stylesDefault = VectorLayerStyleHelper.getStylesDict(layerInfo.drawingInfo.renderer),
						styles = {};
					for (var property in stylesDefault) {
						styles[property] = {
							def: this.getDefaultStyle(layerInfo) || stylesDefault[property],
							generic: this.getGenericStyle(layerInfo)
						}
					}
					this.populateAdvancedStyle(styles, key, specificRenderingType);
					style = function(feature, resolution){
						var skipFeature = CommonHelper.checkIfSkipFeature(feature, myMap, layerInfo);
						if (!skipFeature) {
							var featureStyle = styles[feature.get(rendererField)];
							if (featureStyle) {
								if (featureStyle.fnc) {
									if (resolution > 1 && featureStyle.generic) {
										featureStyle = featureStyle.generic;
									} else {
										featureStyle = featureStyle.fnc(feature, resolution);
									}
								} else {
									featureStyle = featureStyle.def;
									if (specificRenderingType) {
										// Pvz. "Kito tipo" poligonai... Arba 1.13.2
										featureStyle = [featureStyle];
										this.addSpecificRenderingIfNeeded(featureStyle, resolution, specificRenderingType, feature);
									}
								}
							}
							return featureStyle;
						}
					}.bind(this);
				}
			}
			return style;
		},

		populateAdvancedStyle: function(styles, key, specificRenderingType){
			if (key == "horizontalPolygons") {
				styles["132"].fnc = function(feature, resolution){
					// Geltonos įstrižos linijos keturkampyje
					// Pvz. http://localhost:3001/admin/?l=4&id=C6BA71B4-3E16-4C99-AE1C-2125D3F48570
					// Pvz. http://localhost:3001/admin/?l=4&id=5F53CE6C-0E33-46CC-90E2-2DCE5988EFA3
					var styles = [
						new Style({ // FAKE! Šito reikia tam, kad užvedus pele į peršviečiamą plotą tinkamai suveiktų "pointermove", "singleclick"...
							fill: new Fill({
								color: this.fakeColor
							})
						}),
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution),
								color: this.yellowColor
							}),
							geometry: function(feature){
								if (feature.styleGeom && !feature.ignoreStyleGeom) { // Čia ale toks mano geometrijos už'cache'avimas...
									return feature.styleGeom;
								}
								var geom = feature.getGeometry();
								if (geom.getType() == "Polygon") {
									var coordinates = geom.getCoordinates();
									if (coordinates.length == 1 && coordinates[0].length == 5) {
										var geometries = [
											new LineString(coordinates[0]),
											new LineString([coordinates[0][0], coordinates[0][2]]),
											new LineString([coordinates[0][1], coordinates[0][3]])
										];
										feature.styleGeom = new GeometryCollection(geometries);
										return feature.styleGeom;
									}
								}
								return geom;
							}
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature); // FIXME! Lyg ir nelabai gerai "tasks" atveju... Pvz. žalios objekto linijos atsiduria žemiau geltonųjų... Tai nėra labai pageidautina, nes pvz. kituose objektuose jos yra aukščiau objekto originalaus simbolio...
					return styles;
				}.bind(this);
				styles["1322"].fnc = function(feature, resolution){
					// Geltonos įstrižos linijos keturkampyje
					var styles = [
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution),
								color: this.yellowColor
							}),
							fill: new Fill({
								color: VectorLayerStyleHelper.getFillColorPattern("esriSFSDiagonalCross", this.yellowColor)
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["1151"].fnc = function(feature, resolution){
					// Įstrižos baltos linijos
					// Pvz. http://localhost:3001/admin/?l=4&id=59501BF5-725B-49D5-901F-FECCEDB10C16
					// FIXME!!! Turbūt tas įstrižas tušavimas turi būti pagal kelio kryptį ar pan.?
					var styles = [
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution),
								color: this.whiteColor
							}),
							fill: new Fill({
								color: VectorLayerStyleHelper.getFillColorPattern("esriSFSForwardDiagonal", this.whiteColor)
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["1152"].fnc = function(feature, resolution){
					// Pvz. http://localhost:3001/admin/?l=4&id=C6A68024-CE12-4DDC-9425-B9DA4C586830
					// Pvz. http://localhost:3001/admin/?l=4&id=C7066B36-C532-45AA-9D65-E9C2B705B185 !!! Čia įdomus pvz.
					var styles = [
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution),
								color: this.whiteColor
							}),
							fill: new Fill({
								color: this.getFillColorPattern("1152", this.whiteColor)
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["1153"].fnc = function(feature, resolution){
					// Pvz. http://localhost:3001/admin/?l=4&id=6DA77436-7FFA-404A-B6C1-BDA8E748A173
					var styles = [
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution),
								color: this.whiteColor
							}),
							fill: new Fill({
								color: this.getFillColorPattern("1153", this.whiteColor)
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
			} else if (key == "horizontalPolylines") {
				styles["11"].fnc = function(feature, resolution){
					// Siaura balta ištisinė linija
					var styles = [
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution),
								color: this.whiteColor,
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["12"].fnc = function(feature, resolution){
					// Plati balta ištisinė linija
					var styles = [
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution, 2),
								color: this.whiteColor,
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["13"].fnc = function(feature, resolution){
					// Dviguba ištisinė linija
					// Pvz. http://localhost:3001/admin/?l=3&id=7B96BD4C-B06B-42DE-8C6F-575F2B92E8E2
					// Pvz. http://localhost:3001/admin/?l=3&id=15345336-97BE-4C33-BFA7-F4D170A9575F
					var distanceBetweenLines = 0.3;
					var styles = [
						new Style({
							stroke: new Stroke({
								width: distanceBetweenLines / resolution,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution, 1),
								color: this.whiteColor,
								lineCap: "butt"
							}),
							geometry: function(feature){
								if (feature.styleGeom && !feature.ignoreStyleGeom) { // Čia ale toks mano geometrijos už'cache'avimas...
									return feature.styleGeom;
								}
								var geom = feature.getGeometry();
								if (geom.getType() == "LineString") {
									feature.styleGeom = new GeometryCollection([
										new LineString(VectorLayerStyleHelper.offsetLineString(geom, - distanceBetweenLines / 2)),
										new LineString(VectorLayerStyleHelper.offsetLineString(geom, distanceBetweenLines / 2))
									]);
									return feature.styleGeom;
								}
								return geom;
							}.bind(this)
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["14"].fnc = function(feature, resolution){
					// Siaura geltona ištisinė linija
					// Pvz. http://localhost:3001/admin/?l=3&id=166E9E3F-03B9-4BAE-891F-CC853BF48F7B
					var styles = [
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution),
								color: this.yellowColor,
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["15"].fnc = function(feature, resolution){
					// Siaura brūkšninė linija, kurios brūkšniai tris kartus trumpesni už tarpus
					var lineDashValue = 1.6 / resolution,
						lineWidth = this.getLineWidth(resolution);
					var styles = [
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.whiteColor,
								lineDash: [lineDashValue, lineDashValue * 3], // Brūkšniai, tarpai
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["16"].fnc = function(feature, resolution){
					// Artėjimo linija — siaura brūkšninė linija, kurios brūkšniai tris kartus ilgesni už tarpus
					// Pvz. http://localhost:3001/admin/?l=3&id=432B7239-1785-4C14-BC32-654F011FA01B
					var lineDashValue = 1 / resolution,
						lineWidth = this.getLineWidth(resolution);
					var styles = [
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.whiteColor,
								lineDash: [lineDashValue * 3, lineDashValue], // Brūkšniai, tarpai
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["17"].fnc = function(feature, resolution){
					// Siaura brūkšninė linija, kurios brūkšnių ir tarpų ilgis vienodas
					var lineDashValue = 0.8 / resolution,
						lineWidth = this.getLineWidth(resolution);
					var styles = [
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.whiteColor,
								lineDash: [lineDashValue, lineDashValue], // Brūkšniai, tarpai
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["18"].fnc = function(feature, resolution){
					// Plati brūkšninė linija, kurios brūkšnių ilgis tris kartus trumpesnis už tarpus
					var lineDashValue = 1 / resolution,
						lineWidth = this.getLineWidth(resolution, 2);
					var styles = [
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.whiteColor,
								lineDash: [lineDashValue, lineDashValue * 3], // Brūkšniai, tarpai
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["19"].fnc = function(feature, resolution){
					// Geltona brūkšninė linija
					// http://localhost:3001/admin/?l=3&id=0F7ACB48-4D60-4ED3-949F-1E4652D3A193
					var lineDashValue = 1.2 / resolution,
						lineWidth = this.getLineWidth(resolution);
					var styles = [
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.yellowColor,
								lineDash: [lineDashValue, lineDashValue], // Brūkšniai, tarpai
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["110"].fnc = function(feature, resolution){
					// Dviguba linija, susidedanti iš dviejų siaurų lygiagrečių linijų, kurių viena yra ištisinė, o kita – brūkšninė
					// Pvz. http://localhost:3001/admin/?l=3&id=778B07A8-6BBC-41C1-B8EF-B6789634517D
					var styleGeom,
						geom = feature.getGeometry();
					if (feature.styleGeom && !feature.ignoreStyleGeom) { // Čia ale toks mano geometrijos už'cache'avimas...
						styleGeom = feature.styleGeom;
					}
					var distanceBetweenLines = 0.3;
					if (!styleGeom) {
						if (geom.getType() == "LineString") {
							styleGeom = [];
							var g1 = geom,
								g2 = new LineString(VectorLayerStyleHelper.offsetLineString(geom, distanceBetweenLines / 2));
							styleGeom.push(g1);
							styleGeom.push(g2);
							feature.styleGeom = styleGeom;
						}
					}
					var styles = [];
					if (styleGeom) {
						var lineDashValue = 1.2 / resolution;
						styles.push(
							new Style({
								stroke: new Stroke({
									width: this.getLineWidth(resolution, 1),
									color: this.whiteColor,
									lineCap: "butt"
								}),
								geometry: styleGeom[0]
							}),
							new Style({
								stroke: new Stroke({
									width: this.getLineWidth(resolution, 1),
									color: this.whiteColor,
									lineDash: [lineDashValue * 2, lineDashValue], // Brūkšniai, tarpai
									lineCap: "butt"
								}),
								geometry: styleGeom[1]
							})
						);
						this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					}
					return styles;
				}.bind(this);
				styles["111"].fnc = function(feature, resolution){
					// Plati ištisinė linija („STOP“ linija)
					var styles = [
						new Style({
							stroke: new Stroke({
								width: 0.6 / resolution,
								color: this.whiteColor,
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["112"].fnc = function(feature, resolution){
					// Iš trikampių sudaryta linija
					if (feature.modifyInAction) {
						return null;
					}
					var styleGeom,
						geom = feature.getGeometry();
					if (feature.styleGeom && !feature.ignoreStyleGeom) { // Čia ale toks mano geometrijos už'cache'avimas...
						styleGeom = feature.styleGeom;
					}
					if (!styleGeom) {
						if (geom.getType() == "LineString") {
							styleGeom = [];
							var densifiedCoordinates = VectorLayerStyleHelper.densifyCoordinates(geom, 1, false),
								point;
							densifiedCoordinates.forEach(function(densifiedCoordinate){
								point = new Point(densifiedCoordinate.c);
								point.set("angle", densifiedCoordinate.a);
								styleGeom.push(point);
							});
							feature.styleGeom = styleGeom;
						}
					}
					var styles = [];
					if (styleGeom) {
						var radius = 0.3 / resolution;
						if (test) {
							styles.push(
								new Style({
									stroke: new Stroke({
										width: 1,
										color: "blue"
									}),
									geometry: geom
								})
							);
							styleGeom.forEach(function(g){
								styles.push(
									new Style({
										image: new CircleStyle({
											radius: 7,
											fill: new Fill({
												color: "red"
											})
										}),
										geometry: g
									})
								);
							});
							styles.push(
								new Style({
									image: new CircleStyle({
										radius: 4,
										fill: new Fill({
											color: "blue"
										})	
									}),
									geometry: new MultiPoint(geom.getCoordinates())
								})
							);
						}
						styleGeom.forEach(function(g){
							var angle = g.get("angle") - Math.PI / 2;
							styles.push(
								new Style({
									image: new RegularShape({
										fill: new Fill({
											color: this.whiteColor
										}),
										points: 3,
										radius: radius,
										rotation: angle,
										displacement: [0, radius / 2]
									}),
									geometry: g
								})
							);
						}.bind(this));
						this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					}
					return styles;
				}.bind(this);
				styles["114"].fnc = function(feature, resolution){
					// Dvi lygiagrečios linijos, sudarytos iš kvadratų
					// Pvz. http://localhost:3001/admin/?l=3&id=EAC4185C-0805-4491-9848-8397C686753D
					var width = feature.get(this.widthFieldName) || 0.6,
						strokeWidth = 0.35 / resolution;
					var styles = [
						new Style({ // FAKE! 
							stroke: new Stroke({
								width: width / resolution + strokeWidth,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: strokeWidth,
								color: this.whiteColor,
								lineDash: [strokeWidth, strokeWidth],
								lineCap: "butt"
							}),
							geometry: function(feature){
								if (feature.styleGeom && !feature.ignoreStyleGeom) { // Čia ale toks mano geometrijos už'cache'avimas...
									return feature.styleGeom;
								}
								var geom = feature.getGeometry();
								if (geom.getType() == "LineString") {
									feature.styleGeom = new GeometryCollection([
										new LineString(VectorLayerStyleHelper.offsetLineString(geom, - width / 2)),
										new LineString(VectorLayerStyleHelper.offsetLineString(geom, width / 2))
									]);
									return feature.styleGeom;
								}
								return geom;
							}.bind(this)
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["122"].fnc = function(feature, resolution){
					// Plati brūkšninė linija, kurios brūkšnių ir tarpų tarp brūkšnių ilgis vienodas
					var lineDashValue = 1 / resolution,
						lineWidth = this.getLineWidth(resolution, 2);
					var styles = [
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: lineWidth,
								color: this.whiteColor,
								lineDash: [lineDashValue, lineDashValue], // Brūkšniai, tarpai
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["125"].fnc = function(feature, resolution){
					// Šachmatų tvarka išdėstyti langeliai
					// Pvz. http://localhost:3001/admin/?l=3&id=C8DEFD70-D865-4C6D-9919-158D7B64764F
					if (feature.modifyInAction) {
						return null;
					}
					var geom = feature.getGeometry(),
						width = 0.3 / resolution,
						offset = width * resolution,
						advanced = false;
					var styles = [
						new Style({
							stroke: new Stroke({
								width: width * 2,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: width,
								color: this.whiteColor,
								lineDash: [width, width],
								lineCap: "butt"
							}),
							geometry: function(){
								var g = new LineString(VectorLayerStyleHelper.offsetLineString(geom, -offset / 2));
								if (advanced) {
									// Kažką daryti su geometrijomis?...
								}
								return g;
							}.bind(this)
						}),
						new Style({
							stroke: new Stroke({
								width: width,
								color: this.whiteColor,
								lineDash: [width, width],
								lineCap: "butt",
								lineDashOffset: width
							}),
							geometry: function(){
								var g = new LineString(VectorLayerStyleHelper.offsetLineString(geom, offset / 2));
								if (advanced) {
									// Kažką daryti su geometrijomis?...
								}
								return g;
							}.bind(this)
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["126"].fnc = function(feature, resolution){
					// Dviguba brūkšninė linija
					// Pvz. http://localhost:3001/admin/?l=3&id=6DB6C392-1CB0-49D6-BD1C-97E37CB2FBE3
					var distanceBetweenLines = 0.3,
						lineDashValue = 0.5 / resolution;
					var styles = [
						new Style({
							stroke: new Stroke({
								width: distanceBetweenLines / resolution,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution, 1),
								color: this.whiteColor,
								lineDash: [lineDashValue, lineDashValue],
								lineCap: "butt"
							}),
							geometry: function(feature){
								if (feature.styleGeom && !feature.ignoreStyleGeom) { // Čia ale toks mano geometrijos už'cache'avimas...
									return feature.styleGeom;
								}
								var geom = feature.getGeometry();
								if (geom.getType() == "LineString") {
									feature.styleGeom = new GeometryCollection([
										new LineString(VectorLayerStyleHelper.offsetLineString(geom, - distanceBetweenLines / 2)),
										new LineString(VectorLayerStyleHelper.offsetLineString(geom, distanceBetweenLines / 2))
									]);
									return feature.styleGeom;
								}
								return geom;
							}.bind(this)
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["127"].fnc = function(feature, resolution){
					// Zigzagas!!!
					// Pvz. http://localhost:3001/admin/?l=3&id=D3FC84F7-AD10-4521-88BC-6F5B7D7ABB42 !!!
					// Pvz. http://localhost:3001/admin/?l=3&id=2616EC0B-5214-40BD-ACBB-E751A34161D5
					// Pvz. http://localhost:3001/admin/?l=3&id=92D2CE17-B58A-4244-AC32-0327D6E453D6
					if (feature.modifyInAction) {
						return null;
					}
					var styles = [
						new Style({
							stroke: new Stroke({
								width: this.getLineWidth(resolution),
								color: this.yellowColor,
								lineCap: "butt"
							}),
							image: new CircleStyle({
								radius: 5,
								fill: new Fill({
									color: "red"
								})
							}),
							geometry: function(feature){
								if (feature.styleGeom && !feature.ignoreStyleGeom) { // Čia ale toks mano geometrijos už'cache'avimas...
									return feature.styleGeom;
								}
								var geom = feature.getGeometry();
								if (geom.getType() == "LineString") {
									var densifiedCoordinates = VectorLayerStyleHelper.densifyCoordinates(geom, 1.1, true),
										densifiedCoordinatesMod = [],
										offset = 1.4;
									densifiedCoordinates.forEach(function(densifiedCoordinate){
										densifiedCoordinatesMod.push(densifiedCoordinate.c);
									});
									var geomNew = new LineString(densifiedCoordinatesMod),
										offset1 = VectorLayerStyleHelper.offsetLineString(geomNew, -offset / 2, true), // TODO! Jei trečias parametras ne "simple", bus bėdų!??
										offset2 = VectorLayerStyleHelper.offsetLineString(geomNew, offset / 2, true), // TODO! Jei trečias parametras ne "simple", bus bėdų!??
										zigzagG = [offset2[0], offset1[0]];
									for (var i = 1; i < offset1.length; i++) {
										if (i % 2) {
											zigzagG.push(offset2[i]);
										} else {
											zigzagG.push(offset1[i]);
										}
									}
									if (!(i % 2)) {
										zigzagG.push(offset1[offset1.length - 1]);
									} else {
										zigzagG.push(offset2[offset2.length - 1]);
									}
									var geometries = [];
									if (test) {
										geometries.push(new MultiPoint(densifiedCoordinatesMod));
										geometries.push(new MultiPoint(offset1));
										geometries.push(new MultiPoint(offset2));
									}
									geometries.push(new LineString(zigzagG));
									feature.styleGeom = new GeometryCollection(geometries);
									return feature.styleGeom;
								}
								return geom;
							}.bind(this)
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["1131"].fnc = function(feature, resolution){
					// Perėja
					var width = (feature.get(this.widthFieldName) || 3) / resolution,
						lineDashValue = 0.5 / resolution;
					var styles = [
						new Style({ // FAKE! 
							stroke: new Stroke({
								width: width,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: width,
								color: this.whiteColor,
								lineDash: [lineDashValue, lineDashValue],
								lineCap: "butt"
							})
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
				styles["1133"].fnc = function(feature, resolution){
					// Dvi lygiagrečios linijos, sudarytos iš stačiakampių
					// Pvz. http://localhost:3001/admin/?l=3&id=86098207-A0A6-4346-83F1-1ACB58E3700E
					// FIXME!!! Šito vizualizacija išsikraipo, jei turime zigzagą! Reiktų geometriją dalinti į atkarpas tik po du taškus? Gal tada simbolizacija būtų gražesnė??!!!
					var width = feature.get(this.widthFieldName) || 3,
						strokeWidth = 0.2 / resolution;
					var styles = [
						new Style({ // FAKE! 
							stroke: new Stroke({
								width: width / resolution + strokeWidth,
								color: this.fakeColor,
								lineCap: "butt"
							})
						}),
						new Style({
							stroke: new Stroke({
								width: strokeWidth,
								color: this.whiteColor,
								lineDash: [strokeWidth * 2, strokeWidth],
								lineCap: "butt"
							}),
							geometry: function(feature){
								if (feature.styleGeom && !feature.ignoreStyleGeom) { // Čia ale toks mano geometrijos už'cache'avimas...
									return feature.styleGeom;
								}
								var geom = feature.getGeometry();
								if (geom.getType() == "LineString") {
									feature.styleGeom = new GeometryCollection([
										new LineString(VectorLayerStyleHelper.offsetLineString(geom, - width / 2)),
										new LineString(VectorLayerStyleHelper.offsetLineString(geom, width / 2))
									]);
									return feature.styleGeom;
								}
								return geom;
							}.bind(this)
						})
					];
					this.addSpecificRenderingIfNeeded(styles, resolution, specificRenderingType, feature);
					return styles;
				}.bind(this);
			}
		},

		getDefaultStyle: function(layerInfo){
			var style,
				defaultColor = "red";
			if (layerInfo.geometryType == "esriGeometryPolyline") {
				style = new Style({
					stroke: new Stroke({
						width: 2,
						color: defaultColor
					})
				});
			} else if (layerInfo.geometryType == "esriGeometryPolygon") {
				style = new Style({
					fill: new Fill({
						color: defaultColor
					})
				});
			}
			return style;
		},

		getGenericStyle: function(layerInfo){
			// Bus naudojamas smulkiam masteliui...
			var style,
				defaultColor = "white";
			if (layerInfo.geometryType == "esriGeometryPolyline") {
				style = new Style({
					stroke: new Stroke({
						width: 1,
						color: defaultColor
					})
				});
			} else if (layerInfo.geometryType == "esriGeometryPolygon") {
				// ...
			}
			return style;
		},

		getLineWidth: function(resolution, multiplyBy){
			if (!multiplyBy) {
				multiplyBy = 1;
			}
			var lineWidth = this.thinLineWidth * multiplyBy;
			if (resolution > 0.2) {
				// Kad smulkiuose masteliuose nebūtų miniatiūrinio plonumo linijų...
				lineWidth = lineWidth * 2;
			}
			lineWidth = lineWidth / resolution / 16;
			return lineWidth;
		},

		getFillColorPattern: function(style, color){
			var canvas = document.createElement("canvas"),
				context = canvas.getContext("2d"),
				dim = 15;
			canvas.width = dim;
			canvas.height = dim;
			context.strokeStyle = color;
			context.beginPath();
			if (style == "1152") {
				context.fillStyle = "rgba(255, 255, 255, 0.5)";
				context.fillRect(5, 5, 9, 9);
			} else if (style == "1153") {
				context.arc(7, 7, 3, 0, 2 * Math.PI, false);
			}
			context.lineWidth = 1;
			context.stroke();
			return context.createPattern(canvas, "repeat");
		},

		addSpecificRenderingIfNeeded: function(style, resolution, specificRenderingType, feature){
			if (specificRenderingType) {
				if (specificRenderingType == "task") {
					var taskStyle = new Style({
						stroke: new Stroke({
							width: this.getLineWidth(resolution) * 2,
							color: VectorLayerStyleHelper.getTaskColor(feature)
						})
					});
					style.push(taskStyle);
				}
			}
		}
	}
</script>