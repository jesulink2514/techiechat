using System;
using System.ComponentModel;
using Com.OneSignal;
using Plugin.Geolocator;
using Xamarin.Auth;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Techiechat
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateAccountPage
    {
        private string _profileIcon = Icons.Profiles[0];

        public CreateAccountPage()
        {
            InitializeComponent();
            this.BindingContext = this;
        }

        private async void StartChat(object sender, EventArgs e)
        {
            var location = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(5));
            var account = new Account(UserName.Text);
            account.Properties.Add("Id",Guid.NewGuid().ToString());

            await AccountStore.Create().SaveAsync(account,"techiechat");
        }

        public string[] ProfileIcons { get; private set; } = Icons.Profiles;

        public string ProfileIcon
        {
            get { return _profileIcon; }
            set
            {
                _profileIcon = value;
                OnPropertyChanged();
            }
        }

        private void FlowListView_OnFlowItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e?.Item == null) return;

            ProfileIcon = e.Item.ToString();
        }
    }
}