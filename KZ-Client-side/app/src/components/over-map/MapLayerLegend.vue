<template>
	<div>
		<MyHeading
			:value="(layer.service ? layer.service.title : '')"
			:collapseAndExpandHandler="collapseAndExpandHandler"
			:initiallyCollapsed="collapsed"
		>
		</MyHeading>
		<v-expand-transition>
			<v-card
				v-show="!collapsed"
				flat
				class="mt-2 body-2 ml-1"
			>
				<template v-if="loading">
					<v-progress-circular
						indeterminate
						color="primary"
						:size="25"
						width="2"
					></v-progress-circular>
				</template>
				<template v-else>
					<template v-if="legend">
						<template v-if="Array.isArray(legend)">
							<MapLayerLegendItem :legendItems="legend" :level="0" />
						</template>
					</template>
					<template v-else>
						Legendos nėra...
					</template>
				</template>
			</v-card>
		</v-expand-transition>
	</div>
</template>

<script>
	import ImageLayer from "ol/layer/Image";
	import LayerGroup from "ol/layer/Group";
	import MapHelper from "../helpers/MapHelper";
	import MapLayerLegendItem from "./MapLayerLegendItem";
	import MyHeading from "../MyHeading";

	export default {
		data: function(){
			var data = {
				collapsed: false,
				loading: true,
				legend: null,
				rendererField: null
			};
			return data;
		},

		props: {
			layer: Object
		},

		components: {
			MapLayerLegendItem,
			MyHeading
		},

		mounted: function(){
			if (this.layer.service) {
				if (this.layer instanceof ImageLayer) {
					// https://developers.arcgis.com/rest/services-reference/enterprise/legend-map-service-.htm
					MapHelper.getLegend(this.layer).then(function(legend){
						this.legend = legend;
						this.loading = false;
					}.bind(this), function(){
						this.loading = false;
					}.bind(this));
				} else if (this.layer instanceof LayerGroup) {
					var legend;
					console.log("VECTOR LEGEND", legend); // TODO...
					this.legend = legend;
					this.loading = false;
				} else {
					// ...
				}
				this.layer.on("custom-renderer-field-change", function(){
					this.rendererField = this.layer.get("custom-renderer-field");
				}.bind(this));
			}
		},

		methods: {
			collapseAndExpandHandler: function(collapsed){
				this.collapsed = collapsed;
			},
			modLabel: function(label){
				var labelSpl = label.split("-");
				if (labelSpl.length == 2) {
					labelSpl.forEach(function(item, i){
						labelSpl[i] = Number.parseFloat(item.trim()).toFixed(3);
					});
					label = labelSpl.join(" - ");
				}
				return label;
			}
		}
	};
</script>