using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CubeChallenge3D.Networking
{
    public sealed class StageProgressRecordsApiClient
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;

        public StageProgressRecordsApiClient(string apiBaseUrl, int requestTimeoutSeconds)
        {
            baseUrl = NormalizeBaseUrl(apiBaseUrl);
            timeoutSeconds = Mathf.Max(1, requestTimeoutSeconds);
        }

        public bool HasServerUrl => !string.IsNullOrWhiteSpace(baseUrl);

        public async Task<StageProgressRecordsResult> SubmitAsync(StageProgressRecordSubmitDto payload)
        {
            if (!HasServerUrl)
            {
                return StageProgressRecordsResult.Unavailable("Server URL is empty.");
            }

            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/stage-records/submit", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                await WaitForRequestAsync(request.SendWebRequest());

                if (HasRequestError(request))
                {
                    return StageProgressRecordsResult.Unavailable(request.error);
                }

                StageProgressSubmitResponseDto response = JsonUtility.FromJson<StageProgressSubmitResponseDto>(request.downloadHandler.text);
                return new StageProgressRecordsResult
                {
                    success = response != null && response.success,
                    message = response != null ? response.message : "Invalid submit response.",
                    records = response?.record != null
                        ? new List<StageProgressRecordDto> { response.record }
                        : new List<StageProgressRecordDto>()
                };
            }
        }

        public async Task<StageProgressRecordsResult> GetLeaderboardAsync(string mode, int limit)
        {
            if (!HasServerUrl)
            {
                return StageProgressRecordsResult.Unavailable("Server URL is empty.");
            }

            string url = $"{baseUrl}/stage-records/leaderboard?mode={UnityWebRequest.EscapeURL(mode ?? string.Empty)}&limit={Mathf.Clamp(limit, 1, 100)}";
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = timeoutSeconds;
                await WaitForRequestAsync(request.SendWebRequest());

                if (HasRequestError(request))
                {
                    return StageProgressRecordsResult.Unavailable(request.error);
                }

                StageProgressLeaderboardResponseDto response = JsonUtility.FromJson<StageProgressLeaderboardResponseDto>(request.downloadHandler.text);
                return new StageProgressRecordsResult
                {
                    success = response != null && response.success,
                    message = response != null ? "ok" : "Invalid leaderboard response.",
                    records = response?.records ?? new List<StageProgressRecordDto>()
                };
            }
        }

        private static string NormalizeBaseUrl(string value)
        {
            string trimmed = (value ?? string.Empty).Trim();
            return trimmed.EndsWith("/") ? trimmed.Substring(0, trimmed.Length - 1) : trimmed;
        }

        private static bool HasRequestError(UnityWebRequest request)
        {
            return request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.ProtocolError
                || request.result == UnityWebRequest.Result.DataProcessingError;
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
    public sealed class StageProgressRecordSubmitDto
    {
        public string playerId;
        public string nickname;
        public int profileImageId;
        public string mode;
        public int clearedStage;
        public int totalStars;
        public string clientUpdatedAtUtc;
    }

    [Serializable]
    public sealed class StageProgressRecordDto
    {
        public int rank;
        public bool tied;
        public string playerId;
        public string nickname;
        public int profileImageId;
        public string mode;
        public int clearedStage;
        public int totalStars;
        public string updatedAt;
    }

    [Serializable]
    public sealed class StageProgressSubmitResponseDto
    {
        public bool success;
        public string message;
        public StageProgressRecordDto record;
    }

    [Serializable]
    public sealed class StageProgressLeaderboardResponseDto
    {
        public bool success;
        public string mode;
        public List<StageProgressRecordDto> records;
    }

    public sealed class StageProgressRecordsResult
    {
        public bool success;
        public string message;
        public List<StageProgressRecordDto> records = new List<StageProgressRecordDto>();

        public static StageProgressRecordsResult Unavailable(string message)
        {
            return new StageProgressRecordsResult
            {
                success = false,
                message = message,
                records = new List<StageProgressRecordDto>()
            };
        }
    }
}
