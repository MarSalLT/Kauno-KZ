<template>
	<div :class="elementData && elementData.type ? 'full-height' : null">
		<template v-if="elementData">
			<template v-if="elementData.type">
				<StreetSignSymbolDashboard
					:data="elementData"
					mode="element-create"
				/>
			</template>
			<template v-else>
				<div class="px-1">
					<div class="d-flex">
						<div class="mt-2 mr-2">Elemento tipas:</div>
						<div class="d-flex flex-column">
							<v-select
								v-model="activeType"
								dense
								hide-details
								:items="types"
								item-value="id"
								item-text="title"
								clearable
								class="body-2"
							></v-select>
							<div class="mt-3">
								<v-btn
									color="blue darken-1"
									text
									outlined
									small
									:disabled="!activeType"
									v-on:click="selectType"
								>
									Pasirinkti elemento tipą
								</v-btn>
							</div>
						</div>
					</div>
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
	import StreetSignSymbolDashboard from "./StreetSignSymbolDashboard";

	export default {
		data: function(){
			var data = {
				elementData: null,
				types: [{
					title: "Rodyklė",
					id: "arrows"
				}],
				activeType: "arrows"
			};
			return data;
		},

		components: {
			StreetSignSymbolDashboard
		},

		methods: {
			getElementData: function(data){
				this.elementData = data;
			},
			selectType: function(){
				this.$router.push({
					path: "/sc/element-create",
					query: {
						type: this.activeType
					}
				});
			}
		},

		watch: {
			"$store.state.scItem": {
				immediate: true,
				handler: function(scItemElement){
					if (scItemElement) {
						if (scItemElement.mode == "element-create") {
							this.getElementData(scItemElement);
						}
					}
				}
			}
		}
	};
</script>

<style scoped>
	.v-select {
		width: 300px;
	}
</style>