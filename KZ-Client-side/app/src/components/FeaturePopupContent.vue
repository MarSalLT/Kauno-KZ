<template>
	<div>
		<div
			:class="[mode ? 'd-none' : null]"
			v-if="!data.isNew"
		>
			<template v-if="data.historicMoment">
				<div class="body-2 mb-2 warning pa-1">Retrospektyvinė informacija datai: {{getPrettyDate(data.historicMoment)}}</div>
			</template>
			<template v-if="data.featureType == 'verticalStreetSigns' || data.featureType == 'verticalStreetSignsSupports'">
				<VerticalStreetSignsData
					:data="data"
					:editingActive="editingActive"
					ref="verticalStreetSignsData"
				/>
			</template>
			<template v-else-if="['vms-inventorization-l', 'vms-inventorization-p', 'vvt'].indexOf(data.featureType) != -1">
				<AttributesTable
					title="Objekto informacija:"
					:data="data"
					:editingActive="editingActive"
					ref="attributesTable"
				/>
				<FeatureHistory
					:data="data"
					:class="['mt-2', editingActive ? 'd-none' : null]"
					v-if="data.featureType == 'vvt'"
				/>
				<FeaturePhotosCarousel
					title="Nuotrauka:"
					:feature="data.feature"
					:featureType="data.featureType"
					:historicMoment="data.historicMoment"
					class="mt-2"
					v-if="data.featureType != 'vvt'"
				/>
			</template>
			<template v-else-if="['general-object'].indexOf(data.featureType) != -1">
				<AttributesTable
					:title="(data.title || 'Objekto informacija') + ':'"
					:data="data"
					:key="key"
				/>
				<FeaturePhotosCarousel
					title="Nuotrauka:"
					:feature="data.feature"
					class="mt-2"
					v-if="data.showAttachments"
				/>
			</template>
			<template v-else>
				<AttributesTable
					title="Objekto informacija:"
					:data="data"
					:editingActive="editingActive"
					ref="attributesTable"
					id="objects-attr"
				/>
				<template v-if="data.type != 'task-related' && data.type != 'waze'">
					<template v-if="['userPoints'].indexOf(data.featureType) == -1">
						<FeatureNotesInfo
							:data="data"
							:editingActive="editingActive"
							class="mt-2"
							ref="featureNotesInfo"
						/>
						<FeatureAdditionalInfo
							:data="data"
							:class="['mt-2', editingActive ? 'd-none' : null]"
						/>
						<FeatureHistory
							:data="data"
							:class="['mt-2', editingActive ? 'd-none' : null]"
						/>
					</template>
					<template v-if="['horizontalPoints', 'horizontalPolylines', 'horizontalPolygons', 'otherPoints', 'otherPolylines', 'otherPolygons'].indexOf(data.featureType) != -1">
						<FeaturePhotosCarousel
							title="Nuotrauka:"
							:feature="data.feature"
							:featureType="data.featureType"
							:historicMoment="data.historicMoment"
							class="mt-2"
						/>
					</template>
				</template>
			</template>
		</div>
		<template v-if="mode == 'vertical-street-sign-addition'">
			<TemplatePicker
				:featureTypes="['verticalStreetSigns']"
				:filterNeeded="true"
				:onItemSelect="onVerticalStreetSignPick"
				:returnCompleteInfo="true"
				ref="templatePicker"
			/>
		</template>
		<template v-if="(mode == 'new-feature') || (mode == 'new-feature-vertical-sign')">
			<AttributesTable
				title="Objekto informacija:"
				:data="activeNewFeatureData"
				:editingActive="true"
				ref="newFeatureAttributesTable"
			/>
			<template v-if="(activeNewFeatureData.type != 'task-related') && (activeNewFeatureData.featureType != 'userPoints')">
				<FeatureNotesInfo
					:data="activeNewFeatureData"
					:editingActive="true"
					class="mt-2"
					ref="newFeatureNotesInfo"
				/>
			</template>
		</template>
	</div>
</template>

<script>
	import AttributesTable from "./AttributesTable";
	import CommonHelper from "../components/helpers/CommonHelper";
	import FeatureAdditionalInfo from "./FeatureAdditionalInfo";
	import FeatureHistory from "./FeatureHistory";
	import FeatureNotesInfo from "./FeatureNotesInfo";
	import FeaturePhotosCarousel from "./FeaturePhotosCarousel";
	import TemplatePicker from "./TemplatePicker";
	import VerticalStreetSignsData from "./VerticalStreetSignsData";

	export default {
		data: function(){
			var data = {
				verticalStreetSignDrawingActive: false,
				key: 0
			};
			return data;
		},

		props: {
			data: Object,
			editingActive: Boolean,
			mode: String,
			activeNewFeatureData: Object
		},

		components: {
			AttributesTable,
			FeatureAdditionalInfo,
			FeaturePhotosCarousel,
			FeatureHistory,
			FeatureNotesInfo,
			TemplatePicker,
			VerticalStreetSignsData
		},

		beforeDestroy: function(){
			this.deactivateVerticalStreetSignDrawing();
		},

		methods: {
			getFormData: function(){
				var formData;
				if ((this.mode == "new-feature") || (this.mode == "new-feature-vertical-sign")) {
					if (this.$refs.newFeatureAttributesTable) {
						formData = this.$refs.newFeatureAttributesTable.getFormData();
					}
					if (this.$refs.newFeatureNotesInfo) {
						if (formData) {
							formData = Object.assign(formData, this.$refs.newFeatureNotesInfo.getFormData());
						}
					}
				} else {
					if (this.editingActive) {
						if (["verticalStreetSigns", "verticalStreetSignsSupports"].indexOf(this.data.featureType) != -1) {
							if (this.$refs.verticalStreetSignsData) {
								formData = this.$refs.verticalStreetSignsData.getFormData();
							}
						} else {
							if (this.$refs.attributesTable) {
								formData = this.$refs.attributesTable.getFormData();
							}
						}
						if (this.$refs.featureNotesInfo) {
							if (formData) {
								formData = Object.assign(formData, this.$refs.featureNotesInfo.getFormData());
							}
						}
					}
				}
				return formData;
			},
			onVerticalStreetSignPick: function(e){
				this.verticalStreetSignDrawingActive = e;
			},
			activateVerticalStreetSignDrawing: function(e){
				if (this.data.featureType == "verticalStreetSignsSupports") {
					this.$vBus.$emit("draw-temp-circles", this.data.feature);
				}
				if (!this.drawInteraction) {
					this.drawInteraction = this.$store.state.myMap.createDrawInteraction(e);
					if (this.drawInteraction) {
						this.drawInteraction.on("drawend", function(drawE){
							if (this.data.featureType == "verticalStreetSignsSupports") {
								drawE.feature.set("GUID", "{" + this.data.globalId + "}");
							}
							this.$vBus.$emit("new-feature-drawn", {
								feature: drawE.feature,
								templateData: e,
								refData: this.data
							});
							if (this.$refs.templatePicker) {
								this.$refs.templatePicker.activeTemplateItem = undefined;
							}
						}.bind(this));
					} else {
						if (this.$refs.templatePicker) {
							setTimeout(function(){
								this.$refs.templatePicker.activeTemplateItem = undefined;
							}.bind(this), 0);
						}
						this.$vBus.$emit("show-message", {
							type: "warning"
						});
					}
				}
			},
			deactivateVerticalStreetSignDrawing: function(){
				if (this.mode != "new-feature-vertical-sign") { // Ką tik pridėjome naują KŽ ir mums tikrai neaktualu valyti tų tarpinių apskritimų...
					this.$vBus.$emit("draw-temp-circles");
				}
				if (this.drawInteraction) {
					this.$store.state.myMap.removeInteraction(this.drawInteraction);
					this.drawInteraction = null;
				}
			},
			getPrettyDate: function(date){
				date = CommonHelper.getPrettyDate(parseInt(date), true);
				return date;
			}
		},

		watch: {
			mode: {
				immediate: true,
				handler: function(){
					this.verticalStreetSignDrawingActive = false;
				}
			},
			verticalStreetSignDrawingActive: {
				immediate: true,
				handler: function(e){
					if (e) {
						this.activateVerticalStreetSignDrawing(e);
					} else {
						this.deactivateVerticalStreetSignDrawing();
					}
				}
			},
			data: {
				immediate: true,
				handler: function(){
					this.key += 1;
				}
			}
		}
	}
</script>