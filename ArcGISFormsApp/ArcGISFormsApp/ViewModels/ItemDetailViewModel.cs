using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGISFormsApp.Models;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI;

namespace ArcGISFormsApp.ViewModels
{
    public class ItemDetailViewModel : BaseViewModel
    {
        public Item Item { get; set; }
        public ItemDetailViewModel(Item item = null)
        {
            Title = item?.Text;
            Item = item;
        }

        public async Task<List<Graphic>> FindPlaces(MapPoint myLocation)
        {
            #region Search for places
            //  From .NET Developers Guide: Search for places topic
            //        https://developers.arcgis.com/net/latest/wpf/guide/search-for-places-geocoding-.htm
            // Finding geocode output attributes: https://developers.arcgis.com/rest/geocode/api-reference/geocoding-service-output.htm
            // ** Go to https://developers.arcgis.com/rest/ and then search "Geocode service" ... it'll be about the 5th match!
            // If letting the user select result output attributes, see "Search for places" topic and search the page for "Get locator info"

            List<Graphic> placeGraphics = new List<Graphic>();
            
            var geocodeServiceUrl = @"https://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer";
            LocatorTask geocodeTask = await LocatorTask.CreateAsync(new Uri(geocodeServiceUrl));

            // return if points of interest are not supported by this locator
            if (!geocodeTask.LocatorInfo.SupportsPoi) { return null; }

            // set geocode parameters to return a max of 10 candidates near the current location
            // var mapCenter = MyMapView.GetCurrentViewpoint(ViewpointType.CenterAndScale).TargetGeometry as MapPoint;
            var geocodeParams = new GeocodeParameters
            {
                MaxResults = 10,
                PreferredSearchLocation = myLocation // mapCenter
            };

            // request address, name, and url as result attributes 
            geocodeParams.ResultAttributeNames.Add("Place_addr");
            geocodeParams.ResultAttributeNames.Add("PlaceName");
            geocodeParams.ResultAttributeNames.Add("URL");

            // find candidates using a place category
            var matches = await geocodeTask.GeocodeAsync(Item.Text.ToLower(), geocodeParams);

            if (matches.Count == 0) { return null; }

            // ** end: search for places
            #endregion


            #region Create graphics from results

            foreach (var m in matches)
            {
                // Get the match attribute values
                string name = m.Attributes["PlaceName"].ToString();
                string address = m.Attributes["Place_addr"].ToString();
                string url = m.Attributes["URL"].ToString();

                // Create a graphic to represent this place, add the attribute values
                Graphic poi = new Graphic(m.DisplayLocation);
                poi.Attributes.Add("Address", address);
                poi.Attributes.Add("Name", name);
                poi.Attributes.Add("URL", url);

                // Add the place graphic to the collection
                placeGraphics.Add(poi);
            }

            return placeGraphics;

            // ** end: create graphics
            #endregion
        }

        public async Task<Graphic> RouteToPoiAsync(MapPoint toLocation, SpatialReference outSpatialReference)
        {
            // From .NET Developers Guide: Find a route topic
            //       https://developers.arcgis.com/net/latest/wpf/guide/find-a-route.htm

            // Use the San Diego route service
            var routeSourceUri = new Uri("https://sampleserver6.arcgisonline.com/arcgis/rest/services/NetworkAnalysis/SanDiego/NAServer/Route");
            var routeTask = await RouteTask.CreateAsync(routeSourceUri);

            // get the default route parameters
            var routeParams = await routeTask.CreateDefaultParametersAsync();

            // explicitly set values for some params
            routeParams.ReturnDirections = true;
            routeParams.ReturnRoutes = true;
            routeParams.OutputSpatialReference = outSpatialReference;

            // ---Call the function to get the "WALK" travel mode
            TravelMode walkMode = FindTravelMode(routeTask.RouteTaskInfo.TravelModes, "WALK");
            if (walkMode != null) { routeParams.TravelMode = walkMode; }
            // ---

            // create a Stop for my location
            var myLocation = Item.DeviceLocation;
            var stop1 = new Stop(myLocation);

            // create a Stop for destination
            var yourLocation = toLocation;
            var stop2 = new Stop(yourLocation);

            // assign the stops to the route parameters
            var stopPoints = new List<Stop> { stop1, stop2 };
            routeParams.SetStops(stopPoints);

            // solve the route
            var routeResult = await routeTask.SolveRouteAsync(routeParams);

            // get the first route from the results
            var route = routeResult.Routes.FirstOrDefault();// [0];
            if (route == null) { return null; }

            // create a graphic (with a dashed line symbol) to represent the route
            var routeSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.MediumPurple, 5);
            var routeGraphic = new Graphic(route.RouteGeometry, routeSymbol);

            return routeGraphic;
        }

        private TravelMode FindTravelMode(IReadOnlyList<TravelMode> travelModes, string mode)
        {
            // From the .NET samples page: search "TravelMode"
            //      https://developers.arcgis.com/net/latest/wpf/sample-code/offline-routing/

            TravelMode foundMode = null;
            foreach (var m in travelModes)
            {
                if (m.Type.ToLower() == mode.ToLower())
                {
                    foundMode = m;
                    break;
                }
            }

            return foundMode;
        }
    }
}
