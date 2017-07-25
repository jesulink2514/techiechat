using System;
using System.Linq;
using System.Threading.Tasks;
using Com.OneSignal;
using Com.OneSignal.Abstractions;
using MvvmHelpers;
using Plugin.Geolocator;
using Techiechat.Helpers;
using TK.CustomMap;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace Techiechat
{
    public partial class MapChatPage : IDisposable
    {
        public MapChatPage()
        {
            InitializeComponent();
            BindingContext = this;
            App.NewMessageReceveid+=AppOnNewMessageReceveid;
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
                    t.Position = new Position(p.Coordinates[0],p.Coordinates[1]);
                    t.Subtitle = techie.Email;
                    
                    MapControl.SelectedPin = t;
                }
            });
        }

        private void Current_PositionChanged(object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e)
        {
            //Send new position
            //e.Position
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

            CrossGeolocator.Current.PositionChanged += Current_PositionChanged;
            await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(5), 10);

            var position = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(5));
            var point = new Position(position.Latitude, position.Longitude);
            MapControl.MapCenter = point;
            MapControl.MoveToMapRegion(MapSpan.FromCenterAndRadius(point,Distance.FromKilometers(1)),true);

            //Get users in 5 Kms
            var users = App.TechiechatService.GetUsers(new Helpers.Point(point.Latitude, point.Longitude))
                .Select(x=> new TKCustomMapPin()
                {
                    Image = ImageSource.FromFile(x.ProfileIcon),
                    Title = x.Username,
                    Subtitle = x.Email,
                    ShowCallout = true,
                    Position = new Position(x.LastLocation.Coordinates[0],x.LastLocation.Coordinates[1]),
                    ID = x.Id
                }).ToList();

            Users.AddRange(users);
            
            IsLoaded = true;
        }

        private void OnNotificationReceived(OSNotification notification)
        {
            
        }

        private void OnNotificationOpened(OSNotificationOpenedResult result)
        {
            
        }
        
        public bool IsLoaded { get; set; }

        private void SendMessage(object sender, EventArgs e)
        {
            var message = Chat.Text;
            Chat.Text = string.Empty;
        }
        private static readonly object Lock = new object();
        private void OnLocationUpdate()
        {
            lock (Lock)
            {
                
            }
        }

        private void OnMessageReceived()
        {
            lock (Lock)
            {
                
            }
        }

        public ObservableRangeCollection<TKCustomMapPin> Users { get; set; } = new ObservableRangeCollection<TKCustomMapPin>();
        public void Dispose()
        {
            App.NewMessageReceveid -= AppOnNewMessageReceveid;
        }
    }
}