<script>
	export default {
		getExif(photo){
			var promise = new Promise(function(resolve){
				this.getPhotoSrc(photo).then(function(src){
					var piexif = require("piexifjs"),
						result = {
							src: src
						};
					try {
						// https://piexifjs.readthedocs.io/en/latest/sample.html
						var exif = piexif.load(src),
							orientation = exif["0th"][piexif.ImageIFD.Orientation];
						result = {
							exif: exif,
							orientation: orientation,
							src: src
						};
					} catch(err) {
						console.warn(err);
					}
					resolve(result);
				});
			}.bind(this));
			return promise;
		},

		getPhotoSrc: function(photo){
			var promise = new Promise(function(resolve){
				if (photo.brandNew) {
					resolve(photo.src);
				} else {
					// https://stackoverflow.com/questions/934012/get-image-data-url-in-javascript
					var img = new Image();
					img.setAttribute("crossOrigin", "anonymous");
					img.onload = function(){
						var canvas = document.createElement("canvas"),
							ctx = canvas.getContext("2d");
						canvas.width = this.width;
						canvas.height = this.height;
						ctx.drawImage(this, 0, 0);
						var dataUrl = canvas.toDataURL("image/jpeg", 0.95);
						resolve(dataUrl);
					};
					img.src = photo.src;
				}
			});
			return promise;
		},

		getProperSrc(e, rotationCode){
			var src = e.src;
			var promise = new Promise(function(resolve){
				if (!e) {
					resolve(src);
				} else {
					// https://stackoverflow.com/questions/20600800/js-client-side-exif-orientation-rotate-and-mirror-jpeg-images
					var image = new Image();
					image.onload = function(){
						var ratio = 1,
							threshold = 1500,
							max = Math.max(image.width, image.height);
						if (max > threshold) {
							ratio = max / threshold;
						}
						var canvas = document.createElement("canvas"),
							ctx = canvas.getContext("2d"),
							srcOrientation = e.orientation,
							width = image.width / ratio,
							height = image.height / ratio;
						if (4 < srcOrientation && srcOrientation < 9) {
							canvas.width = height;
							canvas.height = width;
						} else {
							canvas.width = width;
							canvas.height = height;
						}
						if (rotationCode) {
							canvas.width = height;
							canvas.height = width;
							if (rotationCode == "cc") {
								ctx.rotate(- Math.PI / 2);
								ctx.translate(- canvas.height, 0);
							} else {
								ctx.rotate(Math.PI / 2);
								ctx.translate(0, - canvas.width);
							}
						}
						switch (srcOrientation) {
							case 2:
								ctx.transform(-1, 0, 0, 1, width, 0);
								break;
							case 3:
								ctx.transform(-1, 0, 0, -1, width, height);
								break;
							case 4:
								ctx.transform(1, 0, 0, -1, 0, height);
								break;
							case 5:
								ctx.transform(0, 1, 1, 0, 0, 0);
								break;
							case 6:
								ctx.transform(0, 1, -1, 0, height, 0);
								break;
							case 7:
								ctx.transform(0, -1, -1, 0, height, width);
								break;
							case 8:
								ctx.transform(0, -1, 1, 0, 0, width);
								break;
							default: break;
						}
						ctx.scale(1 / ratio, 1 / ratio);
						ctx.drawImage(image, 0, 0);
						try {
							src = canvas.toDataURL("image/jpeg", 0.95);
						} catch(err) {
							console.warn(err);
						}
						resolve(src);
					};
					image.src = src;
				}
			});
			return promise;
		},

		prependExif: function(src, exif){
			if (exif) {
				try {
					var piexif = require("piexifjs");
					exif["0th"][piexif.ImageIFD.Orientation] = 1; // Kadangi fiziškai apvertėme nuotrauką, nustatome atitinkamą įrašą EXIF faile...
					var exifStr = piexif.dump(exif);
					src = piexif.insert(exifStr, src);
				} catch(err) {
					console.warn(err);
				}
			}
			return src;
		},

		getFile: function(src, photo){
			var fileName = photo.name;
			if (photo.file) {
				fileName = photo.file.name;
			}
			var promise = new Promise(function(resolve, reject){
				// https://stackoverflow.com/questions/49925039/create-a-file-object-from-an-img-tag
				window.fetch(src).then(res => res.blob()).then(blob => {
					var file = new File([blob], fileName, blob);
					file.src = src;
					resolve(file);
				}, function(e){
					reject(e);
				});
			});
			return promise;
		}
	}
</script>