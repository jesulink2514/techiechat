using System;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Com.OneSignal;
using Com.OneSignal.Abstractions;
using Plugin.Geolocator;
using Techiechat.Helpers;
using Xamarin.Auth;
using Xamarin.Forms;

namespace Techiechat
{
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
            using (UserDialogs.Instance.Loading("Registering your account..."))
            {                                
                var id = await GetPushId();

                var location = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(5));

                //Send to the cloud
                var techie = new Techie()
                {
                    Id = id,
                    Username = UserName.Text,
                    Email = Email.Text,
                    ProfileIcon = ProfileIcon,
                    LastLocation = new Helpers.Point(location.Latitude, location.Longitude)
                };
                
                var resp = await App.TechiechatService.RegisterAsync(techie);

                if (!resp)
                {
                    UserDialogs.Instance.ShowError("Check if your info is correct and try again.");
                    return;
                }

                var account = new Account(UserName.Text);
                account.Properties.Add("Id", techie.Id);
                account.Properties.Add("Email", techie.Email);
                account.Properties.Add("ProfileIcon", techie.ProfileIcon);

                App.User = techie;

                await AccountStore.Create().SaveAsync(account, "techiechat");                
            }

            await Navigation.PushAsync(new MapChatPage());            
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

        private Task<string> GetPushId()
        {
            var taskCompletionSource = new TaskCompletionSource<string>();

            Action<string, string> action = (id, push) =>
            {
                taskCompletionSource.SetResult(id);
            };

            OneSignal.Current.IdsAvailable(new IdsAvailableCallback(action));

            return taskCompletionSource.Task;
        }
    }
}