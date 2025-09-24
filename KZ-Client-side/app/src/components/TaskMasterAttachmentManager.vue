<template>
	<div class="full-height">
		<div
			ref="map"
			class="map"
		>
		</div>
		<template v-if="loading">
			<v-progress-circular
				indeterminate
				color="primary"
				:size="40"
				width="2"
				class="ma-3 loading-indicator"
			></v-progress-circular>
		</template>
		<template v-else>
			<div ref="tooltip" class="pa-1 body-2 map-tooltip"></div>
			<TaskMasterAttachmentControls
				:map="map"
				:contentData="contentData"
				:tooltipRef="$refs.tooltip"
				:images="images"
				:task="task"
				class="interactive-image-controls stop-event"
				ref="taskMasterAttachmentControls"
				v-if="map && images && (images != 'error')"
			>
				<PhotosRow
					:photos.sync="images"
					:maxPhotosCount="20"
					:getKey="getKey"
					:onPhotoAdd="onPhotoAdd"
					:onClick="addImage"
					:small="true"
					:asColumn="true"
					:hideKeywords="true"
					hoverIcon="mdi-transfer-left"
					tooltip="Perkelti į brėžinį/schemą"
					v-if="images && (images != 'error')"
				/>
			</TaskMasterAttachmentControls>
		</template>
	</div>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";
	import TaskMasterAttachmentControls from "./task-master-attachment-manager-components/TaskMasterAttachmentControls";
	import Map from "ol/Map";
	import PhotosRow from "./feature-photos-manager/PhotosRow";
	import Projection from "ol/proj/Projection";
	import TaskMasterAttachmentMapHelper from "./helpers/TaskMasterAttachmentMapHelper";
	import View from "ol/View";
	import {getCenter} from "ol/extent";
	import "ol/ol.css";

	export default {
		data: function(){
			var data = {
				loading: true,
				map: null,
				contentData: null,
				images: [],
				extent: [0, 0, 10000, 10000],
				key: 0
			};
			return data;
		},

		props: {
			task: Object,
			attachment: Object,
			onMapLoad: Function
		},

		components: {
			TaskMasterAttachmentControls,
			PhotosRow
		},

		mounted: function(){
			var callback = function(){
				// this.loading = false; // Taip buvo aktualu seniau... Dabar `viskas pasikrovė` tik tada, kai gaunamos nuotraukos...
				this.createMap();
				this.loadImages();
			}.bind(this);
			if (this.attachment) {
				TaskMasterAttachmentMapHelper.getAttachmentContentData(this.attachment.globalId).then(function(contentData){
					if (contentData && contentData.data) {
						this.contentData = JSON.parse(contentData.data);
					}
					callback();
				}.bind(this), function(){
					console.warn("Taip neturėtų būti! Neturime attachment'o aprašomosios info...");
					callback();
				});
			} else {
				callback();
			}
		},

		methods: {
			createMap: function(){
				var extent = this.extent;
				var projection = new Projection({ // Tik bėda, kad koordinačių sistema prasideda nuo kairiojo kampo APAČIOS? O HTML Canvas'e nuo kairiojo kampo VIRŠAUS??..
					code: "xkcd-image",
					units: "pixels",
					extent: extent
				});
				var map = new Map({
					target: this.$refs.map,
					view: new View({
						projection: projection,
						center: getCenter(extent),
						zoom: 2,
						maxZoom: 8,
						constrainOnlyCenter: true,
						smoothExtentConstraint: true,
						extent: extent
					})
				});
				setTimeout(function(){
					map.updateSize();
					map.getView().fit(extent);
				}.bind(this), 0);
				map.once("postrender", function(){
					if (this.onMapLoad) {
						this.onMapLoad(map);
					}
				}.bind(this));
				this.map = map;
			},
			loadImages: function(){
				if (this.task && (this.task.objectId || this.task.feature)) {
					CommonHelper.getPhotos({
						featureObjectId: this.task.objectId,
						feature: this.task.feature,
						featureType: "tasks",
						justImages: true,
						store: this.$store
					}).then(function(images){
						images.forEach(function(image, i){
							image.key = i;
						});
						this.images = images;
						this.loading = false;
					}.bind(this), function(){
						this.images = "error"; // Čia blogai... Nes persiduos kaip `error` Controls'ams...
						this.loading = false;
					}.bind(this));
				} else {
					this.loading = false;
				}
			},
			addImage: function(e){
				if (this.$refs.taskMasterAttachmentControls) {
					this.$refs.taskMasterAttachmentControls.addImage(e.src, e.id, e.key);
				}
			},
			getKey: function(){
				this.key += 1;
				return 'custom-' + this.key;
			},
			onPhotoAdd: function(){
				// ...
			},
			getAllData: function(){
				var taskMasterAttachmentControls = this.$refs.taskMasterAttachmentControls;
				if (taskMasterAttachmentControls && taskMasterAttachmentControls.getAllData) {
					var data = taskMasterAttachmentControls.getAllData();
					return data;
				}
			}
		}
	};
</script>

<style scoped>
	.map {
		height: 100%;
		border: 2px dotted #cccccc;
	}
	.loading-indicator {
		position: absolute;
		top: 0;
	}
	.interactive-image-controls {
		position: absolute;
		top: 3px;
		right: 3px;
		bottom: 3px;
		overflow: auto;
	}
	.map-tooltip {
		position: absolute;
		z-index: 1;
		background-color: white;
		pointer-events: none;
		display: none;
		border: 1px solid #cccccc;
	}
</style>