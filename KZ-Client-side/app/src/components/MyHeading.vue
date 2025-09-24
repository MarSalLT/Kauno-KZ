<template>
	<div class="pa-1 d-flex align-center body-2">
		<span class="flex-grow-1">
			{{value}}
			<slot name="additional"></slot>
		</span>
		<template v-if="collapseAndExpandHandler">
			<v-btn
				icon
				v-on:click="collapsed = !collapsed"
				x-small
			>
				<template v-if="collapsed">
					<v-icon title="Išskleisti">mdi-menu-down</v-icon>
				</template>
				<template v-else>
					<v-icon title="Suskleisti">mdi-menu-up</v-icon>
				</template>
			</v-btn>
		</template>
	</div>
</template>

<script>
	export default {
		data: function(){
			var data = {
				collapsed: Boolean(this.initiallyCollapsed)
			};
			return data;
		},

		props: {
			value: String,
			collapseAndExpandHandler: Function,
			initiallyCollapsed: Boolean
		},

		watch: {
			collapsed: {
				immediate: true,
				handler: function(collapsed){
					if (this.collapseAndExpandHandler) {
						this.collapseAndExpandHandler(collapsed);
					}
				}
			}
		}
	};
</script>

<style scoped>
	div {
		background-color: #e2e2e2;
	}
</style>