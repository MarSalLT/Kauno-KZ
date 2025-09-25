<script>
	import store from "../../admin-app/store.js";
	import Vue from "vue";

	export default {
		addressSearchUrl: "https://maps.kaunas.lt/arcgis/rest/services/GP/RC_lokatorius/GeocodeServer/findAddressCandidates",
		addressSuggestionsUrl: "https://maps.kaunas.lt/arcgis/rest/services/GP/RC_lokatorius/GeocodeServer/suggest",
		adminPageBase: window.appEnv == "development" ? "/admin" : process.env.VUE_APP_ROOT + "",
		colors: {
			sc: {
				blue: "#0b4c9d",
				red: "red",
				green: "green",
				yellow: "yellow",
				grey: "grey"
			}
		},
		customEditorFieldName: "editor_app",
		customSymbolIdFieldName: "UnikSimbolID",
		cutUrl: "https://kp.kaunas.lt/image/rest/services/Utilities/Geometry/GeometryServer/cut",
		defaultAreaFieldName: "Shape__Area",
		defaultLengthFieldName: "Shape__Length",
		dwg2JsonGPUrl: "https://kp.kaunas.lt/image/rest/services/GP_EOPIS/dwgtojson/GPServer/",
		dwg2JsonGPToolName: "DWG%20To%20JSON",
		dwg2JsonGPOutputParamName: "json_string",
		historyLayerIdOffset: 8,
		mapillaryClientId: "dTV1a0JzZUFUVVM4alVidEU0M2ZjTzo0NDk3NjUwZGY2Yjc1Y2U2",
		mapillaryOrganizationId: "300602728221908",
		mapillaryOrganizationKey: "1Wkk8dAr5kab6Rl88sgyTt",
		mapillaryV4ClientToken: "MLY|4257478260995500|c787fd639a06ff5fcc12d494c0d7a81a",
		pointsZoomThreshold: 7,
		proxyUrl: process.env.VUE_APP_ROOT + "/web-services/proxy",
		scEnabled: true || (window.appEnv == "development"),
		scUniqueContent: ["117", "118", "304", "314", "315", "316", "317", "318", "319", "329", "330", "413", "507", "508", "509", "510", "511", "512", "513", "514", "515", "516", "517", "518", "520", "521", "522", "523", "524", "529", "530", "531", "540", "541", "542", "543", "545", "546", "604", "611", "612", "615", "616", "617", "620", "622", "623", "801", "802", "803", "804", "805", "806", "809", "810", "811", "824", "825", "826", "828", "829", "830", "831", "832", "833", "834", "835", "836", "837", "838", "840", "842", "843", "920", "921", "922", "923", "924"],
		scUniqueAdvancedContent: ["507", "508", "509", "510", "511", "512", "513", "514", "515", "516", "517", "518", "519", "520", "521", "522", "523", "524", "540", "541", "542", "543", "545", "604", "611", "620", "622", "623", "840", "842", "843", "920", "921", "922", "923", "924"],
		scUniqueVeryBasicContent: ["117", "118", "304", "314", "315", "316", "317", "318", "319", "329", "330", "529", "530", "531", "540", "541", "542", "543", "545", "546", "612", "615", "617", "620", "801", "802", "803", "804", "805", "806", "809", "810", "811", "824", "825", "826", "828"],
		sc5XXContent: ["507", "508", "509", "510", "511", "512", "513", "514", "515", "516", "517", "518", "520", "521", "522", "523", "524"],
		securedServicesUrlMatch: "https://zemelapiai.vplanas.lt/arcgisin/rest/services/Kelio_zenklai/", // FIXME! Idealiu atveju tą turi gauti iš serverio, nes ten analogiškas naudojamas.. Dabar reikia nustatinėti tiek server-side'e, tiek client-side'e...
		statsGPUrl: "https://kp.kaunas.lt/image/rest/services/GP_EOPIS/",
		streetSignsZoomThreshold: 2,
		symbolsUrl: process.env.VUE_APP_ROOT + "/web-services/" + "symbols/get-symbol?id=",
		symbolTextFieldName: "ZENKLO_TEKSTAS",
		systemLastEditor: "KPDC" + "\\" + "appuser",
		taskFeatureActionFieldName: "UzduotiesVeiksmas",
		taskFeatureActionValues: {
			"add": 0,
			"update": 1,
			"delete": 2
		},
		taskFeatureOriginalGlobalIdFieldName: "OriginalObjGlobalID",
		taskFeatureTaskGUIDFieldName: "UzduotiesGUID",
		unapprovedFromUsers: [], // Duomenys užsipildys iš Backend...
		verticalStreetSignDestroyedStatusValue: 3,
		webServicesRoot: process.env.VUE_APP_ROOT + "/web-services/",
		widthFieldName: "PLOTAS",
		layerIds: {
			verticalStreetSignsSupports: ["street-signs-vertical", 1],
			verticalStreetSigns: ["street-signs-vertical", 0],
			horizontalPoints: ["street-signs", 2],
			horizontalPolylines: ["street-signs", 3],
			horizontalPolygons: ["street-signs", 4],
			otherPoints: ["street-signs", 5],
			otherPolylines: ["street-signs", 6],
			otherPolygons: ["street-signs", 7],
			horizontalLayerIds: [2, 3, 4],
			otherLayerIds: [5, 6, 7],
			userPoints: ["street-signs", 16, "user-points"],
			tasksRelated: {
				tasks: 0,
				verticalStreetSignsSupports: 2,
				verticalStreetSigns: 1,
				horizontalPoints: 3,
				horizontalPolylines: 4,
				horizontalPolygons: 5,
				otherPoints: 6,
				otherPolylines: 7,
				otherPolygons: 8
			},
			tasks: ["tasks", 0]
		},
		importantFields: {
			verticalStreetSignsSupports: ["IRENGINIOID", "TVIRTINIMO_VIETA", "IRENGIMO_DATA", "STATUSAS", "PATVIRTINIMAS", "TVIRTINTOJAS", "ATMESTA", "NUSIDEVEJIMAS", "PRIEZIURA"],
			verticalStreetSigns: [
				"ZENKLO_ID",
				"KET_NR",
				"TIPAS",
				"IRENGIMO_DATA",
				"ZENKLO_TEKSTAS",
				"PASUKIMO_KAMPAS",
				"PASTATYMO_AUKSTIS",
				"BUKLE",
				"STATUSAS",
				"PATVIRTINTAS",
				"ATSPINDZIO_GRUPE",
				"ZENKLU_GRUPE",
				"SPRENDIMO_NR",
				"SPRENDIMO_DATA",
				"GALIOJA_NUO",
				"GALIOJA_IKI",
				"PASAL_PRIEZASTIS",
				"PASAL_DATA",
				"TVIRTINTOJAS",
				"ATMESTA",
				"NUSIDEVEJIMAS",
				"PRIEZIURA"
			],
			horizontalPoints: ["KET_NR", "ZENKL_BUDAS", "NUSIDEVEJIMAS", "PRIEZIURA", "DAZU_KIEKIS_PRIEZIURAI", "IRENGTA_NAUJAI", "DAZU_KIEKIS_NAUJAI_IRENGTAM"],
			horizontalPolylines: ["KET_NR", "ZENKL_BUDAS", "PLOTAS", "NUSIDEVEJIMAS", "PRIEZIURA", "DAZU_KIEKIS_PRIEZIURAI", "IRENGTA_NAUJAI", "DAZU_KIEKIS_NAUJAI_IRENGTAM"],
			horizontalPolygons: ["KET_NR", "ZENKL_BUDAS", "NUSIDEVEJIMAS", "PRIEZIURA", "DAZU_KIEKIS_PRIEZIURAI", "IRENGTA_NAUJAI", "DAZU_KIEKIS_NAUJAI_IRENGTAM"],
			otherPoints: ["TIPAS", "BUKLE", "STATUSAS", "NUSIDEVEJIMAS", "PRIEZIURA", "KIEKIS_PRIEZIURAI", "IRENGTA_NAUJAI", "KIEKIS_NAUJAI_IRENGTAM"],
			otherPolylines: ["TIPAS", "BUKLE", "Shape__Length", "STATUSAS", "NUSIDEVEJIMAS", "PRIEZIURA", "KIEKIS_PRIEZIURAI", "IRENGTA_NAUJAI", "KIEKIS_NAUJAI_IRENGTAM"],
			otherPolygons: ["TIPAS", "BUKLE", "STATUSAS", "NUSIDEVEJIMAS", "PRIEZIURA", "KIEKIS_PRIEZIURAI", "IRENGTA_NAUJAI", "KIEKIS_NAUJAI_IRENGTAM"],
			tasks: ["Pavadinimas", "Aprasymas", "Uzduoties_tipas", "Svarba", "Teritorija", "Pabaigos_data", "Adresas", "uzsakovo_email", "Uzsakovo_vardas", "Uzsakovo_imone", "Imone", "rangovo_email", "Statusas", "Patvirtinimas"],
			userPoints: ["tipas", "pastaba"],
			waze: ["street"]
		},

		getFetchPromise: function(url, jsonCallback, method, params, files, arrayBuffer){
			var promise = new Promise(function(resolve, reject){
				setTimeout(function(){
					var requestObject = {
						method: method
					};
					if (method == "POST") {
						if (files && files.length) {
							var body = new FormData();
							for (var property in params) {
								body.append(property, params[property]);
							}
							files.forEach(function(file){
								body.append(file.key, file);
							});
							requestObject.body = body;
						} else {
							requestObject.body = this.getFormBody(params);
							requestObject.headers = {
								"Content-Type": "application/x-www-form-urlencoded"
							};
						}
					}
					window.fetch(url, requestObject).then(function(response){
						if (response.status == 200) {
							if (arrayBuffer) {
								return response.arrayBuffer();
							} else {
								return response.text();
							}
						} else {
							if (!/user\/check-if-valid-session$/.test(url)) {
								Vue.prototype.$vBus.$emit("check-if-valid-session"); // Gal negražus toks sprendimas, bet veikia?..
							} else {
								console.log("Do not check-if-valid-session");
							}
							reject();
						}
					}).then(function(text){
						if (arrayBuffer) {
							resolve(text);
						} else {
							var json;
							if (text) {
								try {
									json = JSON.parse(text);
								} catch(err) {
									// ...
								}
							}
							if (json) {
								if (jsonCallback) {
									var result = jsonCallback(json);
									if (result) {
										resolve(result);
									} else {
										reject(json.reason);
									}
								} else {
									resolve(json);
								}
							} else {
								reject();
							}
						}
					}.bind(this), function(){
						reject();
					});
				}.bind(this), store.state.testDelay);
			}.bind(this));
			return promise;
		},

		getFormBody: function(params){
			var formBody = [];
			for (var property in params) {
				var encodedKey = encodeURIComponent(property),
					encodedValue = encodeURIComponent(params[property]);
				formBody.push(encodedKey + "=" + encodedValue);
			}
			formBody = formBody.join("&");
			return formBody;
		},

		prependProxyIfNeeded: function(url){
			if (this.securedServicesUrlMatch) {
				if (url.substring(0, this.securedServicesUrlMatch.length) == this.securedServicesUrlMatch) {
					url = this.proxyUrl + "?" + url;
				}
			}
			return url;
		},

		getPointFromString: function(str){
			str = str.split(/[\s,;]+/);
			var filteredStr = [];
			str.forEach(function(s){
				if (s) {
					filteredStr.push(s);
				}
			});
			if (filteredStr.length == 2) {
				var pattern = /^([-+]?[0-9]*\.?[0-9]*)°?$/,
					match1 = str[0].match(pattern),
					match2 = str[1].match(pattern);
				if (match1 && match2 && match1[0] && match2[0]) {
					// TODO: reikia protingesnio tikrinimo, pvz. reiktų tikrinti ar patenka į LT teritoriją!!!
					var x = parseFloat(match1[0]),
						y = parseFloat(match2[0]),
						wkid = 3346;
					if (x > y) {
						var tempX = x;
						x = y;
						y = tempX;
					}
					if (x <= 180 && y <= 180) {
						wkid = 4326;
					}
					var point = {
						x: x,
						y: y,
						wkid: wkid,
					};
					point.text = this.getCoordinatesText(point);
					return point;
				}
			}
		},

		getCoordinatesText: function(point, plain){
			var coordinates;
			if (point.wkid == 4326) {
				if (plain) {
					coordinates = [point.y.toFixed(5), point.x.toFixed(5)];
				} else {
					coordinates = ["Platuma" + ": " + point.y.toFixed(5) + "°", "Ilguma" + ": " + point.x.toFixed(5) + "°"];
				}
			} else {
				if (plain) {
					coordinates = [point.y.toFixed(), point.x.toFixed()];
				} else {
					coordinates = ["X: " + point.y.toFixed() + " m", "Y: " + point.x.toFixed() + " m"];
				}
			}
			return coordinates.join(", ");
		},

		routeTo: function(e){
			var featureGuid,
				featureServiceId,
				featureLayerId,
				historicMoment,
				type;
			if (e.feature) {
				if (e.feature.layer) {
					if ((["vvt"].indexOf(e.feature.layer.parentServiceId) != -1) || e.feature.layer.clickable) {
						type = e.feature.layer.parentServiceId;
					}
					featureGuid = e.feature.get(e.feature.layer.globalIdField) || e.feature.get(e.feature.layer.objectIdField);
					featureServiceId = e.feature.layer.parentServiceId;
					featureLayerId = e.feature.layer.layerId;
					historicMoment = e.feature.layer.historicMoment;
				}
			} else if (e.featureData) {
				featureGuid = e.featureData.globalId;
				featureServiceId = e.featureData.serviceId;
				featureLayerId = e.featureData.layerId;
				type = e.featureData.type
			}
			var routerParams = {};
			routerParams.path = e.router.history.current.path;
			routerParams.query = {};
			if (featureGuid) {
				featureGuid = this.stripGuid(featureGuid);
				if (type) {
					routerParams.query.t = type;
				} else if (featureServiceId == "street-signs-vertical") {
					routerParams.query.t = "v";
				} else {
					if (featureServiceId == "vms-inventorization-l" || featureServiceId == "vms-inventorization-p") {
						routerParams.query.t = featureServiceId;
					}
				}
				if (e.feature && e.feature.layer && e.feature.layer.isTasksRelatedLayer) {
					routerParams.query.t = "task-related" + (routerParams.query.t ? "-" + routerParams.query.t : "");
				}
				routerParams.query.l = featureLayerId;
				routerParams.query.id = featureGuid;
				if (historicMoment) {
					routerParams.query.h = historicMoment;
				}
			} else if (e.identifyUsingCoordinates) {
				routerParams.query.action = "identify";
				routerParams.query.coordinates = e.identifyUsingCoordinates.coordinate.join(",");
				routerParams.query.z = e.identifyUsingCoordinates.mapZoom;
				// TODO: gal dar kažkokią info pateikti? Pvz. aprėptį?
			}
			e.router.push(routerParams).catch(function(){
				// Darome prielaidą, kad tai klaida "Error: Avoided redundant navigation to current location"... TODO: reikia įsitikinti, jog tai tikrai ši klaida...
				e.vBus.$emit("route-to", routerParams);
			});
		},

		stripGuid: function(guid){
			if (guid && guid[0] == "{" && guid[guid.length - 1] == "}") {
				guid = guid.substring(1, guid.length - 1);
			}
			return guid;
		},

		isActiveFeatureTheSameAsActiveFeaturePreview: function(activeFeaturePreview, activeFeature){
			var theSame = false;
			if (activeFeaturePreview && activeFeature) {
				if (activeFeaturePreview.layer) {
					var activeFeaturePreviewGuid = this.stripGuid(activeFeaturePreview.get(activeFeaturePreview.layer.globalIdField) || activeFeaturePreview.get(activeFeaturePreview.layer.objectIdField));
					if (activeFeaturePreviewGuid == activeFeature.globalId) {
						theSame = true;
					}
				}
			}
			return theSame;
		},

		getPrettyDate: function(value, full, timeReference){
			if (value) {
				if (value == 253402293599000) {
					value = "&infin;";
				} else {
					if (timeReference) {
						if (timeReference.timeZone == "FLE Standard Time") {
							value += 2 * 60 * 60 * 1000;
							if (timeReference.respectsDaylightSaving) {
								// BIG FIXME! Neaišku, ar visiems laikams reikia daryti šį pokytį, ar tik datoms, kurios yra vasaros laiku... TODO: reikia pratestuoti žiemos metu...
								value += 1 * 60 * 60 * 1000;
							}
						} else {
							console.log("OTHER TIME ZONE...", timeReference.timeZone);
						}
					}
					var date = new Date(value);
					value = [date.getFullYear(), this.getPrettyDateComponent(date.getMonth() + 1), this.getPrettyDateComponent(date.getDate())].join("-");
					if (full) {
						value += " " + [this.getPrettyDateComponent(date.getHours()), this.getPrettyDateComponent(date.getMinutes())].join(":");
					}
				}
			}
			return value;
		},

		getPrettyDateComponent: function(component){
			return ("0" + component).slice(-2);
		},

		simpleSort: function(items, key){
			items.sort(function(a, b) {
				if (a[key] < b[key]) {
					return -1;
				}
				if (a[key] > b[key]) {
					return 1;
				}
				return 0;
			});
		},

		getPhotos: function(e){
			// console.log("GET PHOTOS...", e); // ...
			var promise = new Promise(function(resolve, reject){
				if ((e.feature || e.featureObjectId) && e.store) {
					var layerIdMeta,
						serviceUrl;
					if (e.featureType) {
						layerIdMeta = this.layerIds[e.featureType];
						serviceUrl = e.store.getters.getServiceUrl(layerIdMeta[0]);
					} else {
						if (e.feature.layer && e.feature.layer.service) {
							layerIdMeta = [e.feature.layer.serviceId, e.feature.layer.layerId];
							serviceUrl = e.feature.layer.service.url;
						}
					}
					if (layerIdMeta && serviceUrl) {
						var featureObjectId;
						if (e.featureObjectId) {
							featureObjectId = e.featureObjectId;
						} else if (e.feature) {
							if (e.store.state.myMap) {
								var layerInfo = e.store.state.myMap.getLayerInfo(e.featureType);
								if (layerInfo) {
									featureObjectId = e.feature.get(layerInfo.objectIdField);
								} else {
									if (e.featureType == "tasks") { // Toks atvejis būna pvz. kai tik atsidarome aktyvią užduotį ir iškart spaudžiame "Valdyti nuotraukas, brėžinius"...
										featureObjectId = e.feature.get("OBJECTID"); // Išimtis... Hardcode'iname lauko pavadinimą... FIXME!...
									} else {
										if (layerIdMeta[0] == "accidents-stats") {
											featureObjectId = e.feature.get("OBJECTID"); // Taip... Vėl nelabai gerai čia hardcode'inti tą lauko pavadinimą... Bet ESRI /identify rezultate negrąžina laukų tipų...
										} else {
											console.warn("NO LAYER info", e.featureType, e, layerIdMeta);
										}
									}
								}
							}
						}
						if (layerIdMeta && featureObjectId) {
							var url = serviceUrl + "/" + layerIdMeta[1] + "/" + featureObjectId + "/attachments";
							url = this.prependProxyIfNeeded(url);
							var tokenPromise = new Promise(function(resolve){
								resolve();
							}.bind(this));
							if ((e.featureType == "vms-inventorization-p") || (e.featureType == "vms-inventorization-l")) {
								tokenPromise = this.getToken({
									url: serviceUrl
								});
							}
							tokenPromise.then(function(token){
								var params = {
									f: "json"
								};
								if (token) {
									params.token = token;
								}
								if (e.historicMoment) {
									params.historicMoment = e.historicMoment;
								}
								this.getFetchPromise(url, function(json){
									if (json.attachmentInfos) {
										var photos = [];
										json.attachmentInfos.forEach(function(attachment){
											// Užduotims gali prikabinti visokio tipo failų, tad jiems išimtis...
											if ((attachment.contentType.substr(0, 6) == "image/") || ((e.featureType == "tasks") && !e.justImages)) {
												attachment.src = url + "/" + attachment.id;
												if (token) {
													attachment.src += "?token=" + token;
												}
												if (attachment.contentType.substr(0, 6) == "image/") {
													attachment.isImage = true;
												}
												photos.push(attachment);
											}
										}.bind(this));
										this.simpleSort(photos, "id");
										return photos;
									}
								}.bind(this), "POST", params).then(function(result){
									resolve(result);
								}.bind(this), function(){
									reject();
								}.bind(this));
							}.bind(this));
						} else {
							reject();
						}
					} else {
						reject();
					}
				} else {
					reject();
				}
			}.bind(this));
			return promise;
		},

		setFeatureAsUnapprovedIfNeeded: function(feature){
			if (this.unapprovedFromUsers) {
				var lastEditedUser = feature.get("last_edited_user");
				if (lastEditedUser && this.unapprovedFromUsers.indexOf(lastEditedUser) != -1) {
					// !!! Gali būti bėdų, jei tas pats objektas turi abu atributus?.. Pvz. 2021-10-06 skype su Justu susirašinėjimuose... Kai iš mob. prietaiso sukuri naują ir po to per web žemėlapį patvirtini...
					feature.set("PATVIRTINTAS", 0);
					feature.set("PATVIRTINIMAS", 0);
				}
			}
		},

		adjustFeatureProperties: function(feature, featureType, onlyForNew){
			var type,
				typeGroups,
				typeGroup,
				markingType;
			if (featureType == "verticalStreetSigns") {
				if (feature.layer && feature.layer.typeIdField) {
					type = feature.get(feature.layer.typeIdField);
					if (type) {
						var typeFirstChar = type.toString().substring(0, 1);
						typeGroups = {
							"1": "ISP",
							"2": "PIR",
							"3": "DRAU",
							"4": "NUK",
							"5": "NUR",
							"6": "INF",
							"7": "PAS",
							"8": "LENT"
						};
						typeGroup = typeGroups[typeFirstChar];
					}
					feature.set("TIPAS", typeGroup);
				}
			} else if (onlyForNew) {
				// BIG TODO, FIXME!!! Info imta iš senos app!!! Net neaišku koks yra info šaltinis... Lygtais kažkoks Vėjo excel'is...
				if (featureType == "horizontalPoints") {
					if (feature.layer && feature.layer.typeIdField) { // Pvz. nebus, jei kursime naują objektą išjungę jo sluoksnį!!!
						type = feature.get(feature.layer.typeIdField);
						if (type) {
							typeGroups = {
								"1161": "t",
								"1162": "t",
								"1163": "t",
								"1164": "t",
								"1165": "t",
								"1166": "t",
								"1167": "t",
								"1168": "t",
								"1169": "t",
								"11611": "t",
								"11621": "t",
								"11631": "t",
								"11641": "t",
								"11651": "t",
								"11691": "t",
								"1171": "t",
								"1172": "t",
								"118": "t",
								"119": "t",
								"120": "t",
								"121": "t",
								"123": "d",
								"11231": "d",
								"11232": "d",
								"124": "d",
								"128": "t",
								"129": "t",
								"130": "t",
								"131": "d",
								"133": "d",
								"14": "d",
								"15": "d",
								"16": "d"
							};
							markingType = typeGroups[type];
							if (markingType) {
								if (markingType == "d") {
									markingType = 2;
								} else if (markingType == "t") {
									markingType = 7;
								}
								feature.set("ZENKL_BUDAS", markingType);
							}
						}
					}
				} else if (featureType == "horizontalPolylines") {
					if (feature.layer && feature.layer.typeIdField) {
						type = feature.get(feature.layer.typeIdField);
						if (type) {
							typeGroups = {
								"11": "t",
								"110": "t",
								"111": "t",
								"112": "t",
								"1131": "t",
								"1132": "t",
								"1133": "t",
								"114": "t",
								"12": "t",
								"122": "t",
								"125": "t",
								"126": "t",
								"127": "d",
								"13": "d",
								"14": "d",
								"15": "t",
								"16": "t",
								"17": "t",
								"18": "t",
								"19": "d"
							};
							markingType = typeGroups[type];
							if (markingType) {
								if (markingType == "d") {
									markingType = 2;
								} else if (markingType == "t") {
									markingType = 7;
								}
								feature.set("ZENKL_BUDAS", markingType);
							}
						}
					}
				} else if (featureType == "horizontalPolygons") {
					if (feature.layer && feature.layer.typeIdField) {
						type = feature.get(feature.layer.typeIdField);
						if (type) {
							typeGroups = {
								"1151": "t",
								"1152": "t",
								"1153": "t",
								"132": 1,
								"1322": 1
							};
							markingType = typeGroups[type];
							if (markingType) {
								if (markingType == "t") {
									markingType = 7;
								}
								feature.set("ZENKL_BUDAS", markingType);
							}
						}
					}
				}
			}
		},

		getLastEditor: function(feature){
			var lastEditor = feature.attributes["last_edited_user"];
			if (lastEditor == this.systemLastEditor) {
				lastEditor = feature.attributes[this.customEditorFieldName]; // Darome prielaidą, kad objektas buvo redaguotas per šitą web app, tad pateikiame APP username'ą, o ne ESRI naudotoją...
			}
			return lastEditor;
		},

		getUniqueSymbolSrc: function(uniqueSymbolId, timestamp){
			if (uniqueSymbolId) {
				uniqueSymbolId = this.stripGuid(uniqueSymbolId);
				if (uniqueSymbolId) {
					if (uniqueSymbolId.toLowerCase) {
						uniqueSymbolId = uniqueSymbolId.toLowerCase();
					}
				}
			}
			var src = (process.env.VUE_APP_SC_TYPE == "test" ? "http://www.dummy.local/unique-symbols/" : this.symbolsUrl) + uniqueSymbolId + ".png";
			if (timestamp) {
				src += "?t=" + timestamp;
			}
			return src;
		},

		getUniqueSymbolElementSrc: function(itemId, timestamp){
			var src = (process.env.VUE_APP_SC_TYPE == "test" ? "http://www.dummy.local/unique-symbols-elements/" : this.symbolsUrl) + itemId + ".png";
			if (itemId == "522-arrow") {
				src = require("@/assets/custom-sc-elements/522-arrow.png"); // TEMP...
			}
			if (timestamp) {
				src += "?t=" + timestamp;
			}
			return src;
		},

		getToken: function(service){
			if (!service) {
				service = {};
			}
			if (!this.tokens) {
				this.tokens = {};
			}
			var promise = new Promise(function(resolve, reject){
				if (service) {
					if (this.tokens[service.url]) {
						resolve(this.tokens[service.url]);
					} else {
						var params = {
							url: service.url
						};
						this.getFetchPromise(this.webServicesRoot + "service/get-token", function(json){
							return json;
						}, "POST", params).then(function(result){
							if (service.id) {
								this.tokens[service.id] = result.token;	
							}
							if (service.url) {
								this.tokens[service.url] = result.token;
							}
							resolve(result.token);
						}.bind(this), function(){
							reject();
						});
					}
				} else {
					resolve();
				}
			}.bind(this));
			return promise;
		},

		getPaintedArea: function(data, myMap, useGeometry){
			var values = {
				horizontalPoints: {
					1161: 1.44,
					1162: 1.82,
					1163: 1.82,
					1164: 2.62,
					1165: 2.62,
					1166: 2.62,
					1167: 2.62,
					1168: 2.62,
					1169: 4,
					11611: 2.5,
					11621: 2.5,
					11631: 2.5,
					11641: 3.5,
					11651: 3.5,
					11691: 4,
					1171: 1.98, // ?
					1172: 1.98, // ?
					118: 2.05,
					119: 2, // Taip Justinas rašė...
					120: 6.5,
					121: 2,
					1211: 2.4,
					123: 0.5,
					11231: 1,
					11232: 1.5,
					124: 0.5,
					128: 2,
					129: 2,
					130: 2,
					131: null,
					133: null,
					134: 1.75,
					135: 1.75,
					14: 1,
					15: 1.5,
					16: null,
					17: 1.56,
					1301: 1.17
				}
			};
			var coefficients = {
				horizontalPolylines: {
					11: 1 * 0.12,
					110: 1.33 * 0.12,
					111: 1 * 0.50,
					112: 0.25 * 0.70,
					1131: 0.50 * (data.feature.get(this.widthFieldName) || 3.5),
					1132: 0.407 * 0.50,
					1133: 0.715 * 0.40,
					114: 1 * 0.5,
					12: 1 * 0.25,
					122: 0.50 * 0.25,
					125: 1 * 0.50,
					126: 0.50 * 0.24,
					127: 0.197 * 1.88,
					13: 1 * 0.24,
					14: 1 * 0.12,
					15: 0.33 * 0.12,
					16: 0.66 * 0.12,
					17: 0.50 * 0.12,
					18: 0.33 * 0.25,
					19: 0.50 * 0.12
				},
				horizontalPolygons: {
					1151: 0.35,
					1152: 0.35,
					1153: 0.35,
					132: 0.20,
					1322: 0.18,
					133: null
				}
			};
			var paintedAreaValue,
				layerInfo = myMap.getLayerInfo(data.featureType);
			if (layerInfo && layerInfo.typeIdField) {
				var type = data.feature.get(layerInfo.typeIdField);
				if (type) {
					if (values[data.featureType]) {
						paintedAreaValue = values[data.featureType][type];
					} else if (coefficients[data.featureType]) {
						var coef = coefficients[data.featureType][type];
						if (coef) {
							if (useGeometry) {
								if (data.featureType == "horizontalPolylines") {
									paintedAreaValue = coef * data.feature.getGeometry().getLength();
								} else if (data.featureType == "horizontalPolygons") {
									paintedAreaValue = coef * data.feature.getGeometry().getArea();
								}
							} else {
								if (data.featureType == "horizontalPolylines") {
									paintedAreaValue = coef * (data.feature.get(this.defaultLengthFieldName) || data.feature.get("SHAPE.STLength()"));
								} else if (data.featureType == "horizontalPolygons") {
									paintedAreaValue = coef * (data.feature.get(this.defaultAreaFieldName) || data.feature.get("SHAPE.STArea()"));
								}
							}
						}
					}
				}
			}
			return paintedAreaValue;
		},

		getIcon: function(key){
			var icons = {
				"arrow-up": "mdi-arrow-up-thick",
				"arrow-left": "mdi-arrow-left-thick",
				"arrow-right": "mdi-arrow-right-thick",
				"arrow-top-left": "mdi-arrow-top-left-thick",
				"arrow-top-right": "mdi-arrow-top-right-thick"
			};
			return icons[key] || key;
		},

		drawArrowHead: function(xEnd, yEnd, angle, arrowHeadWidth, arrowHeadHeight, ctx, test){
			var arrowHeadCoordinates = [],
				arrowHeadHalfWidth = arrowHeadWidth / 2;
			arrowHeadCoordinates.push(
				[xEnd + arrowHeadHeight * Math.sin(angle), yEnd + arrowHeadHeight * Math.cos(angle)],
				[xEnd + arrowHeadHalfWidth * Math.sin(angle + 0.5 * Math.PI), yEnd + arrowHeadHalfWidth * Math.cos(angle + 0.5 * Math.PI)],
				[xEnd + arrowHeadHalfWidth * Math.sin(angle - 0.5 * Math.PI), yEnd + arrowHeadHalfWidth * Math.cos(angle - 0.5 * Math.PI)]
			);
			ctx.beginPath();
			arrowHeadCoordinates.forEach(function(arrowHeadCoordinate, i){
				if (i) {
					ctx.lineTo(arrowHeadCoordinate[0], arrowHeadCoordinate[1]);
				} else {
					ctx.moveTo(arrowHeadCoordinate[0], arrowHeadCoordinate[1]);
				}
			}.bind(this));
			ctx.fill();
			if (test) {
				arrowHeadCoordinates.forEach(function(arrowHeadCoordinate, i){
					ctx.save();
					ctx.lineWidth = 1;
					if (i) {
						ctx.strokeStyle = "red";
					} else {
						ctx.strokeStyle = "green";
					}
					ctx.beginPath();
					ctx.arc(arrowHeadCoordinate[0], arrowHeadCoordinate[1], 3, 0, 2 * Math.PI, true);
					ctx.stroke();
					ctx.restore();
				}.bind(this));
			}
		},

		getActiveTaskFeatures: function(activeTask){
			var activeTaskFeatures;
			if (activeTask && activeTask.data) {
				activeTaskFeatures = {};
				var action;
				activeTask.data.forEach(function(item){
					if (item.feature) {
						action = null;
						for (var key in this.taskFeatureActionValues) {
							if (item.feature.get(this.taskFeatureActionFieldName) == this.taskFeatureActionValues[key]) {
								action = key;
							}
						}
						item.action = action;
						if (!activeTaskFeatures[item.featureType]) {
							activeTaskFeatures[item.featureType] = [];
						}
						activeTaskFeatures[item.featureType].push(item);
					}
				}.bind(this));
			}
			return activeTaskFeatures;
		},

		checkIfSkipFeature: function(feature, myMap, layerInfo){
			var skipFeature = false;
			if (myMap && myMap.activeTaskFeaturesGrouped) {
				if (layerInfo && layerInfo.globalIdField) {
					var featureId = feature.get(layerInfo.globalIdField);
					if (featureId) {
						if (myMap.activeTaskFeaturesGrouped[featureId]) {
							skipFeature = true;
						}
					}
				}
			}
			return skipFeature;
		},

		formatLength: function(line){
			var length = line.getLength(),
				output;
			if (length > 100) {
				output = Math.round((length / 1000) * 100) / 100 + " " + "km";
			} else {
				output = Math.round(length * 100) / 100 + " " + "m";
			}
			return output;
		},

		getPrettyName: function(layerName, featureType){
			var layerNames = {
				horizontalPoints: "Horizontaliojo ženklinimo taškiniai objektai",
				horizontalPolylines: "Horizontaliojo ženklinimo linijiniai objektai",
				horizontalPolygons: "Horizontaliojo ženklinimo plotiniai objektai",
				verticalStreetSigns: "Kelio ženklai",
				verticalStreetSignsSupports: "Vertikaliojo ženklinimo tvirtinimo vietos",
				otherPoints: "Taškiniai objektai",
				otherPolylines: "Linijiniai objektai",
				otherPolygons: "Plotiniai objektai",
				userPoints: "Naudotojo taškai"
			};
			return layerNames[featureType] || layerName;
		},

		isActiveFeatureInSingleClickFeatures: function(singleClickFeatures, activeFeature){
			var isActiveFeatureInSingleClickFeatures = false;
			if (singleClickFeatures && activeFeature) {
				singleClickFeatures.some(function(singleClickFeature){
					if (singleClickFeature.layer && (singleClickFeature.layer.layerId == activeFeature.layerId)) {
						var singleClickFeatureId = this.stripGuid(singleClickFeature.get(singleClickFeature.layer.globalIdField) || singleClickFeature.get(singleClickFeature.layer.objectIdField));
						if (singleClickFeatureId == activeFeature.globalId) {
							isActiveFeatureInSingleClickFeatures = true;
							return true;
						}
					}
				}.bind(this));
			}
			return isActiveFeatureInSingleClickFeatures;
		},

		getFeatureType: function(feature){
			var featureType,
				layerIdMeta,
				serviceId = "street-signs";
			if ((feature.type == "v") || (feature.type == "task-related-v")) {
				serviceId = "street-signs-vertical";
			}
			if (["waze"].indexOf(feature.type) != -1) {
				featureType = feature.type;
			} else {
				for (var key in this.layerIds) {
					layerIdMeta = this.layerIds[key];
					if (layerIdMeta[0] == serviceId && layerIdMeta[1] == feature.layerId) {
						featureType = key;
						break;
					}
				}
			}
			return featureType;
		},

		setUnapprovedFromUsers: function(userData){
			// Sprendimas hack'iškas, bet Kaunui norėjosi padaryti greitą sprendimą, kad jie galėtų lengvai keisti mobilios app naudotojų sąrašą...
			if (userData && userData["mobile-users"]) {
				this.unapprovedFromUsers = userData["mobile-users"].split(",");
			}
		}
	}
</script>