<script>
	import CommonHelper from "../helpers/CommonHelper";

	export default {
		tryExecutingPhotosTask: function(objectId, featureType, photos, photosCase, photosTaskCallback, vBus){
			var callback = function(){
				photosTaskCallback("start");
				var tasks = [];
				photos.forEach(function(photo){
					if (photo.brandNew) {
						tasks.push({
							action: "add",
							photo: photo
						});
					} else {
						if (photo.rotated) {
							tasks.push({
								action: "replace",
								photo: photo
							});
						}
					}
				});
				var photosToRemoveIds = [];
				photosCase.photosToRemove.forEach(function(photo){
					photosToRemoveIds.push(photo.id);
				});
				if (photosToRemoveIds.length) {
					tasks.push({
						action: "remove",
						attachmentIds: photosToRemoveIds
					});
				}
				this.executePhotoTask(objectId, featureType, tasks, 0, [], photosTaskCallback, []);
			}.bind(this);
			if (photosCase.photosToRemove.length) {
				var photoMessage = photosCase.photosToRemove.length == 1 ? "nuotraukos" : "nuotraukų",
					message = "Nuotraukų išsaugojimo metu bus negrįžtamai " + (photosCase.photosToRemove.length == 1 ? "pašalinta viena nuotrauka" : "pašalintos " + photosCase.photosToRemove.length + " nuotraukos");
				if (featureType == "tasks") {
					photoMessage = photosCase.photosToRemove.length == 1 ? "failo" : "failų";
					message = "Failų išsaugojimo metu bus negrįžtamai " + (photosCase.photosToRemove.length == 1 ? "pašalintas vienas failas" : "pašalinti " + photosCase.photosToRemove.length + " failai");
				}
				message += ". Ar tvirtinate " + photoMessage + " šalinimą?";
				vBus.$emit("confirm", {
					title: "Ar tvirtinate " + photoMessage + " šalinimą?",
					message: message,
					positiveActionTitle: "Tvirtinti " + photoMessage + " šalinimą",
					negativeActionTitle: "Atšaukti",
					positive: function(){
						callback();
					}.bind(this)
				});
			} else {
				callback();
			}
		},

		executePhotoTask: function(objectId, featureType, tasks, i, errors, photosTaskCallback, res){
			var task = tasks[i],
				callback = function(success){
					if (!success) {
						errors.push(i);
					}
					this.executePhotoTask(objectId, featureType, tasks, i + 1, errors, photosTaskCallback, res); // Vykdome kitą... Esmė ta, kad vykdome po vieną task'ą...
				}.bind(this);
			if (task) {
				if (task.action == "add") {
					this.addPhoto(objectId, featureType, task.photo).then(function(r){
						if (res) {
							res.push({
								task: task,
								res: r
							});
						}
						callback(true);
					}, function(){
						callback(false);
					});
				} else if (task.action == "remove") {
					this.removePhoto(objectId, featureType, task.attachmentIds).then(function(){
						callback(true);
					}, function(){
						callback(false);
					});
				} else if (task.action == "replace") {
					this.replacePhoto(objectId, featureType, task.photo, task.photo.keywords).then(function(){
						callback(true);
					}, function(){
						callback(false);
					});
				}
			} else {
				photosTaskCallback("end", errors, res);
			}
		},

		addPhoto: function(objectId, featureType, photo, keywords){
			// https://developers.arcgis.com/rest/services-reference/add-attachment.htm
			var params = {
				objectId: objectId,
				featureType: featureType
			};
			if (keywords) {
				params.keywords = keywords;
			}
			var files = [photo.file];
			var promise = new Promise(function(resolve, reject){
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "feature/add-photo", function(json){
					return json;
				}, "POST", params, files).then(function(result){
					if (result && !result.error) {
						resolve(result);
					} else {
						reject();
					}
				}.bind(this), function(){
					reject();
				}.bind(this));
			}.bind(this));
			return promise;
		},

		removePhoto: function(objectId, featureType, attachmentIds){
			// https://developers.arcgis.com/rest/services-reference/delete-attachments.htm
			var params = {
				objectId: objectId,
				featureType: featureType,
				attachmentIds: attachmentIds.join(",")
			};
			var promise = new Promise(function(resolve, reject){
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "feature/remove-photo", function(json){
					resolve(json);
				}, "POST", params).then(function(result){
					resolve(result);
				}.bind(this), function(){
					reject();
				}.bind(this));
			}.bind(this));
			return promise;
		},

		replacePhoto: function(objectId, featureType, photo, keywords){
			// https://developers.arcgis.com/rest/services-reference/add-attachment.htm
			var params = {
				objectId: objectId,
				featureType: featureType,
				attachmentId: photo.id
			};
			if (keywords) {
				params.keywords = keywords;
			}
			var files = [photo.file];
			var promise = new Promise(function(resolve, reject){
				CommonHelper.getFetchPromise(CommonHelper.webServicesRoot + "feature/replace-photo", function(json){
					return json;
				}, "POST", params, files).then(function(result){
					if (result && !result.error) {
						resolve(result);
					} else {
						reject();
					}
				}.bind(this), function(){
					reject();
				}.bind(this));
			}.bind(this));
			return promise;
		}
	}
</script>