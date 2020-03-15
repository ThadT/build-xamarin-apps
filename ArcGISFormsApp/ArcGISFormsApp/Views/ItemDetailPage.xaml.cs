using System;
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

        // An image for the callout button (find walking route)
        private RuntimeImage WalkIcon => new RuntimeImage(new Uri("https://static.arcgis.com/images/Symbols/NPS/npsPictograph_0231.png"));

        public ItemDetailPage(Item poiItem)
        {
            InitializeComponent();

            // Create a view model with the current item (place category)
            this._viewModel = new ItemDetailViewModel(poiItem);
            BindingContext = _viewModel;
            
            InitPlaces();
        }

        private async Task InitPlaces()
        {
            // Test compass control by setting rotation
            //await MyMapView.SetViewpointRotationAsync(90);

            // Find places in this category near the current location
            List<Graphic> places = await _viewModel.FindPlaces(_viewModel.Item.DeviceLocation);

            // Create a symbol for the places (red circle), use it to create a renderer
            SimpleMarkerSymbol sym = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 10);
            SimpleRenderer rndrr = new SimpleRenderer(sym);

            // Create a graphics overlay to show the places, add it to the map view
            _placesGraphicsOverlay = new GraphicsOverlay
            {
                Renderer = rndrr
            };

            // ** Better performance using AddRange **
            //    https://community.esri.com/community/developers/native-app-developers/arcgis-runtime-sdk-for-net/blog/2019/10/09/improve-performance-with-addrange
            _placesGraphicsOverlay.Graphics.AddRange(places);

            MyMapView.GraphicsOverlays.Add(_placesGraphicsOverlay);

            // Add the current location to the graphics overlay
            var locationGraphic = new Graphic(_viewModel.Item.DeviceLocation, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Blue, 12));
            _placesGraphicsOverlay.Graphics.Add(locationGraphic);

            // Add the route graphics overlay
            _routeGraphicsOverlay = new GraphicsOverlay();
            MyMapView.GraphicsOverlays.Add(_routeGraphicsOverlay);
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
                clickedElement.IsSelected = true;
                string name = clickedElement.Attributes["Name"].ToString();
                string address = clickedElement.Attributes["Address"].ToString();
                CalloutDefinition definition = new CalloutDefinition(name, address);
                definition.Tag = clickedElement;
                definition.OnButtonClick = new Action<object>(async (tag) =>
                {
                    // This event receives the value assigned as the CalloutDefinition.Tag
                    // ** Fix API ref for this!
                    // https://developers.arcgis.com/net/latest/wpf/api-reference/html/P_Esri_ArcGISRuntime_UI_CalloutDefinition_OnButtonClick.htm
                    GeoElement poiElement = tag as GeoElement;
                    if (poiElement == null) { return; }

                    // Call a function in the viewmodel that will route to this location
                    var routeGraphic = await _viewModel.RouteToPoiAsync(poiElement.Geometry as MapPoint, MyMapView.SpatialReference);

                    _routeGraphicsOverlay.Graphics.Clear();
                    // add the route graphic to the map view and zoom to its extent
                    _routeGraphicsOverlay.Graphics.Add(routeGraphic);
                    await MyMapView.SetViewpointGeometryAsync(routeGraphic.Geometry, 30);

                });
                definition.ButtonImage = WalkIcon;
                MyMapView.ShowCalloutAt(e.Location, definition);
            }
        }

        // Copied from Microsoft docs (Xamarin.Essentials)
        //      https://docs.microsoft.com/en-us/xamarin/essentials/open-browser
        private async Task OpenBrowser(Uri uri)
        {
            await Browser.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
        }

        private async void ShowWebsiteClicked(System.Object sender, System.EventArgs e)
        {
            // Get the selected place from the map (if any)
            Graphic selectedPlace = _placesGraphicsOverlay.SelectedGraphics.FirstOrDefault();
            if (selectedPlace == null) { return; }

            // Get the url from the selected place (if it has one)
            string url = selectedPlace.Attributes["URL"].ToString();
            if (string.IsNullOrEmpty(url)) { return; }

            // Create a Uri
            Uri placeUri = new Uri(url);

            // Call a function to open the URI in the default browser on the device
            await OpenBrowser(placeUri);
        }
    }
}