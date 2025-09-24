<template>
	<div>
		<v-btn
			icon
			small
			:title="title"
			v-on:click="goToSC"
			:disabled="disabled"
		>
			<v-icon
				color="grey"
				size="24"
			>
				mdi-grease-pencil
			</v-icon>
		</v-btn>
	</div>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";

	export default {
		data: function(){
			var data = {
				title: "Kelio ženklų simbolių valdymas",
				disabled: !CommonHelper.scEnabled
			};
			return data;
		},

		methods: {
			goToSC: function(){
				window.open(this.$router.history.base + "/sc/gallery", "_blank");
			}
		}
	}
</script>