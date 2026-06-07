using UnityEngine;

namespace CubeChallenge3D.Cube.View
{
    public sealed class CubieVisual : MonoBehaviour
    {
        [SerializeField] private Vector3Int initialGridPosition;
        [SerializeField] private Vector3Int currentGridPosition;

        public Vector3Int InitialGridPosition => initialGridPosition;
        public Vector3Int CurrentGridPosition => currentGridPosition;

        public void Initialize(Vector3Int gridPosition)
        {
            initialGridPosition = gridPosition;
            currentGridPosition = gridPosition;
        }

        public void SetCurrentGridPosition(Vector3Int gridPosition)
        {
            currentGridPosition = gridPosition;
        }
    }
}
