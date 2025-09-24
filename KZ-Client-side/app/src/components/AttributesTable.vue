<template>
	<div>
		<MyHeading
			:value="title"
			v-if="title"
		/>
		<table class="body-2 mt-1 my-table">
			<template v-if="items">
				<template v-for="(item, i) in items">
					<tr :key="i" v-if="!item.hidden">
						<template v-if="item.title">
							<th class="px-2 py-1 font-weight-medium wide">
								<template v-if="editingActive && item.field">
									<label :for="getId(i)">
										{{item.title}}:
										<span
											title="Laukas privalomas"
											class="red--text ml-1"
											v-if="markRequired && item.rawField && !item.rawField.nullable"
										>*</span>
									</label>
								</template>
								<template v-else>
									{{item.title}}:
								</template>
							</th>
							<td class="px-2 py-1">
								<template v-if="editingActive && item.field">
									<MyField
										:field="item.field"
										:rawField="item.rawField"
										:id="getId(i)"
										:value="item.field.value"
										:ref="item.field.name"
										:invalid="invalid[item.field.name]"
										:editable="item.field.editable"
									/>
								</template>
								<template v-else>
									<span v-html="item.valuePretty" class="val-pretty"></span>
								</template>
							</td>
						</template>
					</tr>
				</template>
			</template>
			<template v-else-if="itemsComputed">
				<template v-if="itemsComputed.fields">
					<tr>
						<template v-for="(field, i) in itemsComputed.fields">
							<th
								class="px-2 py-1 text-no-wrap font-weight-medium bolder-background"
								:key="i"
							>
								{{itemsComputed.prettyFields[field] || field}}
							</th>
						</template>
					</tr>
					<template v-for="(item, i) in itemsComputed.items">
						<tr :key="i">
							<template v-for="(field, j) in itemsComputed.fields">
								<td
									:class="['px-2 py-1', field == 'date' ? 'text-no-wrap' : null]"
									:key="i + '-' + j"
								>
									{{item[field]}}
								</td>
							</template>
						</tr>
					</template>
				</template>
			</template>
		</table>
	</div>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";
	import MyHeading from "./MyHeading";
	import MyField from "./MyField";

	export default {
		data: function(){
			var data = {
				items: null,
				invalid: {}
			};
			if (this.itemsComputed) {
				if (!this.itemsComputed.fields) {
					data.items = this.itemsComputed.items;
				}
			} else {
				data.items = this.getItems();
			}
			if (data.items) {
				data.items.forEach(function(item){
					if (item.rawField) {
						item.field = {
							name: item.rawField.name,
							domain: item.rawField.domain,
							editable: item.rawField.editable
						};
						if (item.rawField.type == "esriFieldTypeDate") {
							item.field.type = "date";
							item.field.withTime = true;
						}
						item.field.value = item.value;
						if (item.field.value || item.field.value === 0) {
							item.field.value += ""; // Nes gi reikalauja string'o...
						}
					}
				}.bind(this));
				if (this.data && this.data.featureType) {
					if (this.data.featureType == "verticalStreetSigns") {
						data.items.forEach(function(item){
							if (item.field) {
								if (item.field.name == "KET_NR") {
									item.field.type = "street-sign-vertical";
								} else if (item.field.name == "TIPAS" || item.field.name == "ATMESTA") {
									item.field.editable = false;
								} else if (item.field.name == CommonHelper.customSymbolIdFieldName) {
									item.field.type = "street-sign-unique-symbol";
								}
							}
						}.bind(this));
						data.items.forEach(function(item){
							if (item.field) {
								if (["STATUSAS", "PATVIRTINTAS"].indexOf(item.field.name) != -1) {
									item.field.editable = false;
									item.hidden = true;
								}
							}
						}.bind(this));
					} else if (this.data.featureType == "verticalStreetSignsSupports") {
						data.items.forEach(function(item){
							if (item.field) {
								if (["STATUSAS", "PATVIRTINIMAS"].indexOf(item.field.name) != -1) {
									item.field.editable = false;
									item.hidden = true;
								}
							}
						}.bind(this));
					} else if (this.data.featureType == "horizontalPoints") {
						data.items.some(function(item){
							if (item.field && item.field.name == "KET_NR") {
								item.field.type = "street-sign-horizontal-points";
								return true;
							}
						}.bind(this));
					} else if (this.data.featureType == "horizontalPolylines") {
						data.items.some(function(item){
							if (item.field && item.field.name == "KET_NR") {
								item.field.type = "street-sign-horizontal-polylines";
								return true;
							}
						}.bind(this));
					} else if (this.data.featureType == "tasks") {
						var statusCode;
						data.items.some(function(item){
							if (item.field) {
								if (item.field.name == "Statusas") {
									statusCode = item.field.value;
								}
							}
						}.bind(this));
						data.items.some(function(item){
							if (item.field) {
								if (["Pavadinimas", "Aprasymas", "Uzduoties_tipas", "Imone", "Pabaigos_data"].indexOf(item.field.name) != -1) {
									if (item.rawField) {
										item.rawField.nullable = false; // Čia toks kaip ir hack'as... TODO: pasakyti Povilui, kad tai nurodytų jau pačiame servise...
									}
								} else if (["rangovo_email", "Statusas", "Patvirtinimas", "uzsakovo_email", "Uzsakovo_vardas", "Uzsakovo_imone"].indexOf(item.field.name) != -1) {
									item.field.editable = false;
								}
								if (["Uzduoties_tipas", "Imone"].indexOf(item.field.name) != -1) {
									if (statusCode && (statusCode != "0")) {
										item.field.editable = false; // To reikia, nes yra tam tikri ribojimai Einpix'e?..
									}
								}
							}
						}.bind(this));
					} else if (this.data.featureType == "userPoints") {
						data.items.some(function(item){
							if (item.field) {
								if (["pastaba"].indexOf(item.field.name) != -1) {
									if (item.rawField) {
										// TODO: make textarea?
									}
								}
							}
						}.bind(this));
					}
					data.items.some(function(item){
						if (item.field) {
							if (["PASTABA", "PASTABOS"].indexOf(item.field.name) != -1) {
								item.field.editable = false;
							}
						}
					}.bind(this));
					if (this.$store.state.userData && this.$store.state.userData.permissions) {
						if (this.$store.state.userData.permissions.indexOf("approve") != -1) {
							// Tam tikrus laukus redaguoti leidžiame tik approver'iui!
							if (this.data.featureType == "verticalStreetSigns") {
								data.items.forEach(function(item){
									if (item.field) {
										if (["STATUSAS", "PATVIRTINTAS"].indexOf(item.field.name) != -1) {
											item.field.editable = true;
											item.hidden = false;
										}
									}
								}.bind(this));
							} else if (this.data.featureType == "verticalStreetSignsSupports") {
								data.items.forEach(function(item){
									if (item.field) {
										if (["STATUSAS", "PATVIRTINIMAS"].indexOf(item.field.name) != -1) {
											item.field.editable = true;
											item.hidden = false;
										}
									}
								}.bind(this));
							}
							data.items.some(function(item){
								if (item.field) {
									if (["PASTABA", "PASTABOS"].indexOf(item.field.name) != -1) {
										item.field.editable = true;
									}
								}
							}.bind(this));
						}
					}
				}
			}
			return data;
		},

		props: {
			title: String,
			data: Object,
			itemsComputed: Object,
			editingActive: Boolean,
			markRequired: Boolean,
			id: String
		},

		components: {
			MyHeading,
			MyField
		},

		created: function(){
			this.$vBus.$on("street-sign-feature-rotation-changed", this.streetSignFeatureRotationChanged);
			this.$vBus.$on("street-sign-feature-nr-changed", this.streetSignFeatureNrChanged);
		},

		beforeDestroy: function(){
			this.$vBus.$off("street-sign-feature-rotation-changed", this.streetSignFeatureRotationChanged);
			this.$vBus.$off("street-sign-feature-nr-changed", this.streetSignFeatureNrChanged);
		},

		methods: {
			getItems: function(){
				var items = [];
				if (this.data) {
					if (this.data.featureType) {
						if (this.data.featureType == "general-object") {
							// FIXME... Turi būti konkretūs atributai nurodyti...
							console.log("SPECIFIC ATTRIBUTES", this.data.feature.layer); // TODO... Gauti `importantFields` pagal šiuos duomenis...
							var featureProperties = this.data.feature.getProperties();
							for (var key in featureProperties) {
								if (key != "geometry") {
									items.push({
										title: key,
										valuePretty: featureProperties[key]
									});
								}
							}
						} else {
							var myMap = this.$store.state.myMap,
								layerFields = myMap.getLayerFields(this.data.featureType, this.data.layerId),
								field,
								item;
							if (layerFields) {
								var featureFields = CommonHelper.importantFields[this.data.featureType];
								if (!featureFields) {
									featureFields = [];
									if (layerFields) {
										layerFields.forEach(function(layerField){
											featureFields.push(layerField.name);
										});
									}
								}
								featureFields = featureFields.slice();
								if (this.data.feature.get("ATMESTA")) {
									var rejectFieldIndex = featureFields.indexOf("ATMESTA");
									if (rejectFieldIndex != -1) {
										featureFields.splice(rejectFieldIndex + 1, 0, "ATMETIMO_PRIEZASTIS");
									}
								}
								if (this.data.featureType == "otherPolylines") {
									if (this.data.feature && (this.data.feature.get("TIPAS") == 28)) {
										featureFields.push("SEGMENTAI_ESAMI", "SEGMENTAI_DEMONTUOTI", "SEGMENTAI_NAUJI", "SEGMENTU_TIPAS", "MONTUOJAMO_SEGMENTO_BUKLE");
									}
								}
								featureFields.forEach(function(key){
									field = myMap.getLayerField(layerFields, key);
									if (field) {
										item = myMap.getValueItems(field, this.data.feature.get(key));
										item.title = myMap.getFieldTitle(field);
										items.push(item);
									}
								}.bind(this));
								if (CommonHelper.scEnabled) {
									if (this.data.featureType == "verticalStreetSigns") {
										var testItem = myMap.getValueItems({
											name: CommonHelper.customSymbolIdFieldName,
											type: "esriFieldTypeString",
											editable: true,
											nullable: true // Aktualu formai...
										}, this.data.feature.get(CommonHelper.customSymbolIdFieldName));
										testItem.title = "Unikalus simbolis";
										items.push(testItem);
									}
								}
								if (this.data.origFeature) {
									var origFeatureActionItem = myMap.getValueItems({
										name: CommonHelper.taskFeatureActionFieldName,
										domain: {
											codedValues: [{
												code: CommonHelper.taskFeatureActionValues["update"] + "",
												name: "Redagavimas"
											},{
												code: CommonHelper.taskFeatureActionValues["delete"] + "",
												name: "Demontavimas"
											}]
										},
										type: "esriFieldTypeString",
										editable: true,
										nullable: false
									});
									origFeatureActionItem.title = "Objekto veiksmo tipas";
									items.push(origFeatureActionItem);
								}
							}
						}
					}
				}
				return items;
			},
			getId: function(i){
				var id = "my-input-" + i;
				if (this.id) {
					id = this.id + "-" + id;
				}
				return id;
			},
			getFormData: function(skipValidation){
				var formData;
				if (this.items) {
					var component,
						val,
						invalid = {},
						errorsIn = [],
						error;
					formData = {};
					this.items.forEach(function(item){
						if (item.field && item.rawField) {
							component = this.$refs[item.field.name];
							if (component && component.length == 1) {
								component = component[0];
								val = component.inputVal;
								error = false;
								if (["esriFieldTypeInteger", "esriFieldTypeSmallInteger", "esriFieldTypeOID"].indexOf(item.rawField.type) != -1) {
									val = parseInt(val);
									if (isNaN(val)) {
										val = null;
										if (component.inputVal) {
											error = true;
										}
									}
								} else if (item.rawField.type == "esriFieldTypeDouble") {
									val = parseFloat(val);
									if (isNaN(val)) {
										val = null;
										if (component.inputVal) {
											error = true;
										}
									}
								} else if (item.rawField.type == "esriFieldTypeDate") {
									val = parseInt(val);
									if (isNaN(val)) {
										val = null;
										if (component.inputVal) {
											error = true;
										}
									}
								} else if (item.rawField.type == "esriFieldTypeString") {
									if (val) {
										val = val.trim();
									}
								}
								if (!item.rawField.nullable) {
									if (!val && (val !== 0)) {
										if (item.rawField.name != CommonHelper.defaultLengthFieldName) { // Šiam laukui leidžiama neturėti reikšmės... Aktualu naujo objekto kūrime...
											error = true;
										}
									}
								}
								if (val || val === 0) {
									if (item.rawField.domain && item.rawField.domain.codedValues) {
										var codedValueExists = false;
										item.rawField.domain.codedValues.some(function(codedValue){
											if (codedValue.code == val) {
												codedValueExists = true;
												return true;
											}
										});
										if (!codedValueExists) {
											// console.log("WEIRD CODED VALUE...", item.field.name, val); // Testavimui...
											error = true;
										}
									}
								} else {
									if (!val) {
										val = null;
									}
								}
								formData[item.field.name] = val;
								if (error) {
									invalid[item.field.name] = true;
									errorsIn.push(item);
								}
							} else {
								if (!component && item.hidden) {
									// Nieko nedarome, viskas OK... Taip yra paprastiems įvedėjams pvz. įvedant naują atramą...
								} else {
									errorsIn.push(item);
								}
							}
						} else {
							errorsIn.push(item);
						}
					}.bind(this));
					if (!skipValidation) {
						if (errorsIn.length) {
							this.invalid = invalid;
							// console.log("INVALID", errorsIn); // Testavimui...
							formData = "invalid";
						} else {
							// console.log("TIKRINTI DĖL UŽDAŽYMO...", formData, this.id);
							this.invalid = {};
						}
					}
				}
				return formData;
			},
			streetSignFeatureRotationChanged: function(e){
				this.setInputVal(e, "rotation-field");
			},
			streetSignFeatureNrChanged: function(e){
				this.setInputVal(e, "TIPAS");
				// BIG TODO!!! Jei čia keičiamas vertikaliojo numeris, tai analizuoti ar simbolis dar aktualus! Jei nebeaktualus, šalia jo rodyti šauktuką?
				// Išsaugant irgi paklausti ar tikrai saugoti... Nes gi simbolis neatitinka vertikaliojo numerio!!!
				var field = this.$refs[CommonHelper.customSymbolIdFieldName];
				if (field && e) {
					// TODO! Reikia panaudoti e.val!..
				}
			},
			setInputVal: function(e, fieldName){
				if (e && e.feature && !e.skipField) {
					if (fieldName == "rotation-field") {
						fieldName = e.feature.origRotation.field;
					}
					if (fieldName) {
						var field = this.$refs[fieldName];
						if (field && field.length == 1) {
							field = field[0];
							field.inputVal = e.feature.get(fieldName);
						}
					}
				}
			}
		},

		watch: {
			editingActive: {
				immediate: true,
				handler: function(editingActive){
					if (editingActive) {
						this.invalid = {};
					}
				}
			}
		}
	};
</script>