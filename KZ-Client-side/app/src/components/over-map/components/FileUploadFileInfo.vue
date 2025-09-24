<template>
	<div class="d-flex align-center" v-if="file">
		<div>{{file.file.name}}</div>
		<v-progress-circular
			indeterminate
			color="primary"
			:size="20"
			width="2"
			class="ml-2"
			v-if="!file.finished"
		></v-progress-circular>
		<v-icon
			title="Atsiprašome, įvyko nenumatyta klaida..."
			color="error"
			v-if="file.error"
			class="ml-2 mr-1"
			:size="20"
		>
			mdi-alert
		</v-icon>
		<v-btn
			icon
			small
			class="ml-1"
			v-on:click="zoomToLayer(file)"
			v-if="file.layer && zoomToLayer"
		>
			<v-icon
				title="Rodyti aprėptį"
				:size="18"
				color="primary"
			>
				mdi-magnify-plus
			</v-icon>
		</v-btn>
		<v-btn
			icon
			small
			class="ml-1"
			v-on:click="remove"
		>
			<v-icon
				title="Pašalinti"
				:size="18"
				color="error"
			>
				mdi-delete
			</v-icon>
		</v-btn>
	</div>
</template>

<script>
	export default {
		data: function(){
			var data = {
				// ...
			};
			return data;
		},

		props: {
			file: Object,
			onRemove: Function,
			zoomToLayer: Function
		},

		methods: {
			remove: function(){
				if (this.onRemove) {
					this.onRemove(this.file);
				}
			}
		}
	}
</script>