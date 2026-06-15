using System;
using System.Collections.Generic;
using System.Diagnostics;
using CubeChallenge3D.Cube.Model;

namespace CubeChallenge3D.Solver.Engine
{
    public sealed class RealSolverEngine : ISolverEngine
    {
        private const string SolvedFacelets = "UUUUUUUUURRRRRRRRRFFFFFFFFFDDDDDDDDDLLLLLLLLLBBBBBBBBB";
        private const int InternalMaxDepth = 8;

        private static readonly CubeMove[] SearchMoves =
        {
            new CubeMove(CubeFace.Up, 1), new CubeMove(CubeFace.Up, -1), new CubeMove(CubeFace.Up, 2),
            new CubeMove(CubeFace.Right, 1), new CubeMove(CubeFace.Right, -1), new CubeMove(CubeFace.Right, 2),
            new CubeMove(CubeFace.Front, 1), new CubeMove(CubeFace.Front, -1), new CubeMove(CubeFace.Front, 2),
            new CubeMove(CubeFace.Down, 1), new CubeMove(CubeFace.Down, -1), new CubeMove(CubeFace.Down, 2),
            new CubeMove(CubeFace.Left, 1), new CubeMove(CubeFace.Left, -1), new CubeMove(CubeFace.Left, 2),
            new CubeMove(CubeFace.Back, 1), new CubeMove(CubeFace.Back, -1), new CubeMove(CubeFace.Back, 2)
        };

        private static readonly int[][] CornerFacelets =
        {
            new[] { 8, 9, 20 },   // URF
            new[] { 6, 18, 38 },  // UFL
            new[] { 0, 36, 47 },  // ULB
            new[] { 2, 45, 11 },  // UBR
            new[] { 29, 26, 15 }, // DFR
            new[] { 27, 44, 24 }, // DLF
            new[] { 33, 53, 42 }, // DBL
            new[] { 35, 17, 51 }  // DRB
        };

        private static readonly string[] CornerPieces =
        {
            "FRU", "FLU", "BLU", "BRU", "DFR", "DFL", "BDL", "BDR"
        };

        private static readonly int[][] EdgeFacelets =
        {
            new[] { 5, 10 },  // UR
            new[] { 7, 19 },  // UF
            new[] { 3, 37 },  // UL
            new[] { 1, 46 },  // UB
            new[] { 32, 16 }, // DR
            new[] { 28, 25 }, // DF
            new[] { 30, 43 }, // DL
            new[] { 34, 52 }, // DB
            new[] { 23, 12 }, // FR
            new[] { 21, 41 }, // FL
            new[] { 50, 39 }, // BL
            new[] { 48, 14 }  // BR
        };

        private static readonly string[] EdgePieces =
        {
            "RU", "FU", "LU", "BU", "DR", "DF", "DL", "BD", "FR", "FL", "BL", "BR"
        };

        public string EngineName => "Internal 3x3 Solver";

        public bool IsAvailable()
        {
            return true;
        }

        public SolverValidationResult Validate(SolverRequest request)
        {
            SolverValidationResult basic = BasicValidate(request);
            if (!basic.isValid)
            {
                return basic;
            }

            string facelets = request.faceletString.Trim().ToUpperInvariant();
            if (!TryBuildPermutation(facelets, CornerFacelets, CornerPieces, out int[] cornerPermutation, out string cornerError))
            {
                return Invalid(SolverErrorCode.InvalidCornerCubie, "Invalid cube state.", cornerError, "This cube cannot be solved from normal turns.");
            }

            if (!TryBuildPermutation(facelets, EdgeFacelets, EdgePieces, out int[] edgePermutation, out string edgeError))
            {
                return Invalid(SolverErrorCode.InvalidEdgeCubie, "Invalid cube state.", edgeError, "This cube cannot be solved from normal turns.");
            }

            if (GetPermutationParity(cornerPermutation) != GetPermutationParity(edgePermutation))
            {
                return Invalid(SolverErrorCode.ParityError, "Invalid cube state.", "Corner/edge permutation parity mismatch.", "Please check corner and edge colors.");
            }

            // Twist/flip validation is represented in the result model and will be strengthened with a full cubie solver.
            var result = new SolverValidationResult
            {
                isValid = true,
                userMessage = "Cube input is valid.",
                debugMessage = "Cubie piece existence and permutation parity passed."
            };
            result.details.Add("Basic facelet validation passed.");
            result.details.Add("Corner and edge piece sets passed.");
            result.details.Add("Parity check passed.");
            return result;
        }

        public SolverResult Solve(SolverRequest request)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            int requestedMaxDepth = request != null ? request.maxDepth : 0;
            int requestedTimeoutMs = request != null ? request.timeoutMs : 0;
            SolverValidationResult validation = Validate(request);
            if (!validation.isValid)
            {
                return new SolverResult
                {
                    success = false,
                    isValidCube = false,
                    isEngineAvailable = true,
                    errorCode = validation.errorCode,
                    message = BuildInvalidMessage(validation),
                    moveNotations = new string[0],
                    solutionNotation = string.Empty,
                    moveCount = 0,
                    elapsedMs = (int)stopwatch.ElapsedMilliseconds,
                    engineName = EngineName,
                    debugMessage = validation.debugMessage,
                    maxDepth = requestedMaxDepth,
                    timeoutMs = requestedTimeoutMs
                };
            }

            string facelets = request.faceletString.Trim().ToUpperInvariant();
            if (facelets == SolvedFacelets)
            {
                return new SolverResult
                {
                    success = true,
                    isValidCube = true,
                    isEngineAvailable = true,
                    message = "Cube is already solved.",
                    moveNotations = new string[0],
                    solutionNotation = string.Empty,
                    moveCount = 0,
                    elapsedMs = (int)stopwatch.ElapsedMilliseconds,
                    engineName = EngineName,
                    maxDepth = requestedMaxDepth,
                    timeoutMs = requestedTimeoutMs
                };
            }

            int timeoutMs = request.timeoutMs > 0 ? request.timeoutMs : 5000;
            int maxDepth = Math.Min(request.maxDepth > 0 ? request.maxDepth : InternalMaxDepth, InternalMaxDepth);
            CubeState startState;
            try
            {
                startState = CubeStateSerializer.FromFaceletString(facelets);
            }
            catch (Exception exception)
            {
                return Failure(SolverErrorCode.InvalidCubieState, "Invalid cube state.", true, stopwatch, exception.Message, maxDepth, timeoutMs, 0);
            }

            var path = new List<CubeMove>(maxDepth);
            long searchedNodes = 0;
            for (int depth = 0; depth <= maxDepth; depth++)
            {
                if (stopwatch.ElapsedMilliseconds > timeoutMs)
                {
                    return Failure(SolverErrorCode.Timeout, "The current solver timed out.", true, stopwatch, "Search timed out.", maxDepth, timeoutMs, searchedNodes);
                }

                if (Search(startState.Clone(), depth, null, path, stopwatch, timeoutMs, ref searchedNodes))
                {
                    string[] moveNotations = new string[path.Count];
                    for (int i = 0; i < path.Count; i++)
                    {
                        moveNotations[i] = path[i].ToString();
                    }

                    return new SolverResult
                    {
                        success = true,
                        isValidCube = true,
                        isEngineAvailable = true,
                        message = "Solution found.",
                        moveNotations = moveNotations,
                        solutionNotation = MoveUtility.ToNotationSequence(path),
                        moveCount = path.Count,
                        elapsedMs = (int)stopwatch.ElapsedMilliseconds,
                        engineName = EngineName,
                        maxDepth = maxDepth,
                        timeoutMs = timeoutMs,
                        searchedNodes = searchedNodes
                    };
                }
            }

            return Failure(
                SolverErrorCode.CurrentSolverLimitation,
                "This cube is valid, but the current solver could not find a solution.",
                true,
                stopwatch,
                $"Depth limit {maxDepth} reached.",
                maxDepth,
                timeoutMs,
                searchedNodes);
        }

        private static bool Search(
            CubeState state,
            int remainingDepth,
            CubeFace? previousFace,
            List<CubeMove> path,
            Stopwatch stopwatch,
            int timeoutMs,
            ref long searchedNodes)
        {
            searchedNodes++;
            if (state.IsSolved())
            {
                return true;
            }

            if (remainingDepth == 0 || stopwatch.ElapsedMilliseconds > timeoutMs)
            {
                return false;
            }

            foreach (CubeMove move in SearchMoves)
            {
                if (previousFace.HasValue && previousFace.Value == move.Face)
                {
                    continue;
                }

                CubeState next = state.Clone();
                next.ApplyMove(move);
                path.Add(move);
                if (Search(next, remainingDepth - 1, move.Face, path, stopwatch, timeoutMs, ref searchedNodes))
                {
                    return true;
                }

                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        private static SolverValidationResult BasicValidate(SolverRequest request)
        {
            var placeholder = new PlaceholderSolverEngine();
            SolverValidationResult result = placeholder.Validate(request);
            if (result.isValid)
            {
                result.debugMessage = "Basic validation passed.";
            }

            return result;
        }

        private static bool TryBuildPermutation(string facelets, int[][] pieceFacelets, string[] knownPieces, out int[] permutation, out string error)
        {
            permutation = new int[pieceFacelets.Length];
            error = string.Empty;
            var seen = new HashSet<int>();
            for (int position = 0; position < pieceFacelets.Length; position++)
            {
                string piece = SortedPiece(facelets, pieceFacelets[position]);
                int pieceIndex = Array.IndexOf(knownPieces, piece);
                if (pieceIndex < 0)
                {
                    error = $"Invalid piece at position {position}: {piece}";
                    return false;
                }

                if (!seen.Add(pieceIndex))
                {
                    error = $"Duplicate piece detected: {piece}";
                    return false;
                }

                permutation[position] = pieceIndex;
            }

            return true;
        }

        private static string SortedPiece(string facelets, int[] indexes)
        {
            char[] values = new char[indexes.Length];
            for (int i = 0; i < indexes.Length; i++)
            {
                values[i] = facelets[indexes[i]];
            }

            Array.Sort(values);
            return new string(values);
        }

        private static int GetPermutationParity(int[] permutation)
        {
            int parity = 0;
            for (int i = 0; i < permutation.Length; i++)
            {
                for (int j = i + 1; j < permutation.Length; j++)
                {
                    if (permutation[i] > permutation[j])
                    {
                        parity ^= 1;
                    }
                }
            }

            return parity;
        }

        private static SolverValidationResult Invalid(string errorCode, string userMessage, string debugMessage, string detail)
        {
            var result = new SolverValidationResult
            {
                isValid = false,
                errorCode = errorCode,
                userMessage = userMessage,
                debugMessage = debugMessage
            };
            result.details.Add(detail);
            return result;
        }

        private static SolverResult Failure(
            string errorCode,
            string message,
            bool isValidCube,
            Stopwatch stopwatch,
            string debug,
            int maxDepth,
            int timeoutMs,
            long searchedNodes)
        {
            return new SolverResult
            {
                success = false,
                isValidCube = isValidCube,
                isEngineAvailable = true,
                errorCode = errorCode,
                message = message,
                moveNotations = new string[0],
                solutionNotation = string.Empty,
                moveCount = 0,
                elapsedMs = (int)stopwatch.ElapsedMilliseconds,
                engineName = "Internal 3x3 Solver",
                debugMessage = debug,
                maxDepth = maxDepth,
                timeoutMs = timeoutMs,
                searchedNodes = searchedNodes
            };
        }

        private static string BuildInvalidMessage(SolverValidationResult validation)
        {
            if (validation.errorCode == SolverErrorCode.InvalidCornerCubie
                || validation.errorCode == SolverErrorCode.InvalidEdgeCubie
                || validation.errorCode == SolverErrorCode.ParityError
                || validation.errorCode == SolverErrorCode.TwistError
                || validation.errorCode == SolverErrorCode.FlipError)
            {
                return "Invalid cube state. This cube cannot be solved from normal turns. Please check the entered colors.";
            }

            return validation.userMessage;
        }
    }
}
