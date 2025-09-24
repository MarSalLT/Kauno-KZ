<template>
	<Entry :mainComponentWidth="550">
		<v-form ref="form">
			<div class="mb-3 body-2">Sistemą administruoti galima tik prisijungusiems vartotojams. Registracijos nėra, prisijungimo duomenis išduoda „Kauno Planas“.</div>
			<v-alert
				dense
				type="error"
				v-if="errorMessage"
				class="mb-3"
			>
				<div>{{errorMessage}}</div>
			</v-alert>
			<v-text-field
				placeholder="Vartotojo vardas"
				dense
				hide-details
				outlined
				class="plain"
				v-model="usernameValue"
				ref="usernameField"
				autocomplete="off"
				required
				:error="usernameError"
				:disabled="loading"
				@keydown.enter="trySubmitting('username')"
			>
				<template v-slot:append>
					<v-btn
						icon
						height="20"
						width="20"
						:class="['mt-1', usernameValue ? null : 'invisible']"
						v-on:click="clearUsername"
						tabindex="-1"
					>
						<v-icon title="Valyti" small>mdi-close-circle</v-icon>
					</v-btn>
				</template>
			</v-text-field>
			<v-text-field
				placeholder="Slaptažodis"
				dense
				hide-details
				outlined
				class="plain mt-2"
				v-model="passwordValue"
				ref="passwordField"
				autocomplete="off"
				type="password"
				required
				:error="passwordError"
				:disabled="loading"
				@keydown.enter="trySubmitting('password')"
			>
				<template v-slot:append>
					<v-btn
						icon
						height="20"
						width="20"
						:class="['mt-1', passwordValue ? null : 'invisible']"
						v-on:click="clearPassword"
						tabindex="-1"
					>
						<v-icon title="Valyti" small>mdi-close-circle</v-icon>
					</v-btn>
				</template>
			</v-text-field>
			<div class="mt-3">
				<v-btn
					small
					v-on:click="login"
					:loading="loading"
				>
					Prisijungti
				</v-btn>
			</div>
		</v-form>
	</Entry>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import Entry from "./templates/BoldEntry";

	export default {
		data: function(){
			var data = {
				usernameValue: null,
				passwordValue: null,
				usernameError: false,
				passwordError: false,
				loading: false,
				errorMessage: null
			};
			return data;
		},

		components: {
			Entry
		},

		methods: {
			clearUsername: function(){
				this.usernameValue = null;
				if (this.$refs.usernameField) {
					this.$refs.usernameField.blur();
				}
			},
			clearPassword: function(){
				this.passwordValue = null;
				if (this.$refs.passwordField) {
					this.$refs.passwordField.blur();
				}
			},
			login: function(){
				this.usernameError = !this.usernameValue;
				this.passwordError = !this.passwordValue;
				if (this.usernameValue && this.passwordValue) {
					this.errorMessage = null;
					this.loading = true;
					var params = {
						username: this.usernameValue,
						password: this.passwordValue
					};
					CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "user/login", function(json){
						if (json.username) {
							return json;
						}
					}, "POST", params).then(function(userData){
						this.loading = false;
						CommonHelper.setUnapprovedFromUsers(userData);
						this.$store.commit("setUserData", userData);
					}.bind(this), function(){
						this.loading = false;
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Deja, prisijungti nepavyksta..."
						});
					}.bind(this));
				} else {
					this.errorMessage = "Yra neužpildytų laukų!";
				}
			},
			trySubmitting: function(field){
				if (field == "username") {
					if (this.usernameValue) {
						if (this.passwordValue) {
							this.login();
						} else {
							if (this.$refs.passwordField) {
								this.$refs.passwordField.focus();
							}
						}
					}
				} else if (field == "password") {
					if (this.passwordValue) {
						if (this.usernameValue) {
							this.login();
						} else {
							if (this.$refs.usernameField) {
								this.$refs.usernameField.focus();
							}
						}
					}
				}
			}
		}
	};
</script>

<style scoped>
	.invisible {
		visibility: hidden;
	}
	.v-input {
		background-color: #f7f7f7 !important;
	}
</style>