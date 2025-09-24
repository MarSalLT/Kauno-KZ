<template>
	<div>
		<div class="mb-2">Į rangovo IS bus perduoti šie duomenys:</div>
		<div class="d-flex flex-column">
			<div class="flex-grow-1">
				<AttributesTable
					:data="formData"
					class="mt-n1"
				/>
			</div>
			<div v-if="data.attachments && data.attachments.length" class="mt-3">
				<PhotosRow
					:photos="data.attachments"
				/>
			</div>
		</div>
	</div>
</template>

<script>
	import AttributesTable from "./AttributesTable";
	import Feature from "ol/Feature";
	import PhotosRow from "./feature-photos-manager/PhotosRow";

	export default {
		data: function(){
			var feature = new Feature();
			feature.setProperties(this.data.attributes);
			var data = {
				formData: {
					featureType: "tasks",
					feature: feature
				}
			};
			return data;
		},

		props: {
			data: Object
		},

		components: {
			AttributesTable,
			PhotosRow
		}
	};
</script>