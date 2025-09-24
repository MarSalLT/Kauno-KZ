<template>
	<div>
		<template v-if="list">
			<template v-if="list == 'error'">
				<v-alert
					dense
					type="error"
					class="ma-0 d-inline-block"
				>
					Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
				</v-alert>
			</template>
			<template v-else>
				<template v-if="list.length">
					<div>
						<template v-for="(group, i) in list">
							<div :key="group.key" class="ml-1">
								<div class="caption"><strong>{{getPrettyTypeTitle(group.key)}}</strong></div>
								<div class="d-flex flex-wrap">
									<template v-for="(item, j) in group.items">
										<StreetSignSymbolCard
											:item="item"
											category="elements"
											:key="i + '-' + j"
											:listKey="listKey"
											class="mt-2 mr-2 mb-2"
										/>
									</template>
								</div>
							</div>
						</template>
					</div>
				</template>
				<template v-else>
					<div>Galerija tuščia...</div>
				</template>
				<div class="fab-buttons d-flex flex-column">
					<v-btn
						fab
						color="success"
						v-on:click="createNewItem"
						class="mb-1"
					>
						<v-icon
							title="Kurti naują elementą"
						>
							mdi-plus
						</v-icon>
					</v-btn>
					<v-btn
						fab
						color="primary"
						v-on:click="getItems"
					>
						<v-icon
							title="Perkrauti sąrašą"
						>
							mdi-refresh
						</v-icon>
					</v-btn>
				</div>
			</template>
		</template>
		<template v-else>
			<v-progress-circular
				indeterminate
				color="primary"
				:size="30"
				width="2"
			></v-progress-circular>
		</template>
	</div>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import StreetSignSymbolCard from "./StreetSignSymbolCard";
	import StreetSignsSymbolsManagementHelper from "../helpers/StreetSignsSymbolsManagementHelper";

	export default {
		data: function(){
			var data = {
				list: null,
				itemsUpdated: 0,
				listKey: null
			};
			return data;
		},

		mounted: function(){
			this.getItems();
		},

		created: function(){
			this.$vBus.$on("sc-gallery-item-updated", this.onItemUpdate);
		},

		beforeDestroy: function(){
			this.$vBus.$off("sc-gallery-item-updated", this.onItemUpdate);
		},

		activated: function(){
			if (this.itemsUpdated) {
				this.getItems();
			}
		},

		components: {
			StreetSignSymbolCard
		},

		methods: {
			getItems: function(){
				this.list = null;
				StreetSignsSymbolsManagementHelper.getUniqueSymbolsElements(this.$route.query).then(function(list){
					this.itemsUpdated = 0;
					var listGrouped = {};
					list.forEach(function(item){
						item.src = CommonHelper.getUniqueSymbolElementSrc(item.id);
						if (!listGrouped[item.type]) {
							listGrouped[item.type] = {
								key: item.type,
								items: []
							};
						}
						listGrouped[item.type]["items"].push(item);
					});
					list = [];
					for (var key in listGrouped) {
						list.push(listGrouped[key]);
					}
					this.list = list;
					this.listKey = Date.now();
				}.bind(this), function(){
					this.list = "error";
				}.bind(this));
			},
			getPrettyTypeTitle: function(key){
				var titles = {
					arrows: "Rodyklės"
				};
				var title = titles[key] || key;
				return title;
			},
			onItemUpdate: function(category){
				if (category == "elements") {
					this.itemsUpdated += 1;
				}
			},
			createNewItem: function(){
				this.$router.push({
					path: "/sc/element-create"
				});
			}
		}
	};
</script>

<style scoped>
	.fab-buttons {
		/* position: absolute; */
		position: fixed;
		right: 0.5rem;
		bottom: 0.5rem;
	}
</style>