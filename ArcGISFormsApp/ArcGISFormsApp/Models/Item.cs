using System;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Xamarin.Essentials;
using Map = Esri.ArcGISRuntime.Mapping.Map;

namespace ArcGISFormsApp.Models
{
    public class Item
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
        public MapPoint DeviceLocation { get; set; }
        public Map Map { get; set; }
        public FeatureLayer FavoritesLayer { get; set; }


        public Item(string id, string placeType, string description)
        {
            Id = id;
            Text = placeType;
            Description = description;

            GetLocationAndMap();
        }

        private async Task GetLocationAndMap()
        {
            // Get the device location
            GeolocationRequest locationRequest = new GeolocationRequest(GeolocationAccuracy.Default);
            Location myLocation = await Geolocation.GetLocationAsync(locationRequest);
            DeviceLocation = new MapPoint(myLocation.Longitude, myLocation.Latitude, SpatialReferences.Wgs84);

            // Let's fake a location in San Diego (where we have a routing service!)
            DeviceLocation = new MapPoint(-117.160258, 32.707366, SpatialReferences.Wgs84);

            // Create a map for the location
            Map map = new Map(Basemap.CreateStreets());

            // Get the hosted layer that contains favorite locations
            FeatureLayer favoritesLayer = new FeatureLayer(new Uri("https://services2.arcgis.com/ZQgQTuoyBrtmoGdP/arcgis/rest/services/Favorite_places/FeatureServer/0"));

            FavoritesLayer = favoritesLayer;

            // If this is the favorites item, add the favorites layer to the map
            if (Id == "favorites")
            {
                map.OperationalLayers.Add(favoritesLayer);
            }

            map.InitialViewpoint = new Viewpoint(DeviceLocation, 24000);
            await map.LoadAsync();
            Map = map;
        }

    }
}