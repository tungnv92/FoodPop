using UnityEngine;

namespace GamePackages
{
    // Daily login gift: seven daily rewards claimed one per day, reset after day seven is claimed.
    // Quà đăng nhập hằng ngày: bảy phần thưởng, mỗi ngày nhận một, đặt lại sau khi nhận ngày thứ bảy.
    public class DailyGiftFeature : FeatureManager<DailyGiftData, DailyGiftRewardConfig>, IFeatureNewDay
    {
        public const int MAX_DAY = 7;

        public static DailyGiftFeature Instance => ProfileManager.instance.Features.Get<DailyGiftFeature>();

        // EN: Advances to the next day when today's gift was claimed, resetting the cycle after day seven.
        // VI: Chuyển sang ngày kế tiếp khi quà hôm nay đã nhận, đặt lại chu kỳ sau ngày thứ bảy.
        public void OnNewDay()
        {
            if (Data.day >= MAX_DAY && IsClaimed(MAX_DAY))
            {
                Data.Reset();
                Debug.Log("Daily Gift Reset");
            }
            ValidateDay();
            if (!IsClaimed(Data.day)) return;
            Data.day++;
            ValidateDay();
            Data.isEnableClaim = true;
        }

        // EN: Returns the daily gift enable switch from its configuration.
        // VI: Trả trạng thái bật quà hằng ngày từ config.
        public override bool IsEnabledFeature()
        {
            return Config.isFeatureEnabled;
        }

        // EN: Returns whether today's gift can still be claimed.
        // VI: Trả về việc quà hôm nay còn nhận được hay không.
        public bool IsGoodToClaim()
        {
            return Data.isEnableClaim;
        }

        // EN: Returns whether the given day (1-based) was claimed; days beyond the list count as claimed.
        // VI: Trả về ngày (tính từ 1) đã nhận hay chưa; ngày vượt quá danh sách coi như đã nhận.
        public bool IsClaimed(int day)
        {
            if (day > Data.l_ClaimedChecker.Count)
                return true;
            return Data.l_ClaimedChecker[day - 1];
        }

        // EN: Marks today's gift as claimed, disables claiming until the next day and saves.
        // VI: Đánh dấu quà hôm nay đã nhận, khóa nhận đến ngày mới và lưu lại.
        public void OnClaimComplete()
        {
            ValidateDay();
            int day = Data.day;
            Data.isEnableClaim = false;
            Data.l_ClaimedChecker[day - 1] = true;
            Save();
        }

        // EN: Returns the current gift day (1-based), clamped to the valid range.
        // VI: Trả về ngày quà hiện tại (tính từ 1), đã giới hạn trong khoảng hợp lệ.
        public int GetCurrentDayId()
        {
            ValidateDay();
            return Data.day;
        }

        // EN: Clamps the saved day into the 1..MAX_DAY range.
        // VI: Giới hạn ngày đã lưu trong khoảng 1..MAX_DAY.
        private void ValidateDay()
        {
            Data.day = Mathf.Clamp(Data.day, 1, MAX_DAY);
        }
    }
}
