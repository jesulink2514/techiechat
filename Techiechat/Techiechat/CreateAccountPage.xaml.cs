using System;
using Plugin.Geolocator;
using Xamarin.Auth;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Techiechat
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateAccountPage : ContentPage
    {
        public CreateAccountPage()
        {
            InitializeComponent();
        }

        private async void StartChat(object sender, EventArgs e)
        {
            var location = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(5));
            var account = new Account(UserName.Text);
            account.Properties.Add("Id",Guid.NewGuid().ToString());

            await AccountStore.Create().SaveAsync(account,"techiechat");
        }
    }
}