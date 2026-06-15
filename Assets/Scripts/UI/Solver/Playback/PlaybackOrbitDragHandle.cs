using CubeChallenge3D.Cube.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CubeChallenge3D.UI.Solver.Playback
{
    public sealed class PlaybackOrbitDragHandle : MonoBehaviour, IDragHandler
    {
        private CubeController cubeController;
        private Camera renderCamera;
        private float sensitivity;

        public void Initialize(CubeController controller, Camera camera, float dragSensitivity = 0.3f)
        {
            cubeController = controller;
            renderCamera = camera;
            sensitivity = Mathf.Max(0.01f, dragSensitivity);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Transform viewRoot = cubeController != null ? cubeController.ViewRoot : null;
            if (viewRoot == null || cubeController.IsBusy)
            {
                return;
            }

            Vector3 pitchAxis = renderCamera != null ? renderCamera.transform.right : Vector3.right;
            Quaternion yaw = Quaternion.AngleAxis(-eventData.delta.x * sensitivity, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(eventData.delta.y * sensitivity, pitchAxis);
            viewRoot.rotation = pitch * yaw * viewRoot.rotation;
        }
    }
}
