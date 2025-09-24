<template>
	<div
		ref="map"
		:class="['map', dark ? 'dark' : null]"
	>
		<v-progress-circular
			indeterminate
			color="primary"
			:size="30"
			width="2"
			v-if="loading"
			class="ma-2"
		></v-progress-circular>
	</div>
</template>

<script>
	import ImageLayer from "ol/layer/Image";
	import Map from "ol/Map";
	import Projection from "ol/proj/Projection";
	import Static from "ol/source/ImageStatic";
	import View from "ol/View";
	import {getCenter} from "ol/extent";
	import "ol/ol.css";

	export default {
		data: function(){
			var data = {
				loading: true,
				map: null
			};
			return data;
		},

		props: {
			photo: Object,
			onInteractiveImageLoad: Function,
			dark: Boolean
		},

		mounted: function(){
			if (this.photo) {
				var img = new Image(),
					that = this;
				img.onload = function(){
					var photoSize = {
						width: this.width,
						height: this.height
					};
					that.photoSize = photoSize;
					that.createMap(photoSize);
				}
				img.src = this.photo.src;
				this.img = img;
			}
		},

		methods: {
			createMap: function(photoSize){
				var extent = [0, 0, photoSize.width, photoSize.height];
				var projection = new Projection({
					code: "xkcd-image",
					units: "pixels",
					extent: extent
				});
				var imageLayer = new ImageLayer({
					source: new Static({
						url: this.photo.src,
						projection: projection,
						imageExtent: extent
					})
				});
				var map = new Map({
					layers: [
						imageLayer
					],
					target: this.$refs.map,
					view: new View({
						projection: projection,
						center: getCenter(extent),
						zoom: 2,
						maxZoom: 8
					}),
				});
				setTimeout(function(){
					map.updateSize();
					map.getView().fit(extent);
				}.bind(this), 0);
				map.once("postrender", function(){
					this.loading = false;
					if (this.onInteractiveImageLoad) {
						this.onInteractiveImageLoad(map);
					}
				}.bind(this));
				this.map = map;
				this.imageLayer = imageLayer;
			}
		}
	};
</script>

<style scoped>
	.map {
		height: 100%;
		background-repeat: repeat;
	}
	.dark {
		background-color: #f4f4f4;
		border: 2px dotted #cccccc;
	}
	.v-progress-circular {
		position: absolute;
	}
</style>