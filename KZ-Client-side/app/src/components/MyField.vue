<template>
	<div>
		<template v-if="field.type == 'date'">
			<DateTextField
				v-model="inputVal"
				:id="id"
				:withTime="Boolean(field.withTime)"
				:simple="true"
				:error="invalid"
				:editable="editable"
				:timeZoneData="rawField ? rawField.timeZoneData : null"
			/>
		</template>
		<template v-else-if="['street-sign-vertical', 'street-sign-horizontal', 'street-sign-horizontal-points', 'street-sign-horizontal-polylines'].indexOf(field.type) != -1">
			<StreetSignField
				v-model="inputVal"
				:id="id"
				:type="field.type"
				:simple="true"
				:error="invalid"
				:editable="editable"
				:readonly="true"
			/>
		</template>
		<template v-else-if="field.type == 'street-sign-unique-symbol'">
			<StreetSignUniqueSymbolField
				v-model="inputVal"
				:id="id"
				:simple="true"
				:error="invalid"
				:editable="editable"
				:readonly="true"
			/>
		</template>
		<template v-else-if="field.domain">
			<SelectField
				v-model="inputVal"
				:id="id"
				:items="field.domain.codedValues"
				:simple="true"
				:error="invalid"
				:editable="editable"
			/>
		</template>
		<template v-else>
			<v-text-field
				v-model="inputVal"
				dense
				hide-details
				:id="id"
				:error="invalid"
				:disabled="!editable"
				class="body-2 ma-0"
				:type="rawField && (['esriFieldTypeInteger', 'esriFieldTypeSmallInteger', 'esriFieldTypeDouble', 'esriFieldTypeOID'].indexOf(rawField.type) != -1) ? 'number' : 'text'"
			>
			</v-text-field>
		</template>
	</div>
</template>

<script>
	import DateTextField from "./fields/DateTextField";
	import SelectField from "./fields/SelectField";
	import StreetSignField from "./fields/StreetSignField";
	import StreetSignUniqueSymbolField from "./fields/StreetSignUniqueSymbolField";

	export default {
		data: function(){
			var data = {
				inputVal: this.value
			};
			return data;
		},

		props: {
			field: Object,
			rawField: Object,
			id: String,
			value: String,
			invalid: Boolean,
			editable: Boolean
		},

		components: {
			DateTextField,
			SelectField,
			StreetSignField,
			StreetSignUniqueSymbolField
		},

		watch: {
			inputVal: {
				immediate: false,
				handler: function(inputVal){
					// BIG TODO, FIXME! Šita visa vieta man irgi nelabai patinka... Visų pirma, kad dubliuojamas reikšmės parse'inimo funkcionalumas. Taip pat tas reikšmės pokyčio
					// įvykio emit'inimas irgi nežavi...
					if (this.rawField) {
						if (["esriFieldTypeInteger", "esriFieldTypeSmallInteger", "esriFieldTypeDate"].indexOf(this.rawField.type) != -1) {
							inputVal = parseInt(inputVal);
						} else if (this.rawField.type == "esriFieldTypeDouble") {
							inputVal = parseFloat(inputVal);
						}
					}
					this.$vBus.$emit("my-field-value-changed", {
						name: this.field.name,
						val: inputVal
					});
				}
			}
		}
	};
</script>