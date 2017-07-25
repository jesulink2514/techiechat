using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace TechieFunctions
{
    public static class UpdateLocation
    {
        [FunctionName("UpdateLocation")]

        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            await PushNotification(new string[] { "" }, new Techie());

            //resp.IsSuccessStatusCode

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            name = name ?? data?.name;

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello " + name);
        }

        private static async Task<HttpResponseMessage> PushNotification(string[] players, Techie user)
        {
            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", "ZjYyZTkxMDQtMjE3My00MWVjLWFlODAtNGRmNGM2MmU3Yzhj");

            var not = new NotificationRequest()
            {
                app_id = "c5f9d14d-2cf2-4436-9df2-df379572a6cb",
                contents = new Contents() { en = user.Email },
                headings = new Contents() { en = user.Username },
                include_player_ids = players,
                small_icon = user.ProfileIcon,
                data = new Dictionary<string, object>()
                {
                    {"techie",JsonConvert.SerializeObject(user)}
                }
            };
            var payload = JsonConvert.SerializeObject(not);

            var resp = await httpClient.PostAsync("https://onesignal.com/api/v1/notifications",
                new StringContent(payload, Encoding.UTF8, "application/json"));

            return resp;
        }
    }

    public class NotificationRequest
    {
        public string app_id { get; set; }
        public string small_icon { get; set; }
        public IDictionary<string, object> data { get; set; }
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