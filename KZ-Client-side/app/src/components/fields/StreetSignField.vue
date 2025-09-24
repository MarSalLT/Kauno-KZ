<template>
	<div>
		<v-text-field
			v-model="inputVal"
			dense
			hide-details
			:outlined="outlined"
			:id="id"
			full-width
			:class="['body-2 ma-0', simple ? null : 'plain']"
			clearable
			ref="field"
			:error="error"
			:disabled="!editable"
			:readonly="readonly"
		>
			<template v-slot:append>
				<v-btn
					icon
					height="24"
					width="24"
					v-on:click.stop="showPicker"
					:disabled="!editable"
				>
					<v-icon title="Pasirinkti KET numerį" small>mdi-cog</v-icon>
				</v-btn>
			</template>
		</v-text-field>
		<v-dialog
			v-model="dialog"
			max-width="800"
			scrollable
		>
			<v-card>
				<v-card-title>
					<span>KET numerio pasirinkimas</span>
				</v-card-title>
				<v-card-text class="pb-0" ref="content">
					<TemplatePicker
						:featureTypes="templatePickerFeatureTypes"
						:onItemSelect="onTemplatePick"
						:initialValue="inputVal"
						ref="templatePicker"
					/>
				</v-card-text>
				<v-card-actions class="mx-2 pb-5 pt-5">
					<v-btn
						color="blue darken-1"
						text
						v-on:click="closePicker"
						outlined
						small
					>
						Uždaryti
					</v-btn>
				</v-card-actions>
			</v-card>
		</v-dialog>
	</div>
</template>

<script>
	import TemplatePicker from "../TemplatePicker";

	export default {
		data: function(){
			var data = {
				dialog: false,
				templatePickerFeatureTypes: null,
				outlined: !this.simple
			};
			if (this.type == "street-sign-vertical") {
				data.templatePickerFeatureTypes = [
					"verticalStreetSigns"
				];
			} else if (this.type == "street-sign-horizontal") {
				data.templatePickerFeatureTypes = [
					"horizontalPoints",
					"horizontalPolylines",
					"horizontalPolygons"
				];
			} else if (this.type == "street-sign-horizontal-points") {
				data.templatePickerFeatureTypes = [
					"horizontalPoints"
				];
			} else if (this.type == "street-sign-horizontal-polylines") {
				data.templatePickerFeatureTypes = [
					"horizontalPolylines"
				];
			}
			return data;
		},

		props: {
			value: String,
			id: String,
			type: String,
			simple: Boolean,
			error: Boolean,
			editable: Boolean,
			readonly: Boolean
		},

		components: {
			TemplatePicker
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
			showPicker: function(){
				this.dialog = true;
			},
			closePicker: function(){
				this.dialog = false;
				this.$refs.field.blur(); // Nes kažkodėl fokusuojasi?..
			},
			onTemplatePick: function(template){
				this.inputVal = template;
				if (this.dialog) {
					this.closePicker();
				}
			}
		},

		watch: {
			dialog: {
				immediate: false,
				handler: function(open){
					if (open) {
						if (this.$refs.content) {
							setTimeout(function(){
								this.$refs.content.scrollTop = 0;
							}.bind(this), 0);
						}
					}
				}
			},
			inputVal: {
				immediate: true,
				handler: function(inputVal){
					if (this.$refs.templatePicker) {
						this.$refs.templatePicker.activeTemplateItem = inputVal;
					}
				}
			}
		}
	}
</script>