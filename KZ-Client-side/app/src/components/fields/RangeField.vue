<template>
	<div class="d-flex align-center">
		<v-text-field
			v-model="from"
			dense
			hide-details
			:outlined="outlined"
			:id="id"
			:class="['body-2 ma-0 mr-1', simple ? null : 'plain', field.additionalClass ? field.additionalClass : null]"
			clearable
			type="number"
			:min="field.start"
			:max="field.end - 1"
			autocomplete="off"
		>
		</v-text-field>
		—
		<v-text-field
			v-model="to"
			dense
			hide-details
			:outlined="outlined"
			:id="id + '-to'"
			:class="['body-2 ma-0 ml-1 mr-1', simple ? null : 'plain']"
			clearable
			type="number"
			:min="field.start + 1"
			:max="field.end"
			autocomplete="off"
		>
		</v-text-field>
		%
	</div>
</template>

<script>
	export default {
		data: function(){
			var from,
				to;
			if (this.field.vmsInventorizationDeprecated) {
				from = 0;
				to = 100;
			}
			if (this.value) {
				var value = this.value.split("—");
				if (value.length == 2) {
					from = value[0] || null;
					to = value[1] || null;
				}
			}
			var data = {
				outlined: !this.simple,
				from: from,
				to: to
			};
			return data;
		},

		props: {
			value: String,
			id: String,
			simple: Boolean,
			field: Object
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
		},

		methods: {
			setInputVal: function(){
				if (this.field.vmsInventorizationDeprecated) {
					this.inputVal = (this.from || 0) + "—" + (this.to || 0);
				} else {
					if (this.from || this.to) {
						this.inputVal = (this.from || "") + "—" + (this.to || "");
					} else {
						this.inputVal = null;
					}
				}
			}
		},

		watch: {
			from: {
				immediate: false,
				handler: function(from){
					if (!from) {
						// this.from = 0; // Kažko normaliai nesuveikia...
					}
					this.setInputVal();
				}
			},
			to: {
				immediate: true,
				handler: function(to){
					if (!to) {
						// this.to = 0; // Kažko normaliai nesuveikia...
					}
					this.setInputVal();
				}
			},
			value: {
				immediate: true,
				handler: function(value){
					if (!value) {
						if (!this.field.vmsInventorizationDeprecated) {
							this.from = this.to = null; // Nukopijuota nuo GSM...
						}
					}
				}
			}
		}
	}
</script>

<style scoped>
	.v-text-field {
		width: 50%;
	}
</style>