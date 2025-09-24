<template>
	<v-btn-toggle
		v-model="activeItemId"
		dense
		:group="true"
		class="d-block"
	>
		<template v-if="items && items.length">
			<template v-for="(item, i) in items">
				<div
					:key="i"
					class="d-inline-block mr-2 mt-2"
				>
					<TemplatePickerButton
						:template="item"
					/>
				</div>
			</template>
		</template>
		<template v-else>
			Sukurtų unikalių simbolių nėra...
		</template>
	</v-btn-toggle>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import TemplatePickerButton from "../TemplatePickerButton";

	export default {
		data: function(){
			var data = {
				items: null,
				activeItemId: this.initialValue
			};
			return data;
		},

		props: {
			list: Array,
			onItemSelect: Function,
			initialValue: String
		},

		components: {
			TemplatePickerButton
		},

		mounted: function(){
			this.setItems();
		},

		methods: {
			setItems: function(){
				var items = [],
					now = Date.now();
				if (this.list) {
					this.list.forEach(function(listItem){
						items.push({
							id: listItem.id,
							// name: listItem.id,
							symbol: {
								altSrc: CommonHelper.getUniqueSymbolSrc(listItem.id, now),
								width: listItem["img_width"],
								height: listItem["img_height"],
								type: "sc"
							}
						});
					});
				}
				this.items = items;
			}
		},

		watch: {
			activeItemId: {
				immediate: false,
				handler: function(activeItemId){
					if (this.onItemSelect) {
						this.onItemSelect(activeItemId);
					}
				}
			}
		}
	};
</script>