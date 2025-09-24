<template>
	<OverMapButton
		title="Rodyti naudotojo taškus"
		icon="mdi-map-marker-star"
		:clickCallback="onClick"
		:activeChangeCallback="onActiveChange"
		ref="btn"
	/>
</template>

<script>
	import MapHelper from "../helpers/MapHelper";
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "Naudotojo taškai"
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

		mounted: function(){
			this.userDataService = MapHelper.userDataService;
		},

		methods: {
			onClick: function(){
				this.$refs.btn.needsAttention = this.$refs.btn.active = !this.$refs.btn.active;
				this.$vBus.$emit("show-or-hide-user-points-manager", {
					title: this.title,
					btn: this.$refs.btn
				});
			},
			onActiveChange: function(active){
				if (this.$refs.btn) {
					this.$refs.btn.needsAttention = this.$refs.btn.active = active;
					if (active) {
						this.showLayer();
					} else {
						this.hideLayer();
					}
					// Jei sluoksnis matomas, reiktų prikabinti jo turinio pokyčių listener'į?.. Kuris iš esmės poreikiui esant atnaujintų features'ų sąrašą list'e!
				}
			},
			showLayer: function(){
				if (this.userDataService && this.myMap) {
					delete this.userDataService.layer; // Kad iš naujo perpieštų...
					this.myMap.addLayer(this.userDataService);
				}
			},
			hideLayer: function(){
				if (this.userDataService && this.myMap) {
					this.myMap.removeLayer(this.userDataService);
				}
			}
		}
	}
</script>