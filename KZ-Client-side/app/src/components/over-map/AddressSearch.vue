<template>
	<v-sheet
		elevation="1"
		class="d-flex align-center px-2 py-1"
	>
		<v-menu
			v-model="listOpen"
			offset-y
			max-height="200"
		>
			<template v-slot:activator="{on}">
				<v-text-field
					slot="activator"
					placeholder="Vietos paieška"
					dense
					hide-details
					:loading="loading"
					v-model="searchValue"
					ref="field"
					v-on="isIE ? undefined : on"
					autocomplete="off"
					class="ma-0"
				>
					<template v-slot:append>
						<v-btn
							icon
							height="20"
							width="20"
							:class="['mt-1', searchValue ? null : 'invisible']"
							v-on:click="clear"
						>
							<v-icon title="Valyti" small>mdi-close-circle</v-icon>
						</v-btn>
					</template>
				</v-text-field>
			</template>
			<v-card v-if="!loading && items">
				<template v-if="items == 'no-results'">
					<div class="body-2 pa-2">Rezultatų nėra...</div>
				</template>
				<template v-else-if="items == 'error'">
					<div class="body-2 pa-2">Atsiprašome, įvyko nenumatyta klaida...</div>
				</template>
				<template v-else>
					<v-list dense>
						<v-list-item-group v-model="selected">
							<v-list-item
								v-for="(item, index) in items"
								:key="index"
							>
								<v-list-item-content>
									<v-list-item-title
										v-html="item.text"
										class="pa-0"
									></v-list-item-title>
								</v-list-item-content>
							</v-list-item>
						</v-list-item-group>
					</v-list>
				</template>
			</v-card>
		</v-menu>
	</v-sheet>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import Feature from "ol/Feature";
	import Point from "ol/geom/Point";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import {Icon, Style} from "ol/style";

	export default {
		data: function(){
			var data = {
				searchValue: null,
				items: null,
				selected: null,
				loading: false,
				listOpen: false,
				isIE: Boolean(window.document.documentMode) // https://stackoverflow.com/questions/19999388/check-if-user-is-using-ie
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		methods: {
			clear: function(){
				this.searchValue = this.items = this.selected = null;
				if (this.$refs.field) {
					this.$refs.field.blur();
				}
				if (this.myMap.addressObjectsLayer) {
					this.myMap.addressObjectsLayer.getSource().clear(true);
				}
			},
			deselect: function(){
				this.selected = null;
			},
			executeSearch: function(){
				this.deselect();
				this.loading = false;
				var searchValue = this.searchValue;
				if (searchValue && searchValue.length > 2) {
					this.rawSearch(searchValue);
				} else {
					this.getSearchId();
					this.items = null;
				}
			},
			getAddress: function(item){
				var promise = new Promise(function(resolve, reject){
					var url = CommonHelper.addressSearchUrl;
					var params = {
						magicKey: item.magicKey,
						f: "json",
						outSR: JSON.stringify({
							latestWkid: 3346,
							wkid: 2600
						}),
						SingleLine: item.text
					};
					CommonHelper.getFetchPromise(url, function(json){
						if (json.candidates) {
							var items = [];
							json.candidates.forEach(function(candidate){
								items.push(candidate);
							}.bind(this));
							return items;
						}
					}.bind(this), "POST", params).then(function(result){
						resolve(result);
					}, function(){
						reject();
					});
				}.bind(this));
				return promise;
			},
			getAddressSuggestions: function(searchValue){
				var promise = new Promise(function(resolve, reject){
					var url = CommonHelper.addressSuggestionsUrl;
					var params = {
						text: searchValue,
						f: "json",
						maxSuggestions: 20,
						location: JSON.stringify({
							spatialReference: {
								wkid: 3346
							},
							x: 581205.6153498844,
							y: 6064062.251849884
						})
					};
					CommonHelper.getFetchPromise(url, function(json){
						if (json.suggestions) {
							var items = [];
							json.suggestions.forEach(function(suggestion){
								items.push(suggestion);
							}.bind(this));
							return items;
						}
					}.bind(this), "POST", params).then(function(result){
						resolve(result);
					}, function(){
						reject();
					});
				}.bind(this));
				return promise;
			},
			getSearchId: function(){
				if (!this.searchId) {
					this.searchId = 0;
				}
				this.searchId += 1;
				return this.searchId;
			},
			rawSearch: function(searchValue){
				var point = CommonHelper.getPointFromString(searchValue);
				if (point) {
					this.items = [{
						text: point.text,
						point: point
					}];
				} else {
					this.loading = true;
					var searchId = this.getSearchId();
					setTimeout(function(){
						if (searchId == this.searchId) {
							this.getAddressSuggestions(searchValue).then(function(items){
								if (searchId == this.searchId) {
									if (items.length) {
										this.items = items;
									} else {
										this.items = "no-results";
									}
									this.loading = false;
									this.listOpen = true;
								}
							}.bind(this), function(){
								if (searchId == this.searchId) {
									this.items = "error";
									this.loading = false;
									this.listOpen = true;
								}
							}.bind(this));
						}
					}.bind(this), 500);
				}
			}
		},

		watch: {
			searchValue: {
				immediate: false,
				handler: function(){
					if (!this.skipSearch) {
						this.executeSearch();
					}
					this.skipSearch = false;
				}
			},
			selected: {
				immediate: false,
				handler: function(selected){
					if (this.$refs.field) {
						this.$refs.field.blur();
					}
					if (!selected && selected != 0) {
						if (this.myMap.addressObjectsLayer) {
							this.myMap.addressObjectsLayer.getSource().clear(true);
						}
					} else {
						if (this.items) {
							var item = this.items[selected];
							this.searchValue = item.text;
							this.skipSearch = true;
							this.loading = true;
							var def;
							if (item.point) {
								if (item.point.wkid != 3346) {
									var p = new Point([item.point.x, item.point.y]).transform("EPSG:" + item.point.wkid, "EPSG:3346"),
										c = p.getCoordinates();
									item.point.x = c[0];
									item.point.y = c[1];
								}
								def = new Promise(function(resolve){
									resolve([{
										location: item.point
									}]);
								});
							} else {
								def = this.getAddress(item);
							}
							def.then(function(items){
								if (items.length) {
									if (!this.myMap.addressObjectsLayer) {
										this.myMap.addressObjectsLayer = new VectorLayer({
											source: new VectorSource(),
											zIndex: 1001
										});
										this.myMap.map.addLayer(this.myMap.addressObjectsLayer);
									} else {
										this.myMap.addressObjectsLayer.getSource().clear(true);
									}
									var item = items[0];
									var feature = new Feature({
										geometry: new Point([item.location.x, item.location.y])
									});
									var iconStyle = new Style({
										image: new Icon({
											anchor: [0.5, 46],
											anchorXUnits: "fraction",
											anchorYUnits: "pixels",
											src: "https://openlayers.org/en/latest/examples/data/icon.png"
										})
									});
									feature.setStyle(iconStyle);
									this.myMap.addressObjectsLayer.getSource().addFeature(feature);
									this.myMap.map.getView().fit(feature.getGeometry(), {
										duration: 500,
										maxZoom: this.myMap.map.getView().getZoom()
									});
								} else {
									this.$vBus.$emit("show-message", {
										type: "warning",
										message: "Deja, įvyko klaida... Nepavyko gauti pasirinktos vietos geografinių duomenų"
									});
								}
								this.loading = false;
							}.bind(this), function(){
								this.loading = false;
							}.bind(this));
						}
					}
				}
			}
		}
	}
</script>

<style scoped>
	.v-input {
		width: 250px;
	}
	.invisible {
		visibility: hidden;
	}
</style>