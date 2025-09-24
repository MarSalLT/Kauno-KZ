<template>
	<v-dialog
		v-model="dialog"
		:max-width="closable ? 1200 : null"
		:content-class="closable ? null : 'sc-dialog'"
		scrollable
	>
		<v-card>
			<v-card-title>
				<span>{{title}}</span>
			</v-card-title>
			<v-card-text>
				<template v-if="e">
					<template v-if="e.id">
						<template v-if="e.mode == 'edit'">
							<template v-if="featuresUsingUniqueSymbol">
								<template v-if="featuresUsingUniqueSymbol == 'error'">
									<v-alert
										dense
										type="error"
										class="ma-0 d-inline-block"
									>
										Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
									</v-alert>
								</template>
								<template v-else-if="featuresUsingUniqueSymbol.length > 1">
									<v-alert
										dense
										type="warning"
										class="ma-0 d-inline-block"
									>
										Simbolį naudojančių objektų skaičius: {{featuresUsingUniqueSymbol.length}}. Galima redaguoti tik tokius simbolius, kuriuos naudoja tik vienas objektas.
									</v-alert>
								</template>
								<template v-else>
									<StreetSignSymbolDashboard
										:data="uniqueSymbolData"
										:mode="e.mode"
										:onClose="closeDialog"
										:onSave="onSave"
										:key="key"
									/>
								</template>
							</template>
							<template v-else>
								<v-progress-circular
									indeterminate
									color="primary"
									:size="30"
									width="2"
								></v-progress-circular>
							</template>
						</template>
					</template>
					<template v-else-if="possibleUniqueSymbols">
						<template v-if="possibleUniqueSymbols == 'loading'">
							<v-progress-circular
								indeterminate
								color="primary"
								:size="30"
								width="2"
							></v-progress-circular>
						</template>
						<template v-else-if="possibleUniqueSymbols == 'error'">
							<v-alert
								dense
								type="error"
								class="ma-0 d-inline-block"
							>
								Atsiprašome, įvyko nenumatyta klaida... Pabandykite vėliau...
							</v-alert>
						</template>
						<template v-else>
							<div class="mb-2">Pagal kelio ženklo teksto reikšmę `{{e.textValue}}` buvo atlikta paieška egzistuojančiuose simboliuose. Rasti šie simboliai:</div>
							<v-radio-group
								v-model="selectedUniqueSymbol"
								class="ma-0 pa-0"
								hide-details
								row
							>
								<template v-for="(image, i) in possibleUniqueSymbols">
									<v-radio
										:label="image.id"
										:value="image.id"
										:key="i"
										:class="['ma-0 pa-0 mr-4']"
									>
										<template v-slot:label>
											<img
												:src="image.src"
												:style="{width: image['img_width'], height: image['img_height']}"
											/>
										</template>
									</v-radio>
								</template>
							</v-radio-group>
							<div class="mt-3 d-flex">
								<v-btn
									color="blue darken-1"
									text
									v-on:click="setSelectedSymbol"
									outlined
									small
									class="mr-1"
									:disabled="!Boolean(selectedUniqueSymbol)"
								>
									Naudoti pasirinktą
								</v-btn>
								<v-btn
									color="blue darken-1"
									text
									v-on:click="ignorePossibleUniqueSymbols"
									outlined
									small
									class="mr-1"
								>
									Kurti naują
								</v-btn>
							</div>
						</template>
					</template>
					<template v-else>
						<StreetSignSymbolDashboard
							:data="e"
							:mode="e.mode ? e.mode : 'create'"
							:onClose="closeDialog"
							:onSave="onSave"
							:key="key"
						/>
					</template>
				</template>
			</v-card-text>
			<v-card-actions class="mx-2 pb-5 pt-5" v-if="closable">
				<v-btn
					color="blue darken-1"
					text
					v-on:click="dialog = false"
					outlined
					small
				>
					Atšaukti
				</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import StreetSignSymbolDashboard from "../sc/StreetSignSymbolDashboard";
	import StreetSignsSymbolsManagementHelper from "../helpers/StreetSignsSymbolsManagementHelper";

	export default {
		data: function(){
			var data = {
				dialog: false,
				e: null,
				key: 0,
				title: null,
				closable: false,
				featuresUsingUniqueSymbol: null,
				possibleUniqueSymbols: null,
				uniqueSymbolData: null,
				selectedUniqueSymbol: null
			};
			return data;
		},

		components: {
			StreetSignSymbolDashboard
		},

		created: function(){
			this.$vBus.$on("show-street-sign-symbol-creator-dialog", this.showDialog);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-street-sign-symbol-creator-dialog", this.showDialog);
		},

		methods: {
			showDialog: function(e){
				this.key += 1;
				this.e = e;
				var title = "Naujo simbolio kūrimas";
				if (e && e.id) {
					title = "Simbolio redagavimas";
				}
				this.title = title;
				this.possibleUniqueSymbols = this.featuresUsingUniqueSymbol = this.uniqueSymbolData = this.selectedUniqueSymbol = null;
				this.closable = false;
				if (e) {
					if (e.id) {
						this.closable = true;
						StreetSignsSymbolsManagementHelper.getFeaturesUsingUniqueSymbol(e.id).then(function(list){
							if (list) {
								if (list.length < 2) {
									StreetSignsSymbolsManagementHelper.getUniqueSymbolData(e.id).then(function(symbolData){
										symbolData.mode = "edit"; // Šiaip nesąmonė, kad tokio hack'o reikia...
										symbolData.vsss = e.vsss;
										// symbolData.vss = e.vss; // Ne, šito perduoti nereikia... Jei jis bus, tai gaus feature'o duomenis ir perrašys `inputVal`?..
										this.featuresUsingUniqueSymbol = list;
										this.uniqueSymbolData = symbolData;
										this.closable = false;
									}.bind(this), function(){
										this.featuresUsingUniqueSymbol = "error";
									}.bind(this));
								} else if (list.length > 1) {
									this.featuresUsingUniqueSymbol = list;
								}
							} else {
								this.featuresUsingUniqueSymbol = "error";
							}
						}.bind(this), function(){
							this.featuresUsingUniqueSymbol = "error";
						}.bind(this));
					} else if (e.trySuggestingExistingSymbolByValue && e.textValue) {
						if (CommonHelper.scUniqueVeryBasicContent.indexOf(e.type) != -1) {
							this.possibleUniqueSymbols = "loading";
							this.closable = true;
							StreetSignsSymbolsManagementHelper.getUniqueSymbols({
								type: e.type
							}).then(function(list){
								var possibleUniqueSymbols = [];
								if (list) {
									if (process.env.VUE_APP_SC_TYPE != "test") {
										list.forEach(function(item){
											item.id = "{" + item.id.toUpperCase() + "}";
										});
									}
									var itemData;
									list.forEach(function(item){
										if (item.data) {
											try {
												itemData = JSON.parse(item.data);
												if (itemData.val == e.textValue) {
													item.src = CommonHelper.getUniqueSymbolSrc(item.id, Date.now());
													possibleUniqueSymbols.push(item);
												}
											} catch(err) {
												// ...
											}
										}
									});
								}
								if (possibleUniqueSymbols.length) {
									this.selectedUniqueSymbol = possibleUniqueSymbols[0].id;
								} else {
									possibleUniqueSymbols = null;
									this.closable = false;
								}
								this.possibleUniqueSymbols = possibleUniqueSymbols;
							}.bind(this), function(){
								this.possibleUniqueSymbols = "error";
							}.bind(this));
						}
					}
					this.dialog = true;
				}
			},
			setSelectedSymbol: function(){
				this.onSave(this.selectedUniqueSymbol);
			},
			closeDialog: function(){
				this.dialog = false;
			},
			onSave: function(val){
				this.closeDialog();
				if (this.e.onSave) {
					this.e.onSave(val);
				}
			},
			ignorePossibleUniqueSymbols: function(){
				this.possibleUniqueSymbols = null;
				this.closable = false;
			}
		}
	}
</script>