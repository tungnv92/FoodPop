using System;

namespace GamePackages
{
    [Serializable]
    public class RecordTrackingData
    {
        public int level = 1;
        public int retryCount;
        public int winstreak;
        public int user_retention;
        public string first_open_time;
        public string last_time_play;
        public int iap_count;
        public int win_streak;
        public int booster_spend_count;
        public int booster_earn_count;
        public int ads_reward_count;
        public int ads_inter_count;
        public int value_coins_earn_count;
        public int value_coins_spend_count;
        public int booster_spend_level;
        public int coin_spend_level;
        public int ads_count_level;

    }

}
