<template>
	<OverMapButton
		:title="title"
		icon="mdi-reload"
		:clickCallback="onClick"
		:disableWhenInteractionsCount="false"
	/>
</template>

<script>
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "Perpiešti vertikalųjį ženklinimą"
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

		components: {
			OverMapButton
		},

		methods: {
			onClick: function(){
				if (this.myMap.interactionsCount) {
					this.$vBus.$emit("show-message", {
						type: "warning",
						message: "Šiuo metu vertikalusis ženklinimas negali būti perpieštas..."
					});
				} else {
					this.myMap.map.getLayers().forEach(function(layer){
						if (layer.service && (layer.service.id == "street-signs-vertical")) {
							layer.service.timestamp = Date.now(); // Atseit šitas perpiešimo mygtukas `pravalo simbolių piešinukų cache'ą`...
							if (layer.getLayers) {
								layer.getLayers().forEach(function(l){
									l.getSource().refresh();
								});
							}
						}
					});
				}
			}
		}
	}
</script>