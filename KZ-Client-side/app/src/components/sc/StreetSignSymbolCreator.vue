<template>
	<div :class="symbolData && symbolData.type ? 'full-height' : null">
		<template v-if="symbolData">
			<template v-if="symbolData.type">
				<StreetSignSymbolDashboard
					:data="symbolData"
					mode="create"
				/>
			</template>
			<template v-else>
				<div class="px-1">
					<template v-if="displayType == 'template-picker'">
						<div class="mt-1">
							<template v-if="layersInfo">
								<template v-if="layersInfo == 'error'">
									<v-alert
										dense
										type="error"
										class="ma-0 d-inline-block"
									>
										Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
									</v-alert>
								</template>
								<template v-else>
									<TemplatePicker
										:featureTypes="['verticalStreetSigns']"
										:layersInfo="layersInfo"
										:showOnly="showOnly"
										:onItemSelect="onTemplatePick"
									/>
									<div class="mt-3">
										<MyHeading
											value="Kita"
										/>
										<v-btn
											color="blue darken-1"
											text
											to="/sc/create?type=unkn"
											outlined
											small
											class="mt-3"
										>
											Kurti naują simbolį iškerpant nuotraukoje
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
	import MyHeading from "../MyHeading";
	import StreetSignSymbolDashboard from "./StreetSignSymbolDashboard";
	import TemplatePicker from "../TemplatePicker";

	export default {
		data: function(){
			var data = {
				symbolData: null,
				activeType: null,
				displayType: "template-picker",
				layersInfo: null,
				showOnly: {
					verticalStreetSigns: {
						all: ["117", "118", "304", "314", "315", "316", "317", "318", "319", "329", "330", "413", "507", "508", "509", "510", "511", "512", "513", "514", "515", "516", "517", "518", "519", "520", "521", "522", "523", "524", "529", "530", "531", "540", "541", "542", "543", "545", "546", "602", "603", "604", "606", "607", "608", "609", "611", "612", "613", "614", "615", "616", "617", "618", "620", "622", "623", "624", "625", "626", "627", "628", "629", "630", "631", "801", "802", "803", "804", "805", "806", "809", "810", "811", "824", "825", "826", "828", "829", "830", "831", "832", "833", "834", "835", "836", "837", "838", "840", "842", "843", "920", "921", "922", "923", "924", "941", "942"], // "901", "902", "903", "904"
						available: CommonHelper.scUniqueContent
					}
				}
			};
			return data;
		},

		components: {
			MyHeading,
			StreetSignSymbolDashboard,
			TemplatePicker
		},

		mounted: function(){
			if (this.displayType == "template-picker") {
				var layerIdMeta = CommonHelper.layerIds["verticalStreetSigns"];
				if (layerIdMeta) {
					var serviceUrl = this.$store.getters.getServiceUrl(layerIdMeta[0]);
					if (serviceUrl) {
						serviceUrl = CommonHelper.prependProxyIfNeeded(serviceUrl) + "/" + layerIdMeta[1] + "?f=json";
						CommonHelper.getFetchPromise(serviceUrl, function(json){
							return json;
						}, "GET").then(function(result){
							this.layersInfo = {
								verticalStreetSigns: result
							};
						}.bind(this), function(){
							this.layersInfo = "error";
						}.bind(this));
					}
				}
			}
		},

		methods: {
			getSymbolData: function(data){
				this.symbolData = data;
			},
			selectType: function(){
				this.$router.push({
					path: "/sc/create",
					query: {
						type: this.activeType
					}
				});
			},
			onTemplatePick: function(template){
				this.activeType = template;
				this.selectType();
			}
		},

		watch: {
			"$store.state.scItem": {
				immediate: true,
				handler: function(scItem){
					if (scItem) {
						if (scItem.mode == "create") {
							this.getSymbolData(scItem);
						}
					}
				}
			}
		}
	};
</script>

<style scoped>
	.v-select {
		width: 300px;
	}
</style>