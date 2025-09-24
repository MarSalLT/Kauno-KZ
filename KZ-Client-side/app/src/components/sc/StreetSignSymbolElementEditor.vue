<template>
	<div class="full-height">
		<template v-if="elementData">
			<StreetSignSymbolDashboard
				:data="elementData"
				mode="element-edit"
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
				elementData: null
			};
			return data;
		},

		components: {
			StreetSignSymbolDashboard
		},

		methods: {
			getSymbolData: function(data){
				if (data && data.id) {
					this.elementData = null;
					StreetSignsSymbolsManagementHelper.getUniqueSymbolElementData(data.id).then(function(elementData){
						elementData.mode = "element-edit";
						this.elementData = elementData;
					}.bind(this), function(){
						this.elementData = {
							error: "unknown"
						};
					}.bind(this));
				} else {
					this.elementData = {
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
						if (scItem.mode == "element-edit") {
							this.getSymbolData(scItem);
						}
					}
				}
			}
		}
	};
</script>