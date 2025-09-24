<template>
	<OverMapButtonContent
		type="street-signs-filter"
		:title="title"
		:btn="btn"
		:onOpen="onOpen"
		ref="wrapper"
	>
		<template v-slot>
			<div class="body-2">
				<MyForm
					:data="formData"
					id="form-data"
					:onDataChange="onFormDataChange"
					ref="form"
					class="compact-form"
				/>
				<div class="mt-5 d-flex justify-end">
					<v-btn
						text
						outlined
						small
						color="primary"
						v-on:click="clearFilter"
						class="mr-1"
						v-if="clearFilterVisible"
					>
						Valyti filtrą
					</v-btn>
					<v-btn
						small
						color="primary lighten-1"
						v-on:click="setLayersFilter"
					>
						Taikyti filtrą
					</v-btn>
				</div>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import MyForm from "./../MyForm";
	import OverMapButtonContent from "./OverMapButtonContent";

	export default {
		data: function(){
			var data = {
				formData: null,
				title: null,
				btn: null,
				clearFilterVisible: false
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			}
		},

		components: {
			MyForm,
			OverMapButtonContent
		},

		created: function(){
			this.$vBus.$on("show-or-hide-street-signs-filter", this.showOrHide);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-or-hide-street-signs-filter", this.showOrHide);
		},

		methods: {
			showOrHide: function(e){
				this.title = e.title;
				this.btn = e.btn;
				this.$refs.wrapper.toggle(this.btn.$el.offsetLeft);
			},
			setLayersFilter: function(){
				this.myMap.setLayersFilter(this.$refs.form.getData());
			},
			clearFilter: function(){
				this.$refs.form.clear();
				this.setLayersFilter();
			},
			onFormDataChange: function(formData){
				var formEmpty = true;
				if (formData) {
					for (var key in formData) {
						if (formData[key]) {
							formEmpty = false;
							break;
						}
					}
				}
				this.clearFilterVisible = !formEmpty;
			},
			onOpen: function(){
				this.setFormData();
			},
			setFormData: function(){
				var vertFormDataFields = [],
					fields,
					field;
				fields = this.myMap.getLayerFields("verticalStreetSigns");
				field = this.myMap.getLayerField(fields, "TIPAS");
				if (field) {
					field.key = "vertical-type";
					vertFormDataFields.push(field);
				}
				field = this.myMap.getLayerField(fields, "KET_NR");
				if (field) {
					field.type = "street-sign-vertical";
					field.key = "vertical-nr";
					vertFormDataFields.push(field);
				}
				var otherObjectsFormDataCodedValues = [];
				fields = this.myMap.getLayerFields("otherPoints");
				field = this.myMap.getLayerField(fields, "TIPAS");
				if (field.domain) {
					otherObjectsFormDataCodedValues = otherObjectsFormDataCodedValues.concat(field.domain.codedValues);
				}
				fields = this.myMap.getLayerFields("otherPolylines");
				field = this.myMap.getLayerField(fields, "TIPAS");
				if (field.domain) {
					otherObjectsFormDataCodedValues = otherObjectsFormDataCodedValues.concat(field.domain.codedValues);
				}
				fields = this.myMap.getLayerFields("otherPolygons");
				field = this.myMap.getLayerField(fields, "TIPAS");
				if (field.domain) {
					otherObjectsFormDataCodedValues = otherObjectsFormDataCodedValues.concat(field.domain.codedValues);
				}
				var formData = [{
					title: "Vertikaliojo ženklinimo filtravimas",
					fields: vertFormDataFields
				},{
					title: "Horizontaliojo ženklinimo filtravimas",
					fields: [{
						title: "KET numeris",
						key: "horizontal-nr",
						type: "street-sign-horizontal"
					}]
				},{
					title: "Kitų objektų filtravimas",
					fields: [{
						title: "Tipas",
						key: "other-type",
						domain: {
							codedValues: otherObjectsFormDataCodedValues
						}
					}]
				},{
					title: "Retrospektyvinė informacija",
					fields: [{
						title: "Situacija šiam momentui",
						key: "historic-moment",
						type: "date",
						withTime: true
					}]
				}];
				if (this.myMap.layersFilter) {
					formData.forEach(function(group){
						if (group.fields) {
							group.fields.forEach(function(field){
								field.value = this.myMap.layersFilter[field.key];
							}.bind(this));
						}
					}.bind(this));
				}
				this.formData = formData;
			}
		}
	}
</script>