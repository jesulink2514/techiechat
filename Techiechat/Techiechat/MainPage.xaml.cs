using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Techiechat.Helpers.AccountKit;
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
            var accountManager = DependencyService.Get<IAccountManager>();
            var result = await accountManager.LoginWithAccountKit(LoginType.Phone, ResponseType.AccessToken);
            if (result.IsSuccessful)
            {
                await App.Current.MainPage.Navigation.PushAsync(new CreateAccountPage());   
            }
        }
    }
}
