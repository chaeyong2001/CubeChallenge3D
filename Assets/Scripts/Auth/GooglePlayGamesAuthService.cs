using System.Threading.Tasks;
using UnityEngine;

namespace CubeChallenge3D.Auth
{
    public sealed class GooglePlayGamesAuthService
    {
        private const string ProviderName = "Google Play Games";

        public bool IsAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR && CUBECHALLENGE_GOOGLE_PLAY_GAMES
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
                    "Google Play Games is not available in this build."));
            }

            Debug.LogWarning("[Auth] Google Play Games SDK hook is not implemented yet.");
            return Task.FromResult(AccountLinkState.Unavailable(
                ProviderName,
                "Google Play Games SDK setup is pending."));
        }
    }
}
