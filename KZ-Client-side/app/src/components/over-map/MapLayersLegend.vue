<template>
	<OverMapButton
		title="Rodyti žemėlapio legendą"
		icon="mdi-map-legend"
		:clickCallback="onClick"
		ref="btn"
	/>
</template>

<script>
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "Žemėlapio legenda"
			};
			return data;
		},

		components: {
			OverMapButton
		},

		methods: {
			onClick: function(){
				this.$vBus.$emit("show-or-hide-legend", {
					title: this.title,
					btn: this.$refs.btn,
					setBtnActive: this.setBtnActive
				});
			},
			setBtnActive: function(active){
				if (this.$refs.btn) {
					this.$refs.btn.active = active;
				}
			}
		}
	}
</script>