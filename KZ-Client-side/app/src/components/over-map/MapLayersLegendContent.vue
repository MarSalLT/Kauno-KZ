<template>
	<OverMapButtonContent
		type="legend"
		:title="title"
		:btn="btn"
		:setBtnActive="setBtnActive"
		:onOpen="onOpen"
		ref="wrapper"
	>
		<template v-slot>
			<div>
				<template v-if="layers && layers.length">
					<template v-for="(layer, i) in layers">
						<div :key="layer.service.id" :class="i ? 'mt-3' : null">
							<keep-alive :name="layer.service.id">
								<MapLayerLegend
									:layer="layer"
								/>
							</keep-alive>
						</div>
					</template>
				</template>
				<template v-else>
					Sluoksnių nėra...
				</template>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import MapLayerLegend from "./MapLayerLegend";
	import OverMapButtonContent from "./OverMapButtonContent";

	export default {
		data: function(){
			var data = {
				title: null,
				btn: null,
				setBtnActive: null,
				layers: null
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		components: {
			MapLayerLegend,
			OverMapButtonContent
		},

		created: function(){
			this.$vBus.$on("show-or-hide-legend", this.showOrHide);
			this.$vBus.$on("layers-reordered", this.redrawList);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-or-hide-legend", this.showOrHide);
			this.$vBus.$off("layers-reordered", this.redrawList);
		},

		methods: {
			showOrHide: function(e){
				this.title = e.title;
				this.btn = e.btn;
				this.setBtnActive = e.setBtnActive;
				if (this.btn) {
					this.$refs.wrapper.toggle(this.btn.$el.offsetLeft);
				}
			},
			onOpen: function(){
				// ...
			},
			redrawList: function(){
				var layers = [];
				this.myMap.map.getLayers().forEach(function(layer){
					if (layer.service && layer.service.showLegend) {
						layers.push(layer);
					}
				});
				layers.sort(function(a, b){
					if (a.getZIndex() < b.getZIndex()) {
						return 1;
					}
					if (a.getZIndex() > b.getZIndex()) {
						return -1;
					}
					return 0;
				});
				this.layers = layers;
			}
		},

		watch: {
			myMap: {
				immediate: true,
				handler: function(myMap){
					if (myMap) {
						if (myMap.map) {
							myMap.map.getLayers().on("change:length", function(){
								this.redrawList();
							}.bind(this));
							// TODO: po reorder'inimo irgi perpiešti
						}
					}
				}
			}
		}
	}
</script>