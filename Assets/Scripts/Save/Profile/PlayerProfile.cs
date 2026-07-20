using System;

namespace CubeChallenge3D.Save.Profile
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public int profileVersion;
        public string profileId;
        public string nickname;
        public int avatarId;
        public string createdAtUtc;
        public string updatedAtUtc;
        public bool linkedGooglePlay;
        public bool linkedGoogle;
        public string googlePlayPlayerId;
        public string googleAccountId;
        public string googleEmailHash;
        public string lastAccountLinkAttemptUtc;
        public string accountLinkError;
        public int nicknameChangeTickets;
        public string lastNicknameChangeDateUtc;
        public bool isServerSynced;
        public bool serverSyncPending;
        public string serverSyncError;
        public string lastServerSyncAttemptUtc;

        public static PlayerProfile Create(string nickname, int avatarId)
        {
            string now = DateTime.UtcNow.ToString("o");
            return new PlayerProfile
            {
                profileVersion = SaveDataValidator.CurrentSaveVersion,
                profileId = Guid.NewGuid().ToString(),
                nickname = nickname,
                avatarId = avatarId,
                createdAtUtc = now,
                updatedAtUtc = now,
                linkedGooglePlay = false,
                linkedGoogle = false,
                googlePlayPlayerId = string.Empty,
                googleAccountId = string.Empty,
                googleEmailHash = string.Empty,
                lastAccountLinkAttemptUtc = string.Empty,
                accountLinkError = string.Empty,
                nicknameChangeTickets = 0,
                lastNicknameChangeDateUtc = string.Empty,
                isServerSynced = false,
                serverSyncPending = false,
                serverSyncError = string.Empty,
                lastServerSyncAttemptUtc = string.Empty
            };
        }
    }
}
