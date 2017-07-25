using System;
using Xamarin.Forms;

namespace Techiechat
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void Button_OnClicked(object sender, EventArgs e)
        {
            await App.Current.MainPage.Navigation.PushAsync(new CreateAccountPage());
        }
    }
}
