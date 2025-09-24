<template>
	<div>
		<v-btn
			fab
			dark
			x-small
			color="primary"
			v-on:click.stop.prevent="managePhotos(data)"
			:loading="loading"
		>
			<v-icon dark small>
				mdi-camera-image
			</v-icon>
		</v-btn>
		<v-tooltip
			bottom
			v-if="data.featureType == 'verticalStreetSigns'"
		>
			<template v-slot:activator="{on, attrs}">
				<span
					class="red--text font-weight-bold manage-photos-asterisk ml-2"
					v-bind="attrs"
					v-on="on"
				>
					*
				</span>
			</template>
			<span>Valdomos su KŽ susijusios tvirtinimo vietos nuotraukos</span>
		</v-tooltip>
	</div>
</template>

<script>
	export default {
		data: function(){
			var data = {
				loading: false
			};
			return data;
		},

		props: {
			data: Object,
			activeTask: Object
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		methods: {
			managePhotos: function(data){
				if (data.action == "add") {
					this.$vBus.$emit("show-task-attachments-transfer-dialog", {
						feature: data,
						task: this.activeTask
					});
				} else {
					this.$vBus.$emit("show-task-attachments-transfer-dialog", {
						feature: data,
						task: this.activeTask
					});
				}
			}
		}
	}
</script>

<style scoped>
	.manage-photos-asterisk {
		font-size: 1rem;
		position: absolute;
	}
</style>