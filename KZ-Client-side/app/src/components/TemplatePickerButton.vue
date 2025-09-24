<template>
	<v-btn
		height="auto"
		min-width="auto"
		class="pa-0"
		elevation="0"
		color="grey lighten-3"
		:value="template.id"
		:disabled="disabled"
		:class="[disabled ? 'hardly-visible' : null]"
	>
		<ESRISymbol
			:descr="template.symbol"
			:title="noTitle ? null : template.name"
			:dark="dark"
		/>
	</v-btn>
</template>

<script>
	import ESRISymbol from "./ESRISymbol";

	export default {
		data: function(){
			var data = {
				disabled: Boolean(this.template.disabled)
			};
			return data;
		},

		props: {
			template: Object,
			dark: Boolean,
			noTitle: Boolean
		},

		components: {
			ESRISymbol
		}
	};
</script>

<style scoped>
	.hardly-visible {
		opacity: 0.15;
	}
</style>