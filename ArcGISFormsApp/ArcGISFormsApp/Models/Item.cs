namespace ArcGISFormsApp.Models
{
    public class Item
    {
        public string Id { get; set; }
        public string PlaceType { get; set; }
        public string Description { get; set; }

        public Item(string id, string placeType, string description)
        {
            Id = id;
            PlaceType = placeType;
            Description = description;
        }
    }
}