import Collection from "ol/Collection";
import CommonHelper from "./CommonHelper";
import EsriJSON from "ol/format/EsriJSON";
import Modify from "ol/interaction/Modify";
import RotateInteraction from "./RotateInteraction.js";
import Translate from "ol/interaction/Translate";

var DrawHelper = (function(){

    function DrawHelper(opt_options){
        var options = opt_options || {};
        this.myMap = options.myMap;
		this.$vBus = options.$vBus;
		this.$store = options.$store;
    }

    DrawHelper.prototype.getSaveFeatureParams = function(dataToSave, activeTask, deletes){
		// Pagal šią info: https://developers.arcgis.com/rest/services-reference/enterprise/apply-edits-feature-service-.htm
		var esriJsonFormat = new EsriJSON(),
			setUnapproved = true,
			params = {},
			featuresInParams = {},
			layerIdMeta,
			layerInfo,
			editType,
			f;
		if (this.$store.state.userData && this.$store.state.userData.permissions) {
			if (this.$store.state.userData.permissions.indexOf("approve") != -1) {
				setUnapproved = false;
			}
		} else if (activeTask) {
			// Gi nenorime koreguoti originalaus objekto atributų...
			setUnapproved = false;
		}
		dataToSave.forEach(function(d){
			if (setUnapproved) {
				// Atseit vos tik išsaugojome objektą, jis pažymimas kaip nepatvirtintas... Approver'is po to turi kiekvieną objektą approve'inti...
				if (d.featureType == "verticalStreetSigns") {
					d.feature.set("PATVIRTINTAS", 0);
				} else if (d.featureType == "verticalStreetSignsSupports") {
					d.feature.set("PATVIRTINIMAS", 0);
				}
			}
			layerIdMeta = CommonHelper.layerIds[d.featureType];
			if (layerIdMeta && layerIdMeta[2]) {
				layerIdMeta = [layerIdMeta[2], layerIdMeta[1]]; // Naudotojų taškams to prireikė...
			}
			if (!layerIdMeta) {
				if (d.featureType == "vvt") {
					layerIdMeta = ["vvt", d.layerId];
				}
			}
			if (activeTask) {
				layerIdMeta = this.getLayerIdMeta4ActiveTask(d.featureType);
			}
			if (layerIdMeta) {
				if (!params[layerIdMeta[0]]) {
					params[layerIdMeta[0]] = {};
				}
				if (!params[layerIdMeta[0]][layerIdMeta[1]]) {
					params[layerIdMeta[0]][layerIdMeta[1]] = {
						adds: [],
						updates: []
					};
				}
				if (d.featureType == "vvt") {
					layerInfo = this.myMap.getLayerInfo(d.featureType, d.layerId);
				} else {
					layerInfo = this.myMap.getLayerInfo(d.featureType);
				}
				if (layerInfo && (layerInfo.globalIdField || layerInfo.objectIdField)) {
					if (d.feature.get(layerInfo.globalIdField) || d.feature.get(layerInfo.objectIdField)) {
						editType = "updates";
					} else {
						editType = "adds";
					}
					if (activeTask) {
						// Jei yra aktyvi užduotis:
						// Kad ir koks veiksmas buvo atliekamas žemėlapyje, jis fiksuojamas kaip naujo įrašo sukūrimas (tik jei dabar turime reikalą su originalaus sluoksnio Feature, žinoma)
						editType = "adds";
						if (d.feature.get(CommonHelper.taskFeatureTaskGUIDFieldName)) {
							// Objektas jau turi task'o GUID... Vadinasi, tai tiesiog redaguojame projektuojamos užduoties objektą?..
							editType = "updates";
						} else {
							d.feature.set(CommonHelper.taskFeatureOriginalGlobalIdFieldName, d.feature.get(layerInfo.globalIdField));
							if (!d.feature.get(CommonHelper.taskFeatureActionFieldName)) { // Nes gi pvz. `remove` jau seniau nustatytas...
								if (d.feature.get(layerInfo.globalIdField)) {
									d.feature.set(CommonHelper.taskFeatureActionFieldName, CommonHelper.taskFeatureActionValues["update"]);
								} else {
									d.feature.set(CommonHelper.taskFeatureActionFieldName, CommonHelper.taskFeatureActionValues["add"]);
								}
							}
							d.feature.set(layerInfo.globalIdField, null); // FIXME: gal naudoti d.feature.unset()?..
						}
						d.feature.set(CommonHelper.taskFeatureTaskGUIDFieldName, activeTask.feature.get("GlobalID")); // FIXME! Ne hardcode'inti reikšmės...
					}
					f = esriJsonFormat.writeFeatureObject(d.feature, {
						featureProjection: this.myMap.map.getView().getProjection()
					});
					featuresInParams[layerIdMeta[0] + "-" + layerIdMeta[1] + "-" + editType + "-" + params[layerIdMeta[0]][layerIdMeta[1]][editType].length] = d;
					params[layerIdMeta[0]][layerIdMeta[1]][editType].push(f);
				} else {
					console.warn("No layer info..."); // ...
				}
			}
		}.bind(this));
		var editsServiceId,
			edits = [],
			item;
		for (var serviceId in params) {
			if (params[serviceId]) {
				if (!editsServiceId) {
					editsServiceId = serviceId;
					for (var layerId in params[serviceId]) {
						item = Object.assign({
							id: layerId
						}, params[serviceId][layerId]);
						edits.push(item);
					}
				} else {
					break;
				}
			}
		}
		var verticalStreetSignsSupportsLayerId = this.myMap.getLayerId("verticalStreetSignsSupports");
		if (activeTask) {
			if (CommonHelper.layerIds.tasksRelated) {
				verticalStreetSignsSupportsLayerId = CommonHelper.layerIds.tasksRelated["verticalStreetSignsSupports"];
			}
		}
		edits.sort(function(a){
			// Tai yra labai aktualu perstūmus tvirtinimo vietos objektą, turintį susijusių KŽ!
			// Jei `edits` masyve bus taip, kad KŽ sluoksnio objektas su pakeitimais bus aukščiau už tvirtinimo vietos sluoksnio objektą su pakeitimais,
			// "išplauks" susijusių KŽ objektų geometrija!?? Nes pačiame duomenų rinkinyje yra kažkokios topologijos taisyklės tarp tvirtinimo vietų ir su jomis susijusių KŽ???
			if (parseInt(a.id) === verticalStreetSignsSupportsLayerId) {
				return -1;
			}
			return 0;
		});
		if (edits.length) {
			if (deletes) {
				if ((edits.length == 1) && !edits[0].deletes) { // Kitaip ir neturėtų būti, nes funkcionalumas įgyvendintas kaip `afterthought` linijų sujungimo atvejui...
					edits[0].deletes = deletes;
				}
			}
			params = {
				edits: JSON.stringify(edits),
				serviceId: editsServiceId
			};
			params = {
				params: params,
				featuresInParams: featuresInParams,
				serviceId: editsServiceId
			}
		}
		return params;
    };

    DrawHelper.prototype.saveFeature = function(dataToSave, activeTask, deletes, actionInitiator){
		var promise = new Promise(function(resolve, reject){
			// Jei išsaugojimas pavyko, esamame žemėlapyje randame pateikto feature'o analogą ir jį update'iname... Jei to analogo nerandame, tai perpiešiame jo sluoksnį?!!
			var params = this.getSaveFeatureParams(dataToSave, activeTask, deletes);
			if (params && params.serviceId) {
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "feature/apply-edits", function(json){
					if (json.res) {
						return json.res;
					}
				}, "POST", params.params).then(function(results){
					var success = true;
					var dataToSaveItem,
						feature,
						featuresRefreshData;
					if (!activeTask) {
						if (results) {
							featuresRefreshData = [];
							results.forEach(function(result){
								if (result.addResults) {
									var layerInfo;
									result.addResults.forEach(function(addResult, i){
										if (addResult.success) {
											dataToSaveItem = params.featuresInParams[params.serviceId + "-" + result.id + "-adds-" + i];
											feature = null;
											if (dataToSaveItem && dataToSaveItem.feature) {
												feature = dataToSaveItem.feature;
												layerInfo = this.myMap.getLayerInfo(dataToSaveItem.featureType);
												if (layerInfo) {
													if (layerInfo.objectIdField) {
														feature.set(layerInfo.objectIdField, addResult.objectId);
														feature.setId(addResult.objectId); // Tikrai prireiks!!!
													}
													if (layerInfo.globalIdField) {
														feature.set(layerInfo.globalIdField, addResult.globalId);
													}
												}
											}
											featuresRefreshData.push({
												serviceId: params.serviceId,
												layerId: result.id,
												result: addResult,
												action: "add",
												feature: feature
											});
										} else {
											success = false;
										}
									}.bind(this));
								}
								if (result.updateResults) {
									result.updateResults.forEach(function(updateResult, i){
										if (updateResult.success) {
											dataToSaveItem = params.featuresInParams[params.serviceId + "-" + result.id + "-updates-" + i];
											feature = null;
											if (dataToSaveItem && dataToSaveItem.feature) {
												feature = dataToSaveItem.feature;
												if (CommonHelper.unapprovedFromUsers) { // T. y. jei tas hack'as dar yra aktualus, pakeičiame feature'io atitinkamą atributą...
													feature.set("last_edited_user", "fake");
												}
											}
											featuresRefreshData.push({
												serviceId: params.serviceId,
												layerId: result.id,
												result: updateResult,
												action: "update",
												feature: feature
											});
										} else {
											success = false;
										}
									});
								}
								if (result.deleteResults) {
									result.deleteResults.forEach(function(deleteResult){
										if (deleteResult.success) {
											featuresRefreshData.push({
												serviceId: params.serviceId,
												layerId: result.id,
												result: deleteResult,
												action: "delete"
											});
										} else {
											// success = false; // Hmmm... Teoriškai gal nėra didelė tragedija, jei linijų sujungimo atveju jungiamoji linija nebūtų pašalinta?..
										}
									});
								}
							}.bind(this));
							var featureDestroyApproved = false;
							if (dataToSave.length == 1) {
								var f = dataToSave[0].feature;
								if (f) {
									if ((f.get("STATUSAS") == CommonHelper.verticalStreetSignDestroyedStatusValue) && (f.get("PATVIRTINTAS") == 1)) {
										if (!f.get("ATMESTA")) {
											featureDestroyApproved = true;
										}
									}
								}
							}
							if (featureDestroyApproved) {
								if (featuresRefreshData[0]) {
									featuresRefreshData[0].action = "delete";
									this.refreshFeatures(featuresRefreshData);
									results.featureDestroyApproved = true;
								} else {
									// Ar taip gali būti?..
								}
							} else {
								this.refreshFeatures(featuresRefreshData);
							}
						} else {
							success = false;
						}
					} else {
						if (results) {
							results.forEach(function(result){
								if (result.addResults) {
									var layerInfo;
									result.addResults.forEach(function(addResult, i){
										if (addResult.success) {
											dataToSaveItem = params.featuresInParams[params.serviceId + "-" + result.id + "-adds-" + i];
											feature = null;
											if (dataToSaveItem && dataToSaveItem.feature) {
												feature = dataToSaveItem.feature;
												layerInfo = this.myMap.getLayerInfo(dataToSaveItem.featureType);
												if (layerInfo) {
													if (layerInfo.objectIdField) {
														feature.set(layerInfo.objectIdField, addResult.objectId);
														feature.setId(addResult.objectId); // Tikrai prireiks!!!
													}
													if (layerInfo.globalIdField) {
														feature.set(layerInfo.globalIdField, addResult.globalId);
													}
												}
											}
										} else {
											success = false;
										}
									}.bind(this));
								}
								if (result.updateResults) {
									result.updateResults.forEach(function(updateResult){
										if (updateResult.success) {
											// ...
										} else {
											success = false;
										}
									});
								}
							}.bind(this));
						} else {
							success = false;
						}
					}
					if (success) {
						if (!activeTask) { // Jei nebus sąlygos, tai lyg ir bus taip: aktyvioje užduotyje paredaguojame feature'o geometriją, uždarome užduotį... Tada pasirodo originalus feature'as, bet su ta pakeista geometrija... Blogai! Turi gi likti sena, originali geometrija!?...
							this.activeGeometryEditingData = null; // Aktualu deactivateGeometryEditing()'ui...
						} else {
							// ESMĖ tokia: originalių sluoksnių objektų info turi likti kaip ir buvusi! T. y. jų geometrija turi atsistatyti į originaliąją?.. T. y. iš esmės turi suveikti deactivateGeometryEditing()'o funkcionalumas... Tik jis neturėtų liesti grynai jau task'e dalyvaujančių objektų?...
							// Hmmm... Koks lengviausias sprendimas? Gal tiesiog kažką pašalinti iš this.activeGeometryEditingData.features arba this.activeGeometryEditingData.relatedFeatures masyvų ir tiek?..
							// Gal suveiktų?..
							// Bandome...
							if (this.activeGeometryEditingData) {
								if (this.activeGeometryEditingData.features) {
									var featuresMod = [];
									this.activeGeometryEditingData.features.forEach(function(feature){
										if (!feature.get(CommonHelper.taskFeatureTaskGUIDFieldName)) {
											featuresMod.push(feature);
										}
									});
									this.activeGeometryEditingData.features = featuresMod;
								}
								if (this.activeGeometryEditingData.relatedFeatures) {
									var relatedFeaturesMod = [];
									this.activeGeometryEditingData.relatedFeatures.forEach(function(feature){
										if (!feature.get(CommonHelper.taskFeatureTaskGUIDFieldName)) {
											relatedFeaturesMod.push(feature);
										}
									});
									this.activeGeometryEditingData.relatedFeatures = relatedFeaturesMod;
								}
								this.activeGeometryEditingData.isTasksRelated = true;
							} else if (actionInitiator) {
								// Tai linijų dalinimo/apjungimo atvejis...
								if (results) {
									// FIXME... Reiktų elegantiškai išspręsti šitą situaciją, bet kol kas visai tinka visos užduoties atnaujinimas tie tiek...
									/*
									featuresRefreshData = [];
									results.forEach(function(result){
										if (result.addResults) {
											var layerInfo;
											result.addResults.forEach(function(addResult, i){
												if (addResult.success) {
													dataToSaveItem = params.featuresInParams[params.serviceId + "-" + result.id + "-adds-" + i];
													feature = null;
													if (dataToSaveItem && dataToSaveItem.feature) {
														feature = dataToSaveItem.feature;
														layerInfo = this.myMap.getLayerInfo(dataToSaveItem.featureType);
														if (layerInfo) {
															if (layerInfo.objectIdField) {
																feature.set(layerInfo.objectIdField, addResult.objectId);
																feature.setId(addResult.objectId);
															}
															if (layerInfo.globalIdField) {
																feature.set(layerInfo.globalIdField, addResult.globalId);
															}
														}
													}
													var layerIdMeta = CommonHelper.layerIds[dataToSaveItem.featureType];
													if (layerIdMeta) {
														featuresRefreshData.push({
															serviceId: params.serviceId,
															layerId: layerIdMeta[1], // result.id netinka, nes jis žymi sluoksnio id užduočių servise..
															result: addResult,
															action: "add",
															feature: feature
														});
													}
												}
											}.bind(this));
										}
										if (result.updateResults) {
											result.updateResults.forEach(function(updateResult, i){
												if (updateResult.success) {
													dataToSaveItem = params.featuresInParams[params.serviceId + "-" + result.id + "-updates-" + i];
													feature = null;
													if (dataToSaveItem && dataToSaveItem.feature) {
														feature = dataToSaveItem.feature;
													}
													var layerIdMeta = CommonHelper.layerIds[dataToSaveItem.featureType];
													if (layerIdMeta) {
														featuresRefreshData.push({
															serviceId: params.serviceId,
															layerId: layerIdMeta[1], // result.id netinka, nes jis žymi sluoksnio id užduočių servise..
															result: updateResult,
															action: "update",
															feature: feature
														});
													}
												}
											});
										}
									}.bind(this));
									this.refreshFeatures(featuresRefreshData);
									*/
									// Va čia ne elegantiškas sprendimas, kuomet refresh'inama visa užduotis (refresh'o metu persigeneruoja užduoties objektų list'as... Atkrenta bjauri užduotis tinkamai, teisingai populate'inti list'ą...)
									this.$store.commit("setActiveTask", {
										globalId: activeTask.globalId
									});
									// TODO... Gal kviesti kažką iš TaskHelper'io?..
								}
							}
						}
						resolve(results);
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
    };

    DrawHelper.prototype.destroyFeature = function(feature, featureType, activeTask){
		var promise = new Promise(function(resolve, reject){
			var layerIdMeta = CommonHelper.layerIds[featureType];
			if (layerIdMeta && layerIdMeta[2]) {
				layerIdMeta = [layerIdMeta[2], layerIdMeta[1]]; // Naudotojų taškams to prireikė...
			}
			if (activeTask) {
				layerIdMeta = this.getLayerIdMeta4ActiveTask(featureType);
			}
			if (layerIdMeta) {
				var layerInfo = this.myMap.getLayerInfo(featureType) || {},
					objectIdField = layerInfo.objectIdField || "OBJECTID";
				if (objectIdField) {
					var params = {
						serviceId: layerIdMeta[0],
						edits: JSON.stringify([{
							id: layerIdMeta[1],
							deletes: [feature.get(objectIdField)]
						}])
					};
					CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "feature/apply-edits", function(json){
						if (json.res) {
							return json.res;
						}
					}, "POST", params).then(function(results){
						var success = true;
						if (results) {
							var featuresRefreshData = [];
							results.forEach(function(result){
								if (result.deleteResults) {
									result.deleteResults.forEach(function(deleteResult){
										if (deleteResult.success) {
											featuresRefreshData.push({
												serviceId: params.serviceId,
												layerId: result.id,
												result: deleteResult,
												action: "delete"
											});
										} else {
											success = false;
										}
									});
								} else {
									success = false;
								}
							});
							this.refreshFeatures(featuresRefreshData);
						} else {
							success = false;
						}
						if (success) {
							resolve(results);
						} else {
							reject();
						}
					}.bind(this), function(){
						reject();
					}.bind(this));
				} else {
					reject();
				}
			} else {
				reject();
			}
		}.bind(this));
		return promise;
    };

    DrawHelper.prototype.getOrigFeature = function(data){
		var origFeature = null,
			serviceId = data.type == "v" ? "street-signs-vertical" : "street-signs",
			layerId = parseInt(data.layerId);
		this.myMap.map.getLayers().forEach(function(layer){
			if (layer.service) {
				if (layer.service.id == serviceId && layer.getLayers) {
					layer.getLayers().forEach(function(l){
						if (layerId === l.layerId) {
							if (l.objectIdField) {
								origFeature = l.getSource().getFeatureById(data.feature.get(l.objectIdField));
								if (origFeature) {
									origFeature.layer = l;
								}
							}
						}
					}.bind(this));
				}
			}
		}.bind(this));
		if (!origFeature) {
			serviceId = null;
			if (["vms-inventorization-l", "vms-inventorization-p"].indexOf(data.type) != -1) {
				serviceId = "vms-inventorization";
			} else if (["vvt"].indexOf(data.type) != -1) {
				serviceId = "vvt";
			} else if (data.featureType == "userPoints") {
				serviceId = "user-points";
			}
			if (serviceId) {
				this.myMap.map.getLayers().forEach(function(layer){
					if (layer.service) {
						if (layer.service.id == serviceId && layer.getLayers) {
							layer.getLayers().forEach(function(l){
								if (serviceId == "vvt" || serviceId == "user-points") {
									if (layerId === l.layerId) {
										if (l.objectIdField) {
											origFeature = l.getSource().getFeatureById(data.feature.get(l.objectIdField));
											if (origFeature) {
												origFeature.layer = l;
											}
										}
									}
								} else {
									if (l.service && (l.service.id == data.type)) {
										if (l.getLayers) {
											l.getLayers().forEach(function(lInner){
												if (layerId === lInner.layerId) {
													if (lInner.objectIdField) {
														origFeature = lInner.getSource().getFeatureById(data.feature.get(lInner.objectIdField));
														if (origFeature) {
															origFeature.layer = lInner;
														}
													}
												}
											}.bind(this));
										}
									}
								}
							}.bind(this));
						}
					}
				}.bind(this));
			}
		}
		if (!origFeature) {
			if (data.isTasksRelated || (["task-related", "task-related-v"].indexOf(data.type) != -1)) {
				this.myMap.map.getLayers().forEach(function(layer){
					if (layer.isTasksRelatedLayer) {
						if (layer.getLayers) {
							layer.getLayers().forEach(function(l){
								if (layerId == l.layerId) {
									if (l.objectIdField) {
										origFeature = l.getSource().getFeatureById(data.feature.get(l.objectIdField));
										if (origFeature) {
											origFeature.layer = l;
										}
									}
								}
							}.bind(this));
						}
					}
				}.bind(this));
				if (!origFeature) {
					if (data.type == "task-related-v") {
						this.myMap.map.getLayers().forEach(function(layer){
							if (layer.service) {
								if (layer.service.id == "street-signs-vertical" && layer.getLayers) {
									layer.getLayers().forEach(function(l){
										if (layerId === l.layerId) {
											if (l.objectIdField) {
												origFeature = l.getSource().getFeatureById(data.feature.get(l.objectIdField));
												if (origFeature) {
													origFeature.layer = l;
												}
											}
										}
									}.bind(this));
								}
							}
						}.bind(this));	
					}
				}
			}
		}
		return origFeature;
    };

    DrawHelper.prototype.activateGeometryEditing = function(data){
		delete data.origSignFeatures;
		var e = {
			success: false
		};
		if (data && data.feature) {
			// DĖMESIO!!! Pačio data.feature redagavimui naudoti negalime, nes šis objektas yra tik "tarpinis", sukurtas
			// naudojant MapHelper.createFeature() iš `featureDescr`!?? Jei redagavimui naudosime jį, tai KŽ sluoksnyje esantis originalus jo atitikmuo
			// taip ir liks nepajudintas!!!... O mums gi aktualu redaguoti vienu metu abu! Tiek data.feature (kuris atlieka highlight'into objekto funkciją), tiek
			// originalų objektą...
			// -
			// Ką daryti? Bandysime žemėlapyje rasti originalų objektą! Ir jį kažkaip susieti su data.feature!!!
			e.success = true;
			var feature = data.feature,
				featureTypeToMod = "highlighted-source", // "orig", "highlighted", "both", "highlighted-source"
				features = [],
				origFeature;
			if (data.isNew) {
				origFeature = feature;
			} else {
				origFeature = this.getOrigFeature(data);
			}
			if (origFeature) {
				if (origFeature.getGeometry().getType() == "Point") {
					featureTypeToMod = "both";
				}
				if (featureTypeToMod == "orig") {
					// Turėtų redaguoti tik originalų objektą, t. y. `highlighted` objektas net nepajudės iš vietos?
					features.push(origFeature);
				} else if (featureTypeToMod == "highlighted") {
					// Turėtų redaguoti tik `highlighted` objektą, t. y. originalus objektas net nepajudės iš vietos?
					features.push(feature);
				} else if (featureTypeToMod == "both") {
					// Kažkokiu mistišku būdu vienu metu redaguoja abu?!
					// TODO, FIXME! Šis būdas nėra išbaigtas! Neveikia, kai turime už'cache'uotą geometriją!!!
					features.push(feature);
					if (origFeature != feature) { // Taip būdavo, kai aktyvuodavome redagavimą naujai sukurtam objektui... Didelė tikimybė, kad po to funkcionalumas pasikeis
					// ir šios sąlygos nebereiks... Šiaip ji niekam nekenkia...
						features.push(origFeature);
					}
				} else if (featureTypeToMod == "highlighted-source") {
					// Originalaus objekto geometriją prilygina `highlighted` objekto geometrijai ir redaguoja tik originalų objektą...
					origFeature.setGeometry(feature.getGeometry());
					features.push(origFeature);
				}
				features.forEach(function(feature){
					feature.origGeometry = feature.getGeometry().clone();
					if (feature.layer && feature.layer.rotationField) {
						feature.origRotation = {
							field: feature.layer.rotationField,
							value: feature.get(feature.layer.rotationField)
						};
					}
					feature.origProperties = Object.assign({}, feature.getProperties());
				});
				var featuresCollection = new Collection(features);
				this.activeGeometryEditingData = {
					features: features,
					origFeature: origFeature,
					dataFeature: feature,
					type: featureTypeToMod,
					featureType: data.featureType,
					isNew: data.isNew
				};
				if (data.type != "vvt") {
					if (origFeature.getGeometry().getType() == "Point") {
						if (["verticalStreetSigns", "verticalStreetSignsSupports"].indexOf(data.featureType) != -1) {
							if (data.additionalData && data.additionalData.signs) {
								var origSignFeature,
									origSignFeatures = [],
									allOrigSignFeaturesFound = true;
								data.additionalData.signs.forEach(function(signFeature){
									origSignFeature = this.getOrigFeature({
										feature: signFeature,
										type: data.type, // Čia įdomesnis atvejis, kai dabartinis elementas yra task'o KŽ, o tarp kitų atramos KŽ yra ir originaliame sluoksnyje esančių KŽ...
										layerId: signFeature.layerId,
										isTasksRelated: signFeature.isTasksRelated
									});
									if (origSignFeature) {
										origSignFeatures.push(origSignFeature);
									} else {
										console.warn("NO origSignFeature for...", signFeature, data.type); // TEMP...
										allOrigSignFeaturesFound = false;
									}
								}.bind(this));
								if (allOrigSignFeaturesFound) {
									if (data.featureType == "verticalStreetSignsSupports") {
										origSignFeatures.forEach(function(feature){
											feature.origGeometry = feature.getGeometry().clone();
										});
										this.activeGeometryEditingData.relatedFeatures = origSignFeatures;
										data.origSignFeatures = origSignFeatures; // Aktualu išsaugant (t. y. kviečiant this.myMap.drawHelper.saveFeature())!
									} else {
										this.activeGeometryEditingData.origSignFeatures = origSignFeatures;
										this.activeGeometryEditingData.data = data;
										this.drawTempCircles();
									}
								} else {
									e.success = false;
									e.message = "Objekto redagavimas negalimas, nes žemėlapyje nerasti visi susiję KŽ";
								}
							} else {
								console.log("No signs...");
							}
						}
						if (e.success) {
							if (!this.translateInteraction) {
								this.translateInteraction = new Translate({
									features: featuresCollection
								});
								this.translateInteraction.on("translatestart", function(e){
									this.disableRotation();
									this.onModifyStart(e);
								}.bind(this));
								this.translateInteraction.on("translateend", function(e){
									this.enableRotationIfNeeded(origFeature, data.featureType, feature);
									this.onModifyEnd(e);
									if (data.featureType == "verticalStreetSignsSupports") {
										this.updateRelatedStreetSignsGeometry(e, this.activeGeometryEditingData);
									}
								}.bind(this));
								if (data.featureType == "verticalStreetSignsSupports") {
									this.translateInteraction.on("translating", function(e){
										this.updateRelatedStreetSignsGeometry(e, this.activeGeometryEditingData);
										this.redrawActiveFeatureAdditionalFeatures();
									}.bind(this));
								} else if (data.featureType == "verticalStreetSigns") {
									if (data.isNew) {
										this.rotateStreetSignFeature();
									}
									this.translateInteraction.on("translating", function(){
										this.rotateStreetSignFeature(); // Mes norime, kad keičiant KŽ geometriją (t. y. padėtį vietovėje) keistųsi ir KŽ pasukimo kampas (priklausomai nuo atramos padėties vietovėje)
										this.redrawActiveFeatureAdditionalFeatures();
									}.bind(this));
								}
								this.myMap.addInteraction(this.translateInteraction);
							}
							this.enableRotationIfNeeded(origFeature, data.featureType, feature);
						}
					} else {
						if (!this.modifyInteraction) {
							this.modifyInteraction = new Modify({
								features: featuresCollection
							});
							this.modifyInteraction.on("modifystart", function(e){
								this.onModifyStart(e);
							}.bind(this));
							this.modifyInteraction.on("modifyend", function(e){
								this.onModifyEnd(e);
							}.bind(this));
							this.myMap.addInteraction(this.modifyInteraction);
							var snapInteractionNeeded = this.myMap.isSnapInteractionNeeded(data, true);
							if (snapInteractionNeeded) {
								this.myMap.addSnapInteraction(snapInteractionNeeded);
							}
							this.myMap.addMeasurementInteractionIfNeeded(origFeature.getGeometry().getType(), this.modifyInteraction, "modify");
						}
					}
				}
			} else {
				e.success = false;
				e.message = "Objekto redagavimas negalimas, nes žemėlapyje nėra aktyvaus objekto atitikmens";
				console.warn("No orig. feature", data);
			}
			if (!e.success) {
				this.activeGeometryEditingData = null;
			}
		}
		return e;
    };

    DrawHelper.prototype.deactivateGeometryEditing = function(){
		if (this.activeGeometryEditingData) {
			this.activeGeometryEditingData.features.forEach(function(feature){
				if (feature.origGeometry) {
					feature.styleGeom = null; // Kad sena už`cache`uota geometrija tikrai niekuo neįtakotų...
					delete feature.geometryModified;
					feature.setGeometry(feature.origGeometry);
				} else {
					console.warn("No origGeometry!!!", feature);
				}
				if (feature.origProperties) {
					delete feature.origProperties.geometry;
					if (CommonHelper.scEnabled) {
						feature.set(CommonHelper.customSymbolIdFieldName, null); // BIG TODO! Šitas reikalingas tik laikinai, kol servise net nėra tokio lauko...
					}
					var uniqueSymbolTimestamp = feature.get("unique-symbol-timestamp");
					feature.setProperties(feature.origProperties, true);
					if (uniqueSymbolTimestamp) {
						feature.set("unique-symbol-timestamp", uniqueSymbolTimestamp);
					}
				}
				if (feature.origRotation) {
					// feature.set(feature.origRotation.field, feature.origRotation.value); // Šito nebereikia, nes jį padengia bendras visų atributų atstatymas su feature.setProperties()!
					this.$vBus.$emit("street-sign-feature-rotation-changed", {
						feature: this.activeGeometryEditingData.origFeature
					});
				}
			}.bind(this));
			if (this.activeGeometryEditingData.relatedFeatures) {
				this.activeGeometryEditingData.relatedFeatures.forEach(function(feature){
					if (feature.origGeometry) {
						feature.styleGeom = null; // Kad sena už`cache`uota geometrija tikrai niekuo neįtakotų...
						feature.setGeometry(feature.origGeometry);
					} else {
						console.warn("No origGeometry!!!", feature);
					}
				});
			}
			if (this.activeGeometryEditingData.type == "highlighted-source") {
				this.activeGeometryEditingData.dataFeature.setGeometry(this.activeGeometryEditingData.origFeature.getGeometry());
			}
			if (["verticalStreetSigns", "verticalStreetSignsSupports"].indexOf(this.activeGeometryEditingData.featureType) != -1) {
				if (this.$store.state.activeFeature) { // Čia toks minimalus apsidraudimas... Jei jo nebus, tai redrawActiveFeatureAdditionalFeatures() būtų kviečiamas
				// pvz. ir tada, kai uždaromas popup'as... Bet gi mus tai klaidintų! Užsidarius popup'ui turi išsivalyti ir tos pagalbinės linijos...
					if (this.activeGeometryEditingData.isTasksRelated) {
						// Ar tikrai ok, jog nekviečiamas this.redrawActiveFeatureAdditionalFeatures()?
					} else {
						this.redrawActiveFeatureAdditionalFeatures();
					}
				} else {
					this.$vBus.$emit("redraw-active-feature-additional-features"); // Tiesiog išvalys...
				}
			}
			this.activeGeometryEditingData = null;
		}
		if (this.translateInteraction) {
			this.myMap.removeInteraction(this.translateInteraction);
			this.translateInteraction = null;
		}
		if (this.modifyInteraction) {
			this.myMap.removeInteraction(this.modifyInteraction);
			this.modifyInteraction = null;
		}
		this.myMap.removeSnapInteraction();
		this.myMap.removeMeasureTooltip();
		this.removeTempCircles();
		this.disableRotation();
    };

    DrawHelper.prototype.onModifyStart = function(e){
		e.features.getArray().forEach(function(feature){
			feature.styleGeom = null;
			feature.ignoreStyleGeom = true; // Aktualu už'cache'uotai simbolių geometrijai... Tegul ji update'inase redagavimo metu!
			feature.modifyInAction = true;
		});
    };

    DrawHelper.prototype.onModifyEnd = function(e){
		e.features.getArray().forEach(function(feature){
			feature.modifyInAction = false;
			feature.styleGeom = null;
			feature.geometryModified = true;
			feature.ignoreStyleGeom = false;
			feature.changed(); // Reikalingas, nes naudojame `modifyInAction`... Jei jo nebūtų, tai ir šis nereikalingas?
		});
    };

    DrawHelper.prototype.updateRelatedStreetSignsGeometry = function(e, activeGeometryEditingData){
		// Senojoje KŽ app buvo iššaukiamas "street-signs-vertical-evt" įvykis, kuris kviesdavo metodą drawTempFeatures()! Šioje app aktualus metodas showActiveFeatureAdditionalFeatures()!
		// Senojoje KŽ app dar buvo aktualus metodas syncStreetSignFeatures()!
		if (activeGeometryEditingData.relatedFeatures) {
			var origFeatureCoordinates = activeGeometryEditingData.origFeature.getGeometry().getCoordinates(),
				origFeatureOrigCoordinates = activeGeometryEditingData.origFeature.origGeometry.getCoordinates(),
				deltaX = origFeatureCoordinates[0] - origFeatureOrigCoordinates[0],
				deltaY = origFeatureCoordinates[1] - origFeatureOrigCoordinates[1],
				relatedFeatureCoordinates;
			activeGeometryEditingData.relatedFeatures.forEach(function(relatedFeature){
				if (relatedFeature.origGeometry) {
					relatedFeatureCoordinates = relatedFeature.origGeometry.clone().getCoordinates();
					relatedFeatureCoordinates[0] += deltaX;
					relatedFeatureCoordinates[1] += deltaY;
					relatedFeature.getGeometry().setCoordinates(relatedFeatureCoordinates);
				}
			});
		}
    };

    DrawHelper.prototype.redrawActiveFeatureAdditionalFeatures = function(){
		var e = {
			type: "v",
			additionalData: {}
		};
		if (this.activeGeometryEditingData) {
			if (this.activeGeometryEditingData.featureType == "verticalStreetSignsSupports") {
				e.additionalData.supports = [this.activeGeometryEditingData.origFeature];
				e.additionalData.signs = this.activeGeometryEditingData.relatedFeatures;
			} else if (this.activeGeometryEditingData.featureType == "verticalStreetSigns") {
				if (this.activeGeometryEditingData.data && this.activeGeometryEditingData.origSignFeatures) {
					if (this.activeGeometryEditingData.data.additionalData) {
						e.additionalData.supports = this.activeGeometryEditingData.data.additionalData.supports;
						e.additionalData.signs = this.activeGeometryEditingData.origSignFeatures;
					}
				}
				if (this.activeGeometryEditingData.isNew) {
					e.additionalData.newSignFeature = this.activeGeometryEditingData.origFeature;
				}
			}
		}
		this.$vBus.$emit("redraw-active-feature-additional-features", e);
    };

    DrawHelper.prototype.rotateStreetSignFeature = function(){
		if (this.activeGeometryEditingData) {
			if (this.activeGeometryEditingData.featureType == "verticalStreetSigns") {
				if (this.activeGeometryEditingData.origFeature.origRotation) {
					if (this.activeGeometryEditingData.origFeature && this.activeGeometryEditingData.data && this.activeGeometryEditingData.data.additionalData) {
						var additionalData = this.activeGeometryEditingData.data.additionalData,
							supportFeature;
						if (additionalData.supports && additionalData.supports.length == 1) {
							supportFeature = additionalData.supports[0];
						}
						if (supportFeature) {
							var featureCoordinates = this.activeGeometryEditingData.origFeature.getGeometry().getCoordinates(),
								supportFeatureCoordinates = supportFeature.getGeometry().getCoordinates(),
								angle = Math.atan2(supportFeatureCoordinates[0] - featureCoordinates[0], supportFeatureCoordinates[1] - featureCoordinates[1]) * 180 / Math.PI;
							if (angle < 0) {
								angle += 360;
							}
							this.activeGeometryEditingData.origFeature.set(this.activeGeometryEditingData.origFeature.origRotation.field, Math.round(angle));
							this.$vBus.$emit("street-sign-feature-rotation-changed", {
								feature: this.activeGeometryEditingData.origFeature,
								temp: true
							});
						}
					}
				}
			}
		}
    };

    DrawHelper.prototype.drawTempCircles = function(){
		if (this.activeGeometryEditingData) {
			if (this.activeGeometryEditingData.featureType == "verticalStreetSigns") {
				if (this.activeGeometryEditingData.origFeature && this.activeGeometryEditingData.data && this.activeGeometryEditingData.data.additionalData) {
					var additionalData = this.activeGeometryEditingData.data.additionalData,
						supportFeature;
					if (additionalData.supports && additionalData.supports.length == 1) {
						supportFeature = additionalData.supports[0];
					}
					if (supportFeature) {
						this.$vBus.$emit("draw-temp-circles", supportFeature);
					}
				}
			}
		}
    };

    DrawHelper.prototype.removeTempCircles = function(){
		this.$vBus.$emit("draw-temp-circles");
    };

    DrawHelper.prototype.enableRotationIfNeeded = function(feature, featureType, refFeature){
		if (featureType != "verticalStreetSigns") {
			var opposite = false;
			if (featureType == "verticalStreetSigns") {
				opposite = true;
			}
			this.rotateInteraction = new RotateInteraction({
				feature: feature,
				refFeature: refFeature,
				myMap: this.myMap,
				opposite: opposite
			});
			this.myMap.map.addInteraction(this.rotateInteraction);
		}
    };

    DrawHelper.prototype.disableRotation = function(){
		if (this.rotateInteraction) {
			this.rotateInteraction.destroy();
			this.myMap.map.removeInteraction(this.rotateInteraction);
			this.rotateInteraction = null;
		}
    };

    DrawHelper.prototype.refreshFeatures = function(data){
		data.forEach(function(item){
			var feature = {
				layerId: item.layerId
			};
			if (item.serviceId == "tasks") {
				// feature.isTasksRelated = true; // Reikėtų šitą naudoti... Ir teisingai įgyvendinti funkcionalumą (kad tinkamai atsinaujintų elementai žemėlapyje ir užduoties dialog'e...)
			}
			this.myMap.setLayerForFeature(feature, item.serviceId);
			if (feature.layer) {
				var fullLayerRefresh = true;
				if (item.action == "add") {
					if (item.feature) {
						feature.layer.getSource().addFeature(item.feature);
						fullLayerRefresh = false;
					}
				} else {
					if (item.result.objectId) {
						var refFeature = feature.layer.getSource().getFeatureById(item.result.objectId);
						if (refFeature) {
							if (item.action == "delete") {
								feature.layer.getSource().removeFeature(refFeature);
								fullLayerRefresh = false;
							}
							if (item.action == "update") {
								if (item.feature) {
									refFeature.setProperties(item.feature.getProperties());
									fullLayerRefresh = false;
								}
							}
						}
					}
				}
				if (fullLayerRefresh) {
					console.warn("fullLayerRefresh");
					feature.layer.getSource().refresh();
				}
			}
		}.bind(this));
    };

	DrawHelper.prototype.getLayerIdMeta4ActiveTask = function(featureType){
		if (CommonHelper.layerIds.tasksRelated) {
			var layerIdMeta = ["tasks", CommonHelper.layerIds.tasksRelated[featureType]];
			return layerIdMeta;
		}
	}

	DrawHelper.prototype.saveSplitFeature = function(origFeatureData, features, activeTask){
		var promise = new Promise(function(resolve, reject){
			var layerInfo = this.myMap.getLayerInfo(origFeatureData.featureType);
			if (layerInfo) {
				var dataToSave = [];
				var origFeatureProperties = Object.assign({}, origFeatureData.feature.getProperties());
				delete origFeatureProperties.geometry;
				delete origFeatureProperties[layerInfo.globalIdField];
				delete origFeatureProperties[layerInfo.objectIdField];
				delete origFeatureProperties[CommonHelper.taskFeatureTaskGUIDFieldName];
				features.forEach(function(feature, i){
					if (!i) {
						// Realiai pirmam elementui priskiriame origFeatureData.feature GlobalId/OBJECTID ir tiek?..
						feature.set(layerInfo.globalIdField, origFeatureData.feature.get(layerInfo.globalIdField));
						feature.set(layerInfo.objectIdField, origFeatureData.feature.get(layerInfo.objectIdField));
						if (origFeatureData.feature.get(CommonHelper.taskFeatureTaskGUIDFieldName)) {
							feature.set(CommonHelper.taskFeatureTaskGUIDFieldName, origFeatureData.feature.get(CommonHelper.taskFeatureTaskGUIDFieldName));
						}
					} else {
						feature.setProperties(origFeatureProperties);
					}
					dataToSave.push({
						feature: feature,
						featureType: origFeatureData.featureType
					});
				});
				this.saveFeature(dataToSave, activeTask, null, "split").then(function(result){
					resolve(result);
				}, function(){
					reject();
				});
			} else {
				reject();
			}
		}.bind(this));
		return promise;
	}

	DrawHelper.prototype.saveJoinFeature = function(origFeatureData, features, joinedCoordinates, activeTask){
		var promise = new Promise(function(resolve, reject){
			var layerInfo = this.myMap.getLayerInfo(origFeatureData.featureType);
			if (layerInfo) {
				if (joinedCoordinates) {
					var dataToSave = [],
						newFeature = origFeatureData.feature.clone();
					newFeature.getGeometry().setCoordinates(joinedCoordinates);
					dataToSave.push({
						feature: newFeature,
						featureType: origFeatureData.featureType
					});
					var deletes = [];
					features.forEach(function(feature){
						deletes.push(feature.get(layerInfo.objectIdField));
					});
					this.saveFeature(dataToSave, activeTask, deletes, "join").then(function(result){
						resolve(result);
					}, function(){
						reject();
					});
				} else {
					// Prie origFeatureData.feature geometrijos prijungiame `features` geometrijas... O `features` objektus pašaliname...
					// Kas yra apjungimas išvis? O jei feature'iai geografiškai nesiliečia??...
					if (origFeatureData && origFeatureData.feature) {
						var allFeatures = [origFeatureData.feature],
							featuresInfo = {};
						allFeatures = allFeatures.concat(features);
						allFeatures.forEach(function(feature, i){
							var featureCoordinates = feature.getGeometry().getCoordinates(),
								featureFirstCoordinates = featureCoordinates[0],
								featureLastCoordinates = featureCoordinates[featureCoordinates.length - 1];
							allFeatures.forEach(function(refFeature, j){
								var refFeatureCoordinates = refFeature.getGeometry().getCoordinates();
								if (i != j) {
									featuresInfo[i] = {
										startEqualsTo: null,
										endEqualsTo: null
									};
									// Reikėtų apvalinti koordinates iki metrų? Ir tikrinti.. vienodus identiškus taškus tarp `feature` ir `refFeature`?..
									console.log("ANALYZE", i, j, featureFirstCoordinates, featureLastCoordinates, refFeatureCoordinates[0], refFeatureCoordinates[refFeatureCoordinates.length - 1]);
									// NEBAIGTA...
								}
							});
						});
					}
					console.log("JOIN FEATURE", featuresInfo); // NEBAIGTA...
					reject();
				}
			} else {
				reject();
			}
		}.bind(this));
		return promise;
	}

    return DrawHelper;

}());

export default DrawHelper;