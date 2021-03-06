﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ArcGISFormsApp.Models;
using ArcGISFormsApp.ViewModels;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ArcGISFormsApp.Views
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class ItemDetailPage : ContentPage
    {
        private ItemDetailViewModel _viewModel;
        private GraphicsOverlay _placesGraphicsOverlay;
        private GraphicsOverlay _routeGraphicsOverlay;
        private MapPoint _deviceLocation;

        // An image for the callout button (find walking route)
        private RuntimeImage WalkIcon => new RuntimeImage(new Uri("https://static.arcgis.com/images/Symbols/NPS/npsPictograph_0231.png"));

        public ItemDetailPage(Item poiItem)
        {
            InitializeComponent();

            // Create a view model with the current item (place category)
            this._viewModel = new ItemDetailViewModel(poiItem);
            BindingContext = _viewModel;

            // Create a symbol for the places (red circle), use it to create a renderer
            SimpleMarkerSymbol sym = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 10);
            SimpleRenderer rndrr = new SimpleRenderer(sym);

            // Create a graphics overlay to show the places, add it to the map view
            _placesGraphicsOverlay = new GraphicsOverlay
            {
                Renderer = rndrr
            };
            MyMapView.GraphicsOverlays.Add(_placesGraphicsOverlay);

            // Add the route graphics overlay
            _routeGraphicsOverlay = new GraphicsOverlay();
            MyMapView.GraphicsOverlays.Add(_routeGraphicsOverlay);

            // Zoom to the device location (Xamarin.Essentials)
            MyMapView.SpatialReferenceChanged += async (a, b) =>
            {
                GeolocationRequest locationRequest = new GeolocationRequest(GeolocationAccuracy.Default);
                Location myLocation = await Geolocation.GetLocationAsync(locationRequest);
                MapPoint deviceLocation = new MapPoint(myLocation.Longitude, myLocation.Latitude, SpatialReferences.Wgs84);
                await MyMapView.SetViewpointCenterAsync(deviceLocation, 24000);
            };

            // Use MapView.LocationDisplay to listen for location updates and for automatic display
            // When the location updates, store the last known location
            //MyMapView.LocationDisplay.IsEnabled = true;
            //MyMapView.LocationDisplay.LocationChanged += async(s, locArgs) =>
            //{
            //    _deviceLocation = locArgs.Position;
            //    await ShowPlaces();
            //};
            #region
            // Why is LocationDisplay null?
            //  Search GeoNet: https://community.esri.com/search.jspa?place=%2Fplaces%2F191827&q=locationdisplay
            //     -Answer from Zack (https://community.esri.com/message/875283-1006-locationdisplay-always-null)
            MyMapView.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == nameof(MyMapView.LocationDisplay) && MyMapView.LocationDisplay != null)
                {
                    // Enable your location display.
                    MyMapView.LocationDisplay.IsEnabled = true;
                    MyMapView.LocationDisplay.LocationChanged += async(s, locArgs) =>
                    {
                        _deviceLocation = locArgs.Position;
                        await ShowPlaces();
                    };
                }
            };
            #endregion

            // TODO: Uncomment to test compass control by setting map rotation
            //await MyMapView.SetViewpointRotationAsync(90);
        }

        private async Task ShowPlaces()
        {
            // Find places in this category near the current location
            List<Graphic> places = await _viewModel.FindPlaces(_deviceLocation);

            // ** Better performance using AddRange **
            //    https://community.esri.com/community/developers/native-app-developers/arcgis-runtime-sdk-for-net/blog/2019/10/09/improve-performance-with-addrange
            _placesGraphicsOverlay.Graphics.AddRange(places);
        }

        async void MyMapView_GeoViewTapped(System.Object sender, Esri.ArcGISRuntime.Xamarin.Forms.GeoViewInputEventArgs e)
        {
            // Clear any currently visible callouts, route graphics, or selections
            MyMapView.DismissCallout();
            _routeGraphicsOverlay.Graphics.Clear();
            _placesGraphicsOverlay.ClearSelection();

            // Get the place under the tap
            IdentifyGraphicsOverlayResult idResult = await MyMapView.IdentifyGraphicsOverlayAsync(_placesGraphicsOverlay, e.Position, 12, false);
            Graphic clickedElement = idResult.Graphics.FirstOrDefault();

            if (clickedElement != null)
            {
                // Select the place to highlight it; get name and address
                clickedElement.IsSelected = true;
                string name = clickedElement.Attributes["Name"].ToString();
                string address = clickedElement.Attributes["Address"].ToString();

                // Create a callout definition that shows the name and address for the place; set the element as a tag
                CalloutDefinition definition = new CalloutDefinition(name, address);
                definition.Tag = clickedElement;

                // Handle button clicks for the button on the callout
                // This event receives the value assigned as the CalloutDefinition.Tag
                // ** Fix API ref for this!
                // https://developers.arcgis.com/net/latest/wpf/api-reference/html/P_Esri_ArcGISRuntime_UI_CalloutDefinition_OnButtonClick.htm
                definition.OnButtonClick = new Action<object>(async (tag) =>
                {
                    // Get the geoelement that represents the place
                    GeoElement poiElement = tag as GeoElement;
                    if (poiElement == null) { return; }

                    // Call a function in the viewmodel that will route to this location
                    var routeGraphic = await _viewModel.RouteToPoiAsync(_deviceLocation, poiElement.Geometry as MapPoint, MyMapView.SpatialReference);

                    // Add the route graphic to the map view and zoom to its extent
                    _routeGraphicsOverlay.Graphics.Add(routeGraphic);
                    await MyMapView.SetViewpointGeometryAsync(routeGraphic.Geometry, 30);

                });

                // Set the button icon and show the callout at the click location
                definition.ButtonImage = WalkIcon;
                MyMapView.ShowCalloutAt(e.Location, definition);
            }
        }

        private void ShowWebsiteClicked(System.Object sender, System.EventArgs e)
        {
            // Get the selected place from the map (if any)
            Graphic selectedPlace = _placesGraphicsOverlay.SelectedGraphics.FirstOrDefault();
            if (selectedPlace == null) { return; }

            // Get the url from the selected place (if it has one)
            string url = selectedPlace.Attributes["URL"].ToString();
            if (string.IsNullOrEmpty(url)) { return; }

            // Create a Uri
            Uri placeUri = new Uri(url);

            // TODO: Call a function to open the URI in the default browser on the device
            
        }

        // TODO: Add a function that uses Xamarin.Essentials to open a web page
        #region
        // Copied from Microsoft docs (Xamarin.Essentials)
        //      https://docs.microsoft.com/en-us/xamarin/essentials/open-browser
        //private async Task OpenBrowser(Uri uri)
        //{
        //    await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        //}
        #endregion

    }
}