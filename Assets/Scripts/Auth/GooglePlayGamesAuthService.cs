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
            Debug.Log("[GPGS] Manual sign-in start.");
            if (!IsAvailable())
            {
                Debug.LogWarning("[GPGS] Manual sign-in unavailable: Google Play Games SDK is not enabled for this build.");
                return Task.FromResult(AccountLinkState.Unavailable(
                    ProviderName,
                    "Google Play Games is not available in this build."));
            }

            Debug.LogWarning("[GPGS] Manual sign-in failed: Google Play Games SDK hook is not implemented yet.");
            return Task.FromResult(AccountLinkState.Unavailable(
                ProviderName,
                "Google Play Games SDK setup is pending."));
        }
    }
}
