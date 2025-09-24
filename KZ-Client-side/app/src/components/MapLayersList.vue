<template>
	<div class="pa-3" v-if="myMap">
		<div v-if="optionalServices">
			<div class="font-weight-medium mb-1">Papildomi sluoksniai:</div>
			<draggable
				v-model="optionalServices"
				group="optionalServices"
				@start="drag = true"
				@end="drag = false"
				v-bind="dragOptions"
				v-on:update="reorderOptionalServices"
			>
				<transition-group
					type="transition"
					name="!drag ? 'flip-list' : null"
				>
					<OptionalLayerCheckbox
						v-for="service in optionalServices"
						:key="service.uniqueKey"
						:service="service"
						:addLayer="addLayer"
						:removeLayer="removeLayer"
					/>
				</transition-group>
			</draggable>
		</div>
		<div v-if="baseServices" :class="[optionalServices ? 'mt-2' : null]">
			<div class="font-weight-medium mb-1">Foninis sluoksnis:</div>
			<v-radio-group
				class="ma-0"
				hide-details
				v-model="baseServiceSelected"
			>
				<v-radio
					v-for="(service, index) in baseServices"
					:key="index"
					:label="service.title"
					:value="index"
				></v-radio>
			</v-radio-group>
		</div>
	</div>
</template>

<script>
	import MapHelper from "./helpers/MapHelper";
	import OptionalLayerCheckbox from "./over-map/components/OptionalLayerCheckbox";
	import draggable from "vuedraggable";

	export default {
		data: function(){
			var userPermissions = [];
			if (this.$store.state.userData) {
				userPermissions = this.$store.state.userData.permissions;
			}
			var services = MapHelper.getMainServices(userPermissions),
				baseServices = [],
				optionalServices = [];
			if (services.base) {
				services.base.forEach(function(service){
					service = this.getService(service);
					service.base = true;
					service.callback = function(status){
						if (!status) {
							this.baseServiceSelected = null;
						}
					}.bind(this);
					baseServices.push(service);
				}.bind(this));
			}
			if (services.optional) {
				var uniqueServiceKey = 0;
				services.optional.forEach(function(service){
					service = this.getService(service);
					service.uniqueKey = uniqueServiceKey;
					uniqueServiceKey += 1;
					service.callback = function(status){
						if (status && (service.id == "street-signs" || service.id == "street-signs-vertical")) {
							console.log("Pasikrovė KŽ sluoksnis!");
						}
					}
					if (this.$store.state.userData && (this.$store.state.userData.role == "street-viewer") && service.notInBasicViewer) {
						// Skip'iname...
					} else {
						optionalServices.push(service);
					}
				}.bind(this));
				if (this.$store.state.userData) {
					if (this.$store.state.userData.permissions) {
						if (this.$store.state.userData.role == "street-viewer") {
							optionalServices.forEach(function(optionalService){
								optionalService.active = false;
								if (optionalService.id == "pano-2022") {
									optionalService.active = true;
								}
							});
						}
					}
				}
			}
			var data = {
				baseServices: baseServices,
				optionalServices: optionalServices,
				baseServiceSelected: null,
				dragOptions: {
					animation: 200,
					group: "features",
					disabled: false,
					ghostClass: "ghost"
				},
				drag: false
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
			draggable,
			OptionalLayerCheckbox
		},

		methods: {
			addLayer: function(service){
				if (this.myMap && service) {
					this.myMap.addLayer(service);
				}
			},
			removeLayer: function(service){
				if (this.myMap && service) {
					this.myMap.removeLayer(service);
				}
			},
			getService: function(service){
				service = Object.assign({}, service);
				if (!service.url) {
					service.url = this.$store.getters.getServiceUrl(service.id);
				}
				return service;
			},
			reorderOptionalServices: function(){
				var length = this.optionalServices.length;
				this.optionalServices.forEach(function(service, i){
					var newZIndex = length + MapHelper.optionalServicesOffset - i;
					service.zIndex = newZIndex;
					if (service.layer) {
						service.layer.setZIndex(newZIndex);
						if (service.layer.getLayers) {
							service.layer.getLayers().forEach(function(l){
								l.setZIndex(newZIndex); // To reikia, nes vaikiniai sluoksniai kaip ir nepaveldi LayerGroup'o zIndex'o?
								if (l.getLayers) {
									l.getLayers().forEach(function(lInner){
										lInner.setZIndex(newZIndex);
									}.bind(this));
								}
							}.bind(this));
						}
					}
				}.bind(this));
				this.$vBus.$emit("layers-reordered");
			}
		},

		watch: {
			myMap: {
				immediate: true,
				handler: function(myMap){
					if (myMap) {
						if (this.baseServices.length) {
							this.baseServiceSelected = 0;
						}
					}
				}
			},
			baseServiceSelected: {
				immediate: true,
				handler: function(baseServiceSelected){
					var service = this.baseServices[baseServiceSelected];
					if (service) {
						this.addLayer(service);
					}
				}
			}
		}
	};
</script>