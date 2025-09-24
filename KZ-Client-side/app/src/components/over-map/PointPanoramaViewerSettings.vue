<template>
	<OverMapButtonContent
		type="point-panorama-viewer"
		:btn="btn"
		ref="wrapper"
		:absolute="true"
	>
		<template v-slot>
			<div class="body-2 ma-1">
				<div class="d-flex align-center">
					<label for-id="point-panorama-viewer-settings">Panoraminio vaizdo šaltinis:</label>
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
					v-model="source"
					dense
					id="point-panorama-viewer-settings"
					hide-details
					class="ma-0 pa-0"
				>
					<template v-for="(mode, i) in modes">
						<v-radio
							:key="i"
							:label="mode.label"
							:value="mode.value"
							class="ma-0"
						></v-radio>
					</template>
				</v-radio-group>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import OverMapButtonContent from "./OverMapButtonContent";

	export default {
		data: function(){
			var modes = [{
				label: "Mapillary",
				value: "mapillary"
			}];
			var data = {
				btn: null,
				source: "mapillary",
				modes: modes
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
			this.$vBus.$on("show-or-hide-panorama-viewer-settings", this.showOrHide);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-or-hide-panorama-viewer-settings", this.showOrHide);
		},

		methods: {
			showOrHide: function(e){
				if (e.state) {
					this.btn = e.btn;
					this.$refs.wrapper.offsetLeft = e.btn.$el.offsetLeft;
					this.$refs.wrapper.open = true;
					e.btn.source = this.source;
				} else {
					this.$refs.wrapper.open = false;
				}
			},
			close: function(){
				this.$refs.wrapper.open = false;
			}
		},

		watch: {
			source: {
				immediate: true,
				handler: function(source){
					if (this.btn) {
						this.btn.source = source;
					}
				}
			}
		}
	}
</script>