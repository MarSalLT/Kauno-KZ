<template>
	<v-select
		v-model="inputVal"
		dense
		hide-details
		:outlined="outlined"
		:id="id"
		full-width
		:class="['body-2 ma-0', simple ? null : 'plain']"
		clearable
		:items="items"
		item-value="code"
		item-text="name"
		:error="error"
		:disabled="!editable"
	>
	</v-select>
</template>

<script>
	export default {
		data: function(){
			var data = {
				outlined: !this.simple
			};
			return data;
		},

		props: {
			value: String,
			id: String,
			items: Array,
			simple: Boolean,
			error: Boolean,
			editable: Boolean
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