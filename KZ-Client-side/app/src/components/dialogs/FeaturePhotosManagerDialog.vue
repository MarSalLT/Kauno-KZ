<template>
	<v-dialog
		persistent
		v-model="dialog"
		content-class="feature-photos-manager-dialog"
		scrollable
	>
		<FeaturePhotosManager
			v-if="e"
			:e="e"
			:key="key"
			:onClose="onClose"
		/>
	</v-dialog>
</template>

<script>
	import FeaturePhotosManager from "../FeaturePhotosManager";

	export default {
		data: function(){
			var data = {
				dialog: false,
				e: null,
				key: 0
			};
			return data;
		},

		created: function(){
			this.$vBus.$on("show-feature-photos-manager-dialog", this.showDialog);
			this.$vBus.$on("refresh-feature-photos-manager-dialog", this.refreshDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-feature-photos-manager-dialog", this.showDialog);
			this.$vBus.$off("refresh-feature-photos-manager-dialog", this.refreshDialog);
		},

		components: {
			FeaturePhotosManager
		},

		methods: {
			showDialog: function(e){
				this.e = e;
				this.key += 1;
				this.dialog = true;
				// Hmmmm... Jei feature'as dalyvavo užduotyje, tai rodyti papildomą suskleistą komponentą, kuriame būtų užduoties attachment'ai??... Ir kurios būtų galima persikelti objektus į FeaturePhotosManager'į?..
			},
			refreshDialog: function(){
				this.key += 1;
			},
			onClose: function(){
				this.dialog = false;
			}
		}
	}
</script>