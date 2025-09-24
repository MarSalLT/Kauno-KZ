<template>
	<OverMapButton
		title="Pateikti eismo organizavimo priemonių statistiką"
		icon="mdi-chart-bar"
		:clickCallback="onClick"
		ref="btn"
	/>
</template>

<script>
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "Eismo organizavimo priemonių statistika"
			};
			return data;
		},

		components: {
			OverMapButton
		},

		methods: {
			onClick: function(){
				this.$vBus.$emit("show-or-hide-stats-generator", {
					title: this.title,
					btn: this.$refs.btn
				});
			}
		}
	}
</script>