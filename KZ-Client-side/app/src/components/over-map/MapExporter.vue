<template>
	<div>
		<OverMapButton
			:title="title"
			icon="mdi-printer"
			:clickCallback="onClick"
			ref="btn"
		/>
		<a id="image-download" download="map.png"></a>
	</div>
</template>

<script>
	import OverMapButton from "./OverMapButton";

	export default {
		data: function(){
			var data = {
				title: "Eksportuoti žemėlapį"
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
					if (navigator.msSaveBlob) {
						navigator.msSaveBlob(mapCanvas.msToBlob(), "map.png");
					} else {
						var link = document.getElementById("image-download");
						link.href = mapCanvas.toDataURL();
						link.click();
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