<template>
	<div>
		<v-btn
			icon
			small
			title="Eiti į žemėlapį"
			v-on:click="goToMap"
		>
			<v-icon
				color="grey"
				size="24"
			>
				mdi-map
			</v-icon>
		</v-btn>
	</div>
</template>

<script>
	export default {
		methods: {
			goToMap: function(){
				window.open(this.$router.history.base);
			}
		}
	}
</script>