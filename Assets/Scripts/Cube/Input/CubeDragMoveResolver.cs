using System;
using CubeChallenge3D.Cube.Model;
using UnityEngine;

namespace CubeChallenge3D.Cube.Input
{
    public static class CubeDragMoveResolver
    {
        public static bool TryResolveLayer(
            CubeFace touchedFace,
            Vector3Int gridPosition,
            Vector2 screenDrag,
            Camera camera,
            Transform cubeRoot,
            bool invert,
            out LayerMoveDescriptor descriptor,
            out string ignoredReason)
        {
            descriptor = default;
            ignoredReason = null;
            if (camera == null || cubeRoot == null || screenDrag.sqrMagnitude <= 0f)
            {
                ignoredReason = "Missing camera/root or empty drag";
                return false;
            }

            Vector3 normal = GetFaceNormal(touchedFace);
            Vector3 worldDrag = (camera.transform.right * screenDrag.x)
                + (camera.transform.up * screenDrag.y);
            Vector3 localDrag = cubeRoot.InverseTransformDirection(worldDrag);
            Vector3 tangent = SnapToMajorAxis(Vector3.ProjectOnPlane(localDrag, normal), normal);
            if (tangent == Vector3.zero)
            {
                ignoredReason = "Could not resolve drag tangent";
                return false;
            }

            Vector3 angularDirection = Vector3.Cross((Vector3)gridPosition, tangent);
            CubeAxis axis = ResolveRotationAxis(angularDirection, normal);
            int layerIndex = GetAxisValue(gridPosition, axis);
            int turns = GetAxisValue(angularDirection, axis) >= 0f ? 1 : -1;
            if (invert)
            {
                turns = -turns;
            }

            descriptor = new LayerMoveDescriptor(axis, layerIndex, turns);
            if (descriptor.IsMiddleLayer)
            {
                ignoredReason = "Middle layer controls are disabled";
                return false;
            }

            return true;
        }

        private static Vector3 SnapToMajorAxis(Vector3 value, Vector3 excludedNormal)
        {
            value = Vector3.ProjectOnPlane(value, excludedNormal);
            if (value.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            Vector3 absolute = new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
            if (absolute.x >= absolute.y && absolute.x >= absolute.z) return new Vector3(Mathf.Sign(value.x), 0f, 0f);
            if (absolute.y >= absolute.z) return new Vector3(0f, Mathf.Sign(value.y), 0f);
            return new Vector3(0f, 0f, Mathf.Sign(value.z));
        }

        private static CubeAxis ResolveRotationAxis(Vector3 angularDirection, Vector3 touchedNormal)
        {
            Vector3 allowed = Vector3.ProjectOnPlane(angularDirection, touchedNormal);
            Vector3 absolute = new Vector3(Mathf.Abs(allowed.x), Mathf.Abs(allowed.y), Mathf.Abs(allowed.z));
            if (absolute.x >= absolute.y && absolute.x >= absolute.z) return CubeAxis.X;
            return absolute.y >= absolute.z ? CubeAxis.Y : CubeAxis.Z;
        }

        private static int GetAxisValue(Vector3Int value, CubeAxis axis)
        {
            switch (axis)
            {
                case CubeAxis.X: return value.x;
                case CubeAxis.Y: return value.y;
                case CubeAxis.Z: return value.z;
                default: throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        private static float GetAxisValue(Vector3 value, CubeAxis axis)
        {
            switch (axis)
            {
                case CubeAxis.X: return value.x;
                case CubeAxis.Y: return value.y;
                case CubeAxis.Z: return value.z;
                default: throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        private static Vector3 GetFaceNormal(CubeFace face)
        {
            switch (face)
            {
                case CubeFace.Up: return Vector3.up;
                case CubeFace.Down: return Vector3.down;
                case CubeFace.Front: return Vector3.forward;
                case CubeFace.Back: return Vector3.back;
                case CubeFace.Right: return Vector3.right;
                case CubeFace.Left: return Vector3.left;
                default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }
    }
}
