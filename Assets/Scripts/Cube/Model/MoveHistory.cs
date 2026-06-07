using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Cube.Model
{
    public sealed class MoveHistory
    {
        private readonly List<CubeMove> moves = new List<CubeMove>();

        public int Count => moves.Count;
        public bool CanUndo => moves.Count > 0;

        public void Add(CubeMove move)
        {
            moves.Add(move);
        }

        public void Clear()
        {
            moves.Clear();
        }

        public CubeMove PopLast()
        {
            if (!CanUndo)
            {
                throw new InvalidOperationException("Move history is empty.");
            }

            int lastIndex = moves.Count - 1;
            CubeMove move = moves[lastIndex];
            moves.RemoveAt(lastIndex);
            return move;
        }

        public bool TryPopLast(out CubeMove move)
        {
            if (!CanUndo)
            {
                move = default;
                return false;
            }

            move = PopLast();
            return true;
        }

        public IReadOnlyList<CubeMove> GetMoves()
        {
            return moves.AsReadOnly();
        }

        public string ToNotationString()
        {
            return MoveUtility.ToNotationSequence(moves);
        }
    }
}
