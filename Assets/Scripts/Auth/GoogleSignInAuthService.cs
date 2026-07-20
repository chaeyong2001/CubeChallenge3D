using System.Threading.Tasks;
using UnityEngine;

namespace CubeChallenge3D.Auth
{
    public sealed class GoogleSignInAuthService
    {
        private const string ProviderName = "Google Account";

        public bool IsAvailable()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && CUBECHALLENGE_GOOGLE_SIGN_IN
            return true;
#else
            return false;
#endif
        }

        public Task<AccountLinkState> SignInAsync()
        {
            if (!IsAvailable())
            {
                return Task.FromResult(AccountLinkState.Unavailable(
                    ProviderName,
                    "Google Sign-In is not available in this build."));
            }

            Debug.LogWarning("[Auth] Google Sign-In SDK hook is not implemented yet.");
            return Task.FromResult(AccountLinkState.Unavailable(
                ProviderName,
                "Google Sign-In SDK setup is pending."));
        }
    }
}
