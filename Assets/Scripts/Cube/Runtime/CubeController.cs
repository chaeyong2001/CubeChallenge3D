using System;
using System.Collections;
using System.Collections.Generic;
using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Utils;
using CubeChallenge3D.Cube.View;
using UnityEngine;

namespace CubeChallenge3D.Cube.Runtime
{
    public sealed class CubeController : MonoBehaviour
    {
        [SerializeField] private CubeVisualBuilder visualBuilder;
        [SerializeField] private float rotationDuration = 0.2f;

        private readonly MoveHistory moveHistory = new MoveHistory();
        private readonly List<CubeMove> lastScrambleMoves = new List<CubeMove>();

        public CubeState CurrentState { get; private set; }
        public bool IsRotating { get; private set; }
        public bool IsSequenceRunning { get; private set; }
        public bool IsBusy => IsRotating || IsSequenceRunning;
        public bool UserInputEnabled { get; private set; } = true;
        public MoveHistory MoveHistory => moveHistory;
        public IReadOnlyList<CubeMove> LastScrambleMoves => lastScrambleMoves.AsReadOnly();
        public int UserMoveCount => moveHistory.Count;
        public CubeMove? LastMove { get; private set; }
        public Transform ViewRoot => visualBuilder != null ? visualBuilder.ViewRoot : null;
        public Transform CubeRoot => visualBuilder != null ? visualBuilder.CubeRoot : null;

        public event Action<int> UserMoveCountChanged;
        public event Action Solved;
        public event Action<CubeMove> MoveAnimationCompleted;
        public event Action<CubeMove> MoveCommandCompleted;
        public event Action<CubeMove> UserMoveApplied;
        public event Action<CubeMove> UndoApplied;
        public event Action ScrambleCompleted;

        public void InitializeSolved()
        {
            ResetSolved();
        }

        public void ResetSolved()
        {
            if (IsBusy)
            {
                return;
            }

            ResetSolvedInternal(true);
        }

        public void SetStateInstant(CubeState state, bool clearHistory = true)
        {
            if (IsBusy || state == null)
            {
                return;
            }

            EnsureVisualBuilder();
            CurrentState = state.Clone();
            visualBuilder.Build(CurrentState);
            if (clearHistory)
            {
                moveHistory.Clear();
                LastMove = null;
                NotifyMoveCountChanged();
            }
        }

        public void ApplyUserMove(CubeMove move)
        {
            if (!UserInputEnabled || IsBusy || CurrentState == null)
            {
                return;
            }

            StartCoroutine(ApplySingleMove(move, true));
        }

        public void ApplySystemMove(CubeMove move)
        {
            if (IsBusy || CurrentState == null)
            {
                return;
            }

            StartCoroutine(ApplySingleMove(move, false));
        }

        public void Undo()
        {
            if (!UserInputEnabled || IsBusy || !moveHistory.TryPopLast(out CubeMove move))
            {
                return;
            }

            NotifyMoveCountChanged();
            UndoApplied?.Invoke(move);
            StartCoroutine(ApplySingleMove(MoveUtility.Inverse(move), false));
        }

        public void Scramble(int length = 20, int? seed = null)
        {
            if (IsBusy)
            {
                return;
            }

            List<CubeMove> moves = ScrambleGenerator.Generate(length, seed);
            StartCoroutine(ScrambleSequence(moves));
        }

        public void ApplyScrambleFromSolved(IEnumerable<CubeMove> moves)
        {
            if (IsBusy || moves == null)
            {
                return;
            }

            StartCoroutine(ScrambleFromSolvedSequence(new List<CubeMove>(moves)));
        }

        public void SetUserInputEnabled(bool enabled)
        {
            UserInputEnabled = enabled;
        }

        public void SetRotationDuration(float seconds)
        {
            rotationDuration = Mathf.Max(0.01f, seconds);
        }

        public void SetViewVisible(bool visible)
        {
            if (ViewRoot != null)
            {
                ViewRoot.gameObject.SetActive(visible);
            }
        }

        public void ReplayMoves(IEnumerable<CubeMove> moves)
        {
            if (IsBusy || moves == null || CurrentState == null)
            {
                return;
            }

            StartCoroutine(ReplaySequence(new List<CubeMove>(moves)));
        }

        public void ReplayInverseLastScramble()
        {
            if (IsBusy || lastScrambleMoves.Count == 0)
            {
                return;
            }

            ReplayMoves(MoveUtility.InverseSequence(lastScrambleMoves));
        }

        public void ApplyMoveAnimated(CubeMove move)
        {
            ApplyUserMove(move);
        }

        private void ResetSolvedInternal(bool clearScramble)
        {
            EnsureVisualBuilder();
            CurrentState = CubeState.CreateSolved();
            visualBuilder.Build(CurrentState);
            moveHistory.Clear();
            LastMove = null;
            if (clearScramble)
            {
                lastScrambleMoves.Clear();
            }

            NotifyMoveCountChanged();
        }

        private IEnumerator ApplySingleMove(CubeMove move, bool recordUserMove)
        {
            yield return RotateFace(move);
            if (recordUserMove)
            {
                moveHistory.Add(move);
                NotifyMoveCountChanged();
                UserMoveApplied?.Invoke(move);
            }

            MoveCommandCompleted?.Invoke(move);
        }

        private IEnumerator ScrambleSequence(List<CubeMove> moves)
        {
            IsSequenceRunning = true;
            lastScrambleMoves.Clear();
            lastScrambleMoves.AddRange(moves);

            // Scramble the current state. Replaying its inverse returns to the
            // exact state that existed immediately before the scramble.
            foreach (CubeMove move in moves)
            {
                yield return RotateFace(move);
            }

            moveHistory.Clear();
            NotifyMoveCountChanged();
            IsSequenceRunning = false;
            ScrambleCompleted?.Invoke();
        }

        private IEnumerator ScrambleFromSolvedSequence(List<CubeMove> moves)
        {
            IsSequenceRunning = true;
            ResetSolvedInternal(false);
            lastScrambleMoves.Clear();
            lastScrambleMoves.AddRange(moves);

            foreach (CubeMove move in moves)
            {
                yield return RotateFace(move);
            }

            moveHistory.Clear();
            NotifyMoveCountChanged();
            IsSequenceRunning = false;
            ScrambleCompleted?.Invoke();
        }

        private IEnumerator ReplaySequence(List<CubeMove> moves)
        {
            IsSequenceRunning = true;
            foreach (CubeMove move in moves)
            {
                yield return RotateFace(move);
            }

            IsSequenceRunning = false;
        }

        private IEnumerator RotateFace(CubeMove move)
        {
            IsRotating = true;

            Transform cubeRoot = visualBuilder.CubeRoot;
            if (cubeRoot == null)
            {
                IsRotating = false;
                yield break;
            }

            List<CubieVisual> targets = FindTargetCubies(cubeRoot, move.Face);
            Transform pivot = new GameObject($"Pivot_{move}").transform;
            pivot.SetParent(cubeRoot, false);

            foreach (CubieVisual cubie in targets)
            {
                cubie.transform.SetParent(pivot, true);
            }

            // Clockwise means clockwise while looking directly at the face from outside.
            // That is a negative rotation around the face's outward normal.
            Vector3 axis = CubeFaceletMapping.FaceNormal(move.Face);
            float targetAngle = -90f * move.QuarterTurns;
            Quaternion startRotation = Quaternion.identity;
            Quaternion endRotation = Quaternion.AngleAxis(targetAngle, axis);

            float elapsed = 0f;
            while (elapsed < rotationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = rotationDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / rotationDuration);
                pivot.localRotation = Quaternion.Slerp(startRotation, endRotation, progress);
                yield return null;
            }

            pivot.localRotation = endRotation;
            foreach (CubieVisual cubie in targets)
            {
                cubie.transform.SetParent(cubeRoot, true);
                Vector3Int nextGridPosition = RotateGridPosition(cubie.CurrentGridPosition, move);
                cubie.SetCurrentGridPosition(nextGridPosition);
                SnapCubie(cubie, nextGridPosition);
            }

            Destroy(pivot.gameObject);

            // The view animation finishes first; then the authoritative model advances.
            CurrentState.ApplyMove(move);
            LastMove = move;
            IsRotating = false;
            MoveAnimationCompleted?.Invoke(move);

            if (CurrentState.IsSolved())
            {
                Solved?.Invoke();
            }
        }

        private void NotifyMoveCountChanged()
        {
            UserMoveCountChanged?.Invoke(moveHistory.Count);
        }

        private void EnsureVisualBuilder()
        {
            if (visualBuilder == null)
            {
                visualBuilder = GetComponent<CubeVisualBuilder>();
            }

            if (visualBuilder == null)
            {
                visualBuilder = gameObject.AddComponent<CubeVisualBuilder>();
            }
        }

        private static List<CubieVisual> FindTargetCubies(Transform cubeRoot, CubeFace face)
        {
            CubieVisual[] cubies = cubeRoot.GetComponentsInChildren<CubieVisual>();
            var result = new List<CubieVisual>(9);
            foreach (CubieVisual cubie in cubies)
            {
                if (IsOnFace(cubie.CurrentGridPosition, face))
                {
                    result.Add(cubie);
                }
            }

            return result;
        }

        private static bool IsOnFace(Vector3Int position, CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Right: return position.x == 1;
                case CubeFace.Left: return position.x == -1;
                case CubeFace.Up: return position.y == 1;
                case CubeFace.Down: return position.y == -1;
                case CubeFace.Front: return position.z == 1;
                case CubeFace.Back: return position.z == -1;
                default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        private static Vector3Int RotateGridPosition(Vector3Int position, CubeMove move)
        {
            Vector3Int axis = GetFaceAxis(move.Face);
            int clockwiseTurns = move.QuarterTurns == -1 ? 3 : move.QuarterTurns;
            Vector3Int result = position;
            for (int i = 0; i < clockwiseTurns; i++)
            {
                result = RotateClockwise(result, axis);
            }

            return result;
        }

        private static Vector3Int GetFaceAxis(CubeFace face)
        {
            Vector3 normal = CubeFaceletMapping.FaceNormal(face);
            return new Vector3Int(
                Mathf.RoundToInt(normal.x),
                Mathf.RoundToInt(normal.y),
                Mathf.RoundToInt(normal.z));
        }

        private static Vector3Int RotateClockwise(Vector3Int value, Vector3Int axis)
        {
            var cross = new Vector3Int(
                (axis.y * value.z) - (axis.z * value.y),
                (axis.z * value.x) - (axis.x * value.z),
                (axis.x * value.y) - (axis.y * value.x));
            int dot = (axis.x * value.x) + (axis.y * value.y) + (axis.z * value.z);
            return -cross + (axis * dot);
        }

        private void SnapCubie(CubieVisual cubie, Vector3Int gridPosition)
        {
            Transform cubieTransform = cubie.transform;
            cubieTransform.localPosition = (Vector3)gridPosition * visualBuilder.CubieSpacing;

            Vector3 euler = cubieTransform.localEulerAngles;
            cubieTransform.localRotation = Quaternion.Euler(
                SnapAngle(euler.x),
                SnapAngle(euler.y),
                SnapAngle(euler.z));

            cubie.name = $"Cubie_{gridPosition.x}_{gridPosition.y}_{gridPosition.z}";
        }

        private static float SnapAngle(float angle)
        {
            return Mathf.Round(angle / 90f) * 90f;
        }
    }
}
