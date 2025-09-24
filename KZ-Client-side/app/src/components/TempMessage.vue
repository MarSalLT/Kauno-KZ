<template>
	<v-snackbar
		v-model="open"
		:timeout="timeout"
		:color="color"
	>
		{{message}}
	</v-snackbar>
</template>

<script>
	export default {
		data: function(){
			var data = {
				open: false,
				message: null,
				color: null,
				timeout: null
			};
			return data;
		},

		created: function(){ // Jei naudojamas beforeCreate — klaida?..
			this.$vBus.$on("show-message", this.showMessage);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-message", this.showMessage);
		},

		methods: {
			showMessage: function(e){
				if (e) {
					var message = e.message,
						color,
						warning = Boolean(e.type == "warning");
					if (warning) {
						color = "error";
					} else if (e.type == "success") {
						color = "success";
					}
					if (!message && warning) {
						message = "Atsiprašome, įvyko nenumatyta klaida...";
					}
					this.open = true;
					this.message = message;
					this.color = color;
					var timeout = e.timeout || 2000;
					if (e.indefinite) {
						timeout = -1;
					}
					this.timeout = timeout;
				}
			}
		}
	};
</script>