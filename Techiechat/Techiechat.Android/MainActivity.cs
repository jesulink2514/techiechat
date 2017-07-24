using Android.App;
using Android.Content.PM;
using Android.OS;
using DLToolkit.Forms.Controls;
using Plugin.Permissions;
using Techiechat.Helpers.AccountKit;
using Xamarin.Forms;

namespace Techiechat.Droid
{
    [Activity(Label = "Techiechat", Icon = "@drawable/icon", Theme = "@style/MainTheme", 
        MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            DependencyService.Register<IAccountManager,AccountKitManager>();

            base.OnCreate(bundle);
            Acr.UserDialogs.UserDialogs.Init(this);
            global::Xamarin.Forms.Forms.Init(this, bundle);
            Xamarin.FormsMaps.Init(this, bundle);
            FlowListView.Init();
            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

