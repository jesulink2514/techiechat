using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Com.OneSignal;
using Com.OneSignal.Abstractions;
using Newtonsoft.Json;
using Techiechat.Helpers;
using Xamarin.Auth;
using Xamarin.Forms;

namespace Techiechat
{
    public partial class App : Application
    {
        public static Techie User { get; set; }
        public static ITechiechatService TechiechatService { get; private set; }
        public App()
        {
            InitializeComponent();

            TechiechatService = DependencyService.Get<ITechiechatService>();

            var account = AccountStore.Create().FindAccountsForService("techiechat").FirstOrDefault();
            if (account != null)
            {
                MainPage = new NavigationPage(new MapChatPage());

                User = new Techie()
                {
                    Id = account.Properties["Id"],
                    Username = account.Username,
                    Email = account.Properties["Email"],
                    ProfileIcon = account.Properties["ProfileIcon"]
                };

                return;
            }
            MainPage = new NavigationPage(new Techiechat.MainPage());
        }

        protected override async void OnStart()
        {
            // Handle when your app starts
            await TechiechatService.InitAsync();

            OneSignal.Current.StartInit("c5f9d14d-2cf2-4436-9df2-df379572a6cb")
                .HandleNotificationOpened(OnNotificationOpened)
                .HandleNotificationReceived(OnNotificationReceived)
                .InFocusDisplaying(OSInFocusDisplayOption.None)
                .EndInit();

            //var account = new Account(UserName.Text);
            //account.Properties.Add("Id", techie.Id);
            //account.Properties.Add("Email", techie.Email);
            //account.Properties.Add("ProfileIcon", techie.ProfileIcon);
        }

        private void OnNotificationReceived(OSNotification notification)
        {
            HandleNotification(notification.payload.additionalData);
        }

        private void OnNotificationOpened(OSNotificationOpenedResult result)
        {
            HandleNotification(result.notification.payload.additionalData);
        }

        private void HandleNotification(Dictionary<string, object> payloadAdditionalData)
        {
            if (payloadAdditionalData.Any())
            {
                var raw = payloadAdditionalData["techie"].ToString();
                var techie = JsonConvert.DeserializeObject<Techie>(raw);
                NewMessageReceveid?.Invoke(this, techie);
            }
        }
        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        public static event EventHandler<Techie> NewMessageReceveid;
    }
}
