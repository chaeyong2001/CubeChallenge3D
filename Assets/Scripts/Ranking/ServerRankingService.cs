using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CubeChallenge3D.Ranking
{
    public sealed class ServerRankingService : IRankingService
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;
        private readonly LocalRankingStore localStore;
        private readonly CachedRankingStore cacheStore;
        private readonly PendingRankingSubmissionStore pendingStore;

        public ServerRankingService(
            string rankingApiBaseUrl,
            int requestTimeoutSeconds,
            LocalRankingStore rankingStore,
            CachedRankingStore cachedStore,
            PendingRankingSubmissionStore pendingSubmissionStore)
        {
            baseUrl = NormalizeBaseUrl(rankingApiBaseUrl);
            timeoutSeconds = Mathf.Max(1, requestTimeoutSeconds);
            localStore = rankingStore ?? new LocalRankingStore();
            cacheStore = cachedStore ?? new CachedRankingStore();
            pendingStore = pendingSubmissionStore ?? new PendingRankingSubmissionStore();
        }

        public async Task<RankingSubmitResult> SubmitAsync(RankingSubmission submission)
        {
            if (submission == null || !submission.isVerified || submission.isDebugClear)
            {
                if (submission != null)
                {
                    submission.syncStatus = submission.isDebugClear
                        ? RankingSyncStatus.LocalOnly
                        : RankingSyncStatus.Rejected;
                }

                string message = submission != null && submission.isDebugClear
                    ? "DEV clear - not submitted"
                    : "Rejected by local verification.";
                return RankingSubmitResult.Rejected(submission, message);
            }

            submission.isSynced = false;
            submission.syncStatus = RankingSyncStatus.Pending;
            localStore.AddSubmission(submission);

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                pendingStore.AddPending(submission);
                return Pending(submission, "Pending sync. Server URL is empty.");
            }

            RankingSubmitResult result = await TrySubmitToServerAsync(submission);
            if (result.success)
            {
                pendingStore.Remove(submission.submissionId);
                return result;
            }

            if (result.isRejected)
            {
                pendingStore.Remove(submission.submissionId);
                return result;
            }

            pendingStore.AddPending(submission);
            return Pending(submission, result.message);
        }

        public async Task<RankingFetchResult> GetTopAsync(string challengeId, int maxCount)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return GetLocalFallback(challengeId, maxCount, "Local Ranking. Server URL is empty.");
            }

            try
            {
                int serverLimit = Mathf.Clamp(Mathf.Max(1, maxCount) * 3, Mathf.Max(1, maxCount), 100);
                string url = $"{baseUrl}/ranking/top?challengeId={UnityWebRequest.EscapeURL(challengeId)}&limit={serverLimit}";
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = timeoutSeconds;
                    await WaitForRequestAsync(request.SendWebRequest());
                    if (HasRequestError(request))
                    {
                        return GetCachedOrLocalFallback(challengeId, maxCount, "Cached Ranking. Server unavailable.");
                    }

                    RankingTopResponseDto response = JsonUtility.FromJson<RankingTopResponseDto>(request.downloadHandler.text);
                    List<RankingSubmission> records = response?.records?
                        .Where(record => record != null)
                        .Select(record => record.ToSubmission())
                        .ToList() ?? new List<RankingSubmission>();
                    records = RankingDisplayHelper.TakeTop(records, maxCount);
                    cacheStore.SaveCache(challengeId, records);
                    return new RankingFetchResult
                    {
                        success = true,
                        fromCache = false,
                        message = "Server Ranking",
                        records = records
                    };
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Ranking top fetch failed: {exception.Message}");
                return GetCachedOrLocalFallback(challengeId, maxCount, "Cached Ranking. Server unavailable.");
            }
        }

        public RankingFetchResult GetCachedTop(string challengeId, int maxCount)
        {
            IReadOnlyList<RankingSubmission> cached = cacheStore.GetCache(challengeId);
            if (cached.Count > 0)
            {
                return new RankingFetchResult
                {
                    success = true,
                    fromCache = true,
                    message = "Cached Ranking",
                    records = RankingDisplayHelper.TakeTop(cached, maxCount)
                };
            }

            return GetLocalFallback(challengeId, maxCount, "Local Ranking. No cached server records.");
        }

        public async Task<RankingFetchResult> GetMyRecordsAsync(string playerId, int maxCount)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return new RankingFetchResult
                {
                    success = false,
                    message = "Create a profile first.",
                    records = new List<RankingSubmission>()
                };
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return GetLocalPlayerFallback(playerId, maxCount, "Local Records. Server URL is empty.");
            }

            try
            {
                int safeLimit = Mathf.Clamp(Mathf.Max(1, maxCount), 1, 100);
                string url = $"{baseUrl}/ranking/my-records?playerId={UnityWebRequest.EscapeURL(playerId)}&limit={safeLimit}";
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = timeoutSeconds;
                    await WaitForRequestAsync(request.SendWebRequest());
                    if (HasRequestError(request))
                    {
                        return GetLocalPlayerFallback(playerId, maxCount, "Local Records. Server unavailable.");
                    }

                    RankingTopResponseDto response = JsonUtility.FromJson<RankingTopResponseDto>(request.downloadHandler.text);
                    List<RankingSubmission> records = response?.records?
                        .Where(record => record != null)
                        .Select(record => record.ToSubmission())
                        .Take(Mathf.Max(0, maxCount))
                        .ToList() ?? new List<RankingSubmission>();
                    return new RankingFetchResult
                    {
                        success = true,
                        fromCache = false,
                        message = "Server Records",
                        records = records
                    };
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Ranking my-records fetch failed: {exception.Message}");
                return GetLocalPlayerFallback(playerId, maxCount, "Local Records. Server unavailable.");
            }
        }

        public async Task<RankingRankResult> GetRankAsync(string challengeId, string playerId, string submissionId)
        {
            if (string.IsNullOrWhiteSpace(submissionId))
            {
                return new RankingRankResult
                {
                    success = false,
                    message = "No latest record.",
                    rank = 0
                };
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return localStore.GetRank(challengeId, submissionId);
            }

            try
            {
                string url = $"{baseUrl}/ranking/my-rank?challengeId={UnityWebRequest.EscapeURL(challengeId)}&playerId={UnityWebRequest.EscapeURL(playerId ?? string.Empty)}&submissionId={UnityWebRequest.EscapeURL(submissionId)}";
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = timeoutSeconds;
                    await WaitForRequestAsync(request.SendWebRequest());
                    if (HasRequestError(request))
                    {
                        return localStore.GetRank(challengeId, submissionId);
                    }

                    RankingRankResponseDto response = JsonUtility.FromJson<RankingRankResponseDto>(request.downloadHandler.text);
                    RankingRankResult result = response?.ToResult();
                    if (result != null && result.success)
                    {
                        return result;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Ranking rank fetch failed: {exception.Message}");
            }

            return localStore.GetRank(challengeId, submissionId);
        }

        public async Task RetryPendingAsync()
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return;
            }

            foreach (RankingSubmission submission in pendingStore.GetPending().ToList())
            {
                if (submission == null || !submission.isVerified || submission.isDebugClear)
                {
                    continue;
                }

                RankingSubmitResult result = await TrySubmitToServerAsync(submission);
                if (result.success)
                {
                    pendingStore.Remove(submission.submissionId);
                }
                else if (result.isRejected)
                {
                    pendingStore.Remove(submission.submissionId);
                }
            }
        }

        private async Task<RankingSubmitResult> TrySubmitToServerAsync(RankingSubmission submission)
        {
            try
            {
                string json = JsonUtility.ToJson(RankingSubmissionDto.FromSubmission(submission));
                byte[] body = Encoding.UTF8.GetBytes(json);
                using (UnityWebRequest request = new UnityWebRequest($"{baseUrl}/ranking/submit", UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = timeoutSeconds;
                    request.SetRequestHeader("Content-Type", "application/json");
                    await WaitForRequestAsync(request.SendWebRequest());
                    if (HasConnectionError(request))
                    {
                        return Pending(submission, "Pending sync. Server unavailable.");
                    }

                    if (HasProtocolError(request))
                    {
                        RankingSubmitErrorDto error = ReadSubmitError(request);
                        submission.isSynced = false;
                        submission.syncStatus = RankingSyncStatus.Rejected;
                        string reason = !string.IsNullOrWhiteSpace(error?.reason)
                            ? error.reason
                            : "server_validation_rejected";
                        string message = !string.IsNullOrWhiteSpace(error?.message)
                            ? error.message
                            : "Server rejected ranking submission.";
                        Debug.LogWarning($"Ranking submit rejected: {reason} - {message}");
                        localStore.Save();
                        return RankingSubmitResult.Rejected(submission, $"{reason}: {message}");
                    }

                    RankingSubmitResponseDto response = JsonUtility.FromJson<RankingSubmitResponseDto>(request.downloadHandler.text);
                    if (response == null || !response.success || !response.isVerified)
                    {
                        submission.isSynced = false;
                        submission.syncStatus = RankingSyncStatus.Rejected;
                        localStore.Save();
                        return RankingSubmitResult.Rejected(submission, response?.message ?? "Server rejected ranking submission.");
                    }

                    submission.isSynced = true;
                    submission.syncStatus = RankingSyncStatus.Synced;
                    localStore.Save();
                    return RankingSubmitResult.Success(submission, "Synced");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Ranking submit failed: {exception.Message}");
                return Pending(submission, "Pending sync. Server unavailable.");
            }
        }

        private RankingFetchResult GetCachedOrLocalFallback(string challengeId, int maxCount, string message)
        {
            IReadOnlyList<RankingSubmission> cached = cacheStore.GetCache(challengeId);
            if (cached.Count > 0)
            {
                return new RankingFetchResult
                {
                    success = true,
                    fromCache = true,
                    message = message,
                    records = RankingDisplayHelper.TakeTop(cached, maxCount)
                };
            }

            return GetLocalFallback(challengeId, maxCount, "Local Ranking. Server unavailable.");
        }

        private RankingFetchResult GetLocalFallback(string challengeId, int maxCount, string message)
        {
            return new RankingFetchResult
            {
                success = true,
                fromCache = false,
                message = message,
                records = localStore.GetTopByTime(challengeId, maxCount).ToList()
            };
        }

        private RankingFetchResult GetLocalPlayerFallback(string playerId, int maxCount, string message)
        {
            return new RankingFetchResult
            {
                success = true,
                fromCache = false,
                message = message,
                records = localStore.GetPlayerRecords(playerId, maxCount).ToList()
            };
        }

        private static RankingSubmitResult Pending(RankingSubmission submission, string message)
        {
            if (submission != null)
            {
                submission.isSynced = false;
                submission.syncStatus = RankingSyncStatus.Pending;
            }

            return new RankingSubmitResult
            {
                isPending = true,
                message = message,
                submission = submission
            };
        }

        private static string NormalizeBaseUrl(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd('/');
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

        private static RankingSubmitErrorDto ReadSubmitError(UnityWebRequest request)
        {
            string text = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    RankingSubmitErrorDto error = JsonUtility.FromJson<RankingSubmitErrorDto>(text);
                    if (error != null && (!string.IsNullOrWhiteSpace(error.message) || !string.IsNullOrWhiteSpace(error.reason)))
                    {
                        return error;
                    }
                }
                catch (Exception)
                {
                    // Non-JSON response. Use Unity's error field below.
                }
            }

            return new RankingSubmitErrorDto
            {
                success = false,
                reason = "http_error",
                message = string.IsNullOrWhiteSpace(request.error) ? "Server rejected ranking submission." : request.error
            };
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
