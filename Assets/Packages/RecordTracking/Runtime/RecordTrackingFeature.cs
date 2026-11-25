using UnityEngine;
using System;
namespace GamePackages
{
    public sealed class RecordTrackingFeature : FeatureManager<RecordTrackingData, RecordTrackingConfig>, IRecordTracking, IFeatureNewDay, IFeatureTrackOpen
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        // EN: Returns the feature enable switch from its configuration.
        // VI: Trả trạng thái bật tính năng từ config.
        public override bool IsEnabledFeature()
        {
            return Config.isFeatureEnabled;
        }

        // EN: Records verified first-open and last-play times; ProfileManager saves after all startup changes.
        // VI: Ghi giờ mở đầu và chơi gần nhất đã xác thực; ProfileManager lưu sau mọi thay đổi khởi động.
        public void OnTrackingOpenApp()
        {
            string now = Services.Time.GetNow().ToString(DateFormat);
            if (string.IsNullOrEmpty(Data.first_open_time)) Data.first_open_time = now;
            Data.last_time_play = now;
            //SDKBridge.TrackUserProperty(UserProperty.last_time_play, Data.last_time_play);
            //SDKBridge.TrackUserProperty(UserProperty.user_retention, GetDayUserRetention().ToString());
        }
        // EN: Returns elapsed retention days from the recorded first-open and last-play timestamps.
        // VI: Trả số ngày giữ chân từ thời điểm mở đầu và chơi gần nhất đã ghi.
        public int GetDayUserRetention()
        {
            DateTime last = TimeUtil.StringToDateTime(Data.last_time_play);
            DateTime first = TimeUtil.StringToDateTime(Data.first_open_time);
            return (int)(last - first).TotalDays;
        }

        // EN: Updates the verified last-play timestamp; ProfileManager saves after every new-day hook completes.
        // VI: Cập nhật giờ chơi gần nhất đã xác thực; ProfileManager lưu sau khi mọi hook ngày mới hoàn tất.
        public void OnNewDay()
        {
            Data.last_time_play = Services.Time.GetNow().ToString(DateFormat);
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
        // EN: Returns the recorded number of in-app purchases.
        // VI: Trả số lần mua hàng trong ứng dụng đã ghi.
        public int GetIAPCount()
        {
            return (int)Data.iap_count;
        }

        // EN: Returns the current level retry count, or -1 before any defeat; keeps the existing level parameter contract.
        // VI: Trả số lần thử lại của màn hiện tại, hoặc -1 khi chưa thua; giữ hợp đồng tham số level hiện có.
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
        // EN: Counts one rewarded ad in the current level.
        // VI: Đếm một quảng cáo thưởng trong màn hiện tại.
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

        // EN: Adds one win to the current gameplay win streak.
        // VI: Cộng một chiến thắng vào chuỗi thắng gameplay hiện tại.
        public void RecordWinStreak()
        {
            Data.win_streak++;
        }
        // EN: Returns the current gameplay win streak.
        // VI: Trả chuỗi thắng gameplay hiện tại.
        public int GetWinStreak()
        {
            return Data.win_streak;
        }
        // EN: Resets the win streak after any lost or abandoned attempt.
        // VI: Reset chuỗi thắng sau mọi lượt thua hoặc bỏ dở.
        public void ResetWinStreak()
        {
            Data.win_streak = 0;
        }

        // EN: Counts one real defeat of the current level; ResetWin clears it when the level is won.
        // VI: Đếm một lần thua thật ở màn hiện tại; ResetWin xóa số này khi thắng màn.
        public void RecordRetry()
        {
            Data.retryCount++;
        }
        // EN: Clears retry and per-level spending and ad counters after a win.
        // VI: Xóa số lần thử lại cùng bộ đếm chi tiêu và quảng cáo theo màn sau khi thắng.
        public void ResetWin()
        {
            Data.retryCount = 0;
            Data.booster_spend_level = 0;
            Data.coin_spend_level = 0;
            Data.ads_count_level = 0;
        }
    }
}
