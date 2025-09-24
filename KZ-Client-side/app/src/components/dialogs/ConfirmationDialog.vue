<template>
	<v-dialog
		persistent
		max-width="600"
		v-model="dialog"
	>
		<v-card v-if="e">
			<v-card-title>
				<span>{{e.title}}</span>
			</v-card-title>
			<v-card-text class="pb-0 pt-1">
				<div>{{e.message}}</div>
				<template v-if="e.textarea">
					<div>
						<v-textarea
							:placeholder="e.textarea.placeholder"
							dense
							hide-details
							outlined
							class="plain mt-4 body-2 pa-0"
							:height="150"
							no-resize
							v-model="message"
						></v-textarea>
					</div>
				</template>
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="positiveAction"
					outlined
					small
					:loading="loading"
				>
					{{e.positiveActionTitle}}
				</v-btn>
				<v-btn
					color="blue darken-1"
					text
					v-on:click="negativeAction"
					outlined
					small
					:disabled="loading"
				>
					{{e.negativeActionTitle}}
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	export default {
		data: function(){
			var data = {
				dialog: false,
				e: null,
				loading: null,
				message: null
			};
			return data;
		},

		created: function(){
			this.$vBus.$on("confirm", function(e){
				this.e = e;
				this.dialog = true;
				this.loading = false;
				this.message = null;
			}.bind(this));
		},

		methods: {
			positiveAction: function(){
				if (!this.e.delayedPositive) {
					this.dialog = false;
				}
				if (this.e.positive) {
					this.e.positive(this);
				}
			},
			negativeAction: function(){
				this.dialog = false;
				if (this.e.negative) {
					this.e.negative();
				}
			}
		}
	}
</script>