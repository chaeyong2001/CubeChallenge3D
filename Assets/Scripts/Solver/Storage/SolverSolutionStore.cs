using CubeChallenge3D.Save;
using CubeChallenge3D.Solver.Model;

namespace CubeChallenge3D.Solver.Storage
{
    public sealed class SolverSolutionStore
    {
        private const string FileName = "solver_recent_solution.json";

        public bool Save(SolverSolution solution)
        {
            return solution != null && SaveService.SaveJson(FileName, solution);
        }

        public SolverSolution Load()
        {
            return SaveService.LoadJson<SolverSolution>(FileName, null);
        }
    }
}
