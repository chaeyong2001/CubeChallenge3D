namespace CubeChallenge3D.Solver.Engine
{
    public interface ISolverEngine
    {
        string EngineName { get; }
        bool IsAvailable();
        SolverValidationResult Validate(SolverRequest request);
        SolverResult Solve(SolverRequest request);
    }
}
