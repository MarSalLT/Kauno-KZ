<template>
	<div v-if="viewingAllowed">
		<MyHeading
			:value="title || 'Objekto istorija:'"
			:collapseAndExpandHandler="collapseAndExpandHandler"
			:initiallyCollapsed="initiallyCollapsed"
		>
			<template v-slot:additional>
				<v-btn
					icon
					small
					class="ml-1"
					v-on:click="showFeatureHistoryDialog"
					v-if="items && items != 'error'"
				>
					<v-icon
						title="Rodyti išsamią istoriją"
						:size="18"
					>
						mdi-format-list-bulleted
					</v-icon>
				</v-btn>
			</template>
		</MyHeading>
		<v-expand-transition>
			<v-card
				v-show="!collapsed"
				flat
			>
				<div class="mt-1">
					<template v-if="items && items != 'error'">
						<AttributesTable
							:itemsComputed="items"
						/>
					</template>
					<template v-else>
						<div class="pa-1 bordered-container">
							<template v-if="items">
								<template v-if="items == 'error'">
									<v-alert
										dense
										type="error"
										class="ma-0"
									>
										Atsiprašome, įvyko nenumatyta klaida...
									</v-alert>
								</template>
							</template>
							<template v-else>
								<v-progress-circular
									indeterminate
									color="primary"
									:size="28"
									width="2"
								></v-progress-circular>
							</template>
						</div>
					</template>
				</div>
			</v-card>
		</v-expand-transition>
	</div>
</template>

<script>
	import AttributesTable from "./AttributesTable";
	import CommonHelper from "./helpers/CommonHelper";
	import MyHeading from "./MyHeading";

	export default {
		data: function(){
			var initiallyCollapsed = (this.data.featureType == "verticalStreetSigns"),
				viewingAllowed = false;
			if (this.$store.state.userData && this.$store.state.userData.permissions) {
				if (this.$store.state.userData.permissions.length) { // TODO, FIXME! Dabar sąlyga tokia primityvi...
					viewingAllowed = true;
				}
			}
			var data = {
				items: null,
				collapsed: initiallyCollapsed,
				initiallyCollapsed: initiallyCollapsed,
				viewingAllowed: viewingAllowed
			};
			return data;
		},

		props: {
			data: Object,
			title: String
		},

		components: {
			AttributesTable,
			MyHeading
		},

		mounted: function(){
			this.findFeature(this.data);
		},

		methods: {
			findFeature: function(data){
				var e = {
					globalId: data.globalId,
					layerId: data.layerId,
					historic: true
				};
				if (data.type == "vvt") {
					e.type = "vvt";
				}
				var items = [],
					item;
				this.$store.state.myMap.findFeature(e).then(function(features){
					var layerInfo = this.$store.state.myMap.getLayerInfo(data.featureType, data.layerId) || {},
						dateFieldsTimeReference = layerInfo.dateFieldsTimeReference;
					features.forEach(function(feature, i){
						item = {
							editor: CommonHelper.getLastEditor(feature) || "—"
						};
						if (i == 0) {
							item.date = CommonHelper.getPrettyDate(feature.attributes["GDB_FROM_DATE"], true, dateFieldsTimeReference);
							item.action = "Sukurtas";
						} else {
							item.date = CommonHelper.getPrettyDate(feature.attributes["GDB_FROM_DATE"], true, dateFieldsTimeReference);
							item.action = "Redaguotas";
							if (i == (features.length - 1)) {
								if (feature.attributes["GDB_TO_DATE"] < 253402293599000) { // 9999 metai...
									item.action = "Pašalintas";
									item.date = CommonHelper.getPrettyDate(feature.attributes["GDB_TO_DATE"], true, dateFieldsTimeReference);
								}
							}
						}
						items.push(item);
					});
					this.items = {
						items: items,
						fields: ["action", "date", "editor"],
						prettyFields: {
							action: "Veiksmas",
							date: "Data",
							editor: "Veiksmą atliko"
						}
					};
					this.featureData = {
						featureType: data.featureType,
						historyFeatures: features,
						data: data
					}; // Prireiks čia -> "show-feature-history-dialog"
				}.bind(this), function(){
					this.items = "error";
				}.bind(this));
			},
			collapseAndExpandHandler: function(collapsed){
				this.collapsed = collapsed;
			},
			showFeatureHistoryDialog: function(){
				this.$vBus.$emit("show-feature-history-dialog", this.featureData);
			}
		}
	};
</script>