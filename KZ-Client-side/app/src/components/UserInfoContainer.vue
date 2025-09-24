<template>
	<div v-if="$store.state.userData && $store.state.userData.username" class="d-flex align-center">
		<span>{{$store.state.userData.username}}</span>
		<v-btn
			icon
			small
			class="ml-1"
			title="Atsijungti"
			v-on:click="logout"
			:loading="logoutInProgress"
		>
			<v-icon
				small
			>
				mdi-logout
			</v-icon>
		</v-btn>
	</div>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";

	export default {
		data: function(){
			var data = {
				logoutInProgress: false
			};
			return data;
		},

		methods: {
			logout: function(){
				this.logoutInProgress = true;
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "user/logout", function(json){
					return json.success;
				}, "POST").then(function(){
					this.logoutInProgress = false;
					this.$store.commit("setUserData", {}); // Anksčiau vietoj šito buvo: this.$router.go(), bet jis vizualiai lėtai veikė? Jokio privalumo jame nebuvo?..
				}.bind(this), function(){
					this.logoutInProgress = false;
					this.$vBus.$emit("show-message", {
						type: "warning",
						message: "Deja, atsijungti nepavyksta..."
					});
				}.bind(this));
			}
		}
	};
</script>