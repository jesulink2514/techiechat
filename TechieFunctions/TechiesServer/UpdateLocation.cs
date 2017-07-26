¡¡¡¡¡using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TechiesServer
{
    public static class UpdateLocation
    {
        [FunctionName("UpdateLocation")]

        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequestMessage req, TraceWriter log)
        {
            var client = new DocumentClient(new Uri("https://techies.documents.azure.com:443/"), "ptLkI7Ub5RH4UIuYWHsnher1QtCoemkMK1hZlH3cpLD4kBQCSg16R8SxO48e0ljjn3B4eBpdLU8XZw3oUJh6Dw==");

            var collection = UriFactory.CreateDocumentCollectionUri("techieschat", "users");

            var raw = await req.Content.ReadAsStringAsync();

            log.Info("RAW message" + raw);

            var techie = JsonConvert.DeserializeObject<Techie>(raw);
            var point = techie.LastLocation;

            var techieDB = client.CreateDocumentQuery<Techie>(collection,$"SELECT * FROM c WHERE c.id = \"{techie.Id}\"")                
                .ToList().FirstOrDefault();

            if (techieDB == null) return req.CreateResponse(HttpStatusCode.BadRequest);
            techieDB.LastLocation = point;

            var queryText = $"SELECT * FROM c WHERE ST_DISTANCE(c.LastLocation, {{\"type\":\"Point\",\"coordinates\":[{point.Coordinates[0]},{point.Coordinates[1]}]}}) < 5 * 1000";

            var users = client.CreateDocumentQuery<Techie>(collection, queryText, new FeedOptions() { MaxItemCount = 2000 }).ToList()
                .Select(x=>x.Id).ToArray();

            log.Info("Ids:" + users.Length);

            await Task.WhenAll(
                client.UpsertDocumentAsync(collection, techieDB),
                PushNotification(users,techie,log));            

            return req.CreateResponse(HttpStatusCode.OK, "Success");            
        }

        private static async Task<HttpResponseMessage> PushNotification(string[] players, Techie user, TraceWriter log)
        {
            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic","ZjYyZTkxMDQtMjE3My00MWVjLWFlODAtNGRmNGM2MmU3Yzhj");

            var andIcon = user.ProfileIcon.Replace(".png", string.Empty).ToLowerInvariant();

            var not = new NotificationRequest()
            {
                app_id = "c5f9d14d-2cf2-4436-9df2-df379572a6cb",
                contents = new Contents() { en = user.Email },
                headings = new Contents() { en = user.Username },
                include_player_ids = players,¡¡¡
                small_icon = andIcon,
                large_icon = andIcon,
                data = new Dictionary<string,string>()
                {
                    {"techie",JsonConvert.SerializeObject(user)}
                }
            };
            var payload = JsonConvert.SerializeObject(not);

            log.Info("OneSignal Payload:" + payload);

            var resp = await httpClient.PostAsync("https://onesignal.com/api/v1/notifications",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            return resp;
        }
    }

    public class NotificationRequest
    {
        public string app_id { get; set; }
        public string small_icon { get; set; }
        public string large_icon { get; set; }
        public IDictionary<string, string> data { get; set; }
        public Contents contents { get; set; }
        public Contents headings { get; set; }
        public string[] include_player_ids { get; set; }
    }

    public class Contents
    {
        public string en { get; set; }
    }

    public class Techie
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string ProfileIcon { get; set; }
        public Point LastLocation { get; set; }
    }

    public class Point
    {
        public Point(double latitude, double longitude)
        {
            Coordinates = new[] { latitude, longitude };
        }
        public Point()
        {
        }
        [JsonProperty("type")]
        public string Type { get; set; } = "Point";
        [JsonProperty("coordinates")]
        public double[] Coordinates { get; set; }
    }

}