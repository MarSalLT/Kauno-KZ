<template>
	<OverMapButton
		title="Įkelti DWG failą"
		icon="mdi-file-upload"
		:clickCallback="onClick"
		ref="btn"
	/>
</template>

<script>
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "DWG failo įkėlimas"
			};
			return data;
		},

		components: {
			OverMapButton
		},

		methods: {
			onClick: function(){
				this.$vBus.$emit("show-or-hide-file-upload", {
					title: this.title,
					btn: this.$refs.btn
				});
			}
		},

		watch: {
			"$store.state.myMap.uploadedFiles": {
				handler: function(uploadedFiles){
					this.$refs.btn.needsAttention = Boolean(uploadedFiles);
				}
			}
		}
	}
</script>