using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CubeChallenge3D.Cloud
{
    public sealed class CloudSyncApiClient
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;

        public CloudSyncApiClient(string apiBaseUrl, int requestTimeoutSeconds)
        {
            baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? string.Empty : apiBaseUrl.Trim().TrimEnd('/');
            timeoutSeconds = Mathf.Max(1, requestTimeoutSeconds);
        }

        public bool HasServerUrl => !string.IsNullOrWhiteSpace(baseUrl);

        public async Task<CloudSaveStatusResult> GetStatusAsync(string profileId)
        {
            if (!HasServerUrl)
            {
                return CloudSaveStatusResult.Unavailable("Server URL is empty.");
            }

            string url = $"{baseUrl}/players/{UnityWebRequest.EscapeURL(profileId ?? string.Empty)}/cloud-save/status";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = timeoutSeconds;
                await WaitForRequestAsync(request.SendWebRequest());
                if (HasConnectionError(request))
                {
                    return CloudSaveStatusResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    return CloudSaveStatusResult.Failed(ReadErrorMessage(request), request.responseCode);
                }

                CloudSaveStatusDto dto = JsonUtility.FromJson<CloudSaveStatusDto>(request.downloadHandler.text);
                return new CloudSaveStatusResult
                {
                    requestSucceeded = true,
                    success = dto != null && !string.IsNullOrWhiteSpace(dto.profileId),
                    message = "Cloud status loaded.",
                    status = dto
                };
            }
        }

        public async Task<CloudSaveUploadResult> UploadAsync(
            string profileId,
            CloudSavePayload payload,
            string deviceIdHash,
            string appVersion)
        {
            if (!HasServerUrl)
            {
                return CloudSaveUploadResult.Unavailable("Server URL is empty.");
            }

            CloudSaveUploadRequestDto dto = new CloudSaveUploadRequestDto
            {
                saveVersion = payload?.saveVersion ?? 1,
                payload = payload,
                clientUpdatedAtUtc = payload?.clientUpdatedAtUtc ?? DateTime.UtcNow.ToString("o"),
                deviceIdHash = deviceIdHash ?? string.Empty,
                appVersion = appVersion ?? string.Empty
            };

            string url = $"{baseUrl}/players/{UnityWebRequest.EscapeURL(profileId ?? string.Empty)}/cloud-save";
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(dto));
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                await WaitForRequestAsync(request.SendWebRequest());
                if (HasConnectionError(request))
                {
                    return CloudSaveUploadResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    return CloudSaveUploadResult.Failed(ReadErrorMessage(request), request.responseCode);
                }

                CloudSaveUploadResponseDto response = JsonUtility.FromJson<CloudSaveUploadResponseDto>(request.downloadHandler.text);
                return new CloudSaveUploadResult
                {
                    requestSucceeded = true,
                    success = response != null && response.success,
                    message = response != null && response.success ? "Cloud save uploaded." : "Invalid cloud upload response.",
                    response = response
                };
            }
        }

        public async Task<CloudSaveDownloadResult> DownloadAsync(string profileId)
        {
            if (!HasServerUrl)
            {
                return CloudSaveDownloadResult.Unavailable("Server URL is empty.");
            }

            string url = $"{baseUrl}/players/{UnityWebRequest.EscapeURL(profileId ?? string.Empty)}/cloud-save";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = timeoutSeconds;
                await WaitForRequestAsync(request.SendWebRequest());
                if (HasConnectionError(request))
                {
                    return CloudSaveDownloadResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    return CloudSaveDownloadResult.Failed(ReadErrorMessage(request), request.responseCode);
                }

                CloudSaveDownloadResponseDto response = JsonUtility.FromJson<CloudSaveDownloadResponseDto>(request.downloadHandler.text);
                return new CloudSaveDownloadResult
                {
                    requestSucceeded = true,
                    success = response != null && response.payload != null,
                    message = response != null ? "Cloud save downloaded." : "Invalid cloud download response.",
                    response = response
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

        private static string ReadErrorMessage(UnityWebRequest request)
        {
            string text = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    CloudSaveErrorDto error = JsonUtility.FromJson<CloudSaveErrorDto>(text);
                    if (!string.IsNullOrWhiteSpace(error?.detail))
                    {
                        return error.detail;
                    }
                }
                catch (Exception)
                {
                    // Non-JSON server response. Fall back to Unity's error below.
                }
            }

            return string.IsNullOrWhiteSpace(request.error) ? "Cloud sync request failed." : request.error;
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
    public sealed class CloudSaveUploadRequestDto
    {
        public int saveVersion;
        public CloudSavePayload payload;
        public string clientUpdatedAtUtc;
        public string deviceIdHash;
        public string appVersion;
    }

    [Serializable]
    public sealed class CloudSaveStatusDto
    {
        public string profileId;
        public bool exists;
        public int saveVersion;
        public string serverUpdatedAt;
        public string payloadHash;
        public bool googlePlayLinked;
        public bool googleLinked;
    }

    [Serializable]
    public sealed class CloudSaveUploadResponseDto
    {
        public bool success;
        public string profileId;
        public string serverUpdatedAt;
        public int saveVersion;
        public string payloadHash;
        public bool overwritten;
    }

    [Serializable]
    public sealed class CloudSaveDownloadResponseDto
    {
        public string profileId;
        public int saveVersion;
        public CloudSavePayload payload;
        public string serverUpdatedAt;
        public string payloadHash;
    }

    [Serializable]
    public sealed class CloudSaveErrorDto
    {
        public string detail;
    }

    public class CloudSaveResult
    {
        public bool requestSucceeded;
        public bool success;
        public long statusCode;
        public string message;
    }

    public sealed class CloudSaveStatusResult : CloudSaveResult
    {
        public CloudSaveStatusDto status;

        public static CloudSaveStatusResult Unavailable(string message)
        {
            return new CloudSaveStatusResult { message = message };
        }

        public static CloudSaveStatusResult Failed(string message, long statusCode)
        {
            return new CloudSaveStatusResult { requestSucceeded = true, statusCode = statusCode, message = message };
        }
    }

    public sealed class CloudSaveUploadResult : CloudSaveResult
    {
        public CloudSaveUploadResponseDto response;

        public static CloudSaveUploadResult Unavailable(string message)
        {
            return new CloudSaveUploadResult { message = message };
        }

        public static CloudSaveUploadResult Failed(string message, long statusCode)
        {
            return new CloudSaveUploadResult { requestSucceeded = true, statusCode = statusCode, message = message };
        }
    }

    public sealed class CloudSaveDownloadResult : CloudSaveResult
    {
        public CloudSaveDownloadResponseDto response;

        public static CloudSaveDownloadResult Unavailable(string message)
        {
            return new CloudSaveDownloadResult { message = message };
        }

        public static CloudSaveDownloadResult Failed(string message, long statusCode)
        {
            return new CloudSaveDownloadResult { requestSucceeded = true, statusCode = statusCode, message = message };
        }
    }
}
