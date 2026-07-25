using System;
using CubeChallenge3D.Save.Settings;
using UnityEngine;

namespace CubeChallenge3D.Save.Profile
{
    public sealed class PlayerProfileStore
    {
        private const string FileName = "player_profile.json";
        public const int MaxNicknameChangeTickets = 3;

        private PlayerProfile current;
        private bool loaded;

        public string ProfilePath => SaveService.GetPath(FileName);

        public PlayerProfile Current
        {
            get
            {
                Load();
                return current;
            }
        }

        public bool Exists()
        {
            return Current != null && !string.IsNullOrWhiteSpace(Current.profileId);
        }

        public PlayerProfile Load()
        {
            if (loaded)
            {
                return current;
            }

            current = SaveService.LoadJson<PlayerProfile>(FileName, null);
            loaded = true;
            Debug.Log($"[PlayerProfile] Exists={current != null} path={ProfilePath}");
            if (current != null && Normalize(current))
            {
                Save();
            }

            if (current != null)
            {
                Debug.Log($"[PlayerProfile] Loaded profileId={current.profileId} nickname={current.nickname} avatarId={current.avatarId}");
            }

            return current;
        }

        public PlayerProfile ReloadFromDisk()
        {
            loaded = false;
            current = null;
            return Load();
        }

        public PlayerProfile CreateDefaultProfile(string nickname, int avatarId)
        {
            return CreateProfile(nickname, avatarId, null, false, false, string.Empty, string.Empty, string.Empty);
        }

        public PlayerProfile CreateProfile(
            string nickname,
            int avatarId,
            string profileId,
            bool isServerSynced,
            bool serverSyncPending,
            string serverSyncError,
            string createdAtUtc,
            string updatedAtUtc,
            string googlePlayPlayerId = "",
            string googlePlayDisplayName = "")
        {
            NicknameValidationResult validation = NicknameValidator.Validate(nickname);
            if (!validation.IsValid)
            {
                return null;
            }

            current = PlayerProfile.Create(validation.NormalizedNickname, ClampAvatarId(avatarId));
            if (!string.IsNullOrWhiteSpace(profileId))
            {
                current.profileId = profileId;
            }

            if (!string.IsNullOrWhiteSpace(createdAtUtc))
            {
                current.createdAtUtc = createdAtUtc;
            }

            if (!string.IsNullOrWhiteSpace(updatedAtUtc))
            {
                current.updatedAtUtc = updatedAtUtc;
            }

            current.isServerSynced = isServerSynced;
            current.serverSyncPending = serverSyncPending;
            current.serverSyncError = serverSyncError ?? string.Empty;
            current.lastServerSyncAttemptUtc = DateTime.UtcNow.ToString("o");
            if (!string.IsNullOrWhiteSpace(googlePlayPlayerId))
            {
                current.googlePlayPlayerId = googlePlayPlayerId;
                current.googlePlayDisplayName = googlePlayDisplayName ?? string.Empty;
                current.linkedGooglePlay = true;
                current.lastGooglePlaySignInAt = DateTime.UtcNow.ToString("o");
            }

            loaded = true;
            Save();
            Debug.Log($"[PlayerProfile] Created profileId={current.profileId} nickname={current.nickname} avatarId={current.avatarId} synced={current.isServerSynced} pending={current.serverSyncPending} path={ProfilePath}");
            return current;
        }

        public bool UpdateNicknameAndAvatar(string nickname, int avatarId)
        {
            if (!Exists())
            {
                return false;
            }

            NicknameValidationResult validation = NicknameValidator.Validate(nickname);
            if (!validation.IsValid)
            {
                return false;
            }

            Current.nickname = validation.NormalizedNickname;
            Current.avatarId = ClampAvatarId(avatarId);
            Current.updatedAtUtc = DateTime.UtcNow.ToString("o");
            Save();
            return true;
        }

        public bool UpdateAvatar(int avatarId)
        {
            if (!Exists())
            {
                return false;
            }

            Current.avatarId = ClampAvatarId(avatarId);
            Current.updatedAtUtc = DateTime.UtcNow.ToString("o");
            Current.serverSyncPending = true;
            Save();
            Debug.Log($"[PlayerProfile] Avatar updated profileId={Current.profileId} nickname={Current.nickname} avatarId={Current.avatarId}");
            return true;
        }

        public bool MarkServerSyncResult(bool success, string error)
        {
            if (!Exists())
            {
                return false;
            }

            Current.isServerSynced = success;
            Current.serverSyncPending = !success;
            Current.serverSyncError = error ?? string.Empty;
            Current.lastServerSyncAttemptUtc = DateTime.UtcNow.ToString("o");
            Current.updatedAtUtc = DateTime.UtcNow.ToString("o");
            Save();
            return true;
        }

        public bool TryChangeNicknameWithTicket(string nickname, out string message)
        {
            message = string.Empty;
            if (!Exists())
            {
                message = "Create a profile first.";
                return false;
            }

            NicknameValidationResult validation = NicknameValidator.Validate(nickname);
            if (!validation.IsValid)
            {
                message = ToUserMessage(validation);
                return false;
            }

            if (Current.nicknameChangeTickets <= 0)
            {
                message = "You need a Nickname Change Ticket.";
                return false;
            }

            if (WasNicknameChangedToday(Current.lastNicknameChangeDateUtc))
            {
                message = "You can change your nickname only once per day.";
                return false;
            }

            Current.nicknameChangeTickets -= 1;
            Current.nickname = validation.NormalizedNickname;
            Current.lastNicknameChangeDateUtc = DateTime.UtcNow.ToString("o");
            Current.updatedAtUtc = DateTime.UtcNow.ToString("o");
            Current.serverSyncPending = true;
            Save();
            message = "Nickname changed successfully.";
            return true;
        }

        public bool AddNicknameChangeTickets(int count)
        {
            if (count <= 0 || !Exists())
            {
                return false;
            }

            int currentTickets = Current.nicknameChangeTickets < 0 ? 0 : Current.nicknameChangeTickets;
            if (currentTickets >= MaxNicknameChangeTickets)
            {
                return false;
            }

            Current.nicknameChangeTickets = Math.Min(MaxNicknameChangeTickets, currentTickets + count);
            Current.updatedAtUtc = DateTime.UtcNow.ToString("o");
            Save();
            Debug.Log($"[PlayerProfile] Added nickname tickets count={count} total={Current.nicknameChangeTickets}");
            return true;
        }

        public bool UpdateGooglePlayLink(string googlePlayPlayerId, string error)
        {
            return UpdateGooglePlayLink(googlePlayPlayerId, string.Empty, error);
        }

        public bool UpdateGooglePlayLink(string googlePlayPlayerId, string googlePlayDisplayName, string error)
        {
            if (!Exists())
            {
                return false;
            }

            Current.lastAccountLinkAttemptUtc = DateTime.UtcNow.ToString("o");
            Current.accountLinkError = error ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(googlePlayPlayerId))
            {
                Current.googlePlayPlayerId = googlePlayPlayerId;
                Current.googlePlayDisplayName = googlePlayDisplayName ?? string.Empty;
                Current.linkedGooglePlay = true;
                Current.lastGooglePlaySignInAt = DateTime.UtcNow.ToString("o");
                Current.accountLinkError = string.Empty;
                Current.updatedAtUtc = DateTime.UtcNow.ToString("o");
            }

            Save();
            return true;
        }

        public bool UpdateGoogleLink(string googleAccountId, string googleEmailHash, string error)
        {
            if (!Exists())
            {
                return false;
            }

            Current.lastAccountLinkAttemptUtc = DateTime.UtcNow.ToString("o");
            Current.accountLinkError = error ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(googleAccountId))
            {
                Current.googleAccountId = googleAccountId;
                Current.googleEmailHash = googleEmailHash ?? string.Empty;
                Current.linkedGoogle = true;
                Current.accountLinkError = string.Empty;
                Current.updatedAtUtc = DateTime.UtcNow.ToString("o");
            }

            Save();
            return true;
        }

        public void SyncAppSettings(SettingsStore settingsStore)
        {
            if (settingsStore?.Current == null || !Exists())
            {
                return;
            }

            bool changed = false;
            AppSettings settings = settingsStore.Current;
            if (settings.playerId != Current.profileId)
            {
                settings.playerId = Current.profileId;
                changed = true;
            }

            if (settings.playerName != Current.nickname)
            {
                settings.playerName = Current.nickname;
                changed = true;
            }

            if (changed)
            {
                settingsStore.Save();
                Debug.Log($"[PlayerProfile] Synced AppSettings playerId={settings.playerId} playerName={settings.playerName}");
            }
        }

        private void Save()
        {
            if (current == null)
            {
                return;
            }

            Normalize(current);
            SaveService.SaveJson(FileName, current);
        }

        private static bool Normalize(PlayerProfile profile)
        {
            if (profile == null)
            {
                return false;
            }

            bool changed = false;
            if (profile.profileVersion < SaveDataValidator.CurrentSaveVersion)
            {
                profile.profileVersion = SaveDataValidator.CurrentSaveVersion;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(profile.profileId))
            {
                profile.profileId = Guid.NewGuid().ToString();
                changed = true;
            }

            NicknameValidationResult validation = NicknameValidator.Validate(profile.nickname);
            if (!validation.IsValid)
            {
                profile.nickname = "CubePlayer";
                changed = true;
            }
            else if (profile.nickname != validation.NormalizedNickname)
            {
                profile.nickname = validation.NormalizedNickname;
                changed = true;
            }

            int avatarId = ClampAvatarId(profile.avatarId);
            if (profile.avatarId != avatarId)
            {
                profile.avatarId = avatarId;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(profile.createdAtUtc))
            {
                profile.createdAtUtc = DateTime.UtcNow.ToString("o");
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(profile.updatedAtUtc))
            {
                profile.updatedAtUtc = profile.createdAtUtc;
                changed = true;
            }

            if (profile.googlePlayPlayerId == null)
            {
                profile.googlePlayPlayerId = string.Empty;
                changed = true;
            }

            if (profile.googlePlayDisplayName == null)
            {
                profile.googlePlayDisplayName = string.Empty;
                changed = true;
            }

            if (profile.lastGooglePlaySignInAt == null)
            {
                profile.lastGooglePlaySignInAt = string.Empty;
                changed = true;
            }

            if (profile.googleAccountId == null)
            {
                profile.googleAccountId = string.Empty;
                changed = true;
            }

            if (profile.googleEmailHash == null)
            {
                profile.googleEmailHash = string.Empty;
                changed = true;
            }

            bool linkedGooglePlay = !string.IsNullOrWhiteSpace(profile.googlePlayPlayerId);
            if (profile.linkedGooglePlay != linkedGooglePlay)
            {
                profile.linkedGooglePlay = linkedGooglePlay;
                changed = true;
            }

            bool linkedGoogle = !string.IsNullOrWhiteSpace(profile.googleAccountId);
            if (profile.linkedGoogle != linkedGoogle)
            {
                profile.linkedGoogle = linkedGoogle;
                changed = true;
            }

            if (profile.lastAccountLinkAttemptUtc == null)
            {
                profile.lastAccountLinkAttemptUtc = string.Empty;
                changed = true;
            }

            if (profile.accountLinkError == null)
            {
                profile.accountLinkError = string.Empty;
                changed = true;
            }

            if (profile.lastNicknameChangeDateUtc == null)
            {
                profile.lastNicknameChangeDateUtc = string.Empty;
                changed = true;
            }

            if (profile.nicknameChangeTickets < 0)
            {
                profile.nicknameChangeTickets = 0;
                changed = true;
            }

            if (profile.serverSyncError == null)
            {
                profile.serverSyncError = string.Empty;
                changed = true;
            }

            if (profile.lastServerSyncAttemptUtc == null)
            {
                profile.lastServerSyncAttemptUtc = string.Empty;
                changed = true;
            }

            return changed;
        }

        private static int ClampAvatarId(int avatarId)
        {
            return Math.Max(0, Math.Min(3, avatarId));
        }

        private static bool WasNicknameChangedToday(string utcTimestamp)
        {
            if (string.IsNullOrWhiteSpace(utcTimestamp)
                || !DateTime.TryParse(utcTimestamp, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime changedAt))
            {
                return false;
            }

            return changedAt.ToUniversalTime().Date == DateTime.UtcNow.Date;
        }

        private static string ToUserMessage(NicknameValidationResult validation)
        {
            switch (validation.Error)
            {
                case NicknameValidationError.Empty:
                    return "Please enter a nickname.";
                case NicknameValidationError.TooLong:
                    return "Nickname is too long.";
                case NicknameValidationError.InvalidCharacters:
                    return "Nickname contains invalid characters.";
                case NicknameValidationError.BannedWord:
                case NicknameValidationError.ReservedName:
                    return "This nickname is not allowed.";
                default:
                    return string.IsNullOrWhiteSpace(validation.Message) ? "Invalid nickname." : validation.Message;
            }
        }
    }
}
