<template>
	<div>
		<v-checkbox
			v-model="params.checked"
			:label="title"
			class="ma-0 pa-0"
			hide-details
		></v-checkbox>
		<div class="mt-2 ml-8">
			<template v-if="type == 'up'">
				<div>
					<div class="mb-2">
						<div class="mb-1">Koto tipas:</div>
						<v-radio-group
							v-model="params.rootType"
							class="ma-0 pa-0 ml-7"
							hide-details
							column
							:disabled="!Boolean(params.checked)"
						>
							<v-radio
								label="Tiesus"
								value="straight"
								class="ma-0 pa-0 mb-1"
							></v-radio>
							<v-radio
								label="Su vingiu"
								value="with-curve"
								class="ma-0 pa-0 mb-1"
							></v-radio>
							<div v-if="params.rootType == 'with-curve'">
								<v-radio-group
									v-model="params.rootCurveSide"
									class="ma-0 pa-0 ml-7"
									hide-details
									column
								>
									<v-radio
										label="Vingis kairiau"
										value="left"
										class="ma-0 pa-0 mb-1"
									></v-radio>
									<v-radio
										label="Vingis dešiniau"
										value="right"
										class="ma-0 pa-0 mb-1"
									></v-radio>
								</v-radio-group>
								<div class="mt-1 body-2 ml-7">
									<div>Delta X:</div>
									<v-slider
										v-model="params.curveDeltaX"
										min="0"
										max="1"
										step="0.05"
										thumb-label
										ticks
										class="ma-0 pa-0 ml-7"
										hide-details
										:disabled="!Boolean(params.checked)"
									></v-slider>
								</div>
								<div class="mt-1 body-2 ml-7">
									<div>Y pasiskirstymas:</div>
									<v-range-slider
										v-model="params.rootCurveYProportions"
										min="0.1"
										max="0.9"
										step="0.1"
										thumb-label
										ticks
										class="ma-0 pa-0 ml-7"
										hide-details
										:disabled="!Boolean(params.checked)"
									></v-range-slider>
								</div>
							</div>
							<v-radio
								label="Su lanku viršuje"
								value="with-top-arc"
								class="ma-0 pa-0 mb-1"
							></v-radio>
							<div v-if="params.rootType == 'with-top-arc'">
								<v-radio-group
									v-model="params.rootTopArcSide"
									class="ma-0 pa-0 ml-7"
									hide-details
									column
								>
									<v-radio
										label="Lankas kairiau"
										value="left"
										class="ma-0 pa-0 mb-1"
									></v-radio>
									<v-radio
										label="Lankas dešiniau"
										value="right"
										class="ma-0 pa-0 mb-1"
									></v-radio>
								</v-radio-group>
								<div class="mt-1 body-2 ml-7">
									<div>Delta X:</div>
									<v-slider
										v-model="params.topArcDeltaX"
										min="0"
										max="1"
										step="0.05"
										thumb-label
										ticks
										class="ma-0 pa-0 ml-7"
										hide-details
										:disabled="!Boolean(params.checked)"
									></v-slider>
								</div>
								<div class="mt-1 body-2 ml-7">
									<div>Delta Y:</div>
									<v-slider
										v-model="params.topArcDeltaY"
										min="0.05"
										max="0.95"
										step="0.05"
										thumb-label
										ticks
										class="ma-0 pa-0 ml-7"
										hide-details
										:disabled="!Boolean(params.checked)"
									></v-slider>
								</div>
							</div>
							<v-radio
								label="Su pusapskritimiu apačioje"
								value="with-bottom-arc"
								class="ma-0 pa-0 mb-1 d-none"
								disabled
							></v-radio>
							<div v-if="params.rootType == 'with-bottom-arc'">
								<v-radio-group
									v-model="params.rootBottomArcSide"
									class="ma-0 pa-0 ml-7"
									hide-details
									column
								>
									<v-radio
										label="Lankas kairiau"
										value="left"
										class="ma-0 pa-0 mb-1"
									></v-radio>
									<v-radio
										label="Lankas dešiniau"
										value="right"
										class="ma-0 pa-0 mb-1"
									></v-radio>
								</v-radio-group>
							</div>
						</v-radio-group>
					</div>
					<template v-if="model.secondary && (params.rootType == 'with-curve')">
						<div class="mb-1">
							<div>Koto nuokrypis nuo centro:</div>
							<v-slider
								v-model="params.rootDeltaX"
								min="0"
								max="1"
								step="0.05"
								thumb-label
								ticks
								class="ma-0 pa-0 ml-7"
								hide-details
								:disabled="!Boolean(params.checked)"
							></v-slider>
						</div>
					</template>
					<div class="mb-1 mt-n1">
						<div class="mb-1">
							<v-checkbox
								v-model="params.restricted"
								label="Važiuoti draudžiama"
								class="ma-0 pa-0"
								hide-details
								:disabled="!Boolean(params.checked)"
							></v-checkbox>
							<div v-if="params.restricted">
								<v-checkbox
									v-model="params.restrictedCargo"
									label="Tik krovin."
									class="ma-0 pa-0 mt-2 ml-8"
									hide-details
									:disabled="!Boolean(params.checked)"
								></v-checkbox>
								<div class="mt-2 mb-1 ml-8">
									<div>Simbolio padėtis:</div>
									<v-slider
										v-model="params.restrictedDistanceFrom"
										min="0"
										max="1"
										step="0.02"
										thumb-label
										ticks
										class="ma-0 pa-0 ml-7"
										hide-details
										:disabled="!Boolean(params.checked)"
									></v-slider>
								</div>
							</div>
						</div>
					</div>
				</div>
			</template>
			<template v-if="(type == 'left') || (type == 'right')">
				<div>
					<div class="mb-2">
						<div class="mb-1">Galvos pasūkimo kryptis:</div>
						<v-radio-group
							v-model="params.headAngle"
							class="ma-0 pa-0 ml-7"
							hide-details
							column
							:disabled="!Boolean(params.checked)"
						>
							<v-radio
								:label="(type == 'left') ? 'ŠV' : 'ŠR'"
								:value="135"
								class="ma-0 pa-0 mb-1"
							></v-radio>
							<v-radio
								:label="(type == 'left') ? 'V' : 'R'"
								:value="90"
								class="ma-0 pa-0 mb-1"
							></v-radio>
							<v-radio
								:label="(type == 'left') ? 'PV' : 'PR'"
								:value="45"
								class="ma-0 pa-0 mb-1"
							></v-radio>
							<v-radio
								label="P"
								:value="0"
								class="ma-0 pa-0"
							></v-radio>
						</v-radio-group>
					</div>
					<div class="mb-1 mt-3">
						<div>Delta X:</div>
						<v-slider
							v-model="params.deltaX"
							min="0.10"
							max="1"
							step="0.05"
							thumb-label
							ticks
							class="ma-0 pa-0 ml-7"
							hide-details
							:disabled="!Boolean(params.checked)"
						></v-slider>
					</div>
					<div class="mb-1">
						<div>Delta Y:</div>
						<v-slider
							v-model="params.deltaY"
							min="0.20"
							max="1.15"
							step="0.05"
							thumb-label
							ticks
							class="ma-0 pa-0 ml-7"
							hide-details
							:disabled="!Boolean(params.checked)"
						></v-slider>
					</div>
					<template v-if="params.headAngle == 0">
						<div class="mb-1">
							<div>Delta Y žemyn:</div>
							<v-slider
								v-model="params.deltaYNegative"
								min="0.20"
								max="0.60"
								step="0.05"
								thumb-label
								ticks
								class="ma-0 pa-0 ml-7"
								hide-details
								:disabled="!Boolean(params.checked)"
							></v-slider>
						</div>
					</template>
					<div class="mb-2" v-if="(params.headAngle == 90) || (params.headAngle == 135)">
						<div class="mb-1">Rodyklės tipas:</div>
						<v-radio-group
							v-model="params.sideArrowType"
							class="ma-0 pa-0 ml-7"
							hide-details
							column
							:disabled="!Boolean(params.checked)"
						>
							<v-radio
								label="Su kaklu"
								value="with-neck"
								class="ma-0 pa-0 mb-1"
							></v-radio>
							<v-radio
								label="Sklandi"
								value="smooth"
								class="ma-0 pa-0"
							></v-radio>
						</v-radio-group>
					</div>
					<template v-if="(params.sideArrowType == 'with-neck') || ([90, 135].indexOf(params.headAngle) == -1)">
						<div class="mb-2">
							<v-checkbox
								v-model="params.neckRounded"
								label="Kaklas suapvalintas"
								class="ma-0 pa-0"
								hide-details
								:disabled="!Boolean(params.checked)"
							></v-checkbox>
						</div>
						<div class="mb-2">
							<v-checkbox
								v-model="params.bendToLeft"
								:label="type == 'left' ? 'Su vingiu kairiau' : 'Su vingiu dešiniau'"
								class="ma-0 pa-0 d-none"
								hide-details
								:disabled="true || !Boolean(params.checked)"
								v-if="params.headAngle == 0"
							></v-checkbox>
						</div>
					</template>
					<div class="mb-1" v-if="([0].indexOf(params.headAngle) == -1)">
						<v-checkbox
							v-model="params.restricted"
							label="Važiuoti draudžiama"
							class="ma-0 pa-0"
							hide-details
							:disabled="!Boolean(params.checked)"
						></v-checkbox>
						<div v-if="params.restricted">
							<v-checkbox
								v-model="params.restrictedCargo"
								label="Tik krovin."
								class="ma-0 pa-0 mt-2 ml-8"
								hide-details
								:disabled="!Boolean(params.checked)"
							></v-checkbox>
							<div class="mt-2 mb-1 ml-8">
								<div>Simbolio padėtis:</div>
								<v-slider
									v-model="params.restrictedDistanceFrom"
									min="0"
									max="1"
									step="0.02"
									thumb-label
									ticks
									class="ma-0 pa-0 ml-7"
									hide-details
									:disabled="!Boolean(params.checked)"
								></v-slider>
							</div>
						</div>
					</div>
				</div>
			</template>
			<div class="mt-3 d-none">
				<div class="mb-1">Rodyklė prasideda nuo:</div>
				<div class="ma-0 pa-0 ml-7">Kuriama...</div>
			</div>
		</div>
	</div>
</template>

<script>
	import Vue from "vue";

	export default {
		data: function(){
			var type,
				title,
				params = {};
			if (this.model) {
				type = this.model.type;
				if (type == "left") {
					title = "Kairiau";
				} else if (type == "right") {
					title = "Dešiniau";
				} else if (type == "up") {
					title = "Į viršų";
				} else if (type == "down") {
					title = "Žemyn";
				}
				if (type == "up") {
					params = {
						rootType: "straight",
						rootCurveSide: "right",
						rootBottomArcSide: "left",
						rootTopArcSide: "left",
						rootCurveYProportions: [0.3, 0.7],
						curveDeltaX: 0.25,
						topArcDeltaX: 0.5,
						topArcDeltaY: 0.5,
						rootDeltaX: 0,
						restrictedDistanceFrom: 0.2
					};
				} else if (type == "down") {
					// ...
				} else {
					params = {
						deltaX: 0.25,
						deltaY: 0.75,
						deltaYNegative: 0.25,
						neckRounded: true,
						headAngle: 90,
						sideArrowType: "with-neck",
						restrictedDistanceFrom: 0.2
					};
				}
			}
			var data = {
				title: title,
				type: type,
				params: Object.assign(params, this.model.params) // FIXME! Nežinau ar taip gerai...
			};
			return data;
		},

		props: {
			model: Object
		},

		methods: {
			// ...
		},

		watch: {
			params: {
				deep: true,
				immediate: true,
				handler: function(params){
					Vue.set(this.model, "params", params);
				}
			}
		}
	};
</script>

<style scoped>
	.v-select {
		width: 150px !important;
	}
</style>