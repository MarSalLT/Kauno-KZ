<template>
	<OverMapButtonContent
		type="painted-area-summary"
		:btn="btn"
		ref="wrapper"
		:absolute="true"
	>
		<template v-slot>
			<div class="body-2 ma-2">
				<div class="d-flex align-center">
					<label for-id="painted-area-summary" class="flex-grow-1">Objektų pasirinkimo tipas:</label>
					<v-btn
						icon
						v-on:click="close"
						class="ml-8"
						small
					>
						<v-icon
							title="Uždaryti"
						>
							mdi-close
						</v-icon>
					</v-btn>
				</div>
				<v-radio-group
					v-model="type"
					dense
					id="painted-area-summary"
					hide-details
					class="ma-0 pa-0 mr-7"
				>
					<v-radio
						label="Aprėpties braižymas"
						value="extent"
						class="ma-0 mb-1"
					></v-radio>
				</v-radio-group>
				<div class="mt-2 ml-1">
					<v-btn
						text
						outlined
						small
						color="primary"
						v-on:click="executeAnalysis"
						:disabled="!Boolean(boundsFeature)"
						:loading="loading"
					>
						Vykdyti analizę
					</v-btn>
				</div>
				<div v-if="totalPaintedArea" class="ml-1">
					<v-divider class="mt-5 mb-4"></v-divider>
					<strong>Uždažytas plotas:</strong> <span v-html="totalPaintedArea"></span>
				</div>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import OverMapButtonContent from "./OverMapButtonContent";

	export default {
		data: function(){
			var data = {
				btn: null,
				type: "extent",
				boundsFeature: null,
				loading: false,
				totalPaintedArea: null
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
			OverMapButtonContent
		},

		created: function(){
			this.$vBus.$on("show-or-hide-painted-area-summary-settings", this.showOrHide);
			this.$vBus.$on("on-painted-area-summary-bounds", this.onPaintedAreaSummaryBounds);
			this.$vBus.$on("on-painted-area-summary-analysis-results", this.onPaintedAreaSummaryAnalysisResults);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-or-hide-painted-area-summary-settings", this.showOrHide);
			this.$vBus.$off("on-painted-area-summary-bounds", this.onPaintedAreaSummaryBounds);
			this.$vBus.$off("on-painted-area-summary-analysis-results", this.onPaintedAreaSummaryAnalysisResults);
		},

		methods: {
			showOrHide: function(e){
				this.boundsFeature = null;
				if (e.state) {
					this.btn = e.btn;
					this.btnComponent = e.btnComponent;
					this.$refs.wrapper.offsetLeft = e.btn.$el.offsetLeft;
					this.$refs.wrapper.open = true;
					if (e.btnComponent) {
						e.btnComponent.type = this.type;
					}
					this.totalPaintedArea = null;
				} else {
					this.$refs.wrapper.open = false;
					if (e.btnComponent) {
						e.btnComponent.type = null;
					}
				}
			},
			close: function(){
				this.$refs.wrapper.open = false;
			},
			executeAnalysis: function(){
				if (this.btnComponent) {
					this.loading = true;
					this.totalPaintedArea = null;
					this.btnComponent.executeAnalysis();
				}
			},
			onPaintedAreaSummaryBounds: function(e){
				this.boundsFeature = e.feature;
			},
			onPaintedAreaSummaryAnalysisResults: function(results){
				if (results) {
					var myMap = this.$store.state.myMap;
					var totalPaintedArea = 0;
					results.forEach(function(result){
						var paintedArea = CommonHelper.getPaintedArea(result, myMap, true);
						if (paintedArea) {
							totalPaintedArea += paintedArea;
						} else {
							console.log("NO V", paintedArea, result); // FIXME!..
						}
					});
					this.totalPaintedArea = totalPaintedArea.toFixed(2) + " m<sup>2</sup>";
				}
				this.loading = false;
			}
		},

		watch: {
			type: {
				immediate: true,
				handler: function(type){
					if (this.btnComponent) {
						this.btnComponent.type = type;
					}
				}
			}
		}
	}
</script>