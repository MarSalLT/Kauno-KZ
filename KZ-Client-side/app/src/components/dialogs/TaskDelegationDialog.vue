<template>
	<v-dialog
		persistent
		v-model="dialog"
		max-width="900"
		:scrollable="Boolean(taskData)"
	>
		<v-card>
			<v-card-title>
				<span>Užduoties delegavimas</span>
			</v-card-title>
			<v-card-text class="pb-1 pt-1">
				<template v-if="taskData">
					<template v-if="taskData == 'error'">
						<v-alert
							dense
							type="error"
							class="ma-0 d-inline-block"
						>
							Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
						</v-alert>
					</template>
					<template v-else>
						<TaskPreview :data="taskData" />
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
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5 d-flex justify-end">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="delegateTask"
					outlined
					small
					v-if="taskData"
					:loading="delegationInProgress"
				>
					Vykdyti delegavimą
				</v-btn>
				<v-btn
					color="blue darken-1"
					text
					v-on:click="closeDialog"
					outlined
					small
					:disabled="Boolean(delegationInProgress)"
				>
					Uždaryti
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import TaskPreview from "../TaskPreview";
	import TaskHelper from "../helpers/TaskHelper";

	export default {
		data: function(){
			var data = {
				dialog: false,
				taskData: null,
				delegationInProgress: false
			};
			return data;
		},

		components: {
			TaskPreview
		},

		created: function(){
			this.$vBus.$on("delegate-task", this.showDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("delegate-task", this.showDialog);
		},

		methods: {
			showDialog: function(e){
				this.getData(e);
				this.delegationInProgress = false;
				this.dialog = true;
			},
			closeDialog: function(){
				this.dialog = false;
			},
			getData: function(e){
				this.taskData = null;
				if (e && e.feature) {
					var taskGlobalId = e.feature.get("GlobalID");
					if (taskGlobalId) {
						TaskHelper.getTaskData(taskGlobalId, true, this.$vBus).then(function(taskData){
							if (taskData) {
								if (taskData.attachments) {
									taskData.attachments.forEach(function(attachment, i){
										attachment.key = i;
										attachment.src = CommonHelper.prependProxyIfNeeded(attachment.src);
										if (attachment.contentType && (attachment.contentType.substr(0, 6) == "image/")) {
											attachment.isImage = true;
										}
									});
								}
								taskData.globalId = taskGlobalId;
							}
							this.taskData = taskData;
						}.bind(this), function(){
							this.taskData = "error";
						}.bind(this));
					} else {
						this.taskData = "error";
					}
				} else {
					this.taskData = "error";
				}
			},
			delegateTask: function(){
				if (this.taskData && this.taskData.attributes) {
					if (this.taskData.attributes["Pabaigos_data"] && (this.taskData.attributes["Pabaigos_data"] > Date.now())) {
						this.delegationInProgress = true;
						TaskHelper.notifyAboutTaskChangeToEinpix(this.taskData.attributes, "delegation").then(function(response){
							this.delegationInProgress = false;
							if (response && (response.status == "OK")) {
								this.$vBus.$emit("show-message", {
									type: "success",
									message: "Užduotis deleguota sėkmingai!",
									timeout: 2000
								});
							} else {
								this.$vBus.$emit("show-message", {
									type: "warning"
								});
							}
						}.bind(this), function(){
							this.delegationInProgress = false;
							this.$vBus.$emit("show-message", {
								type: "warning"
							});
						}.bind(this));
					} else {
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Turi būti nurodyta teisinga užduoties pabaigos data!"
						});
					}
				} else {
					this.$vBus.$emit("show-message", {
						type: "warning"
					});
				}
			},
			getTaskStatus: function(taskData){
				var promise = new Promise(function(resolve, reject){
					if (taskData && taskData.globalId) {
						TaskHelper.getTaskData(taskData.globalId, false, this.$vBus).then(function(taskData){
							if (taskData && taskData.attributes) {
								resolve(taskData.attributes["Statusas"]);
							} else {
								reject();
							}
						}.bind(this), function(){
							reject();
						}.bind(this));
					} else {
						reject();
					}
				}.bind(this));
				return promise;
			}
		}
	}
</script>