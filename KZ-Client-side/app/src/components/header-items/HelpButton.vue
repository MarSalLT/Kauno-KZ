<template>
	<div>
		<v-btn
			icon
			small
			:title="title"
			v-on:click="dialog = true"
		>
			<v-icon
				color="grey"
				size="24"
			>
				mdi-help-circle
			</v-icon>
		</v-btn>
		<v-dialog
			persistent
			v-model="dialog"
			content-class="help-dialog"
			scrollable
		>
			<v-card>
				<v-card-title>
					<span>{{title}}</span>
				</v-card-title>
				<v-card-text class="pb-1 pt-4">
					<div
						class="content d-flex flex-column"
					>
						Kuriama...
					</div>
				</v-card-text>
				<v-card-actions class="mx-2 pb-5 pt-5">
					<v-btn
						color="blue darken-1"
						text
						v-on:click="dialog = false"
						outlined
						small
					>
						Atšaukti
					</v-btn>
				</v-card-actions>
			</v-card>
		</v-dialog>
	</div>
</template>

<script>
	export default {
		data: function(){
			var data = {
				title: "Pagalba",
				dialog: false
			};
			return data;
		}
	}
</script>

<style scoped>
	.content {
		height: 100%;
	}
</style>