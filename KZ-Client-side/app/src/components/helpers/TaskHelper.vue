<script>
	import CommonHelper from "./CommonHelper";
	import EsriJSON from "ol/format/EsriJSON";

	export default {
		getRelatedFeatures: function(activeTask, store){
			activeTask = Object.assign({}, activeTask);
			activeTask.initialData = "loading";
			store.commit("setActiveTask", activeTask);
			this.findRelatedFeatures(activeTask, store).then(function(result){
				if (store.state.activeTask && (store.state.activeTask.globalId == activeTask.globalId)) {
					activeTask = Object.assign({}, activeTask);
					activeTask.initialData = "loaded";
					if (!activeTask.data) {
						activeTask.data = [];
					}
					var esriJsonFormat = new EsriJSON();
					result.forEach(function(item){
						var layerId,
							featureType;
						if (CommonHelper.layerIds.tasksRelated) {
							for (var key in CommonHelper.layerIds.tasksRelated) {
								layerId = CommonHelper.layerIds.tasksRelated[key];
								if (layerId == item.layerId) {
									featureType = key;
								}
							}
						}
						if (featureType) {
							activeTask.data.push({
								feature: esriJsonFormat.readFeature(item),
								featureType: featureType
							});
						}
					});
					store.commit("setActiveTask", activeTask);
				}
			}.bind(this), function(){
				if (store.state.activeTask && (store.state.activeTask.globalId == activeTask.globalId)) {
					activeTask = Object.assign({}, activeTask);
					activeTask.initialData = "error";
					store.commit("setActiveTask", activeTask);
				}
			}.bind(this));
		},

		findRelatedFeatures: function(activeTask, store){
			var promise = new Promise(function(resolve, reject){
				var tasksServiceUrl = store.getters.getServiceUrl("tasks");
				if (tasksServiceUrl) {
					if (activeTask.feature) {
						tasksServiceUrl = CommonHelper.prependProxyIfNeeded(tasksServiceUrl.replace("FeatureServer", "MapServer") + "/find");
						var layerIds = [],
							layerId;
						if (CommonHelper.layerIds.tasksRelated) {
							for (var key in CommonHelper.layerIds.tasksRelated) {
								layerId = CommonHelper.layerIds.tasksRelated[key];
								if (key != "tasks") {
									layerIds.push(layerId);
								}
							}
						}
						var params = {
							f: "json",
							outFields: "*",
							returnGeometry: true,
							searchText: activeTask.feature.get("GlobalID"), // FIXME! Ne hardcode'inti reikšmės...
							searchFields: [CommonHelper.taskFeatureTaskGUIDFieldName].join(","),
							layers: layerIds,
							contains: false,
							returnFieldName: true,
							returnUnformattedValues: true
						};
						CommonHelper.getFetchPromise(tasksServiceUrl, function(json){
							if (json.results) {
								return json.results;
							}
						}.bind(this), "POST", params).then(function(result){
							if (result) {
								resolve(result);
							} else {
								reject();
							}
						}.bind(this), function(){
							reject();
						});
					} else {
						reject();
					}
				} else {
					reject();
				}
			}.bind(this));
			return promise;
		},

		saveFeature: function(data, activeTask, store){
			var promise = new Promise(function(resolve){
				if (store.state.activeTask) {
					activeTask = Object.assign({}, activeTask);
					if (!activeTask.data) {
						activeTask.data = [];
					} else {
						// Gi gali būti pvz. toks atvejis, kad pastūmėme prieš tai sukurtą objektą ar pan... Senas veiksmas turi anuliuotis :)
					}
					var newActiveTaskData = [],
						substituteExists;
					activeTask.data.forEach(function(activeTaskDataItem){
						substituteExists = false;
						data.forEach(function(dataItem){
							if (activeTaskDataItem.feature.get("GlobalID") == dataItem.feature.get("GlobalID")) { // FIXME: ne'hardcode'inti...
								substituteExists = true;
							}
						});
						if (!substituteExists) {
							newActiveTaskData.push(activeTaskDataItem);
						}
					});
					activeTask.data = newActiveTaskData.concat(data);
					store.commit("setActiveTask", activeTask);
				}
				resolve();
			}.bind(this));
			return promise;
		},

		destroyFeature: function(feature, featureType, activeTask, store){
			var data = [{
				feature: feature,
				featureType: featureType
			}];
			var promise = this.saveFeature(data, activeTask, store);
			return promise;
		},

		onFeatureDestroy: function(currentActiveTask, store, feature, router, vBus){
			var fullRefresh = false;
			if (fullRefresh) {
				if (currentActiveTask) {
					store.commit("setActiveTask", {
						globalId: currentActiveTask.globalId
					});
				}
			} else {
				if (currentActiveTask) {
					var activeTask = Object.assign({}, currentActiveTask);
					if (activeTask.data) {
						var newActiveTaskData = [];
						activeTask.data.forEach(function(activeTaskDataItem){
							if (activeTaskDataItem.feature.get("GlobalID") == feature.get("GlobalID")) {
								// ...
							} else {
								newActiveTaskData.push(activeTaskDataItem);
							}
						});
						activeTask.data = newActiveTaskData;
						store.commit("setActiveTask", activeTask);
						console.log("TIKRINTI...", store.state.activeFeature, feature.get("GlobalID")); // TODO...
						CommonHelper.routeTo({ // BIG FIXME! Route'inti ne visais atvejais, o tik tuo atveju, jei pašalintas objektas ir yra tas aktyvus!!!
							router: router,
							vBus: vBus
						});
					}
				}
			}
		},

		getTaskData: function(id, returnAttachments, vBus){
			var promise = new Promise(function(resolve, reject){
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "tasks/get-task-data-g?id=" + id + "&returnAttachments=" + Boolean(returnAttachments), function(json){
					return json;
				}).then(function(result){
					if (result) {
						if (vBus) {
							vBus.$emit("update-tasks-list-item", {feature: result});
						}
						resolve(result);
					} else {
						reject();
					}
				}.bind(this), function(){
					reject();
				}.bind(this));
			}.bind(this));
			return promise;
		},

		getPrettyValue: function(feature, attr, fields){
			var val = feature.attributes[attr];
			if (fields) {
				var field = fields[attr];
				if (field) {
					if (field.domain) {
						var codedValues = field.domain.codedValues;
						if (codedValues) {
							codedValues.some(function(codedValue){
								if (codedValue.code == val) {
									val = codedValue.name;
									return true;
								}
							});
						}
					}
				}
			}
			return val;
		},

		getApproversList: function(){
			var promise = new Promise(function(resolve, reject){
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "users-data/get-approvers", function(json){
					return json;
				}).then(function(result){
					if (result) {
						resolve(result);
					} else {
						reject();
					}
				}.bind(this), function(){
					reject();
				}.bind(this));
			}.bind(this));
			return promise;
		},

		notifyAboutTaskChangeToEinpix: function(featureProperties, actionType){
			var doNotify;
			if (featureProperties) {
				var globalId = featureProperties["GlobalID"];
				if (globalId) {
					globalId = CommonHelper.stripGuid(globalId);
					// Hmmm... Gal nelabai galima čia pasikliauti užduoties statusu?.. Ar galima? O jei kažkas jį pakeitė kitame tab'e ar pan?.. Gal geriau serverio pusėje tą statusą tikrinti??... FIXME!!!
					//if ((featureProperties["Statusas"] != "0") || force) {
						doNotify = true;
					//}
				} else {
					console.warn("notifyAboutTaskChangeToEinpix failed...");
				}
			} else {
				console.warn("notifyAboutTaskChangeToEinpix failed...");
			}
			var promise = new Promise(function(resolve, reject){
				if (doNotify) {
					var params = {
						enterpriseId: featureProperties["Imone"],
						id: globalId,
						attachment: true,
						actionType: actionType || "update"
					};
					CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "tasks/notify-about-change-to-tasks-system", function(json){
						return json;
					}, "POST", params).then(function(result){
						if (result) {
							resolve(result);
						} else {
							reject();
						}
					}.bind(this), function(){
						reject();
					}.bind(this));
				} else {
					reject("Do not notify on purpose");
				}
			}.bind(this));
			return promise;
		},

		getComments: function(featureProperties){
			var promise = new Promise(function(resolve, reject){
				if (featureProperties) {
					var globalId = featureProperties["GlobalID"];
					if (globalId) {
						globalId = CommonHelper.stripGuid(globalId);
						var params = {
							id: globalId
						};
						CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "tasks/get-task-comments-from-einpix", function(json){
							return json;
						}, "POST", params).then(function(result){
							if (result && result.comments) {
								resolve(result);
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
		},

		addNewComment: function(featureProperties, comment, attachmentIds, token){
			var promise = new Promise(function(resolve, reject){
				if (featureProperties && (comment || (attachmentIds && attachmentIds.length))) {
					var globalId = featureProperties["GlobalID"];
					if (globalId) {
						globalId = CommonHelper.stripGuid(globalId);
						var params = {
							id: globalId,
							comment: comment,
							token: token
						};
						if (attachmentIds) {
							params.attachmentIds = attachmentIds.join(",");
						}
						CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "tasks/send-task-comment-to-einpix", function(json){
							return json;
						}, "POST", params).then(function(result){
							if (result) {
								resolve(result);
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
		},

		uploadTaskCommentAttachment: function(attachment, token){
			var promise = new Promise(function(resolve, reject){
				if (attachment && attachment.file) {
					var params = {
						token: token
					};
					var files = [attachment.file];
					CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "tasks/upload-task-comment-attachment-to-einpix", function(json){
						return json;
					}, "POST", params, files).then(function(result){
						if (result) {
							resolve(result);
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

		approveTask: function(feature){
			var promise = new Promise(function(resolve, reject){
				if (feature) {
					var taskGlobalId = feature.get("GlobalID");
					if (taskGlobalId) {
						var params = {
							id: CommonHelper.stripGuid(taskGlobalId),
							status: "approved"
						}
						CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "tasks/approve-or-reject-task", function(json){
							return json;
						}, "POST", params).then(function(result){
							if (result && (result.status == "Ok")) {
								resolve(result);
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
		},

		rejectTask: function(feature, reason){
			var promise = new Promise(function(resolve, reject){
				if (feature) {
					var taskGlobalId = feature.get("GlobalID");
					if (taskGlobalId) {
						var params = {
							id: CommonHelper.stripGuid(taskGlobalId),
							status: "reject"
						}
						if (reason) {
							params.reason = reason;
						}
						CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "tasks/approve-or-reject-task", function(json){
							return json;
						}, "POST", params).then(function(result){
							if (result && (result.status == "Ok")) {
								resolve(result);
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
		},

		getTasksList: function(){
			var promise = new Promise(function(resolve, reject){
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "tasks/get-tasks-list-g", function(json){
					return json;
				}).then(function(result){
					resolve(result);
				}.bind(this), function(){
					reject();
				}.bind(this));
			}.bind(this));
			return promise;
		},

		logTaskViewAction: function(taskGlobalId){
			var promise = new Promise(function(resolve, reject){
				if (taskGlobalId) {
					var params = {
						id: taskGlobalId
					}
					CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "tasks/log-task-view-action", function(json){
						return json;
					}, "POST", params).then(function(){
						resolve();
					}.bind(this), function(){
						reject();
					}.bind(this));
				} else {
					reject();
				}
			}.bind(this));
			return promise;
		}
	}
</script>