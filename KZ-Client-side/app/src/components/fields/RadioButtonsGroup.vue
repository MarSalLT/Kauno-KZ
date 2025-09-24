<template>
	<v-radio-group
		v-model="inputVal"
		class="ma-0 pa-0"
		hide-details
		row
	>
		<template v-for="(item, i) in items">
			<v-radio
				:label="item.title"
				:value="item.value"
				:key="i"
				class="ma-0 pa-0 mr-3"
			></v-radio>
		</template>
	</v-radio-group>
</template>

<script>
	export default {
		data: function(){
			var data = {
				// ...
			};
			return data;
		},

		props: {
			value: String,
			id: String,
			items: Array
		},

		computed: {
			inputVal: {
				get: function(){
					return this.value;
				},
				set: function(val){
					this.$emit("input", val);
				}
			}
		}
	}
</script>