using System;
using System.Collections.ObjectModel;
using System.Linq;
using Plugin.Geolocator;
using TK.CustomMap;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using Xamarin.Forms.Xaml;

namespace Techiechat
{
    public partial class MapChatPage : ContentPage
    {
        public MapChatPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var position = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(5));
            var point = new Position(position.Latitude, position.Longitude);
            var pin = new TKCustomMapPin()
            {
                Image = "http://www.gravatar.com/avatar/63359672e0ecb75e7ed261a358bf0478.jpg",
                Title = "jesulink",
                Subtitle = "jesus.angulo@outlook.com",
                Position = point,
                ShowCallout = true
            };
            Users.Add(pin);

            Map.MapCenter = point;
        }

        private void SendMessage(object sender, EventArgs e)
        {
            var pin = Map.CustomPins.First();
            pin.Subtitle = Chat.Text;
            Map.SelectedPin = null;
            Map.SelectedPin = pin;
            Chat.Text = string.Empty;
        }

        public ObservableCollection<TKCustomMapPin> Users { get; set; } = new ObservableCollection<TKCustomMapPin>();
    }
}