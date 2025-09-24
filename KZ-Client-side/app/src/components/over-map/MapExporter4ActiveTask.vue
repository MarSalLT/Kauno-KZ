<template>
	<div>
		<OverMapButton
			:title="title"
			icon="mdi-camera"
			color="success"
			:clickCallback="onClick"
			ref="btn"
			v-if="activeTask"
		/>
	</div>
</template>

<script>
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "Eksportuoti žemėlapį kaip piešinuką aktyviai užduočiai"
			};
			return data;
		},

		computed: {
			myMap: {
				get: function(){
					return this.$store.state.myMap;
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

		components: {
			OverMapButton
		},

		methods: {
			onClick: function(){
				// Vadovautasi: https://openlayers.org/en/latest/examples/export-map.html
				var map = this.myMap.map,
					mapCanvas = document.createElement("canvas"),
					size = map.getSize();
				mapCanvas.width = size[0];
				mapCanvas.height = size[1];
				var mapContext = mapCanvas.getContext("2d");
				Array.prototype.forEach.call(
					document.querySelectorAll(".map-cont .ol-layer canvas"),
					function(canvas){
						if (canvas.width > 0) {
							var opacity = canvas.parentNode.style.opacity;
							mapContext.globalAlpha = opacity === "" ? 1 : Number(opacity);
							var transform = canvas.style.transform;
							var matrix = transform
							.match(/^matrix\(([^(]*)\)$/)[1]
							.split(",")
							.map(Number);
							CanvasRenderingContext2D.prototype.setTransform.apply(
								mapContext,
								matrix
							);
							mapContext.drawImage(canvas, 0, 0);
						}
					}
				);
				try {
					var dataUrl;
					if (navigator.msSaveBlob) {
						dataUrl = mapCanvas.msToBlob();
					} else {
						dataUrl = mapCanvas.toDataURL();
					}
					if (dataUrl) {
						this.$vBus.$emit("task-attachment-new", {
							src: dataUrl,
							task: this.activeTask,
							tempAttachment: true
						});
					}
				} catch(err) {
					this.$vBus.$emit("show-message", {
						type: "warning",
						message: "Deja, atsirado klaida eksportuojant vaizdą..." + " " + err
					});
				}
			}
		}
	}
</script>