using System;
using System.Collections.Generic;
using System.Linq;

namespace CubeChallenge3D.Cube.Model
{
    public static class MoveUtility
    {
        public static CubeMove Inverse(CubeMove move)
        {
            return new CubeMove(move.Face, -move.QuarterTurns);
        }

        public static IReadOnlyList<CubeMove> InverseSequence(IEnumerable<CubeMove> moves)
        {
            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            CubeMove[] source = moves.ToArray();
            var result = new CubeMove[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                result[i] = Inverse(source[source.Length - 1 - i]);
            }

            return result;
        }

        public static int NormalizeQuarterTurns(int turns)
        {
            int normalized = turns % 4;
            if (normalized < 0)
            {
                normalized += 4;
            }

            return normalized == 3 ? -1 : normalized;
        }

        public static IReadOnlyList<CubeMove> ParseSequence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<CubeMove>();
            }

            string[] tokens = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            var moves = new CubeMove[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!CubeMove.TryParse(tokens[i], out moves[i]))
                {
                    throw new FormatException($"Invalid cube move notation: {tokens[i]}");
                }
            }

            return moves;
        }

        public static string ToNotationSequence(IEnumerable<CubeMove> moves)
        {
            if (moves == null)
            {
                throw new ArgumentNullException(nameof(moves));
            }

            return string.Join(" ", moves.Select(move => move.ToString()));
        }
    }
}
