using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CubeChallenge3D.Ranking
{
    public sealed class WeeklyRankingRewardService
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;

        public WeeklyRankingRewardService(string rankingApiBaseUrl, int requestTimeoutSeconds)
        {
            baseUrl = string.IsNullOrWhiteSpace(rankingApiBaseUrl)
                ? string.Empty
                : rankingApiBaseUrl.Trim().TrimEnd('/');
            timeoutSeconds = Mathf.Max(1, requestTimeoutSeconds);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(baseUrl);

        public async Task<WeeklyRankingRewardDto> GetClaimableAsync(string playerId)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(playerId))
            {
                return new WeeklyRankingRewardDto { exists = false, message = "Weekly ranking rewards are unavailable." };
            }

            try
            {
                string url = $"{baseUrl}/ranking/weekly-rewards/claimable?playerId={UnityWebRequest.EscapeURL(playerId)}";
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = timeoutSeconds;
                    await WaitForRequestAsync(request.SendWebRequest());
                    if (HasRequestError(request))
                    {
                        return new WeeklyRankingRewardDto { exists = false, message = "Weekly ranking rewards are unavailable." };
                    }

                    return JsonUtility.FromJson<WeeklyRankingRewardDto>(request.downloadHandler.text)
                        ?? new WeeklyRankingRewardDto { exists = false };
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Weekly ranking reward check failed: {exception.Message}");
                return new WeeklyRankingRewardDto { exists = false, message = "Weekly ranking rewards are unavailable." };
            }
        }

        public async Task<WeeklyRankingRewardInfoResponseDto> GetInfoAsync()
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return BuildOfflineInfo();
            }

            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get($"{baseUrl}/ranking/weekly-rewards/info"))
                {
                    request.timeout = timeoutSeconds;
                    await WaitForRequestAsync(request.SendWebRequest());
                    if (HasRequestError(request))
                    {
                        return BuildOfflineInfo();
                    }

                    return JsonUtility.FromJson<WeeklyRankingRewardInfoResponseDto>(request.downloadHandler.text)
                        ?? BuildOfflineInfo();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Weekly ranking reward info failed: {exception.Message}");
                return BuildOfflineInfo();
            }
        }

        public async Task<WeeklyRankingRewardClaimResponseDto> ClaimAsync(string playerId, string weekStartKst)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(playerId))
            {
                return new WeeklyRankingRewardClaimResponseDto
                {
                    success = false,
                    claimed = false,
                    message = "Weekly ranking rewards are unavailable."
                };
            }

            try
            {
                string json = JsonUtility.ToJson(new WeeklyRankingRewardClaimRequestDto
                {
                    playerId = playerId,
                    weekStartKst = weekStartKst ?? string.Empty
                });
                byte[] body = Encoding.UTF8.GetBytes(json);
                using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/ranking/weekly-rewards/claim", UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = timeoutSeconds;
                    request.SetRequestHeader("Content-Type", "application/json");
                    await WaitForRequestAsync(request.SendWebRequest());
                    if (HasRequestError(request))
                    {
                        return new WeeklyRankingRewardClaimResponseDto
                        {
                            success = false,
                            claimed = false,
                            message = "Weekly ranking rewards are unavailable."
                        };
                    }

                    return JsonUtility.FromJson<WeeklyRankingRewardClaimResponseDto>(request.downloadHandler.text)
                        ?? new WeeklyRankingRewardClaimResponseDto { success = false, message = "Invalid reward response." };
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Weekly ranking reward claim failed: {exception.Message}");
                return new WeeklyRankingRewardClaimResponseDto
                {
                    success = false,
                    claimed = false,
                    message = "Weekly ranking reward claim failed."
                };
            }
        }

        private static WeeklyRankingRewardInfoResponseDto BuildOfflineInfo()
        {
            return new WeeklyRankingRewardInfoResponseDto
            {
                success = false,
                description = "Weekly rankings run from Monday to Sunday.\nRewards are distributed every Monday at 00:00 KST.",
                rewards = new[]
                {
                    "1st Place: 15 Gems",
                    "2nd Place: 10 Gems",
                    "3rd Place: 100 Coins"
                }
            };
        }

        private static bool HasRequestError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError
                || request.result == UnityWebRequest.Result.ProtocolError
                || request.result == UnityWebRequest.Result.DataProcessingError;
#else
            return request.isNetworkError || request.isHttpError;
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
}
