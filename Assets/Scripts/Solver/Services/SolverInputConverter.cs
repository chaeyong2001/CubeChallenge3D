using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Solver.Model;

namespace CubeChallenge3D.Solver.Services
{
    public sealed class SolverInputConverter : ISolverInputConverter
    {
        public bool TryConvertToCubeState(SolverInputState inputState, out CubeState cubeState, out string error)
        {
            cubeState = null;
            if (!TryConvertToFaceletString(inputState, out string facelets, out error))
            {
                return false;
            }

            try
            {
                cubeState = CubeStateSerializer.FromFaceletString(facelets);
                return true;
            }
            catch (System.Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public bool TryConvertToFaceletString(SolverInputState inputState, out string faceletString, out string error)
        {
            return SolverInputSerializer.TryToFaceletString(inputState, out faceletString, out error);
        }
    }
}
