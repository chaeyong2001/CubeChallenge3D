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
            return await CreateProfileInternalAsync(profileId, nickname, avatarId, string.Empty);
        }

        public async Task<PlayerProfileCreateResult> CreateProfileWithGooglePlayAsync(
            string profileId,
            string nickname,
            int avatarId,
            string googlePlayGamesPlayerId)
        {
            return await CreateProfileInternalAsync(profileId, nickname, avatarId, googlePlayGamesPlayerId);
        }

        public async Task<GooglePlayProfileResolveResult> ResolveGooglePlayProfileAsync(string googlePlayGamesPlayerId)
        {
            if (!HasServerUrl)
            {
                return GooglePlayProfileResolveResult.Unavailable("Server URL is empty.");
            }

            GooglePlayProfileResolveRequestDto payload = new GooglePlayProfileResolveRequestDto
            {
                googlePlayGamesPlayerId = googlePlayGamesPlayerId
            };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/players/resolve-google-play", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                await WaitForRequestAsync(request.SendWebRequest());

                if (HasConnectionError(request))
                {
                    return GooglePlayProfileResolveResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    return GooglePlayProfileResolveResult.Failed(ReadErrorMessage(request), request.responseCode);
                }

                GooglePlayProfileResolveResponseDto response = JsonUtility.FromJson<GooglePlayProfileResolveResponseDto>(request.downloadHandler.text);
                if (response == null)
                {
                    return GooglePlayProfileResolveResult.Unavailable("Invalid Google Play profile resolve response.");
                }

                return new GooglePlayProfileResolveResult
                {
                    requestSucceeded = true,
                    success = true,
                    found = response.found,
                    profile = response.profile,
                    message = response.found ? "Google Play profile found." : "Google Play profile not found."
                };
            }
        }

        public async Task<GooglePlayProfileLinkResult> LinkGooglePlayAsync(
            string profileId,
            string googlePlayGamesPlayerId,
            string displayName)
        {
            if (!HasServerUrl)
            {
                return GooglePlayProfileLinkResult.Unavailable("Server URL is empty.");
            }

            GooglePlayProfileLinkRequestDto payload = new GooglePlayProfileLinkRequestDto
            {
                profileId = profileId,
                googlePlayGamesPlayerId = googlePlayGamesPlayerId,
                displayName = displayName
            };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/players/link-google-play", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                await WaitForRequestAsync(request.SendWebRequest());

                if (HasConnectionError(request))
                {
                    return GooglePlayProfileLinkResult.Unavailable(request.error);
                }

                if (HasProtocolError(request))
                {
                    return GooglePlayProfileLinkResult.Failed(ReadErrorMessage(request), request.responseCode);
                }

                GooglePlayProfileLinkResponseDto response = JsonUtility.FromJson<GooglePlayProfileLinkResponseDto>(request.downloadHandler.text);
                if (response == null || !response.success || response.profile == null)
                {
                    return GooglePlayProfileLinkResult.Failed(
                        response != null && !string.IsNullOrWhiteSpace(response.message)
                            ? response.message
                            : "Invalid Google Play link response.",
                        request.responseCode);
                }

                return new GooglePlayProfileLinkResult
                {
                    requestSucceeded = true,
                    success = true,
                    profile = response.profile,
                    message = string.IsNullOrWhiteSpace(response.message) ? "Google Play account linked." : response.message
                };
            }
        }

        private async Task<PlayerProfileCreateResult> CreateProfileInternalAsync(
            string profileId,
            string nickname,
            int avatarId,
            string googlePlayGamesPlayerId)
        {
            if (!HasServerUrl)
            {
                return PlayerProfileCreateResult.Unavailable("Server URL is empty.");
            }

            PlayerProfileCreateRequestDto payload = new PlayerProfileCreateRequestDto
            {
                profileId = profileId,
                nickname = nickname,
                avatarId = avatarId,
                googlePlayPlayerId = googlePlayGamesPlayerId
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
    public sealed class GooglePlayProfileResolveResult
    {
        public bool requestSucceeded;
        public bool success;
        public bool found;
        public long statusCode;
        public string message;
        public PlayerProfileResponseDto profile;

        public bool IsUnavailable => !requestSucceeded;

        public static GooglePlayProfileResolveResult Unavailable(string message)
        {
            return new GooglePlayProfileResolveResult
            {
                requestSucceeded = false,
                success = false,
                statusCode = 0,
                message = message
            };
        }

        public static GooglePlayProfileResolveResult Failed(string message, long statusCode)
        {
            return new GooglePlayProfileResolveResult
            {
                requestSucceeded = true,
                success = false,
                statusCode = statusCode,
                message = message
            };
        }
    }

    [Serializable]
    public sealed class GooglePlayProfileLinkResult
    {
        public bool requestSucceeded;
        public bool success;
        public long statusCode;
        public string message;
        public PlayerProfileResponseDto profile;

        public bool IsUnavailable => !requestSucceeded;

        public static GooglePlayProfileLinkResult Unavailable(string message)
        {
            return new GooglePlayProfileLinkResult
            {
                requestSucceeded = false,
                success = false,
                statusCode = 0,
                message = message
            };
        }

        public static GooglePlayProfileLinkResult Failed(string message, long statusCode)
        {
            return new GooglePlayProfileLinkResult
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
        public string googlePlayPlayerId;
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
        public string googlePlayPlayerId;
        public string googlePlayGamesPlayerId;
        public string createdAt;
        public string updatedAt;
        public bool linkedGooglePlay;
        public bool linkedGoogle;
    }

    [Serializable]
    public sealed class GooglePlayProfileResolveRequestDto
    {
        public string googlePlayGamesPlayerId;
    }

    [Serializable]
    public sealed class GooglePlayProfileResolveResponseDto
    {
        public bool found;
        public PlayerProfileResponseDto profile;
    }

    [Serializable]
    public sealed class GooglePlayProfileLinkRequestDto
    {
        public string profileId;
        public string googlePlayGamesPlayerId;
        public string displayName;
    }

    [Serializable]
    public sealed class GooglePlayProfileLinkResponseDto
    {
        public bool success;
        public PlayerProfileResponseDto profile;
        public string message;
    }

    [Serializable]
    public sealed class ApiErrorResponseDto
    {
        public string detail;
    }
}
