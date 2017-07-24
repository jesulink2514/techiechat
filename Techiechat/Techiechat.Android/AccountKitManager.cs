using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Com.Facebook.Accountkit;
using Com.Facebook.Accountkit.UI;
using Techiechat.Helpers.AccountKit;
using Xamarin.Forms;
using LoginType = Techiechat.Helpers.AccountKit.LoginType;

namespace Techiechat.Droid
{
    public class AccountKitManager: IAccountManager
    {
        public Task<LoginAccount> GetCurrentAccount(ResponseType responseType)
        {
            var taskCompletionSource = new TaskCompletionSource<LoginAccount>();

            AccountKit.GetCurrentAccount(new InnerAccountKitCallback(taskCompletionSource));

            return taskCompletionSource.Task;
        }

        public Task<TechiesLoginResult> LoginWithAccountKit(LoginType loginType,ResponseType responseType)
        {
            var taskCompletionSource = new TaskCompletionSource<TechiesLoginResult>();

            Action<IAccountKitLoginResult> onAKResult = (e) =>
            {
                if (e == null)
                {
                    taskCompletionSource.SetResult(new TechiesLoginResult(false,true));
                }
                else
                {
                    var tokenOrCode = responseType == ResponseType.AccessToken
                        ? e.AccessToken?.Token
                        : e.AuthorizationCode;
                    var result = new TechiesLoginResult(true,false,tokenOrCode);
                    taskCompletionSource.SetResult(result);
                }

                HiddenAccountKitActivity.Completed = null;
            };

            HiddenAccountKitActivity.Completed = onAKResult;

            var intent = new Intent(Forms.Context, typeof(HiddenAccountKitActivity));

            intent.PutExtra(nameof(ResponseType),(int)responseType);
            intent.PutExtra(nameof(LoginType),(int)loginType);

            Forms.Context.StartActivity(intent);

            return taskCompletionSource.Task;
        }

        class InnerAccountKitCallback : Java.Lang.Object, IAccountKitCallback
        {
            readonly TaskCompletionSource<LoginAccount> _taskCompleteionSource;

            public InnerAccountKitCallback(TaskCompletionSource<LoginAccount> taskCompleteionSource)
            {
                this._taskCompleteionSource = taskCompleteionSource;
            }

            public void OnError(AccountKitError p0)
            {
                //TODO return exception
                LoginAccount result = new LoginAccount(true);
                _taskCompleteionSource.SetResult(result);
            }

            public void OnSuccess(Java.Lang.Object p0)
            {
                var account = p0 as Account;
                _taskCompleteionSource.SetResult(new LoginAccount(false,account?.PhoneNumber?.RawPhoneNumber,account?.Email));
            }
        }
    }

    [Activity(Theme = "@style/AppLoginTheme")]
    public class HiddenAccountKitActivity : AppCompatActivity
    {
        const int AppRequestCode = 9999;
        internal static Action<IAccountKitLoginResult> Completed;

        protected override void OnCreate(Android.OS.Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var responseType = (ResponseType)Intent.Extras.GetInt(nameof(ResponseType));
            var loginType = (LoginType)Intent.Extras.GetInt(nameof(LoginType));

            var intent = new Intent(this, typeof(AccountKitActivity));
            var configurationBuilder = new AccountKitConfiguration.AccountKitConfigurationBuilder
                (loginType== LoginType.Email? 
                Com.Facebook.Accountkit.UI.LoginType.Email: Com.Facebook.Accountkit.UI.LoginType.Phone,
                responseType == ResponseType.AccessToken? 
                AccountKitActivity.ResponseType.Token: AccountKitActivity.ResponseType.Code);

            intent.PutExtra(AccountKitActivity.AccountKitActivityConfiguration,configurationBuilder.Build());

            StartActivityForResult(intent, AppRequestCode);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
        {
            if (requestCode != AppRequestCode)
            {
                base.OnActivityResult(requestCode, resultCode, data);

                Finish();
                return;
            }

            var result = data?.GetParcelableExtra(AccountKitLoginResult.ResultKey) as IAccountKitLoginResult;
            Completed?.Invoke(result);
            Finish();
        }
    }

    public class AccountKitLoginResultImpl:Java.Lang.Object,IAccountKitLoginResult
    {
        public int DescribeContents()
        {
         return 0;
        }

        public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
        {
        }

        public bool WasCancelled()
        {
            return false;
        }

        public AccessToken AccessToken { get; set; }
        public string AuthorizationCode { get; set; }
        public AccountKitError Error { get; set; }
        public string FinalAuthorizationState { get; set; }
        public long TokenRefreshIntervalInSeconds { get; set; }
    }
}
