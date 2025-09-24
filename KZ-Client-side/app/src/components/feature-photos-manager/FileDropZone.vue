<template>
	<v-row
		no-gutters
		justify="center"
		:class="dragover ? 'active' : null"
	>
		<div
			class="d-flex flex-column justify-center wrapper"
		>
			<v-icon
				:color="disabled ? 'grey' : (dragover ? 'primary darken-2' : 'primary')" 
				:size="small ? 40 : 75"
			>
				mdi-image-plus
			</v-icon>
			<span class="text-center mt-1">
				<template v-if="disabled">
					Viršytas leistinų pridėti failų limitas...
				</template>
				<template v-else>
					Spustelkite failo pasirinkimui<br />arba nutempkite failą čia
				</template>
			</span>
		</div>
		<input 
			type="file"
			:accept="accept || 'image/*'" 
			class="d-none"
			ref="input"
		/>
	</v-row>
</template>

<script>
	export default {
		data: function(){
			var data = {
				dragover: false
			};
			return data;
		},

		props: {
			addPhotos: Function,
			accept: String,
			disabled: Boolean,
			small: Boolean
		},

		mounted: function(){
			// https://medium.com/swlh/drop-and-click-file-upload-with-vuetifyjs-f2c2a8357377
			var dropZone = this.$el;
			if (dropZone) {
				dropZone.addEventListener("dragenter", function(e){
					e.preventDefault();
					this.dragover = true;
				}.bind(this));
				dropZone.addEventListener("dragleave", function(e){
					e.preventDefault();
					this.dragover = false;
				}.bind(this));
				dropZone.addEventListener("dragover", function(e){
					e.preventDefault();
					this.dragover = true;
				}.bind(this));
				dropZone.addEventListener("drop", function(e){
					e.preventDefault();
					this.dragover = false;
					this.onFilesSelected(e.dataTransfer.files);
				}.bind(this));
			}
			var input = this.$refs.input;
			if (input) {
				dropZone.addEventListener("click", function(){
					if (input && !this.disabled) {
						input.click();
					}
				}.bind(this));
				input.addEventListener("change", function(e){
					this.onFilesSelected(e.target.files);
					input.value = "";
				}.bind(this));
			}
		},

		methods: {
			onFilesSelected: function(files){
				if (this.addPhotos) {
					this.addPhotos(files);
				}
			}
		}
	};
</script>

<style scoped>
	.row {
		height: 100%;
	}
	.active {
		background-color: #efefef;
	}
	.wrapper {
		pointer-events: none;
	}
</style>