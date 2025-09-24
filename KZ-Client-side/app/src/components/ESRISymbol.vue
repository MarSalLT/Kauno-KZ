<template>
	<div
		v-if="descrMod"
		:class="[
			dark ? 'dark' : null,
			small ? 'small' : 'pa-2',
			alignStart ? 'align-start' : null,
			descr.type ? descr.type : null
		]"
	>
		<template v-if="descrMod.src">
			<img
				:src="descrMod.src"
				:width="descrMod.width && descrMod.height ? descrMod.width : ''"
				:height="descrMod.width && descrMod.height ? descrMod.height : ''"
				:class="descr.type == 'sc-element' ? [descr.fromTop ? 'pa-2 pt-0' : 'pa-2 pb-0'] : ['my-image', descr.fromRight ? 'pr-0' : null]"
			>
		</template>
		<template v-else-if="descrMod.html">
			<span v-html="descrMod.html" class="d-flex"></span>
		</template>
		<template v-else-if="descrMod.icon">
			<v-icon>{{descrMod.icon}}</v-icon>
		</template>
		<span
			:class="['body-2 text-center d-block', descrMod.symbolExists ? 'mt-1' : null]"
			v-if="title"
		>
			{{title}}
		</span>
	</div>
</template>

<script>
	export default {
		props: {
			descr: Object,
			title: String,
			dark: Boolean,
			small: Boolean,
			alignStart: Boolean
		},

		computed: {
			descrMod: {
				get: function(){
					var descr;
					if (this.descr) {
						descr = {};
						var color;
						if (this.descr.altSrc) {
							descr.src = this.descr.altSrc;
							descr.width = this.descr.width;
							descr.height = this.descr.height;
							descr.symbolExists = true;
						} else {
							switch (this.descr.type) {
								case "esriPMS":
									descr.src = "data:" + this.descr.contentType + ";base64," + this.descr.imageData;
									// descr.width = this.descr.width; // Sprendimas specialiai Kaunui dėl serviso `bug`, kai gavosi per dideli imageData simboliai...
									// descr.height = this.descr.height; // Sprendimas specialiai Kaunui dėl serviso `bug`, kai gavosi per dideli imageData simboliai...
									descr.symbolExists = true;
									break;
								case "esriSLS":
									color = this.descr.color.join(",");
									descr.html = '<svg width="30" height="14" class="svg-symbol">' + 
                                                 '<line x1="0" y1="7" x2="30" y2="7" stroke="rgb(' + color + ')" stroke-width="' +  this.descr.width + '" ' + this.getStrokeDashArray(this.descr.style, this.descr.width) + ' />' +
                                                 '</svg>';
									descr.symbolExists = true;
									break;
								case "esriSFS":
									color = this.descr.color.join(",");
									descr.html = '<svg width="30" height="24" class="svg-symbol"><polygon points="0,0 30,10, 30,24 0,24" fill="rgb(' + color + ')" /></svg>';
									descr.symbolExists = true;
									break;
								case "esriSMS":
									color = this.descr.color.join(",");
									var strokeColor,
										strokeWidth,
										radius = 9,
										size = 21;
									if (this.descr.outline) {
										strokeColor = this.descr.outline.color.join(",");
										strokeWidth = 1;
									}
									var centerCoordinates = {
										x: 10,
										y: 10
									};
									descr.html = '<svg width="' + size + '" height="' + size + '"><circle cx="' + centerCoordinates.x + '" cy="' + centerCoordinates.y + '" r="' + radius + '" stroke="rgb(' + strokeColor + ')" stroke-width="' + strokeWidth + '" fill="rgb(' + color + ')" /></svg>';
									descr.symbolExists = true;
									break;
								case "text":
									descr.html = this.descr.html;
									break;
								case "color":
									descr.html = '<span class="color" style="width:20px;height:20px;background-color:' + this.descr.value + ';"></span>';
									break;
								case "icon":
									descr.icon = this.descr.value;
									break;
							}
						}
					}
					return descr;
				}
			}
		},

		methods: {
			getStrokeDashArray: function(style, width){
				var strokeDashArray = "";
				if (style == "esriSLSDash") {
					strokeDashArray = 'stroke-dasharray="' + width + '"';
				}
				return strokeDashArray;
			}
		}
	};
</script>

<style scoped>
	.my-image {
		vertical-align: middle;
		display: block;
		margin: 0 auto;
		max-width: 60px;
		height: auto;
	}
	.dark img {
		background-color: #111111;
	}
	.small img {
		max-height: 30px;
		max-width: none;
	}
	.align-start img {
		margin: 0;
	}
	.sc img {
		max-width: none;
		height: 38px;
		width: auto;
	}
	.sc-element img {
		background-color: #444444;
		box-sizing: content-box;
		float: left;
	}
	.sc-element-custom img {
		background-color: #444444;
		box-sizing: content-box;
		float: left;
		padding: 4px;
	}
</style>