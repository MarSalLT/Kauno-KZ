<template>
	<div class="main-wrapper">
		<template v-if="mainComponent">
			<template v-if="mainComponent == 'error'">
				<v-app>
					<div class="pa-2">Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...</div>
				</v-app>
			</template>
			<template v-else>
				<component :is="mainComponent"></component>
				<v-app>
					<SessionExpiredDialog />
				</v-app>
			</template>
		</template>
		<template v-else>
			<MainLoadingComponent />
		</template>
	</div>
</template>

<script>
	import CommonHelper from "../components/helpers/CommonHelper";
	import MainLoadingComponent from "../components/MainLoadingComponent";
	import SessionExpiredDialog from "../components/dialogs/SessionExpiredDialog";
	import Vue from "vue";
	import "whatwg-fetch";

	export default {
		data: function(){
			var data = {
				mainComponent: null
			};
			return data;
		},

		created: function(){
			this.getUserData().then(function(userData){
				CommonHelper.setUnapprovedFromUsers(userData);
				this.$store.commit("setUserData", userData);
			}.bind(this), function(){
				this.mainComponent = "error";
			}.bind(this));
		},

		beforeDestroy: function(){
			this.$vBus.$off("check-if-valid-session", this.checkIfValidSession);
		},

		computed: {
			userData: {
				get: function(){
					return this.$store.state.userData;
				}
			}
		},

		components: {
			MainLoadingComponent,
			SessionExpiredDialog,
			"login-form": () => ({
				component: import("../components/main/LoginForm"),
				loading: MainLoadingComponent,
				delay: 0
			})
		},

		watch: {
			$route(to) {
				this.routeTo(to);
			},
			userData: {
				immediate: true,
				handler: function(userData){
					this.$vBus.$off("check-if-valid-session", this.checkIfValidSession);
					if (userData && userData.username) {
						this.$vBus.$on("check-if-valid-session", this.checkIfValidSession);
					}
					this.routeTo(this.$router.history.current);
				}
			}
		},

		beforeCreate: function(){
			this.$vBus.$on("route-to", function(p){
				this.routeTo(p);
			}.bind(this));
		},

		methods: {
			getUserData: function(){
				var promise = new Promise(function(resolve, reject){
					CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "user/get-data").then(function(result){
						if (result) {
							resolve(result);
						} else {
							reject();
						}
					}, function(){
						reject();
					});
				}.bind(this));
				return promise;
			},
			routeTo: function(p){
				var mainComponent;
				if (this.userData) {
					if (this.userData.username) {
						var regExp,
							match;
						if (this.userData.permissions && this.userData.permissions.length) {
							this.userData.permissions.some(function(permission){
								regExp = new RegExp("^/" + permission + "(?:/|$)");
								match = p.path.match(regExp);
								if (match) {
									if (permission == "manage-users") {
										mainComponent = "UsersManager";
									} else if (permission == "sc") {
										mainComponent = "StreetSignsSymbolsManager";
										regExp = new RegExp("^/sc/(edit|create|element-create|element-edit)$");
										match = p.path.match(regExp);
										if (match) {
											var scMode = match[1],
												scItem = {
													mode: scMode
												};
											if (scMode == "create") {
												scItem.type = p.query.type;
												scItem.vsss = p.query.vsss;
												scItem.vss = p.query.vss;
											} else if (scMode == "edit" || scMode == "element-edit") {
												scItem.id = p.query.id;
											} else if (scMode == "element-create") {
												scItem.type = p.query.type;
											}
											this.$store.commit("setSCItem", scItem);
										}
									}
									return true;
								}
							}.bind(this));
						}
						if (!mainComponent) {
							if (p.path == "/") {
								var activeAction,
									activeFeature;
								if (p.query.action) {
									activeAction = p.query;
								} else if (p.query.coordinates) {
									this.$store.commit("setInitialCoordinates", p.query.coordinates);
								} else {
									if (Number.isInteger(parseInt(p.query.l)) && p.query.id) {
										activeFeature = {
											type: p.query.t,
											layerId: p.query.l,
											globalId: p.query.id,
											historicMoment: p.query.h
										};
									}
								}
								this.$store.commit("setActiveAction", activeAction);
								this.$store.commit("setActiveFeature", activeFeature);
								if (p.query.t == "task" && p.query.id) {
									// Pvz. http://localhost:3001/admin/?t=task&id=7CC5CF44-83EC-4AA0-ABE0-525F69651DA7
									this.$store.commit("setActiveTask", {
										globalId: "{" + p.query.id + "}", // Apjuosimo reikia užduočių sąrašui... Kad būtų paryškinas įrašas...
									});
								}
								mainComponent = "AdminMap";
							} else {
								this.$router.replace("/");
								return;
							}
						}
						if (mainComponent) {
							Vue.component(
								mainComponent,
								() => import("../components/main/" + mainComponent)
							);
						}
						if (!mainComponent) {
							mainComponent = "error";
						}
					} else {
						mainComponent = "login-form";
					}
				}
				this.mainComponent = mainComponent;
			},
			checkIfValidSession: function(){
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "user/check-if-valid-session", function(json){
					return json;
				}, "POST").then(function(result){
					if (result) {
						if (!result.valid) {
							this.$vBus.$emit("show-session-expired-dialog");
						}
					}
				}.bind(this), function(){
					// ...
				}.bind(this));
			}
		},

		mounted: function(){
			var initialLoadingIndicator = document.querySelector("#initial-loading-indicator");
			if (initialLoadingIndicator) {
				if (initialLoadingIndicator.parentNode) {
					initialLoadingIndicator.parentNode.removeChild(initialLoadingIndicator);
				}
			}
		}
	};
</script>

<style>
	body, html {
		height: 100% !important;
		overflow: hidden;
	}
</style>

<style scoped>
	.main-wrapper {
		height: 100%;
	}
</style>