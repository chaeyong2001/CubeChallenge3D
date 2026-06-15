using CubeChallenge3D.Cube.Model;

namespace CubeChallenge3D.Solver.Services
{
    public static class SolverPlaybackMoveMapper
    {
        public static CubeMove ToPlaybackMove(CubeMove move)
        {
            switch (move.Face)
            {
                case CubeFace.Up:
                    return new CubeMove(CubeFace.Up, -move.QuarterTurns);
                case CubeFace.Down:
                    return new CubeMove(CubeFace.Down, -move.QuarterTurns);
                case CubeFace.Front:
                    return new CubeMove(CubeFace.Front, -move.QuarterTurns);
                case CubeFace.Right:
                    return new CubeMove(CubeFace.Left, -move.QuarterTurns);
                case CubeFace.Left:
                    return new CubeMove(CubeFace.Right, -move.QuarterTurns);
                case CubeFace.Back:
                    return new CubeMove(CubeFace.Back, -move.QuarterTurns);
                default:
                    return move;
            }
        }
    }
}
