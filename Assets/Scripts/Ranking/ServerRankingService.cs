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
                    if (HasRequestError(request))
                    {
                        return Pending(submission, "Pending sync. Server unavailable.");
                    }

                    RankingSubmitResponseDto response = JsonUtility.FromJson<RankingSubmitResponseDto>(request.downloadHandler.text);
                    if (response == null || !response.success || !response.isVerified)
                    {
                        submission.isSynced = false;
                        submission.syncStatus = RankingSyncStatus.Failed;
                        return Pending(submission, response?.message ?? "Pending sync. Server rejected response.");
                    }

                    submission.isSynced = true;
                    submission.syncStatus = RankingSyncStatus.Synced;
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

        private static async Task WaitForRequestAsync(UnityWebRequestAsyncOperation operation)
        {
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }
    }
}
