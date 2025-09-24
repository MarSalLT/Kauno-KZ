<template>
	<v-sheet
		elevation="1"
		:class="['component-wrapper d-flex flex-column', maximized ? 'maximized' : null, absolute ? 'absolute rounded': null, wide ? 'wide' : null]"
		:style="{marginLeft: offsetLeft + 'px', height: (maximized ? '100%' : (fixedHeight ? fixedHeight + 'px' : 'auto'))}"
		v-if="open"
	>
		<div class="pl-3 pr-2 py-2 d-flex align-center header" v-if="title">
			<span class="flex-grow-1 font-weight-bold mr-10">{{title}}</span>
			<slot name="buttons"></slot>
			<v-btn
				icon
				v-on:click="close"
				class="ml-2"
				small
			>
				<v-icon
					title="Uždaryti"
				>
					mdi-close
				</v-icon>
			</v-btn>
		</div>
		<div :class="['content flex-grow-1', noContentPadding ? null : (title ? 'pa-3 d-flex flex-column' : 'pa-1')]">
			<slot></slot>
		</div>
	</v-sheet>
</template>

<script>
	import Vue from "vue";

	export default {
		data: function(){
			var data = {
				open: false,
				offsetLeft: 0,
				maximized: false
			};
			return data;
		},

		props: {
			type: String,
			title: String,
			btn: Object,
			onOpen: Function,
			onClose: Function,
			absolute: Boolean,
			noContentPadding: Boolean,
			fixedHeight: Number,
			wide: Boolean
		},

		created: function(){
			this.$vBus.$on("close-over-map-button-contents", this.forceClose);
			this.$vBus.$on("close-specific-over-map-button-contents", this.forceCloseSpecific);
		},

		beforeDestroy: function(){
			this.$vBus.$off("close-over-map-button-contents", this.forceClose);
			this.$vBus.$off("close-specific-over-map-button-contents", this.forceCloseSpecific);
		},

		methods: {
			close: function(){
				this.open = false;
			},
			forceClose: function(type){
				if (this.type) {
					if (type != this.type) {
						this.close();
					}
				}
			},
			forceCloseSpecific: function(type){
				if (type == this.type) {
					this.close();
				}
			},
			toggle: function(offsetLeft){
				if (!this.open) {
					this.offsetLeft = offsetLeft;
				}
				this.open = !this.open;
			}
		},

		watch: {
			open: {
				immediate: true,
				handler: function(open){
					if (open) {
						this.$vBus.$emit("close-over-map-button-contents", this.type);
						if (this.onOpen) {
							this.onOpen();
						}
					} else {
						if (this.onClose) {
							this.onClose();
						}
					}
					if (this.btn) {
						Vue.set(this.btn, "active", open);
					}
				}
			}
		}
	}
</script>

<style scoped>
	.component-wrapper {
		max-width: 500px;
		overflow: auto;
		max-height: 100%;
	}
	.maximized {
		max-width: none !important;
		width: 100%;
	}
	.header {
		border-bottom: 1px solid #eeeeee;
	}
	.content {
		overflow: auto;
	}
	.absolute {
		position: absolute;
		top: 0;
	}
	.wide {
		max-width: 520px;
	}
</style>