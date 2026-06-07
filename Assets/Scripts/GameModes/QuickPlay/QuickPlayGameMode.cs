using System;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Cube.Utils;
using CubeChallenge3D.Save;
using CubeChallenge3D.Save.Records;
using UnityEngine;

namespace CubeChallenge3D.GameModes.QuickPlay
{
    public sealed class QuickPlayGameMode : MonoBehaviour
    {
        [SerializeField] private CubeController cubeController;
        [SerializeField] private CubeControlModeController controlModeController;
        [SerializeField, Min(1)] private int scrambleLength = 20;

        private readonly List<CubeMove> activeScramble = new List<CubeMove>();
        private QuickPlayState state = QuickPlayState.Ready;
        private float elapsedTime;
        private float solvedTime;
        private int solvedMoveCount;
        private bool hasSavedCurrentResult;
        private bool usedUndo;
        private QuickPlayRecordStore recordStore;

        public QuickPlayState State => state;
        public float ElapsedTime => state == QuickPlayState.Solved ? solvedTime : elapsedTime;
        public int MoveCount => state == QuickPlayState.Solved ? solvedMoveCount : cubeController?.UserMoveCount ?? 0;
        public IReadOnlyList<CubeMove> ActiveScramble => activeScramble.AsReadOnly();
        public QuickPlayRecordStore RecordStore => recordStore;

        public event Action<QuickPlayState> StateChanged;

        public void Initialize(CubeController controller)
        {
            Initialize(controller, null, null);
        }

        public void Initialize(
            CubeController controller,
            CubeControlModeController controlController,
            QuickPlayRecordStore store)
        {
            if (cubeController != null)
            {
                cubeController.MoveCommandCompleted -= HandleMoveCompleted;
                cubeController.ScrambleCompleted -= HandleScrambleCompleted;
                cubeController.UndoApplied -= HandleUndoApplied;
            }

            cubeController = controller;
            controlModeController = controlController;
            recordStore = store ?? new QuickPlayRecordStore();
            cubeController.MoveCommandCompleted += HandleMoveCompleted;
            cubeController.ScrambleCompleted += HandleScrambleCompleted;
            cubeController.UndoApplied += HandleUndoApplied;
            cubeController.SetUserInputEnabled(true);
            SetState(QuickPlayState.Ready);
        }

        public void StartNewGame()
        {
            if (!CanStartSequence())
            {
                return;
            }

            activeScramble.Clear();
            activeScramble.AddRange(ScrambleGenerator.Generate(scrambleLength));
            StartScramble();
        }

        public void Retry()
        {
            if (!CanStartSequence() || activeScramble.Count == 0)
            {
                return;
            }

            StartScramble();
        }

        public void ResetToReady()
        {
            if (cubeController == null || cubeController.IsBusy)
            {
                return;
            }

            cubeController.SetUserInputEnabled(true);
            cubeController.ResetSolved();
            activeScramble.Clear();
            elapsedTime = 0f;
            solvedTime = 0f;
            solvedMoveCount = 0;
            hasSavedCurrentResult = false;
            usedUndo = false;
            SetState(QuickPlayState.Ready);
        }

        public void SetPaused(bool paused)
        {
            if (paused && state == QuickPlayState.Playing)
            {
                cubeController.SetUserInputEnabled(false);
                SetState(QuickPlayState.Paused);
            }
            else if (!paused && state == QuickPlayState.Paused)
            {
                cubeController.SetUserInputEnabled(true);
                SetState(QuickPlayState.Playing);
            }
        }

        [ContextMenu("DEV Force Clear")]
        public void ForceClearForDebug()
        {
            if (cubeController == null || cubeController.IsBusy)
            {
                return;
            }

            solvedTime = elapsedTime;
            solvedMoveCount = cubeController.UserMoveCount;
            cubeController.SetUserInputEnabled(false);
            // Development-only shortcut. It still writes a record so local save UI can be tested quickly.
            SaveResultOnce();
            SetState(QuickPlayState.Solved);
        }

        [ContextMenu("DEV Clear Quick Play Records")]
        public void ClearRecordsForDebug()
        {
            recordStore?.ClearAll();
        }

        private void Update()
        {
            if (state == QuickPlayState.Playing)
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
            cubeController.UndoApplied -= HandleUndoApplied;
        }

        private bool CanStartSequence()
        {
            return cubeController != null && !cubeController.IsBusy;
        }

        private void StartScramble()
        {
            elapsedTime = 0f;
            solvedTime = 0f;
            solvedMoveCount = 0;
            hasSavedCurrentResult = false;
            usedUndo = false;
            cubeController.SetUserInputEnabled(false);
            SetState(QuickPlayState.Scrambling);
            cubeController.ApplyScrambleFromSolved(activeScramble);
        }

        private void HandleScrambleCompleted()
        {
            if (state != QuickPlayState.Scrambling)
            {
                return;
            }

            elapsedTime = 0f;
            cubeController.SetUserInputEnabled(true);
            SetState(QuickPlayState.Playing);
        }

        private void HandleMoveCompleted(CubeMove move)
        {
            if (state != QuickPlayState.Playing || !cubeController.CurrentState.IsSolved())
            {
                return;
            }

            solvedTime = elapsedTime;
            solvedMoveCount = cubeController.UserMoveCount;
            cubeController.SetUserInputEnabled(false);
            SaveResultOnce();
            SetState(QuickPlayState.Solved);
        }

        private void HandleUndoApplied(CubeMove move)
        {
            if (state == QuickPlayState.Playing)
            {
                usedUndo = true;
            }
        }

        private void SaveResultOnce()
        {
            if (hasSavedCurrentResult || recordStore == null)
            {
                return;
            }

            hasSavedCurrentResult = true;
            string controlMode = controlModeController != null
                ? controlModeController.CurrentControlMode.ToString()
                : string.Empty;
            QuickPlayResult result = QuickPlayResult.Create(
                solvedTime,
                solvedMoveCount,
                MoveUtility.ToNotationSequence(activeScramble),
                cubeController.MoveHistory.ToNotationString(),
                controlMode,
                usedUndo);
            recordStore.AddResult(result);
        }

        private void SetState(QuickPlayState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            StateChanged?.Invoke(state);
        }
    }
}
