using System;

namespace CubeChallenge3D.Economy
{
    [Serializable]
    public sealed class EconomyWallet
    {
        public int saveVersion;
        public int coins;
        public int gems;
        public int hearts;
        public string lastHeartRegenTimeUtc;
    }
}
