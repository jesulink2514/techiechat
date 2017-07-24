using System.Threading.Tasks;
using Acr.Support.iOS;
using Facebook.Accountkit;
using Foundation;
using Techiechat.Helpers.AccountKit;
using UIKit;

namespace Techiechat.iOS
{
    public class AccountKitManager: NSObject,IAccountManager, IAKFViewController
    {
        static AKFTheme theme;

        AKFAccountKit accountKit;
        UIViewController pendingLoginViewController;

        static AccountKitManager()
        {
            theme = AKFTheme.DefaultTheme;
        }

        public Task<LoginAccount> GetCurrentAccount(ResponseType responseType)
        {
            var taskCompletionSource = new TaskCompletionSource<LoginAccount>();

            InitAK(responseType == ResponseType.AccessToken
                ? AKFResponseType.AccessToken
                : AKFResponseType.AuthorizationCode);

            accountKit.RequestAccount((obj, error) =>
            {
                if (error != null)
                {
                    taskCompletionSource.SetResult(new LoginAccount(true));
                    return;
                }

                var account = obj as IAKFAccount;

                var phoneNumber = account?.PhoneNumber?.stringRepresentation()?.ToString();
                taskCompletionSource.SetResult(new LoginAccount(obj == null, phoneNumber, account?.EmailAddress));
            });

            return taskCompletionSource.Task;
        }

        public Task<TechiesLoginResult> LoginWithAccountKit(LoginType type, ResponseType responseType)
        {
            InitAK(responseType == ResponseType.AccessToken
                ? AKFResponseType.AccessToken
                : AKFResponseType.AuthorizationCode, true);

            loginTaskCompletionSource?.TrySetCanceled();
            loginTaskCompletionSource = new TaskCompletionSource<TechiesLoginResult>();
            pendingLoginViewController = type == Helpers.AccountKit.LoginType.Phone
                ? accountKit.ViewControllerForPhoneLogin()
                : accountKit.ViewControllerForEmailLogin();

            var loginViewController = (pendingLoginViewController as IAKFViewController);

            if (loginViewController != null)
            {
                loginViewController.WeakDelegate = this;
                loginViewController.EnableSendToFacebook = true;

                loginViewController.SetTheme(theme);
            }

            NSOperationQueue.MainQueue.BeginInvokeOnMainThread(() =>
            {
                var vcInContext = GetTopMostController();

                vcInContext.PresentViewController(pendingLoginViewController, true, null);
            });

            return loginTaskCompletionSource.Task;
        }

        UIViewController GetTopMostController()
        {
            var vc = UIApplication.SharedApplication.GetTopViewController();

            if (vc is AKFNavigationController)
            {
                vc = vc.PresentingViewController;
            }

            return vc;
        }

        void InitAK(AKFResponseType responseType, bool forced = false)
        {
            if (accountKit == null || forced)
            {
                accountKit = new AKFAccountKit(responseType);
            }
        }

        TaskCompletionSource<TechiesLoginResult> loginTaskCompletionSource;

        [Export("viewControllerDidCancel:")]
        public void DidCancel(UIViewController viewController)
        {
            loginTaskCompletionSource?.SetResult(new TechiesLoginResult(false, true));
            loginTaskCompletionSource = null;
        }

        [Export("viewController:didCompleteLoginWithAuthorizationCode:state:")]
        public void DidCompleteLoginWithAuthorizationCode(UIViewController viewController, string code, string state)
        {
            loginTaskCompletionSource?.SetResult(new TechiesLoginResult(true, false, code));
            loginTaskCompletionSource = null;
        }

        [Export("viewController:didCompleteLoginWithAccessToken:state:")]
        public void DidCompleteLoginWithAccessToken(UIViewController viewController, IAKFAccessToken accessToken, string state)
        {
            loginTaskCompletionSource?.SetResult(new TechiesLoginResult(true, false, accessToken.AccountId));
            loginTaskCompletionSource = null;
        }

        [Export("viewController:didFailWithError:")]
        public void DidFailWithError(UIViewController viewController, NSError error)
        {
            loginTaskCompletionSource?.SetResult(new TechiesLoginResult(false));
            loginTaskCompletionSource = null;
        }

        public string[] BlacklistedCountryCodes { get; set; }
        public string DefaultCountryCode { get; set; }
        public bool EnableSendToFacebook { get; set; }
        public string[] WhitelistedCountryCodes { get; set; }
        public void SetAdvancedUiManager(NSObject uiManager)
        {
        }

        public void SetTheme(AKFTheme theme)
        {
            
        }

        public NSObject UIManager { get; set; }
        public NSObject WeakDelegate { get; set; }
        public AKFLoginType LoginType { get; set; }
    }
}
