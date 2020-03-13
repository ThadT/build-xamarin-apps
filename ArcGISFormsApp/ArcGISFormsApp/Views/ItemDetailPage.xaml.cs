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

        // Used in Callout to see feature details in PopupViewer
        private RuntimeImage WalkIcon => new RuntimeImage(new Uri("http://static.arcgis.com/images/Symbols/NPS/npsPictograph_0231.png"));

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
            // Find places for this location
            List<Graphic> places = await _viewModel.FindPlaces(_viewModel.Item.DeviceLocation);

            // Create a symbol for the places (red circle), use it to create a renderer
            SimpleMarkerSymbol sym = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.Red, 10);
            SimpleRenderer rndrr = new SimpleRenderer(sym);

            // Create a graphics overlay to show the places using the renderer, add it to the map view
            _placesGraphicsOverlay = new GraphicsOverlay();
            _placesGraphicsOverlay.Renderer = rndrr;
            _placesGraphicsOverlay.Graphics.AddRange(places); 
            MyMapView.GraphicsOverlays.Add(_placesGraphicsOverlay);

            // Add the current location to the graphics overlay
            var locationGraphic = new Graphic(_viewModel.Item.DeviceLocation, new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.X, System.Drawing.Color.Blue, 16));
            _placesGraphicsOverlay.Graphics.Add(locationGraphic);

            // Add the route graphics overlay
            _routeGraphicsOverlay = new GraphicsOverlay();
            MyMapView.GraphicsOverlays.Add(_routeGraphicsOverlay);
        }

        async void MyMapView_GeoViewTapped(System.Object sender, Esri.ArcGISRuntime.Xamarin.Forms.GeoViewInputEventArgs e)
        {
            MyMapView.DismissCallout();
            GeoElement clickedElement = null;

            if(_viewModel.Item.Id == "favorites")
            {
                IdentifyLayerResult layerResult = await MyMapView.IdentifyLayerAsync(_viewModel.Item.FavoritesLayer, e.Position, 12, false);
                clickedElement = layerResult.GeoElements.FirstOrDefault();
            }
            else
            {
                IdentifyGraphicsOverlayResult idResult = await MyMapView.IdentifyGraphicsOverlayAsync(_placesGraphicsOverlay, e.Position, 12, false);
                clickedElement = idResult.Graphics.FirstOrDefault();
            }
            

            if (clickedElement != null)
            {
                string name = clickedElement.Attributes["Name"].ToString();
                string address = clickedElement.Attributes["Address"].ToString();
                CalloutDefinition definition = new CalloutDefinition(name, address);
                definition.Tag = clickedElement;
                definition.OnButtonClick = new Action<object>(async (tag) => 
                {
                    // This event receives the value assigned as the CalloutDefinition.Tag
                    GeoElement poiElement = tag as GeoElement;
                    if(poiElement == null) { return; }

                    // TODO: call a function in the viewmodel that will route to this location
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
    }
}