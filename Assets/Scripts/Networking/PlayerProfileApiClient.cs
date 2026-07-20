using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CubeChallenge3D.Networking
{
    public sealed class PlayerProfileApiClient
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;

        public PlayerProfileApiClient(string apiBaseUrl, int requestTimeoutSeconds)
        {
            baseUrl = NormalizeBaseUrl(apiBaseUrl);
            timeoutSeconds = Mathf.Max(1, requestTimeoutSeconds);

            if (Application.isMobilePlatform
                && (baseUrl.Contains("127.0.0.1") || baseUrl.Contains("localhost")))
            {
                Debug.LogWarning("[PlayerProfileApi] Android/iOS builds cannot reach a PC server through 127.0.0.1 or localhost. Use the PC LAN IP instead.");
            }
        }

        public bool HasServerUrl => !string.IsNullOrWhiteSpace(baseUrl);

        public async Task<NicknameCheckResult> CheckNicknameAsync(string nickname)
        {
            if (!HasServerUrl)
            {
                return NicknameCheckResult.Unavailable("Server URL is empty.");
            }

            string url = $"{baseUrl}/players/check-nickname?nickname={UnityWebRequest.EscapeURL(nickname ?? string.Empty)}";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = timeoutSeconds;
                await WaitForRequestAsync(request.SendWebRequest());

                if (HasConnectionError(request))
                {
                    return NicknameCheckResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    string message = ReadErrorMessage(request);
                    Debug.LogWarning($"[PlayerProfileApi] Nickname check failed. status={request.responseCode} error={message}");
                    return NicknameCheckResult.Unavailable(message);
                }

                string responseText = request.downloadHandler?.text;
                NicknameCheckResponseDto response = JsonUtility.FromJson<NicknameCheckResponseDto>(responseText);
                if (response == null || string.IsNullOrWhiteSpace(response.message) && !response.available && !response.valid)
                {
                    Debug.LogWarning($"[PlayerProfileApi] Invalid nickname check response. status={request.responseCode} body={responseText}");
                    return NicknameCheckResult.Unavailable("Invalid nickname check response.");
                }

                return new NicknameCheckResult
                {
                    requestSucceeded = true,
                    available = response.available,
                    valid = response.valid,
                    reason = response.reason,
                    message = response.message
                };
            }
        }

        public async Task<PlayerProfileCreateResult> CreateProfileAsync(string profileId, string nickname, int avatarId)
        {
            if (!HasServerUrl)
            {
                return PlayerProfileCreateResult.Unavailable("Server URL is empty.");
            }

            PlayerProfileCreateRequestDto payload = new PlayerProfileCreateRequestDto
            {
                profileId = profileId,
                nickname = nickname,
                avatarId = avatarId
            };
            string json = JsonUtility.ToJson(payload);
            byte[] body = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/players/create", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                await WaitForRequestAsync(request.SendWebRequest());

                if (HasConnectionError(request))
                {
                    return PlayerProfileCreateResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    return PlayerProfileCreateResult.Failed(ReadErrorMessage(request), request.responseCode);
                }

                PlayerProfileResponseDto response = JsonUtility.FromJson<PlayerProfileResponseDto>(request.downloadHandler.text);
                if (response == null || string.IsNullOrWhiteSpace(response.profileId))
                {
                    return PlayerProfileCreateResult.Unavailable("Invalid profile create response.");
                }

                return new PlayerProfileCreateResult
                {
                    requestSucceeded = true,
                    success = true,
                    profile = response,
                    message = "Profile created."
                };
            }
        }

        public async Task<PlayerProfileCreateResult> GetProfileAsync(string profileId)
        {
            if (!HasServerUrl)
            {
                return PlayerProfileCreateResult.Unavailable("Server URL is empty.");
            }

            string url = $"{baseUrl}/players/{UnityWebRequest.EscapeURL(profileId ?? string.Empty)}";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = timeoutSeconds;
                await WaitForRequestAsync(request.SendWebRequest());

                if (HasConnectionError(request))
                {
                    return PlayerProfileCreateResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    return PlayerProfileCreateResult.Failed(ReadErrorMessage(request), request.responseCode);
                }

                PlayerProfileResponseDto response = JsonUtility.FromJson<PlayerProfileResponseDto>(request.downloadHandler.text);
                return new PlayerProfileCreateResult
                {
                    requestSucceeded = true,
                    success = response != null && !string.IsNullOrWhiteSpace(response.profileId),
                    profile = response,
                    message = response != null ? "Profile loaded." : "Invalid profile response."
                };
            }
        }

        public async Task<PlayerProfileCreateResult> UpdateAvatarAsync(string profileId, int avatarId)
        {
            if (!HasServerUrl)
            {
                return PlayerProfileCreateResult.Unavailable("Server URL is empty.");
            }

            PlayerAvatarUpdateRequestDto payload = new PlayerAvatarUpdateRequestDto { avatarId = avatarId };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            string url = $"{baseUrl}/players/{UnityWebRequest.EscapeURL(profileId ?? string.Empty)}/avatar";
            using (UnityWebRequest request = new UnityWebRequest(url, "PATCH"))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                await WaitForRequestAsync(request.SendWebRequest());

                if (HasConnectionError(request))
                {
                    return PlayerProfileCreateResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    return PlayerProfileCreateResult.Failed(ReadErrorMessage(request), request.responseCode);
                }

                PlayerProfileResponseDto response = JsonUtility.FromJson<PlayerProfileResponseDto>(request.downloadHandler.text);
                return new PlayerProfileCreateResult
                {
                    requestSucceeded = true,
                    success = response != null && !string.IsNullOrWhiteSpace(response.profileId),
                    profile = response,
                    message = response != null ? "Avatar updated." : "Invalid profile response."
                };
            }
        }

        private static string NormalizeBaseUrl(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd('/');
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
                    ApiErrorResponseDto error = JsonUtility.FromJson<ApiErrorResponseDto>(text);
                    if (!string.IsNullOrWhiteSpace(error?.detail))
                    {
                        return error.detail;
                    }
                }
                catch (Exception)
                {
                    // Fall through to UnityWebRequest.error for non-JSON server responses.
                }
            }

            return string.IsNullOrWhiteSpace(request.error) ? "Server rejected profile request." : request.error;
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
    public sealed class NicknameCheckResult
    {
        public bool requestSucceeded;
        public bool available;
        public bool valid;
        public string reason;
        public string message;

        public bool IsUnavailable => !requestSucceeded;

        public static NicknameCheckResult Unavailable(string message)
        {
            return new NicknameCheckResult
            {
                requestSucceeded = false,
                available = false,
                valid = false,
                reason = "server_unavailable",
                message = message
            };
        }
    }

    [Serializable]
    public sealed class PlayerProfileCreateResult
    {
        public bool requestSucceeded;
        public bool success;
        public long statusCode;
        public string message;
        public PlayerProfileResponseDto profile;

        public bool IsUnavailable => !requestSucceeded;

        public static PlayerProfileCreateResult Unavailable(string message)
        {
            return new PlayerProfileCreateResult
            {
                requestSucceeded = false,
                success = false,
                statusCode = 0,
                message = message
            };
        }

        public static PlayerProfileCreateResult Failed(string message, long statusCode)
        {
            return new PlayerProfileCreateResult
            {
                requestSucceeded = true,
                success = false,
                statusCode = statusCode,
                message = message
            };
        }
    }

    [Serializable]
    public sealed class NicknameCheckResponseDto
    {
        public bool available;
        public bool valid;
        public string reason;
        public string message;
    }

    [Serializable]
    public sealed class PlayerProfileCreateRequestDto
    {
        public string profileId;
        public string nickname;
        public int avatarId;
    }

    [Serializable]
    public sealed class PlayerAvatarUpdateRequestDto
    {
        public int avatarId;
    }

    [Serializable]
    public sealed class PlayerProfileResponseDto
    {
        public string profileId;
        public string nickname;
        public int avatarId;
        public string createdAt;
        public string updatedAt;
        public bool linkedGooglePlay;
        public bool linkedGoogle;
    }

    [Serializable]
    public sealed class ApiErrorResponseDto
    {
        public string detail;
    }
}
