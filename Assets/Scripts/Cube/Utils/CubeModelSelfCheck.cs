using System;
using CubeChallenge3D.Cube.Model;

namespace CubeChallenge3D.Cube.Utils
{
    public static class CubeModelSelfCheck
    {
        public static bool RunAll()
        {
            return IsFourTurnsIdentity(CubeFace.Right)
                && IsFourTurnsIdentity(CubeFace.Up)
                && AreAllFourTurnsIdentity()
                && AreAllMoveInversePairsIdentity()
                && IsScrambleInverseIdentity()
                && IsSolvedColorCountValid()
                && IsSerializationRoundTripValid();
        }

        public static bool AreAllFourTurnsIdentity()
        {
            foreach (CubeFace face in Enum.GetValues(typeof(CubeFace)))
            {
                if (!IsFourTurnsIdentity(face))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool AreAllMoveInversePairsIdentity()
        {
            foreach (CubeFace face in Enum.GetValues(typeof(CubeFace)))
            {
                var move = new CubeMove(face, 1);
                CubeState state = CubeState.CreateSolved();
                state.ApplyMove(move);
                state.ApplyMove(MoveUtility.Inverse(move));
                if (!state.Equals(CubeState.CreateSolved()))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSolvedColorCountValid()
        {
            return CubeStateValidator.IsColorCountValid(CubeState.CreateSolved());
        }

        public static bool IsSerializationRoundTripValid()
        {
            CubeState original = CubeState.CreateSolved();
            original.ApplyMoves(MoveUtility.ParseSequence("R U R' U' F2"));
            string serialized = CubeStateSerializer.ToFaceletString(original);
            CubeState restored = CubeStateSerializer.FromFaceletString(serialized);
            return original.Equals(restored);
        }

        public static bool IsScrambleInverseIdentity()
        {
            CubeState state = CubeState.CreateSolved();
            var scramble = ScrambleGenerator.Generate(20, 12345);
            state.ApplyMoves(scramble);
            state.ApplyMoves(MoveUtility.InverseSequence(scramble));
            return state.IsSolved();
        }

        private static bool IsFourTurnsIdentity(CubeFace face)
        {
            CubeState solved = CubeState.CreateSolved();
            CubeState state = solved.Clone();
            var move = new CubeMove(face, 1);
            for (int i = 0; i < 4; i++)
            {
                state.ApplyMove(move);
            }

            return state.Equals(solved);
        }
    }
}
