using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GamePackages
{
    [Serializable]
    public class TimeManager : ISaveData<TimeManager>
    {
        private const string SaveKey = "TimeManager";
        private const string RewardDateFormat = "yyyy-MM-dd";

        [Tooltip("Dùng giờ máy để thử thời gian và bỏ yêu cầu Internet; độc lập với chế độ thử màn chơi.")]
        public bool cheatTime;
        public string sLastOnlineTime;
        public string lastRewardDate;
        public int localUtcOffsetMinutes;
        public bool hasLocalUtcOffset;
        [NonSerialized] public bool isInitialized;

        private DateTime syncedUtc;
        private double syncRealtime;
        private DateTime savedLocalTime = DateTime.UnixEpoch;
        private DateTime rewardDate;
        private DateTime cheatDate;
        private bool wasCheating;
        private readonly Dictionary<string, Action> dayCallbacks = new();

        // EN: Restores the confirmed local clock, fixed offset and cached reward date without trusting device time or starting a request.
        // VI: Khôi phục đồng hồ local đã xác nhận, offset cố định và cache ngày thưởng, không tin giờ máy hoặc tự gửi request.
        public void LoadData()
        {
            TimeManager data = OnLoadData(SaveKey);
            if (data == null) return;
            sLastOnlineTime = data.sLastOnlineTime;
            lastRewardDate = data.lastRewardDate;
            localUtcOffsetMinutes = data.localUtcOffsetMinutes;
            hasLocalUtcOffset = data.hasLocalUtcOffset;
            if (DateTime.TryParseExact(sLastOnlineTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime saved))
            {
                savedLocalTime = saved;
                if (string.IsNullOrEmpty(lastRewardDate)) lastRewardDate = saved.ToString(RewardDateFormat, CultureInfo.InvariantCulture);
            }
            DateTime.TryParseExact(lastRewardDate, RewardDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out rewardDate);
        }

        // EN: Registers one daily callback under its unique key.
        // VI: Đăng ký một callback theo ngày bằng khóa duy nhất.
        public void RegisterNewDayCallback(string key, Action callback)
        {
            if (!dayCallbacks.ContainsKey(key)) dayCallbacks.Add(key, callback);
        }

        // EN: Removes the daily callback owned by the given key.
        // VI: Gỡ callback theo ngày thuộc khóa được truyền vào.
        public void UnregisterNewDayCallback(string key)
        {
            dayCallbacks.Remove(key);
        }

        // EN: Accepts verified UTC, selects the local offset once and checks the reward day immediately; cheat mode leaves trusted day state unchanged.
        // VI: Nhận UTC đã xác thực, chỉ chọn offset local một lần và kiểm tra ngày thưởng ngay; chế độ cheat giữ nguyên trạng thái ngày thật.
        public void Synchronize(DateTime utcTime)
        {
            syncedUtc = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            syncRealtime = Time.realtimeSinceStartupAsDouble;
            isInitialized = true;
            if (!hasLocalUtcOffset)
            {
                TimeZoneInfo.ClearCachedData();
                localUtcOffsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(syncedUtc).TotalMinutes;
                hasLocalUtcOffset = true;
            }
            if (!cheatTime) CheckRewardDay();
        }

        // EN: Checks for a new game day using the trusted clock or cheat device date; changing cheat mode only resets its test baseline.
        // VI: Kiểm tra ngày game mới theo đồng hồ đáng tin hoặc ngày máy khi cheat; đổi chế độ cheat chỉ đặt lại mốc test.
        public void CheckForNewDay()
        {
            bool cheating = cheatTime;
            if (cheating != wasCheating)
            {
                wasCheating = cheating;
                cheatDate = GetNow().Date;
                return;
            }
            if (cheating)
            {
                DateTime today = GetNow().Date;
                if (today == cheatDate) return;
                cheatDate = today;
                NotifyNewDay();
            }
            else if (isInitialized) CheckRewardDay();
        }

        // EN: Uses device time only with cheatTime; otherwise returns verified UTC plus Unity realtime using the saved fixed local offset.
        // VI: Chỉ dùng giờ máy khi bật cheatTime; bình thường trả UTC đã xác nhận cộng thời gian thực Unity theo offset local cố định đã lưu.
        public DateTime GetNow()
        {
            if (cheatTime) return DateTime.Now;
            if (!isInitialized) return savedLocalTime;
            double elapsed = Time.realtimeSinceStartupAsDouble - syncRealtime;
            return DateTime.SpecifyKind(syncedUtc.AddSeconds(elapsed).AddMinutes(localUtcOffsetMinutes), DateTimeKind.Unspecified);
        }

        // EN: Saves confirmed time and real reward-day state; this clock's cheatTime never overwrites trusted time.
        // VI: Lưu giờ đã xác nhận và trạng thái ngày thưởng thật; cheatTime của đồng hồ không ghi đè giờ đáng tin.
        public void SaveData()
        {
            if (!isInitialized || cheatTime) return;
            sLastOnlineTime = TimeUtil.DateTimeToString(GetNow());
            OnSaveData(SaveKey);
            PlayerPrefs.Save();
        }

        // EN: Clears the persisted clock alongside the profile reset.
        // VI: Xóa đồng hồ đã lưu cùng thao tác đặt lại hồ sơ.
        [Button]
        public void ClearData()
        {
            PlayerPrefs.DeleteKey(SaveKey);
        }

        // EN: Saves the first verified local date or opens one later reward day; cached dates avoid parsing or formatting every frame.
        // VI: Lưu ngày local đã xác thực đầu tiên hoặc mở một ngày thưởng mới hơn; cache ngày để tránh parse hay định dạng mỗi frame.
        private void CheckRewardDay()
        {
            DateTime today = GetNow().Date;
            if (today <= rewardDate) return;

            bool isNewDay = rewardDate != default;
            rewardDate = today;
            lastRewardDate = today.ToString(RewardDateFormat, CultureInfo.InvariantCulture);
            if (isNewDay) NotifyNewDay();
            SaveData();
        }

        // EN: Notifies feature callbacks and the existing global new-day event.
        // VI: Báo callback tính năng và sự kiện sang ngày toàn cục hiện có.
        private void NotifyNewDay()
        {
            foreach (Action callback in dayCallbacks.Values) callback?.Invoke();
            Observer.OnNewday?.Invoke();
        }
    }
}
