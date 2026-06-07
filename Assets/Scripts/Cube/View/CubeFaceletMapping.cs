using System;
using CubeChallenge3D.Cube.Model;
using UnityEngine;

namespace CubeChallenge3D.Cube.View
{
    public static class CubeFaceletMapping
    {
        // Row 0 is the visual top of each face when viewed directly from outside.
        public static void GridPositionToFacelet(
            CubeFace face,
            Vector3Int gridPosition,
            out int row,
            out int col)
        {
            switch (face)
            {
                case CubeFace.Up:
                    row = gridPosition.z + 1;
                    col = gridPosition.x + 1;
                    return;
                case CubeFace.Down:
                    row = 1 - gridPosition.z;
                    col = gridPosition.x + 1;
                    return;
                case CubeFace.Front:
                    row = 1 - gridPosition.y;
                    col = gridPosition.x + 1;
                    return;
                case CubeFace.Back:
                    row = 1 - gridPosition.y;
                    col = 1 - gridPosition.x;
                    return;
                case CubeFace.Right:
                    row = 1 - gridPosition.y;
                    col = 1 - gridPosition.z;
                    return;
                case CubeFace.Left:
                    row = 1 - gridPosition.y;
                    col = gridPosition.z + 1;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }

        public static Vector3 FaceNormal(CubeFace face)
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

        public static Quaternion StickerRotation(CubeFace face)
        {
            // Unity's Quad primitive faces along its local -Z axis.
            switch (face)
            {
                case CubeFace.Up: return Quaternion.Euler(90f, 0f, 0f);
                case CubeFace.Down: return Quaternion.Euler(-90f, 0f, 0f);
                case CubeFace.Front: return Quaternion.Euler(0f, 180f, 0f);
                case CubeFace.Back: return Quaternion.identity;
                case CubeFace.Right: return Quaternion.Euler(0f, -90f, 0f);
                case CubeFace.Left: return Quaternion.Euler(0f, 90f, 0f);
                default: throw new ArgumentOutOfRangeException(nameof(face), face, null);
            }
        }
    }
}
