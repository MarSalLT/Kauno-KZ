<template>
	<div
		v-if="canDraw && !(activeFeature || activeAction)"
		class="d-flex stop-event popup-wrapper"
	>
		<v-btn
			fab
			small
			title="Kurti naują objektą"
			:elevation="2"
			v-if="!isOpen"
			:color="activeTask ? 'success' : 'primary'"
			v-on:click="open"
			class="ml-2"
		>
			<v-icon color="white">mdi-pencil</v-icon>
		</v-btn>
		<div
			class="popup d-flex flex-column"
			v-if="isOpen"
			:key="key"
		>
			<div :class="['header d-flex align-center pa-1 white--text', activeTask ? 'success' : 'primary']">
				<span class="ml-2">Naujo objekto kūrimas</span>
				<div class="d-flex flex-grow-1 justify-end">
					<v-btn
						icon
						v-on:click="isOpen = false"
						class="ml-1"
						small
						color="white"
					>
						<v-icon
							title="Uždaryti"
						>
							mdi-close
						</v-icon>
					</v-btn>
				</div>
			</div>
			<div class="content pa-3 d-flex flex-column">
				<v-btn-toggle
					v-model="activeTab"
					group
					mandatory
					tile
					class="my-tabs"
					v-if="tabs"
				>
					<template v-for="(item, index) in tabs">
						<div 
							:key="index"
						>
							<template v-if="item.icon">
								<v-btn
									small
									:class="['py-1 px-4 d-block', index ? 'ml-1' : null]"
									elevation="0"
									icon
									:title="item.title"
									:tile="true"
									:ripple="false"
								>
									<v-icon :color="item.color"
									>
										{{item.icon}}
									</v-icon>
								</v-btn>
							</template>
							<template v-else>
								<v-btn
									small
									:class="['py-1 px-4 d-block', index ? 'ml-1' : null]"
									elevation="0"
									:tile="true"
									:ripple="false"
									v-html="item.title"
								>
								</v-btn>
							</template>
						</div>
					</template>
				</v-btn-toggle>
				<div class="tabs-content flex-grow-1" ref="content">
					<template v-if="tabs && tabs.length">
						<div class="pa-4">
							<v-carousel
								v-model="activeTab"
								height="100%"
								:show-arrows="false"
								hide-delimiters
								light
							>
								<v-carousel-item
									v-for="(tab, index) in tabs"
									:key="index"
								>
									<TemplatePicker
										:featureTypes="tab.featureTypes"
										:templatePickerConfig="tab.templatePickerConfig"
										:onItemSelect="onTemplatePick"
										:returnCompleteInfo="true"
										ref="templatePicker"
									/>
								</v-carousel-item>
							</v-carousel>
						</div>
					</template>
					<template v-else>
						<div class="body-2 pa-3">Įrankių nėra...</div>
					</template>
				</div>
			</div>
		</div>
	</div>
</template>

<script>
	import TemplatePicker from "./TemplatePicker";

	export default {
		data: function(){
			var canDraw = false;
			if (this.$store.state.userData && this.$store.state.userData.permissions) {
				if (this.$store.state.userData.permissions.length) { // TODO, FIXME! Dabar sąlyga tokia primityvi...
					canDraw = true;
				}
			}
			var data = {
				type: "feature-drawing-manager",
				canDraw: canDraw,
				isOpen: false,
				isHidden: false,
				tabs: null,
				activeTab: 0,
				key: 0
			};
			return data;
		},

		components: {
			TemplatePicker
		},

		computed: {
			activeFeature: {
				get: function(){
					return this.$store.state.activeFeature;
				}
			},
			activeAction: {
				get: function(){
					return this.$store.state.activeAction;
				}
			},
			userData: {
				get: function(){
					return this.$store.state.userData;
				}
			},
			activeTask: {
				get: function(){
					var activeTask = null;
					if (this.$store.state.activeTask) {
						var feature = this.$store.state.activeTask.feature;
						if (feature && feature != "error") {
							activeTask = this.$store.state.activeTask;
						}
					}
					return activeTask;
				}
			}
		},

		methods: {
			open: function(){
				this.isOpen = true;
			},
			onTemplatePick: function(template){
				this.deactivateDrawing();
				if (template) {
					this.activateDrawing(template);
				}
			},
			resetTemplatePickers: function(){
				if (this.$refs.templatePicker) {
					this.$refs.templatePicker.forEach(function(templatePicker){
						templatePicker.activeTemplateItem = undefined;
					});
				}
			},
			activateDrawing: function(e){
				if (!this.drawInteraction) {
					this.drawInteraction = this.$store.state.myMap.createDrawInteraction(e);
					if (this.drawInteraction) {
						this.drawInteraction.on("drawend", function(drawE){
							this.$vBus.$emit("new-feature-drawn", {
								feature: drawE.feature,
								templateData: e
							});
							this.resetTemplatePickers();
						}.bind(this));
					} else {
						setTimeout(function(){
							this.resetTemplatePickers();
						}.bind(this), 0);
						this.$vBus.$emit("show-message", {
							type: "warning"
						});
					}
				}
			},
			deactivateDrawing: function(){
				if (this.drawInteraction) {
					this.$store.state.myMap.removeInteraction(this.drawInteraction);
					this.drawInteraction = null;
				}
				if (this.$store.state.myMap) {
					this.$store.state.myMap.removeSnapInteraction();
					this.$store.state.myMap.removeMeasureTooltip();
				}
			},
			setTabs: function(){
				this.resetTemplatePickers();
				setTimeout(function(){
					var tabs = [];
					if (this.userData) {
						if (this.userData.permissions) {
							if ((this.userData.permissions.indexOf("kz-vertical-edit") != -1) || ((this.userData.permissions.indexOf("manage-tasks-test") != -1) && this.activeTask)) {
								tabs.push({
									title: "Vertikalusis<br />ženklinimas",
									featureTypes: ["verticalStreetSignsSupports"]
								});
							}
							if ((this.userData.permissions.indexOf("kz-horizontal-edit") != -1) || ((this.userData.permissions.indexOf("manage-tasks-test") != -1) && this.activeTask)) {
								tabs.push({
									title: "Horizontalusis<br />ženklinimas",
									featureTypes: ["horizontalPoints", "horizontalPolylines", "horizontalPolygons"]
								});
							}
							if ((this.userData.permissions.indexOf("kz-infra-edit") != -1) || ((this.userData.permissions.indexOf("manage-tasks-test") != -1) && this.activeTask)) {
								tabs.push({
									title: "Kiti objektai",
									featureTypes: ["otherPoints", "otherPolylines", "otherPolygons"]
								});
							}
							tabs.push({
								title: "Mano objektai",
								icon: "mdi-map-marker-star",
								color: "green darken-3",
								featureTypes: ["userPoints"],
								templatePickerConfig: {
									noTemplateNames: true
								}
							});
						}
					}
					this.tabs = tabs;
					this.activeTab = 0;
					this.key += 1;
				}.bind(this), 0);
			}
		},

		watch: {
			activeFeature: {
				immediate: true,
				handler: function(activeFeature){
					if (activeFeature) {
						this.isOpen = false; // T. y. jei aktyvuojame kokį nors `feature` pvz. click'indami ant žemėlapio... Ir to `feature` popup'ą uždarome, šis manager'is turi
						// susiskleisti iki apskritiminio mygtuko su pieštuku...
					}
				}
			},
			userData: {
				immediate: true,
				handler: function(){
					this.setTabs();
				}
			},
			activeTab: {
				immediate: true,
				handler: function(){
					this.resetTemplatePickers();
					if (this.$refs.content) {
						this.$refs.content.scrollTop = 0;
					}
				}
			},
			isOpen: {
				immediate: true,
				handler: function(isOpen){
					if (isOpen) {
						this.$store.commit("setMapNewItemState", this.type);
					} else {
						this.$store.commit("setMapNewItemState", null);
						this.deactivateDrawing();
					}
				}
			},
			"$store.state.newItemState": {
				immediate: true,
				handler: function(newItemState){
					this.isHidden = Boolean(newItemState); // Kažkada buvo aktualu, bet dabar lyg ir nebe... Palikta istoriniais sumetimais...
				}
			},
			activeTask: {
				immediate: true,
				handler: function(){
					this.setTabs();
				}
			},
		}
	}
</script>

<style scoped>
	.popup-wrapper {
		max-height: 100%;
	}
	.popup {
		width: 500px;
		background-color: white;
		box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3) !important;
		overflow: hidden;
		overflow-y: auto;
	}
	.header {
		border-bottom: 1px solid #cccccc;
	}
	.content {
		overflow: hidden;
		overflow-y: auto;
		width: 100%;
	}
	.v-btn-toggle .v-btn {
		height: 100% !important;
		border: 1px solid #cacaca;
		z-index: 0;
	}
	.tabs-content {
		border: 1px solid #cacaca;
		margin-top: -1px;
		background-color: white;
		overflow: auto;
		z-index: 1;
	}
</style>