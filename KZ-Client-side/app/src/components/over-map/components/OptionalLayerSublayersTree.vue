<template>
	<div>
		<template v-if="sublayers">
			<template v-for="(sublayer, i) in sublayers">
				<OptionalLayerSublayerCheckbox
					:layer="sublayer"
					:key="sublayer && sublayer.service && sublayer.service.id ? sublayer.service.id : i"
				/>
			</template>
		</template>
		<template v-else-if="sublayersTree && sublayersTree.children">
			<template v-if="sublayersTreeAsTreeView">
				<v-treeview
					v-model="selected"
					:items="sublayersTree.children"
					:selectable="true"
					selection-type="leaf"
					item-disabled="locked"
					dense
					:class="sublayersTreeOneLevel ? 'ml-n10' : 'ml-n4'"
				>
					<template v-slot:label="{item}">
						<div :title="item.name">{{item.name}}</div>
					</template>
				</v-treeview>
			</template>
			<template v-else>
				<template v-for="(branch) in sublayersTree.children">
					<OptionalImageLayerSublayersTreeBranch
						:service="service"
						:data="branch"
						:level="0"
						:key="branch.id"
					/>
				</template>
			</template>
		</template>
	</div>
</template>

<script>
	import ImageLayer from "ol/layer/Image";
	import LayerGroup from "ol/layer/Group";
	import OptionalLayerSublayerCheckbox from "./OptionalLayerSublayerCheckbox";
	import OptionalImageLayerSublayersTreeBranch from "./OptionalImageLayerSublayersTreeBranch";

	export default {
		data: function(){
			var data = {
				sublayers: null,
				sublayersTree: null,
				sublayersTreeAsTreeView: true,
				sublayersTreeOneLevel: true,
				selected: []
			};
			return data;
		},

		props: {
			service: Object
		},

		components: {
			OptionalLayerSublayerCheckbox,
			OptionalImageLayerSublayersTreeBranch
		},

		mounted: function(){
			if (this.service.layer) {
				if (this.service.layer instanceof ImageLayer) {
					var sublayersTree = {
						children: []
					};
					var selected = [],
						sublayersTreeOneLevel = true;
					if (this.service.showLayers) {
						if (this.service.capabilities && this.service.capabilities.esri) {
							var layersInfo = this.service.capabilities.esri.layers;
							if (layersInfo) {
								var layersInfoTree = {};
								layersInfo.forEach(function(layerInfo){
									layersInfoTree[layerInfo.id] = layerInfo;
									if (layerInfo.subLayerIds) {
										sublayersTreeOneLevel = false;
									}
								});
							}
							this.setChildren(sublayersTree, this.service.showLayers, layersInfoTree, selected);
						}
					}
					this.sublayersTree = sublayersTree;
					this.sublayersTreeOneLevel = sublayersTreeOneLevel;
					if (this.service.layer.getSource().getParams) {
						var sourceParams = this.service.layer.getSource().getParams();
						if (sourceParams && typeof sourceParams["layers"] !== "undefined") {
							// Jei sluoksnis turi atributą, nusakantį kokius sublayer'ius jam rodyti, tai medžio checkbox'ų vaizdavimą pagal tai ir rodome, o ne pagal numatytąsias reikšmes...
							selected = sourceParams["layers"].replace("show:", "").split(",");
							if (selected == "-1") {
								selected = [];
							}
						}
					}
					this.selected = selected;
				} else if (this.service.layer instanceof LayerGroup) {
					this.setSublayers();
					this.service.layer.getLayers().on("add", function(){
						this.setSublayers();
					}.bind(this));
				}
			}
		},

		methods: {
			setSublayers: function(){
				var sublayers = [];
				this.service.layer.getLayers().forEach(function(innerLayer){
					sublayers.push(innerLayer);
				});
				sublayers.sort(function(a, b){
					if (a.service && b.service) {
						if (a.service.zIndex < b.service.zIndex) {
							return 1;
						}
						if (a.service.zIndex > b.service.zIndex) {
							return -1;
						}
					}
					return 0;
				});
				this.sublayers = sublayers;
			},
			setChildren: function(parent, layerIds, layersInfoTree, selected){
				if (layerIds) {
					layerIds.forEach(function(layerId){
						var layerInfo = layersInfoTree[layerId];
						if (layerInfo) {
							var branch = {
								id: layerInfo.id + "",
								name: layerInfo.name,
								children: []
							};
							// O kaip dėl branch.defaultVisibility?
							this.setChildren(branch, layerInfo.subLayerIds, layersInfoTree, selected);
							parent.children.push(branch);
							selected.push(branch.id);
						}
					}.bind(this));	
				}
			}
		},

		watch: {
			selected: {
				immediate: false,
				handler: function(selected){
					if (selected) {
						selected = selected.slice();
						selected.sort();
					}
					this.service.layer.getSource().updateParams({
						"layers": "show:" + selected.join(",")
					});
					this.service.layer.getSource().refresh();
				}
			}
		}
	}
</script>