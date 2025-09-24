<template>
	<div>
		<template v-if="comments">
			<template v-if="comments == 'error'">
				<v-alert
					dense
					type="error"
					class="ma-0 d-inline-block"
				>
					Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
				</v-alert>
			</template>
			<template v-else>
				<table class="comments">
					<template v-for="(comment, i) in comments">
						<tr :key="i">
							<td class="comment pa-2">
								<div class="d-flex align-center">
									<div class="flex-grow-1">
										<div>
											<span class="font-weight-medium body-2 mr-1" :title="comment.authorName + ' (' + comment.authorEmail + ')'">{{comment.authorName || comment.author}}</span> | <span class="caption grey--text ml-1">{{comment.created}}</span>
										</div>
										<div>
											{{comment.message}}
										</div>
									</div>
									<div v-if="comment.attachments" class="mt-1">
										<template v-for="(attachment, j) in comment.attachments">
											<div :key="i + '-' + j">
												<div :class="j ? 'mt-2' : null">
													<template v-if="attachment.type == 'image'">
														<div class="img-wrapper">
															<v-img
																:src="getAttachmentUrl(attachment.url)"
																:width="100"
																:height="100"
																:contain="true"
																v-on:click="showImage(attachment)"
															>
																<template v-slot:placeholder>
																	<v-row
																		class="fill-height ma-0 full-height"
																		align="center"
																		justify="center"
																	>
																		<v-progress-circular
																			indeterminate
																			color="primary"
																			:size="25"
																			width="2"
																		></v-progress-circular>
																	</v-row>
																</template>
															</v-img>
															<AttachmentTransferButton
																:attachment="attachment"
																:feature="feature"
																:getAttachmentUrl="getAttachmentUrl"
															/>
														</div>
													</template>
													<template v-else>
														<v-btn
															color="blue darken-1"
															text
															v-on:click="downloadAttachment(attachment)"
															outlined
															small
														>
															<v-icon
																left
																dark
															>
																mdi-attachment
															</v-icon>
															{{attachment.original_filename + "." + attachment.original_extension}}
														</v-btn>
													</template>
												</div>
											</div>
										</template>
									</div>
								</div>
							</td>
						</tr>
					</template>
				</table>
			</template>
			<div class="mt-2">
				<div v-if="letCommentsRefresh">
					<v-btn
						color="blue darken-1"
						text
						v-on:click="getComments"
						outlined
						small
					>
						<v-icon left size="18">mdi-reload</v-icon>
						{{comments == 'error' ? 'Gauti komentarus iš naujo' : 'Perkrauti'}}
					</v-btn>
				</div>
				<div>
					<div class="d-flex mt-2 align-end">
						<v-textarea
							placeholder="Naujas komentaras"
							dense
							hide-details
							outlined
							class="plain body-2 pa-0 new-comment mr-1"
							:height="178"
							no-resize
							v-model="newComment"
						></v-textarea>
						<div>
							<FeaturePhotosManager
								:e="{
									featureType: 'comments',
									inline: true,
									noSave: true,
									customUpload: true,
									small: true
								}"
								:onPhotosCase="onPhotosCase"
								ref="photosManager"
							/>
						</div>
					</div>
					<v-btn
						color="blue darken-1"
						text
						v-on:click="sendComment"
						outlined
						small
						class="mt-2"
						:disabled="!(newComment || attachmentsExist)"
						:loading="newCommentSending"
					>
						Siųsti komentarą
					</v-btn>
				</div>
			</div>
		</template>
		<template v-else>
			<template v-if="loading">
				<v-progress-circular
					indeterminate
					color="primary"
					:size="30"
					width="2"
				></v-progress-circular>
			</template>
		</template>
	</div>
</template>

<script>
	import AttachmentTransferButton from "./AttachmentTransferButton";
	import CommonHelper from "./helpers/CommonHelper";
	import FeaturePhotosManager from "./FeaturePhotosManager";
	import TaskHelper from "./helpers/TaskHelper";

	export default {
		data: function(){
			var data = {
				loading: false,
				comments: null,
				newComment: null,
				letCommentsRefresh: false,
				newCommentSending: false,
				attachmentsExist: false
			};
			return data;
		},

		props: {
			feature: Object
		},

		mounted: function(){
			this.getComments();
		},

		components: {
			AttachmentTransferButton,
			FeaturePhotosManager
		},

		methods: {
			getComments: function(){
				this.loading = true;
				this.comments = null;
				TaskHelper.getComments(this.feature.getProperties()).then(function(result){
					var comments,
						token;
					if (result) {
						comments = result.comments;
						if (comments) {
							comments.forEach(function(comment){
								comment.created = CommonHelper.getPrettyDate(new Date(comment.created), true);
								// TODO: komentare dar reiktų atpažinti URL? Ir jį vaizduoti kaip paspaudžiamą nuorodą?..
							});
						}
						token = result.token;
					}
					this.comments = comments;
					this.token = token;
					this.loading = false;
				}.bind(this), function(){
					this.comments = "error";
					this.loading = false;
				}.bind(this));
			},
			getAttachmentUrl: function(url){
				url = CommonHelper.webServicesRoot + "tasks/get-task-comment-attachment-from-einpix?url=" + url + "&token=" + this.token;
				return url;
			},
			showImage: function(attachment){
				window.open(this.getAttachmentUrl(attachment.url), "_blank");
			},
			downloadAttachment: function(attachment){
				window.open(this.getAttachmentUrl(attachment.url), "_blank");
			},
			sendComment: function(){
				var doSendComment = function(attachmentIds){
					this.newCommentSending = true;
					TaskHelper.addNewComment(this.feature.getProperties(), this.newComment, attachmentIds, this.token).then(function(response){
						if (response && response.status == "OK") {
							this.getComments();
							this.newComment = null;
						}
						this.newCommentSending = false;
					}.bind(this), function(){
						this.newCommentSending = false;
						this.$vBus.$emit("show-message", {
							type: "warning"
						});
					}.bind(this));
				}.bind(this);
				if (this.$refs.photosManager && this.$refs.photosManager.isAnythingToSave() && this.$refs.photosManager.photos) {
					this.newCommentSending = true;
					var showCommentAttachmentsErrorMessage = function(){
						this.$vBus.$emit("show-message", {
							type: "warning",
							message: "Komentaras nebuvo išsaugotas, nes nepavyko išsaugoti įkeltų komentaro failų!",
							timeout: 7000
						});
					}.bind(this);
					var promises = [];
					this.$refs.photosManager.photos.forEach(function(attachment){
						promises.push(TaskHelper.uploadTaskCommentAttachment(attachment, this.token));
					}.bind(this));
					promises = Promise.allSettled(promises);
					promises.then(function(values){
						var allFulfilled = true,
							attachmentIds = [];
						values.forEach(function(v){
							if (v.status == "fulfilled") {
								if (v.value && v.value.id) {
									attachmentIds.push(v.value.id);
								}
							} else {
								allFulfilled = false;
							}
						});
						if (allFulfilled) {
							this.newCommentSending = false;
							doSendComment(attachmentIds);
						} else {
							this.newCommentSending = false;
							showCommentAttachmentsErrorMessage();
						}
					}.bind(this), function(){
						this.newCommentSending = false;
						showCommentAttachmentsErrorMessage();
					}.bind(this));
				} else {
					doSendComment();
				}
			},
			onPhotosCase: function(){
				var attachmentsExist;
				if (this.$refs.photosManager) {
					attachmentsExist = this.$refs.photosManager.isAnythingToSave();
				}
				this.attachmentsExist = attachmentsExist;
			}
		}
	}
</script>

<style scoped>
	.comments {
		width: 100%;
		border-spacing: 0;
		border-collapse: collapse;
	}
	.comments, .comment {
		border: 1px solid #dedede;
	}
	.new-comment {
		max-width: 800px;
		min-width: 400px;
	}
	.v-image {
		border: 1px solid #cccccc;
		cursor: pointer;
	}
	.img-wrapper {
		position: relative;
	}
	.img-wrapper .v-btn {
		position: absolute;
		bottom: 0;
	}
</style>