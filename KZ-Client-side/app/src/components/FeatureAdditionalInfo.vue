<template>
	<div v-if="items">
		<MyHeading
			value="Papildoma objekto informacija:"
		/>
		<AttributesTable
			:itemsComputed="items"
		/>
	</div>
</template>

<script>
	import AttributesTable from "./AttributesTable";
	import CommonHelper from "../components/helpers/CommonHelper";
	import MyHeading from "./MyHeading";
	import LineString from "ol/geom/LineString";
	import Polygon from "ol/geom/Polygon";

	export default {
		data: function(){
			var data = {
				items: this.getItems()
			};
			return data;
		},

		props: {
			data: Object
		},

		components: {
			AttributesTable,
			MyHeading
		},

		methods: {
			getItems: function(){
				var items;
				if (this.data) {
					if (this.data.featureType && ["horizontalPoints", "horizontalPolylines", "horizontalPolygons"].indexOf(this.data.featureType) != -1) {
						items = [{
							title: "Uždažytas plotas",
							valuePretty: this.getPaintedAreaValue(this.data) || "?"
						}];
						var geometryType = this.data.feature.getGeometry(),
							length = this.data.feature.get(CommonHelper.defaultLengthFieldName) || this.data.feature.get("SHAPE.STLength()") || 0.0;
						if (geometryType instanceof Polygon) {
							var area = this.data.feature.get(CommonHelper.defaultAreaFieldName) || this.data.feature.get("SHAPE.STArea()") || 0.0;
							items.push({
								title: "Plotas",
								valuePretty: area.toFixed(2) + " m<sup>2</sup>"
							});
							items.push({
								title: "Perimetras",
								valuePretty: length.toFixed(2) + " m"
							});
						} else if (geometryType instanceof LineString) {
							items.push({
								title: "Ilgis",
								valuePretty: length.toFixed(2) + " m"
							});
						}
						items = {
							items: items
						}
					}
				}
				return items;
			},
			getPaintedAreaValue: function(data){
				var paintedAreaValue = CommonHelper.getPaintedArea(data, this.$store.state.myMap);
				if (paintedAreaValue) {
					paintedAreaValue = paintedAreaValue.toFixed(2) + " m<sup>2</sup>";
				}
				return paintedAreaValue;
			}
		}
	};
</script>