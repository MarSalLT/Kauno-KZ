<template>
	<v-btn
		color="primary"
		title="Perkelti į užduotį"
		class="pa-0 mr-2 ma-1"
		fab
		x-small
		:loading="saveInProgress"
		v-on:click.stop="transferAttachmentToFeature"
		v-if="feature && feature.get('Objekto_GUID')"
	>
		<v-icon dark>mdi-transfer</v-icon>
	</v-btn>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";
	import PhotoHelper from "./feature-photos-manager/PhotoHelper";
	import PhotosHelper from "./feature-photos-manager/PhotosHelper";

	export default {
		data: function(){
			var data = {
				saveInProgress: false
			};
			return data;
		},

		props: {
			attachment: Object,
			feature: Object,
			getAttachmentUrl: Function
		},

		methods: {
			transferAttachmentToFeature: function(){
				if (this.attachment && this.attachment.url && this.attachment.original_extension && this.feature && this.getAttachmentUrl) {
					this.$vBus.$emit("confirm", {
						title: "Ar tikrai perkelti šį grafinį priedą į susijusio KŽ objekto priedų masyvą?",
						message: "Ar tikrai perkelti šį grafinį priedą į susijusio KŽ objekto priedų masyvą?",
						positiveActionTitle: "Perkelti",
						negativeActionTitle: "Atšaukti",
						positive: function(){
							var src = this.getAttachmentUrl(this.attachment.url),
								keywords = ""; // "from-einpix"
							// Realiai dabar turime tik GlobalID... Reikia gauti objekto ID... Ir objekto serviso ID, objekto sluoksnio ID...
							this.saveInProgress = true;
							this.doTransferAttachmentToFeature(src, keywords).then(function(){
								this.$vBus.$emit("show-message", {
									type: "success",
									message: "Grafinis objektas perkeltas sėmingai...",
									timeout: 3000
								});
								this.saveInProgress = false;
							}.bind(this), function(){
								this.$vBus.$emit("show-message", {
									type: "warning",
									message: "Deja, grafinio objekto perkelti nepavyko..."
								});
								this.saveInProgress = false;
							}.bind(this));
						}.bind(this)
					});
				}
			},
			doTransferAttachmentToFeature: function(src, keywords){
				var promise = new Promise(function(resolve, reject){
					var streetSignsServiceUrl = this.$store.getters.getServiceUrl("street-signs"),
						streetSignsVerticalServiceUrl = this.$store.getters.getServiceUrl("street-signs-vertical"),
						myMap = this.$store.state.myMap;
					if ((streetSignsServiceUrl == streetSignsVerticalServiceUrl) && myMap) {
						var findServiceUrl = CommonHelper.prependProxyIfNeeded(streetSignsServiceUrl.replace("/FeatureServer", "/MapServer") + "/find"),
							params = {
								f: "json",
								outFields: "*",
								searchText: this.feature.get("Objekto_GUID"),
								searchFields: "GlobalID",
								layers: [
									myMap.getLayerId("verticalStreetSignsSupports"),
									myMap.getLayerId("verticalStreetSigns"),
									myMap.getLayerId("horizontalPoints"),
									myMap.getLayerId("horizontalPolylines"),
									myMap.getLayerId("horizontalPolygons"),
									myMap.getLayerId("otherPoints"),
									myMap.getLayerId("otherPolylines"),
									myMap.getLayerId("otherPolygons")
								],
								contains: false,
								returnFieldName: true,
								returnUnformattedValues: true
							};
						CommonHelper.getFetchPromise(findServiceUrl, function(json){
							return json;
						}.bind(this), "POST", params).then(function(result){
							// Dabar perkėlimas vykta, tik jei objektas yra vienas?..
							if (result && result.results) {
								if (result.results.length == 1) {
									var r = result.results[0];
									if (r && r.attributes) {
										var callback = function(objectId, layerId){
											PhotoHelper.getFile(src, {name: "from-einpix-comment." + this.attachment.original_extension}).then(function(file){
												var featureType;
												if (CommonHelper.layerIds) {
													["verticalStreetSignsSupports", "verticalStreetSigns", "horizontalPoints", "horizontalPolylines", "horizontalPolygons", "otherPoints", "otherPolylines", "otherPolygons"].forEach(function(key){
														if (CommonHelper.layerIds[key]) {
															if (layerId == CommonHelper.layerIds[key][1]) {
																featureType = key;
															}
														}
													});
												}
												if (featureType) {
													PhotosHelper.addPhoto(objectId, featureType, {file: file}, keywords).then(function(response){ // FIXME... Ne'hardcode'inti "OBJECTID"...
														if (response && response.addAttachmentResult && response.addAttachmentResult.success) {
															resolve();
														} else {
															reject();
														}
													}.bind(this), function(){
														reject();
													}.bind(this));
												} else {
													reject();
												}
											}.bind(this), function(err){
												reject();
												console.error(err);
											}.bind(this));
										}.bind(this);
										if (r.layerId == myMap.getLayerId("verticalStreetSigns")) {
											myMap.findFeature({
												globalId: r.attributes["GUID"],
												layerId: myMap.getLayerId("verticalStreetSignsSupports"),
											}).then(function(e){
												if (e.feature && e.feature.attributes) {
													callback(e.feature.attributes["OBJECTID"], myMap.getLayerId("verticalStreetSignsSupports"));
												} else {
													reject();
												}
											}.bind(this), function(){
												reject();
											}.bind(this));
										} else {
											callback(r.attributes["OBJECTID"], r.layerId);
										}
									} else {
										reject();
									}
								} else {
									reject(); // TODO: gal reikia specifinio pranešimo?..
								}
							} else {
								reject();
							}
						}.bind(this), function(){
							reject();
						});
					}
				}.bind(this));
				return promise;
			}
		}
	}
</script>

<style scoped>
	div {
		background-color: #e6f6ff;
	}
</style>