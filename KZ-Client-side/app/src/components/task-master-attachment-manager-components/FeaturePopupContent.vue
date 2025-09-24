<template>
	<div>
		<div class="body-2 pa-1">
			<template v-if="type == 'boundary'">
				<div>
					<div>Rėmelio dydis:</div>
					<v-select
						v-model="frameSize"
						dense
						hide-details
						:items="frameSizes"
						item-value="id"
						item-text="title"
						class="body-2 ma-0"
					></v-select>
				</div>
			</template>
			<template v-else-if="type == 'point'">
				<div class="mb-4">
					<div>Simbolio tipas:</div>
					<v-select
						v-model="pointType"
						dense
						hide-details
						:items="pointTypes"
						item-value="id"
						item-text="title"
						class="body-2 ma-0"
					></v-select>
				</div>
			</template>
			<template v-else-if="type == 'text'">
				<div class="mb-4">
					<div>Tekstas:</div>
					<v-text-field
						v-model="textValue"
						placeholder="Tekstas"
						dense
						hide-details
						class="body-2"
					>
					</v-text-field>
				</div>
				<div class="mb-4">
					<div>Teksto dydis:</div>
					<v-text-field
						v-model="textSize"
						dense
						hide-details
						class="body-2"
						type="number"
						:min="5"
						:max="30"
					>
					</v-text-field>
				</div>
			</template>
			<template v-else-if="type == 'arrow'">
				<div class="mb-4">
					<div>Rodyklės galvos dydis:</div>
					<v-text-field
						v-model="arrowHeadSize"
						dense
						hide-details
						class="body-2"
						type="number"
						:min="1"
					>
					</v-text-field>
				</div>
				<div class="mb-2">
					<div>Rodyklės galva su užpildu:</div>
					<v-checkbox
						v-model="arrowHeadFilled"
						:value="true"
						hide-details
						class="ma-0 pa-0 d-inline-block"
					></v-checkbox>
				</div>
			</template>
			<template v-if="(['point', 'polygon', 'rectangle', 'circle', 'text'].indexOf(type) != -1)">
				<div>
					<div>{{type == 'text' ? 'Teksto spalva' : 'Užpildo spalva'}}:</div>
					<v-color-picker
						v-model="fillColor"
						hide-inputs
						mode="rgba"
						swatches-max-height="100"
					></v-color-picker>
				</div>
			</template>
			<template v-if="(['line', 'polygon', 'rectangle', 'circle', 'arrow', 'image-mock-polygon'].indexOf(type) != -1)">
				<div>
					<div>{{type == "line" ? "Linijos spalva" : (type == 'arrow' ? "Rodyklės spalva" : "Kraštinės spalva")}}:</div>
					<v-color-picker
						v-model="strokeColor"
						hide-inputs
						mode="rgba"
						swatches-max-height="100"
					></v-color-picker>
				</div>
			</template>
			<template v-if="(['line', 'polygon', 'rectangle', 'circle', 'image-mock-polygon', 'arrow'].indexOf(type) != -1)">
				<div>
					<div>{{(type == "line" || type == "arrow") ? "Linijos storis" : "Kraštinės storis"}}:</div>
					<v-text-field
						v-model="strokeWidth"
						dense
						hide-details
						class="body-2"
						type="number"
						:min="(type == 'line' || type == 'arrow') ? 1 : 0"
						:max="100"
					>
					</v-text-field>
				</div>
			</template>
			<div v-if="(['boundary'].indexOf(type) == -1)" class="mt-4">
				<div>Objekto Z indeksas:</div>
				<v-text-field
					v-model="zIndex"
					dense
					hide-details
					class="body-2"
					type="number"
					:min="0"
					:max="1000"
				>
				</v-text-field>
			</div>
			<div v-if="(['text', 'image-mock-polygon'].indexOf(type) != -1)" class="mt-4">
				<div>Pasukimo kampas:</div>
				<v-text-field
					v-model="rotationAngle"
					dense
					hide-details
					class="body-2"
					type="number"
					:min="0"
					:max="360"
				>
				</v-text-field>
			</div>
		</div>
	</div>
</template>

<script>
	import TaskMasterAttachmentMapHelper from "../helpers/TaskMasterAttachmentMapHelper";
	import {getCenter as getExtentCenter} from "ol/extent";
	import Vue from "vue";

	export default {
		data: function(){
			var type;
			var data = {
				type: type,
				fillColor: {},
				strokeColor: {},
				strokeWidth: 0,
				pointType: null,
				pointTypes: [{
					id: "circle",
					title: "Apskritimas"
				},{
					id: "rectangle",
					title: "Kvadratas"
				}],
				frameSize: null,
				frameSizes: [{
					id: "a4-portrait",
					title: "A4, stačias"
				},{
					id: "a3-landscape",
					title: "A3, gulsčias"
				}],
				textValue: null,
				textSize: 0,
				arrowHeadSize: 0,
				arrowHeadFilled: false,
				zIndex: null,
				rotationAngle: null
			};
			return data;
		},

		props: {
			feature: Object,
			map: Object
		},

		methods: {
			getColor: function(arr){
				var color = {};
				if (arr) {
					if (Array.isArray(arr)) {
						color.r = arr[0];
						color.g = arr[1];
						color.b = arr[2];
						color.a = arr[3];
					}
				}
				return color;
			},
			getColor4Feature: function(color){
				var color4Feature = [];
				if (color) {
					color4Feature = [color.r, color.g, color.b, color.a];
				}
				return color4Feature;
			}
		},

		watch: {
			feature: {
				immediate: true,
				handler: function(feature){
					if (feature) {
						this.type = feature.get("type");
						this.fillColor = this.getColor(this.feature.get("fill-color"));
						this.strokeColor = this.getColor(this.feature.get("stroke-color"));
						this.strokeWidth = this.feature.get("stroke-width") || 0;
						this.pointType = this.feature.get("point-type");
						this.textValue = this.feature.get("text-value");
						this.textSize = this.feature.get("text-size") || 10;
						this.arrowHeadSize = this.feature.get("arrow-head-size") || 0;
						this.arrowHeadFilled = Boolean(this.feature.get("arrow-head-filled"));
						this.frameSize = this.feature.get("frame-size");
						this.zIndex = this.feature.get("z-index") || 0;
						this.rotationAngle = this.feature.get("rotation-angle") || 0;
					}
				}
			},
			fillColor: {
				immediate: false,
				handler: function(fillColor){
					if (this.feature) {
						this.feature.set("fill-color", this.getColor4Feature(fillColor));
					}
				}
			},
			strokeColor: {
				immediate: false,
				handler: function(strokeColor){
					if (this.feature) {
						this.feature.set("stroke-color", this.getColor4Feature(strokeColor));
					}
				}
			},
			strokeWidth: {
				immediate: false,
				handler: function(strokeWidth){
					if (this.feature) {
						this.feature.set("stroke-width", parseInt(strokeWidth));
					}
				}
			},
			pointType: {
				immediate: false,
				handler: function(pointType){
					if (this.feature && (this.type == "point")) {
						this.feature.set("point-type", pointType);
					}
				}
			},
			textValue: {
				immediate: false,
				handler: function(textValue){
					if (this.feature && (this.type == "text")) {
						this.feature.set("text-value", textValue);
					}
				}
			},
			textSize: {
				immediate: false,
				handler: function(textSize){
					if (this.feature && (this.type == "text")) {
						this.feature.set("text-size", textSize);
					}
				}
			},
			arrowHeadSize: {
				immediate: false,
				handler: function(arrowHeadSize){
					if (this.feature && (this.type == "arrow")) {
						this.feature.set("arrow-head-size", arrowHeadSize);
						Vue.set(this.feature, "styleGeom", null); // Numušame geometrijos cache'ą...
					}
				}
			},
			arrowHeadFilled: {
				immediate: false,
				handler: function(arrowHeadFilled){
					if (this.feature && (this.type == "arrow")) {
						this.feature.set("arrow-head-filled", arrowHeadFilled);
						Vue.set(this.feature, "styleGeom", null); // Numušame geometrijos cache'ą...
					}
				}
			},
			frameSize: {
				immediate: false,
				handler: function(frameSize){
					if (this.feature && (this.type == "boundary")) {
						this.feature.set("frame-size", frameSize);
						var extentCenter = getExtentCenter(this.feature.getGeometry().getExtent());
						this.feature.setGeometry(TaskMasterAttachmentMapHelper.getBoundaryGeometry(extentCenter, frameSize));
						if (this.feature.boundaryOutsideFeature) {
							this.feature.boundaryOutsideFeature.setGeometry(TaskMasterAttachmentMapHelper.getBoundaryOutsideGeometry(this.map, this.feature.getGeometry().getCoordinates()));
						}
					}
				}
			},
			zIndex: {
				immediate: false,
				handler: function(zIndex){
					if (this.feature) {
						this.feature.set("z-index", zIndex);
					}
				}
			},
			rotationAngle: {
				immediate: false,
				handler: function(rotationAngle){
					if (this.feature) {
						if (this.type == "image-mock-polygon") {
							TaskMasterAttachmentMapHelper.rotateImageMockPolygon(this.feature, rotationAngle);
						}
						this.feature.set("rotation-angle", rotationAngle);
					}
				}
			}
		}
	}
</script>