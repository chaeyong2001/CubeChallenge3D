using System;
using System.Collections.Generic;

namespace CubeChallenge3D.Ranking
{
    [Serializable]
    public sealed class RankingSubmissionDto
    {
        public string submissionId;
        public string challengeId;
        public string playerId;
        public string playerName;
        public int avatarId = -1;
        public float elapsedSeconds;
        public int moveCount;
        public string scrambleNotation;
        public string moveLogNotation;
        public string controlMode;
        public string completedAtUtc;
        public string clientVersion;
        public string deviceIdHash;

        public static RankingSubmissionDto FromSubmission(RankingSubmission submission)
        {
            return new RankingSubmissionDto
            {
                submissionId = submission.submissionId,
                challengeId = submission.challengeId,
                playerId = submission.playerId,
                playerName = submission.playerName,
                avatarId = submission.avatarId,
                elapsedSeconds = submission.elapsedSeconds,
                moveCount = submission.moveCount,
                scrambleNotation = submission.scrambleNotation,
                moveLogNotation = submission.moveLogNotation,
                controlMode = submission.controlMode,
                completedAtUtc = submission.completedAtUtc,
                clientVersion = submission.clientVersion,
                deviceIdHash = submission.deviceIdHash
            };
        }
    }

    [Serializable]
    public sealed class RankingRecordDto
    {
        public string submissionId;
        public string challengeId;
        public string playerId;
        public string playerName;
        public int avatarId = -1;
        public float elapsedSeconds;
        public int moveCount;
        public string scrambleNotation;
        public string moveLogNotation;
        public string controlMode;
        public string completedAtUtc;
        public string clientVersion;
        public string deviceIdHash;
        public bool isVerified;
        public string verifyReason;
        public string createdAt;

        public RankingSubmission ToSubmission()
        {
            return new RankingSubmission
            {
                submissionId = submissionId,
                challengeId = challengeId,
                playerId = playerId,
                playerName = playerName,
                avatarId = avatarId,
                elapsedSeconds = elapsedSeconds,
                moveCount = moveCount,
                scrambleNotation = scrambleNotation,
                moveLogNotation = moveLogNotation,
                controlMode = controlMode,
                completedAtUtc = completedAtUtc,
                completed = true,
                isVerified = isVerified,
                isSynced = true,
                isDebugClear = false,
                clearSource = "Normal",
                syncStatus = RankingSyncStatus.Synced,
                clientVersion = clientVersion,
                deviceIdHash = deviceIdHash
            };
        }
    }

    [Serializable]
    public sealed class RankingSubmitResponseDto
    {
        public bool success;
        public bool isVerified;
        public bool duplicate;
        public string message;
        public RankingRecordDto submission;
    }

    [Serializable]
    public sealed class RankingSubmitErrorDto
    {
        public bool success;
        public string reason;
        public string message;
        public string detail;
    }

    [Serializable]
    public sealed class RankingTopResponseDto
    {
        public bool success;
        public string challengeId;
        public List<RankingRecordDto> records = new List<RankingRecordDto>();
    }

    [Serializable]
    public sealed class RankingRankResponseDto
    {
        public bool success;
        public string message;
        public int rank;
        public RankingRecordDto record;

        public RankingRankResult ToResult()
        {
            return new RankingRankResult
            {
                success = success,
                message = message,
                rank = rank,
                record = record?.ToSubmission()
            };
        }
    }

    [Serializable]
    public sealed class ChallengeTodayResponseDto
    {
        public string challengeId;
        public string dateUtc;
        public int seed;
        public int scrambleLength;
    }

    [Serializable]
    public sealed class WeeklyRankingRewardDto
    {
        public bool exists;
        public bool claimed;
        public string weekStartKst;
        public string weekEndKst;
        public string playerId;
        public string nickname;
        public int rank;
        public string rewardType;
        public int rewardAmount;
        public string message;
    }

    [Serializable]
    public sealed class WeeklyRankingRewardClaimRequestDto
    {
        public string playerId;
        public string weekStartKst;
    }

    [Serializable]
    public sealed class WeeklyRankingRewardClaimResponseDto
    {
        public bool success;
        public bool claimed;
        public string message;
        public WeeklyRankingRewardDto reward;
    }

    [Serializable]
    public sealed class WeeklyRankingRewardInfoResponseDto
    {
        public bool success;
        public string weekStartKst;
        public string weekEndKst;
        public string description;
        public string[] rewards;
    }
}
