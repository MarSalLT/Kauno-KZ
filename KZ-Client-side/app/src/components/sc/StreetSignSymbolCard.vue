<template>
	<v-card
		class="elevation-2 d-flex flex-column"
		tile
	>
		<v-card-text class="d-flex flex-column align-center pa-3 pb-1 flex-grow-1 justify-end">
			<div :class="['pa-1 pb-0 mb-2', category == 'elements' ? 'img-wrapper' : null, (category == 'elements') && (item.subtype == 'down') ? 'pt-0 pb-1' : null]">
				<v-img
					:src="item.src + '?t=' + listKey"
					:width="imageWidth"
					:height="imageHeight"
					:contain="true"
				>
					<template v-slot:placeholder>
						<v-row
							class="fill-height ma-0 full-height"
							align="center"
							justify="center"
						>
							<v-progress-circular
								indeterminate
								color="primary"
								:size="25"
								width="2"
							></v-progress-circular>
						</v-row>
					</template>
				</v-img>
			</div>
			<div class="mt-1 d-none">{{item.author}}</div>
		</v-card-text>
		<v-card-actions class="actions d-flex justify-center">
			<v-btn
				icon
				v-on:click="showSymbolInfo(item)"
				v-if="item['signs_count']"
			>
				<v-avatar
					color="primary"
					size="22"
					title="Simbolį naudojantys kelio ženklai"
				>
					<span class="white--text text-caption street-signs-count">{{item["signs_count"]}}</span>
				</v-avatar>
			</v-btn>
			<v-btn
				icon
				color="indigo"
				small
				:to="(category == 'elements' ? 'element-' : '') + 'edit?id=' + item.id"
				v-if="!uneditable"
			>
				<v-icon title="Aktyvuoti redagavimą" size="20">mdi-pencil</v-icon>
			</v-btn>
			<v-btn
				icon
				color="error"
				small
				:disabled="Boolean(item['signs_count']) || (category == 'elements')"
				:loading="deleteInProgress"
				v-on:click="deleteSymbol(item)"
			>
				<v-icon title="Šalinti" size="20">mdi-delete</v-icon>
			</v-btn>
		</v-card-actions>
	</v-card>
</template>

<script>
	import StreetSignsSymbolsManagementHelper from "../helpers/StreetSignsSymbolsManagementHelper";

	export default {
		data: function(){
			var imageDimensions = StreetSignsSymbolsManagementHelper.getImageDimensions(this.item, this.category);
			var data = {
				imageWidth: imageDimensions.width,
				imageHeight: imageDimensions.height,
				deleteInProgress: false
			};
			return data;
		},

		props: {
			item: Object,
			category: String,
			uneditable: Boolean,
			listKey: Number,
			onItemDelete: Function
		},

		methods: {
			showSymbolInfo: function(item){
				this.$vBus.$emit("show-symbol-info", item);
			},
			deleteSymbol: function(item){
				this.$vBus.$emit("confirm", {
					title: "Ar tvirtinate simbolio šalinimą?",
					message: "Simbolis bus pašalintas negrįžtamai. Ar tvirtinate simbolio šalinimą?",
					positiveActionTitle: "Tvirtinti simbolio šalinimą",
					negativeActionTitle: "Atšaukti",
					positive: function(){
						this.deleteInProgress = true;
						var params = {
							category: item.category,
							id: item.id
						};
						StreetSignsSymbolsManagementHelper.deleteItem(params).then(function(response){
							if (response && response.success) {
								if (this.onItemDelete) {
									this.onItemDelete(item.id);
								}
							} else {
								this.$vBus.$emit("show-message", {
									type: "warning"
								});
							}
							this.deleteInProgress = false;
						}.bind(this), function(){
							this.$vBus.$emit("show-message", {
								type: "warning"
							});
							this.deleteInProgress = false;
						}.bind(this));
					}.bind(this)
				});
			}
		}
	};
</script>

<style scoped>
	.actions {
		border-top: 1px solid #dddddd;
		min-height: 56px !important;
	}
	.img-wrapper {
		background-color: #444444;
	}
	.street-signs-count {
		font-size: 0.6rem !important;
	}
</style>