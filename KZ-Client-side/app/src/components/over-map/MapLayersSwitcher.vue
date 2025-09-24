<template>
	<v-menu
		open-on-hover
		:close-on-content-click="false"
		tile
		right
		v-if="myMap"
		:eager="true"
		content-class="map-layers-menu"
		class="overflow-auto"
	>
		<template v-slot:activator="{on, attrs}">
			<v-btn
				fab
				small
				title="Žemėlapio sluoksniai"
				v-on="on"
				v-bind="attrs"
				:elevation="2"
				color="rgba(255, 255, 255, 0.8)"
			>
				<v-icon color="black">mdi-layers</v-icon>
			</v-btn>
		</template>
		<MapLayersList />
	</v-menu>
</template>

<script>
	import MapLayersList from "../MapLayersList";

	export default {
		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		components: {
			MapLayersList
		}
	}
</script>

<style scoped>
	.map-layers-menu {
		max-height: calc(100% - 70px); /* FIXME! Ne pats geriausias sprendimas */
	}
</style>