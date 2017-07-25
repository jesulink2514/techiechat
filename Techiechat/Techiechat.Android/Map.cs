using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Techiechat.Droid;
using Techiechat.Helpers;
using TK.CustomMap;
using TK.CustomMap.Api.Google;
using TK.CustomMap.Droid;
using TK.CustomMap.Interfaces;
using TK.CustomMap.Models;
using TK.CustomMap.Overlays;
using TK.CustomMap.Utilities;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Maps.Android;
using Xamarin.Forms.Platform.Android;
using Color = Xamarin.Forms.Color;
using Point = Xamarin.Forms.Point;

[assembly: ExportRenderer(typeof(LocalMap), typeof(LocalMapRenderer))]

namespace Techiechat.Droid

{
    public class LocalMapRenderer : MapRenderer, IRendererFunctions, IOnMapReadyCallback,
        GoogleMap.ISnapshotReadyCallback

    {
        private readonly Dictionary<TKCircle, Circle> _circles = new Dictionary<TKCircle, Circle>();

        private readonly Dictionary<TKCustomMapPin, Marker> _markers = new Dictionary<TKCustomMapPin, Marker>();

        private readonly Dictionary<TKPolygon, Polygon> _polygons = new Dictionary<TKPolygon, Polygon>();

        private readonly Dictionary<TKPolyline, Polyline> _polylines = new Dictionary<TKPolyline, Polyline>();


        private readonly Dictionary<TKRoute, Polyline> _routes = new Dictionary<TKRoute, Polyline>();


        private readonly List<TKRoute> _tempRouteList = new List<TKRoute>();

        private GoogleMap _googleMap;

        private bool _init = true;

        private bool _isDragging;

        private bool _onMapReadyPerformed; // Hack


        private Marker _selectedMarker;

        private byte[] _snapShot;


        private TileOverlay _tileOverlay;


        private TKCustomMap FormsMap => Element as TKCustomMap;

        private IMapFunctions MapFunctions => Element as IMapFunctions;

        /// <summary>
        ///     When the map is ready to use
        /// </summary>
        /// <param name="googleMap">The map instance</param>
        public virtual void OnMapReady(GoogleMap googleMap)

        {
            if (_onMapReadyPerformed) return;


            _onMapReadyPerformed = true;


            InvokeOnMapReadyBaseClassHack(googleMap);


            _googleMap = googleMap;


            _googleMap.MarkerClick += OnMarkerClick;

            _googleMap.MapClick += OnMapClick;

            _googleMap.MapLongClick += OnMapLongClick;

            _googleMap.MarkerDragEnd += OnMarkerDragEnd;

            _googleMap.MarkerDrag += OnMarkerDrag;

            _googleMap.CameraChange += OnCameraChange;

            _googleMap.MarkerDragStart += OnMarkerDragStart;

            _googleMap.InfoWindowClick += OnInfoWindowClick;

            _googleMap.MyLocationChange += OnUserLocationChange;


            UpdateTileOptions();

            MoveToCenter();

            UpdatePins();

            UpdateRoutes();

            UpdateLines();

            UpdateCircles();

            UpdatePolygons();

            UpdateShowTraffic();
        }

        /// <inheritdoc />
        public async Task<byte[]> GetSnapshot()

        {
            if (_googleMap == null) return null;


            _snapShot = null;

            _googleMap.Snapshot(this);


            while (_snapShot == null) await Task.Delay(10);


            return _snapShot;
        }

        /// <inheritdoc />
        public void FitMapRegionToPositions(IEnumerable<Position> positions, bool animate = false)

        {
            if (_googleMap == null) throw new InvalidOperationException("Map not ready");

            if (positions == null) throw new InvalidOperationException("positions can't be null");


            var builder = new LatLngBounds.Builder();


            positions.ToList().ForEach(i => builder.Include(i.ToLatLng()));


            if (animate)

                _googleMap.AnimateCamera(CameraUpdateFactory.NewLatLngBounds(builder.Build(), 30));

            else

                _googleMap.MoveCamera(CameraUpdateFactory.NewLatLngBounds(builder.Build(), 30));
        }

        /// <inheritdoc />
        public void MoveToMapRegion(MapSpan region, bool animate)

        {
            if (_googleMap == null) return;


            if (region == null) return;


            var bounds = BoundsFromMapSpans(region);

            if (bounds == null) return;

            var cam = CameraUpdateFactory.NewLatLngBounds(bounds, 0);


            if (animate)

                _googleMap.AnimateCamera(cam);

            else

                _googleMap.MoveCamera(cam);
        }

        /// <inheritdoc />
        public void FitToMapRegions(IEnumerable<MapSpan> regions, bool animate)

        {
            if (_googleMap == null || regions == null || !regions.Any()) return;


            var bounds = BoundsFromMapSpans(regions.ToArray());

            if (bounds == null) return;

            var cam = CameraUpdateFactory.NewLatLngBounds(bounds, 0);


            if (animate)

                _googleMap.AnimateCamera(cam);

            else

                _googleMap.MoveCamera(cam);
        }

        /// <inheritdoc />
        public IEnumerable<Position> ScreenLocationsToGeocoordinates(params Point[] screenLocations)

        {
            if (_googleMap == null)

                throw new InvalidOperationException("Map not initialized");


            return screenLocations.Select(
                i => _googleMap.Projection.FromScreenLocation(i.ToAndroidPoint()).ToPosition());
        }

        /// <inheritdoc />
        public void OnSnapshotReady(Bitmap snapshot)

        {
            using (var strm = new MemoryStream())

            {
                snapshot.Compress(Bitmap.CompressFormat.Png, 100, strm);

                _snapShot = strm.ToArray();
            }
        }


        /// <inheritdoc />
        protected override void OnElementChanged(ElementChangedEventArgs<Map> e)

        {
            base.OnElementChanged(e);


            var mapView = Control;

            if (mapView == null) return;


            if (e.OldElement != null && _googleMap != null)

            {
                e.OldElement.PropertyChanged -= FormsMapPropertyChanged;


                _googleMap.MarkerClick -= OnMarkerClick;

                _googleMap.MapClick -= OnMapClick;

                _googleMap.MapLongClick -= OnMapLongClick;

                _googleMap.MarkerDragEnd -= OnMarkerDragEnd;

                _googleMap.MarkerDrag -= OnMarkerDrag;

                _googleMap.CameraChange -= OnCameraChange;

                _googleMap.MarkerDragStart -= OnMarkerDragStart;

                _googleMap.InfoWindowClick -= OnInfoWindowClick;

                _googleMap.MyLocationChange -= OnUserLocationChange;

                UnregisterCollections((TKCustomMap) e.OldElement);
            }


            if (e.NewElement != null)

            {
                MapFunctions.SetRenderer(this);


                mapView.GetMapAsync(this);

                FormsMap.PropertyChanged += FormsMapPropertyChanged;
            }
        }

        /// <inheritdoc />
        protected override void OnLayout(bool changed, int l, int t, int r, int b)

        {
            base.OnLayout(changed, l, t, r, b);


            if (_init)

            {
                MoveToCenter();

                _init = false;
            }
        }

        /// <summary>
        ///     When a property of the Forms map changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void FormsMapPropertyChanged(object sender, PropertyChangedEventArgs e)

        {
            if (_googleMap == null) return;


            if (e.PropertyName == TKCustomMap.CustomPinsProperty.PropertyName)

                UpdatePins();

            else if (e.PropertyName == TKCustomMap.SelectedPinProperty.PropertyName)

                SetSelectedItem();

            else if (e.PropertyName == TKCustomMap.MapCenterProperty.PropertyName)

                MoveToCenter();

            else if (e.PropertyName == TKCustomMap.PolylinesProperty.PropertyName)

                UpdateLines();

            else if (e.PropertyName == TKCustomMap.CirclesProperty.PropertyName)

                UpdateCircles();

            else if (e.PropertyName == TKCustomMap.PolygonsProperty.PropertyName)

                UpdatePolygons();

            else if (e.PropertyName == TKCustomMap.RoutesProperty.PropertyName)

                UpdateRoutes();

            else if (e.PropertyName == TKCustomMap.TilesUrlOptionsProperty.PropertyName)

                UpdateTileOptions();

            else if (e.PropertyName == TKCustomMap.ShowTrafficProperty.PropertyName)

                UpdateShowTraffic();

            else if (e.PropertyName == TKCustomMap.MapRegionProperty.PropertyName)

                UpdateMapRegion();
        }

        /// <summary>
        ///     When the location of the user changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnUserLocationChange(object sender, GoogleMap.MyLocationChangeEventArgs e)

        {
            if (e.Location == null || FormsMap == null || FormsMap.UserLocationChangedCommand == null) return;


            var newPosition = new Position(e.Location.Latitude, e.Location.Longitude);

            MapFunctions.RaiseUserLocationChanged(newPosition);
        }

        /// <summary>
        ///     When the info window gets clicked
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnInfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs e)

        {
            var pin = GetPinByMarker(e.Marker);


            if (pin == null) return;


            if (pin.IsCalloutClickable)

                MapFunctions.RaiseCalloutClicked(pin);
        }

        /// <summary>
        ///     Dragging process
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnMarkerDrag(object sender, GoogleMap.MarkerDragEventArgs e)

        {
            var item = _markers.SingleOrDefault(i => i.Value.Id.Equals(e.Marker.Id));

            if (item.Key == null) return;


            item.Key.Position = e.Marker.Position.ToPosition();
        }

        /// <summary>
        ///     When a dragging starts
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnMarkerDragStart(object sender, GoogleMap.MarkerDragStartEventArgs e)

        {
            _isDragging = true;
        }

        /// <summary>
        ///     When the camera position changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnCameraChange(object sender, GoogleMap.CameraChangeEventArgs e)

        {
            if (FormsMap == null) return;


            FormsMap.MapCenter = e.Position.Target.ToPosition();

            base.OnCameraChange(e.Position);
        }

        /// <summary>
        ///     When a pin gets clicked
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnMarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)

        {
            if (FormsMap == null) return;

            var item = _markers.SingleOrDefault(i => i.Value.Id.Equals(e.Marker.Id));

            if (item.Key == null) return;


            _selectedMarker = e.Marker;

            FormsMap.SelectedPin = item.Key;

            if (item.Key.ShowCallout)

                item.Value.ShowInfoWindow();
        }

        /// <summary>
        ///     When a drag of a marker ends
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnMarkerDragEnd(object sender, GoogleMap.MarkerDragEndEventArgs e)

        {
            _isDragging = false;


            if (FormsMap == null) return;


            var pin = _markers.SingleOrDefault(i => i.Value.Id.Equals(e.Marker.Id));

            if (pin.Key == null) return;


            MapFunctions.RaisePinDragEnd(pin.Key);
        }

        /// <summary>
        ///     When a long click was performed on the map
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnMapLongClick(object sender, GoogleMap.MapLongClickEventArgs e)

        {
            if (FormsMap == null || FormsMap.MapLongPressCommand == null) return;


            var position = e.Point.ToPosition();

            MapFunctions.RaiseMapLongPress(position);
        }

        /// <summary>
        ///     When the map got tapped
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnMapClick(object sender, GoogleMap.MapClickEventArgs e)

        {
            if (FormsMap == null) return;


            var position = e.Point.ToPosition();


            if (FormsMap.Routes != null && FormsMap.RouteClickedCommand != null)

                foreach (var route in FormsMap.Routes.Where(i => i.Selectable))

                {
                    var internalRoute = _routes[route];


                    if (GmsPolyUtil.IsLocationOnPath(
                        position,
                        internalRoute.Points.Select(i => i.ToPosition()),
                        true,
                        (int) _googleMap.CameraPosition.Zoom,
                        FormsMap.MapCenter.Latitude))

                    {
                        MapFunctions.RaiseRouteClicked(route);

                        return;
                    }
                }

            MapFunctions.RaiseMapClicked(position);
        }

        /// <summary>
        ///     Updates the markers when a pin gets added or removed in the collection
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnCustomPinsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)

        {
            if (e.Action == NotifyCollectionChangedAction.Add)

                foreach (TKCustomMapPin pin in e.NewItems)

                    AddPin(pin);

            else if (e.Action == NotifyCollectionChangedAction.Remove)

                foreach (TKCustomMapPin pin in e.OldItems)

                    if (!FormsMap.CustomPins.Contains(pin))

                        RemovePin(pin);

                    else if (e.Action == NotifyCollectionChangedAction.Reset)

                        UpdatePins(false);
        }

        /// <summary>
        ///     When a property of a pin changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private async void OnPinPropertyChanged(object sender, PropertyChangedEventArgs e)

        {
            var pin = sender as TKCustomMapPin;

            if (pin == null) return;


            Marker marker = null;

            if (!_markers.ContainsKey(pin) || (marker = _markers[pin]) == null) return;


            switch (e.PropertyName)

            {
                case TKCustomMapPin.TitlePropertyName:

                    marker.Title = pin.Title;

                    break;

                case TKCustomMapPin.SubititlePropertyName:

                    marker.Snippet = pin.Subtitle;

                    break;

                case TKCustomMapPin.ImagePropertyName:

                    await UpdateImage(pin, marker);

                    break;

                case TKCustomMapPin.DefaultPinColorPropertyName:

                    await UpdateImage(pin, marker);

                    break;

                case TKCustomMapPin.PositionPropertyName:

                    if (!_isDragging)

                        marker.Position = new LatLng(pin.Position.Latitude, pin.Position.Longitude);

                    break;

                case TKCustomMapPin.IsVisiblePropertyName:

                    marker.Visible = pin.IsVisible;

                    break;

                case TKCustomMapPin.AnchorPropertyName:

                    if (pin.Image != null)

                        marker.SetAnchor((float) pin.Anchor.X, (float) pin.Anchor.Y);

                    break;

                case TKCustomMapPin.IsDraggablePropertyName:

                    marker.Draggable = pin.IsDraggable;

                    break;

                case TKCustomMapPin.RotationPropertyName:

                    marker.Rotation = (float) pin.Rotation;

                    break;
            }
        }

        /// <summary>
        ///     Collection of routes changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnLineCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)

        {
            if (e.Action == NotifyCollectionChangedAction.Add)

                foreach (TKPolyline line in e.NewItems)

                    AddLine(line);

            else if (e.Action == NotifyCollectionChangedAction.Remove)

                foreach (TKPolyline line in e.OldItems)

                    if (!FormsMap.Polylines.Contains(line))

                    {
                        _polylines[line].Remove();

                        line.PropertyChanged -= OnLinePropertyChanged;

                        _polylines.Remove(line);
                    }

                    else if (e.Action == NotifyCollectionChangedAction.Reset)

                    {
                        UpdateLines(false);
                    }
        }

        /// <summary>
        ///     A property of a route changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnLinePropertyChanged(object sender, PropertyChangedEventArgs e)

        {
            var line = (TKPolyline) sender;


            if (e.PropertyName == TKPolyline.LineCoordinatesPropertyName)

                if (line.LineCoordinates != null && line.LineCoordinates.Count > 1)

                    _polylines[line].Points = new List<LatLng>(line.LineCoordinates.Select(i => i.ToLatLng()));

                else

                    _polylines[line].Points = null;

            else if (e.PropertyName == TKOverlay.ColorPropertyName)

                _polylines[line].Color = line.Color.ToAndroid().ToArgb();

            else if (e.PropertyName == TKPolyline.LineWidthProperty)

                _polylines[line].Width = line.LineWidth;
        }

        /// <summary>
        ///     Creates all Markers on the map
        /// </summary>
        private void UpdatePins(bool firstUpdate = true)

        {
            if (_googleMap == null) return;


            foreach (var i in _markers)

                RemovePin(i.Key, false);

            _markers.Clear();

            if (FormsMap.CustomPins != null)

            {
                foreach (var pin in FormsMap.CustomPins)

                    AddPin(pin);

                if (firstUpdate)

                {
                    var observAble = FormsMap.CustomPins as INotifyCollectionChanged;

                    if (observAble != null)

                        observAble.CollectionChanged += OnCustomPinsCollectionChanged;
                }

                MapFunctions.RaisePinsReady();
            }
        }

        /// <summary>
        ///     Adds a marker to the map
        /// </summary>
        /// <param name="pin">The Forms Pin</param>
        private async void AddPin(TKCustomMapPin pin)

        {
            if (_markers.Keys.Contains(pin)) return;


            pin.PropertyChanged += OnPinPropertyChanged;


            var markerWithIcon = new MarkerOptions();

            markerWithIcon.SetPosition(new LatLng(pin.Position.Latitude, pin.Position.Longitude));


            if (!string.IsNullOrWhiteSpace(pin.Title))

                markerWithIcon.SetTitle(pin.Title);

            if (!string.IsNullOrWhiteSpace(pin.Subtitle))

                markerWithIcon.SetSnippet(pin.Subtitle);


            await UpdateImage(pin, markerWithIcon);

            markerWithIcon.Draggable(pin.IsDraggable);

            markerWithIcon.Visible(pin.IsVisible);

            markerWithIcon.SetRotation((float) pin.Rotation);

            if (pin.Image != null)

                markerWithIcon.Anchor((float) pin.Anchor.X, (float) pin.Anchor.Y);


            _markers.Add(pin, _googleMap.AddMarker(markerWithIcon));
        }

        /// <summary>
        ///     Remove a pin from the map and the internal dictionary
        /// </summary>
        /// <param name="pin">The pin to remove</param>
        private void RemovePin(TKCustomMapPin pin, bool removeMarker = true)

        {
            var item = _markers[pin];

            if (item == null) return;


            if (_selectedMarker != null)

                if (item.Id.Equals(_selectedMarker.Id))

                    FormsMap.SelectedPin = null;


            item.Remove();

            pin.PropertyChanged -= OnPinPropertyChanged;


            if (removeMarker)

                _markers.Remove(pin);
        }

        /// <summary>
        ///     Set the selected item on the map
        /// </summary>
        private void SetSelectedItem()

        {
            if (_selectedMarker != null)

            {
                _selectedMarker.HideInfoWindow();

                _selectedMarker = null;
            }

            if (FormsMap.SelectedPin != null)

            {
                if (!_markers.ContainsKey(FormsMap.SelectedPin)) return;


                var selectedPin = _markers[FormsMap.SelectedPin];

                _selectedMarker = selectedPin;

                if (FormsMap.SelectedPin.ShowCallout)

                    selectedPin.ShowInfoWindow();

                MapFunctions.RaisePinSelected(FormsMap.SelectedPin);
            }
        }

        /// <summary>
        ///     Move the google map to the map center
        /// </summary>
        private void MoveToCenter()

        {
            if (_googleMap == null) return;


            if (FormsMap != null && !FormsMap.MapCenter.Equals(_googleMap.CameraPosition.Target.ToPosition()))

            {
                var cameraUpdate = CameraUpdateFactory.NewLatLng(FormsMap.MapCenter.ToLatLng());


                if (FormsMap.IsRegionChangeAnimated && !_init)

                    _googleMap.AnimateCamera(cameraUpdate);

                else

                    _googleMap.MoveCamera(cameraUpdate);
            }
        }

        /// <summary>
        ///     Creates the routes on the map
        /// </summary>
        private void UpdateLines(bool firstUpdate = true)

        {
            if (_googleMap == null) return;


            foreach (var i in _polylines)

            {
                i.Key.PropertyChanged -= OnLinePropertyChanged;

                i.Value.Remove();
            }

            _polylines.Clear();


            if (FormsMap.Polylines != null)

            {
                foreach (var line in FormsMap.Polylines)

                    AddLine(line);


                if (firstUpdate)

                {
                    var observAble = FormsMap.Polylines as INotifyCollectionChanged;

                    if (observAble != null)

                        observAble.CollectionChanged += OnLineCollectionChanged;
                }
            }
        }

        /// <summary>
        ///     Updates all circles
        /// </summary>
        private void UpdateCircles(bool firstUpdate = true)

        {
            if (_googleMap == null) return;


            foreach (var i in _circles)

            {
                i.Key.PropertyChanged -= CirclePropertyChanged;

                i.Value.Remove();
            }

            _circles.Clear();

            if (FormsMap.Circles != null)

            {
                foreach (var circle in FormsMap.Circles)

                    AddCircle(circle);

                if (firstUpdate)

                {
                    var observAble = FormsMap.Circles as INotifyCollectionChanged;

                    if (observAble != null)

                        observAble.CollectionChanged += CirclesCollectionChanged;
                }
            }
        }

        /// <summary>
        ///     Creates the polygones on the map
        /// </summary>
        /// <param name="firstUpdate">If the collection updates the first time</param>
        private void UpdatePolygons(bool firstUpdate = true)

        {
            if (_googleMap == null) return;


            foreach (var i in _polygons)

            {
                i.Key.PropertyChanged -= OnPolygonPropertyChanged;

                i.Value.Remove();
            }

            _polygons.Clear();

            if (FormsMap.Polygons != null)

            {
                foreach (var i in FormsMap.Polygons)

                    AddPolygon(i);

                if (firstUpdate)

                {
                    var observAble = FormsMap.Polygons as INotifyCollectionChanged;

                    if (observAble != null)

                        observAble.CollectionChanged += OnPolygonsCollectionChanged;
                }
            }
        }

        /// <summary>
        ///     Create all routes
        /// </summary>
        /// <param name="firstUpdate">If first update of collection or not</param>
        private void UpdateRoutes(bool firstUpdate = true)

        {
            _tempRouteList.Clear();


            if (_googleMap == null) return;


            foreach (var i in _routes)

            {
                if (i.Key != null)

                    i.Key.PropertyChanged -= OnRoutePropertyChanged;

                i.Value.Remove();
            }

            _routes.Clear();


            if (FormsMap == null || FormsMap.Routes == null) return;


            foreach (var i in FormsMap.Routes)

                AddRoute(i);


            if (firstUpdate)

            {
                var observAble = FormsMap.Routes as INotifyCollectionChanged;

                if (observAble != null)

                    observAble.CollectionChanged += OnRouteCollectionChanged;
            }
        }

        /// <summary>
        ///     When the collection of routes changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnRouteCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)

        {
            if (e.Action == NotifyCollectionChangedAction.Add)

                foreach (TKRoute route in e.NewItems)

                    AddRoute(route);

            else if (e.Action == NotifyCollectionChangedAction.Remove)

                foreach (TKRoute route in e.OldItems)

                    if (!FormsMap.Routes.Contains(route))

                    {
                        _routes[route].Remove();

                        route.PropertyChanged -= OnRoutePropertyChanged;

                        _routes.Remove(route);
                    }

                    else if (e.Action == NotifyCollectionChangedAction.Reset)

                    {
                        UpdateRoutes(false);
                    }
        }

        /// <summary>
        ///     When a property of a route changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnRoutePropertyChanged(object sender, PropertyChangedEventArgs e)

        {
            var route = (TKRoute) sender;


            if (e.PropertyName == TKRoute.SourceProperty ||
                e.PropertyName == TKRoute.DestinationProperty ||
                e.PropertyName == TKRoute.TravelModelProperty)

            {
                route.PropertyChanged -= OnRoutePropertyChanged;

                _routes[route].Remove();

                _routes.Remove(route);


                AddRoute(route);
            }

            else if (e.PropertyName == TKOverlay.ColorPropertyName)

            {
                _routes[route].Color = route.Color.ToAndroid().ToArgb();
            }

            else if (e.PropertyName == TKPolyline.LineWidthProperty)

            {
                _routes[route].Width = route.LineWidth;
            }
        }

        /// <summary>
        ///     When the polygon collection changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnPolygonsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)

        {
            if (e.Action == NotifyCollectionChangedAction.Add)

                foreach (TKPolygon poly in e.NewItems)

                    AddPolygon(poly);

            else if (e.Action == NotifyCollectionChangedAction.Remove)

                foreach (TKPolygon poly in e.OldItems)

                    if (!FormsMap.Polygons.Contains(poly))

                    {
                        _polygons[poly].Remove();

                        poly.PropertyChanged -= OnPolygonPropertyChanged;

                        _polygons.Remove(poly);
                    }

                    else if (e.Action == NotifyCollectionChangedAction.Reset)

                    {
                        UpdatePolygons(false);
                    }
        }

        /// <summary>
        ///     Adds a polygon to the map
        /// </summary>
        /// <param name="polygon">The polygon to add</param>
        private void AddPolygon(TKPolygon polygon)

        {
            polygon.PropertyChanged += OnPolygonPropertyChanged;


            var polygonOptions = new PolygonOptions();


            if (polygon.Coordinates != null && polygon.Coordinates.Any())

                polygonOptions.Add(polygon.Coordinates.Select(i => i.ToLatLng()).ToArray());

            if (polygon.Color != Color.Default)

                polygonOptions.InvokeFillColor(polygon.Color.ToAndroid().ToArgb());

            if (polygon.StrokeColor != Color.Default)

                polygonOptions.InvokeStrokeColor(polygon.StrokeColor.ToAndroid().ToArgb());

            polygonOptions.InvokeStrokeWidth(polygonOptions.StrokeWidth);


            _polygons.Add(polygon, _googleMap.AddPolygon(polygonOptions));
        }

        /// <summary>
        ///     When a property of a <see cref="TKPolygon" /> changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void OnPolygonPropertyChanged(object sender, PropertyChangedEventArgs e)

        {
            var tkPolygon = (TKPolygon) sender;


            switch (e.PropertyName)

            {
                case TKPolygon.CoordinatesPropertyName:

                    _polygons[tkPolygon].Points = tkPolygon.Coordinates.Select(i => i.ToLatLng()).ToList();

                    break;

                case TKOverlay.ColorPropertyName:

                    _polygons[tkPolygon].FillColor = tkPolygon.Color.ToAndroid().ToArgb();

                    break;

                case TKPolygon.StrokeColorPropertyName:

                    _polygons[tkPolygon].StrokeColor = tkPolygon.StrokeColor.ToAndroid().ToArgb();

                    break;

                case TKPolygon.StrokeWidthPropertyName:

                    _polygons[tkPolygon].StrokeWidth = tkPolygon.StrokeWidth;

                    break;
            }
        }

        /// <summary>
        ///     When the circle collection changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void CirclesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)

        {
            if (e.Action == NotifyCollectionChangedAction.Add)

                foreach (TKCircle circle in e.NewItems)

                    AddCircle(circle);

            else if (e.Action == NotifyCollectionChangedAction.Remove)

                foreach (TKCircle circle in e.OldItems)

                    if (!FormsMap.Circles.Contains(circle))

                    {
                        circle.PropertyChanged -= CirclePropertyChanged;

                        _circles[circle].Remove();

                        _circles.Remove(circle);
                    }

                    else if (e.Action == NotifyCollectionChangedAction.Reset)

                    {
                        UpdateCircles(false);
                    }
        }

        /// <summary>
        ///     Adds a circle to the map
        /// </summary>
        /// <param name="circle">The circle to add</param>
        private void AddCircle(TKCircle circle)

        {
            circle.PropertyChanged += CirclePropertyChanged;


            var circleOptions = new CircleOptions();


            circleOptions.InvokeRadius(circle.Radius);

            circleOptions.InvokeCenter(circle.Center.ToLatLng());


            if (circle.Color != Color.Default)

                circleOptions.InvokeFillColor(circle.Color.ToAndroid().ToArgb());

            if (circle.StrokeColor != Color.Default)

                circleOptions.InvokeStrokeColor(circle.StrokeColor.ToAndroid().ToArgb());

            circleOptions.InvokeStrokeWidth(circle.StrokeWidth);

            _circles.Add(circle, _googleMap.AddCircle(circleOptions));
        }

        /// <summary>
        ///     When a property of a <see cref="TKCircle" /> changed
        /// </summary>
        /// <param name="sender">Event Sender</param>
        /// <param name="e">Event Arguments</param>
        private void CirclePropertyChanged(object sender, PropertyChangedEventArgs e)

        {
            var tkCircle = (TKCircle) sender;

            var circle = _circles[tkCircle];


            switch (e.PropertyName)

            {
                case TKCircle.RadiusPropertyName:

                    circle.Radius = tkCircle.Radius;

                    break;

                case TKCircle.CenterPropertyName:

                    circle.Center = tkCircle.Center.ToLatLng();

                    break;

                case TKOverlay.ColorPropertyName:

                    circle.FillColor = tkCircle.Color.ToAndroid().ToArgb();

                    break;

                case TKCircle.StrokeColorPropertyName:

                    circle.StrokeColor = tkCircle.StrokeColor.ToAndroid().ToArgb();

                    break;
            }
        }

        /// <summary>
        ///     Adds a route to the map
        /// </summary>
        /// <param name="line">The route to add</param>
        private void AddLine(TKPolyline line)

        {
            line.PropertyChanged += OnLinePropertyChanged;


            var polylineOptions = new PolylineOptions();

            if (line.Color != Color.Default)

                polylineOptions.InvokeColor(line.Color.ToAndroid().ToArgb());

            if (line.LineWidth > 0)

                polylineOptions.InvokeWidth(line.LineWidth);


            if (line.LineCoordinates != null)

                polylineOptions.Add(line.LineCoordinates.Select(i => i.ToLatLng()).ToArray());


            _polylines.Add(line, _googleMap.AddPolyline(polylineOptions));
        }

        /// <summary>
        ///     Calculates and adds the route to the map
        /// </summary>
        /// <param name="route">The route to add</param>
        private async void AddRoute(TKRoute route)

        {
            if (route == null) return;


            _tempRouteList.Add(route);


            route.PropertyChanged += OnRoutePropertyChanged;


            GmsDirectionResult routeData = null;

            string errorMessage = null;


            routeData = await GmsDirection.Instance.CalculateRoute(route.Source, route.Destination,
                route.TravelMode.ToGmsTravelMode());


            if (FormsMap == null || Map == null || !_tempRouteList.Contains(route)) return;


            if (routeData != null && routeData.Routes != null)

                if (routeData.Status == GmsDirectionResultStatus.Ok)

                {
                    var r = routeData.Routes.FirstOrDefault();

                    if (r != null && r.Polyline.Positions != null && r.Polyline.Positions.Any())

                    {
                        SetRouteData(route, r);


                        var routeOptions = new PolylineOptions();


                        if (route.Color != Color.Default)

                            routeOptions.InvokeColor(route.Color.ToAndroid().ToArgb());

                        if (route.LineWidth > 0)

                            routeOptions.InvokeWidth(route.LineWidth);

                        routeOptions.Add(r.Polyline.Positions.Select(i => i.ToLatLng()).ToArray());


                        _routes.Add(route, _googleMap.AddPolyline(routeOptions));


                        MapFunctions.RaiseRouteCalculationFinished(route);
                    }

                    else

                    {
                        errorMessage = "Unexpected result";
                    }
                }

                else

                {
                    errorMessage = routeData.Status.ToString();
                }

            else

                errorMessage = "Could not connect to api";

            if (!string.IsNullOrEmpty(errorMessage))

            {
                var routeCalculationError = new TKRouteCalculationError(route, errorMessage);


                MapFunctions.RaiseRouteCalculationFailed(routeCalculationError);
            }
        }

        /// <summary>
        ///     Sets the route calculation data
        /// </summary>
        /// <param name="route">The PCL route</param>
        /// <param name="routeResult">The rourte api result</param>
        private void SetRouteData(TKRoute route, GmsRouteResult routeResult)

        {
            var latLngBounds = new LatLngBounds(
                new LatLng(routeResult.Bounds.SouthWest.Latitude, routeResult.Bounds.SouthWest.Longitude),
                new LatLng(routeResult.Bounds.NorthEast.Latitude, routeResult.Bounds.NorthEast.Longitude));


            var apiSteps = routeResult.Legs.First().Steps;

            var steps = new TKRouteStep[apiSteps.Count()];

            var routeFunctions = (IRouteFunctions) route;


            for (var i = 0; i < steps.Length; i++)

            {
                steps[i] = new TKRouteStep();

                var stepFunctions = (IRouteStepFunctions) steps[i];

                var apiStep = apiSteps.ElementAt(i);


                stepFunctions.SetDistance(apiStep.Distance.Value);

                stepFunctions.SetInstructions(apiStep.HtmlInstructions);
            }

            routeFunctions.SetSteps(steps);

            routeFunctions.SetDistance(routeResult.Legs.First().Distance.Value);

            routeFunctions.SetTravelTime(routeResult.Legs.First().Duration.Value);


            routeFunctions.SetBounds(
                MapSpan.FromCenterAndRadius(
                    latLngBounds.Center.ToPosition(),
                    Distance.FromKilometers(
                        new Position(latLngBounds.Southwest.Latitude, latLngBounds.Southwest.Longitude)
                            .DistanceTo(
                                new Position(latLngBounds.Northeast.Latitude, latLngBounds.Northeast.Longitude)) / 2)));

            routeFunctions.SetIsCalculated(true);
        }

        /// <summary>
        ///     Updates the image of a pin
        /// </summary>
        /// <param name="pin">The forms pin</param>
        /// <param name="markerOptions">The native marker options</param>
        private async Task UpdateImage(TKCustomMapPin pin, MarkerOptions markerOptions)

        {
            BitmapDescriptor bitmap;

            try

            {
                if (pin.Image != null)

                {
                    bitmap = BitmapDescriptorFactory.FromBitmap(await pin.Image.ToBitmap(Context));
                }

                else

                {
                    if (pin.DefaultPinColor != Color.Default)

                    {
                        var hue = pin.DefaultPinColor.ToAndroid().GetHue();

                        bitmap = BitmapDescriptorFactory.DefaultMarker(Math.Min(hue, 359.99f));
                    }

                    else

                    {
                        bitmap = BitmapDescriptorFactory.DefaultMarker();
                    }
                }
            }

            catch (Exception)

            {
                bitmap = BitmapDescriptorFactory.DefaultMarker();
            }

            markerOptions.SetIcon(bitmap);
        }

        /// <summary>
        ///     Updates the image on a marker
        /// </summary>
        /// <param name="pin">The forms pin</param>
        /// <param name="marker">The native marker</param>
        private async Task UpdateImage(TKCustomMapPin pin, Marker marker)

        {
            BitmapDescriptor bitmap;

            try

            {
                if (pin.Image != null)

                {
                    bitmap = BitmapDescriptorFactory.FromBitmap(await pin.Image.ToBitmap(Context));
                }

                else

                {
                    if (pin.DefaultPinColor != Color.Default)

                    {
                        var hue = pin.DefaultPinColor.ToAndroid().GetHue();

                        bitmap = BitmapDescriptorFactory.DefaultMarker(hue);
                    }

                    else

                    {
                        bitmap = BitmapDescriptorFactory.DefaultMarker();
                    }
                }
            }

            catch (Exception)

            {
                bitmap = BitmapDescriptorFactory.DefaultMarker();
            }

            marker.SetIcon(bitmap);
        }

        /// <summary>
        ///     Updates the custom tile provider
        /// </summary>
        private void UpdateTileOptions()

        {
            if (_tileOverlay != null)

            {
                _tileOverlay.Remove();

                _googleMap.MapType = GoogleMap.MapTypeNormal;
            }


            if (FormsMap == null || _googleMap == null) return;


            if (FormsMap.TilesUrlOptions != null)

            {
                _googleMap.MapType = GoogleMap.MapTypeNone;


                _tileOverlay = _googleMap.AddTileOverlay(
                    new TileOverlayOptions()
                        .InvokeTileProvider(
                            new TKCustomTileProvider(FormsMap.TilesUrlOptions))
                        .InvokeZIndex(-1));
            }
        }

        /// <summary>
        ///     Updates the visible map region
        /// </summary>
        private void UpdateMapRegion()

        {
            if (FormsMap == null) return;


            if (FormsMap.VisibleRegion != FormsMap.MapRegion)

                MoveToMapRegion(FormsMap.MapRegion, FormsMap.IsRegionChangeAnimated);
        }

        /// <summary>
        ///     Sets traffic enabled on the google map
        /// </summary>
        private void UpdateShowTraffic()

        {
            if (FormsMap == null || _googleMap == null) return;


            _googleMap.TrafficEnabled = FormsMap.ShowTraffic;
        }

        /// <summary>
        ///     Creates a <see cref="LatLngBounds" /> from a collection of <see cref="MapSpan" />
        /// </summary>
        /// <param name="spans">The spans to get calculate the bounds from</param>
        /// <returns>The bounds</returns>
        private LatLngBounds BoundsFromMapSpans(params MapSpan[] spans)

        {
            var builder = new LatLngBounds.Builder();


            foreach (var region in spans)

                builder
                    .Include(GmsSphericalUtil.ComputeOffset(region.Center, region.Radius.Meters, 0).ToLatLng())
                    .Include(GmsSphericalUtil.ComputeOffset(region.Center, region.Radius.Meters, 90).ToLatLng())
                    .Include(GmsSphericalUtil.ComputeOffset(region.Center, region.Radius.Meters, 180).ToLatLng())
                    .Include(GmsSphericalUtil.ComputeOffset(region.Center, region.Radius.Meters, 270).ToLatLng());

            return builder.Build();
        }

        /// <summary>
        ///     Unregisters all collections
        /// </summary>
        private void UnregisterCollections(TKCustomMap map)

        {
            UnregisterCollection(map.CustomPins, OnCustomPinsCollectionChanged, OnPinPropertyChanged);

            UnregisterCollection(map.Routes, OnRouteCollectionChanged, OnRoutePropertyChanged);

            UnregisterCollection(map.Polylines, OnLineCollectionChanged, OnLinePropertyChanged);

            UnregisterCollection(map.Circles, CirclesCollectionChanged, CirclePropertyChanged);

            UnregisterCollection(map.Polygons, OnPolygonsCollectionChanged, OnPolygonPropertyChanged);
        }

        /// <summary>
        ///     Unregisters one collection and all of its items
        /// </summary>
        /// <param name="collection">The collection to unregister</param>
        /// <param name="observableHandler">The <see cref="NotifyCollectionChangedEventHandler" /> of the collection</param>
        /// <param name="propertyHandler">The <see cref="PropertyChangedEventHandler" /> of the collection items</param>
        private void UnregisterCollection(
            IEnumerable collection,
            NotifyCollectionChangedEventHandler observableHandler,
            PropertyChangedEventHandler propertyHandler)

        {
            if (collection == null) return;


            var observable = collection as INotifyCollectionChanged;

            if (observable != null)

                observable.CollectionChanged -= observableHandler;

            foreach (INotifyPropertyChanged obj in collection)

                obj.PropertyChanged -= propertyHandler;
        }

        /// <summary>
        ///     Gets the <see cref="TKCustomMapPin" /> by the native <see cref="Marker" />
        /// </summary>
        /// <param name="marker">The marker to search the pin for</param>
        /// <returns>The forms pin</returns>
        protected TKCustomMapPin GetPinByMarker(Marker marker)

        {
            return _markers.SingleOrDefault(i => i.Value.Id == marker.Id).Key;
        }

        private void InvokeOnMapReadyBaseClassHack(GoogleMap googleMap)

        {
            MethodInfo onMapReadyMethodInfo = null;


            var baseType = typeof(MapRenderer);

            foreach (var currentMethod in baseType.GetMethods(BindingFlags.NonPublic |
                                                              BindingFlags.Instance |
                                                              BindingFlags.DeclaredOnly))

                if (currentMethod.IsFinal && currentMethod.IsPrivate)

                {
                    if (string.Equals(currentMethod.Name, "OnMapReady", StringComparison.Ordinal))

                    {
                        onMapReadyMethodInfo = currentMethod;


                        break;
                    }


                    if (currentMethod.Name.EndsWith(".OnMapReady", StringComparison.Ordinal))

                    {
                        onMapReadyMethodInfo = currentMethod;


                        break;
                    }
                }


            if (onMapReadyMethodInfo != null)

                onMapReadyMethodInfo.Invoke(this, new[] {googleMap});
        }
    }
}