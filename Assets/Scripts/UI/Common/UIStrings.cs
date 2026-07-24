namespace CubeChallenge3D.UI.Common
{
    public static class UIStrings
    {
        public static string Back => Text("back");
        public static string Close => Text("close");
        public static string Start => Text("start");
        public static string Retry => Text("retry");
        public static string Continue => Text("continue");
        public static string ComingSoon => Text("coming_soon");
        public static string NoRecords => Text("no_records");
        public static string Purchased => Text("purchased");
        public static string Equipped => Text("equipped");
        public static string AlreadyOwned => Text("already_owned");
        public static string NotEnoughCoins => Text("not_enough_coins");
        public static string NotEnoughGems => Text("not_enough_gems");
        public static string Hearts => Text("hearts");
        public static string UsesOneHeart => Text("uses_one_heart");
        public static string NotEnoughHearts => Text("not_enough_hearts");
        public static string HeartsRecharge => Text("hearts_recharge");
        public static string NextHeartIn => Text("next_heart_in");
        public static string GetMoreHearts => Text("get_more_hearts");
        public static string NoHeartsRequired => Text("no_hearts_required");
        public static string GoToShop => Text("go_to_shop");
        public static string RewardClaimed => Text("reward_claimed");
        public static string AdNotReady => Text("ad_not_ready");
        public static string AdNotCompleted => Text("ad_not_completed");
        public static string AdsUnavailable => Text("ads_unavailable");

        private static string Text(string key)
        {
            return CubeChallenge3D.Core.LocalizationManager.Instance != null
                ? CubeChallenge3D.Core.LocalizationManager.Instance.GetText(key)
                : key;
        }
    }
}
