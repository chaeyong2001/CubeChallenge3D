using System;
using CubeChallenge3D.Cube.Model;

namespace CubeChallenge3D.Ranking
{
    public static class RankingVerificationHelper
    {
        public static bool VerifySubmission(RankingSubmission submission)
        {
            return VerifySubmissionDetailed(submission).isValid;
        }

        public static RankingVerificationResult VerifySubmissionDetailed(RankingSubmission submission)
        {
            if (submission == null || !submission.completed)
            {
                return RankingVerificationResult.Invalid("Submission is null or incomplete.");
            }

            if (string.IsNullOrWhiteSpace(submission.challengeId))
            {
                return RankingVerificationResult.Invalid("Challenge id is empty.");
            }

            if (submission.elapsedSeconds <= 0f)
            {
                return RankingVerificationResult.Invalid("Elapsed time must be greater than zero.");
            }

            try
            {
                var scrambleMoves = MoveUtility.ParseSequence(submission.scrambleNotation);
                var userMoves = MoveUtility.ParseSequence(submission.moveLogNotation);
                if (submission.moveCount != userMoves.Count)
                {
                    return RankingVerificationResult.Invalid(
                        "Move count does not match move log.",
                        scrambleMoves.Count,
                        userMoves.Count);
                }

                CubeState state = CubeState.CreateSolved();
                state.ApplyMoves(scrambleMoves);
                state.ApplyMoves(userMoves);
                return state.IsSolved()
                    ? RankingVerificationResult.Valid(scrambleMoves.Count, userMoves.Count)
                    : RankingVerificationResult.Invalid("Final cube state is not solved.", scrambleMoves.Count, userMoves.Count);
            }
            catch (Exception)
            {
                return RankingVerificationResult.Invalid("Move notation could not be parsed.");
            }
        }
    }
}
