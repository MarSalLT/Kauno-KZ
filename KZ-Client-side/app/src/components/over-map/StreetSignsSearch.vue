<template>
	<OverMapButton
		title="Ieškoti žemėlapyje"
		icon="mdi-map-search-outline"
		:clickCallback="onClick"
		ref="btn"
	/>
</template>

<script>
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "Žemėlapio objektų paieška"
			};
			return data;
		},

		components: {
			OverMapButton
		},

		methods: {
			onClick: function(){
				this.$vBus.$emit("show-or-hide-street-signs-search", {
					title: this.title,
					btn: this.$refs.btn
				});
			}
		},

		watch: {
			"$store.state.myMap.searchResults": {
				handler: function(searchResults){
					this.$refs.btn.needsAttention = Boolean(searchResults);
				}
			}
		}
	}
</script>