using System.Collections.Generic;
using System.Threading.Tasks;
using CubeChallenge3D.Audio;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Cube.Utils;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Profile;
using UnityEngine;

namespace CubeChallenge3D.GameModes.RankingChallenge
{
    public sealed class RankingChallengeGameMode : MonoBehaviour
    {
        [SerializeField] private CubeController cubeController;
        [SerializeField] private CubeControlModeController controlModeController;
        [SerializeField] private int scrambleLength = 20;
        [SerializeField] private int entryCoinCost = 50;

        private readonly List<CubeMove> activeScramble = new List<CubeMove>();
        private SettingsStore settingsStore;
        private PlayerProfileStore playerProfileStore;
        private LocalRankingStore localRankingStore;
        private CachedRankingStore cachedRankingStore;
        private PendingRankingSubmissionStore pendingSubmissionStore;
        private WalletStore walletStore;
        private IRankingService rankingService;
        private WeeklyRankingRewardService weeklyRewardService;
        private RankingChallengeConfig config;
        private readonly List<RankingSubmission> displayedRankingRecords = new List<RankingSubmission>();
        private RankingChallengeState state = RankingChallengeState.Ready;
        private float elapsedTime;
        private float solvedTime;
        private int solvedMoveCount;
        private bool hasSubmittedCurrentResult;
        private string lastSubmitMessage = "Server: Not connected";
        private string rankingSourceLabel = "Local Ranking";
        private RankingSubmission latestSubmittedRecord;

        public RankingChallengeState State => state;
        public RankingChallengeConfig Config => config;
        public LocalRankingStore LocalRankingStore => localRankingStore;
        public IReadOnlyList<RankingSubmission> DisplayedRankingRecords => displayedRankingRecords.AsReadOnly();
        public string RankingSourceLabel => rankingSourceLabel;
        public RankingSubmission LatestSubmittedRecord => latestSubmittedRecord;
        public string LastSubmitMessage => lastSubmitMessage;
        public float ElapsedTime => state == RankingChallengeState.Solved ? solvedTime : elapsedTime;
        public int MoveCount => state == RankingChallengeState.Solved ? solvedMoveCount : cubeController?.UserMoveCount ?? 0;
        public PlayerProfile CurrentPlayerProfile => playerProfileStore?.Current;
        public int EntryCoinCost => entryCoinCost;

        public void Initialize(
            CubeController controller,
            CubeControlModeController controlController,
            SettingsStore store,
            LocalRankingStore rankingStore)
        {
            AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.RankingChallengeBgmReason, true);

            if (cubeController != null)
            {
                cubeController.MoveCommandCompleted -= HandleMoveCompleted;
                cubeController.ScrambleCompleted -= HandleScrambleCompleted;
            }

            cubeController = controller;
            controlModeController = controlController;
            settingsStore = store;
            playerProfileStore = new PlayerProfileStore();
            playerProfileStore.SyncAppSettings(settingsStore);
            localRankingStore = rankingStore ?? new LocalRankingStore();
            cachedRankingStore = new CachedRankingStore();
            pendingSubmissionStore = new PendingRankingSubmissionStore();
            walletStore = new WalletStore();
            config = RankingChallengeConfig.CreateToday(scrambleLength);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (settingsStore?.Current != null && settingsStore.Current.showDebugPanel)
            {
                RankingTestDataSeeder.EnsureWorldRankingTestData(config, localRankingStore, cachedRankingStore);
            }
#endif
            rankingService = CreateRankingService();
            weeklyRewardService = CreateWeeklyRewardService();
            cubeController.MoveCommandCompleted += HandleMoveCompleted;
            cubeController.ScrambleCompleted += HandleScrambleCompleted;
            cubeController.SetUserInputEnabled(false);
            SetState(RankingChallengeState.Ready);
            RefreshRankings();
            _ = rankingService.RetryPendingAsync();
        }

        public void PrepareChallenge(bool chargeEntry = true, bool instantScramble = true)
        {
            if (cubeController == null || cubeController.IsBusy)
            {
                return;
            }

            if (chargeEntry && !walletStore.SpendCoins(entryCoinCost))
            {
                lastSubmitMessage = $"Not enough coins. This challenge costs {entryCoinCost} Coins.";
                return;
            }

            config = RankingChallengeConfig.CreateToday(scrambleLength);
            activeScramble.Clear();
            activeScramble.AddRange(ScrambleGenerator.Generate(config.scrambleLength, config.seed));
            elapsedTime = 0f;
            solvedTime = 0f;
            solvedMoveCount = 0;
            hasSubmittedCurrentResult = false;
            lastSubmitMessage = IsServerConfigured() ? "Ready" : "Server: Not connected";
            cubeController.SetUserInputEnabled(false);
            SetState(RankingChallengeState.Scrambling);
            RefreshRankings();
            if (instantScramble)
            {
                cubeController.ApplyScrambleFromSolvedInstant(activeScramble);
            }
            else
            {
                cubeController.ApplyScrambleFromSolved(activeScramble);
            }
        }

        public void StartChallenge()
        {
            if (cubeController == null
                || cubeController.IsBusy
                || state != RankingChallengeState.Previewing)
            {
                return;
            }

            elapsedTime = 0f;
            cubeController.SetUserInputEnabled(true);
            SetState(RankingChallengeState.Playing);
        }

        public async void RetryPendingSubmissions()
        {
            if (rankingService == null)
            {
                return;
            }

            lastSubmitMessage = "Retrying pending sync...";
            await rankingService.RetryPendingAsync();
            lastSubmitMessage = IsServerConfigured() ? "Retry complete" : "Server: Not connected";
            await RefreshRankingsAsync();
        }

        public Task<RankingFetchResult> FetchWorldRankingsAsync(int maxCount)
        {
            if (rankingService == null || config == null)
            {
                return Task.FromResult(new RankingFetchResult
                {
                    success = false,
                    message = "Ranking is not ready.",
                    records = new List<RankingSubmission>()
                });
            }

            return rankingService.GetTopAsync(config.challengeId, maxCount);
        }

        public Task<RankingFetchResult> FetchMyRankingsAsync(int maxCount)
        {
            PlayerProfile profile = CurrentPlayerProfile;
            string playerId = profile != null ? profile.profileId : string.Empty;
            if (rankingService == null || string.IsNullOrWhiteSpace(playerId))
            {
                return Task.FromResult(new RankingFetchResult
                {
                    success = false,
                    message = "Create a profile first.",
                    records = new List<RankingSubmission>()
                });
            }

            return rankingService.GetMyRecordsAsync(playerId, maxCount);
        }

        public Task<RankingRankResult> FetchLatestRankAsync()
        {
            if (rankingService == null || config == null || latestSubmittedRecord == null)
            {
                return Task.FromResult(new RankingRankResult
                {
                    success = false,
                    message = "No latest record.",
                    rank = 0
                });
            }

            return rankingService.GetRankAsync(
                config.challengeId,
                latestSubmittedRecord.playerId,
                latestSubmittedRecord.submissionId);
        }

        public Task<WeeklyRankingRewardDto> FetchWeeklyRankingRewardAsync()
        {
            if (weeklyRewardService == null || !weeklyRewardService.IsConfigured)
            {
                return Task.FromResult(new WeeklyRankingRewardDto
                {
                    exists = false,
                    message = "Weekly ranking rewards are unavailable."
                });
            }

            return weeklyRewardService.GetClaimableAsync(GetCurrentPlayerId());
        }

        public Task<WeeklyRankingRewardInfoResponseDto> FetchWeeklyRankingRewardInfoAsync()
        {
            return weeklyRewardService != null
                ? weeklyRewardService.GetInfoAsync()
                : Task.FromResult(new WeeklyRankingRewardInfoResponseDto
                {
                    success = false,
                    description = "Weekly rankings run from Monday to Sunday.\nRewards are distributed every Monday at 00:00 KST.",
                    rewards = new[] { "1st Place: 15 Gems", "2nd Place: 10 Gems", "3rd Place: 100 Coins" }
                });
        }

        public async Task<WeeklyRankingRewardClaimResponseDto> ClaimWeeklyRankingRewardAsync(WeeklyRankingRewardDto reward)
        {
            if (weeklyRewardService == null || reward == null)
            {
                return new WeeklyRankingRewardClaimResponseDto
                {
                    success = false,
                    message = "Weekly ranking rewards are unavailable."
                };
            }

            WeeklyRankingRewardClaimResponseDto response = await weeklyRewardService.ClaimAsync(
                GetCurrentPlayerId(),
                reward.weekStartKst);
            if (response != null && response.success && response.claimed && response.reward != null)
            {
                if (string.Equals(response.reward.rewardType, "gem", System.StringComparison.OrdinalIgnoreCase))
                {
                    walletStore.AddGems(response.reward.rewardAmount);
                }
                else if (string.Equals(response.reward.rewardType, "coin", System.StringComparison.OrdinalIgnoreCase))
                {
                    walletStore.AddCoins(response.reward.rewardAmount);
                }
            }

            return response;
        }

        private void Update()
        {
            if (state == RankingChallengeState.Playing)
            {
                elapsedTime += Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            if (cubeController == null)
            {
                return;
            }

            cubeController.MoveCommandCompleted -= HandleMoveCompleted;
            cubeController.ScrambleCompleted -= HandleScrambleCompleted;
        }

        private void HandleScrambleCompleted()
        {
            if (state != RankingChallengeState.Scrambling)
            {
                return;
            }

            elapsedTime = 0f;
            cubeController.SetUserInputEnabled(false);
            SetState(RankingChallengeState.Previewing);
        }

        private void HandleMoveCompleted(CubeMove move)
        {
            if (state != RankingChallengeState.Playing || !cubeController.CurrentState.IsSolved())
            {
                return;
            }

            solvedTime = elapsedTime;
            solvedMoveCount = cubeController.UserMoveCount;
            cubeController.SetUserInputEnabled(false);
            AudioFeedbackManager.PlayClearVibration();
            SubmitResultOnce();
            SetState(RankingChallengeState.Solved);
        }

        [ContextMenu("DEV Force Clear")]
        public void ForceClearForDebug()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            return;
#else
            if (cubeController == null || cubeController.IsBusy || state == RankingChallengeState.Solved)
            {
                return;
            }

            if (activeScramble.Count == 0)
            {
                config = RankingChallengeConfig.CreateToday(scrambleLength);
                activeScramble.AddRange(ScrambleGenerator.Generate(config.scrambleLength, config.seed));
            }

            solvedTime = Mathf.Max(elapsedTime, 0.01f);
            solvedMoveCount = cubeController.UserMoveCount;
            cubeController.SetUserInputEnabled(false);
            MarkDebugResultWithoutSubmission();
            // DEV force clear is a result shortcut, so keep the submitted log but
            // also bring the visible cube back to the solved state for diagnostics.
            cubeController.ResetSolved();
            cubeController.SetUserInputEnabled(false);
            AudioFeedbackManager.PlayClearVibration();
            SetState(RankingChallengeState.Solved);
#endif
        }

        private void MarkDebugResultWithoutSubmission()
        {
            if (hasSubmittedCurrentResult)
            {
                return;
            }

            hasSubmittedCurrentResult = true;
            lastSubmitMessage = "DEV clear - not submitted";
            RefreshRankings();
        }

        private async void SubmitResultOnce()
        {
            if (hasSubmittedCurrentResult || rankingService == null)
            {
                return;
            }

            hasSubmittedCurrentResult = true;
            string controlMode = controlModeController != null
                ? controlModeController.CurrentControlMode.ToString()
                : string.Empty;
            PlayerProfile profile = playerProfileStore?.Current;
            string playerName = profile != null && !string.IsNullOrWhiteSpace(profile.nickname)
                ? profile.nickname
                : settingsStore?.Current?.playerName ?? "Player";
            string playerId = profile != null && !string.IsNullOrWhiteSpace(profile.profileId)
                ? profile.profileId
                : settingsStore?.Current?.playerId ?? "local";
            int avatarId = profile != null ? profile.avatarId : -1;
            RankingSubmission submission = RankingSubmission.Create(
                config.challengeId,
                playerId,
                playerName,
                avatarId,
                solvedTime,
                solvedMoveCount,
                MoveUtility.ToNotationSequence(activeScramble),
                cubeController.MoveHistory.ToNotationString(),
                controlMode);
            RankingVerificationResult verification = RankingVerificationHelper.VerifySubmissionDetailed(submission);
            submission.isVerified = verification.isValid;
            if (!verification.isValid)
            {
                submission.syncStatus = RankingSyncStatus.Rejected;
                lastSubmitMessage = $"Rejected: {verification.reason}";
                return;
            }

            lastSubmitMessage = IsServerConfigured() ? "Submitting..." : "Saved locally";
            RankingSubmitResult submitResult = await rankingService.SubmitAsync(submission);
            latestSubmittedRecord = submitResult?.submission ?? submission;
            lastSubmitMessage = submitResult.message;
            await RefreshRankingsAsync();
        }

        private void SetState(RankingChallengeState nextState)
        {
            state = nextState;
        }

        private IRankingService CreateRankingService()
        {
            if (IsServerConfigured())
            {
                return new ServerRankingService(
                    settingsStore.Current.rankingApiBaseUrl,
                    settingsStore.Current.rankingRequestTimeoutSeconds,
                    localRankingStore,
                    cachedRankingStore,
                    pendingSubmissionStore);
            }

            return new LocalRankingService(localRankingStore, cachedRankingStore, pendingSubmissionStore);
        }

        private WeeklyRankingRewardService CreateWeeklyRewardService()
        {
            if (IsServerConfigured())
            {
                return new WeeklyRankingRewardService(
                    settingsStore.Current.rankingApiBaseUrl,
                    settingsStore.Current.rankingRequestTimeoutSeconds);
            }

            return null;
        }

        private string GetCurrentPlayerId()
        {
            PlayerProfile profile = CurrentPlayerProfile;
            if (profile != null && !string.IsNullOrWhiteSpace(profile.profileId))
            {
                return profile.profileId;
            }

            return settingsStore?.Current?.playerId ?? string.Empty;
        }

        private bool IsServerConfigured()
        {
            return settingsStore?.Current != null
                && settingsStore.Current.useServerRanking
                && !string.IsNullOrWhiteSpace(settingsStore.Current.rankingApiBaseUrl);
        }

        private async void RefreshRankings()
        {
            await RefreshRankingsAsync();
        }

        private async Task RefreshRankingsAsync()
        {
            if (rankingService == null || config == null)
            {
                return;
            }

            RankingFetchResult result = await rankingService.GetTopAsync(config.challengeId, 10);
            displayedRankingRecords.Clear();
            if (result?.records != null)
            {
                displayedRankingRecords.AddRange(result.records);
            }

            rankingSourceLabel = ResolveRankingSourceLabel(result);
        }

        private string ResolveRankingSourceLabel(RankingFetchResult result)
        {
            if (result == null)
            {
                return "Local Ranking";
            }

            if (result.message != null && result.message.Contains("Server Ranking"))
            {
                return "Server Ranking";
            }

            if (result.fromCache || (result.message != null && result.message.Contains("Cached")))
            {
                return "Cached Ranking";
            }

            return "Local Ranking";
        }
    }
}
