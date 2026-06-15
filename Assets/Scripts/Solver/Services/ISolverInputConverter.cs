using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Solver.Model;

namespace CubeChallenge3D.Solver.Services
{
    public interface ISolverInputConverter
    {
        bool TryConvertToCubeState(SolverInputState inputState, out CubeState cubeState, out string error);
        bool TryConvertToFaceletString(SolverInputState inputState, out string faceletString, out string error);
    }
}
