<template>
	<OverMapButton
		title="Ieškoti užduočių"
		icon="mdi-text-box-search"
		:clickCallback="onClick"
		ref="btn"
	/>
</template>

<script>
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "Užduočių paieška"
			};
			return data;
		},

		components: {
			OverMapButton
		},

		methods: {
			onClick: function(){
				this.$vBus.$emit("show-or-hide-tasks-search", {
					title: this.title,
					btn: this.$refs.btn
				});
			}
		}
	}
</script>