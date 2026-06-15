using CubeChallenge3D.Cube.Model;
using CubeChallenge3D.Solver.Storage;

namespace CubeChallenge3D.Solver.Engine
{
    public static class SolverEngineSelfCheck
    {
        public static bool RunAll(out string message)
        {
            ISolverEngine engine = SolverEngineProvider.GetEngine();
            SolverRequest solved = CreateRequest("UUUUUUUUURRRRRRRRRFFFFFFFFFDDDDDDDDDLLLLLLLLLBBBBBBBBB");
            SolverResult solvedResult = engine.Solve(solved);
            if (!solvedResult.success || solvedResult.moveCount != 0)
            {
                message = $"Solved cube self-check failed on {engine.EngineName}.";
                return false;
            }

            string[] shortScrambles =
            {
                "U",
                "R",
                "F",
                "U R",
                "R U'",
                "R U R' U'"
            };

            foreach (string scramble in shortScrambles)
            {
                if (!CheckScramble(engine, scramble, true, out message))
                {
                    return false;
                }
            }

            string[] deterministicScrambles =
            {
                "F R U R' U' F'",
                "R U F D L B R' U' F' D'",
                "R U F D L B R' U' F' D' L' B' R2 U2 F2 D2 L2 B2 R U"
            };

            bool requireDeepSolutions = SolverEngineProvider.IsHighPerformanceAvailable();
            foreach (string scramble in deterministicScrambles)
            {
                if (!CheckScramble(engine, scramble, requireDeepSolutions, out message))
                {
                    return false;
                }
            }

            SolverResult invalid = engine.Solve(CreateRequest("UUUUUUUUURRRRRRRRRFFFFFFFFFDDDDDDDDDLLLLLLLLLBBBBBBBBU"));
            if (invalid.success || invalid.isValidCube)
            {
                message = "Invalid color count self-check failed.";
                return false;
            }

            if (SolverDebugCaseStore.TryLoadLatest(out string savedFacelets))
            {
                SolverValidationResult savedValidation = engine.Validate(CreateRequest(savedFacelets));
                if (!savedValidation.isValid)
                {
                    message = $"Saved debug case validation failed: {savedValidation.errorCode}";
                    return false;
                }
            }

            message = $"Solver engine self-check passed. Active engine: {engine.EngineName}.";
            return true;
        }

        private static bool CheckScramble(ISolverEngine engine, string scramble, bool requireSolution, out string message)
        {
            CubeState scrambled = CubeState.CreateSolved();
            scrambled.ApplyMoves(MoveUtility.ParseSequence(scramble));
            string scrambledFacelets = CubeStateSerializer.ToFaceletString(scrambled);
            SolverValidationResult validation = engine.Validate(CreateRequest(scrambledFacelets));
            if (!validation.isValid)
            {
                message = $"Scramble validation failed ({scramble}): {validation.errorCode}";
                return false;
            }

            SolverResult scrambleResult = engine.Solve(CreateRequest(scrambledFacelets));
            if (!scrambleResult.success)
            {
                if (!requireSolution && scrambleResult.isValidCube)
                {
                    message = string.Empty;
                    return true;
                }

                message = $"Scramble solve failed ({scramble}): {scrambleResult.errorCode}";
                return false;
            }

            CubeState check = scrambled.Clone();
            check.ApplyMoves(MoveUtility.ParseSequence(scrambleResult.solutionNotation));
            if (!check.IsSolved())
            {
                message = $"Returned solution did not solve scramble: {scramble}";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static SolverRequest CreateRequest(string facelets)
        {
            return new SolverRequest
            {
                faceletString = facelets,
                faceOrder = "URFDLB",
                maxDepth = 8,
                timeoutMs = 5000,
                requireFullValidation = true
            };
        }
    }
}
