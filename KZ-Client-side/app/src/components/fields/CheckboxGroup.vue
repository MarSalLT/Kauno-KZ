<template>
	<div>
		<template v-for="(item, i) in items">
			<v-checkbox
				v-model="inputVal"
				:label="item.title"
				:value="item.value"
				:key="i"
				class="ma-0 pa-0"
				hide-details
			></v-checkbox>
		</template>
	</div>
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
			value: Array,
			id: String,
			items: Array
		},

		computed: {
			inputVal: {
				get: function(){
					return this.value;
				},
				set: function(val){
					if (val) {
						if (!Array.isArray(val)) {
							val = [val];
						}
					}
					if (!val.length) {
						val = null;
					}
					this.$emit("input", val);
				}
			}
		}
	}
</script>