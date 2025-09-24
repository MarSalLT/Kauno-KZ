<template>
	<v-dialog
		persistent
		max-width="600"
		v-model="dialog"
	>
		<v-card>
			<v-card-title>
				<span>Pasibaigė sesija</span>
			</v-card-title>
			<v-card-text class="pb-0 pt-1">
				Kadangi kurį laiką šiame puslapyje neatlikote jokių veiksmų, pasibaigė jūsų sesija, t. y. jūs buvote atjungtas.
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="reload"
					outlined
					small
				>
					Prisijungti iš naujo
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	export default {
		data: function(){
			var data = {
				dialog: false
			};
			return data;
		},

		created: function(){
			this.$vBus.$on("show-session-expired-dialog", this.showDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-session-expired-dialog", this.showDialog);
		},

		methods: {
			showDialog: function(){
				this.dialog = true;
			},
			reload: function(){
				window.location.reload();
			}
		}
	}
</script>