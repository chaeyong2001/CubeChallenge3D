using UnityEngine;

namespace CubeChallenge3D.Cube.Input
{
    public static class SnapRotationUtility
    {
        public static Quaternion GetNearestRightAngleRotation(Quaternion current)
        {
            Vector3 euler = current.eulerAngles;
            return Quaternion.Euler(
                SnapAngle(euler.x),
                SnapAngle(euler.y),
                SnapAngle(euler.z));
        }

        private static float SnapAngle(float angle)
        {
            return Mathf.Round(angle / 90f) * 90f;
        }
    }
}
