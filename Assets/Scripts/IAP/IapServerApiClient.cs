using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CubeChallenge3D.IAP
{
    public sealed class IapServerApiClient
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;

        public IapServerApiClient(string apiBaseUrl, int requestTimeoutSeconds)
        {
            baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? string.Empty : apiBaseUrl.Trim().TrimEnd('/');
            timeoutSeconds = Mathf.Max(1, requestTimeoutSeconds);
        }

        public bool HasServerUrl => !string.IsNullOrWhiteSpace(baseUrl);

        public async Task<IapVerifyResult> VerifyGooglePurchaseAsync(IapGoogleVerifyRequestDto payload)
        {
            if (!HasServerUrl)
            {
                return IapVerifyResult.Unavailable("Server URL is empty.");
            }

            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/iap/google/verify", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                await WaitForRequestAsync(request.SendWebRequest());

                if (HasConnectionError(request))
                {
                    return IapVerifyResult.Unavailable(request.error);
                }

                IapGoogleVerifyResponseDto response = null;
                try
                {
                    response = JsonUtility.FromJson<IapGoogleVerifyResponseDto>(request.downloadHandler.text);
                }
                catch (Exception)
                {
                    // Fall through to generic request error.
                }

                if (HasProtocolError(request))
                {
                    return IapVerifyResult.Failed(
                        response != null && !string.IsNullOrWhiteSpace(response.message)
                            ? response.message
                            : request.error,
                        request.responseCode,
                        response?.errorCode);
                }

                if (response == null)
                {
                    return IapVerifyResult.Failed("Invalid IAP verification response.", request.responseCode, "INVALID_RESPONSE");
                }

                return new IapVerifyResult
                {
                    requestSucceeded = true,
                    success = response.success,
                    statusCode = request.responseCode,
                    message = response.message,
                    errorCode = response.errorCode,
                    response = response
                };
            }
        }

        public async Task<IapEntitlementsResult> GetEntitlementsAsync(string profileId)
        {
            if (!HasServerUrl)
            {
                return IapEntitlementsResult.Unavailable("Server URL is empty.");
            }

            string url = $"{baseUrl}/iap/entitlements?profileId={UnityWebRequest.EscapeURL(profileId ?? string.Empty)}";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = timeoutSeconds;
                await WaitForRequestAsync(request.SendWebRequest());
                if (HasConnectionError(request))
                {
                    return IapEntitlementsResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    return IapEntitlementsResult.Failed(request.error, request.responseCode);
                }

                IapProfileStateDto response = JsonUtility.FromJson<IapProfileStateDto>(request.downloadHandler.text);
                return new IapEntitlementsResult
                {
                    requestSucceeded = true,
                    success = response != null && !string.IsNullOrWhiteSpace(response.profileId),
                    statusCode = request.responseCode,
                    message = response != null ? "Entitlements loaded." : "Invalid entitlement response.",
                    profile = response
                };
            }
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

        private static async Task WaitForRequestAsync(UnityWebRequestAsyncOperation operation)
        {
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }
    }

    [Serializable]
    public sealed class IapGoogleVerifyRequestDto
    {
        public string profileId;
        public string productId;
        public string purchaseToken;
        public string orderId;
        public string packageName;
    }

    [Serializable]
    public sealed class IapGoogleVerifyResponseDto
    {
        public bool success;
        public bool alreadyGranted;
        public IapProfileStateDto profile;
        public IapPurchaseDto purchase;
        public string errorCode;
        public string message;
    }

    [Serializable]
    public sealed class IapProfileStateDto
    {
        public string profileId;
        public int gems;
        public int coins;
        public bool removeAdsPurchased;
        public int refundDebtGems;
    }

    [Serializable]
    public sealed class IapPurchaseDto
    {
        public string productId;
        public string status;
        public string productType;
        public string grantedCurrencyType;
        public int grantedAmount;
    }

    public sealed class IapVerifyResult
    {
        public bool requestSucceeded;
        public bool success;
        public long statusCode;
        public string message;
        public string errorCode;
        public IapGoogleVerifyResponseDto response;

        public static IapVerifyResult Unavailable(string message)
        {
            return new IapVerifyResult { requestSucceeded = false, success = false, message = message };
        }

        public static IapVerifyResult Failed(string message, long statusCode, string errorCode)
        {
            return new IapVerifyResult
            {
                requestSucceeded = true,
                success = false,
                statusCode = statusCode,
                message = message,
                errorCode = errorCode
            };
        }
    }

    public sealed class IapEntitlementsResult
    {
        public bool requestSucceeded;
        public bool success;
        public long statusCode;
        public string message;
        public IapProfileStateDto profile;

        public static IapEntitlementsResult Unavailable(string message)
        {
            return new IapEntitlementsResult { requestSucceeded = false, success = false, message = message };
        }

        public static IapEntitlementsResult Failed(string message, long statusCode)
        {
            return new IapEntitlementsResult { requestSucceeded = true, success = false, statusCode = statusCode, message = message };
        }
    }
}
