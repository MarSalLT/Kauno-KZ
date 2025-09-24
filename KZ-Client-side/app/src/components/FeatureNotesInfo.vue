<template>
	<div v-if="items">
		<MyHeading
			value="Pastabos:"
		/>
		<AttributesTable
			:itemsComputed="items"
			:editingActive="editingActive"
			ref="attributesTable"
		/>
	</div>
</template>

<script>
	import AttributesTable from "./AttributesTable";
	import MyHeading from "./MyHeading";

	export default {
		data: function(){
			var data = {
				items: this.getItems()
			};
			return data;
		},

		props: {
			data: Object,
			editingActive: Boolean
		},

		components: {
			AttributesTable,
			MyHeading
		},

		methods: {
			getItems: function(){
				var items;
				if (this.data) {
					if (this.data.featureType) {
						var featureFields = ["PASTABOS", "KOMENTARAI"];
						if (this.data.featureType == "verticalStreetSigns") {
							featureFields = ["PASTABA", "KOMENTARAI"];
						}
						items = [];
						var myMap = this.$store.state.myMap,
							layerFields = myMap.getLayerFields(this.data.featureType),
							field,
							item;
						if (layerFields) {
							featureFields.forEach(function(key){
								field = myMap.getLayerField(layerFields, key);
								if (field) {
									item = myMap.getValueItems(field, this.data.feature.get(key));
									item.title = myMap.getFieldTitle(field);
									items.push(item);
								}
							}.bind(this));
						}
						items = {
							items: items
						}
					}
				}
				return items;
			},
			getFormData: function(){
				var formData;
				if (this.$refs.attributesTable) {
					formData = this.$refs.attributesTable.getFormData();
				}
				return formData || {};
			}
		}
	};
</script>