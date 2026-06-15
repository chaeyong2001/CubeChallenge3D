using CubeChallenge3D.Solver.Engine.HighPerformance;

namespace CubeChallenge3D.Solver.Engine
{
    public static class SolverEngineProvider
    {
        private static readonly ISolverEngine highPerformanceEngine = new TwoPhaseSolverEngine();
        private static readonly ISolverEngine fallbackEngine = new RealSolverEngine();
        private static readonly ISolverEngine placeholderEngine = new PlaceholderSolverEngine();

        public static ISolverEngine GetEngine()
        {
            if (highPerformanceEngine.IsAvailable())
            {
                return highPerformanceEngine;
            }

            if (fallbackEngine.IsAvailable())
            {
                return fallbackEngine;
            }

            return placeholderEngine;
        }

        public static string GetActiveEngineName()
        {
            return GetEngine().EngineName;
        }

        public static bool IsHighPerformanceAvailable()
        {
            return highPerformanceEngine.IsAvailable();
        }

        public static bool IsUsingFallback()
        {
            return !highPerformanceEngine.IsAvailable();
        }

        public static string GetFallbackReason()
        {
            if (highPerformanceEngine.IsAvailable())
            {
                return string.Empty;
            }

            var twoPhase = highPerformanceEngine as TwoPhaseSolverEngine;
            string detail = twoPhase != null ? twoPhase.AvailabilityError : string.Empty;
            return string.IsNullOrWhiteSpace(detail)
                ? "TwoPhaseSolverEngine is unavailable. Using the internal fallback solver."
                : $"TwoPhaseSolverEngine initialization failed: {detail}";
        }
    }
}
