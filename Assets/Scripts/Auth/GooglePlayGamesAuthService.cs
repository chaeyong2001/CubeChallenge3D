using System;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace CubeChallenge3D.Auth
{
    public sealed class GooglePlayGamesAuthService
    {
        private const string ProviderName = "Google Play Games";
        private static bool initialized;

        public bool IsAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
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
                Debug.LogWarning("[GPGS] Manual sign-in unavailable: Google Play Games is only available on Android device builds.");
                return Task.FromResult(AccountLinkState.Unavailable(
                    ProviderName,
                    "Google Play Games is not available in this build."));
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            EnsureInitialized();

            TaskCompletionSource<AccountLinkState> completion = new TaskCompletionSource<AccountLinkState>();
            try
            {
                PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
                {
                    Debug.Log($"[GPGS] Manual sign-in callback status={status}");
                    if (status != SignInStatus.Success)
                    {
                        Debug.LogWarning($"[GPGS] Manual sign-in failed. status={status}");
                        Debug.LogWarning("[GPGS] Check testers allowlist / OAuth SHA-1 / package name / PGS publish state / Google Play Games app account");
                        completion.TrySetResult(new AccountLinkState
                        {
                            available = true,
                            success = false,
                            cancelled = status == SignInStatus.Canceled,
                            provider = ProviderName,
                            message = status.ToString()
                        });
                        return;
                    }

                    string playerId = PlayGamesPlatform.Instance.GetUserId();
                    string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
                    Debug.Log("[GPGS] Manual sign-in success");
                    Debug.Log($"[GPGS] PlayerId={MaskId(playerId)}");
                    Debug.Log($"[GPGS] DisplayName={displayName}");
                    completion.TrySetResult(new AccountLinkState
                    {
                        available = true,
                        success = true,
                        provider = ProviderName,
                        providerUserId = playerId,
                        displayName = displayName,
                        message = "Google Play Games sign-in succeeded."
                    });
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GPGS] Manual sign-in failed. exception={exception.GetType().Name} message={exception.Message}");
                Debug.LogWarning("[GPGS] Check testers allowlist / OAuth SHA-1 / package name / PGS publish state / Google Play Games app account");
                completion.TrySetResult(AccountLinkState.Unavailable(
                    ProviderName,
                    exception.Message));
            }

            return completion.Task;
#else
            return Task.FromResult(AccountLinkState.Unavailable(
                ProviderName,
                "Google Play Games is not available in this build."));
#endif
        }

        public Task<AccountLinkState> TrySilentSignInAsync()
        {
            Debug.Log("[GPGS] Silent sign-in start");
            if (!IsAvailable())
            {
                Debug.LogWarning("[GPGS] Silent sign-in failed status=Unavailable");
                return Task.FromResult(AccountLinkState.Unavailable(
                    ProviderName,
                    "Google Play Games is not available in this build."));
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            EnsureInitialized();

            TaskCompletionSource<AccountLinkState> completion = new TaskCompletionSource<AccountLinkState>();
            try
            {
                PlayGamesPlatform.Instance.Authenticate(status =>
                {
                    Debug.Log($"[GPGS] Silent sign-in callback status={status}");
                    if (status != SignInStatus.Success)
                    {
                        Debug.LogWarning($"[GPGS] Silent sign-in failed status={status}");
                        completion.TrySetResult(new AccountLinkState
                        {
                            available = true,
                            success = false,
                            cancelled = status == SignInStatus.Canceled,
                            provider = ProviderName,
                            message = status.ToString()
                        });
                        return;
                    }

                    string playerId = PlayGamesPlatform.Instance.GetUserId();
                    string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
                    Debug.Log($"[GPGS] Silent sign-in success playerId={MaskId(playerId)}");
                    completion.TrySetResult(new AccountLinkState
                    {
                        available = true,
                        success = true,
                        provider = ProviderName,
                        providerUserId = playerId,
                        displayName = displayName,
                        message = "Google Play Games silent sign-in succeeded."
                    });
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GPGS] Silent sign-in failed exception={exception.GetType().Name} message={exception.Message}");
                completion.TrySetResult(AccountLinkState.Unavailable(
                    ProviderName,
                    exception.Message));
            }

            return completion.Task;
#else
            return Task.FromResult(AccountLinkState.Unavailable(
                ProviderName,
                "Google Play Games is not available in this build."));
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            Debug.Log("[GPGS] Initialize start");
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();
            initialized = true;
            Debug.Log("[GPGS] Initialize complete");
        }
#endif

        private static string MaskId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "(empty)";
            }

            string trimmed = value.Trim();
            if (trimmed.Length <= 6)
            {
                return "***";
            }

            return $"{trimmed.Substring(0, 3)}***{trimmed.Substring(trimmed.Length - 3)}";
        }
    }
}
