<template>
	<Entry>
		<template v-slot:header-components>
			<div
				class="d-flex mr-5"
			>
				<GoToMapButton />
			</div>
		</template>
		<div class="d-flex flex-column full-height">
			<div class="pa-4 content">
				<template v-if="list">
					<template v-if="list == 'error'">
						<v-alert
							dense
							type="error"
							class="ma-0 d-inline-block"
						>
							Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
						</v-alert>
					</template>
					<template v-else>
						<v-data-table
							:headers="listHeaders"
							:items="list"
							:options="listOptions"
							hide-default-footer
							disable-pagination
							class="simple-data-table users-manager"
						>
							<template v-slot:[`item.actions`]="{item}">
								<MyButton
									:descr="{
										color: 'primary',
										title: 'Redaguoti vartotoją',
										icon: 'mdi-pencil',
										onClick: showUserDialog,
										item: item
									}"
									class="mr-1"
								/>
								<MyButton
									:descr="{
										color: 'error',
										title: 'Šalinti vartotoją',
										icon: 'mdi-delete',
										onClick: deleteUser,
										item: item
									}"
								/>
							</template>
						</v-data-table>
						<v-btn
							fab
							bottom
							right
							fixed
							color="success"
							class="mr-4"
							v-on:click="showUserDialog()"
						>
							<v-icon
								title="Kurti naują vartotoją"
							>
								mdi-plus
							</v-icon>
						</v-btn>
						<v-dialog
							persistent
							max-width="600"
							v-model="userDialog"
							scrollable
						>
							<v-card v-if="userFormData">
								<v-card-title>
									<span>{{userFormData.id ? 'Vartotojo duomenys' : 'Naujo vartotojo duomenys'}}</span>
								</v-card-title>
								<v-card-text class="pb-2 pt-2">
									<div v-if="userFormData.id" class="mb-3">
										Jei norima keisti dabartinį vartotojo slaptažodį, užpildykite abu slaptažodžio laukus. Jei dabartinio vartotojo slaptažodžio keisti nenorima — abu slaptažodžio laukus palikite tuščius.
									</div>
									<MyForm
										:data="userFormData"
										id="user-data"
										:spacious="true"
										ref="form"
										:key="userFormKey"
										class="user-data-form"
									/>
								</v-card-text>
								<v-card-actions class="mx-2 pb-5 pt-5">
									<v-btn
										color="blue darken-1"
										text
										v-on:click="saveUser"
										outlined
										small
										:loading="userSaveInProgress"
									>
										{{userFormData.id ? 'Išsaugoti' : 'Kurti naują vartotoją'}}
									</v-btn>
									<v-btn
										color="blue darken-1"
										text
										v-on:click="userDialog = false"
										outlined
										small
									>
										Uždaryti
									</v-btn>
								</v-card-actions>
							</v-card>
						</v-dialog>
					</template>
				</template>
				<template v-else>
					<v-progress-circular
						indeterminate
						color="primary"
						:size="30"
						width="2"
					></v-progress-circular>
				</template>
			</div>
		</div>
		<ConfirmationDialog />
	</Entry>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import ConfirmationDialog from "../dialogs/ConfirmationDialog";
	import Entry from "./templates/SimpleEntry";
	import MyForm from "./../MyForm";
	import GoToMapButton from "../header-items/GoToMapButton";
	import MyButton from "../MyButton";

	export default {
		data: function(){
			var data = {
				list: null,
				listHeaders: [{
					value: "name",
					text: "Vartotojo vardas"
				},{
					value: "prettyRole",
					text: "Vartotojo vaidmuo"
				},{
					value: "email",
					text: "El. paštas"
				},{
					value: "uzsakovo_vardas",
					text: "Pilnas vardas, pavardė"
				},{
					value: "uzsakovo_imone",
					text: "Įmonės pavadinimas"
				},{
					value: "actions",
					text: "Veiksmai",
					align: "center",
					sortable: false
				}],
				listOptions: {
					sortBy: ["name"],
					sortDesc: [false],
					mustSort: true,
				},
				userDialog: false,
				userFormData: null,
				userSaveInProgress: false,
				userFormKey: 0
			};
			return data;
		},

		mounted: function(){
			this.getList();
		},

		components: {
			ConfirmationDialog,
			Entry,
			MyForm,
			GoToMapButton,
			MyButton
		},

		methods: {
			getList: function(callback){
				this.list = null;
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "users/get-users", function(json){
					return json;
				}, "GET").then(function(list){
					list.forEach(function(item){
						item.prettyRole = this.getPrettyRole(item.role);
					}.bind(this));
					this.list = list;
					if (callback) {
						callback();
					}
				}.bind(this), function(){
					this.list = "error";
					if (callback) {
						callback();
					}
				}.bind(this));
			},
			showUserDialog: function(user){
				var rolesDict = this.getRolesDict(),
					roles = [];
				for (var key in rolesDict) {
					roles.push({
						code: key,
						name: rolesDict[key]
					});
				}
				var userFormData = [{
					fields: [{
						title: "Vartotojo vardas",
						key: "name",
						required: true,
						disabled: Boolean(user)
					},{
						title: "Slaptažodis",
						key: "password1",
						required: Boolean(!user),
						type: "password"
					},{
						title: "Slaptažodis (pakartoti)",
						key: "password2",
						required: Boolean(!user),
						type: "password"
					},{
						// TODO! Turi būti kažkokia validacija, kad neprivestų nesąmonių?..
						title: "El. paštas",
						key: "email"
					},{
						title: "Pilnas vardas, pavardė",
						key: "clientName"
					},{
						title: "Įmonės pavadinimas",
						key: "clientEnterprise",
						domain: {
							codedValues: [] // TODO... Čia gaunasi šiek tiek blogai... Kad reikia hardcode'inti reikšmes?..
						},
					},{
						title: "Vaidmuo",
						key: "role",
						domain: {
							codedValues: roles
						},
						required: true
					}]
				}];
				if (user) {
					userFormData.id = user.id;
					userFormData[0].fields.forEach(function(field){
						field.value = user[field.key];
					});
				}
				this.userFormData = userFormData;
				this.userFormKey += 1;
				this.userSaveInProgress = false;
				this.userDialog = true;
			},
			deleteUser: function(user, btn){
				console.log("DELETE", user, btn);
				this.$vBus.$emit("confirm", {
					title: "Ar tikrai šalinti vartotoją?",
					message: "Ar tikrai šalinti vartotoją? Pašalinus vartotoją nebebus galima jo atstatyti...",
					positiveActionTitle: "Šalinti vartotoją",
					negativeActionTitle: "Atšaukti",
					positive: function(){
						btn.loading = true;
						var params = {
							id: user.id
						};
						var promise = new Promise(function(resolve, reject){
							CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "users/delete", function(json){
								if (json && json.success) {
									resolve(json);
								} else {
									reject();
								}
							}, "POST", params).then(function(result){
								resolve(result);
							}.bind(this), function(){
								reject();
							}.bind(this));
						}.bind(this));
						promise.then(function(){
							var callback = function(){
								btn.loading = false;
							}
							this.getList(callback); // FIXME: sugalvoti kažkokį `inline` atsinaujinimo sprendimą...
						}.bind(this), function(){
							btn.loading = false;
							this.$vBus.$emit("show-message", {
								type: "warning"
							});
						}.bind(this));
					}.bind(this)
				});
			},
			saveUser: function(){
				if (this.$refs.form) {
					var formData = this.$refs.form.getData(),
						invalid = {},
						errorsIn = [];
					if (this.userFormData && formData) {
						this.userFormData.forEach(function(group){
							if (group.fields) {
								group.fields.forEach(function(field){
									if (field.required && !formData[field.key]) {
										invalid[field.key] = true;
										errorsIn.push(field);
									}
								}.bind(this));
							}
						});
					}
					if (errorsIn.length) {
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Yra formos pildymo klaidų!"
						});
					} else {
						var password1 = formData.password1,
							password2 = formData.password2;
						if (!password1) {
							password1 = "";
						}
						if (!password2) {
							password2 = "";
						}
						if (password1 != password2) {
							invalid["password1"] = true;
							invalid["password2"] = true;
							errorsIn.push("pswd");
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: "Slaptažodžiai abiejuose laukuose turi sutapti!",
								timeout: 2000
							});
						}
					}
					this.$refs.form.invalid = invalid;
					if (!errorsIn.length) {
						this.userSaveInProgress = true;
						var params = {
							id: this.userFormData.id
						};
						params = Object.assign(params, formData);
						params.password = params.password1;
						delete params.password1;
						delete params.password2;
						this.createOrUpdateUser(params).then(function(){
							var callback = function(){
								this.userSaveInProgress = false;
								this.userDialog = false;
							}.bind(this);
							this.getList(callback); // FIXME: sugalvoti kažkokį `inline` atsinaujinimo sprendimą...
						}.bind(this), function(reason){
							this.userSaveInProgress = false;
							this.$vBus.$emit("show-message", {
								type: "warning",
								message: reason
							});
						}.bind(this));
					}
				}
			},
			createOrUpdateUser: function(params){
				for (var key in params) {
					params[key] = params[key] || "";
				}
				var promise = new Promise(function(resolve, reject){
					CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "users/create-or-update", function(json){
						if (json) {
							if (json.success) {
								resolve(json);
							} else {
								reject(json.readon);
							}
						} else {
							reject();
						}
					}, "POST", params).then(function(result){
						resolve(result);
					}.bind(this), function(){
						reject();
					}.bind(this));
				}.bind(this));
				return promise;
			},
			getRolesDict: function(){
				if (!this.prettyRoles) {
					this.prettyRoles = {
						"admin": "Visos sistemos administratorius",
						"approver": "Duomenų tvirtintojas",
						"kz-edit": "Visų KŽ valdytojas",
						"kz-horizontal-edit": "Horizontaliųjų KŽ valdytojas",
						"kz-infra-edit": "Infrastruktūros objektų valdytojas",
						"kz-vertical-edit": "Vertikaliųjų KŽ valdytojas",
						"street-viewer": "`Street Viewer`'is",
						"symbols-manager": "Unikalių simbolių kūrėjas",
						"viewer": "Viešų duomenų peržiūrėtojas",
						"viewer-history": "Viešų duomenų ir istorijos peržiūrėtojas"
					};
				}
				return this.prettyRoles;
			},
			getPrettyRole: function(role){
				var rolesDict = this.getRolesDict();
				return rolesDict[role] || role;
			}
		}
	};
</script>

<style scoped>
	.content {
		overflow: auto;
	}
</style>