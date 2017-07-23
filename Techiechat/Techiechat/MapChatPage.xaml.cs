using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Techiechat
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MapChatPage : ContentPage
    {
        public MapChatPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        private void SendMessage(object sender, EventArgs e)
        {
            
        }
    }
}