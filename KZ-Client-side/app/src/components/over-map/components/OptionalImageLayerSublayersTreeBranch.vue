<template>
	<div :class="level ? 'ml-8' : null">
		<div>
			<v-checkbox
				:label="data.name"
				hide-details
				:disabled="true"
				class="ma-0"
				v-model="checked"
			></v-checkbox>
		</div>
		<template v-for="(innerBranch) in data.children">
			<OptionalImageLayerSublayersTreeBranch
				:service="service"
				:data="innerBranch"
				:level="level + 1"
				:key="innerBranch.id"
			/>
		</template>
	</div>
</template>

<script>
	import OptionalImageLayerSublayersTreeBranch from "./OptionalImageLayerSublayersTreeBranch";

	export default {
		name: 'OptionalImageLayerSublayersTreeBranch',
		data: function(){
			var data = {
				checked: true
			};
			return data;
		},

		props: {
			service: Object,
			data: Object,
			level: Number
		},

		components: {
			OptionalImageLayerSublayersTreeBranch
		},

		watch: {
			checked: {
				immediate: false,
				handler: function(checked){
					console.log("CHECKED...", checked, this.data.id, this.service.layer); // TODO...
				}
			}
		}
	}
</script>