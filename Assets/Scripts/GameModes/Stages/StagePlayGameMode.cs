using System;
using System.Collections.Generic;
using System.Linq;
using CubeChallenge3D.Ads;
using CubeChallenge3D.Audio;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Economy;
using CubeChallenge3D.Inventory;
using CubeChallenge3D.Stages.Assist;
using CubeChallenge3D.Stages.Model;
using CubeChallenge3D.Stages.Progress;
using CubeChallenge3D.Stages.Services;
using UnityEngine;

namespace CubeChallenge3D.GameModes.Stages
{
    public sealed class StagePlayGameMode : MonoBehaviour
    {
        private CubeController cubeController;
        private CubeControlModeController controlModeController;
        private StageDataLoader dataLoader;
        private StageProgressStore progressStore;
        private StagePlayState state = StagePlayState.Ready;
        private readonly StageRuntimeState runtime = new StageRuntimeState();
        private CubeState targetState;
        private StageAssistState assistState;
        private StageHintResult lastHint;
        private RewardedAdService rewardService;
        private InventoryStore inventoryStore;
        private WalletStore walletStore;
        private string statusMessage = "Ready";
        private int earnedStars;
        private int earnedCoins;
        private int baseStars;
        private int previousBestStars;
        private int maxStarsAllowed = 3;
        private bool bestStarsUpdated;

        public StagePlayState State => state;
        public StageRuntimeState Runtime => runtime;
        public string StatusMessage => statusMessage;
        public int EarnedStars => earnedStars;
        public int EarnedCoins => earnedCoins;
        public int BaseStars => baseStars;
        public int PreviousBestStars => previousBestStars;
        public int MaxStarsAllowed => maxStarsAllowed;
        public bool BestStarsUpdated => bestStarsUpdated;
        public StageData CurrentStage => runtime.stage;
        public int CurrentProgressStageNumber => GetCurrentProgressStageNumber();
        public bool IsReverseTargetStage => IsTargetPatternStage(runtime.stage);
        public CubeState TargetState => targetState?.Clone();
        public StageAssistState AssistState => assistState;
        public StageHintResult LastHint => lastHint;
        public InventoryStore InventoryStore => inventoryStore;
        public WalletStore WalletStore => walletStore;
        public int StageContinueMaxPerRun => rewardService?.Config.stageContinueMaxPerRun ?? 2;
        public int StageContinueMovesReward => rewardService?.Config.stageContinueMovesReward ?? 2;
        public bool IsShowingAd => rewardService != null && rewardService.IsShowingAd;
        public string StageContinueAdStatus => assistState != null
            && assistState.adContinueCount >= StageContinueMaxPerRun
                ? "Stage continue limit reached."
                : rewardService?.GetUnavailableMessage(RewardedAdPlacement.OutOfMovesPlus2)
                    ?? "Ad is not available yet.";

        public event Action StateChanged;

        public void Initialize(
            CubeController controller,
            CubeControlModeController controlController,
            StageDataLoader loader,
            StageProgressStore store)
        {
            AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.StageGameplayBgmReason, true);
            AudioFeedbackManager.SetBgmSuppressed(AudioFeedbackManager.StageAdvanceBgmReason, false);

            if (cubeController != null)
            {
                cubeController.MoveCommandCompleted -= HandleMoveCompleted;
                cubeController.ScrambleCompleted -= HandleScrambleCompleted;
            }

            cubeController = controller;
            controlModeController = controlController;
            dataLoader = loader ?? new StageDataLoader();
            progressStore = store ?? new StageProgressStore();
            rewardService = RewardedAdService.CreateDefault();
            inventoryStore = new InventoryStore();
            walletStore = new WalletStore();
            cubeController.MoveCommandCompleted += HandleMoveCompleted;
            cubeController.ScrambleCompleted += HandleScrambleCompleted;
            cubeController.SetUserInputEnabled(false);
        }

        public void LoadStage(StageData stage)
        {
            if (stage == null)
            {
                SetState(StagePlayState.NotPlayable, "Stage not found.");
                return;
            }

            if (!IsPlayableStageType(stage.stageType))
            {
                SetState(StagePlayState.NotPlayable, "Not playable yet.");
                return;
            }

            runtime.stage = stage;
            assistState = StageAssistState.Create(stage);
            lastHint = null;
            runtime.elapsedSeconds = 0f;
            runtime.currentMoves = 0;
            runtime.moveLimit = assistState.currentMoveLimit;
            runtime.remainingMoves = assistState.currentMoveLimit;
            runtime.isPlaying = false;
            runtime.isCompleted = false;
            runtime.isFailed = false;
            runtime.targetSummary = BuildTargetSummary(stage);
            targetState = null;
            earnedStars = 0;
            earnedCoins = 0;
            baseStars = 0;
            previousBestStars = 0;
            maxStarsAllowed = 3;
            bestStarsUpdated = false;

            if (IsTargetPatternStage(stage))
            {
                if (!TryBuildTargetState(stage, out targetState))
                {
                    SetState(StagePlayState.NotPlayable, "Target state is invalid.");
                    return;
                }

                cubeController?.SetViewVisible(false);
                SetState(StagePlayState.TargetIntro, "Make this target.");
                return;
            }

            cubeController?.SetViewVisible(true);
            SetState(StagePlayState.Ready, "Ready");
        }

        public void StartStage()
        {
            if (cubeController == null
                || cubeController.IsBusy
                || runtime.stage == null
                || state == StagePlayState.Preparing)
            {
                return;
            }

            if (!IsPlayableStageType(runtime.stage.stageType))
            {
                SetState(StagePlayState.NotPlayable, "Not playable yet.");
                return;
            }

            walletStore ??= new WalletStore();
            if (!walletStore.TrySpendHeart())
            {
                cubeController.SetUserInputEnabled(false);
                SetState(
                    IsTargetPatternStage(runtime.stage)
                        ? StagePlayState.TargetIntro
                        : StagePlayState.Ready,
                    "Not enough hearts. Get more hearts from Shop or Daily Rewards.");
                return;
            }

            assistState = StageAssistState.Create(runtime.stage);
            lastHint = null;
            runtime.elapsedSeconds = 0f;
            runtime.currentMoves = 0;
            runtime.moveLimit = assistState.currentMoveLimit;
            runtime.remainingMoves = runtime.moveLimit;
            runtime.isCompleted = false;
            runtime.isFailed = false;
            if (!IsTargetPatternStage(runtime.stage))
            {
                targetState = null;
            }
            earnedStars = 0;
            earnedCoins = 0;
            baseStars = 0;
            previousBestStars = progressStore.GetProgress(runtime.stage.stageId).stars;
            maxStarsAllowed = 3;
            bestStarsUpdated = false;
            cubeController.SetUserInputEnabled(false);
            cubeController.SetViewVisible(true);
            SetState(StagePlayState.Preparing, "Preparing stage...");

            if (IsTargetPatternStage(runtime.stage))
            {
                StartReverseTargetStage();
                return;
            }

            if (TryApplyStartFacelets(runtime.stage.startStateFacelets))
            {
                BeginPlaying();
                return;
            }

            if (!TryParseMoves(runtime.stage.scrambleNotation, out IReadOnlyList<CubeMove> scrambleMoves))
            {
                SetState(StagePlayState.NotPlayable, "Stage scramble is invalid.");
                return;
            }

            // Stage setup moves are not user moves. A later polish pass can animate
            // this scramble differently without changing StageProgress rules.
            cubeController.ApplyScrambleFromSolved(scrambleMoves);
        }

        private void StartReverseTargetStage()
        {
            if (targetState == null && !TryBuildTargetState(runtime.stage, out targetState))
            {
                SetState(StagePlayState.NotPlayable, "Target state is invalid.");
                return;
            }

            CubeState startState = CubeState.CreateSolved();
            if (!string.IsNullOrWhiteSpace(runtime.stage.startStateFacelets))
            {
                try
                {
                    startState = CubeStateSerializer.FromFaceletString(runtime.stage.startStateFacelets);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Reverse target startStateFacelets parse failed. Using solved start. {exception.Message}");
                }
            }

            cubeController.SetStateInstant(startState, true);
            if (cubeController.ViewRoot != null)
            {
                cubeController.ViewRoot.localRotation = Quaternion.identity;
            }

            BeginPlaying();
        }

        public void RetryStage()
        {
            StartStage();
        }

        public void ExitToStageList()
        {
            CubeChallenge3D.Core.GameLaunchContext.RequestStageListOnMainMenu();
            CubeChallenge3D.Core.SceneLoader.LoadMainMenu();
        }

        public void LoadNextStage()
        {
            StageData next = GetNextPlayableStage();
            if (next == null)
            {
                ExitToStageList();
                return;
            }

            StageData currentStage = runtime.stage;
            StageInterstitialPolicy.TryShowBeforeNextStage(currentStage, () =>
            {
                LoadStage(next);
                StartStage();
            });
        }

        public bool HasNextPlayableStage()
        {
            return GetNextPlayableStage() != null;
        }

        public StageData PeekNextPlayableStage()
        {
            return GetNextPlayableStage();
        }

        private int GetCurrentProgressStageNumber()
        {
            if (runtime.stage == null)
            {
                return 0;
            }

            IReadOnlyList<StageData> stages = dataLoader.LoadAllStages()
                .Where(stage => stage != null && stage.stageType == runtime.stage.stageType)
                .OrderBy(stage => stage.stageNumber)
                .ToList();
            int highestClearedStage = 0;
            foreach (StageData stage in stages)
            {
                if (progressStore.GetProgress(stage.stageId).isCleared)
                {
                    highestClearedStage = Mathf.Max(highestClearedStage, stage.stageNumber);
                }
            }

            StageData next = stages.FirstOrDefault(stage => stage.stageNumber > highestClearedStage);
            return next != null ? next.stageNumber : highestClearedStage;
        }

        [ContextMenu("DEV Force Clear Stage")]
        public void ForceClearForDebug()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (cubeController == null
                || cubeController.IsBusy
                || runtime.stage == null
                || state == StagePlayState.Cleared
                || state == StagePlayState.NotPlayable)
            {
                return;
            }

            CubeState completedState = IsTargetPatternStage(runtime.stage)
                ? targetState?.Clone()
                : CubeState.CreateSolved();
            if (completedState == null)
            {
                Debug.LogWarning("DEV stage clear failed because the target state is unavailable.");
                return;
            }

            // DEV only: use the normal clear pipeline with the current moves
            // and assist state so the resulting stars match the real rules.
            cubeController.SetStateInstant(completedState, true);
            cubeController.SetViewVisible(true);
            runtime.isFailed = false;
            runtime.isPlaying = false;
            CompleteStage();
            Debug.Log($"DEV force-cleared stage: {runtime.stage.stageId} moves={runtime.currentMoves} stars={earnedStars}. Remove this shortcut before release.");
#endif
        }

        public void SetTargetPreviewOpen(bool open)
        {
            if (!IsReverseTargetStage || state == StagePlayState.Cleared || state == StagePlayState.Failed)
            {
                return;
            }

            if (open)
            {
                cubeController?.SetUserInputEnabled(false);
                cubeController?.SetViewVisible(false);
                SetState(StagePlayState.TargetPreviewPopup, "View target.");
            }
            else
            {
                bool resumePlaying = runtime.isPlaying && !runtime.isCompleted && !runtime.isFailed;
                cubeController?.SetViewVisible(true);
                cubeController?.SetUserInputEnabled(resumePlaying);
                SetState(resumePlaying ? StagePlayState.Playing : StagePlayState.TargetIntro, resumePlaying ? "Make the target pattern." : "Make this target.");
            }
        }

        public bool CanContinueAfterAd()
        {
            return state == StagePlayState.Failed
                && assistState != null
                && assistState.adContinueCount < StageContinueMaxPerRun
                && rewardService != null
                && rewardService.CanShow(RewardedAdPlacement.OutOfMovesPlus2);
        }

        public bool CanRequestContinueAfterAd()
        {
            return state == StagePlayState.Failed
                && assistState != null
                && assistState.adContinueCount < StageContinueMaxPerRun
                && rewardService != null
                && !rewardService.IsShowingAd
                && rewardService.IsPlacementAvailable(RewardedAdPlacement.OutOfMovesPlus2);
        }

        public int GetCurrentPotentialStars()
        {
            if (runtime.stage == null)
            {
                return 0;
            }

            if (state == StagePlayState.Cleared)
            {
                return Mathf.Clamp(earnedStars, 0, 3);
            }

            int basePreviewStars = CalculateStars(runtime.stage, Mathf.Max(0, runtime.currentMoves));
            int assistUses = assistState != null ? assistState.assistUseCount : 0;
            int cappedStars = runtime.stage.stageType == StageType.TutorialStage
                ? basePreviewStars
                : StageContinuePolicy.ApplyAssistStarCap(basePreviewStars, assistUses);
            return Mathf.Clamp(Mathf.Max(1, cappedStars), 1, 3);
        }

        public string GetStarConditionsSummary()
        {
            if (runtime.stage == null)
            {
                return "Star conditions are unavailable.";
            }

            int minMoves = runtime.stage.minimumMoves > 0 ? runtime.stage.minimumMoves : runtime.stage.minMoveCount;
            int threeStarLimit = runtime.stage.starMoveLimit3 > 0 ? runtime.stage.starMoveLimit3 : minMoves;
            int twoStarLimit = runtime.stage.starMoveLimit2 > 0 ? runtime.stage.starMoveLimit2 : minMoves + 2;
            int oneStarLimit = runtime.stage.starMoveLimit1 > 0 ? runtime.stage.starMoveLimit1 : runtime.stage.moveLimit;

            return $"3 Stars\n- Within {threeStarLimit} moves\n- No help items used\n\n"
                + $"2 Stars\n- Within {twoStarLimit} moves\n- Or after using 1-2 help items\n\n"
                + $"1 Star\n- Clear within {oneStarLimit} moves\n- Or after additional help item penalties";
        }

        public int GetThreeStarMoveLimit()
        {
            if (runtime.stage == null)
            {
                return 0;
            }

            int minMoves = runtime.stage.minimumMoves > 0 ? runtime.stage.minimumMoves : runtime.stage.minMoveCount;
            return runtime.stage.starMoveLimit3 > 0 ? runtime.stage.starMoveLimit3 : minMoves;
        }

        public void ContinueAfterAd()
        {
            ContinueAfterAd(null);
        }

        public void ContinueAfterAd(Action<RewardedAdResult> onCompleted)
        {
            if (state != StagePlayState.Failed || assistState == null)
            {
                onCompleted?.Invoke(RewardedAdResult.Unavailable);
                return;
            }

            if (assistState.adContinueCount >= StageContinueMaxPerRun)
            {
                SetState(StagePlayState.Failed, "Stage continue limit reached.");
                onCompleted?.Invoke(RewardedAdResult.LimitReached);
                return;
            }

            if (rewardService == null || !rewardService.IsPlacementAvailable(RewardedAdPlacement.OutOfMovesPlus2))
            {
                SetState(StagePlayState.Failed, "Ad is not available yet.");
                onCompleted?.Invoke(RewardedAdResult.Unavailable);
                return;
            }

            rewardService.Show(
                RewardedAdPlacement.OutOfMovesPlus2,
                () =>
                {
                    if (state != StagePlayState.Failed)
                    {
                        return;
                    }

                    int moveLimitBefore = runtime.moveLimit;
                    StageContinuePolicy.ApplyAdContinue(assistState, StageContinueMovesReward);
                    runtime.moveLimit = assistState.currentMoveLimit;
                    runtime.remainingMoves = Mathf.Max(0, runtime.moveLimit - runtime.currentMoves);
                    runtime.isFailed = false;
                    runtime.isPlaying = true;
                    cubeController.SetUserInputEnabled(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log(
                        $"[RewardedExtraMoves] stageMode={runtime.stage?.stageType} stageNumber={runtime.stage?.stageNumber} "
                        + $"movesUsed={runtime.currentMoves} moveLimitBefore={moveLimitBefore} moveLimitAfter={runtime.moveLimit} "
                        + $"extraMovesUsed={assistState.adContinueCount} rewardReceived=True popupClosed=True resumeState=Playing");
#endif
                    SetState(StagePlayState.Playing, $"Reward claimed. Continued with +{StageContinueMovesReward} moves.");
                },
                result =>
                {
                    if (result != RewardedAdResult.Rewarded && state == StagePlayState.Failed)
                    {
                        SetState(StagePlayState.Failed, result == RewardedAdResult.NotReady
                            ? "Ad failed to load."
                            : "Ad not completed.");
                    }

                    onCompleted?.Invoke(result);
                });
        }

        public void UseUndoAssist()
        {
            if (state != StagePlayState.Playing || cubeController == null || cubeController.IsBusy || !cubeController.MoveHistory.CanUndo)
            {
                return;
            }

            if (assistState != null && assistState.freeUndoRemaining > 0)
            {
                assistState.freeUndoRemaining--;
                cubeController.Undo();
                SetState(StagePlayState.Playing, "Undo used.");
                return;
            }

            if (inventoryStore != null && inventoryStore.TryConsume(StageAssistItemType.Undo))
            {
                if (assistState != null)
                {
                    assistState.paidUndoUsed++;
                }

                cubeController.Undo();
                SetState(StagePlayState.Playing, "Undo item used.");
            }
        }

        public void UseMoveItem(StageAssistItemType itemType)
        {
            if (state != StagePlayState.Playing || assistState == null || inventoryStore == null)
            {
                return;
            }

            int bonus = GetMoveItemBonus(itemType);
            if (bonus <= 0 || !inventoryStore.TryConsume(itemType))
            {
                return;
            }

            assistState.moveItemUseCount++;
            assistState.assistUseCount++;
            assistState.bonusMovesAdded += bonus;
            assistState.currentMoveLimit += bonus;
            assistState.usedMoveItem = true;
            runtime.moveLimit = assistState.currentMoveLimit;
            runtime.remainingMoves = Mathf.Max(0, runtime.moveLimit - runtime.currentMoves);
            SetState(StagePlayState.Playing, $"+{bonus} move item used.");
        }

        public void RequestHint()
        {
            if (state != StagePlayState.Playing || runtime.stage == null || cubeController == null)
            {
                return;
            }

            if (assistState == null)
            {
                assistState = StageAssistState.Create(runtime.stage);
            }

            assistState.hintCount++;
            assistState.assistUseCount++;
            assistState.usedHint = true;
            lastHint = StageHintPolicy.BuildHint(runtime.stage, cubeController.MoveHistory.GetMoves(), runtime.remainingMoves);
            SetState(StagePlayState.Playing, lastHint.message);
        }

        private void Update()
        {
            if (state == StagePlayState.Playing)
            {
                runtime.elapsedSeconds += Time.deltaTime;
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
            if (state == StagePlayState.Preparing)
            {
                BeginPlaying();
            }
        }

        private void HandleMoveCompleted(CubeMove move)
        {
            if (state != StagePlayState.Playing)
            {
                return;
            }

            runtime.currentMoves = cubeController.UserMoveCount;
            runtime.remainingMoves = Mathf.Max(0, runtime.moveLimit - runtime.currentMoves);
            CheckSuccessOrFailure();
        }

        private void CheckSuccessOrFailure()
        {
            bool goalReached = IsStageGoalReached();
            if (goalReached)
            {
                CompleteStage();
                return;
            }

            if (runtime.currentMoves >= runtime.moveLimit)
            {
                FailStage();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log(
                    $"[StageMoveLimit] mode={runtime.stage?.stageType} stage={runtime.stage?.stageNumber} "
                    + $"movesUsed={runtime.currentMoves} moveLimit={runtime.moveLimit} "
                    + $"beforeInputBlocked=False moveApplied=True goalReached=False showOutOfMoves=True");
#endif
            }
        }

        private void CompleteStage()
        {
            runtime.isPlaying = false;
            runtime.isCompleted = true;
            StageProgress previousProgress = progressStore.GetProgress(runtime.stage.stageId);
            previousBestStars = previousProgress != null ? previousProgress.stars : 0;
            baseStars = CalculateStars(runtime.stage, runtime.currentMoves);
            int assistUses = assistState != null ? assistState.assistUseCount : 0;
            maxStarsAllowed = runtime.stage.stageType == StageType.TutorialStage
                ? 3
                : assistUses >= 3 ? 1 : assistUses >= 1 ? 2 : 3;
            earnedStars = runtime.stage.stageType == StageType.TutorialStage
                ? baseStars
                : StageContinuePolicy.ApplyAssistStarCap(baseStars, assistUses);
            bestStarsUpdated = earnedStars > previousBestStars;
            earnedCoins = GetClearCoinReward(earnedStars, previousBestStars);
            cubeController.SetUserInputEnabled(false);
            progressStore.MarkCleared(runtime.stage.stageId, runtime.currentMoves, runtime.elapsedSeconds, earnedStars);
            StageInterstitialPolicy.RecordStageClear(runtime.stage);
            if (earnedCoins > 0)
            {
                walletStore?.AddCoins(earnedCoins);
            }
            AudioFeedbackManager.PlayClearVibration();
            UnlockNextStage();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log(
                $"[StageReward] mode={runtime.stage.stageType} stage={runtime.stage.stageNumber} "
                + $"earnedStars={earnedStars} previousBestStars={previousBestStars} "
                + $"coinReward={earnedCoins} isFirstClear={previousBestStars <= 0} isUpgrade={earnedStars > previousBestStars}");
#endif
            SetState(StagePlayState.Cleared, "Clear!");
        }

        private void FailStage()
        {
            runtime.isPlaying = false;
            runtime.isFailed = true;
            cubeController.SetUserInputEnabled(false);
            SetState(StagePlayState.Failed, "Out of moves");
        }

        private void BeginPlaying()
        {
            runtime.startedAt = DateTime.UtcNow;
            runtime.elapsedSeconds = 0f;
            runtime.currentMoves = 0;
            runtime.remainingMoves = runtime.moveLimit;
            runtime.isPlaying = true;
            cubeController.SetUserInputEnabled(true);
            string message = IsTargetPatternStage(runtime.stage)
                ? "Make the target pattern."
                : "Solve the cube.";
            SetState(StagePlayState.Playing, message);
        }

        private bool IsStageGoalReached()
        {
            if (cubeController.CurrentState == null || runtime.stage == null)
            {
                return false;
            }

            if (IsTargetPatternStage(runtime.stage))
            {
                return targetState != null && cubeController.CurrentState.Equals(targetState);
            }

            return cubeController.CurrentState.IsSolved();
        }

        private bool TryApplyStartFacelets(string facelets)
        {
            if (string.IsNullOrWhiteSpace(facelets))
            {
                return false;
            }

            try
            {
                CubeState stateFromFacelets = CubeStateSerializer.FromFaceletString(facelets);
                cubeController.SetStateInstant(stateFromFacelets, true);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Stage startStateFacelets parse failed. Falling back to scrambleNotation. {exception.Message}");
                return false;
            }
        }

        private static bool TryParseMoves(string notation, out IReadOnlyList<CubeMove> moves)
        {
            try
            {
                moves = MoveUtility.ParseSequence(notation);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Stage move notation parse failed: {exception.Message}");
                moves = Array.Empty<CubeMove>();
                return false;
            }
        }

        private static bool TryBuildTargetState(StageData stage, out CubeState target)
        {
            target = null;
            if (stage == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(stage.targetStateFacelets))
            {
                try
                {
                    target = CubeStateSerializer.FromFaceletString(stage.targetStateFacelets);
                    return true;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"targetStateFacelets parse failed: {exception.Message}");
                    return false;
                }
            }

            string notation = !string.IsNullOrWhiteSpace(stage.solutionNotation)
                ? stage.solutionNotation
                : stage.scrambleNotation;
            if (!TryParseMoves(notation, out IReadOnlyList<CubeMove> moves))
            {
                return false;
            }

            target = CubeState.CreateSolved();
            target.ApplyMoves(moves);
            return true;
        }

        private int CalculateStars(StageData stage, int moves)
        {
            int minMoves = stage.minimumMoves > 0 ? stage.minimumMoves : stage.minMoveCount;
            int threeStarLimit = stage.starMoveLimit3 > 0 ? stage.starMoveLimit3 : minMoves;
            int twoStarLimit = stage.starMoveLimit2 > 0 ? stage.starMoveLimit2 : minMoves + 2;
            int oneStarLimit = stage.starMoveLimit1 > 0 ? stage.starMoveLimit1 : stage.moveLimit;

            if (moves <= threeStarLimit)
            {
                return 3;
            }

            if (moves <= twoStarLimit)
            {
                return 2;
            }

            return moves <= oneStarLimit || moves <= stage.moveLimit ? 1 : 0;
        }

        private static int GetMoveItemBonus(StageAssistItemType itemType)
        {
            switch (itemType)
            {
                case StageAssistItemType.MovePlus1:
                    return 1;
                case StageAssistItemType.MovePlus2:
                    return 2;
                case StageAssistItemType.MovePlus3:
                    return 3;
                default:
                    return 0;
            }
        }

        private static int GetClearCoinReward(int earnedStars, int previousBestStars)
        {
            int earnedReward = EconomyBalanceConfig.GetStageClearCoinsForStars(earnedStars);
            int previousReward = EconomyBalanceConfig.GetStageClearCoinsForStars(previousBestStars);
            return Mathf.Max(0, earnedReward - previousReward);
        }

        private void UnlockNextStage()
        {
            StageData next = GetNextStage();
            if (next != null)
            {
                progressStore.UnlockStage(next.stageId);
            }
        }

        private StageData GetNextPlayableStage()
        {
            StageData next = GetNextStage();
            if (next == null || !progressStore.IsUnlocked(next.stageId))
            {
                return null;
            }

            return next;
        }

        private StageData GetNextStage()
        {
            if (runtime.stage == null)
            {
                return null;
            }

            return dataLoader.LoadAllStages()
                .Where(stage => stage.stageType == runtime.stage.stageType
                    && stage.stageNumber > runtime.stage.stageNumber)
                .OrderBy(stage => stage.stageNumber)
                .FirstOrDefault();
        }

        private static bool IsPlayableStageType(StageType type)
        {
            return type == StageType.TutorialStage
                || type == StageType.SolveStage
                || type == StageType.ReverseTargetStage
                || type == StageType.InfinityStage;
        }

        private static string BuildTargetSummary(StageData stage)
        {
            if (stage == null)
            {
                return string.Empty;
            }

            if (!IsTargetPatternStage(stage))
            {
                return "Goal: Solve the cube";
            }

            return "Goal: Make the target pattern";
        }

        public static bool IsTargetPatternStage(StageData stage)
        {
            return stage != null
                && (stage.stageType == StageType.ReverseTargetStage
                    || stage.stageType == StageType.InfinityStage && string.IsNullOrWhiteSpace(stage.scrambleNotation));
        }

        private void SetState(StagePlayState nextState, string message)
        {
            state = nextState;
            statusMessage = message;
            StateChanged?.Invoke();
        }
    }
}
