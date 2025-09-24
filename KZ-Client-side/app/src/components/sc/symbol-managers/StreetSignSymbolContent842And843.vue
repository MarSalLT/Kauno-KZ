<template>
	<div>
		<div class="mb-3">Simbolio komponentai / sugeneruotas vaizdas:</div>
		<div class="d-flex">
			<div
				ref="map"
				class="map ml-2 mr-5"
			>
			</div>
			<div>
				<v-radio-group
					dense
					hide-details
					class="pa-0 mt-0 mb-2"
					v-model="params.mainRoadType"
					column
					v-if="mainRoadTypes"
				>
					<template v-for="(mainRoadTypeItem, i) in mainRoadTypes">
						<v-radio
							:label="mainRoadTypeItem.title"
							:value="mainRoadTypeItem.key"
							:key="i"
							:class="[i ? 'mt-2' : null, 'mb-0']"
						></v-radio>
					</template>
					<template v-if="params.mainRoadType == 'custom'">
						<div class="ml-8 mt-2">
							<v-radio-group
								dense
								hide-details
								class="pa-0 ma-0"
								v-model="customMainRoadTypeDrawingTool"
								column
							>
								<v-radio
									label="Kampuota linija, einanti per tris taškus"
									value="three-points"
									class="mb-0"
								></v-radio>
								<v-radio
									value="curve"
									class="mt-2 mb-0"
								>
									<template v-slot:label>
										<div class="mr-2">Sklandi linija</div>
										<v-menu
											open-on-hover
											:close-on-content-click="false"
											tile
										>
											<template v-slot:activator="{on, attrs}">
												<v-btn
													icon
													x-small
													title="Informacija"
													v-on="on"
													v-bind="attrs"
												>
													<v-icon color="grey">mdi-information</v-icon>
												</v-btn>
											</template>
											<div class="pa-3 body-2">
												Spustelkite dviejuose taškuose, žyminčiuose linijos pradžią ir pabaigą, o trečiuoju tašku nurodykite linijos kreivumą
											</div>
										</v-menu>
									</template>
								</v-radio>
							</v-radio-group>
							<v-btn-toggle
								v-model="primaryRoad"
								dense
								class="mt-2"
							>
								<v-btn
									small
									value="1"
									:disabled="Boolean(primaryRoadFeaturesList && primaryRoadFeaturesList.length)"
								>
									Aktyvuoti pagrindinio kelio braižymo įrankį
								</v-btn>
							</v-btn-toggle>
						</div>
					</template>
				</v-radio-group>
				<template v-if="(params.mainRoadType && (params.mainRoadType != 'custom') || (params.mainRoadType == 'custom' && primaryRoadFeaturesList && primaryRoadFeaturesList.length))">
					<v-divider class="mt-4 pt-4"></v-divider>
					<v-btn-toggle
						v-model="secondaryRoad"
						dense
					>
						<v-btn small value="1">
							Aktyvuoti šalutinio kelio braižymo įrankį
						</v-btn>
					</v-btn-toggle>
				</template>
				<template v-if="interactiveFeaturesList && interactiveFeaturesList.length">
					<v-divider class="mt-4 mb-1"></v-divider>
					<ul class="d-inline-block">
						<template v-for="(item, i) in interactiveFeaturesList">
							<li
								:key="i"
								class="mt-1"
								v-on:mouseenter="mouseEnterFeatureListItem(item.feature)"
								v-on:mouseleave="mouseLeaveFeatureListItem(item.feature)"
							>
								<span class="mr-1">{{item.title}}</span>
								<v-btn
									icon
									v-on:click="removeFeature(item.feature)"
								>
									<v-icon
										title="Šalinti objektą"
										color="error"
										size="20"
									>
										mdi-delete
									</v-icon>
								</v-btn>
							</li>
						</template>
					</ul>
				</template>
			</div>
		</div>
	</div>
</template>

<script>
	import Circle from "ol/geom/Circle";
	import ImageCanvasSource from "ol/source/ImageCanvas";
	import ImageLayer from "ol/layer/Image";
	import Feature from "ol/Feature";
	import LineString from "ol/geom/LineString";
	import Map from "ol/Map";
	import MultiPoint from "ol/geom/MultiPoint";
	import Projection from "ol/proj/Projection";
	import VectorLayer from "ol/layer/VectorImage";
	import VectorSource from "ol/source/Vector";
	import View from "ol/View";
	import {Draw, Snap} from "ol/interaction";
	import {defaults as defaultInteractions} from "ol/interaction";
	import {Circle as CircleStyle, Fill, Stroke, Style} from "ol/style";
	import {bezier, booleanPointOnLine, lineString as turfLineString, point as turfPoint} from "@turf/turf";
	import {fromCircle} from "ol/geom/Polygon";

	export default {
		data: function(){
			var data = {
				params: {
					mainRoadType: "vertical"
				},
				customMainRoadTypeDrawingTool: "three-points",
				mainRoadTypes: [{
					title: "Pagrindinis kelias — vertikali linija (tiesiai)",
					key: "vertical"
				},{
					title: "Pagrindinis kelias — horizontali linija",
					key: "horizontal"
				},{
					title: "Pagrindinis kelias — iš apačios į kairę, 90 laipsnių",
					key: "from-bottom-to-left"
				},{
					title: "Pagrindinis kelias — iš apačios į dešinę, 90 laipsnių",
					key: "from-bottom-to-right"
				},{
					title: "Pagrindinis kelias — iš kairės į viršų, 90 laipsnių",
					key: "from-up-to-left"
				},{
					title: "Pagrindinis kelias — iš dešinės į viršų, 90 laipsnių",
					key: "from-up-to-right"
				},{
					title: "Pagrindinis kelias — tiesiai, su vingiu kairiau",
					key: "vertical-with-small-left-curve"
				},{
					title: "Pagrindinis kelias — tiesiai, su vingiu dešiniau",
					key: "vertical-with-small-right-curve"
				},{
					title: "Pagrindinis kelias — žiedas, centre",
					key: "is-roundabout"
				},{
					title: "Pagrindinis kelias kito tipo (piešti patiems)",
					key: "custom"
				}],
				mapDim: 350,
				circleRadius: 70,
				useImageLayer: false,
				primaryRoad: undefined,
				secondaryRoad: undefined,
				primaryRoadFeaturesList: null,
				interactiveFeaturesList: null
			};
			if (this.data.mode == "edit") {
				if (this.data.data) {
					try {
						var initialParams = JSON.parse(this.data.data);
						if (initialParams) {
							if (initialParams.params) {
								data.params = initialParams.params;
							}
							if (initialParams.customMainRoadTypeDrawingTool) {
								data.customMainRoadTypeDrawingTool = initialParams.customMainRoadTypeDrawingTool;
							}
							if (initialParams.lines) {
								this.initialLines = JSON.parse(initialParams.lines);
							}
						}
					} catch {
						// ...
					}
				}
			}
			return data;
		},

		props: {
			data: Object
		},

		mounted: function(){
			this.createMap();
			this.renderCanvas();
			if (this.initialLines && this.vectorLayer) {
				var feature;
				this.initialLines.forEach(function(lineInfo){
					if (lineInfo.type == "custom-primary") {
						feature = new Feature({
							geometry: new LineString(lineInfo.coordinates)
						});
						feature.set("type", "primary");
						feature.set("main-road-type", "custom");
						this.vectorLayer.getSource().addFeature(feature);
					} else if (lineInfo.type == "custom-primary-curve") {
						feature = new Feature({
							geometry: this.getCurvedGeometry(lineInfo.coordinates)
						});
						feature.set("type", "primary");
						feature.set("main-road-type", "custom");
						feature.set("main-road-subtype", "curve");
						feature.set("orig-coordinates", lineInfo.coordinates);
						this.vectorLayer.getSource().addFeature(feature);
					}
				}.bind(this));
				this.initialLines.forEach(function(lineInfo){
					if (lineInfo.type == "secondary") {
						feature = new Feature({
							geometry: new LineString(lineInfo.coordinates)
						});
						this.setFeatureOffsetProperty(feature);
						this.vectorLayer.getSource().addFeature(feature);
					}
				}.bind(this));
				this.initialLines = null;
			}
		},

		methods: {
			renderCanvas: function(){
				if (this.map) {
					// https://jsfiddle.net/jonataswalker/hg5g4yLk/
					if (!this.outlineVectorLayer) {
						this.outlineVectorLayer = new VectorLayer({
							source: new VectorSource(),
							style: function(){
								return [
									new Style({
										stroke: new Stroke({
											width: 3,
											color: "black",
											lineCap: "butt"
										})
									})
								];
							}
						});
						this.map.addLayer(this.outlineVectorLayer);
					} else {
						this.outlineVectorLayer.getSource().clear(true);
					}
					if (!this.vectorLayer) {
						this.vectorLayer = new VectorLayer({
							source: new VectorSource(),
							style: function(feature){
								var featureType = feature.get("type"),
									lineWidth = 10,
									color = "black",
									lineDash;
								if (featureType == "primary") {
									lineWidth = 30;
								} else if (featureType == "outline") {
									lineWidth = 3;
								} else if (featureType == "snap-helper" || featureType == "snap-helper-outline") {
									lineWidth = 2;
									color = "rgba(0, 0, 255, 0.2)";
									lineDash = [5, 5];
								}
								if (feature.get("active")) {
									color = "red";
								}
								return [
									new Style({
										stroke: new Stroke({
											width: lineWidth,
											color: color,
											lineCap: "butt",
											lineDash: lineDash
										}),
										geometry: function(feature){
											var geometry = feature.getGeometry(),
												coordinates,
												lineAngle;
											if (!feature.get("type")) {
												if (!feature.styleGeom) {
													feature.styleGeom = geometry;
													coordinates = geometry.getCoordinates();
													if (coordinates.length == 2) {
														var offsetData = feature.get("offset");
														if (offsetData) {
															if (geometry.getLength() > offsetData.offset) {
																lineAngle = Math.atan2(coordinates[1][1] - coordinates[0][1], coordinates[1][0] - coordinates[0][0]);
																if (offsetData.coordinateI == 0) {
																	feature.styleGeom = new LineString([
																		[coordinates[0][0] + offsetData.offset * Math.cos(lineAngle), coordinates[0][1] + offsetData.offset * Math.sin(lineAngle)],
																		coordinates[1]
																	]);
																} else if (offsetData.coordinateI == 1) {
																	feature.styleGeom = new LineString([
																		coordinates[0],
																		[coordinates[1][0] - offsetData.offset * Math.cos(lineAngle), coordinates[1][1] - offsetData.offset * Math.sin(lineAngle)],
																	]);
																}
															}
														}
													}
												}
												geometry = feature.styleGeom;
											} else if (feature.get("rounded")) {
												if (geometry.getType() == "LineString") {
													if (geometry.getCoordinates().length == 3) {
														if (!feature.styleGeom) {
															feature.styleGeom = geometry;
															coordinates = feature.getGeometry().getCoordinates();
															var newCoordinates = [];
															newCoordinates.push(coordinates[0]);
															newCoordinates = newCoordinates.concat(this.getRoundedCornerCoordinates(coordinates, 40, feature));
															newCoordinates.push(coordinates[coordinates.length - 1]);
															feature.styleGeom = new LineString(newCoordinates);
														}
														geometry = feature.styleGeom;
													}
												}
											}
											return geometry;
										}.bind(this)
									})
								];
							}.bind(this)
						});
						this.vectorLayer.getSource().on("change", function(){ // FIXME! Blogai, nes suveikia net kai hover'iname ant sąrašo? Gi tuo metu keičiasi feature'o `active` savybė...
							this.setInteractiveFeaturesList();
						}.bind(this));
						this.map.addLayer(this.vectorLayer);
					} else {
						this.vectorLayer.getSource().clear(true);
					}
					if (this.useImageLayer) {
						if (!this.imageLayer) {
							this.imageLayer = new ImageLayer({
								source: new ImageCanvasSource({
									canvasFunction: this.canvasFunction
								})
							});
							this.map.addLayer(this.imageLayer);
						}
						this.imageLayer.getSource().changed();
					}
					if (!this.useImageLayer) {
						var feature;
						if (this.params.mainRoadType) {
							if (this.params.mainRoadType != "custom") {
								var geometry,
									halfMapDim = this.mapDim / 2,
									rounded = false,
									curveXOffset = 20,
									curveYOffset = 4;
								switch (this.params.mainRoadType) {
									case "vertical":
										geometry = new LineString([[halfMapDim, 0], [halfMapDim, this.mapDim]]);
										break;
									case "horizontal":
										geometry = new LineString([[0, halfMapDim], [this.mapDim, halfMapDim]]);
										break;
									case "from-bottom-to-left":
										geometry = new LineString([[0, halfMapDim], [halfMapDim, halfMapDim], [halfMapDim, 0]]);
										rounded = true;
										break;
									case "from-bottom-to-right":
										geometry = new LineString([[this.mapDim, halfMapDim], [halfMapDim, halfMapDim], [halfMapDim, 0]]);
										rounded = true;
										break;
									case "from-up-to-left":
										geometry = new LineString([[halfMapDim, this.mapDim], [halfMapDim, halfMapDim], [0, halfMapDim]]);
										rounded = true;
										break;
									case "from-up-to-right":
										geometry = new LineString([[halfMapDim, this.mapDim], [halfMapDim, halfMapDim], [this.mapDim, halfMapDim]]);
										rounded = true;
										break;
									case "vertical-with-small-left-curve":
										geometry = new LineString([
											[halfMapDim + curveXOffset, 0],
											[halfMapDim + curveXOffset, halfMapDim - curveYOffset],
											[halfMapDim - curveXOffset, halfMapDim + curveYOffset],
											[halfMapDim - curveXOffset, this.mapDim]
										]);
										break;
									case "vertical-with-small-right-curve":
										geometry = new LineString([
											[halfMapDim - curveXOffset, 0],
											[halfMapDim - curveXOffset, halfMapDim - curveYOffset],
											[halfMapDim + curveXOffset, halfMapDim + curveYOffset],
											[halfMapDim + curveXOffset, this.mapDim]
										]);
										break;
									case "is-roundabout":
										geometry = new Circle([halfMapDim, halfMapDim], this.circleRadius);
										break;
								}
								if (geometry) {
									feature = new Feature({
										geometry: geometry
									});
									feature.set("rounded", rounded);
									feature.set("type", "primary");
									feature.set("main-road-type", this.params.mainRoadType);
									this.vectorLayer.getSource().addFeature(feature);
								}
							}
						}
						feature = new Feature({
							geometry: new LineString([
								[0, 0],
								[0, this.mapDim],
								[this.mapDim, this.mapDim],
								[this.mapDim, 0],
								[0, 0]
							])
						});
						this.outlineVectorLayer.getSource().addFeature(feature);
						var o = 0; // 8?
						feature = new Feature({
							geometry: new LineString([
								[0 + o, 0 + o],
								[0 + o, this.mapDim - o],
								[this.mapDim - o, this.mapDim - o],
								[this.mapDim - o, 0 + o],
								[0 + o, 0 + o]
							])
						});
						feature.set("type", "snap-helper-outline");
						this.vectorLayer.getSource().addFeature(feature);
					}
				}
			},
			createMap: function(){
				// https://leafletjs.com/examples/crs-simple/crs-simple.html
				// https://gis.stackexchange.com/questions/354906/non-geographical-maps-in-openlayers
				var extent = [0, 0, this.mapDim, this.mapDim];
				var projection = new Projection({
					code: "xkcd-image",
					units: "pixels",
					extent: extent
				});
				this.map = new Map({
					target: this.$refs.map,
					interactions: defaultInteractions({
						dragPan: false,
						mouseWheelZoom: false,
						doubleClickZoom: false
					}),
					controls: [],
					view: new View({
						projection: projection,
						zoom: 5,
						minZoom: 0,
						maxZoom: 10
					})
				});
				setTimeout(function(){ // Visa tai aktualu kai unikalaus simbolio kūrimo aplinką atsidarome tiesiai iš žemėlapio, dialog'e...
					this.map.updateSize();
					this.map.getView().fit(extent);
				}.bind(this), 0);
			},
			canvasFunction: function(extent, resolution, pixelRatio, size){
				var canvas = document.createElement("canvas"),
					context = canvas.getContext("2d"),
					canvasWidth = size[0],
					canvasHeight = size[1];
				canvas.setAttribute("width", canvasWidth);
				canvas.setAttribute("height", canvasHeight);
				var mapExtent = this.map.getView().calculateExtent(this.map.getSize()),
					canvasOrigin = this.map.getPixelFromCoordinate([extent[0], extent[3]]),
					mapOrigin = this.map.getPixelFromCoordinate([mapExtent[0], mapExtent[3]]),
					delta = [mapOrigin[0] - canvasOrigin[0], mapOrigin[1] - canvasOrigin[1]];
				context.lineWidth = 20;
				if (this.params.mainRoadType) {
					if (this.params.mainRoadType != "custom") {
						var halfMapDim = this.mapDim / 2;
						context.save();
						switch (this.params.mainRoadType) {
							case "vertical":
								context.moveTo(delta[0] + halfMapDim, delta[1] - 1);
								context.lineTo(delta[0] + halfMapDim, delta[1] + this.mapDim + 1);
								context.stroke();
								break;
							case "from-bottom-to-left":
								// ...
								break;
							case "from-bottom-to-right":
								// ...
								break;
							case "from-up-to-left":
								// ...
								break;
							case "is-roundabout":
								context.arc(delta[0] + halfMapDim, delta[1] + halfMapDim, 70, 0, 2 * Math.PI, true);
								context.stroke();
								break;
						}
						context.restore();
					}
				}
				return canvas;
			},
			activateSecondaryRoadDrawing: function(){
				if (this.map && this.vectorLayer) {
					if (!this.drawInteraction) {
						this.drawInteraction = new Draw({
							type: "LineString",
							maxPoints: 2,
							geometryFunction: function(coordinates, optGeometry){
								var geometry = optGeometry;
								if (geometry) {
									geometry.setCoordinates(coordinates);
								} else {
									this.addTempSnapFeatures(false, coordinates);
									geometry = new LineString(coordinates);
								}
								return geometry;
							}.bind(this)
						});
						this.drawInteraction.on("drawend", function(e){
							this.setFeatureOffsetProperty(e.feature);
							this.vectorLayer.getSource().addFeature(e.feature);
							this.secondaryRoad = undefined;
						}.bind(this));
						this.map.addInteraction(this.drawInteraction);
						this.addSnapInteraction();
					}
				}
			},
			deactivateSecondaryRoadDrawing: function(){
				if (this.drawInteraction) {
					this.map.removeInteraction(this.drawInteraction);
					this.drawInteraction = null;
				}
				if (this.snapInteraction) {
					this.map.removeInteraction(this.snapInteraction);
					this.snapInteraction = null;
				}
				this.removeTempSnapFeatures();
			},
			activatePrimaryRoadDrawing: function(){
				if (this.map && this.vectorLayer) {
					if (!this.primaryRoadDrawInteraction) {
						if (this.customMainRoadTypeDrawingTool == "three-points") {
							this.primaryRoadDrawInteraction = new Draw({
								type: "LineString",
								maxPoints: 3,
								geometryFunction: function(coordinates, optGeometry){
									var geometry = optGeometry;
									if (geometry) {
										geometry.setCoordinates(coordinates);
									} else {
										geometry = new LineString(coordinates);
									}
									return geometry;
								}.bind(this)
							});
						} else if (this.customMainRoadTypeDrawingTool == "curve") {
							this.primaryRoadDrawInteraction = new Draw({
								type: "LineString",
								maxPoints: 3,
								geometryFunction: function(coordinates, optGeometry){
									// Turime du galų taškus... O trečias taškas žymi tarpinį tašką...
									// ! https://math.stackexchange.com/questions/983875/equation-of-a-curved-line-that-passes-through-3-points/983904
									var geometry = optGeometry;
									if (geometry) {
										geometry.setCoordinates(coordinates);
									} else {
										geometry = new MultiPoint(coordinates);
									}
									return geometry;
								}.bind(this),
								style: function(feature){
									var geometry = feature.getGeometry();
									if (feature.getGeometry().getCoordinates().length == 3) {
										// Įdomūs pvz.: https://codesandbox.io/s/draw-shapes-fr7dz?file=/index.js:111-163
										// Dar įdomesnis http://jsfiddle.net/16nahz4k/ !
										geometry = this.getCurvedGeometry(feature.getGeometry().getCoordinates());
										feature.styleGeom = geometry;
									}
									return new Style({
										image: new CircleStyle({
											radius: 6,
											fill: new Fill({
												color: [0, 153, 255, 1]
											}),
											stroke: new Stroke({
												color: [255, 255, 255, 1],
												width: 3
											})
										}),
										stroke: new Stroke({
											color: [0, 153, 255, 1],
											width: 3,
										}),
										geometry: geometry
									});
								}.bind(this)
							});
						}
						if (this.primaryRoadDrawInteraction) {
							this.primaryRoadDrawInteraction.on("drawend", function(e){
								if (e.feature.getGeometry().getType() == "LineString") {
									// BIG TODO: jei paskutiniai taškai yra outline'o viduje, tai linijas šiek tiek pratęsti... Dabar pvz. jei linija yra įstriža — šioks toks trūkis iki outline'o matomas...
									e.feature.set("type", "primary");
									e.feature.set("main-road-type", "custom");
									this.vectorLayer.getSource().addFeature(e.feature);
								} else if (e.feature.getGeometry().getType() == "MultiPoint") {
									if (e.feature.styleGeom) {
										var coordinates = e.feature.getGeometry().getCoordinates();
										e.feature.setGeometry(e.feature.styleGeom);
										e.feature.set("type", "primary");
										e.feature.set("main-road-type", "custom");
										e.feature.set("main-road-subtype", "curve");
										e.feature.set("orig-coordinates", coordinates);
										this.vectorLayer.getSource().addFeature(e.feature);
									}
								}
								this.primaryRoad = undefined;
							}.bind(this));
							this.map.addInteraction(this.primaryRoadDrawInteraction);
							this.addPrimaryRoadSnapInteraction();
						}
					}
				}
			},
			getCurvedGeometry: function(coordinates){
				coordinates = [coordinates[0], coordinates[2], coordinates[1]];
				var line = {
					type: "Feature",
					geometry: {
						type: "LineString",
						coordinates: coordinates
					}
				};
				var curved = bezier(line),
					geometry = new LineString(curved.geometry.coordinates);
				return geometry;
			},
			deactivatePrimaryRoadDrawing: function(){
				if (this.primaryRoadDrawInteraction) {
					this.map.removeInteraction(this.primaryRoadDrawInteraction);
					this.primaryRoadDrawInteraction = null;
				}
				if (this.primaryRoadSnapInteraction) {
					this.map.removeInteraction(this.primaryRoadSnapInteraction);
					this.primaryRoadSnapInteraction = null;
				}
				this.removeTempSnapFeatures();
			},
			addSnapInteraction: function(){
				if (!this.snapInteraction) {
					this.snapInteraction = new Snap({
						source: this.vectorLayer.getSource()
					});
					this.map.addInteraction(this.snapInteraction);
				}
				setTimeout(function(){
					this.addTempSnapFeatures(true);
				}.bind(this));
			},
			addPrimaryRoadSnapInteraction: function(){
				if (!this.primaryRoadSnapInteraction) {
					this.primaryRoadSnapInteraction = new Snap({
						source: this.vectorLayer.getSource()
					});
					this.map.addInteraction(this.primaryRoadSnapInteraction);
				}
				setTimeout(function(){
					this.addTempSnapFeatures(true);
				}.bind(this));
			},
			addTempSnapFeatures: function(initial, coordinates){
				// Reikia daryti ciklą per visus feature'us ir rasti pagrindinį gal?.. Ar nebūtina?..
				// Čia įdomus pvz.: https://viglino.github.io/ol-ext/examples/interaction/map.interaction.snapguides.html
				var geometries = [],
					halfMapDim = this.mapDim / 2;
				if (initial) {
					if (this.params.mainRoadType == "from-bottom-to-left") {
						geometries.push(new LineString([[halfMapDim, halfMapDim], [this.mapDim, halfMapDim]]));
						geometries.push(new LineString([[halfMapDim, halfMapDim], [halfMapDim, this.mapDim]]));
					} else if (this.params.mainRoadType == "from-bottom-to-right") {
						geometries.push(new LineString([[halfMapDim, halfMapDim], [halfMapDim, this.mapDim]]));
						geometries.push(new LineString([[halfMapDim, halfMapDim], [0, halfMapDim]]));
					} else if (this.params.mainRoadType == "from-up-to-left") {
						geometries.push(new LineString([[halfMapDim, halfMapDim], [this.mapDim, halfMapDim]]));
						geometries.push(new LineString([[halfMapDim, halfMapDim], [halfMapDim, 0]]));
					} else if (this.params.mainRoadType == "from-up-to-right") {
						geometries.push(new LineString([[0, halfMapDim], [halfMapDim, halfMapDim]]));
						geometries.push(new LineString([[halfMapDim, halfMapDim], [halfMapDim, 0]]));
					} else if (["vertical-with-small-left-curve", "vertical-with-small-right-curve"].indexOf(this.params.mainRoadType) != -1) {
						geometries.push(new LineString([[0, halfMapDim], [this.mapDim, halfMapDim]]));
					} else if (this.params.mainRoadType == "is-roundabout") {
						geometries.push(new LineString([[halfMapDim - this.circleRadius, halfMapDim], [0, halfMapDim]]));
						geometries.push(new LineString([[halfMapDim, halfMapDim + this.circleRadius], [halfMapDim, this.mapDim]]));
						geometries.push(new LineString([[halfMapDim + this.circleRadius, halfMapDim], [this.mapDim, halfMapDim]]));
						geometries.push(new LineString([[halfMapDim, halfMapDim - this.circleRadius], [halfMapDim, 0]]));
					} else if (this.params.mainRoadType == "custom") {
						if (this.primaryRoadFeaturesList && this.primaryRoadFeaturesList.length) {
							// FIXME! Čia galima visai kitokį sprendimą padaryti... Reiktų analizuoti this.primaryRoadFeaturesList turinį ir pagal jį atitinkamai reaguoti?..
							/*
							geometries.push(new LineString([[halfMapDim, 0], [halfMapDim, this.mapDim]]));
							geometries.push(new LineString([[0, halfMapDim], [this.mapDim, halfMapDim]]));
							*/
						} else {
							geometries.push(new LineString([[halfMapDim, 0], [halfMapDim, this.mapDim]]));
							geometries.push(new LineString([[0, halfMapDim], [this.mapDim, halfMapDim]]));
						}
					}
				} else {
					var xCoordinate,
						yCoordinate;
					if (["vertical", "vertical-with-small-left-curve", "vertical-with-small-right-curve"].indexOf(this.params.mainRoadType) != -1) {
						yCoordinate = coordinates[0][1];
						if (yCoordinate > 0 && yCoordinate < this.mapDim) {
							geometries.push(new LineString([[0, yCoordinate], [this.mapDim, yCoordinate]]));
						}
					} else if (["horizontal"].indexOf(this.params.mainRoadType) != -1) {
						xCoordinate = coordinates[0][0];
						if (xCoordinate > 0 && xCoordinate < this.mapDim) {
							geometries.push(new LineString([[xCoordinate, 0], [xCoordinate, this.mapDim]]));
						}
					} else if (this.params.mainRoadType == "custom") {
						yCoordinate = coordinates[0][1];
						if (yCoordinate > 0 && yCoordinate < this.mapDim) {
							geometries.push(new LineString([[0, yCoordinate], [this.mapDim, yCoordinate]]));
						}
					}
				}
				geometries.forEach(function(geometry){
					var feature = new Feature({
						geometry: geometry,
						properties: {
							type: "snap-helper"
						}
					});
					feature.set("type", "snap-helper");
					this.vectorLayer.getSource().addFeature(feature);
				}.bind(this));
			},
			removeTempSnapFeatures: function(){
				if (this.vectorLayer) {
					var source = this.vectorLayer.getSource();
					source.forEachFeature(function(feature){
						if (feature.get("type") == "snap-helper") {
							source.removeFeature(feature);
						}
					});
				}
			},
			setFeatureOffsetProperty: function(feature){
				// Dabar reikia rasti ar vienas kuris galas liečia pagr. feature'ą... Jei liečia, tai per kažkokį atstumą render'inant reikės patrumpinti liniją...
				if (this.vectorLayer) {
					var source = this.vectorLayer.getSource(),
						primaryRoadFeature;
					source.forEachFeature(function(feature){
						if (feature.get("type") == "primary") {
							primaryRoadFeature = feature;
						}
					});
					if (primaryRoadFeature) {
						if (feature.getGeometry().getType() == "LineString") {
							var featureCoordinates = feature.getGeometry().getCoordinates(),
								primaryRoadFeatureGeometryType = primaryRoadFeature.getGeometry().getType(),
								primaryRoadFeatureTurfLineString;
							if (primaryRoadFeatureGeometryType == "LineString") {
								primaryRoadFeatureTurfLineString = turfLineString(primaryRoadFeature.getGeometry().getCoordinates());
							} else if (primaryRoadFeatureGeometryType == "Circle") {
								var polygonFromCircle = fromCircle(primaryRoadFeature.getGeometry());
								primaryRoadFeatureTurfLineString = turfLineString(polygonFromCircle.getCoordinates()[0]); // Deja, netinka, nes booleanPointOnLine gan griežtai tikrina?..
							}
							if (primaryRoadFeatureTurfLineString) {
								if (featureCoordinates.length == 2) {
									var offsetFromPrimaryRoadFeature = 30;
									featureCoordinates.some(function(coordinate, i){
										var isOffset;
										if (primaryRoadFeatureGeometryType == "Circle") {
											// Apskritimui kitokia logika... Tikriname kiek toli yra dominantis taškas nuo apskritimo centro...
											var distanceFromCircleCenter = new LineString([primaryRoadFeature.getGeometry().getCenter(), coordinate]).getLength(),
												dif = (distanceFromCircleCenter - primaryRoadFeature.getGeometry().getRadius());
											if (0 <= dif && dif < offsetFromPrimaryRoadFeature) {
												isOffset = true;
											}
										} else {
											if (booleanPointOnLine(turfPoint(coordinate), primaryRoadFeatureTurfLineString)) {
												isOffset = true;
											}
										}
										if (isOffset) {
											feature.set("offset", {
												coordinateI: i,
												offset: offsetFromPrimaryRoadFeature
											});
											return true;
										}
									});
								}
							}
						} else {
							console.log("OTHER GEOM TYPE?...", feature.getGeometry().getType());
						}
					}
				}
			},
			setInteractiveFeaturesList: function(){
				var interactiveFeaturesList,
					primaryRoadFeaturesList;
				if (this.vectorLayer) {
					interactiveFeaturesList = [];
					primaryRoadFeaturesList = [];
					this.vectorLayer.getSource().forEachFeature(function(feature){
						if (!feature.get("type")) {
							interactiveFeaturesList.push({
								feature: feature,
								title: "Šalutinis kelias nr. " + (interactiveFeaturesList.length + 1)
							});
						} else {
							if (feature.get("main-road-type") == "custom") {
								primaryRoadFeaturesList.push({
									feature: feature,
									title: "Pagrindinis kelias nr. " + (primaryRoadFeaturesList.length + 1)
								});
							}
						}
					});
					interactiveFeaturesList = primaryRoadFeaturesList.concat(interactiveFeaturesList);
				}
				this.interactiveFeaturesList = interactiveFeaturesList;
				this.primaryRoadFeaturesList = primaryRoadFeaturesList;
			},
			removeFeature: function(feature){
				if (this.vectorLayer) {
					this.vectorLayer.getSource().removeFeature(feature);
				}
			},
			mouseEnterFeatureListItem: function(feature){
				if (feature) {
					feature.set("active", true);
				}
			},
			mouseLeaveFeatureListItem: function(feature){
				if (feature) {
					feature.set("active", false);
				}
			},
			getRoundedCornerCoordinates: function(coordinates, roundRadius, feature){
				var curveCoordinates = [],
					lineAngle;
				for (var i = 0; i <= coordinates.length - 2; i++) {
					var coordinate = coordinates[i],
						nextCoordinate = coordinates[i + 1];
					lineAngle = Math.atan2(nextCoordinate[1] - coordinate[1], nextCoordinate[0] - coordinate[0]);
					if (i == 0) {
						curveCoordinates.push([nextCoordinate[0] - roundRadius * Math.cos(lineAngle), nextCoordinate[1] - roundRadius * Math.sin(lineAngle)]);
					} else if (i == 1) {
						curveCoordinates.push([coordinate[0] + roundRadius * Math.cos(lineAngle), coordinate[1] + roundRadius * Math.sin(lineAngle)]);
					}
				}
				var middlePointCoordinates = [(curveCoordinates[0][0] + curveCoordinates[1][0]) / 2, (curveCoordinates[0][1] + curveCoordinates[1][1]) / 2],
					circleCenterCoordinates = [
						middlePointCoordinates[0] * 2 - coordinates[1][0],
						middlePointCoordinates[1] * 2 - coordinates[1][1]
					];
				var degrees,
					curvePointAngle,
					newCurveCoordinates = [];
				// FIXME! Šitą vietą reikia sutvarkyti, kad būtų teisingesnė logika...
				switch (feature.get("main-road-type")) {
					case "from-bottom-to-left":
						degrees = [90, 75, 60, 45, 30, 15, 0];
						degrees.forEach(function(degree){
							curvePointAngle = degree * Math.PI / 180;
							newCurveCoordinates.push([circleCenterCoordinates[0] + roundRadius * Math.cos(curvePointAngle), circleCenterCoordinates[1] + roundRadius * Math.sin(curvePointAngle)]);
						});
						break;
					case "from-bottom-to-right":
						degrees = [270, 285, 300, 315, 330, 345, 360];
						degrees.forEach(function(degree){
							curvePointAngle = degree * Math.PI / 180;
							newCurveCoordinates.push([circleCenterCoordinates[0] - roundRadius * Math.cos(curvePointAngle), circleCenterCoordinates[1] - roundRadius * Math.sin(curvePointAngle)]);
						});
						break;
					case "from-up-to-left":
						degrees = [360, 345, 330, 315, 300, 285, 270];
						degrees.forEach(function(degree){
							curvePointAngle = degree * Math.PI / 180;
							newCurveCoordinates.push([circleCenterCoordinates[0] + roundRadius * Math.cos(curvePointAngle), circleCenterCoordinates[1] + roundRadius * Math.sin(curvePointAngle)]);
						});
						break;
					case "from-up-to-right":
						degrees = [0, 15, 30, 45, 60, 75, 90];
						degrees.forEach(function(degree){
							curvePointAngle = degree * Math.PI / 180;
							newCurveCoordinates.push([circleCenterCoordinates[0] - roundRadius * Math.cos(curvePointAngle), circleCenterCoordinates[1] - roundRadius * Math.sin(curvePointAngle)]);
						});
						break;
				}
				return newCurveCoordinates;
			},
			getDataUrl: function(){
				var map = this.map;
				if (map) {
					var mapCanvas = document.createElement("canvas"),
						size = map.getSize();
					mapCanvas.width = size[0];
					mapCanvas.height = size[1];
					var mapContext = mapCanvas.getContext("2d");
					Array.prototype.forEach.call(
						this.$refs.map.querySelectorAll(".ol-layer canvas"),
						function(canvas){
							if (canvas.width > 0) {
								var opacity = canvas.parentNode.style.opacity;
								mapContext.globalAlpha = opacity === "" ? 1 : Number(opacity);
								var transform = canvas.style.transform;
								var matrix = transform
								.match(/^matrix\(([^(]*)\)$/)[1]
								.split(",")
								.map(Number);
								CanvasRenderingContext2D.prototype.setTransform.apply(
									mapContext,
									matrix
								);
								mapContext.drawImage(canvas, 0, 0);
							}
						}
					);
					var outputCanvas = document.createElement("canvas");
					outputCanvas.width = 70;
					outputCanvas.height = outputCanvas.width * mapCanvas.height / mapCanvas.width;
					var outputContext = outputCanvas.getContext("2d");
					outputContext.fillStyle = "white";
					outputContext.fillRect(0, 0, outputCanvas.width, outputCanvas.height);
					outputContext.drawImage(mapCanvas, 0, 0, mapCanvas.width, mapCanvas.height, 0, 0, outputCanvas.width, outputCanvas.height);
					// Čia dar toks mažas hack'as outline'ui... TODO: idealiu atveju reiktų ne mažinti esamą map'ą-simbolį, o kurti nuo nulio mažesnę simbolio versiją? Bet daug reikalų, visai neapsimoka...
					outputContext.lineWidth = 2;
					outputContext.strokeStyle="#000000";
					outputContext.strokeRect(0, 0, outputCanvas.width, outputCanvas.height);
					// ...
					return outputCanvas.toDataURL();
				}
			},
			getAllData: function(){
				var data = {
					params: this.params,
					customMainRoadTypeDrawingTool: this.customMainRoadTypeDrawingTool,
					dataURL: this.getDataUrl()
				};
				if (this.vectorLayer) {
					var source = this.vectorLayer.getSource(),
						lines = [];
					source.forEachFeature(function(feature){
						if (!feature.get("type")) {
							lines.push({
								type: "secondary",
								coordinates: feature.getGeometry().getCoordinates()
							});
						} else {
							if (feature.get("type") == "primary") {
								if (feature.get("main-road-type") == "custom") {
									if (feature.get("main-road-subtype") == "curve") {
										lines.push({
											type: "custom-primary-curve",
											coordinates: feature.get("orig-coordinates")
										});
									} else {
										lines.push({
											type: "custom-primary",
											coordinates: feature.getGeometry().getCoordinates()
										});
									}
								}
							}
						}
					});
					if (lines.length) {
						data.lines = JSON.stringify(lines);
					}
				}
				return data;
			}
		},

		watch: {
			params: {
				deep: true,
				immediate: true,
				handler: function(){
					this.renderCanvas();
				}
			},
			"params.mainRoadType": {
				immediate: true,
				handler: function(){
					this.primaryRoad = false;
					this.secondaryRoad = false;
				}
			},
			secondaryRoad: {
				immediate: true,
				handler: function(active){
					if (active) {
						this.activateSecondaryRoadDrawing();
					} else {
						this.deactivateSecondaryRoadDrawing();
					}
				}
			},
			primaryRoad: {
				immediate: true,
				handler: function(active){
					if (active) {
						this.activatePrimaryRoadDrawing();
					} else {
						this.deactivatePrimaryRoadDrawing();
					}
				}
			},
			customMainRoadTypeDrawingTool: {
				immediate: true,
				handler: function(customMainRoadTypeDrawingTool){
					if (customMainRoadTypeDrawingTool && this.primaryRoad) {
						this.deactivatePrimaryRoadDrawing();
						this.activatePrimaryRoadDrawing();
					}
				}
			},
			primaryRoadFeaturesList: {
				immediate: true,
				handler: function(primaryRoadFeaturesList){
					if (primaryRoadFeaturesList && primaryRoadFeaturesList.length) {
						// this.secondaryRoad = false; // Bent jau aktualu kai yra aktyvuotas šalutinės linijos braižymas ir mes šiukšlių dėžės mygtuku pašaliname pagr. kelio feature'ą...
						// NEPASITEISINO! Reikia kito sprendimo?..
					}
				}
			}
		}
	};
</script>

<style scoped>
	.map {
		background-color: white;
		width: 350px;
		height: 350px;
		/* border: 2px solid black; */
		box-sizing: content-box;
	}
	li {
		cursor: default;
	}
	li:hover {
		background-color: #ebebeb;
	}
</style>