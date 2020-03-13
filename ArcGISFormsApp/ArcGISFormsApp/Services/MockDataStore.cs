using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGISFormsApp.Models;

namespace ArcGISFormsApp.Services
{
    public class MockDataStore : IDataStore<Item>
    {
        private List<Item> items;

        public MockDataStore()
        {
            var favoritesItem = new Item("favorites", "Favorite places", "These are my favorite places.");
            var iceCreamItem = new Item(Guid.NewGuid().ToString(), "Ice cream", "Find nearby ice cream or frozen yogurt.");
            var coffeeFavorite = new Item(Guid.NewGuid().ToString(), "Coffee", "Coffee shops in the area.");

            items = new List<Item>()
            {
                favoritesItem,
                iceCreamItem,
                coffeeFavorite
            };
        }

        public async Task<bool> AddItemAsync(Item item)
        {
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateItemAsync(Item item)
        {
            var oldItem = items.Where((Item arg) => arg.Id == item.Id).FirstOrDefault();
            items.Remove(oldItem);
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            var oldItem = items.Where((Item arg) => arg.Id == id).FirstOrDefault();
            items.Remove(oldItem);

            return await Task.FromResult(true);
        }

        public async Task<Item> GetItemAsync(string id)
        {
            return await Task.FromResult(items.FirstOrDefault(s => s.Id == id));
        }

        public async Task<IEnumerable<Item>> GetItemsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(items);
        }
    }
}