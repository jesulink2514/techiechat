using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Techiechat.Helpers
{
    public interface ITechiechatService
    {
        Task<bool> RegisterAsync(Techie techie);
        Task InitAsync();
        List<Techie> GetUsers(Point point);
    }

    public class Techie
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string ProfileIcon{ get; set; }
        public Point LastLocation{ get; set; }
    }

    public class Point
    {
        public Point(double latitude,double longitude)
        {
            Coordinates = new[] {latitude, longitude};
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
