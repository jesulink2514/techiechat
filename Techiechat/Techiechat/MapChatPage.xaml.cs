using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Com.OneSignal;
using Com.OneSignal.Abstractions;
using MvvmHelpers;
using Newtonsoft.Json;
using Plugin.Geolocator;
using Techiechat.Helpers;
using TK.CustomMap;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Point = Techiechat.Helpers.Point;

namespace Techiechat
{
    public partial class MapChatPage : IDisposable
    {
        private readonly HttpClient _httpClient;
        public MapChatPage()
        {
            InitializeComponent();
            BindingContext = this;
            App.NewMessageReceveid+=AppOnNewMessageReceveid;
            _httpClient = new HttpClient();
        }

        private void AppOnNewMessageReceveid(object sender, Techie techie)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                lock (Lock)
                {
                    MapControl.SelectedPin = null;

                    var p = techie.LastLocation;
                    var t = Users.FirstOrDefault(x => x.ID == techie.Id);

                    if (t == null)
                    {
                        t = new TKCustomMapPin()
                        {
                            ID = techie.Id,Image = techie.ProfileIcon,
                            Title = techie.Username
                        };
                        Users.Add(t);
                    }

                    t.Position = new Position(p.Coordinates[0],p.Coordinates[1]);
                    t.Subtitle = techie.Email;
                    
                    MapControl.SelectedPin = t;
                }
            });
        }

        private void Current_PositionChanged(object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e)
        {
            //Send new position
            var newposition = new Position(e.Position.Latitude, e.Position.Longitude);
            lock (Lock)
            {
                var user = Users.FirstOrDefault(x => x.ID == App.User.Id);
                if(user == null)return;
                user.Position = newposition;
            }            
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            CrossGeolocator.Current.PositionChanged -= Current_PositionChanged;
            await CrossGeolocator.Current.StopListeningAsync();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if(IsLoaded)return;
            IsLoaded = true;

            using (Acr.UserDialogs.UserDialogs.Instance.Loading("Getting nearby users around you..."))
            {
                CrossGeolocator.Current.PositionChanged += Current_PositionChanged;

                await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(5), 10);
                var position = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(5));

                var point = new Position(position.Latitude, position.Longitude);
                MapControl.MapCenter = point;
                MapControl.MoveToMapRegion(MapSpan.FromCenterAndRadius(point, Distance.FromKilometers(1)), true);

                //Get users in 5 Kms
                var users = App.TechiechatService.GetUsers(new Helpers.Point(point.Latitude, point.Longitude))
                    .Select(x => new TKCustomMapPin()
                    {
                        Image = ImageSource.FromFile(x.ProfileIcon),
                        Title = x.Username,
                        Subtitle = x.Email,
                        ShowCallout = true,
                        Position = new Position(x.LastLocation.Coordinates[0], x.LastLocation.Coordinates[1]),
                        ID = x.Id
                    }).ToList();

                Users.AddRange(users);
            }                        
        }
        
        public bool IsLoaded { get; set; }

        private async void SendMessage(object sender, EventArgs e)
        {
            var message = Chat.Text;
            Chat.Text = string.Empty;

            var position = await CrossGeolocator.Current.GetLastKnownLocationAsync();
            var request = App.User;
            request.LastLocation = new Helpers.Point(position.Latitude,position.Longitude);
            request.Email = message;

            var raw = JsonConvert.SerializeObject(request);

            await _httpClient.PostAsync("https://techiechat.azurewebsites.net/api/UpdateLocation?code=vRnvGMQUES8p7D/exn0sRG3IexwJGaFM5VCweBGiIHWj4xAMKFGR4A==",new StringContent(raw, Encoding.UTF8, "application/json"));
        }
        private static readonly object Lock = new object();

        public ObservableRangeCollection<TKCustomMapPin> Users { get; set; } = new ObservableRangeCollection<TKCustomMapPin>();
        public void Dispose()
        {
            App.NewMessageReceveid -= AppOnNewMessageReceveid;
        }
    }
}