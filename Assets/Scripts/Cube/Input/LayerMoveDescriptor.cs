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
            if (LayerIndex < -1 || LayerIndex > 1 || QuarterTurns == 0)
            {
                return false;
            }

            // Descriptor turns use right-hand rotation around the positive axis.
            move = CubeMove.CreateLayer(Axis, LayerIndex, QuarterTurns);
            return true;
        }

        public override string ToString()
        {
            return $"{Axis}[{LayerIndex}] {QuarterTurns}";
        }
    }
}
