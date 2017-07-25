using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using DLToolkit.Forms.Controls;
using Plugin.Permissions;
using Techiechat.Data;
using Techiechat.Helpers;
using Xamarin.Forms;

namespace Techiechat.Droid
{
    [Activity(Label = "Techiechat", Icon = "@drawable/icon", Theme = "@style/splashscreen", 
        MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.Window.RequestFeature(WindowFeatures.ActionBar);
            base.SetTheme(Resource.Style.MainTheme);
            
            base.OnCreate(bundle);
            Acr.UserDialogs.UserDialogs.Init(this);
            Forms.Init(this, bundle);
            Xamarin.FormsMaps.Init(this, bundle);
            FlowListView.Init();

            DependencyService.Register<ITechiechatService,TechiechatService>();

            LoadApplication(new App());
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

