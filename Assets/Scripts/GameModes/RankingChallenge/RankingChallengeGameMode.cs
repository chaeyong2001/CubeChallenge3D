using System.Collections.Generic;
using System.Threading.Tasks;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Cube.Utils;
using CubeChallenge3D.Ranking;
using CubeChallenge3D.Save;
using UnityEngine;

namespace CubeChallenge3D.GameModes.RankingChallenge
{
    public sealed class RankingChallengeGameMode : MonoBehaviour
    {
        [SerializeField] private CubeController cubeController;
        [SerializeField] private CubeControlModeController controlModeController;
        [SerializeField] private int scrambleLength = 20;

        private readonly List<CubeMove> activeScramble = new List<CubeMove>();
        private SettingsStore settingsStore;
        private LocalRankingStore localRankingStore;
        private CachedRankingStore cachedRankingStore;
        private PendingRankingSubmissionStore pendingSubmissionStore;
        private IRankingService rankingService;
        private RankingChallengeConfig config;
        private readonly List<RankingSubmission> displayedRankingRecords = new List<RankingSubmission>();
        private RankingChallengeState state = RankingChallengeState.Ready;
        private float elapsedTime;
        private float solvedTime;
        private int solvedMoveCount;
        private bool hasSubmittedCurrentResult;
        private string lastSubmitMessage = "Server: Not connected";
        private string rankingSourceLabel = "Local Ranking";

        public RankingChallengeState State => state;
        public RankingChallengeConfig Config => config;
        public LocalRankingStore LocalRankingStore => localRankingStore;
        public IReadOnlyList<RankingSubmission> DisplayedRankingRecords => displayedRankingRecords.AsReadOnly();
        public string RankingSourceLabel => rankingSourceLabel;
        public string LastSubmitMessage => lastSubmitMessage;
        public float ElapsedTime => state == RankingChallengeState.Solved ? solvedTime : elapsedTime;
        public int MoveCount => state == RankingChallengeState.Solved ? solvedMoveCount : cubeController?.UserMoveCount ?? 0;

        public void Initialize(
            CubeController controller,
            CubeControlModeController controlController,
            SettingsStore store,
            LocalRankingStore rankingStore)
        {
            if (cubeController != null)
            {
                cubeController.MoveCommandCompleted -= HandleMoveCompleted;
                cubeController.ScrambleCompleted -= HandleScrambleCompleted;
            }

            cubeController = controller;
            controlModeController = controlController;
            settingsStore = store;
            localRankingStore = rankingStore ?? new LocalRankingStore();
            cachedRankingStore = new CachedRankingStore();
            pendingSubmissionStore = new PendingRankingSubmissionStore();
            rankingService = CreateRankingService();
            config = RankingChallengeConfig.CreateToday(scrambleLength);
            cubeController.MoveCommandCompleted += HandleMoveCompleted;
            cubeController.ScrambleCompleted += HandleScrambleCompleted;
            cubeController.SetUserInputEnabled(false);
            SetState(RankingChallengeState.Ready);
            RefreshRankings();
            _ = rankingService.RetryPendingAsync();
        }

        public void PrepareChallenge()
        {
            if (cubeController == null || cubeController.IsBusy)
            {
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
            cubeController.ApplyScrambleFromSolved(activeScramble);
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
            string playerName = settingsStore?.Current?.playerName ?? "Player";
            string playerId = settingsStore?.Current?.playerId ?? "local";
            RankingSubmission submission = RankingSubmission.Create(
                config.challengeId,
                playerId,
                playerName,
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
