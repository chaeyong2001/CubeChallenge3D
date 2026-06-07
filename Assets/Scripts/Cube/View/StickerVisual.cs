using CubeChallenge3D.Cube.Model;
using UnityEngine;

namespace CubeChallenge3D.Cube.View
{
    public sealed class StickerVisual : MonoBehaviour
    {
        [SerializeField] private CubeFace face;
        [SerializeField] private int row;
        [SerializeField] private int col;

        public CubeFace Face => face;
        public int Row => row;
        public int Col => col;

        public void Initialize(CubeFace cubeFace, int faceletRow, int faceletCol)
        {
            face = cubeFace;
            row = faceletRow;
            col = faceletCol;
        }
    }
}
