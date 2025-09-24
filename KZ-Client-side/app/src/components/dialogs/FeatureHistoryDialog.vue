<template>
	<v-dialog
		v-model="dialog"
		scrollable
	>
		<v-card>
			<v-card-title>
				<span>Objekto redagavimo istorija</span>
			</v-card-title>
			<v-card-text class="pb-0">
				<template v-if="e && e.fields">
					<div class="table-wrapper" ref="content">
						<table class="body-2 my-table">
							<tr>
								<th
									class="px-2 py-1 font-weight-black bolder-background"
								>
									<span>Objektas aktualus (nuo, iki)</span>
								</th>
								<template v-for="(feature, i) in e.features">
									<td
										:key="'f-l-' + i"
										class="px-2 py-1"
									>
										<span>
											<span>{{feature.gdbFromDate.valuePretty}}</span>
											<v-divider class="my-1"></v-divider>
											<span v-html="feature.gdbToDate.valuePretty"></span>
										</span>
									</td>
								</template>
							</tr>
							<template v-for="(field, i) in e.fields">
								<tr :key="'h-' + i">
									<th
										class="px-2 py-1 font-weight-medium bolder-background"
									>
										<span>{{field.title}}</span>
									</th>
									<template v-for="(feature, j) in e.features">
										<td
											:key="'f-' + j"
											class="px-2 py-1"
										>
											<span
												v-if="feature[field.name] && feature[field.name].v"
												:class="feature[field.name].v.changed ? 'changed' : null"
											>{{feature[field.name].v.valuePretty}}</span>
										</td>
									</template>
								</tr>
							</template>
							<tr>
								<th
									class="px-2 py-1 font-weight-black bolder-background"
								>
									<span>Geometrija keitėsi?</span>
								</th>
								<template v-for="(feature, i) in e.features">
									<td
										:key="'f-g-' + i"
										class="px-2 py-1"
									>
										<span
											v-if="feature.geometryChanged"
										>
											<v-icon
												size="18"
												color="success"
											>
												mdi-check-circle
											</v-icon>
										</span>
									</td>
								</template>
							</tr>
						</table>
					</div>
				</template>
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="dialog = false"
					outlined
					small
				>
					Uždaryti
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";

	export default {
		data: function(){
			var data = {
				dialog: false,
				e: null
			};
			return data;
		},

		created: function(){
			this.$vBus.$on("show-feature-history-dialog", this.showDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-feature-history-dialog", this.showDialog);
		},

		methods: {
			showDialog: function(e){
				var myMap = this.$store.state.myMap,
					layerFields = myMap.getLayerFields(e.featureType),
					featureFields = CommonHelper.importantFields[e.featureType],
					layerInfo = myMap.getLayerInfo(e.featureType) || {};
				if (e.data && e.data.type == "vvt") {
					layerInfo = myMap.getLayerInfo(e.data.featureType, e.data.layerId) || {};
					layerFields = layerInfo.fields;
					if (layerFields) {
						featureFields = [];
						layerFields.forEach(function(layerField){
							featureFields.push(layerField.name);
						});
					}
				}
				if (layerFields && featureFields) {
					featureFields = featureFields.slice();
					featureFields.push(CommonHelper.customEditorFieldName);
					featureFields.push("last_edited_user");
					var fields = [],
						field;
					featureFields.forEach(function(key){
						field = myMap.getLayerField(layerFields, key);
						if (field) {
							fields.push({
								title: myMap.getFieldTitle(field),
								name: field.name
							});
						}
					});
					var dateFieldsTimeReference = layerInfo.dateFieldsTimeReference;
					var features = [],
						feature,
						v;
					e.historyFeatures.forEach(function(historyFeature, i){
						feature = {};
						featureFields.forEach(function(key){
							field = myMap.getLayerField(layerFields, key);
							if (field) {
								// Pvz. su http://localhost:3001/admin//?t=vvt&l=5&id=5510
								v = myMap.getValueItems(field, historyFeature.attributes[field.name], dateFieldsTimeReference); // TODO... Jei tai redagavimo laukai, tai pateikti kitokį `dateFieldsTimeReference`?..
								feature[field.name] = {
									v: v
								};
								if (i) {
									var noChangeFields = ["GDB_FROM_DATE", "GDB_TO_DATE"];
									if (layerInfo && layerInfo.editFieldsInfo) {
										noChangeFields.push(layerInfo.editFieldsInfo.creationDateField);
										noChangeFields.push(layerInfo.editFieldsInfo.editDateField);
									}
									if (noChangeFields.indexOf(field.name) == -1) {
										if (v.value != features[i - 1][field.name].v.value) {
											feature[field.name].v.changed = true;
										}
									}
								}
							}
						});
						feature.gdbFromDate = myMap.getValueItems({type: "esriFieldTypeDate"}, historyFeature.attributes["GDB_FROM_DATE"], dateFieldsTimeReference);
						feature.gdbToDate = myMap.getValueItems({type: "esriFieldTypeDate"}, historyFeature.attributes["GDB_TO_DATE"], dateFieldsTimeReference);
						if (i) {
							if (JSON.stringify(historyFeature.geometry) != JSON.stringify(e.historyFeatures[i - 1].geometry)) {
								feature.geometryChanged = true;
							}
						}
						features.push(feature);
					});
					this.e = {
						fields: fields,
						features: features
					};
					this.dialog = true;
				}
			}
		},

		watch: {
			dialog: {
				immediate: true,
				handler: function(open){
					if (open) {
						if (this.$refs.content) {
							setTimeout(function(){
								this.$refs.content.scrollTop = 0;
								this.$refs.content.scrollLeft = 0;
							}.bind(this), 0);
						}
					}
				}
			}
		}
	}
</script>

<style scoped>
	.changed {
		background-color: #f1ffa2;
	}
</style>