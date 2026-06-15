using System;
using System.Collections.Generic;
using System.Diagnostics;
using CubeChallenge3D.Cube.Model;
using TwoPhaseSolver;
using TwoPhaseCube = TwoPhaseSolver.Cube;

namespace CubeChallenge3D.Solver.Engine.HighPerformance
{
    public sealed class TwoPhaseSolverEngine : ISolverEngine
    {
        private const int DefaultMaxDepth = 24;
        private const int DefaultTimeoutMs = 15000;
        private static bool availabilityChecked;
        private static bool available;
        private static string availabilityError = string.Empty;

        public string EngineName => "TwoPhaseSolverEngine";
        public string AvailabilityError
        {
            get
            {
                EnsureAvailable();
                return availabilityError;
            }
        }

        public bool IsAvailable()
        {
            EnsureAvailable();
            return available;
        }

        public SolverValidationResult Validate(SolverRequest request)
        {
            SolverValidationResult validation = new RealSolverEngine().Validate(request);
            if (!validation.isValid || !IsAvailable())
            {
                if (validation.isValid)
                {
                    validation.isValid = false;
                    validation.errorCode = SolverErrorCode.HighPerformanceEngineNotAvailable;
                    validation.userMessage = "High performance solver is unavailable.";
                    validation.debugMessage = availabilityError;
                }

                return validation;
            }

            try
            {
                _ = new TwoPhaseCube(TwoPhaseFaceletConverter.ToTwoPhaseColors(request.faceletString));
                validation.debugMessage = "Cube validation passed for TwoPhaseSolverEngine.";
                return validation;
            }
            catch (Exception exception)
            {
                return new SolverValidationResult
                {
                    isValid = false,
                    errorCode = SolverErrorCode.InvalidCubieState,
                    userMessage = "Invalid cube state. Please check the entered colors.",
                    debugMessage = exception.Message
                };
            }
        }

        public SolverResult Solve(SolverRequest request)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            if (!IsAvailable())
            {
                return Failure(
                    SolverErrorCode.HighPerformanceEngineNotAvailable,
                    "High performance solver is unavailable.",
                    false,
                    stopwatch,
                    availabilityError,
                    request);
            }

            SolverValidationResult validation = Validate(request);
            if (!validation.isValid)
            {
                return Failure(validation.errorCode, validation.userMessage, false, stopwatch, validation.debugMessage, request);
            }

            int maxDepth = Math.Max(DefaultMaxDepth, request != null ? request.maxDepth : 0);
            int timeoutMs = Math.Max(DefaultTimeoutMs, request != null ? request.timeoutMs : 0);
            try
            {
                TwoPhaseCube cube = new TwoPhaseCube(TwoPhaseFaceletConverter.ToTwoPhaseColors(request.faceletString));
                Move solution = Search.fullSolve(cube, maxDepth, timeoutMs, false);
                string notation = solution == null || solution.Length == 0 ? string.Empty : solution.ToString();
                string[] moves = NormalizeMoves(notation);

                if (!VerifySolution(request.faceletString, moves, out string verifyError))
                {
                    return Failure(
                        SolverErrorCode.SolutionNotFound,
                        "The solver returned an invalid solution.",
                        true,
                        stopwatch,
                        verifyError,
                        request,
                        maxDepth,
                        timeoutMs);
                }

                return new SolverResult
                {
                    success = true,
                    isValidCube = true,
                    isEngineAvailable = true,
                    message = moves.Length == 0 ? "Cube is already solved." : "Solution found.",
                    moveNotations = moves,
                    solutionNotation = string.Join(" ", moves),
                    moveCount = moves.Length,
                    elapsedMs = (int)stopwatch.ElapsedMilliseconds,
                    engineName = EngineName,
                    maxDepth = maxDepth,
                    timeoutMs = timeoutMs
                };
            }
            catch (TimeoutException exception)
            {
                return Failure(SolverErrorCode.Timeout, "The current solver timed out.", true, stopwatch, exception.Message, request, maxDepth, timeoutMs);
            }
            catch (Exception exception)
            {
                return Failure(SolverErrorCode.SolutionNotFound, "The solver could not find a solution.", true, stopwatch, exception.Message, request, maxDepth, timeoutMs);
            }
        }

        private static void EnsureAvailable()
        {
            if (availabilityChecked)
            {
                return;
            }

            availabilityChecked = true;
            try
            {
                _ = new TwoPhaseCube();
                _ = MoveTables.moveCO[0, 0];
                _ = PruneTable.pruneCO[0];
                available = true;
            }
            catch (Exception exception)
            {
                available = false;
                availabilityError = exception.GetBaseException().Message;
            }
        }

        private static string[] NormalizeMoves(string notation)
        {
            if (string.IsNullOrWhiteSpace(notation) || notation == "None")
            {
                return Array.Empty<string>();
            }

            string[] tokens = notation.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var normalized = new List<string>(tokens.Length);
            foreach (string rawToken in tokens)
            {
                string token = rawToken.Trim().Trim('(', ')', ',', '.');
                if (token.EndsWith("3", StringComparison.Ordinal))
                {
                    token = token.Substring(0, token.Length - 1) + "'";
                }

                if (!CubeMove.TryParse(token, out _))
                {
                    throw new FormatException($"Unknown solver move notation: {rawToken}");
                }

                normalized.Add(token);
            }

            return normalized.ToArray();
        }

        private static bool VerifySolution(string facelets, IEnumerable<string> moves, out string error)
        {
            try
            {
                CubeState state = CubeStateSerializer.FromFaceletString(facelets);
                foreach (string notation in moves)
                {
                    if (!CubeMove.TryParse(notation, out CubeMove move))
                    {
                        error = $"Unknown move during verification: {notation}";
                        return false;
                    }

                    state.ApplyMove(move);
                }

                error = state.IsSolved() ? string.Empty : "Applying the returned moves did not solve the source state.";
                return state.IsSolved();
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private SolverResult Failure(
            string errorCode,
            string message,
            bool isValidCube,
            Stopwatch stopwatch,
            string debug,
            SolverRequest request,
            int maxDepth = 0,
            int timeoutMs = 0)
        {
            return new SolverResult
            {
                success = false,
                isValidCube = isValidCube,
                isEngineAvailable = errorCode != SolverErrorCode.HighPerformanceEngineNotAvailable,
                errorCode = errorCode,
                message = message,
                moveNotations = Array.Empty<string>(),
                solutionNotation = string.Empty,
                moveCount = 0,
                elapsedMs = (int)stopwatch.ElapsedMilliseconds,
                engineName = EngineName,
                debugMessage = debug,
                maxDepth = maxDepth > 0 ? maxDepth : request != null ? request.maxDepth : 0,
                timeoutMs = timeoutMs > 0 ? timeoutMs : request != null ? request.timeoutMs : 0
            };
        }
    }
}
