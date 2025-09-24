<template>
	<div>
		<v-form>
			<template v-if="data">
				<template v-for="(item, i) in data">
					<template v-if="item.fields">
						<div
							:key="'table-title-' + i"
							:class="[i ? 'mt-5' : null, 'mb-2 font-weight-bold']"
						>
							{{item.title}}
						</div>
						<div :key="'table-' + i" class="table">
							<div
								v-for="(field, j) in item.fields"
								:key="'f-' + i + '-' + j"
								class="tr"
							>
								<div class="th pr-2">
									<label
										:for="getId(i, j)"
									>
										<span
											v-if="field.required"
											title="Laukas privalomas"
											class="red--text mr-1"
										>*</span>
										{{field.title || field.alias || field.name}}:
									</label>
								</div>
								<div :class="['td', spacious ? 'py-1' : null]">
									<template v-if="field.type == 'date'">
										<div :class="field.spaced ? 'mt-2 mb-2' : null">
											<DateTextField
												v-model="dataModel[field.key]"
												:id="getId(i, j)"
												:withTime="Boolean(field.withTime)"
												:error="Boolean(invalid[field.key])"
												:editable="true"
											/>
										</div>
									</template>
									<template v-else-if="field.domain">
										<div :class="field.spaced ? 'mt-2 mb-2' : null">
											<SelectField
												v-model="dataModel[field.key]"
												:id="getId(i, j)"
												:items="field.domain.codedValues"
												:error="Boolean(invalid[field.key])"
												:editable="true"
											/>
										</div>
									</template>
									<template v-else-if="field.type == 'street-sign-vertical' || field.type == 'street-sign-horizontal'">
										<div :class="field.spaced ? 'mt-2 mb-2' : null">
											<StreetSignField
												v-model="dataModel[field.key]"
												:id="getId(i, j)"
												:type="field.type"
												:error="Boolean(invalid[field.key])"
												:editable="true"
											/>
										</div>
									</template>
									<template v-else-if="field.type == 'geom-advanced'">
										<div :class="field.spaced ? 'mt-2 mb-2' : null">
											<GeometryCreator
												v-model="dataModel[field.key]"
												:id="getId(i, j)"
												:additionalTopItem="field.additionalTopItem"
												:useNameAsCode="field.useNameAsCode"
											/>
										</div>
									</template>
									<template v-else-if="field.type == 'checkbox'">
										<div :class="field.spaced ? 'mt-2 mb-2' : null">
											<CheckboxGroup
												v-model="dataModel[field.key]"
												:id="getId(i, j)"
												:items="field.items"
											/>
										</div>
									</template>
									<template v-else-if="field.type == 'radio-button'">
										<div :class="field.spaced ? 'mt-2 mb-2' : null">
											<RadioButtonsGroup
												v-model="dataModel[field.key]"
												:id="getId(i, j)"
												:items="field.items"
											/>
										</div>
									</template>
									<template v-else-if="field.type == 'range'">
										<div :class="field.spaced ? 'mt-2 mb-2' : null">
											<RangeField
												v-model="dataModel[field.key]"
												:id="getId(i, j)"
												:field="field"
											/>
										</div>
									</template>
									<template v-else>
										<div :class="field.spaced ? 'mt-2 mb-2' : null">
											<v-text-field
												v-model="dataModel[field.key]"
												dense
												hide-details
												outlined
												:id="getId(i, j)"
												:error="Boolean(invalid[field.key])"
												:editable="true"
												class="plain body-2 ma-0"
												:disabled="Boolean(field.disabled)"
												:type="field.type == 'password' ? (appendIconShow[field.key] ? 'text' : 'password') : field.type"
												:append-icon="field.type == 'password' ? (appendIconShow[field.key] ? 'mdi-eye' : 'mdi-eye-off') : null"
												@click:append="appendIconShow[field.key] = !appendIconShow[field.key]"
											>
											</v-text-field>
										</div>
									</template>
								</div>
							</div>
						</div>
					</template>
				</template>
			</template>
			<template v-else>
				...
			</template>
		</v-form>
	</div>
</template>

<script>
	import CheckboxGroup from "./fields/CheckboxGroup";
	import DateTextField from "./fields/DateTextField";
	import GeometryCreator from "./GeometryCreator";
	import RadioButtonsGroup from "./fields/RadioButtonsGroup";
	import RangeField from "./fields/RangeField";
	import SelectField from "./fields/SelectField";
	import StreetSignField from "./fields/StreetSignField";

	export default {
		data: function(){
			var dataModel = {},
				appendIconShow = {};
			if (this.data) {
				var value;
				this.data.forEach(function(group){
					if (group.fields) {
						group.fields.forEach(function(field){
							if (field.key) {
								value = field.value;
								dataModel[field.key] = value;
								appendIconShow[field.key] = false;
							}
						});
					}
				});
			}
			var data = {
				dataModel: dataModel,
				invalid: {},
				appendIconShow: appendIconShow
			};
			return data;
		},

		props: {
			title: String,
			data: Array,
			id: String,
			onDataChange: Function,
			spacious: Boolean
		},

		components: {
			CheckboxGroup,
			DateTextField,
			GeometryCreator,
			RadioButtonsGroup,
			RangeField,
			SelectField,
			StreetSignField
		},

		methods: {
			getId: function(i, j){
				return this.id + "-my-input-" + i + "-" + j;
			},
			getData: function(){
				var data = Object.assign({}, this.dataModel);
				return data;
			},
			clear: function(){
				for (var key in this.dataModel) {
					this.dataModel[key] = null;
				}
			}
		},

		watch: {
			dataModel: {
				deep: true,
				immediate: true,
				handler: function(d){
					var data = Object.assign({}, d);
					if (this.onDataChange) {
						this.onDataChange(data);
					}
				}
			}
		}
	}
</script>

<style scoped>
	.v-form {
		max-width: 600px;
	}
	.table {
		display: table;
		width: 100%;
	}
	.tr {
		display: table-row;
	}
	.th {
		width: 45%;
		text-align: right;
	}
	.th, .td {
		display: table-cell;
		vertical-align: middle;
		line-height: normal;
	}
</style>