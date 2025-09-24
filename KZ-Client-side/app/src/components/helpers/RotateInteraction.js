import Feature from "ol/Feature";
import LineString from "ol/geom/LineString";
import VectorLayer from "ol/layer/Vector";
import VectorSource from "ol/source/Vector";
import {Pointer as PointerInteraction} from "ol/interaction";
import {Circle as CircleStyle, Fill, RegularShape, Stroke, Style} from "ol/style";

// Iš čia ėmiau pvz.: https://openlayers.org/en/latest/examples/custom-interactions.html

var RotateInteraction = (function(PointerInteraction){

    function RotateInteraction(opt_options){
        var options = opt_options || {};
		this.feature = options.feature;
		this.refFeature = options.refFeature;
		this.myMap = options.myMap;
		this.opposite = options.opposite;
		this.rotationField = null;
		if (this.feature && this.myMap) {
			var featureGeometry = this.feature.getGeometry();
			if (featureGeometry.getType() == "Point") {
				if (this.feature.layer) {
					this.rotationField = this.feature.layer.rotationField;
				}
				if (this.rotationField) {
					this.createRotationFeatures(this.feature, this.myMap);
					PointerInteraction.call(this, {
						handleDownEvent: this.handleDownEvent,
						handleDragEvent: this.handleDragEvent,
						handleMoveEvent: this.handleMoveEvent,
						handleUpEvent: this.handleUpEvent
					});
				}
			}
		}
		this.coordinate_ = null;
		this.cursor_ = "move";
		this.feature_ = null;
		this.previousCursor_ = undefined;
    }

	if (PointerInteraction) {
		RotateInteraction.__proto__ = PointerInteraction;
	}
	RotateInteraction.prototype = Object.create(PointerInteraction && PointerInteraction.prototype);
	RotateInteraction.prototype.constructor = RotateInteraction;

    RotateInteraction.prototype.createRotationFeatures = function(feature, myMap){
		if (!myMap.drawHelperFeaturesLayer) {
			myMap.drawHelperFeaturesLayer = new VectorLayer({
				source: new VectorSource(),
				zIndex: 1000
			});
			myMap.map.addLayer(myMap.drawHelperFeaturesLayer);
		}
		var layerSource = myMap.drawHelperFeaturesLayer.getSource(),
			color = "#1976d2";
		this.rotationCenterFeature = new Feature(feature.getGeometry());
		this.rotationCenterFeature.setStyle(new Style({
			image: new RegularShape({
				fill: new Fill({
					color: color
				}),
				stroke: new Stroke({
					color: "black",
					width: 1
				}),
				points: 4,
				radius: 3,
				angle: Math.PI / 4
			})
		}));
		this.rotationEndFeature = new Feature(this.getRotationEndFeatureGeometry());
		this.rotationEndFeature.setStyle(new Style({
			image: new CircleStyle({
				radius: 7,
				fill: new Fill({
					color: color
				}),
				stroke: new Stroke({
					color: "black",
					width: 1
				})
			})
		}));
		this.rotationLineFeature = new Feature();
		this.setRotationLineFeatureGeometry();
		this.rotationLineFeature.setStyle(new Style({
			stroke: new Stroke({
				width: 2,
				color: color,
				lineDash: [2, 2],
				lineCap: "butt"
			})
		}));
		this.setRotationLineFeatureGeometry();
		layerSource.addFeatures([this.rotationLineFeature, this.rotationCenterFeature, this.rotationEndFeature]);
    };

    RotateInteraction.prototype.handleDownEvent = function(evt){
		var map = evt.map;
		var feature = map.forEachFeatureAtPixel(evt.pixel, function(feature){
			if (feature == this.rotationEndFeature) {
				return feature;
			}
		}.bind(this));
		if (feature) {
			this.coordinate_ = evt.coordinate;
			this.feature_ = feature;
		}
		return !!feature;
    };

    RotateInteraction.prototype.handleDragEvent = function(evt){
		var deltaX = evt.coordinate[0] - this.coordinate_[0];
		var deltaY = evt.coordinate[1] - this.coordinate_[1];
		var geometry = this.feature_.getGeometry();
		geometry.translate(deltaX, deltaY);
		this.coordinate_[0] = evt.coordinate[0];
		this.coordinate_[1] = evt.coordinate[1];
		this.setRotationLineFeatureGeometry();
		var angleBetweenPoints = this.getAngleBetweenPoints(this.rotationCenterFeature.getGeometry().getCoordinates(), this.rotationEndFeature.getGeometry().getCoordinates());
		angleBetweenPoints = Math.round(angleBetweenPoints);
		this.feature.set(this.rotationField, angleBetweenPoints);
		if (this.refFeature) {
			this.refFeature.set(this.rotationField, angleBetweenPoints); // To reikia objekto išsaugojimui... Jei to nebus, tai išsaugojus objektą nebus išsaugotas jo pasūkimas...
		}
    }

    RotateInteraction.prototype.handleMoveEvent = function(evt){
		if (this.cursor_) {
			var map = evt.map;
			var feature = map.forEachFeatureAtPixel(evt.pixel, function(feature){
				if (feature == this.rotationEndFeature) {
					return feature;
				}
			}.bind(this));
			var element = evt.map.getTargetElement();
			if (feature) {
				if (element.style.cursor != this.cursor_) {
					this.previousCursor_ = element.style.cursor;
					element.style.cursor = this.cursor_;
				}
			} else if (this.previousCursor_ !== undefined) {
				element.style.cursor = this.previousCursor_;
				this.previousCursor_ = undefined;
			}
		}
    }

    RotateInteraction.prototype.handleUpEvent = function(){
		this.coordinate_ = null;
		this.feature_ = null;
		if (this.rotationEndFeature) {
			this.rotationEndFeature.setGeometry(this.getRotationEndFeatureGeometry());
		}
		this.setRotationLineFeatureGeometry();
		return false;
    }

    RotateInteraction.prototype.setRotationLineFeatureGeometry = function(){
		if (this.rotationLineFeature) {
			var geometry = new LineString([
				this.rotationCenterFeature.getGeometry().getCoordinates(),
				this.rotationEndFeature.getGeometry().getCoordinates()
			]);
			this.rotationLineFeature.setGeometry(geometry);
		}
    }

    RotateInteraction.prototype.destroy = function(){
		if (this.myMap) {
			if (this.myMap.drawHelperFeaturesLayer) {
				this.myMap.drawHelperFeaturesLayer.getSource().clear(true);
			}
		}
    }

    RotateInteraction.prototype.getAngleBetweenPoints = function(p1, p2){
		var dx = p2[0] - p1[0],
			dy = p2[1] - p1[1],
			angle = Math.atan2(dx, dy);
		angle = angle * 180 / Math.PI;
		if (this.opposite) {
			angle += 180;
		}
		if (angle < 360) {
			angle += 360;
		} else if (angle > 360) {
			angle -= 360;
		}
		return angle;
    }

    RotateInteraction.prototype.getRotationEndFeatureGeometry = function(){
		var rotationEndFeatureGeometry = this.rotationCenterFeature.getGeometry().clone(),
			rotationAngle = this.feature.get(this.rotationField) || 0,
			distanceFromCenterFeature = 2;
		if (this.opposite) {
			rotationAngle += 180;
		}
		rotationAngle = rotationAngle * Math.PI / 180;
		var rotationEndFeatureGeometryCoordinates = rotationEndFeatureGeometry.getCoordinates();
		rotationEndFeatureGeometryCoordinates[0] += distanceFromCenterFeature * Math.sin(rotationAngle);
		rotationEndFeatureGeometryCoordinates[1] += distanceFromCenterFeature * Math.cos(rotationAngle);
		rotationEndFeatureGeometry.setCoordinates(rotationEndFeatureGeometryCoordinates);
		return rotationEndFeatureGeometry;
    }

    return RotateInteraction;

}(PointerInteraction));

export default RotateInteraction;