namespace CubeChallenge3D.Solver.Engine
{
    public static class SolverErrorCode
    {
        public const string EmptyInput = "EmptyInput";
        public const string InvalidLength = "InvalidLength";
        public const string InvalidFaceletCharacters = "InvalidFaceletCharacters";
        public const string InvalidColorCount = "InvalidColorCount";
        public const string DuplicateCenters = "DuplicateCenters";
        public const string InvalidCornerCubie = "InvalidCornerCubie";
        public const string InvalidEdgeCubie = "InvalidEdgeCubie";
        public const string InvalidCubieState = "InvalidCubieState";
        public const string ParityError = "ParityError";
        public const string TwistError = "TwistError";
        public const string FlipError = "FlipError";
        public const string CurrentSolverLimitation = "CurrentSolverLimitation";
        public const string SolutionNotFound = "SolutionNotFound";
        public const string HighPerformanceEngineNotAvailable = "HighPerformanceEngineNotAvailable";
        public const string SolverNotConnected = "SolverNotConnected";
        public const string SolverEngineNotImplemented = "SolverEngineNotImplemented";
        public const string Timeout = "Timeout";
    }
}
