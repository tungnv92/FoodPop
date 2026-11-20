using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GamePackages
{
    // Lives system: hearts refill one at a time on a timer and unlimited lives last until a moment. All time comes from TimeManager (Services.Time), so offline time is caught up automatically.
    // Hệ thống mạng: mạng hồi từng cái theo thời gian, mạng vô hạn kéo dài tới một mốc. Mọi thời gian lấy từ TimeManager (Services.Time) nên thời gian offline tự được tính bù.
    public class HeartFeature : FeatureManager<HeartData, HeartConfig>, IHeart, IFeatureUpdate
    {
        public const int DefaultMaxHeart = 5;

        public static HeartFeature Instance => ProfileManager.instance.Features.Get<HeartFeature>();
        private DateTime Now => Services.Time.GetNow();
        private long RefillTicks => TimeSpan.FromMinutes(Config.refillMinutesPerHeart).Ticks;

        // EN: Returns the feature enable switch from its configuration.
        // VI: Trả trạng thái bật tính năng từ config.
        public override bool IsEnabledFeature()
        {
            return Config.isFeatureEnabled;
        }

        // EN: Catches up elapsed refills and starts missing timers only during updates after the profile has verified time.
        // VI: Bù mạng hồi đã tới hạn và tạo bộ đếm còn thiếu chỉ khi cập nhật sau lúc hồ sơ đã xác thực giờ.
        public void OnUpdate()
        {
            RefillByTime();
            StartRefillIfNeeded();
        }

        // EN: True when the player may start a level (unlimited lives or at least one heart).
        // VI: True khi người chơi được vào màn (đang có mạng vô hạn hoặc còn ít nhất một mạng).
        public bool IsEnoughHeartToPlay()
        {
            if (IsUnlimitedLives()) return true;
            return Data.currentHeartAmount > 0;
        }

        // EN: Spends one heart unless lives are unlimited and starts the refill timer when it is not running.
        // VI: Trừ một mạng trừ khi đang vô hạn và bắt đầu đếm hồi mạng nếu chưa chạy.
        public void UseHeart()
        {
            if (IsUnlimitedLives()) return;
            Data.currentHeartAmount = Mathf.Max(0, Data.currentHeartAmount - 1);
            StartRefillIfNeeded();
        }

        // EN: Adds hearts, capped at the maximum unless isLimitMax is false; stops the timer once full.
        // VI: Cộng mạng, giới hạn ở mức tối đa trừ khi isLimitMax là false; dừng bộ đếm khi đã đầy.
        public void AddHeart(int amount = 1, bool isLimitMax = true)
        {
            Data.currentHeartAmount += amount;
            if (isLimitMax) Data.currentHeartAmount = Mathf.Clamp(Data.currentHeartAmount, 0, Data.maxHeart);
            if (IsMaxHeart()) Data.nextHeartTicks = 0;
        }

        // EN: Refills hearts to the maximum and stops the refill timer.
        // VI: Hồi đầy mạng và dừng bộ đếm hồi mạng.
        [Button]
        public void FillFullHeart()
        {
            Data.currentHeartAmount = Data.maxHeart;
            Data.nextHeartTicks = 0;
        }

        // EN: Adds minutes of unlimited lives, extending from now or from the current end time if still running.
        // VI: Cộng số phút mạng vô hạn, tính từ bây giờ hoặc từ mốc kết thúc hiện tại nếu vẫn còn.
        [Button(ButtonStyle.FoldoutButton)]
        public void AddUnlimitedLiveTime(int minute)
        {
            long start = Math.Max(Now.Ticks, Data.unlimitedUntilTicks);
            Data.unlimitedUntilTicks = start + TimeSpan.FromMinutes(minute).Ticks;
        }

        // EN: True while the unlimited-lives end moment is in the future.
        // VI: True khi mốc hết mạng vô hạn vẫn ở tương lai.
        public bool IsUnlimitedLives()
        {
            return Data.unlimitedUntilTicks > Now.Ticks;
        }

        // EN: True when hearts are at the maximum.
        // VI: True khi mạng đang ở mức tối đa.
        public bool IsMaxHeart()
        {
            return Data.currentHeartAmount >= Data.maxHeart;
        }

        // EN: Remaining hearts, or -1 while lives are unlimited.
        // VI: Số mạng còn lại, hoặc -1 khi đang có mạng vô hạn.
        public int GetRemainHeart()
        {
            if (IsUnlimitedLives()) return -1;
            return Data.currentHeartAmount;
        }

        // EN: Text for the lives widget: (heart count, timer or "FULL").
        // VI: Chữ cho ô hiển thị mạng: (số mạng, thời gian hoặc "FULL").
        public (string, string) GetHeartDisplay()
        {
            if (IsUnlimitedLives()) return ("", GetTimerText());
            if (IsMaxHeart()) return (Data.maxHeart + "", "FULL");
            return ("" + Data.currentHeartAmount, GetTimerText());
        }

        // EN: Raises the maximum to the Master Pass value and starts refilling if hearts are now below it.
        // VI: Nâng mức tối đa theo Master Pass và bắt đầu hồi mạng nếu mạng đang thấp hơn.
        public void OnActiveMasterPass()
        {
            Data.maxHeart = Config.masterPassMaxHeart;
            StartRefillIfNeeded();
        }

        // EN: Returns the maximum to the default value.
        // VI: Trả mức tối đa về giá trị mặc định.
        public void OnEndMasterPass()
        {
            Data.maxHeart = DefaultMaxHeart;
        }

        // EN: Adds one heart for every refill moment that has passed, scheduling the next one until full.
        // VI: Cộng một mạng cho mỗi mốc hồi đã qua, hẹn mốc kế tiếp tới khi đầy.
        private void RefillByTime()
        {
            long now = Now.Ticks;
            while (Data.nextHeartTicks > 0 && now >= Data.nextHeartTicks && !IsMaxHeart())
            {
                Data.currentHeartAmount++;
                Data.nextHeartTicks = IsMaxHeart() ? 0 : Data.nextHeartTicks + RefillTicks;
            }
            if (IsMaxHeart()) Data.nextHeartTicks = 0;
        }

        // EN: Starts the refill timer when hearts are below the maximum and no timer is running.
        // VI: Bắt đầu đếm hồi mạng khi mạng thấp hơn tối đa và chưa có bộ đếm.
        private void StartRefillIfNeeded()
        {
            if (!IsMaxHeart() && Data.nextHeartTicks == 0)
                Data.nextHeartTicks = Now.Ticks + RefillTicks;
        }

        // EN: Remaining time text of unlimited lives, else of the next refill, else empty.
        // VI: Chữ thời gian còn lại của mạng vô hạn, nếu không thì của lần hồi kế tiếp, nếu không thì rỗng.
        private string GetTimerText()
        {
            long now = Now.Ticks;
            if (Data.unlimitedUntilTicks > now) return TimeUtil.TimeToString(TimeSpan.FromTicks(Data.unlimitedUntilTicks - now).TotalSeconds);
            if (Data.nextHeartTicks > now) return TimeUtil.TimeToString(TimeSpan.FromTicks(Data.nextHeartTicks - now).TotalSeconds);
            return "";
        }
    }
}
