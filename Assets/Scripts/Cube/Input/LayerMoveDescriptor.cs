using CubeChallenge3D.Cube.Model;

namespace CubeChallenge3D.Cube.Input
{
    public readonly struct LayerMoveDescriptor
    {
        public CubeAxis Axis { get; }
        public int LayerIndex { get; }
        public int QuarterTurns { get; }
        public bool IsMiddleLayer => LayerIndex == 0;

        public LayerMoveDescriptor(CubeAxis axis, int layerIndex, int quarterTurns)
        {
            Axis = axis;
            LayerIndex = layerIndex;
            QuarterTurns = MoveUtility.NormalizeQuarterTurns(quarterTurns);
        }

        public bool TryToCubeMove(out CubeMove move)
        {
            move = default;
            if (IsMiddleLayer || LayerIndex < -1 || LayerIndex > 1 || QuarterTurns == 0)
            {
                return false;
            }

            CubeFace face;
            switch (Axis)
            {
                case CubeAxis.X: face = LayerIndex > 0 ? CubeFace.Right : CubeFace.Left; break;
                case CubeAxis.Y: face = LayerIndex > 0 ? CubeFace.Up : CubeFace.Down; break;
                case CubeAxis.Z: face = LayerIndex > 0 ? CubeFace.Front : CubeFace.Back; break;
                default: return false;
            }

            // Descriptor turns use right-hand rotation around the positive axis.
            // CubeMove turns are clockwise while viewing the selected face from outside.
            move = new CubeMove(face, -QuarterTurns * LayerIndex);
            return true;
        }

        public override string ToString()
        {
            return $"{Axis}[{LayerIndex}] {QuarterTurns}";
        }
    }
}
