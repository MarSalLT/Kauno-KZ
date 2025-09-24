<script>
	import CommonHelper from "./CommonHelper";

	var webServicesRoot = process.env.VUE_APP_SC_TYPE == "test" ? (process.env.VUE_APP_ROOT + "-dummy/web-services/sc/") : (CommonHelper.webServicesRoot + "sc/");

	export default {
		maxSymbolHeight: 65,
		maxSymbolElementHeight: 145,
		maxIconHeight: 50,
		webServicesRoot: webServicesRoot,
		getDataWebService: webServicesRoot + (process.env.VUE_APP_SC_TYPE == "test" ? "get-data.php" : "get-data"),

		getImageDimensions: function(item, category, maxIconHeight){
			var maxItemHeight = this.maxSymbolHeight;
			if (category == "elements") {
				maxItemHeight = this.maxSymbolElementHeight;
			}
			if (!maxIconHeight) {
				maxIconHeight = this.maxIconHeight;
			}
			var height = item["img_height"] / maxItemHeight * maxIconHeight,
				width = item["img_width"] * height / item["img_height"];
			var dimensions = {
				width: width,
				height: height
			};
			return dimensions;
		},

		getUniqueSymbols: function(query, withCount){
			var promise = new Promise(function(resolve, reject){
				var url = this.getDataWebService + "?category=symbols";
				if (query && query.type) {
					url += "&type=" + query.type;
				}
				if (withCount) {
					url += "&withCount=true";
				}
				CommonHelper.getFetchPromise(url).then(function(result){
					if (result) {
						resolve(result);
					} else {
						reject();
					}
				}, function(){
					reject();
				});
			}.bind(this));
			return promise;
		},

		getUniqueSymbolData: function(id){
			var promise = new Promise(function(resolve, reject){
				if (id) {
					var url = this.getDataWebService + "?category=symbols&id=" + id;
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

		getUniqueSymbolsElements: function(query){
			var promise = new Promise(function(resolve, reject){
				var url = this.getDataWebService + "?category=elements";
				if (query && query.type) {
					url += "&type=" + query.type;
					if (query.subtype) {
						url += "&subtype=" + query.subtype;
					}
				}
				CommonHelper.getFetchPromise(url).then(function(result){
					if (result) {
						resolve(result);
					} else {
						reject();
					}
				}, function(){
					reject();
				});
			}.bind(this));
			return promise;
		},

		getUniqueSymbolElementData: function(id){
			var promise = new Promise(function(resolve, reject){
				if (id) {
					var url = this.getDataWebService + "?category=elements&id=" + id;
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

		saveData: function(params){
			var promise = new Promise(function(resolve, reject){
				var url = webServicesRoot + (process.env.VUE_APP_SC_TYPE == "test" ? "save-data.php" : "save-data");
				CommonHelper.getFetchPromise(url, null, "POST", params).then(function(result){
					if (result && result.success) {
						resolve(result);
					} else {
						reject();
					}
				}, function(){
					reject();
				});
			}.bind(this));
			return promise;
		},

		deleteItem: function(params){
			var promise = new Promise(function(resolve, reject){
				var url = webServicesRoot + (process.env.VUE_APP_SC_TYPE == "test" ? "delete-item.php" : "delete-item");
				CommonHelper.getFetchPromise(url, null, "POST", params).then(function(result){
					if (result && result.success) {
						resolve(result);
					} else {
						reject();
					}
				}, function(){
					reject();
				});
			}.bind(this));
			return promise;
		},

		findFeature: function(e, store){
			var promise = new Promise(function(resolve, reject){
				var layerIdMeta = CommonHelper.layerIds["verticalStreetSigns"];
				if (layerIdMeta) {
					var serviceUrl = store.getters.getServiceUrl(layerIdMeta[0]);
					if (serviceUrl) {
						serviceUrl = CommonHelper.prependProxyIfNeeded(serviceUrl) + "/" + layerIdMeta[1] + "/query";
						var params = {
							f: "json",
							outFields: "*",
							returnGeometry: false,
							where: "GlobalID = '" + e.globalId + "'"
						};
						CommonHelper.getFetchPromise(serviceUrl, function(json){
							var feature;
							if (json.features && json.features.length == 1) {
								feature = json.features[0];
							}
							return feature;
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

		getRawElementData: function(id, small){
			var allData = {
				"circle-test": {
					rawElementData: true,
					symbol: {
						altSrc: require("@/assets/custom-sc-elements/circle-test.png"),
						width: 170 / (small ? 6 : 3),
						height: 145 / (small ? 6 : 3),
						type: "sc-element-custom"
					},
					id: "circle-test"
				},
				"522-arrow": {
					rawElementData: true,
					symbol: {
						altSrc: require("@/assets/custom-sc-elements/522-arrow.png"),
						width: 68 / (small ? 6 : 3),
						height: 145 / (small ? 6 : 3),
						type: "sc-element-custom",
						fromRight: true
					},
					id: "522-arrow"
				}
			};
			var data = allData[id] || id;
			return data;
		},

		getFeaturesUsingUniqueSymbol: function(id){
			var promise = new Promise(function(resolve, reject){
				if (id) {
					var url = this.getDataWebService.replace("get-data", "get-data-by-symbol-id") + "?id=" + CommonHelper.stripGuid(id);
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
	}
</script>