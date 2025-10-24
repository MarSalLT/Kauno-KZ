<template>
	<span :class="
		[
			'status-code pa-1 text-no-wrap',
			code,
			code == '1' ? 'orange white--text' : null,
			(code == '4' || code == '2') ? 'success white--text' : null,
			code == '5' ? 'error white--text' : null,
			code == '6' ? 'yellow' : null,
			((code == '4' || code == '2') && approved) ? 'darken-2' : null,
			code == '7' ? 'error white--text lighten-1' : null,
		]
	">
		{{title}}
	</span>
</template>

<script>
	export default {
		props: {
			code: String,
			title: String,
			approved: Number
		}
	}
</script>

<style scoped>
	div {
		background-color: #e6f6ff;
	}
</style>