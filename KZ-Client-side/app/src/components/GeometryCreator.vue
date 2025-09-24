<template>
	<div class="d-flex align-center geometry-creator">
		<template v-if="zones">
			<template v-if="zones == 'error'">
				<v-icon title="Atsiprašome, ženklų priežiūros teritorijų sąrašas šiuo metu nepasiekiamas..." color="error">mdi-alert</v-icon>
			</template>
			<template v-else>
				<v-btn
					small
					title="Brėžti plotą žemėlapyje"
					class="mr-1 d-none"
				>
					<v-icon>mdi-vector-polygon</v-icon>
				</v-btn>
				<SelectField
					v-model="inputVal"
					:id="id"
					:items="zones"
					:editable="true"
				/>
			</template>
		</template>
		<template v-else>
			<v-progress-circular
				indeterminate
				color="primary"
				:size="25"
				width="2"
			></v-progress-circular>
		</template>
	</div>
</template>

<script>
	import SelectField from "./fields/SelectField";

	export default {
		data: function(){
			var data = {
				zones: null
			};
			return data;
		},

		props: {
			value: String,
			id: String,
			additionalTopItem: Object,
			useNameAsCode: Boolean
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
				}
			},
			inputVal: {
				get: function(){
					return this.value;
				},
				set: function(val){
					this.$emit("input", val);
				}
			}
		},

		components: {
			SelectField
		},

		mounted: function(){
			this.myMap.getZones().then(function(features){
				var zones = [],
					code;
				if (this.additionalTopItem) {
					zones.push(this.additionalTopItem);
				}
				features.forEach(function(feature){
					if (this.useNameAsCode) {
						code = feature.attributes["SEN_PAV"];
					} else {
						code = feature.attributes["OBJECTID"] + "";
					}
					zones.push({
						code: code,
						name: "Priežiūros teritorija: " + feature.attributes["SEN_PAV"]
					});
				}.bind(this));
				this.zones = zones;
			}.bind(this), function(){
				this.zones = "error";
			}.bind(this));
		},

		methods: {
			// ...
		},

		watch: {
			inputVal: {
				immediate: true,
				handler: function(zone){
					if (zone) {
						console.log("DRAW GEOMETRY...");
					} else {
						console.log("REMOVE GEOMETRY...");
					}
				}
			}
		}
	}
</script>