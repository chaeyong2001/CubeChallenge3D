using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Cube.Debugging;
using CubeChallenge3D.Cube.Input;
using CubeChallenge3D.GameModes.QuickPlay;
using CubeChallenge3D.GameModes.RankingChallenge;
using CubeChallenge3D.GameModes.Stages;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CubeChallenge3D.Cube.Runtime
{
    public sealed class CubeDevInput : MonoBehaviour
    {
        [SerializeField] private CubeController controller;
        [SerializeField] private CubeRuntimeDiagnostics diagnostics;
        [SerializeField] private CubeViewOrbitController orbitController;
        [SerializeField] private CubeControlModeController controlModeController;
        [SerializeField] private QuickPlayGameMode quickPlay;
        [SerializeField] private RankingChallengeGameMode rankingChallenge;
        [SerializeField] private StagePlayGameMode stagePlay;
        [SerializeField] private bool allowUndo = true;

        public void Initialize(
            CubeController cubeController,
            CubeRuntimeDiagnostics runtimeDiagnostics,
            CubeViewOrbitController viewOrbitController,
            CubeControlModeController controlController,
            QuickPlayGameMode quickPlayGameMode,
            RankingChallengeGameMode rankingChallengeGameMode = null,
            StagePlayGameMode stagePlayGameMode = null,
            bool enableUndo = true)
        {
            controller = cubeController;
            diagnostics = runtimeDiagnostics;
            orbitController = viewOrbitController;
            controlModeController = controlController;
            quickPlay = quickPlayGameMode;
            rankingChallenge = rankingChallengeGameMode;
            stagePlay = stagePlayGameMode;
            allowUndo = enableUndo;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || controller == null)
            {
                return;
            }

            // Development-only shortcuts. These will be replaced by production input later.
            if (keyboard.cKey.wasPressedThisFrame)
            {
                diagnostics?.LogSummary();
                return;
            }

            if (keyboard.vKey.wasPressedThisFrame)
            {
                diagnostics?.LogValidation();
                return;
            }

            if (keyboard.tKey.wasPressedThisFrame)
            {
                diagnostics?.RunSelfCheck();
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // DEV force clear must never be available in release builds.
            if (keyboard.yKey.wasPressedThisFrame)
            {
                if (quickPlay != null)
                {
                    quickPlay.ForceClearForDebug();
                }
                else if (stagePlay != null)
                {
                    stagePlay.ForceClearForDebug();
                }
                else
                {
                    rankingChallenge?.ForceClearForDebug();
                }

                return;
            }
#endif

            if (keyboard.mKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            {
                orbitController?.ToggleMode();
                return;
            }

            if (keyboard.nKey.wasPressedThisFrame)
            {
                orbitController?.SetViewMode();
                return;
            }

            if (keyboard.kKey.wasPressedThisFrame)
            {
                orbitController?.SetSolveMode();
                return;
            }

            if (keyboard.oKey.wasPressedThisFrame)
            {
                orbitController?.ResetViewRotation();
                return;
            }

            // K is already used for Solve Mode, so G/H/J control Drag/Keypad/Toggle.
            if (keyboard.gKey.wasPressedThisFrame)
            {
                controlModeController?.SetDragControlMode();
                return;
            }

            if (keyboard.hKey.wasPressedThisFrame)
            {
                controlModeController?.SetKeypadControlMode();
                return;
            }

            if (keyboard.jKey.wasPressedThisFrame)
            {
                controlModeController?.ToggleControlMode();
                return;
            }

            if (controller.IsBusy || (orbitController != null && orbitController.IsSnapping))
            {
                return;
            }

            if (keyboard.iKey.wasPressedThisFrame)
            {
                controller.ReplayInverseLastScramble();
                return;
            }

            if (allowUndo && (keyboard.zKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame))
            {
                controller.Undo();
                return;
            }

            if (keyboard.sKey.wasPressedThisFrame)
            {
                if (quickPlay != null)
                {
                    quickPlay.StartNewGame();
                }
                else
                {
                    bool shortScramble = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                    controller.Scramble(shortScramble ? 5 : 20);
                }
                return;
            }

            if (keyboard.xKey.wasPressedThisFrame)
            {
                if (quickPlay != null)
                {
                    quickPlay.ResetToReady();
                }
                else
                {
                    controller.ResetSolved();
                }
                return;
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                controller.ReplayInverseLastScramble();
                return;
            }

            int turns = GetQuarterTurns(keyboard);
            if (TryGetPressedFace(keyboard, out CubeFace face))
            {
                controller.ApplyUserMove(new CubeMove(face, turns));
            }
        }

        private static int GetQuarterTurns(Keyboard keyboard)
        {
            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            {
                return 2;
            }

            return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? -1 : 1;
        }

        private static bool TryGetPressedFace(Keyboard keyboard, out CubeFace face)
        {
            if (keyboard.rKey.wasPressedThisFrame) { face = CubeFace.Right; return true; }
            if (keyboard.lKey.wasPressedThisFrame) { face = CubeFace.Left; return true; }
            if (keyboard.uKey.wasPressedThisFrame) { face = CubeFace.Up; return true; }
            if (keyboard.dKey.wasPressedThisFrame) { face = CubeFace.Down; return true; }
            if (keyboard.fKey.wasPressedThisFrame) { face = CubeFace.Front; return true; }
            if (keyboard.bKey.wasPressedThisFrame) { face = CubeFace.Back; return true; }

            face = default;
            return false;
        }
    }
}
