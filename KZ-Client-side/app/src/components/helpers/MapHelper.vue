<script>
	import Collection from "ol/Collection";
	import CommonHelper from "./CommonHelper";
	import EsriJSON from "ol/format/EsriJSON";
	import Feature from "ol/Feature";
	import ImageArcGISRestSource from "ol/source/ImageArcGISRest";
	import ImageLayer from "ol/layer/Image";
	import LayerGroup from "ol/layer/Group";
	import store from "../../admin-app/store.js";
	import TileGrid from "ol/tilegrid/TileGrid";
	import TileLayer from "ol/layer/Tile";
	import VectorImageLayer from "ol/layer/VectorImage";
	import VectorLayerStyleHelper from "./VectorLayerStyleHelper";
	import VectorSource from "ol/source/Vector";
	import XYZSource from "ol/source/XYZ";
	import {createXYZ} from "ol/tilegrid";
	import {tile as tileStrategy} from "ol/loadingstrategy";
	import {transformExtent} from "ol/proj";

	export default {
		optionalServicesOffset: 2,
		userDataService: {
			url: store.getters.getServiceUrl("street-signs"),
			title: "Naudotojo taškai",
			id: "user-points",
			showLayers: [16],
			showOnlyUserFeatures: true
		},
		getLayer: function(service, serviceCapabilitiesManager, myMap){
			if (!serviceCapabilitiesManager) {
				serviceCapabilitiesManager = "esri";
			}
			var promise = new Promise(function(resolve, reject){
				setTimeout(function(){
					if (service.layer) {
						// TODO!!! Gali tekti revoke'inti token'ą???
						resolve(service.layer);
					} else {
						service.timestamp = Date.now(); // Prireiks unikaliems simboliams...
						var layer,
							zIndex;
						var serviceCopy = Object.assign({}, service);
						if (service.base) {
							zIndex = 0;
						} else if (service.zIndex) {
							zIndex = service.zIndex;
						} else {
							zIndex = 1;
						}
						if (service.url && !(service.securedWithToken && !service.token)) {
							service.url = CommonHelper.prependProxyIfNeeded(service.url);
							this.getServiceCapabilities(service.url, serviceCapabilitiesManager, service.token).then(function(capabilities){
								if (!service.capabilities) {
									service.capabilities = {};
								}
								service.capabilities[capabilities.type] = capabilities.content;
								var json,
									extent = this.getServiceExtent(service);
								if (service.capabilities["esri"]) {
									json = service.capabilities["esri"];
									if (json.singleFusedMapCache) {
										var resolutions = [];
										json.tileInfo.lods.forEach(function(lod){
											if (!service.maxScale || (lod.scale >= service.maxScale)) {
												resolutions.push(lod.resolution);
											}
										});
										var wkid = this.normalizeWkid(json.spatialReference.wkid);
										layer = new TileLayer({
											source: new XYZSource({
												projection: "EPSG:" + wkid,
												tileUrlFunction: function(tileCoord){
													var u = service.url + "/tile/" + tileCoord[0] + "/" + tileCoord[2] + "/" + tileCoord[1];
													return u;
												},
												tileGrid: new TileGrid({
													tileSize: [json.tileInfo.cols, json.tileInfo.rows],
													origin: [json.tileInfo.origin.x, json.tileInfo.origin.y],
													resolutions: resolutions
												}),
												crossOrigin: "Anonymous"
											}),
											extent: extent
										});
									} else {
										if (json.capabilities) {
											var jsonCapabilities = json.capabilities.split(",");
											if (!service.forceImage && jsonCapabilities.indexOf("Editing") != -1 || service.asVectorLayer) {
												if (json.layers && service.showLayers) {
													this.getServiceCapabilities(service.url + "/layers", "esri", service.token).then(function(layersInfo){
														var vectorLayers = [],
															vectorLayer,
															layersInfoDict = {};
														if (layersInfo && layersInfo.content && layersInfo.content.layers) {
															layersInfo.content.layers.forEach(function(layerInfo){
																layersInfoDict[layerInfo.id] = layerInfo;
															});
														}
														myMap.populateLayersInfoDict(service.id, layersInfoDict);
														service.showLayers.forEach(function(showLayerId){
															var layerInfo = layersInfoDict[showLayerId];
															if (layerInfo) {
																var globalIdField = layerInfo.globalIdField,
																	rotationField,
																	outFields = [];
																if (globalIdField) {
																	outFields.push(globalIdField);
																}
																if (layerInfo.objectIdField) {
																	outFields.push(layerInfo.objectIdField);
																}
																if (layerInfo.typeIdField) {
																	outFields.push(layerInfo.typeIdField);
																}
																rotationField = this.getRotationField(layerInfo);
																if (rotationField) {
																	outFields.push(rotationField);
																}
																if (layerInfo.drawingInfo) {
																	var rendererFields = [];
																	if (layerInfo.drawingInfo.renderer.field1) {
																		rendererFields.push(layerInfo.drawingInfo.renderer.field1);
																	}
																	if (layerInfo.drawingInfo.renderer.field2) {
																		rendererFields.push(layerInfo.drawingInfo.renderer.field2);
																	}
																	if (layerInfo.drawingInfo.renderer.field3) {
																		rendererFields.push(layerInfo.drawingInfo.renderer.field3);
																	}
																	if (layerInfo.drawingInfo.labelingInfo) {
																		if (Array.isArray(layerInfo.drawingInfo.labelingInfo)) {
																			layerInfo.drawingInfo.labelingInfo.forEach(function(labelInfo){
																				if (labelInfo.labelExpressionInfo && labelInfo.labelExpressionInfo.expression) {
																					var fieldName = VectorLayerStyleHelper.getLabelingInfoFieldFromExpression(labelInfo.labelExpressionInfo);
																					if (outFields.indexOf(fieldName) == -1) {
																						// Nesupratau... Pvz. 5-as VVT sluoksnis turėjo "$feature.fiderioNr", nors jis net neturi tokio field'o...
																						// Kad pasikeitė serviso struktūra, o label'is paliko "iš seniau"? Tad reikia būti atsargesniems ir tikrinti, ar toks laukas yra tarp "fields" laukų...
																						if (layerInfo.fields) {
																							layerInfo.fields.some(function(field){
																								if (field.name == field) {
																									outFields.push(fieldName);
																									return true;
																								}
																							});
																						}
																					}
																				}
																			});
																		}
																	}
																	if (rendererFields) {
																		rendererFields.forEach(function(rendererField){
																			if (outFields.indexOf(rendererField) == -1) {
																				outFields.push(rendererField);
																			}
																		});
																	}
																}
																if (service.id == "street-signs-vertical") {
																	if (layerInfo.fields) {
																		layerInfo.fields.forEach(function(field){
																			// To reikia, kad galėtume pavaizduoti klaustuką žemėlapyje...
																			if (["PATVIRTINTAS", "PATVIRTINIMAS", "STATUSAS", "PASAL_DATA", "last_edited_user", CommonHelper.customSymbolIdFieldName, "ATMESTA"].indexOf(field.name) != -1) {
																				outFields.push(field.name);
																			}
																		});
																	}
																	if (layerInfo.relationships && layerInfo.relationships.length == 1) {
																		outFields.push(layerInfo.relationships[0].keyField);
																	}
																} else if ((service.id == "vms-inventorization-l") || (service.id == "vms-inventorization-p")) {
																	outFields = ["*"];
																} else {
																	if (layerInfo.fields) {
																		layerInfo.fields.some(function(field){
																			if ([CommonHelper.widthFieldName].indexOf(field.name) != -1) {
																				outFields.push(field.name);
																			}
																		});
																	}
																}
																vectorLayer = this.getVectorLayer(service, layerInfo, myMap, outFields);
																if (vectorLayer) {
																	vectorLayer.parentServiceId = service.id;
																	vectorLayer.layerId = showLayerId;
																	vectorLayer.typeIdField = layerInfo.typeIdField;
																	vectorLayer.typesCodedValues = {};
																	vectorLayer.globalIdField = globalIdField;
																	vectorLayer.objectIdField = layerInfo.objectIdField;
																	vectorLayer.rotationField = rotationField;
																	vectorLayer.timestamp = Date.now(); // Prireiks unikaliems simboliams...
																	vectorLayer.clickable = service.clickable;
																	if (layerInfo.types) {
																		layerInfo.types.forEach(function(type){
																			vectorLayer.typesCodedValues[type.id] = type.name;
																		});
																	}
																	if (layerInfo.fields) {
																		layerInfo.fields.some(function(field){
																			if (field.name == layerInfo.typeIdField) {
																				vectorLayer.typeField = field;
																				return true;
																			}
																		});
																	}
																	if (service.minZoom4InnerLayers) {
																		var minZoom = service.minZoom4InnerLayers[showLayerId] || service.minZoom4InnerLayers.default;
																		if (minZoom) {
																			vectorLayer.setMinZoom(minZoom);
																		}
																	}
																	vectorLayer.setZIndex(zIndex);
																	vectorLayers.push(vectorLayer);
																}
															}
														}.bind(this));
														if ((service.id == "street-signs" || service.id == "vms-inventorization-l" || service.id == "vms-inventorization-p") && !service.maxAllowableOffset) {
															var maxZoomThreshold = 5,
																forceImage = false;
															if (service.id == "vms-inventorization-l" || service.id == "vms-inventorization-p") {
																maxZoomThreshold = 3;
															}
															serviceCopy.maxZoom = maxZoomThreshold;
															serviceCopy.maxAllowableOffset = 4;
															if (service.id == "vms-inventorization-l" || service.id == "vms-inventorization-p") {
																serviceCopy.maxAllowableOffset = 10;
															}
															if (forceImage) { // Jei norime, kad smulkiame mastelyje rodytų "/MapServer" sluoksnį...
																serviceCopy.forceImage = true;
																serviceCopy.url = serviceCopy.url.replace("/FeatureServer", "/MapServer");
															}
															myMap.addLayer(serviceCopy, layer);
															vectorLayers.forEach(function(vectorLayer){
																if (vectorLayer.getMinZoom() < maxZoomThreshold) {
																	vectorLayer.setMinZoom(maxZoomThreshold);
																}
															});
														}
														layer.setLayers(new Collection(vectorLayers));
													}.bind(this));
													layer = new LayerGroup();
												}
											} else if (jsonCapabilities.indexOf("Map") != -1) {
												var dummyUrlStart = "DUMMY/MapServer";
												var params = {};
												if (service.showLayers) {
													params.layers = "show:" + service.showLayers.join(",")
												}
												if (service.layerDefs) {
													params.layerDefs = JSON.stringify(service.layerDefs);
												}
												if (service.token) {
													params.token = service.token;
												}
												if (service.id == "social-mobility") {
													extent = undefined; // Nes jis kažkoks blogas...
												}
												layer = new ImageLayer({
													source: new ImageArcGISRestSource({
														ratio: 1,
														url: dummyUrlStart, // Toks "dummy" paduodamas, nes "The url should include /MapServer or /ImageServer", o tokį ne visada galime turėti...
														imageLoadFunction: function(image, src){
															src = src.replace(dummyUrlStart, service.url);
															// TODO... Čia nustatyti dinamiškai pažymėtus/atžymėtus sublayer'ius...
															image.getImage().src = src;
														},
														params: params,
														crossOrigin: "Anonymous"
													}),
													extent: extent
												});
											}
										} else {
											// Yra ir taip buvę... Pvz. grąžino -> {"error":{"code":499,"message":"Token Required","details":[]}}
										}
									}
								}
								if (layer) {
									this.modLayer(layer, service, zIndex);
									resolve(layer);
								} else {
									reject();
								}
							}.bind(this), function(){
								reject();
							});
						} else if (service.urls) {
							layer = new LayerGroup();
							layer.service = service; // Šito reikia `advancedDetails` funkcionalumui...
							service.layer = layer; // Šito reikia `advancedDetails` funkcionalumui...
							service.urls.forEach(function(item){
								this.getLayer(item, serviceCapabilitiesManager, myMap).then(function(l){
									layer.getLayers().push(l);
								}.bind(this), function(){
									// ...
								});
							}.bind(this));
							this.modLayer(layer, service, zIndex);
							resolve(layer);
						} else if (service.securedWithToken) {
							CommonHelper.getToken(service).then(function(token){
								service.token = token;
								this.getLayer(service, serviceCapabilitiesManager, myMap).then(function(l){
									resolve(l);
								}.bind(this), function(){
									reject();
								});
							}.bind(this), function(){
								reject();
							});
						} else {
							reject();
						}
					}
				}.bind(this), store.state.testDelay);
			}.bind(this));
			return promise;
		},

		getFullExtent: function(){
			var extent = {
				xmin: 485765,
				ymin: 6072642,
				xmax: 505741,
				ymax: 6093068,
				spatialReference: {
					wkid: 3346
				}
			};
			return extent;
		},

		getLods: function(){
			var lods = [{
				resolution: 66.1459656252646,
				scale: 250000
			},{
				resolution: 26.458386250105836,
				scale: 100000
			},{
				resolution: 13.229193125052918,
				scale: 50000
			},{
				resolution: 6.614596562526459,
				scale: 25000
			},{
				resolution: 2.6458386250105836,
				scale: 10000
			},{
				resolution: 1.3229193125052918,
				scale: 5000
			},{
				resolution: 0.5291677250021167,
				scale: 2000
			},{
				resolution: 0.26458386250105836,
				scale: 1000
			},{
				resolution: 0.13229193125052918,
				scale: 500
			},{
				resolution: 0.06614596562526459,
				scale: 250
			},{
				resolution: 0.026458386250105836,
				scale: 100
			},{
				resolution: 0.013229193125052918,
				scale: 50
			},{
				resolution: 0.006614596562526459,
				scale: 25
			}];
			return lods;
		},

		getMainServices: function(){
			var mainServices = {
				base: [{
					title: "Ortofoto",
					url: "https://www.geoportal.lt/mapproxy/nzt_ort10lt_recent/MapServer",
					maxScale: 500
				}],
				optional: [{
					title: "Vertikalieji kelio ženklai",
					id: "street-signs-vertical", // URL'ą grąžina serveris kartu su vartotojo info...
					showLayers: [1, 0],
					minZoom4InnerLayers: {
						default: CommonHelper.pointsZoomThreshold
					},
					active: true,
					zIndex: 9, // 7 ir 8 rezervuota tarpinėms linijoms
					notInBasicViewer: true,
				},{
					title: "Horizontalusis ženklinimas ir kelio infrastruktūra",
					id: "street-signs", // URL'ą grąžina serveris kartu su vartotojo info...
					showLayers: [4, 7, 3, 6, 2, 5],
					minZoom4InnerLayers: {
						2: CommonHelper.pointsZoomThreshold, // Horizontalių kelio ženklų taškiniai objektai
						4: 4, // Horizontalių kelio ženklų plotiniai objektai
						5: CommonHelper.pointsZoomThreshold, // Infrastruktūros taškiniai objektai
						6: 4, // Infrastruktūros linijiniai objektai
						7: 4, // Infrastruktūros plotiniai objektai
						default: CommonHelper.streetSignsZoomThreshold
					},
					active: true,
					zIndex: 6,
					notInBasicViewer: true
				},{
					title: "Juodosios dėmės",
					id: "black-spots",
					url: "https://kp.kaunas.lt/image/rest/services/Kelio_zenklai/Papildomi_sluoksniai/MapServer",
					showLayers: [0],
					zIndex: 5,
					opacity: 1,
					notInBasicViewer: true,
					showLegend: true,
					clickable: true
				},{
					title: "Eismo įvykiai",
					id: "accidents",
					url: "https://kp.kaunas.lt/image/rest/services/Kelio_zenklai/Papildomi_sluoksniai/MapServer",
					showLayers: [1],
					zIndex: 4,
					opacity: 1,
					notInBasicViewer: true,
					showLegend: true,
					clickable: true
				},{
					title: "Seniūnijos",
					id: "zones",
					url: "https://kp.kaunas.lt/image/rest/services/Kelio_zenklai/Papildomi_sluoksniai/MapServer",
					showLayers: [2],
					zIndex: 3,
					opacity: 1,
					asVectorLayer: true,
					notInBasicViewer: true
				},{
					title: "Ortofoto (dronų)",
					url: "https://kp.kaunas.lt/image/rest/services/KaunasOrtho/KaunasCache/MapServer",
					maxScale: 100,
					zIndex: 2
				}]
			};
			mainServices.optional.forEach(function(optionalService, i){
				optionalService.zIndex = mainServices.optional.length + this.optionalServicesOffset - i;
				if (!optionalService.id) {
					optionalService.id = "service-id-" + i;
				}
			}.bind(this));
			return mainServices;
		},

		getProjectionCode: function(){
			var projectionCode = 3346;
			return projectionCode;
		},

		getResolutions: function(maxScale){
			if (!maxScale) {
				maxScale = 0;
			}
			var resolutions = [];
			this.getLods().forEach(function(lod){
				if (lod.scale >= maxScale) {
					resolutions.push(lod.resolution);
				}
			});
			return resolutions;
		},

		getServiceCapabilities: function(url, type, token){
			if (!this.serviceCapabilities) {
				this.serviceCapabilities = {};
			}
			if (!this.serviceCapabilities[url]) {
				this.serviceCapabilities[url] = new Promise(function(resolve, reject){
					if (type == "esri") {
						var capabUrl = url + "?f=json";
						if (token) {
							capabUrl += "&token=" + token;
						}
						CommonHelper.getFetchPromise(capabUrl, function(json){
							var capabilities = {
								type: "esri",
								content: json
							};
							return capabilities;
						}, "GET").then(function(result){
							resolve(result);
						}, function(){
							reject();
						});
					} else {
						reject();
					}
				});
			}
			this.serviceCapabilities[url].then(function(){
				// ...
			}, function(){
				this.serviceCapabilities[url] = null; // Jei užklausa nepavyko, nėra reikalo cache'uoti promise'o!!!
			}.bind(this));
			return this.serviceCapabilities[url];
		},

		getTasksServiceCapabilities: function(myMap){
			var promise = new Promise(function(resolve, reject){
				var tasksServiceUrl = store.getters.getServiceUrl("tasks");
				if (tasksServiceUrl) {
					this.getServiceCapabilities(CommonHelper.prependProxyIfNeeded(tasksServiceUrl + "/layers"), "esri").then(function(layersInfo){
						if (layersInfo && layersInfo.content && layersInfo.content.layers) {
							var layersInfoDict = {};
							layersInfo.content.layers.forEach(function(layerInfo){
								layersInfoDict[layerInfo.id] = layerInfo;
							});
							if (myMap) {
								myMap.populateLayersInfoDict("tasks", layersInfoDict);
							}
							resolve(layersInfoDict);
						} else {
							reject();
						}
					}.bind(this), function(){
						reject();
					}.bind(this));
				} else {
					reject();
				}
			}.bind(this));
			return promise;
		},

		getServiceExtent: function(service){
			var capabilities = service.capabilities,
				extent;
			if (capabilities) {
				capabilities = capabilities["esri"];
				if (capabilities) {
					var extentConfig = capabilities.fullExtent || capabilities.initialExtent;
					if (extentConfig) {
						try {
							var wkid = this.normalizeWkid(extentConfig.spatialReference.latestWkid || extentConfig.spatialReference.wkid);
							extent = [extentConfig.xmin, extentConfig.ymin, extentConfig.xmax, extentConfig.ymax];
							extent = transformExtent(extent, "EPSG:" + wkid, "EPSG:" + this.getProjectionCode());
						} catch(err) {
							console.log("ERR", err, extent, wkid);
							extent = null;
						}
					}
				}
			}
			return extent;
		},

		getVectorLayer: function(service, layerInfo, myMap, outFields){
			var vectorLayer;
			if (service) {
				var esriJsonFormat = new EsriJSON(),
					projectionCode = this.getProjectionCode();
				var tileSize = 256;
				if (service.id == "street-signs" || service.id == "street-signs-vertical") {
					if (service.maxAllowableOffset) {
						tileSize = 768;
					} else {
						if (service.id == "street-signs-vertical") {
							tileSize = 2048;
						} else {
							tileSize = 1536;
						}
					}
				} else if (service.id == "vvt") {
					tileSize = 512;
				}
				var vectorSource = new VectorSource({
					loader: function(extent, resolution, projection) {
						if (!outFields) {
							outFields = ["*"];
						}
						if (myMap) {
							myMap.layersLoadingCounter += 1;
						}
						var url = service.url + "/" + layerInfo.id + '/query/?f=json&' +
						'returnGeometry=true&spatialRel=esriSpatialRelIntersects&geometry=' +
						encodeURIComponent('{"xmin":' + extent[0] + ',"ymin":' +
						extent[1] + ',"xmax":' + extent[2] + ',"ymax":' + extent[3] +
						',"spatialReference":{"wkid":' + projectionCode + '}}') +
						'&geometryType=esriGeometryEnvelope&inSR=' + projectionCode + '&outFields=' + outFields.join(",") +
						'&outSR=' + projectionCode;
						if (service.maxAllowableOffset) {
							url += "&maxAllowableOffset=" + service.maxAllowableOffset;
						}
						if (vectorLayer.where) {
							url += "&where=" + vectorLayer.where;
						}
						if (service.showOnlyUserFeatures) {
							if (myMap && myMap.userData) {
								url += "&where=" + "editor_app='" + myMap.userData.username + "'"; // FIXME... Jei jau query'je egiztuoja `where`, tai reikia modifikuoti `where`, o ne pridėti dar vieną parametrą..
							} else {
								// TODO... Pagal idėją turi nieko nerodyti?..
							}
						}
						if (vectorLayer.historicMoment) {
							url += "&historicMoment=" + vectorLayer.historicMoment;
						}
						if (service.token) {
							url += "&token=" + service.token;
						}
						window.fetch(url, {
							method: "GET"
						}).then(function(response){
							if (myMap) {
								myMap.layersLoadingCounter -= 1;
							}
							if (response.status == 200) {
								return response.text();
							} else {
								// ...
							}
						}).then(function(text){
							var json;
							if (text) {
								try {
									json = JSON.parse(text);
								} catch(err) {
									// ...
								}
							}
							if (json && !json.error) { // Nes gali būti pvz.: {"error":{"code":500,"message":"Error performing query operation","details":[]}}
								var features = esriJsonFormat.readFeatures(json, {
									featureProjection: projection
								});
								if (features.length > 0) {
									if (json.exceededTransferLimit) {
										console.warn("Some objects didn't render?", features.length, layerInfo.id);
									}
									if (service.id == "street-signs-vertical") {
										// Čia tas didelis proceso hack'as, kad tuos objektus, kuriuos paskutinis redagavo vartotojas iš mobilios app, priimtų kaip nepatvirtintus...
										var featuresMod = [];
										features.forEach(function(feature){
											if ((feature.get("STATUSAS") == CommonHelper.verticalStreetSignDestroyedStatusValue) && ((feature.get("PATVIRTINTAS") == 1) || (feature.get("PATVIRTINIMAS") == 1))) {
												if (feature.get("ATMESTA")) {
													// Jei turime atmestų PAŠALINTŲ objektų, tai juos rodome žemėlapyje ir t. t.
													CommonHelper.setFeatureAsUnapprovedIfNeeded(feature);
													featuresMod.push(feature);
												}
												// Ženklas yra panaikintas ir patvirtintas ir `atmesta` nelygu [1]... Jį ignoruojame...
											} else {
												CommonHelper.setFeatureAsUnapprovedIfNeeded(feature);
												featuresMod.push(feature);
											}
										}.bind(this));
										features = featuresMod;
									} else if (service.id == "street-signs") {
										// TODO... Kaip padaryti vertikaliesiems, taip reikia padaryti ir horizontaliesimes... T. y. slėpti demontuotus KŽ?..
									}
									vectorSource.addFeatures(features);
								}
							} else {
								// ...
							}
						}.bind(this), function(){
							// ...
						});
					}.bind(this),
					strategy: tileStrategy(createXYZ({
						tileSize: tileSize
					}))
				});
				var VectorLayerClass = VectorImageLayer;
				vectorLayer = new VectorLayerClass({
					source: vectorSource,
					style: VectorLayerStyleHelper.getVectorLayerStyle(service, layerInfo, myMap),
					declutter: false,
					minZoom: service.minZoom || undefined
				});
				vectorLayer.set("layerId", layerInfo.id);
			}
			return vectorLayer;
		},

		modLayer: function(layer, service, zIndex){
			layer.service = service;
			service.layer = layer;
			if (service.minZoom) {
				layer.setMinZoom(service.minZoom);
			}
			if (service.maxZoom) {
				layer.setMaxZoom(service.maxZoom);
			}
			if (service.hidden) {
				layer.set("visible", false);
			}
			if (service.opacity) {
				layer.setOpacity(service.opacity);
			}
			layer.setZIndex(zIndex);
			if (layer.getLayers) { // Darome prielaidą, kad tai yra LayerGroup'as
				layer.getLayers().forEach(function(l){
					l.setZIndex(zIndex); // To reikia, nes vaikiniai sluoksniai kaip ir nepaveldi LayerGroup'o zIndex'o?
				}.bind(this));
			}
		},

		normalizeWkid: function(wkid){
			if (wkid == 2600) {
				wkid = 3346;
			}
			return wkid;
		},

		createFeature: function(descr, setFeatureAsUnapprovedIfNeeded){
			var feature;
			if (descr instanceof Feature) {
				feature = descr;
			} else {
				var esriJsonFormat = new EsriJSON();
				feature = esriJsonFormat.readFeature(descr);
			}
			if (setFeatureAsUnapprovedIfNeeded) {
				CommonHelper.setFeatureAsUnapprovedIfNeeded(feature);
			}
			return feature;
		},

		getRotationField: function(layerInfo){
			var rotationField;
			if (layerInfo.drawingInfo.renderer) {
				var renderer = layerInfo.drawingInfo.renderer;
				if (renderer.visualVariables) {
					renderer.visualVariables.forEach(function(visualVariable){
						if (visualVariable.type == "rotationInfo") {
							rotationField = visualVariable.valueExpression.replace("$feature.", "");
						}
					});
				}
			}
			return rotationField;
		},

		modDateFields: function(layerInfo){
			if (layerInfo && layerInfo.dateFieldsTimeReference) {
				if (layerInfo.fields) {
					layerInfo.fields.forEach(function(field){
						if (field.type == "esriFieldTypeDate") {
							field.timeZoneData = layerInfo.dateFieldsTimeReference;
						}
					});
				}
			}
		},

		getLegend: function(layer){
			var promise = new Promise(function(resolve, reject){
				if (layer && layer.service && layer.service.url) {
					this.getServiceCapabilities(layer.service.url + "/legend", "esri").then(function(result){
						var legends = [];
						if (result && result.content) {
							if (layer.service.showLayers) {
								if (result.content.layers) {
									var layerLegendsDict = {};
									result.content.layers.forEach(function(layer){
										layerLegendsDict[layer.layerId] = layer;
									});
									var layersInfo = {};
									if (layer.service.capabilities && layer.service.capabilities.esri && layer.service.capabilities.esri.layers) {
										layer.service.capabilities.esri.layers.forEach(function(layerInfo){
											layersInfo[layerInfo.id] = layerInfo;
										});
									}
									this.setLegends(legends, layer.service.showLayers, layersInfo, layerLegendsDict);
								}
							}
						}
						resolve(legends);
					}.bind(this), function(){
						reject();
					});
				} else {
					reject();
				}
			}.bind(this));
			return promise;
		},

		setLegends: function(legends, layerIds, layersInfo, layerLegendsDict){
			if (layerIds) {
				layerIds.forEach(function(layerId){
					if (layersInfo[layerId]) {
						var layerInfo = JSON.parse(JSON.stringify(layersInfo[layerId]));
						layerInfo.childrenLegends = [];
						if (layerLegendsDict[layerId]) {
							layerInfo.legend = layerLegendsDict[layerId].legend;
						}
						this.setLegends(layerInfo.childrenLegends, layerInfo.subLayerIds, layersInfo, layerLegendsDict);
						legends.push(layerInfo);
					}
				}.bind(this));
			}
		}
	}
</script>