using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CubeChallenge3D.Networking
{
    public sealed class AccountLinkApiClient
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;

        public AccountLinkApiClient(string apiBaseUrl, int requestTimeoutSeconds)
        {
            baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? string.Empty : apiBaseUrl.Trim().TrimEnd('/');
            timeoutSeconds = Mathf.Max(1, requestTimeoutSeconds);
        }

        public bool HasServerUrl => !string.IsNullOrWhiteSpace(baseUrl);

        public Task<AccountLinkResult> LinkGooglePlayAsync(string profileId, string googlePlayPlayerId, string displayName)
        {
            GooglePlayLinkRequestDto payload = new GooglePlayLinkRequestDto
            {
                googlePlayPlayerId = googlePlayPlayerId,
                displayName = displayName
            };
            return PostAsync(profileId, "link-google-play", payload);
        }

        public Task<AccountLinkResult> LinkGoogleAsync(string profileId, string googleAccountId, string googleEmailHash)
        {
            GoogleLinkRequestDto payload = new GoogleLinkRequestDto
            {
                googleAccountId = googleAccountId,
                googleEmailHash = googleEmailHash
            };
            return PostAsync(profileId, "link-google", payload);
        }

        public async Task<AccountLinkResult> GetAccountLinksAsync(string profileId)
        {
            if (!HasServerUrl)
            {
                return AccountLinkResult.Unavailable("Server URL is empty.");
            }

            string url = $"{baseUrl}/players/{UnityWebRequest.EscapeURL(profileId ?? string.Empty)}/account-links";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = timeoutSeconds;
                await WaitForRequestAsync(request.SendWebRequest());
                return BuildResult(request, "Account links loaded.");
            }
        }

        private async Task<AccountLinkResult> PostAsync<T>(string profileId, string path, T payload)
        {
            if (!HasServerUrl)
            {
                return AccountLinkResult.Unavailable("Server URL is empty.");
            }

            string url = $"{baseUrl}/players/{UnityWebRequest.EscapeURL(profileId ?? string.Empty)}/{path}";
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                await WaitForRequestAsync(request.SendWebRequest());
                return BuildResult(request, "Account linked.");
            }
        }

        private static AccountLinkResult BuildResult(UnityWebRequest request, string successMessage)
        {
            if (HasConnectionError(request))
            {
                return AccountLinkResult.Unavailable(request.error);
            }

            if (HasProtocolError(request))
            {
                return AccountLinkResult.Failed(ReadErrorMessage(request), request.responseCode);
            }

            AccountLinksResponseDto response = JsonUtility.FromJson<AccountLinksResponseDto>(request.downloadHandler.text);
            return new AccountLinkResult
            {
                requestSucceeded = true,
                success = response != null && !string.IsNullOrWhiteSpace(response.profileId),
                message = response != null ? successMessage : "Invalid account link response.",
                links = response
            };
        }

        private static bool HasConnectionError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.DataProcessingError;
#else
            return request.isNetworkError;
#endif
        }

        private static bool HasProtocolError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ProtocolError;
#else
            return request.isHttpError;
#endif
        }

        private static string ReadErrorMessage(UnityWebRequest request)
        {
            string text = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    AccountLinkErrorDto error = JsonUtility.FromJson<AccountLinkErrorDto>(text);
                    if (!string.IsNullOrWhiteSpace(error?.detail))
                    {
                        return error.detail;
                    }
                }
                catch (Exception)
                {
                    // Non-JSON server response. Use Unity's error below.
                }
            }

            return string.IsNullOrWhiteSpace(request.error) ? "Server rejected account link request." : request.error;
        }

        private static async Task WaitForRequestAsync(UnityWebRequestAsyncOperation operation)
        {
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }
    }

    [Serializable]
    public sealed class AccountLinkResult
    {
        public bool requestSucceeded;
        public bool success;
        public long statusCode;
        public string message;
        public AccountLinksResponseDto links;

        public static AccountLinkResult Unavailable(string message)
        {
            return new AccountLinkResult { message = message };
        }

        public static AccountLinkResult Failed(string message, long statusCode)
        {
            return new AccountLinkResult
            {
                requestSucceeded = true,
                success = false,
                statusCode = statusCode,
                message = message
            };
        }
    }

    [Serializable]
    public sealed class GooglePlayLinkRequestDto
    {
        public string googlePlayPlayerId;
        public string displayName;
    }

    [Serializable]
    public sealed class GoogleLinkRequestDto
    {
        public string googleAccountId;
        public string googleEmailHash;
    }

    [Serializable]
    public sealed class AccountLinksResponseDto
    {
        public string profileId;
        public bool googlePlayLinked;
        public bool googleLinked;
    }

    [Serializable]
    public sealed class AccountLinkErrorDto
    {
        public string detail;
    }
}
