using System;
using CubeChallenge3D.Save;
using CubeChallenge3D.Solver.Model;

namespace CubeChallenge3D.Solver.Storage
{
    public sealed class SolverInputStore
    {
        private const string FileName = "solver_input.json";

        public SolverInputState Load()
        {
            SolverInputState state = SaveService.LoadJson(FileName, SolverInputState.CreateEmpty());
            if (state == null)
            {
                return SolverInputState.CreateEmpty();
            }

            state.EnsureShape();
            return state;
        }

        public bool Save(SolverInputState state)
        {
            if (state == null)
            {
                return false;
            }

            state.EnsureShape();
            state.updatedAtUtc = DateTime.UtcNow.ToString("o");
            return SaveService.SaveJson(FileName, state);
        }

        public SolverInputState ResetToSolved()
        {
            SolverInputState state = SolverInputState.CreateSolved();
            Save(state);
            return state;
        }
    }
}
