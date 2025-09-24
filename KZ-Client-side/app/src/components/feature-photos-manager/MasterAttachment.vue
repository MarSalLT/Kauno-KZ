<template>
	<v-row
		no-gutters
		justify="center"
		v-on:click="onClick"
	>
		<div
			class="d-flex flex-column justify-center wrapper"
		>
			<v-icon
				:color="disabled ? 'grey' : 'success'" 
				size="75"
			>
				mdi-file-image-plus
			</v-icon>
			<span class="text-center mt-1">
				<template v-if="disabled">
					Viršytas leistinų pridėti failų limitas...
				</template>
				<template v-else>
					Kurkite naują brėžinį/schemą
				</template>
			</span>
		</div>
	</v-row>
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
			addPhotos: Function,
			disabled: Boolean
		},

		computed: {
			activeTask: {
				get: function(){
					var activeTask = null;
					if (this.$store.state.activeTask) {
						var feature = this.$store.state.activeTask.feature;
						if (feature && feature != "error") {
							activeTask = this.$store.state.activeTask;
						}
					}
					return activeTask;
				}
			}
		},

		methods: {
			onClick: function(){
				if (!this.disabled) {
					this.$vBus.$emit("task-attachment-new", {
						task: this.activeTask,
						masterAttachment: true
					});
				}
			}
		}
	};
</script>

<style scoped>
	.row {
		height: 100%;
	}
	.wrapper {
		pointer-events: none;
	}
</style>