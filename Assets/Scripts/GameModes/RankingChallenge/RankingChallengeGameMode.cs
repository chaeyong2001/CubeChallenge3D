using System.Collections.Generic;
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
        private RankingChallengeState state = RankingChallengeState.Ready;
        private float elapsedTime;
        private float solvedTime;
        private int solvedMoveCount;
        private bool hasSubmittedCurrentResult;
        private string lastSubmitMessage = "Server: Not connected";

        public RankingChallengeState State => state;
        public RankingChallengeConfig Config => config;
        public LocalRankingStore LocalRankingStore => localRankingStore;
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
            rankingService = new LocalRankingService(localRankingStore, cachedRankingStore, pendingSubmissionStore);
            config = RankingChallengeConfig.CreateToday(scrambleLength);
            cubeController.MoveCommandCompleted += HandleMoveCompleted;
            cubeController.ScrambleCompleted += HandleScrambleCompleted;
            cubeController.SetUserInputEnabled(true);
            SetState(RankingChallengeState.Ready);
        }

        public void StartChallenge()
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
            lastSubmitMessage = "Server: Not connected";
            cubeController.SetUserInputEnabled(false);
            SetState(RankingChallengeState.Scrambling);
            cubeController.ApplyScrambleFromSolved(activeScramble);
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
            cubeController.SetUserInputEnabled(true);
            SetState(RankingChallengeState.Playing);
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
            SubmitDebugResultOnce();
            SetState(RankingChallengeState.Solved);
        }

        private async void SubmitDebugResultOnce()
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
            submission.isVerified = true;
            submission.syncStatus = RankingSyncStatus.LocalOnly;
            RankingSubmitResult submitResult = await rankingService.SubmitAsync(submission);
            lastSubmitMessage = $"{submitResult.message} DEV force clear.";
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

            RankingSubmitResult submitResult = await rankingService.SubmitAsync(submission);
            lastSubmitMessage = submitResult.message;
        }

        private void SetState(RankingChallengeState nextState)
        {
            state = nextState;
        }
    }
}
