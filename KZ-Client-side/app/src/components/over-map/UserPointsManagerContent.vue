<template>
	<OverMapButtonContent
		type="user-points-manager"
		:title="title"
		:btn="btn"
		:onOpen="onOpen"
		ref="wrapper"
	>
		<template v-slot>
			<div class="body-2">
				<template v-if="loading">
					<v-progress-circular
						indeterminate
						color="primary"
						:size="30"
						width="2"
					></v-progress-circular>
				</template>
				<template v-else>
					<v-data-table
						:headers="headers"
						:items="features"
						class="body-2 rounded-0 body-2 simple-data-table search-content-table"
						must-sort
						:options="{sortBy: ['id'], sortDesc: [false]}"
						v-if="features"
					>
						<template v-slot:item="{item}">
							<tr
								:class="['clickable', activeFeatureGlobalId == item.globalId ? 'primary lighten-4' : null]"
								v-on:click="onRowClick(item)"
							>
								<template v-for="(headerItem, j) in headers">
									<td :class="['px-2 py-1 caption']" :key="j">{{item[headerItem.value]}}</td>
								</template>
							</tr>
						</template>
					</v-data-table>
					<v-divider class="pt-4"></v-divider>
					<div>
						<v-btn
							color="blue darken-1"
							text
							v-on:click="onOpen"
							outlined
							small
						>
							<v-icon left size="18">mdi-reload</v-icon>
							Perkrauti
						</v-btn>
					</div>
				</template>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import MapHelper from "../helpers/MapHelper";
	import OverMapButtonContent from "./OverMapButtonContent";

	export default {
		data: function(){
			var data = {
				title: null,
				btn: null,
				loading: false,
				features: null,
				headers: [{
					value: "id",
					text: "ID"
				},{
					value: "tipas",
					text: "Tipas"
				},{
					value: "pastaba",
					text: "Pastaba"
				}],
				activeFeatureGlobalId: null
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			activeFeature: {
				get: function(){
					return this.$store.state.activeFeature;
				}
			}
		},

		components: {
			OverMapButtonContent
		},

		created: function(){
			this.$vBus.$on("show-or-hide-user-points-manager", this.showOrHide);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-or-hide-user-points-manager", this.showOrHide);
		},

		methods: {
			showOrHide: function(e){
				this.title = e.title;
				this.btn = e.btn;
				this.$refs.wrapper.toggle(this.btn.$el.offsetLeft);
			},
			onOpen: function(){
				this.loading = true;
				this.features = null;
				this.getList().then(function(result){
					this.loading = false;
					var features = [];
					if (result.features) {
						var layerInfo = this.myMap.getLayerInfo("userPoints");
						result.features.forEach(function(feature){
							if (layerInfo) {
								feature.attributes.id = feature.attributes[layerInfo.objectIdField];
								feature.attributes.globalId = feature.attributes[layerInfo.globalIdField];
								feature.attributes.serviceId = result.serviceId;
								feature.attributes.layerId = result.layerId;
							}
							features.push(feature.attributes);
						});
					}
					this.features = features;
				}.bind(this), function(){
					this.loading = false;
				}.bind(this));
			},
			getList: function(){
				var promise = new Promise(function(resolve, reject){
					var service = MapHelper.userDataService;
					if (service) {
						var params = {
							where: "1=1",
							f: "json",
							outFields: "*",
							returnGeometry: false
						};
						if (service.showOnlyUserFeatures) {
							if (this.myMap && this.myMap.userData) {
								params.where = "editor_app='" + this.myMap.userData.username + "'";
							}
						}
						var layerId = service.showLayers[0];
						CommonHelper.getFetchPromise(CommonHelper.prependProxyIfNeeded(service.url + "/" + layerId + "/query"), function(json){
							json.serviceId = service.id;
							json.layerId = layerId;
							resolve(json);
						}, "POST", params).then(function(result){
							resolve(result);
						}.bind(this), function(){
							reject();
						}.bind(this));
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			},
			onRowClick: function(item){
				CommonHelper.routeTo({
					router: this.$router,
					vBus: this.$vBus,
					featureData: {
						globalId: item.globalId,
						serviceId: item.serviceId,
						layerId: item.layerId
					}
				});
			}
		},

		watch: {
			activeFeature: {
				immediate: true,
				handler: function(activeFeature){
					if (activeFeature && activeFeature.globalId) {
						// Kažin ar yra tikimybė, kad tokie pat GlobalID būtų skirtinguose sluoksniuose, t. y. objektai su tokiu pačiu GlobalID skirtinguose sluoksniuose?
						// Jei taip, šita logika nėra `bulletproof`...
						this.activeFeatureGlobalId = "{" + activeFeature.globalId + "}";
					} else {
						this.activeFeatureGlobalId = null;
					}
				}
			}
		}
	}
</script>

<style scoped>
	.clickable {
		cursor: pointer;
	}
	.v-data-table td {
		height: auto !important;
	}
</style>