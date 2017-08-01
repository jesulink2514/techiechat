using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Techiechat.Helpers;

namespace Techiechat.Data
{
    public class TechiechatService : ITechiechatService
    {
        private const string Database = "techieschat";
        private const string Collection = "users";
        private readonly DocumentClient _client = new DocumentClient(new Uri("https://{account}.documents.azure.com:443/"),
            "KEY HERE");

        public async Task InitAsync()
        {
            await _client.CreateDatabaseIfNotExistsAsync(new Database() { Id = Database });

            await _client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(Database),
                new DocumentCollection { Id = Collection , IndexingPolicy = new IndexingPolicy()
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true,
                    IncludedPaths = new Collection<IncludedPath>()
                    {
                        new IncludedPath
                        {
                            Path = "/*",
                            Indexes = new Collection<Index>()
                            {
                                new RangeIndex(DataType.Number),
                                new HashIndex(DataType.String,3),
                                new SpatialIndex(DataType.Point)
                            }
                        }
                    }
                }}, new RequestOptions
                {
                    OfferThroughput = 400                    
                });
        }

        public async Task<bool> RegisterAsync(Techie user)
        {
            try
            {
                var coll = UriFactory.CreateDocumentCollectionUri(Database, Collection);

                await _client.UpsertDocumentAsync(coll, user);

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
            var query = _client.CreateDocumentQuery<Techie>(collection, queryText, new FeedOptions()
            {
                MaxItemCount = 500
            });

            var items = query.ToList();

            return items;
        }
    }
}
