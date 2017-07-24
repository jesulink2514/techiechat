using System.Threading.Tasks;

namespace Techiechat.Helpers.AccountKit
{
    public interface IAccountManager
    {
        Task<TechiesLoginResult> LoginWithAccountKit(LoginType loginType, ResponseType responseType);

        Task<LoginAccount> GetCurrentAccount(ResponseType responseType);
    }

    public class TechiesLoginResult
    {
        public TechiesLoginResult(bool isSuccessful = false, bool isCancelled = false, string tokenOrCode = null)
        {
            IsSuccessful = isSuccessful;
            IsCancelled = isCancelled;
            TokenOrCode = tokenOrCode;
        }

        public bool IsSuccessful { get; private set; }

        public bool IsCancelled { get; private set; }

        public string TokenOrCode { get; private set; }
    }

    public class LoginAccount
    {
        public LoginAccount(bool hasError = false, string phoneNumber = null, string email = null)
        {
            HasError = hasError;
            PhoneNumber = phoneNumber;
            Email = email;
        }

        public string PhoneNumber { get; private set; }

        public string Email { get; private set; }

        public bool HasError { get; private set; }
    }

    public enum LoginType
    {
        Phone,
        Email
    }

    public enum ResponseType
    {
        AccessToken,
        AuthorizationCode
    }
}
