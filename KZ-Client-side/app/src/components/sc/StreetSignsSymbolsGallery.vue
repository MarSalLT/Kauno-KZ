<template>
	<div class="mr-7">
		<template v-if="list">
			<template v-if="list == 'error'">
				<v-alert
					dense
					type="error"
					class="ma-0 d-inline-block"
				>
					Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
				</v-alert>
			</template>
			<template v-else>
				<template v-if="list.length">
					<v-expansion-panels multiple>
						<template v-for="(group, i) in list">
							<v-expansion-panel :key="group.key">
								<v-expansion-panel-header>
									<span class="body-2 expansion-panel-header-item">
										{{group.key}}
									</span>
									<ESRISymbol
										:descr="group.symbol"
										small
										class="ml-2 expansion-panel-header-item"
									/>
									<span class="body-2 ml-2 expansion-panel-header-item">
										({{group.items.length}})
									</span>
								</v-expansion-panel-header>
								<v-expansion-panel-content>
									<div class="d-flex flex-wrap">
										<template v-for="(item, j) in group.items">
											<StreetSignSymbolCard
												:item="item"
												:key="i + '-' + j"
												:listKey="listKey"
												:onItemDelete="onItemDelete"
												class="mt-2 mr-2 mb-2"
											/>
										</template>
									</div>
								</v-expansion-panel-content>
							</v-expansion-panel>
						</template>
					</v-expansion-panels>
				</template>
				<template v-else>
					<div>Galerija tuščia...</div>
				</template>
				<div class="fab-buttons d-flex flex-column">
					<v-btn
						fab
						color="success"
						v-on:click="createNewItem"
						class="mb-1"
					>
						<v-icon
							title="Kurti naują simbolį"
						>
							mdi-plus
						</v-icon>
					</v-btn>
					<v-btn
						fab
						color="primary"
						v-on:click="getItems"
					>
						<v-icon
							title="Perkrauti sąrašą"
						>
							mdi-refresh
						</v-icon>
					</v-btn>
				</div>
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
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import ESRISymbol from "./../ESRISymbol";
	import StreetSignSymbolCard from "./StreetSignSymbolCard";
	import StreetSignsSymbolsManagementHelper from "../helpers/StreetSignsSymbolsManagementHelper";

	export default {
		data: function(){
			var data = {
				list: null,
				itemsUpdated: 0,
				listKey: null
			};
			return data;
		},

		mounted: function(){
			this.getVerticalStreetSignsLayerInfo().then(function(verticalStreetSignsLayerInfo){
				var symbols = {};
				if (verticalStreetSignsLayerInfo) {
					if (verticalStreetSignsLayerInfo.types && verticalStreetSignsLayerInfo.types.length) {
						if (verticalStreetSignsLayerInfo.drawingInfo) {
							var renderer = verticalStreetSignsLayerInfo.drawingInfo.renderer;
							if (renderer.type == "uniqueValue") {
								renderer.uniqueValueInfos.forEach(function(uniqueValueInfo){
									symbols[uniqueValueInfo.value] = uniqueValueInfo.symbol;
								});
							}
						}
					}
				}
				this.symbols = symbols;
				this.getItems();
			}.bind(this));
		},

		created: function(){
			this.$vBus.$on("sc-gallery-item-updated", this.onItemUpdate);
		},

		beforeDestroy: function(){
			this.$vBus.$off("sc-gallery-item-updated", this.onItemUpdate);
		},

		activated: function(){
			if (this.itemsUpdated) {
				this.getItems();
			}
		},

		components: {
			ESRISymbol,
			StreetSignSymbolCard
		},

		methods: {
			getVerticalStreetSignsLayerInfo: function(){
				var promise = new Promise(function(resolve){
					var layerIdMeta = CommonHelper.layerIds["verticalStreetSigns"];
					if (layerIdMeta) {
						var serviceUrl = this.$store.getters.getServiceUrl(layerIdMeta[0]);
						if (serviceUrl) {
							serviceUrl = CommonHelper.prependProxyIfNeeded(serviceUrl) + "/" + layerIdMeta[1] + "?f=json";
							CommonHelper.getFetchPromise(serviceUrl, function(json){
								return json;
							}, "GET").then(function(result){
								resolve(result);
							}.bind(this), function(){
								resolve();
							}.bind(this));
						} else {
							resolve();
						}
					} else {
						resolve();
					}
				}.bind(this));
				return promise;
			},
			getItems: function(){
				this.list = null;
				StreetSignsSymbolsManagementHelper.getUniqueSymbols(this.$route.query, true).then(function(list){
					this.itemsUpdated = 0;
					var listGrouped = {},
						symbol;
					list.forEach(function(item){
						item.src = CommonHelper.getUniqueSymbolSrc(item.id);
						if (!listGrouped[item.type]) {
							if (this.symbols) {
								symbol = this.symbols[item.type];
							}
							listGrouped[item.type] = {
								key: item.type,
								items: [],
								symbol: symbol
							};
						}
						listGrouped[item.type]["items"].push(item);
					}.bind(this));
					list = [];
					for (var key in listGrouped) {
						list.push(listGrouped[key]);
					}
					this.list = list;
					this.listKey = Date.now();
				}.bind(this), function(){
					this.list = "error";
				}.bind(this));
			},
			onItemUpdate: function(category){
				if (category == "symbols") {
					this.itemsUpdated += 1;
				}
			},
			createNewItem: function(){
				this.$router.push({
					path: "/sc/create"
				});
			},
			onItemDelete: function(id){
				if (this.list) {
					this.list.forEach(function(group){
						if (group.items) {
							group.items.forEach(function(item, index){
								if (item.id == id) {
									group.items.splice(index, 1);
								}
							});
						}
					});
				}
			}
		}
	};
</script>

<style scoped>
	.fab-buttons {
		/* position: absolute; */
		position: fixed;
		right: 0.5rem;
		bottom: 0.5rem;
		z-index: 1;
	}
	.expansion-panel-header-item {
		flex: none !important;
	}
</style>