<template>
	<OverMapButtonContent
		type="file-upload"
		:title="title"
		:btn="btn"
		:onOpen="onOpen"
		ref="wrapper"
	>
		<template v-slot>
			<div class="body-2">
				<v-card
					width="300"
					height="225"
					class="image-card drop-zone"
					tile
					outlined
					tabindex="0"
				>
					<FileDropZone
						:addPhotos="addFiles"
						accept=".dwg"
					/>
				</v-card>
				<template v-if="file">
					<div class="mt-2">
						<strong>Pasirinktas failas:</strong> {{file.name}}
					</div>
					<div class="mt-1">
						<v-btn
							small
							color="primary lighten-1"
							v-on:click="processFile"
						>
							Įkelti failą
						</v-btn>
					</div>
				</template>
				<div v-if="files.length" class="mt-4 files-wrapper pt-2">
					<strong>Įkelti failai:</strong>
					<ul class="mt-1">
						<template v-for="(file, i) in files">
							<li :key="i">
								<FileUploadFileInfo
									:file="file"
									:onRemove="removeFile"
									:zoomToLayer="zoomToLayer"
								/>
							</li>
						</template>
					</ul>
				</div>
			</div>
		</template>
	</OverMapButtonContent>
</template>

<script>
	import CommonHelper from "../helpers/CommonHelper";
	import EsriJSON from "ol/format/EsriJSON";
	import FileDropZone from "../feature-photos-manager/FileDropZone";
	import FileUploadFileInfo from "./components/FileUploadFileInfo";
	import OverMapButtonContent from "./OverMapButtonContent";
	import VectorLayer from "ol/layer/Vector";
	import VectorSource from "ol/source/Vector";
	import Vue from "vue";

	export default {
		data: function(){
			var data = {
				title: null,
				btn: null,
				file: null,
				files: [],
				gpRoot: CommonHelper.dwg2JsonGPUrl
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
			FileDropZone,
			FileUploadFileInfo,
			OverMapButtonContent
		},

		created: function(){
			this.$vBus.$on("show-or-hide-file-upload", this.showOrHide);
		},

		beforeDestroy: function(){
			this.$vBus.$off("show-or-hide-file-upload", this.showOrHide);
		},

		methods: {
			showOrHide: function(e){
				this.title = e.title;
				this.btn = e.btn;
				this.$refs.wrapper.toggle(this.btn.$el.offsetLeft);
			},
			onOpen: function(){
				// ...
			},
			addFiles: function(files){
				var file;
				if (files && files.length) {
					file = files[0];
				}
				this.file = file;
			},
			processFile: function(){
				if (this.file) {
					var file = {
						file: this.file
					};
					this.files.push(file);
					this.file = null;
					this.uploadFile(file.file).then(function(item){
						if (this.getFileIndex(file) != -1) { // Jei kartais nebuvo pašalintas...
							this.dwgToJson(item).then(function(jobId){
								if (this.getFileIndex(file) != -1) { // Jei kartais nebuvo pašalintas...
									var task = window.setInterval(function(){
										if (this.getFileIndex(file) != -1) { // Jei kartais nebuvo pašalintas...
											this.checkJob(jobId).then(function(response){
												if (response.jobStatus == "esriJobSucceeded" || response.jobStatus == "esriJobFailed" || response.jobStatus == "esriJobTimedOut") {
													window.clearInterval(task);
													if (this.getFileIndex(file) != -1) { // Jei kartais nebuvo pašalintas...
														if (response.jobStatus == "esriJobSucceeded" && response.results[CommonHelper.dwg2JsonGPOutputParamName]) {
															this.getFeatures(jobId, response.results[CommonHelper.dwg2JsonGPOutputParamName].paramUrl).then(function(result){
																Vue.set(file, "finished", true);
																if (this.getFileIndex(file) != -1) { // Jei kartais nebuvo pašalintas...
																	this.renderFeatures(result, file);
																}
															}.bind(this), function(){
																Vue.set(file, "finished", true);
																Vue.set(file, "error", true);
															}.bind(this));
														} else {
															Vue.set(file, "finished", true);
															Vue.set(file, "error", true);
														}
													}
												}
											}.bind(this), function(){
												// FIXME! Gal jei nepavyko vienąkart patikrinti job'o, tai nėra klaida?! Gal tokiu atveju reikia sukurti refresh'o mygtuką prie item'o?!
												// Paspaudi refresh'o mygtuką — toliau vykdosi checkJob'ai...
												// Arba sustabdyti task'ą tik jei iš eilės 3 kartus klaida...
												window.clearInterval(task);	
												Vue.set(file, "finished", true);
												Vue.set(file, "error", true);
											});
										} else {
											window.clearInterval(task);
										}
									}.bind(this), 2000);
								}
							}.bind(this), function(){
								Vue.set(file, "finished", true);
								Vue.set(file, "error", true);
							}.bind(this));
						}
					}.bind(this), function(){
						Vue.set(file, "finished", true);
						Vue.set(file, "error", true);
					}.bind(this));
				}
			},
			getFileIndex: function(file){
				var index = this.files.indexOf(file);
				return index;
			},
			removeFile: function(file){
				if (file.layer) {
					this.myMap.map.removeLayer(file.layer);
				}
				var index = this.getFileIndex(file);
				this.files.splice(index, 1);
			},
			uploadFile: function(file){
				file.key = "file"; // Būtinas!
				var url = this.gpRoot + "uploads/upload";
				var params = {
					f: "json"
				};
				var promise = new Promise(function(resolve, reject){
					CommonHelper.getFetchPromise(url, function(json){
						return json;
					}.bind(this), "POST", params, [file]).then(function(result){
						if (result && result.success && result.item) {
							resolve(result.item);
						} else {
							reject();
						}
					}, function(){
						reject();
					});
				}.bind(this));
				return promise;
			},
			dwgToJson: function(item){
				var url = this.gpRoot + CommonHelper.dwg2JsonGPToolName + "/submitJob";
				var params = {
					f: "json",
					dwg_file: JSON.stringify({
						itemID: item.itemID
					}),
					out_spatial_reference: "3346"
				};
				var promise = new Promise(function(resolve, reject){
					CommonHelper.getFetchPromise(url, function(json){
						return json;
					}.bind(this), "POST", params).then(function(result){
						if (result && result.jobId) {
							resolve(result.jobId);
						} else {
							reject();
						}
					}, function(){
						reject();
					});
				}.bind(this));
				return promise;
			},
			checkJob: function(jobId){
				var url = this.gpRoot + "jobs/" + jobId;
				var params = {
					f: "json"
				};
				var promise = new Promise(function(resolve, reject){
					CommonHelper.getFetchPromise(url, function(json){
						return json;
					}.bind(this), "POST", params).then(function(result){
						if (result) {
							resolve(result);
						} else {
							reject();
						}
					}, function(){
						reject();
					});
				}.bind(this));
				return promise;
			},
			getFeatures: function(jobId, paramUrl){
				var url = this.gpRoot + "jobs/" + jobId + "/" + paramUrl;
				var params = {
					f: "json"
				};
				var promise = new Promise(function(resolve, reject){
					CommonHelper.getFetchPromise(url, function(json){
						return json;
					}.bind(this), "POST", params).then(function(result){
						if (result && result.value) {
							if (result.value.error) {
								reject();
							} else {
								resolve(result.value);
							}
						} else {
							reject();
						}
					}, function(){
						reject();
					});
				}.bind(this));
				return promise;
			},
			renderFeatures: function(features, file){
				// Svarbu, kad nebūtų curvePaths, curveRings?..
				var esriJsonFormat = new EsriJSON();
				if (this.myMap) {
					var layer = new VectorLayer({
						source: new VectorSource(),
						zIndex: 1004,
					});
					this.myMap.map.addLayer(layer);
					features.forEach(function(f){
						f = esriJsonFormat.readFeatures(f);
						if (f.length > 0) {
							layer.getSource().addFeatures(f);
						}
					}.bind(this));
					Vue.set(file, "layer", layer);
					this.zoomToLayer(file);
				}
			},
			zoomToLayer: function(file){
				if (file.layer) {
					this.myMap.map.getView().fit(file.layer.getSource().getExtent());
				}
			}
		},

		watch: {
			files: {
				immediate: true,
				handler: function(files){
					if (this.myMap) {
						if (files && files.length) {
							this.myMap.setUploadedFiles(files);
						} else {
							this.myMap.setUploadedFiles();
						}
					}
				}
			}
		}
	}
</script>

<style scoped>
	.files-wrapper {
		border-top: 1px dashed #cccccc;
	}
</style>