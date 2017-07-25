using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Techiechat.Helpers;

namespace Techiechat.Data
{
    public class TechiechatService: ITechiechatService
    {
        private const string Database = "techieschat";
        private const string Collection = "users";
        private readonly DocumentClient _client = new DocumentClient(
            new Uri("https://techies.documents.azure.com:443/"), 
            "ptLkI7Ub5RH4UIuYWHsnher1QtCoemkMK1hZlH3cpLD4kBQCSg16R8SxO48e0ljjn3B4eBpdLU8XZw3oUJh6Dw==");
        
        public async Task InitAsync()
        {
            await _client.CreateDatabaseIfNotExistsAsync(new Database(){Id = Database});

            await _client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(Database),
                new DocumentCollection{Id = Collection},new RequestOptions{ OfferThroughput = 400});
        }

        public async Task<bool> RegisterAsync(Techie user)
        {
            try
            {
                var coll = UriFactory.CreateDocumentCollectionUri(Database,Collection);

                //check if already exists
                var exists = _client.CreateDocumentQuery<Techie>(coll)
                    .Where(x => x.Id == user.Id || x.Username == user.Username)
                    .ToList().Any();

                if (exists) return false;

                //add
                await _client.CreateDocumentAsync(coll, user);

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }
        }

        public List<Techie> GetUsers(Point point)
        {
            var queryText = $"SELECT * FROM c WHERE ST_DISTANCE(c.LastLocation, {{\"type\":\"Point\",\"coordinates\":[{point.Coordinates[0]},{point.Coordinates[1]}]}}) < 5 * 1000";
            var collection = UriFactory.CreateDocumentCollectionUri(Database, Collection);
            var query = _client.CreateDocumentQuery<Techie>(collection,queryText,new FeedOptions()
            {
                MaxItemCount = 500
            });

            var items = query.ToList();

            return items;
        }
    }
}
