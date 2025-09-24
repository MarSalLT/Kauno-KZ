<template>
	<div
		v-if="activeFeature || activeAction"
		class="d-flex stop-event popup-wrapper"
	>
		<FeaturePopupActionButtons
			v-if="activeFeature && activeFeature.featureDescr && activeFeature.featureDescr != 'error'"
			:data="activeFeature"
			:editingActive="editingActive"
			:lineCuttingActive="lineCuttingActive"
			:lineJoiningActive="lineJoiningActive"
			:mode="mode"
			:setEditingActive="setEditingActive"
			:setToggleLineCuttingActive="setToggleLineCuttingActive"
			:setToggleLineJoiningActive="setToggleLineJoiningActive"
			:getFeaturePopupContent="getFeaturePopupContent"
			:setMode="setMode"
		/>
		<div class="popup d-flex flex-column">
			<div :class="['header d-flex align-center pa-1', activeProject ? 'success' : null]">
				<div>
					<v-btn
						text
						:class="['body-2', activeProject ? 'white--text' : null]"
						small
						v-on:click="zoomTo"
						:disabled="!(activeFeature && activeFeature.feature)"
					>
						<v-icon left size="18">mdi-magnify-plus</v-icon> Parodyti visą
					</v-btn>
				</div>
				<div class="d-flex align-center flex-grow-1 justify-end">
					<ActiveFeatureSelector />
					<div>
						<v-btn
							icon
							v-on:click="close"
							class="ml-1"
							small
							:color="activeProject ? 'white' : null"
						>
							<v-icon
								title="Uždaryti"
							>
								mdi-close
							</v-icon>
						</v-btn>
					</div>
				</div>
			</div>
			<div class="content px-3" ref="content">
				<div class="my-3">
					<template v-if="activeFeature && activeFeature.featureDescr">
						<template v-if="activeFeature.featureDescr == 'error'">
							<v-alert
								dense
								type="error"
								class="ma-0"
							>
								Atsiprašome, įvyko nenumatyta klaida...
							</v-alert>
						</template>
						<template v-else>
							<template v-if="activeProject">
								<v-alert
									dense
									type="info"
									class="d-none ma-0 mb-2"
								>
									Aktyvi užduotis...
								</v-alert>
							</template>
							<FeaturePopupContent
								:data="activeFeature"
								:editingActive="editingActive"
								:mode="mode"
								:activeNewFeatureData="activeNewFeatureData"
								ref="featurePopupContent"
							>
							</FeaturePopupContent>
						</template>
					</template>
					<template v-else-if="activeFeature && activeFeature.queryResult">
						<FeaturePopupContent
							:data="{
								feature: activeFeature.queryResult.feature,
								featureType: 'general-object'
							}"
						>
						</FeaturePopupContent>
					</template>
					<template v-else-if="activeAction && activeAction.res">
						<template v-if="!activeAction.res.count">
							Įrašų nėra...
						</template>
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
			</div>
		</div>
	</div>
</template>

<script>
	import ActiveFeatureSelector from "./ActiveFeatureSelector";
	import CommonHelper from "./helpers/CommonHelper";
	import Draw from "ol/interaction/Draw";
	import Feature from "ol/Feature";
	import FeaturePopupActionButtons from "./FeaturePopupActionButtons";
	import FeaturePopupContent from "./FeaturePopupContent";
	import LineString from "ol/geom/LineString";
	import {Circle as CircleStyle, Fill, Stroke, Style} from "ol/style";
	import {lineString as turfLineString, lineSplit as turfLineSplit, lineIntersect as turfLineIntersect, polygon as turfPolygon} from "@turf/turf";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import Select from "ol/interaction/Select";

	export default {
		data: function(){
			var data = {
				editingActive: false,
				lineCuttingActive: false,
				lineJoiningActive: false,
				mode: null,
				activeNewFeatureData: null,
				lineJoiningByPolygonBoundary: false,
				lineJoinPossibleFeature: null
			};
			return data;
		},

		computed: {
			activeFeature: {
				get: function(){
					return this.$store.state.activeFeature;
				}
			},
			activeAction: {
				get: function(){
					return null; // `Usability` klausimas... Seniau buvo -> return this.$store.state.activeAction;
				}
			},
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			activeProject: {
				get: function(){
					return this.$store.state.activeProject;
				}
			},
			activeTask: {
				get: function(){
					var activeTask = null;
					if (this.$store.state.activeTask) {
						var feature = this.$store.state.activeTask.feature;
						if (feature && feature != "error") {
							activeTask = this.$store.state.activeTask;
						}
					}
					return activeTask;
				}
			}
		},

		created: function(){
			this.$vBus.$on("new-feature-drawn", this.newFeatureDrawn);
			this.$vBus.$on("my-field-value-changed", this.myFieldValueChanged);
		},

		beforeDestroy: function(){
			this.$vBus.$off("new-feature-drawn", this.newFeatureDrawn);
			this.$vBus.$off("my-field-value-changed", this.myFieldValueChanged);
		},

		components: {
			ActiveFeatureSelector,
			FeaturePopupActionButtons,
			FeaturePopupContent
		},

		methods: {
			close: function(){
				CommonHelper.routeTo({
					router: this.$router,
					vBus: this.$vBus
				});
			},
			zoomTo: function(){
				if (this.mode == "new-feature-vertical-sign") {
					if (this.activeNewFeatureData) {
						this.myMap.zoomToFeatures([this.activeNewFeatureData.feature]);
					}
				} else {
					this.myMap.zoomToFeatures([this.activeFeature.feature]);
				}
			},
			setEditingActive: function(editingActive){
				this.editingActive = editingActive;
			},
			setToggleLineCuttingActive: function(lineCuttingActive){
				this.lineCuttingActive = lineCuttingActive;
			},
			setToggleLineJoiningActive: function(lineJoiningActive){
				this.lineJoiningActive = lineJoiningActive;
			},
			getFeaturePopupContent: function(){
				return this.$refs.featurePopupContent;
			},
			newFeatureDrawn: function(e){
				var featureType = e.templateData.featureType,
					layerInfo = this.myMap.getLayerInfo(featureType);
				if (layerInfo) {
					if (layerInfo.typeIdField) {
						e.feature.set(layerInfo.typeIdField, e.templateData.id);
					}
				}
				this.myMap.setLayerForFeatureByFeatureType(e.feature, featureType); // Realiai kol kas reikia tik vertikaliesiems KŽ, kad išgauti origRotation'ą!
				if (featureType == "verticalStreetSigns") {
					e.feature.set("STATUSAS", 1);
					e.feature.set("PATVIRTINTAS", 0);
				} else if (featureType == "verticalStreetSignsSupports") {
					e.feature.set("STATUSAS", 1);
					e.feature.set("PATVIRTINIMAS", 0);
				}
				CommonHelper.adjustFeatureProperties(e.feature, featureType, true);
				this.activeNewFeatureData = {
					featureType: featureType,
					feature: e.feature,
					isNew: true
				};
				if (featureType == "verticalStreetSigns") {
					// Vertikaliųjų KŽ kūrime visai kitokia logika...
					this.activeNewFeatureData.feature.refreshAdditionalFeatures = "street-sign-vertical"; // Reikia tik showActiveFeaturePreview() sąlygai, kad "permetus" žvilgsnį į naujai pridėtą KŽ nepradingtų tarpinės linijos...
					this.$store.commit("setActiveFeaturePreview", this.activeNewFeatureData.feature); // "Permetame" žvilgsnį nuo atramos objekto į naujai pridėtą KŽ!
					if (e.refData) {
						this.activeNewFeatureData.additionalData = e.refData.additionalData; // Prireiks tarpinių linijų kūrimui...
						this.activeNewFeatureData.type = e.refData.type;
					}
					this.setMode("new-feature-vertical-sign");
				} else {
					var activeFeature = {
						featureDescr: {},
						feature: e.feature,
						featureType: featureType,
						isNew: true
					};
					this.$store.commit("setActiveFeature", activeFeature);
				}
			},
			setMode: function(mode){
				this.mode = mode;
			},
			activateGeometryEditing: function(){
				var data = this.activeFeature;
				if ((this.mode == "new-feature") || (this.mode == "new-feature-vertical-sign")) {
					data = this.activeNewFeatureData;
				}
				var success,
					errorMessage,
					e = this.myMap.drawHelper.activateGeometryEditing(data);
				if (e.success) {
					this.$vBus.$emit("deactivate-interactions", "feature-editing");
					success = true;
				} else {
					errorMessage = e.message;
				}
				if (!success) {
					this.setEditingActive(false);
					this.$vBus.$emit("show-message", {
						type: "warning",
						message: errorMessage
					});
				} else {
					this.setToggleLineCuttingActive(false);
					this.setToggleLineJoiningActive(false);
				}
			},
			myFieldValueChanged: function(e){
				if (this.myMap.drawHelper && this.myMap.drawHelper.activeGeometryEditingData) {
					var origFeature = this.myMap.drawHelper.activeGeometryEditingData.origFeature;
					if (origFeature) {
						origFeature.styleGeom = null;
						origFeature.set(e.name, e.val);
						if (origFeature.layer && origFeature.layer.typeIdField && (origFeature.layer.typeIdField == e.name)) {
							// Kol kas aktualu tik jei keičiasi tipas... Ateityje gali tekti ir praplėsti...
							CommonHelper.adjustFeatureProperties(origFeature, this.myMap.drawHelper.activeGeometryEditingData.featureType);
						}
						if (origFeature.origRotation) {
							if (origFeature.origRotation.field == e.name) {
								this.$vBus.$emit("street-sign-feature-rotation-changed", {
									feature: origFeature,
									skipField: true
								});
							}
						}
						if (this.myMap.drawHelper.activeGeometryEditingData.featureType == "verticalStreetSigns") {
							if (origFeature.layer) {
								var typeIdField = origFeature.layer.typeIdField;
								if (typeIdField == e.name) {
									this.$vBus.$emit("street-sign-feature-nr-changed", {
										feature: origFeature,
										val: e.val
									});
								}
							}
						}
						if (e.name == "PRIEZIURA") {
							this.$vBus.$emit("on-supervision-val-change", {
								feature: origFeature,
								val: e.val
							});
						}
					} else {
						console.warn("myFieldValueChanged... no origFeature!"); // TODO, FIXME!..
					}
				}
			},
			activateLineCutting: function(){
				this.$vBus.$emit("deactivate-interactions", "line-cutting");
				var style = new Style({
					stroke: new Stroke({
						color: "red",
						width: 2,
						lineDash: [3, 3]
					}),
					image: new CircleStyle({
						radius: 5,
						fill: new Fill({
							color: "red"
						})	
					})
				});
				if (!this.lineCuttingDrawLayer) {
					this.lineCuttingDrawLayer = new VectorLayer({
						source: new VectorSource(),
						style: style,
						zIndex: 999
					});
					this.myMap.map.addLayer(this.lineCuttingDrawLayer);
				}
				if (!this.lineCuttingDrawInteraction) {
					this.lineCuttingDrawInteraction = new Draw({
						type: "LineString",
						source: this.lineCuttingDrawLayer.getSource(),
						style: style,
						stopClick: true
					});
					this.lineCuttingDrawInteraction.on("drawend", function(e){
						// TODO: naudoti kažką tokio? http://www.cercalia.com/api/v5/examples/clickcutlinestring.html
						// Padaryti kažką tokio: https://openlayers.org/en/latest/examples/tracing.html
						// T. y. jau vedant pele kažkaip skirtingai vaizduoti ant žemėlapio perkirptas linijas?..
						// O gal norma... Tiesiog ant viršaus vaizduoti dar dvi linijas...
						// https://turfjs.org/docs/#lineSplit
						var line = turfLineString(this.activeFeature.feature.getGeometry().getCoordinates()),
							splitter = turfLineString(e.feature.getGeometry().getCoordinates()),
							split = turfLineSplit(line, splitter);
						if (split.features && split.features.length) {
							this.$vBus.$emit("confirm", {
								title: "Ar tvirtinti linijos dalijimą?",
								message: "Linijos dalijimo metu bus sukurtas papildomas objektas duomenų rinkinyje. Papildomas objektas turės tokias pat atributines reikšmes kaip ir dalijamas objektas.",
								positiveActionTitle: "Tvirtinti linijos dalijimą",
								negativeActionTitle: "Atšaukti",
								delayedPositive: true,
								positive: function(dialog){
									var positiveCallback = function(){
										var splitResultFeatures = [];
										split.features.forEach(function(splitFeature){
											var feature = new Feature({
												geometry: new LineString(splitFeature.geometry.coordinates)
											});
											splitResultFeatures.push(feature);
										}.bind(this));
										if (this.myMap.drawHelper) {
											dialog.loading = true;
											this.myMap.drawHelper.saveSplitFeature(this.activeFeature, splitResultFeatures, this.activeTask).then(function(){
												if (splitResultFeatures[0]) {
													var feature = splitResultFeatures[0];
													if (this.activeTask) {
														feature.isTasksRelated = true;
													}
													this.myMap.setLayerForFeatureByFeatureType(feature, this.activeFeature.featureType);
													this.$store.commit("setActiveFeaturePreview", feature);
													CommonHelper.routeTo({
														router: this.$router,
														vBus: this.$vBus,
														feature: feature
													});
												} else {
													CommonHelper.routeTo({
														router: this.$router,
														vBus: this.$vBus
													});
												}
												dialog.loading = false;
												dialog.dialog = false;
											}.bind(this), function(){
												dialog.loading = false;
												this.$vBus.$emit("show-message", {
													type: "warning",
													message: "Atsiprašome, įvyko nenumatyta klaida... Linija nebuvo padalinta"
												});
											}.bind(this));
										}
									}.bind(this);
									positiveCallback();
								}.bind(this),
								negative: function(){
									this.lineCuttingActive = false;
								}.bind(this)
							});
						} else {
							setTimeout(function(){
								if (this.lineCuttingDrawLayer) {
									this.lineCuttingDrawLayer.getSource().clear(true);
								}
							}.bind(this), 0);
						}
					}.bind(this));
					this.myMap.addInteraction(this.lineCuttingDrawInteraction);
				}
			},
			deactivateLineCutting: function(){
				if (this.lineCuttingDrawLayer) {
					this.myMap.map.removeLayer(this.lineCuttingDrawLayer);
					this.lineCuttingDrawLayer = null;
				}
				if (this.lineCuttingDrawInteraction) {
					this.myMap.removeInteraction(this.lineCuttingDrawInteraction);
					this.lineCuttingDrawInteraction = null;
				}
			},
			activateLineJoining: function(){
				this.$vBus.$emit("deactivate-interactions", "line-joining");
				if (!this.lineJoiningDrawLayer) {
					this.lineJoiningDrawLayer = new VectorLayer({
						source: new VectorSource(),
						zIndex: 999
					});
					this.myMap.map.addLayer(this.lineJoiningDrawLayer);
				}
				if (this.lineJoiningByPolygonBoundary) {
					this.$vBus.$emit("show-message", {
						message: "Nubrėžkite daugiakampį, į kurį patenka objektai", // TODO: pateikti detalesnį pranešimą... Pvz., kad turi sutapti `KET nr.`?..
						timeout: 8000
					});
					if (!this.lineJoiningDrawInteraction) {
						this.lineJoiningDrawInteraction = new Draw({
							type: "Polygon",
							source: this.lineJoiningDrawLayer.getSource(),
							stopClick: true
						});
						this.lineJoiningDrawInteraction.on("drawend", function(e){
							var features = [];
							if (this.activeFeature && this.activeFeature.feature) {
								var source = null,
									serviceId = this.activeFeature.type == "v" ? "street-signs-vertical" : "street-signs",
									layerId = parseInt(this.activeFeature.layerId);
								this.myMap.map.getLayers().forEach(function(layer){
									if (layer.service) {
										if (layer.service.id == serviceId && layer.getLayers) {
											layer.getLayers().forEach(function(l){
												if (layerId === l.layerId) {
													source = l.getSource();
												}
											}.bind(this));
										}
									}
								}.bind(this));
								if (source) {
									var layerInfo = this.myMap.getLayerInfo(this.activeFeature.featureType);
									if (layerInfo) {
										var boundsTurfPolygon = turfPolygon(e.feature.getGeometry().getCoordinates());
										source.forEachFeatureInExtent(e.feature.getGeometry().getExtent(), function(feature){
											var canBeJoined = false;
											if (this.activeFeature.feature.get(layerInfo.globalIdField) != feature.get(layerInfo.globalIdField)) {
												if (layerInfo.typeIdField) {
													if (this.activeFeature.feature.get(layerInfo.typeIdField) == feature.get(layerInfo.typeIdField)) {
														canBeJoined = true;
													}
												} else {
													canBeJoined = true;
												}
											}
											if (canBeJoined) {
												if (turfLineIntersect(turfLineString(feature.getGeometry().getCoordinates()), boundsTurfPolygon)) {
													features.push(feature);
												}
											}
										}.bind(this));
									}
								}
							}
							if (features.length) {
								this.$vBus.$emit("confirm", {
									title: "Ar tvirtinti linijos sujungimą?",
									message: "Linijos sujungimo metu jungiamieji elementai (" + features.length + ") bus pašalinti!",
									positiveActionTitle: "Tvirtinti linijos sujungimą",
									negativeActionTitle: "Atšaukti",
									delayedPositive: true,
									positive: function(dialog){
										var positiveCallback = function(){
											if (this.myMap.drawHelper) {
												dialog.loading = true;
												this.myMap.drawHelper.saveJoinFeature(this.activeFeature, features, null, this.activeTask).then(function(){
													// FIXME... Nesuvaldytas atvejis, jei buvo apjungiami this.activeTask'o objektai...
													CommonHelper.routeTo({
														router: this.$router,
														vBus: this.$vBus,
														feature: this.activeFeature.feature
													});
													dialog.loading = false;
													dialog.dialog = false;
												}.bind(this), function(){
													dialog.loading = false;
													this.$vBus.$emit("show-message", {
														type: "warning",
														message: "Atsiprašome, įvyko nenumatyta klaida... Linijos nebuvo sujungtos"
													});
												}.bind(this));
											}
										}.bind(this);
										positiveCallback();
									}.bind(this),
									negative: function(){
										this.lineJoiningActive = false;
									}.bind(this)
								});
							} else {
								setTimeout(function(){
									if (this.lineJoiningDrawLayer) {
										this.lineJoiningDrawLayer.getSource().clear(true);
									}
								}.bind(this), 0);
							}
						}.bind(this));
						this.myMap.addInteraction(this.lineJoiningDrawInteraction);
					}
				} else {
					if (!this.lineJoiningDrawInteraction) {
						if (this.activeFeature && this.activeFeature.feature) {
							var layerOfActiveFeature = null,
								serviceId = this.activeFeature.type == "v" ? "street-signs-vertical" : "street-signs",
								layerId = parseInt(this.activeFeature.layerId);
							this.myMap.map.getLayers().forEach(function(layer){
								if (layer.service) {
									if (layer.service.id == serviceId && layer.getLayers) {
										layer.getLayers().forEach(function(l){
											if (layerId === l.layerId) {
												layerOfActiveFeature = l;
											}
										}.bind(this));
									}
								}
							}.bind(this));
							if (this.activeFeature.type == "task-related") {
								this.myMap.map.getLayers().forEach(function(layer){
									if (layer.isTasksRelatedLayer) {
										layer.getLayers().forEach(function(l){
											if (layerId === l.layerId) {
												layerOfActiveFeature = l;
											}
										}.bind(this));
									}
								}.bind(this));
							}
							if (layerOfActiveFeature) {
								this.layerOfActiveFeature = layerOfActiveFeature; // Prireiks this.lineJoinPointerMoveListenerHandler'iui..
								if (!this.highlightedLineJoinPossibleFeatureVectorLayer) {
									this.highlightedLineJoinPossibleFeatureVectorLayer = this.getHighlightedLineJoinPossibleFeatureVectorLayer();
									this.myMap.map.addLayer(this.highlightedLineJoinPossibleFeatureVectorLayer);
								}
								if (!this.lineJoinPointerMoveListenerHandler) {
									this.lineJoinPointerMoveListenerHandler = this.myMap.map.on("pointermove", this.lineJoinPointerMoveListener);
								}
								this.setLineJoinHelpMessage();
								this.myMap.addPointerMoveHandler();
								this.lineJoiningDrawInteraction = new Select({
									filter: function(feature, layer){
										feature = this.getLineFeatureToJoin(feature, layer, layerOfActiveFeature);
										if (feature) {
											// TODO! Tikrinti ar ne tas pats objektų GlobalId!! Nes dabar leidžia pažymėti tą patį objektą... O tai nėra gerai...
											console.log("ANALYZE", feature, this.activeFeature.feature); // TODO... Tikrinti, ar tas pats KET?..
											return true;
										}
										return false;
									}.bind(this),
									multi: false
								});
								this.lineJoiningDrawInteraction.on("select", function(e){
									if (e.selected && e.selected.length) {
										var activeFeatureCoordinates = this.activeFeature.feature.getGeometry().getCoordinates(),
											selectedFeatureCoordinates = e.selected[0].getGeometry().getCoordinates();
										var closestL = this.getClosestL(activeFeatureCoordinates, selectedFeatureCoordinates);
										if (closestL) {
											var joinedCoordinates;
											if (closestL == "l1") {
												// Aktyvios pradžia - pažymėtos pradžia
												// Į aktyvios geometrijos pradžią pridedame pažymėtą reverse'intą geometriją?..
												// console.log("L1", JSON.stringify(activeFeatureCoordinates), JSON.stringify(selectedFeatureCoordinates));
												joinedCoordinates = selectedFeatureCoordinates.slice().reverse();
												joinedCoordinates = joinedCoordinates.concat(activeFeatureCoordinates);
											} else if (closestL == "l2") {
												// Aktyvios pradžia - pažymėtos pabaiga
												// Į aktyvios geometrijos pradžią pridedame pažymėtą geometriją?..
												// console.log("L2", JSON.stringify(activeFeatureCoordinates), JSON.stringify(selectedFeatureCoordinates));
												joinedCoordinates = selectedFeatureCoordinates.slice();
												joinedCoordinates = joinedCoordinates.concat(activeFeatureCoordinates);
											} else if (closestL == "l3") {
												// Aktyvios pabaiga - pažymėtos pradžia
												// Į aktyvios geometrijos pabaigą pridedame pažymėtą geometriją?..
												// console.log("L3", JSON.stringify(activeFeatureCoordinates), JSON.stringify(selectedFeatureCoordinates));
												joinedCoordinates = activeFeatureCoordinates.slice();
												joinedCoordinates = joinedCoordinates.concat(selectedFeatureCoordinates);
											} else if (closestL == "l4") {
												// Aktyvios pabaiga - pažymėtos pabaiga
												// Į aktyvios geometrijos pabaigą pridedame pažymėtą reverse'intą geometriją?..
												// console.log("L4", JSON.stringify(activeFeatureCoordinates), JSON.stringify(selectedFeatureCoordinates));
												joinedCoordinates = activeFeatureCoordinates.slice();
												joinedCoordinates = joinedCoordinates.concat(selectedFeatureCoordinates.slice().reverse());
											}
											if (joinedCoordinates) {
												// console.log("JOINED GEOMETRY", JSON.stringify(joinedCoordinates));
												this.$vBus.$emit("confirm", {
													title: "Ar tvirtinti linijos sujungimą?",
													message: "Linijos sujungimo metu jungiamasis elementas bus pašalintas!",
													positiveActionTitle: "Tvirtinti linijos sujungimą",
													negativeActionTitle: "Atšaukti",
													delayedPositive: true,
													positive: function(dialog){
														var positiveCallback = function(){
															if (this.myMap.drawHelper) {
																dialog.loading = true;
																this.myMap.drawHelper.saveJoinFeature(this.activeFeature, [e.selected[0]], joinedCoordinates, this.activeTask).then(function(){
																	if (this.activeTask) {
																		this.activeFeature.feature.isTasksRelated = true;
																	}
																	this.myMap.setLayerForFeatureByFeatureType(this.activeFeature.feature, this.activeFeature.featureType);
																	this.$store.commit("setActiveFeaturePreview", this.activeFeature.feature);
																	CommonHelper.routeTo({
																		router: this.$router,
																		vBus: this.$vBus,
																		feature: this.activeFeature.feature
																	});
																	dialog.loading = false;
																	dialog.dialog = false;
																}.bind(this), function(){
																	dialog.loading = false;
																	this.$vBus.$emit("show-message", {
																		type: "warning",
																		message: "Atsiprašome, įvyko nenumatyta klaida... Linijos nebuvo sujungtos"
																	});
																}.bind(this));
															}
														}.bind(this);
														positiveCallback();
													}.bind(this),
													negative: function(){
														this.lineJoiningDrawInteraction.getFeatures().remove(e.selected[0]);
														this.lineJoinPossibleFeature = null;
														// this.lineJoiningActive = false;
													}.bind(this)
												});
											}
										} else {
											this.$vBus.$emit("show-message", {
												type: "warning",
												message: "Linijų sujungimas negalimas, nes linijos nesiliečia!"
											});
											this.lineJoiningDrawInteraction.getFeatures().remove(e.selected[0]);
										}
									}
								}.bind(this));
								this.myMap.addInteraction(this.lineJoiningDrawInteraction);
							}
						}
					}
				}
			},
			deactivateLineJoining: function(){
				if (this.lineJoiningDrawLayer) {
					this.myMap.map.removeLayer(this.lineJoiningDrawLayer);
					this.lineJoiningDrawLayer = null;
				}
				if (this.lineJoiningDrawInteraction) {
					this.myMap.removeInteraction(this.lineJoiningDrawInteraction);
					this.lineJoiningDrawInteraction = null;
				}
				if (this.lineJoinPointerMoveListenerHandler) {
					this.myMap.map.un("pointermove", this.lineJoinPointerMoveListener);
					this.lineJoinPointerMoveListenerHandler = null;
				}
				if (this.highlightedLineJoinPossibleFeatureVectorLayer) {
					this.myMap.map.removeLayer(this.highlightedLineJoinPossibleFeatureVectorLayer);
					this.highlightedLineJoinPossibleFeatureVectorLayer = null;
				}
				if (this.myMap) {
					this.myMap.removePointerMoveHandler();
				}
			},
			getLineFeatureToJoin: function(feature, layer, layerOfActiveFeature){
				if (this.activeFeature) {
					if ((layer == layerOfActiveFeature) && (feature != this.activeFeature.feature)) {
						return feature;
					}
				}
			},
			setLineJoinHelpMessage: function(feature){
				if (this.myMap) {
					var message = "Pažymėkite su objektu besiliečiančią liniją, kurią norite prijungti prie aktyvaus objekto";
					if (feature) {
						// message = "feature"; // TODO... Galima specifišką pranešimą pateikti...
					}
					this.myMap.helpMessage = message;
				}
			},
			lineJoinPointerMoveListener: function(e){
				if (e.originalEvent.target && e.originalEvent.target.tagName && (e.originalEvent.target.tagName.toUpperCase() == "CANVAS")) {
					var matchedFeature;
					this.myMap.map.forEachFeatureAtPixel(e.pixel, function(feature, layer){
						matchedFeature = this.getLineFeatureToJoin(feature, layer, this.layerOfActiveFeature);
					}.bind(this), {hitTolerance: 2});
					this.lineJoinPossibleFeature = matchedFeature;
				}
			},
			getHighlightedLineJoinPossibleFeatureVectorLayer: function(){
				var layer = new VectorLayer({
					source: new VectorSource(),
					style: function(feature){
						var activeFeatureCoordinates = this.activeFeature.feature.getGeometry().getCoordinates(),
							selectedFeatureCoordinates = feature.getGeometry().getCoordinates(),
							closestL = this.getClosestL(activeFeatureCoordinates, selectedFeatureCoordinates);
						var style = new Style({
							stroke: new Stroke({
								color: closestL ? "green" : "grey",
								width: closestL ? 3 : 1,
								lineCap: "butt"
							})
						});
						return style;
					}.bind(this),
					zIndex: 1001
				});
				return layer;
			},
			getClosestL: function(activeFeatureCoordinates, selectedFeatureCoordinates){
				var lengths = {},
					maxDistanceBetweenLines = 1,
					closestL;
				lengths.l1 = new LineString([activeFeatureCoordinates[0], selectedFeatureCoordinates[0]]).getLength(),
				lengths.l2 = new LineString([activeFeatureCoordinates[0], selectedFeatureCoordinates[selectedFeatureCoordinates.length - 1]]).getLength(),
				lengths.l3 = new LineString([activeFeatureCoordinates[activeFeatureCoordinates.length - 1], selectedFeatureCoordinates[0]]).getLength(),
				lengths.l4 = new LineString([activeFeatureCoordinates[activeFeatureCoordinates.length - 1], selectedFeatureCoordinates[selectedFeatureCoordinates.length - 1]]).getLength();
				for (var key in lengths) {
					if (closestL) {
						if (lengths[key] < lengths[closestL]) {
							closestL = key;
						}
					} else {
						closestL = key;
					}
				}
				if (closestL && (lengths[closestL] < maxDistanceBetweenLines)) {
					// console.log("LL", this.activeFeature.feature.getGeometry().getLength()); // Įgyvendinti Justino atvejį dėl labai trumpų linijų...
				} else {
					closestL = null;
				}
				return closestL;
			}
		},

		watch: {
			activeFeature: {
				immediate: true,
				handler: function(activeFeature){
					this.setEditingActive(false);
					this.setToggleLineCuttingActive(false);
					this.setToggleLineJoiningActive(false);
					// Esmė tokia: jei uždaromas popup'as, tai ir drawLayer'is turi išsivalyti... Gal kartais ten buvo kas nupiešta? Triname lauk!
					// Taip pat jei ir žemėlapyje paspaudžiame ant kito objekto...
					var clearDrawLayer = true;
					if (activeFeature) {
						if (activeFeature.isNew) {
							this.setMode("new-feature");
							clearDrawLayer = false;
						} else {
							this.setMode(null);
						}
					}
					if (clearDrawLayer) {
						if (this.myMap) {
							if (this.myMap.drawLayer) {
								this.myMap.drawLayer.getSource().clear(true);
							}
							this.myMap.drawHelper.deactivateGeometryEditing();
						}
					}
				}
			},
			mode: {
				immediate: true,
				handler: function(mode, prevMode){
					if (this.$refs.content) {
						this.$refs.content.scrollTop = 0;
					}
					if (mode == "vertical-street-sign-addition" && prevMode == "new-feature-vertical-sign") {
						this.myMap.drawHelper.deactivateGeometryEditing();
						this.activeFeature.feature.refreshAdditionalFeatures = true; // Reikia tik showActiveFeaturePreview() sąlygai, kad "permetus" žvilgsnį atgal į atramos objektą nepradingtų tarpinės linijos...
						this.$store.commit("setActiveFeaturePreview", this.activeFeature.feature); // "Permetame" žvilgsnį atgal į atramos objektą!
					}
				}
			},
			editingActive: {
				immediate: true,
				handler: function(editingActive){
					if (editingActive) {
						this.activateGeometryEditing();
					} else {
						if (this.myMap) {
							this.myMap.drawHelper.deactivateGeometryEditing();
						}
					}
				}
			},
			lineCuttingActive: {
				immediate: true,
				handler: function(lineCuttingActive){
					if (lineCuttingActive) {
						this.activateLineCutting();
					} else {
						this.deactivateLineCutting();
					}
				}
			},
			lineJoiningActive: {
				immediate: true,
				handler: function(lineJoiningActive){
					if (lineJoiningActive) {
						this.activateLineJoining();
					} else {
						this.deactivateLineJoining();
					}
				}
			},
			activeNewFeatureData: {
				immediate: true,
				handler: function(activeNewFeatureData){
					if (activeNewFeatureData) {
						this.activateGeometryEditing();
					}
				}
			},
			lineJoinPossibleFeature: {
				immediate: true,
				handler: function(feature){
					if (this.highlightedLineJoinPossibleFeatureVectorLayer) {
						this.highlightedLineJoinPossibleFeatureVectorLayer.getSource().clear(true);
						if (feature) {
							this.highlightedLineJoinPossibleFeatureVectorLayer.getSource().addFeature(feature);
						}
					}
					this.setLineJoinHelpMessage(feature);
				}
			}
		}
	}
</script>

<style scoped>
	.popup-wrapper {
		max-height: 100%;
	}
	.popup {
		width: 500px;
		background-color: white;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3) !important;
		overflow: hidden;
		overflow-y: auto;
	}
	.header {
		border-bottom: 1px solid #cccccc;
	}
	.content {
		overflow: hidden;
		overflow-y: auto;
		width: 100%;
	}
</style>