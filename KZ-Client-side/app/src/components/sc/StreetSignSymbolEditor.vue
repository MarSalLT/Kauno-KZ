<template>
	<div class="full-height">
		<template v-if="symbolData">
			<StreetSignSymbolDashboard
				:data="symbolData"
				mode="edit"
			/>
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
	import StreetSignsSymbolsManagementHelper from "../helpers/StreetSignsSymbolsManagementHelper";
	import StreetSignSymbolDashboard from "./StreetSignSymbolDashboard";

	export default {
		data: function(){
			var data = {
				symbolData: null
			};
			return data;
		},

		components: {
			StreetSignSymbolDashboard
		},

		methods: {
			getSymbolData: function(data){
				if (data && data.id) {
					this.symbolData = null;
					StreetSignsSymbolsManagementHelper.getUniqueSymbolData(data.id).then(function(symbolData){
						symbolData.mode = "edit";
						this.symbolData = symbolData;
					}.bind(this), function(){
						this.symbolData = {
							error: "unknown"
						};
					}.bind(this));
				} else {
					this.symbolData = {
						error: "no-id"
					};
				}
			}
		},

		watch: {
			"$store.state.scItem": {
				immediate: true,
				handler: function(scItem){
					if (scItem) {
						if (scItem.mode == "edit") {
							this.getSymbolData(scItem);
						}
					}
				}
			}
		}
	};
</script>