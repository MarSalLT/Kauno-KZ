<template>
	<div>
		<v-btn
			icon
			small
			:title="title"
			v-on:click="goToUsersManager"
		>
			<v-icon
				color="grey"
				size="24"
			>
				mdi-account-multiple
			</v-icon>
		</v-btn>
	</div>
</template>

<script>
	export default {
		data: function(){
			var data = {
				title: "Vartotojų valdymas"
			};
			return data;
		},

		methods: {
			goToUsersManager: function(){
				window.open(this.$router.history.base + "/manage-users", "_blank");
			}
		}
	}
</script>