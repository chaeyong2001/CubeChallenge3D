using System;
using CubeChallenge3D.Cube.Runtime;
using CubeChallenge3D.Save;
using UnityEngine;

namespace CubeChallenge3D.Cube.Input
{
    public sealed class CubeControlModeController : MonoBehaviour
    {
        [SerializeField] private CubeController cubeController;
        [SerializeField] private CubeViewOrbitController orbitController;
        [SerializeField] private CubeControlMode currentControlMode = CubeControlMode.Drag;

        public CubeControlMode CurrentControlMode => currentControlMode;
        public event Action<CubeControlMode> ControlModeChanged;

        private SettingsStore settingsStore;

        public void Initialize(
            CubeController controller,
            CubeViewOrbitController viewOrbitController,
            SettingsStore store = null)
        {
            cubeController = controller;
            orbitController = viewOrbitController;
            settingsStore = store;
            ApplySavedControlMode();
        }

        public void SetDragControlMode()
        {
            SetControlMode(CubeControlMode.Drag);
        }

        public void SetKeypadControlMode()
        {
            SetControlMode(CubeControlMode.Keypad);
        }

        public void ToggleControlMode()
        {
            SetControlMode(currentControlMode == CubeControlMode.Drag
                ? CubeControlMode.Keypad
                : CubeControlMode.Drag);
        }

        private void SetControlMode(CubeControlMode mode)
        {
            if (cubeController == null
                || orbitController == null
                || cubeController.IsBusy
                || orbitController.IsSnapping)
            {
                return;
            }

            SetControlModeInternal(mode, true);
        }

        private void ApplySavedControlMode()
        {
            if (settingsStore == null || settingsStore.Current == null)
            {
                return;
            }

            if (Enum.TryParse(settingsStore.Current.controlMode, out CubeControlMode savedMode)
                && savedMode != CubeControlMode.Disabled)
            {
                SetControlModeInternal(savedMode, false);
            }
        }

        private void SetControlModeInternal(CubeControlMode mode, bool save)
        {
            if (currentControlMode == mode)
            {
                return;
            }

            currentControlMode = mode;
            ControlModeChanged?.Invoke(currentControlMode);
            if (save && settingsStore != null && settingsStore.Current != null)
            {
                settingsStore.Current.controlMode = currentControlMode.ToString();
                settingsStore.Save();
            }
        }
    }
}
