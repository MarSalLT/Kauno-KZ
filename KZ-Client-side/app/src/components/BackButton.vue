<template>
	<div class="d-flex align-center pa-1">
		<v-btn
			icon
			color="primary"
			v-on:click="action"
			small
			class="mr-1"
		>
			<v-icon>mdi-keyboard-backspace</v-icon>
		</v-btn>
		<span class="body-2">{{title}}</span>
	</div>
</template>

<script>
	export default {
		props: {
			title: String,
			action: Function
		}
	}
</script>

<style scoped>
	div {
		background-color: #e6f6ff;
	}
</style>