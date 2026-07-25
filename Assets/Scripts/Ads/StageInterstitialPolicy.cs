using System;
using CubeChallenge3D.Stages.Generation;
using CubeChallenge3D.Stages.Model;
using UnityEngine;

namespace CubeChallenge3D.Ads
{
    public static class StageInterstitialPolicy
    {
        private const string ClearsSinceLastKey = "ads_stage_interstitial_clears_since_last";
        private const int FirstInterstitialStageNumber = 11;
        private const int DefaultClearInterval = 5;
        private const int LongSessionClearInterval = 4;
        private const int LongSessionMinutes = 20;
        private const int LongSessionClearCount = 20;

        private static readonly DateTime SessionStartedAtUtc = DateTime.UtcNow;
        private static int sessionEligibleClearCount;

        public static void RecordStageClear(StageData stage)
        {
            if (!IsEligibleStage(stage))
            {
                return;
            }

            int clearsSinceLast = Mathf.Max(0, PlayerPrefs.GetInt(ClearsSinceLastKey, 0)) + 1;
            PlayerPrefs.SetInt(ClearsSinceLastKey, clearsSinceLast);
            PlayerPrefs.Save();
            sessionEligibleClearCount++;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogPolicy(stage, "record_clear", clearsSinceLast, GetCurrentInterval(), false, false, false);
#endif
        }

        public static bool TryShowBeforeNextStage(StageData currentStage, Action onCompleted)
        {
            if (currentStage == null)
            {
                onCompleted?.Invoke();
                return false;
            }

            int clearsSinceLast = Mathf.Max(0, PlayerPrefs.GetInt(ClearsSinceLastKey, 0));
            int interval = GetCurrentInterval();
            int localStageNumber = GetLocalStageNumber(currentStage);
            bool thresholdReached = IsEligibleMode(currentStage.stageType)
                && localStageNumber >= FirstInterstitialStageNumber
                && clearsSinceLast >= interval;

            AdManager manager = AdManager.Instance;
            bool canShow = thresholdReached
                && manager.CanShowInterstitial(InterstitialPlacement.StageClearTransition);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogPolicy(currentStage, "before_next_stage", clearsSinceLast, interval, thresholdReached, manager.RemoveAdsPurchased, canShow);
#endif

            if (!canShow)
            {
                onCompleted?.Invoke();
                return false;
            }

            return manager.TryShowInterstitial(
                InterstitialPlacement.StageClearTransition,
                () =>
                {
                    PlayerPrefs.SetInt(ClearsSinceLastKey, 0);
                    PlayerPrefs.Save();
                    onCompleted?.Invoke();
                });
        }

        private static bool IsEligibleStage(StageData stage)
        {
            return stage != null
                && IsEligibleMode(stage.stageType)
                && GetLocalStageNumber(stage) >= FirstInterstitialStageNumber;
        }

        private static bool IsEligibleMode(StageType stageType)
        {
            return stageType == StageType.TutorialStage
                || stageType == StageType.SolveStage
                || stageType == StageType.ReverseTargetStage
                || stageType == StageType.InfinityStage;
        }

        private static int GetCurrentInterval()
        {
            return IsLongSession() ? LongSessionClearInterval : DefaultClearInterval;
        }

        private static bool IsLongSession()
        {
            return (DateTime.UtcNow - SessionStartedAtUtc).TotalMinutes >= LongSessionMinutes
                || sessionEligibleClearCount >= LongSessionClearCount;
        }

        private static int GetLocalStageNumber(StageData stage)
        {
            if (stage == null)
            {
                return 0;
            }

            if (stage.stageType == StageType.ReverseTargetStage)
            {
                return stage.stageNumber - StagePackGenerator.NormalStageCount;
            }

            if (stage.stageType == StageType.InfinityStage)
            {
                return stage.stageNumber - (StagePackGenerator.NormalStageCount + StagePackGenerator.HardStageCount);
            }

            if (stage.stageType == StageType.TutorialStage)
            {
                return stage.stageNumber - StagePackGenerator.TutorialFirstStageNumber + 1;
            }

            return stage.stageNumber;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static void LogPolicy(
            StageData stage,
            string eventName,
            int clearsSinceLast,
            int interval,
            bool thresholdReached,
            bool removeAds,
            bool adShowAttempted)
        {
            double sessionMinutes = (DateTime.UtcNow - SessionStartedAtUtc).TotalMinutes;
            int localStageNumber = GetLocalStageNumber(stage);
            bool longSession = IsLongSession();
            Debug.Log(
                $"[AdsPolicy] event={eventName}, selectedPlacement={InterstitialPlacement.StageClearTransition}, "
                + $"mode={stage.stageType}, stage={stage.stageNumber}, localStageNumber={localStageNumber}, "
                + $"eligibleClearCount={clearsSinceLast}, sessionEligibleClearCount={sessionEligibleClearCount}, "
                + $"sessionMinutes={sessionMinutes:0.0}, longSession={longSession}, requiredClearInterval={interval}, "
                + $"removeAdsPurchased={removeAds}, thresholdReached={thresholdReached}, adShowAttempted={adShowAttempted}");
        }
#endif
    }
}
