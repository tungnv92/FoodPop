using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace GamePackages
{
    [System.Serializable]
    public class RecordTrackingManager : ContextData<RecordTrackingData>, IFeatureNewDay, IFeatureTrackOpen, IRequirePlayerData
    {
        public static string sFormatDate = "yyyy-MM-dd HH:mm:ss";
        private PlayerDataManager PlayerData;
        public void SetPlayerDataManager(PlayerDataManager _dataManager)
        {
            PlayerData = _dataManager;
        }
        bool IsTrackFirstDate;

        // EN: Records first open and last play timestamps when the application opens.
        // VI: Ghi nhận thời gian mở lần đầu và lần chơi gần nhất khi mở ứng dụng.
        public void OnTrackingOpenApp()
        {
            IsTrackFirstDate = PlayerPrefs.HasKey("IsTrackFirstDate");
            if (!IsTrackFirstDate)
            {
                IsTrackFirstDate = true;
                PlayerPrefs.SetInt("IsTrackFirstDate", 1);
                Data.first_open_time = DateTime.Now.ToString(sFormatDate);
                //SDKBridge.TrackUserProperty(UserProperty.first_open_time, Data.first_open_time);
                //SDKBridge.TrackUserPropertyLevel(1);
            }
            Data.last_time_play = DateTime.Now.ToString(sFormatDate);
            //SDKBridge.TrackUserProperty(UserProperty.last_time_play, Data.last_time_play);
            //SDKBridge.TrackUserProperty(UserProperty.user_retention, GetDayUserRetention().ToString());
        }
        public int GetDayUserRetention()
        {
            DateTime last = TimeUtil.StringToDateTime(Data.last_time_play);
            DateTime first = TimeUtil.StringToDateTime(Data.first_open_time);
            return (int)(last - first).TotalDays;
        }

        // EN: Updates the last play timestamp when a new day starts.
        // VI: Cập nhật thời gian chơi gần nhất khi bước sang ngày mới.
        public void OnNewDay()
        {
            Data.last_time_play = DateTime.Now.ToString(sFormatDate);
            //SDKBridge.TrackUserProperty(UserProperty.last_time_play, Data.last_time_play);
            //SDKBridge.TrackUserProperty(UserProperty.user_retention, GetDayUserRetention().ToString());
        }

        // EN: Increments the recorded in-app purchase count.
        // VI: Tăng số lượng giao dịch mua trong ứng dụng được ghi nhận.
        public void RecordIAP(int value = 1)
        {
            Data.iap_count += value;
            //SDKBridge.TrackUserProperty(UserProperty.iap_count, Data.iap_count.ToString());
        }
        public int GetIAPCount()
        {
            return (int)Data.iap_count;
        }

        public int GetRetryCount(int level)
        {
            if (Data.retryCount <= 0) return -1;
            return Data.retryCount;
        }

        // EN: Adds the earned booster amount to the tracking data.
        // VI: Cộng số lượng booster nhận được vào dữ liệu theo dõi.
        public void RecordBoosterEarn(int amount)
        {
            Data.booster_earn_count += amount;
            //SDKBridge.TrackUserProperty(UserProperty.booster_earn_count, Data.booster_earn_count.ToString());
        }

        // EN: Adds the spent booster amount to total and per-level tracking data.
        // VI: Cộng số lượng booster đã dùng vào dữ liệu theo dõi tổng và theo màn chơi.
        public void RecordBoosterSpend(int amount = 1)
        {
            Data.booster_spend_count += amount;
            Data.booster_spend_level += amount;
            //SDKBridge.TrackUserProperty(UserProperty.booster_spend_count, Data.booster_spend_count.ToString());
        }

        // EN: Adds the earned coin amount to the tracking data.
        // VI: Cộng số xu nhận được vào dữ liệu theo dõi.
        public void RecordCoinEarn(int value)
        {
            Data.value_coins_earn_count += value;
            //SDKBridge.TrackUserProperty(UserProperty.value_coins_earn_count, Data.value_coins_earn_count.ToString());
        }

        // EN: Adds the spent coin amount to total and per-level tracking data.
        // VI: Cộng số xu đã tiêu vào dữ liệu theo dõi tổng và theo màn chơi.
        public void RecordCoinSpend(int value)
        {
            Data.value_coins_spend_count += value;
            Data.coin_spend_level += value;
            //SDKBridge.TrackUserProperty(UserProperty.value_coins_spend_count, Data.value_coins_spend_count.ToString());
        }

        // EN: Increments the rewarded ad watch count.
        // VI: Tăng số lần xem quảng cáo nhận thưởng.
        public void RecordAdsReward()
        {
            Data.ads_reward_count++;
            //SDKBridge.TrackUserProperty(UserProperty.ads_reward_count, Data.ads_reward_count.ToString());
        }
        public void RecordAdsReward_Level()
        {
            Data.ads_count_level++;
        }

        // EN: Increments the interstitial ad impression count.
        // VI: Tăng số lần hiển thị quảng cáo xen kẽ.
        public void RecordInterShow()
        {
            Data.ads_inter_count++;
            //SDKBridge.TrackUserProperty(UserProperty.ads_inter_count, Data.ads_inter_count.ToString());
        }

        // EN: Marks the start checkpoint for the specified level if not yet logged.
        // VI: Đánh dấu mốc bắt đầu cho màn chơi chỉ định nếu chưa được ghi nhận.
        public void RecordCheckPointStart(int level)
        {
            if (!PlayerPrefs.HasKey("log_checkpoint_start_" + level))
            {
                //SDKBridge.TrackEventCheckPointStart(level, GetDayUserRetention());
                PlayerPrefs.SetInt("log_checkpoint_start_" + level, 1);
            }
        }

        // EN: Records the completion checkpoint for the specified level and duration.
        // VI: Ghi nhận mốc hoàn thành cho màn chơi và thời lượng chỉ định.
        public void RecordCheckPointEnd(int level, int duration)
        {
            //SDKBridge.TrackEventCheckPointEnd(level, Data.retryCount, duration, Data.booster_spend_level, Data.coin_spend_level, Data.ads_count_level, GetDayUserRetention());
        }

        public void RecordWinStreak()
        {
            Data.win_streak++;
        }
        public int GetWinStreak()
        {
            return Data.win_streak;
        }
        public void ResetWinStreak()
        {
            Data.win_streak = 0;
            Data.retryCount++;
        }
        public void ResetWin()
        {
            Data.retryCount = 0;
            Data.booster_spend_level = 0;
            Data.coin_spend_level = 0;
            Data.ads_count_level = 0;
        }
    }
    [Serializable]
    public class RecordTrackingData
    {
        // normal
        public int level;
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

        public RecordTrackingData()
        {
            last_time_play = DateTime.Now.ToString(RecordTrackingManager.sFormatDate);
            iap_count = 0;
            win_streak = 0;
            level = 1;
        }

    }

}
