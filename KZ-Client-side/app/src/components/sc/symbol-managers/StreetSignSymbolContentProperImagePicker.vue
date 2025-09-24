<template>
	<div>
		<template v-if="images && images.length">
			<div class="mb-3">Simbolio pasirinkimas:</div>
			<v-radio-group
				v-model="selectedImage"
				class="ma-0 pa-0"
				hide-details
				row
			>
				<template v-for="(image, i) in images">
					<v-radio
						:label="image.id"
						:value="image.id"
						:key="i"
						:class="['ma-0 pa-0 mr-4']"
					>
						<template v-slot:label>
							<img
								:src="image.src"
								:style="{width: dimensions.width, height: dimensions.height}"
								:class="image.flip ? 'flip' : null"
							/>
						</template>
					</v-radio>
				</template>
			</v-radio-group>
			<div class="mt-7 mb-3" v-if="selectedImage">Sugeneruotas simbolio piešinukas:</div>
			<canvas ref="canvas" :class="[selectedImage ? 'd-block' : 'd-none']"></canvas>
		</template>
	</div>
</template>

<script>
	export default {
		data: function(){
			var images = [],
				dimensions = {};
			if (this.data.type == "413") {
				images.push({
					id: "413-1",
					src: require("@/assets/custom-sc-images/413-1-resized.png")
				},{
					id: "413-2",
					src: require("@/assets/custom-sc-images/413-2-resized.png")
				},{
					id: "413-3",
					src: require("@/assets/custom-sc-images/413-3-resized.png")
				});
				dimensions.width = "70px";
				dimensions.height = "70px";
			} else if (["830", "831", "832", "833", "834", "835", "836", "837", "838"].indexOf(this.data.type) != -1) {
				images.push({
					id: this.data.type + "-1",
					src: require("@/assets/custom-sc-images/" + this.data.type + "-resized.png"),
				},{
					id: this.data.type + "-2",
					src: require("@/assets/custom-sc-images/" + this.data.type + "-resized.png"),
					flip: true
				});
				dimensions.width = "70px";
				dimensions.height = "auto";
			} else if (this.data.type == "611") {
				images.push({
					id: "611-1",
					src: require("@/assets/custom-sc-images/611-1-resized.png")
				},{
					id: "611-2",
					src: require("@/assets/custom-sc-images/611-2-resized.png")
				},{
					id: "611-3",
					src: require("@/assets/custom-sc-images/611-3-resized.png")
				});
				dimensions.width = "70px";
				dimensions.height = "70px";
			}
			var selectedImage;
			if (images[0]) {
				selectedImage = images[0].id;
			}
			var data = {
				images: images,
				selectedImage: selectedImage,
				dimensions: dimensions,
				canvasWidth: 70
			};
			if (this.data.mode == "edit") {
				if (this.data.data) {
					var initialParams = JSON.parse(this.data.data);
					if (initialParams) {
						if (initialParams.selected) {
							data.selectedImage = initialParams.selected;
						}
					}
				}
			}
			return data;
		},

		props: {
			data: Object
		},

		mounted: function(){
			this.renderCanvas();
		},

		methods: {
			renderCanvas: function(){
				var canvas = this.$refs.canvas;
				if (canvas) {
					var image = new Image();
					image.crossOrigin = "Anonymous";
					image.onload = function(){
						this.renderCanvasWithImage();
					}.bind(this);
					if (this.images) {
						this.images.some(function(img){
							if (img.id == this.selectedImage) {
								image.src = img.src;
								image.flip = img.flip;
								this.image = image;
							}
						}.bind(this));
					}
				}
			},
			renderCanvasWithImage: function(){
				if (this.image.width && this.image.height) {
					var canvas = this.$refs.canvas,
						ctx = canvas.getContext("2d"),
						canvasWidth = this.canvasWidth;
					canvas.width = canvasWidth;
					canvas.height = (this.image.height / this.image.width) * canvasWidth;
					if (this.data.type == "611") {
						ctx.fillStyle = "white";
						ctx.fillRect(0, 0, canvas.width, canvas.height);
					}
					if (this.image.flip) {
						ctx.scale(-1, 1);
					}
					ctx.drawImage(this.image, this.image.flip ? (- canvas.width) : 0, 0, canvas.width, canvas.height);
				}
			},
			getDataUrl: function(){
				var canvas = this.$refs.canvas;
				if (canvas) {
					return canvas.toDataURL();
				}
			},
			getAllData: function(){
				var data = {
					dataURL: this.getDataUrl(),
					selected: this.selectedImage
				};
				return data;
			}
		},

		watch: {
			selectedImage: {
				immediate: true,
				handler: function(){
					this.renderCanvas();
				}
			}
		}
	};
</script>

<style scoped>
	.flip {
		-webkit-transform: scaleX(-1);
		transform: scaleX(-1);
	}
</style>