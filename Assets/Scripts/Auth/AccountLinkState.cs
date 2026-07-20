using System;

namespace CubeChallenge3D.Auth
{
    [Serializable]
    public sealed class AccountLinkState
    {
        public bool available;
        public bool success;
        public bool cancelled;
        public string provider;
        public string providerUserId;
        public string displayName;
        public string emailHash;
        public string message;

        public static AccountLinkState Unavailable(string provider, string message)
        {
            return new AccountLinkState
            {
                available = false,
                success = false,
                provider = provider,
                message = message
            };
        }
    }
}
