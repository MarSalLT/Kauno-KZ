<template>
	<div class="mt-1">
		<v-checkbox
			:label="label"
			class="ma-0 pa-0"
			hide-details
			v-model="checked"
		></v-checkbox>
	</div>
</template>

<script>
	export default {
		data: function(){
			var data = {
				checked: this.layer.get("visible"),
				label: this.getLabel(this.layer.service)
			};
			return data;
		},

		props: {
			layer: Object
		},

		methods: {
			getLabel: function(service){
				var label;
				if (service) {
					label = service.title;
				}
				if (!label) {
					label = "Sluoksnis";
				}
				return label;
			}
		},

		watch: {
			checked: {
				immediate: true,
				handler: function(checked){
					this.layer.setVisible(checked);
				}
			}
		}
	}
</script>