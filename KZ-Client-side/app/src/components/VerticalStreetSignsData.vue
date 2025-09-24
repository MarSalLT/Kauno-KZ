<template>
	<div>
		<template v-if="data.additionalData">
			<VerticalStreetSignSelector
				:data="data"
			/>
			<template v-if="data.featureType == 'verticalStreetSigns'">
				<AttributesTable
					title="Aktyvaus kelio ženklo informacija:"
					:data="{
						featureType: 'verticalStreetSigns',
						feature: data.feature
					}"
					:editingActive="editingActive"
					ref="verticalStreetSignAttributesTable"
					class="mt-2"
				/>
				<FeatureHistory
					title="Aktyvaus kelio ženklo redagavimo istorija:"
					:data="{
						globalId: data.feature.get('GlobalID'),
						layerId: data.layerId,
						featureType: data.featureType
					}"
					:class="['mt-2', editingActive ? 'd-none' : null]"
				/>
			</template>
			<template v-if="data.additionalData.supports && data.additionalData.supports.length == 1">
				<AttributesTable
					title="Atramos informacija:"
					:data="{
						featureType: 'verticalStreetSignsSupports',
						feature: data.additionalData.supports[0]
					}"
					ref="verticalStreetSignsSupportAttributesTable"
					:editingActive="editingActive && data.featureType == 'verticalStreetSignsSupports'"
					class="mt-2"
				/>
				<template v-if="data.type != 'task-related-v'">
					<template v-if="data.featureType == 'verticalStreetSignsSupports'">
						<FeatureHistory
							title="Atramos redagavimo istorija:"
							:data="{
								globalId: data.additionalData.supports[0].get('GlobalID'),
								layerId: data.additionalData.supports[0].layerId,
								featureType: data.featureType
							}"
							:class="['mt-2', editingActive ? 'd-none' : null]"
						/>
					</template>
					<FeaturePhotosCarousel
						title="Atramos nuotrauka:"
						:feature="data.additionalData.supports[0]"
						featureType="verticalStreetSignsSupports"
						:historicMoment="data.historicMoment"
						class="mt-2"
					/>
				</template>
			</template>
		</template>
		<template v-else-if="(data.type == 'task-related-v') && (data.featureType == 'verticalStreetSigns')">
			<div class="body-2 mb-2 warning pa-1">Funkcionalumas kuriamas... Kol kas rodoma tik kelio ženklo informacija be jokių sąsajų su atrama... Dėl to neveikia ir ženklo automatinis posūkio kampo parinkimas :)</div>
			<AttributesTable
				title="Aktyvaus kelio ženklo informacija:"
				:data="{
					featureType: 'verticalStreetSigns',
					feature: data.feature
				}"
				:editingActive="editingActive"
				ref="verticalStreetSignAttributesTable"
				class="mt-2"
			/>
		</template>
		<template v-else>
			<v-alert
				dense
				type="error"
				class="ma-0"
			>
				Atsiprašome, įvyko nenumatyta klaida...
			</v-alert>
		</template>
	</div>
</template>

<script>
	import AttributesTable from "./AttributesTable";
	import FeatureHistory from "./FeatureHistory";
	import FeaturePhotosCarousel from "./FeaturePhotosCarousel";
	import VerticalStreetSignSelector from "./VerticalStreetSignSelector";

	export default {
		props: {
			data: Object,
			editingActive: Boolean
		},

		components: {
			AttributesTable,
			FeatureHistory,
			FeaturePhotosCarousel,
			VerticalStreetSignSelector
		},

		methods: {
			getFormData: function(){
				var formData = null;
				if (this.data.featureType == "verticalStreetSignsSupports") {
					if (this.$refs.verticalStreetSignsSupportAttributesTable) {
						formData = this.$refs.verticalStreetSignsSupportAttributesTable.getFormData();
					}
				} else if (this.data.featureType == "verticalStreetSigns") {
					if (this.$refs.verticalStreetSignAttributesTable) {
						formData = this.$refs.verticalStreetSignAttributesTable.getFormData();
					}
				}
				return formData;
			}
		}
	};
</script>