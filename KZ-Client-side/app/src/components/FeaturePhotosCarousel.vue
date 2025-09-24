<template>
	<div>
		<MyHeading
			:value="title"
			v-if="title"
		/>
		<div class="mt-1 body-2">
			<div class="pa-1 bordered-container">
				<template v-if="photos">
					<template v-if="photos == 'error'">
						<v-alert
							dense
							type="error"
							class="ma-0"
						>
							Atsiprašome, įvyko nenumatyta klaida...
						</v-alert>
					</template>
					<template v-else-if="photos.length">
						<v-carousel
							:show-arrows="photos.length > 1"
							:hide-delimiters="photos.length < 2"
							v-model="activePhoto"
							:height="height"
						>
							<v-carousel-item
								v-for="(photo, i) in photos"
								:key="i"
								reverse-transition="fade-transition"
								transition="fade-transition"
							>
								<template v-if="asMaps">
									<InteractiveImage
										:photo="photo"
										:onInteractiveImageLoad="onInteractiveImageLoad"
										ref="interactiveImage"
									/>
								</template>
								<template v-else>
									<v-img
										:src="photo.src"
										:height="height"
										:contain="true"
										v-on:click="showGallery"
									>
										<template v-slot:placeholder>
											<v-row
												class="fill-height ma-0 full-height"
												align="center"
												justify="center"
											>
												<v-progress-circular
													indeterminate
													color="primary"
													:size="25"
													width="2"
												></v-progress-circular>
											</v-row>
										</template>
									</v-img>
								</template>
							</v-carousel-item>
						</v-carousel>
					</template>
					<template v-else>
						Nuotraukų nėra...
					</template>
				</template>
				<template v-else>
					<v-progress-circular
						indeterminate
						color="primary"
						:size="28"
						width="2"
					></v-progress-circular>
				</template>
			</div>
		</div>
	</div>
</template>

<script>
	import CommonHelper from "./helpers/CommonHelper";
	import MyHeading from "./MyHeading";
	import InteractiveImage from "./sc/InteractiveImage";

	export default {
		data: function(){
			var data = {
				photos: null,
				activePhoto: 0,
				height: this.compact ? 150 : 350
			};
			return data;
		},

		props: {
			feature: Object,
			featureObjectId: String,
			asMaps: Boolean,
			featureType: String,
			historicMoment: String,
			title: String,
			compact: Boolean
		},

		created: function(){
			this.$vBus.$on("feature-photos-modified", this.getPhotos); // Jei ką patvarkėme dialog'e "Objekto nuotraukų valdymas", tai viskas iškart turi atsispindėti popup'e...
		},

		beforeDestroy: function(){
			this.$vBus.$off("feature-photos-modified", this.getPhotos);
		},

		components: {
			MyHeading,
			InteractiveImage
		},

		methods: {
			getPhotos: function(){
				this.photos = null;
				// FIXME... Čia reiktų protingesnio sprendimo... Tais atvejais, kai identifikuojame 'general-object'... Konkrečiau "eismo įvykių statistika"... Nes ne visi serviso sluoksniukai turi enable'intus attachment'us... Reiktų pagal serviso->sluoksnio `capabilities` atsekti... Ar išvis nuotraukos galimos ar ne. Jei ne — tyliai nieko nerodyti...
				CommonHelper.getPhotos({
					feature: this.feature,
					featureObjectId: this.featureObjectId,
					featureType: this.featureType,
					historicMoment: this.historicMoment,
					store: this.$store
				}).then(function(photos){
					this.photos = photos;
				}.bind(this), function(){
					this.photos = "error";
				}.bind(this));
			},
			showGallery: function(){
				var photo = this.photos[this.activePhoto];
				if (photo) {
					window.open(photo.src, "_blank");
				}
			},
			onInteractiveImageLoad: function(){
				if (this.$refs.interactiveImage) {
					var interactiveImage = this.$refs.interactiveImage[this.activePhoto];
					if (interactiveImage) {
						// TODO... Kažką galima su juo daryti...
					}
				}
			}
		},

		watch: {
			feature: {
				immediate: true,
				handler: function(feature){
					if (feature) {
						this.getPhotos();
					}
				}
			},
			featureObjectId: {
				immediate: true,
				handler: function(featureObjectId){
					if (featureObjectId) {
						this.getPhotos();
					}
				}
			}
		}
	}
</script>

<style scoped>
	.v-image {
		cursor: pointer;
	}
</style>