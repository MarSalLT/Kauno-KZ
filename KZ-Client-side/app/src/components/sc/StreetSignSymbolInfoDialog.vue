<template>
	<v-dialog
		v-model="dialog"
		max-width="600"
		:scrollable="Boolean(list)"
	>
		<v-card>
			<v-card-title>
				<span>Simbolį naudojantys kelio ženklai</span>
			</v-card-title>
			<v-card-text class="pb-0 pt-1">
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
						<ol class="mt-n2">
							<template v-for="(item, i) in list">
								<li :key="i" class="mt-2">
									<router-link :to="item.href" target='_blank'>Objektas {{item.id}}</router-link>
								</li>
							</template>
						</ol>
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
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="dialog = false"
					outlined
					small
				>
					Uždaryti
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";

	export default {
		data: function(){
			var data = {
				dialog: false,
				e: null,
				list: null
			};
			return data;
		},

		created: function(){
			this.$vBus.$on("show-symbol-info", this.showDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-symbol-info", this.showDialog);
		},

		methods: {
			showDialog: function(e){
				this.e = e;
				this.getData();
				this.dialog = true;
			},
			getData: function(){
				this.list = null;
				if (this.e && this.e.id) {
					var layerIdMeta = CommonHelper.layerIds["verticalStreetSigns"],
						serviceUrl = this.$store.getters.getServiceUrl(layerIdMeta[0]);
					if (serviceUrl) {
						serviceUrl = serviceUrl.replace("FeatureServer", "MapServer");
						serviceUrl = CommonHelper.prependProxyIfNeeded(serviceUrl) + "/" + layerIdMeta[1] + "/query";
						var params = {
							f: "json",
							outFields: "*",
							returnGeometry: false,
							where: CommonHelper.customSymbolIdFieldName + " = '" + this.e.id + "'"
						};
						CommonHelper.getFetchPromise(serviceUrl, function(json){
							return json;
						}, "POST", params).then(function(result){
							if (result && result.features) {
								var list = [];
								result.features.forEach(function(feature){
									// FIXME! Imti iš "layerInfo"?!!
									list.push({
										id: feature.attributes["GlobalID"],
										href: "/?t=v&l=" + layerIdMeta[1] + "&id=" + CommonHelper.stripGuid(feature.attributes["GlobalID"])
									});
								});
								this.list = list;
							} else {
								this.list = "error";
							}
						}.bind(this), function(){
							this.list = "error";
						}.bind(this));
					} else {
						this.list = "error";
					}
				} else {
					this.list = "error";
				}
			}
		}
	}
</script>