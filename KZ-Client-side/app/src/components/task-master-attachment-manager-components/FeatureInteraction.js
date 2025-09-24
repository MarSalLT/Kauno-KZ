import Feature from "ol/Feature";
import LineString from "ol/geom/LineString";
import Point from "ol/geom/Point";
import VectorLayer from "ol/layer/Vector";
import VectorSource from "ol/source/Vector";
import {Pointer as PointerInteraction} from "ol/interaction";
import {RegularShape, Fill, Stroke, Style} from "ol/style";
import {fromExtent} from "ol/geom/Polygon";

// Iš čia ėmiau pvz.: https://openlayers.org/en/latest/examples/custom-interactions.html

var FeatureInteraction = (function(PointerInteraction){

	if (PointerInteraction) {
		FeatureInteraction.__proto__ = PointerInteraction;
	}
	FeatureInteraction.prototype = Object.create(PointerInteraction && PointerInteraction.prototype);
	FeatureInteraction.prototype.constructor = FeatureInteraction;

	var stroke = new Stroke({
		width: 2,
		color: "orange"
	});

	var transparentColor = "rgba(255, 255, 255, 0.01)";

    function FeatureInteraction(opt_options){
        var options = opt_options || {};
		this.feature = options.feature;
		this.map = options.map;
		// this.scaleAnchor = "center";
		if (this.feature && this.map) {
			var type = this.feature.get("type");
			if (["point", "line", "polygon", "rectangle", "circle", "text", "arrow", "image-mock-polygon"].indexOf(type) != -1) {
				this.type = type;
				this.cursor = "grab" || "move";
				this.decorateFeature(this.feature, this.map, this.type);
				if (PointerInteraction) {
					PointerInteraction.call(this, {
						handleDownEvent: this.handleDownEvent,
						handleDragEvent: this.handleDragEvent,
						handleMoveEvent: this.handleMoveEvent,
						handleUpEvent: this.handleUpEvent
					});
				}
			}
		}
    }

	FeatureInteraction.prototype.decorateFeature = function(feature, map, type){
		if (!map.drawHelperFeaturesLayer) {
			map.drawHelperFeaturesLayer = new VectorLayer({
				source: new VectorSource(),
				style: this.styleFunction,
				zIndex: 1000
			});
			map.addLayer(map.drawHelperFeaturesLayer);
		}
		var layerSource = map.drawHelperFeaturesLayer.getSource(),
			decoratorFeatureGeometry = feature.getGeometry();
		if (decoratorFeatureGeometry) {
			var decoratorFeature = new Feature({
				geometry: decoratorFeatureGeometry,
				type: type
			});
			if (type == "point") {
				decoratorFeature.set("point-size", feature.get("point-size"));
			}
			this.decoratorFeature = decoratorFeature;
			layerSource.addFeature(decoratorFeature);
			this.drawDecoratorFeatures(this.decoratorFeature);
			this.featureGeometryChangeListener = function(f){
				decoratorFeature.setGeometry(f.target.getGeometry());
				this.drawDecoratorFeatures();
			}.bind(this);
			feature.on("change:geometry", this.featureGeometryChangeListener); // Aktualu keičiant nuotraukų pasukimo kampą... Hmmm... Gal galima ir kažką gudresnio sugalvoti, bet visai veikia ir tai?..
		}
    };

    FeatureInteraction.prototype.styleFunction = function(feature, resolution){
		var style,
			type = feature.get("type");
		if (type == "point") {
			style = new Style({
				image: new RegularShape({
					stroke: stroke,
					fill: new Fill({
						color: transparentColor
					}),
					radius: (feature.get("point-size") || 10) / resolution * 10 + 80 / resolution,
					points: 4,
					angle: Math.PI / 4
				})
			});
		} else if (type == "text") {
			// O kaip gauti teksto ribas?.. Visų pirma, pats `feature` turi turėti stilių (o ne jo sluoksnis?), o po to jau nagrinėti... Gal atsakymas modulyje `TextBuilder.js`?..
			style = new Style({
				image: new RegularShape({
					stroke: stroke,
					fill: new Fill({
						color: transparentColor
					}),
					radius: (feature.get("point-size") || 10) / resolution * 10 + 80 / resolution,
					points: 4,
					angle: Math.PI / 4
				})
			});
		} else if (type == "vertex") {
			style = new Style({
				image: new RegularShape({
					stroke: stroke,
					fill: new Fill({
						color: "orange"
					}),
					radius: 10,
					points: 4,
					angle: Math.PI / 4
				})
			});
		} else {
			style = new Style({
				stroke: stroke,
				fill: new Fill({
					color: transparentColor
				}),
				geometry: function(feature){
					return fromExtent(feature.getGeometry().getExtent());
				}.bind(this)
			});
		}
		return style;
    }

    FeatureInteraction.prototype.handleMoveEvent = function(evt){
		if (!this.paused) {
			if (this.cursor) {
				var map = evt.map;
				var feature = map.forEachFeatureAtPixel(evt.pixel, function(feature, layer){
					if (layer && (layer == map.drawHelperFeaturesLayer)) {
						return feature;
					}
				});
				var element = evt.map.getTargetElement();
				if (feature) {
					var c = this.cursor;
					if (feature.get("type") == "vertex") {
						c = "move";
					}
					if (element.style.cursor != c) {
						element.style.cursor = c;
					}
				} else {
					element.style.cursor = "default";
				}
			}
		}
    }

    FeatureInteraction.prototype.handleDownEvent = function(evt){
		if (!this.paused) {
			var map = evt.map;
			var feature = map.forEachFeatureAtPixel(evt.pixel, function(feature, layer){
				if (layer && (layer == map.drawHelperFeaturesLayer)) {
					return feature;
				}
			});
			if (feature) {
				this.feature.ignoreStyleGeom = feature.ignoreStyleGeom = true;
				this.feature.origGeometry = null; // Nuotraukų rotate'inimui aktualu!!!
				this.pointerDownCoordinate = evt.coordinate;
				this.pointerActiveFeature = feature;
				this.pointerActiveFeatureInitialGeometry = feature.getGeometry().clone();
			}
			return !!feature;
		}
    };

    FeatureInteraction.prototype.handleDragEvent = function(evt){
		if (!this.paused) {
			if (this.pointerActiveFeature && this.pointerActiveFeature.get("type") == "vertex") {
				var i = this.pointerActiveFeature.get("i"),
					oppositeI;
				if (i > 1) {
					oppositeI = i - 2;
				} else {
					oppositeI = i + 2;
				}
				if (Number.isInteger(i) && Number.isInteger(oppositeI)) {
					// Dabar reikia gauti ilgį nuo dabartinio taško iki priešingojo bei jį lyginti su prieš tai buvusiu ilgiu?
					// Ir elgtis atitinkamai...
					var layerSource = this.map.drawHelperFeaturesLayer.getSource(),
						point,
						oppositePoint;
					layerSource.getFeatures().forEach(function(f){
						if (f.get("type") == "vertex") {
							if (f.get("i") === i) {
								point = f.getGeometry();
							} else if (f.get("i") === oppositeI) {
								oppositePoint = f.getGeometry();
							}
						}
					});
					if (point && oppositePoint) {
						var oldLength = new LineString([point.getCoordinates(), oppositePoint.getCoordinates()]).getLength(),
							newLength = new LineString([evt.coordinate, oppositePoint.getCoordinates()]).getLength();
					}
					var g = this.feature.getGeometry().clone();
					g.scale(newLength / oldLength, undefined, this.scaleAnchor == "center" ? undefined : oppositePoint.getCoordinates()); // https://openlayers.org/en/latest/apidoc/module-ol_geom_Geometry-Geometry.html#scale
					if (this.feature.clipFeatures) {
						this.feature.clipFeatures.forEach(function(clipFeature){
							var clipFeatureGeometry = clipFeature.getGeometry().clone();
							clipFeatureGeometry.scale(newLength / oldLength, undefined, this.scaleAnchor == "center" ? undefined : oppositePoint.getCoordinates());
							clipFeature.setGeometry(clipFeatureGeometry);
						}.bind(this));
					}
					this.feature.setGeometry(g);
				}
			} else {
				var deltaX = evt.coordinate[0] - this.pointerDownCoordinate[0],
					deltaY = evt.coordinate[1] - this.pointerDownCoordinate[1],
					geometry = this.pointerActiveFeature.getGeometry();
				geometry.translate(deltaX, deltaY);
				if (this.feature.clipFeatures) {
					this.feature.clipFeatures.forEach(function(clipFeature){
						clipFeature.getGeometry().translate(deltaX, deltaY);
					});
				}
				this.pointerDownCoordinate[0] = evt.coordinate[0];
				this.pointerDownCoordinate[1] = evt.coordinate[1];
				this.drawDecoratorFeatures();
			}
		}
    }

    FeatureInteraction.prototype.handleUpEvent = function(){
		if (!this.paused) {
			if (this.pointerActiveFeature) {
				this.pointerActiveFeature.ignoreStyleGeom = false;
			}
			this.feature.ignoreStyleGeom = false;
			this.pointerDownCoordinate = null;
			this.pointerActiveFeature = null;
			return false;
		}
    }

    FeatureInteraction.prototype.drawDecoratorFeatures = function(){
		var feature = this.feature,
			type = this.feature.get("type"),
			layerSource = this.map.drawHelperFeaturesLayer.getSource(),
			decoratorVertexFeature;
		if (["Linestring", "Polygon", "Circle"].indexOf(feature.getGeometry().getType()) != -1) {
			if (["rectangle", "circle", "image-mock-polygon"].indexOf(type) != -1) {
				var g = fromExtent(feature.getGeometry().getExtent()),
					features = [];
				if (this.decoratorFeature) {
					features.push(this.decoratorFeature);
				}
				var coordinates = g.getCoordinates()[0].slice(0, 4);
				coordinates.forEach(function(c, i){
					decoratorVertexFeature = new Feature({
						geometry: new Point(c),
						type: "vertex",
						i: i
					});
					features.push(decoratorVertexFeature);
				});
				layerSource.clear(true);
				layerSource.addFeatures(features);
			}
		}
		
    }

    FeatureInteraction.prototype.destroy = function(){
		if (this.feature && this.featureGeometryChangeListener) {
			this.feature.un("change:geometry", this.featureGeometryChangeListener); 
		}
		if (this.map) {
			if (this.map.drawHelperFeaturesLayer) {
				this.map.drawHelperFeaturesLayer.getSource().clear(true);
			}
			this.map.getTargetElement().style.cursor = "default"; // Atsarga gėdos nedaro? Jei to nebus, tai retais atvejais kartais žemėlapiui taip ir pasilieka tas "grab" kursorius...
		}
    }

    FeatureInteraction.prototype.pause = function(){
		this.paused = true;
		if (this.map) {
			this.map.getTargetElement().style.cursor = "default"; // Atsarga gėdos nedaro? Jei to nebus, tai retais atvejais kartais žemėlapiui taip ir pasilieka tas "grab" kursorius...
		}
    }

    FeatureInteraction.prototype.resume = function(){
		this.paused = false;
    }

    return FeatureInteraction;

}(PointerInteraction));

export default FeatureInteraction;