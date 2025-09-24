<template>
	<div>
		<v-tooltip bottom>
			<template v-slot:activator="{on, attrs}">
				<v-btn
					:width="40"
					:height="40"
					min-width="auto"
					:elevation="2"
					:color="needsAttention ? 'orange' : (active ? 'rgba(255, 255, 255, 1)' : color ? color : 'rgba(255, 255, 255, 0.8)')"
					v-on:click="clickCallback"
					v-bind="attrs"
					v-on="on"
					class="rounded-circle"
					:disabled="disabled"
				>
					<v-icon :color="color ? 'white' : 'black'">{{icon}}</v-icon>
				</v-btn>
			</template>
			<span>{{title}}</span>
		</v-tooltip>
	</div>
</template>

<script>
	export default {
		data: function(){
			var data = {
				active: false,
				needsAttention: false,
				disabled: false
			};
			return data;
		},

		props: {
			title: String,
			icon: String,
			color: String,
			clickCallback: Function,
			activeChangeCallback: Function,
			disableWhenInteractionsCount: Boolean
		},

		watch: {
			active: {
				immediate: true,
				handler: function(active){
					if (this.activeChangeCallback) {
						this.activeChangeCallback(active);
					}
				}
			},
			"$store.state.myMap.interactionsCount": {
				immediate: true,
				handler: function(interactionsCount){
					if (this.disableWhenInteractionsCount) {
						// Čia gan įdomus funkcionalumas... Reiktų jį išplėtoti...
						this.disabled = Boolean(interactionsCount);
					}
				}
			}
		}
	}
</script>