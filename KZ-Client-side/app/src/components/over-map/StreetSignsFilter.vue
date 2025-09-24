<template>
	<OverMapButton
		title="Filtruoti kelio ženklus"
		icon="mdi-filter"
		:clickCallback="onClick"
		ref="btn"
	/>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "Kelio ženklų filtras"
			};
			return data;
		},

		components: {
			OverMapButton
		},

		methods: {
			onClick: function(){
				this.$vBus.$emit("show-or-hide-street-signs-filter", {
					title: this.title,
					btn: this.$refs.btn
				});
			}
		},

		watch: {
			"$store.state.myMap.layersFilter": {
				handler: function(layersFilter){
					var needsAttention = false;
					if (layersFilter) {
						for (var key in layersFilter) {
							if (layersFilter[key]) {
								needsAttention = true;
								break;
							}
						}
					}
					this.$refs.btn.needsAttention = needsAttention;
					if (needsAttention) {
						// Mintis ta, kad reikia uždaryti popup'ą... Bet tai aktualu turbūt tikrai ne visada?.. Aktualiausia, kai keičiasi historicMoment'as!
						CommonHelper.routeTo({
							router: this.$router,
							vBus: this.$vBus
						});
					}
				}
			}
		}
	}
</script>