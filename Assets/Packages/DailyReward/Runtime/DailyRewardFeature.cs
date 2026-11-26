using UnityEngine;
using UnityEngine.Events;

namespace GamePackages
{
    // Daily reward: one free reward with cooldown between claims plus a chain of ad rewards, reset every day. All time comes from TimeManager (Services.Time).
    // Thưởng hằng ngày: phần thưởng miễn phí có thời gian chờ giữa các lần nhận và chuỗi thưởng xem quảng cáo, đặt lại mỗi ngày. Mọi thời gian lấy từ TimeManager (Services.Time).
    public class DailyRewardFeature : FeatureManager<DailyRewardData, DailyRewardGlobalConfig>, IFeatureNewDay
    {
        // EN: Free claims allowed per day, one per configured free reward.
        // VI: Số lượt nhận miễn phí mỗi ngày, bằng số phần thưởng miễn phí trong config.
        public int TotalAbleFreeClaim => Config.free_rewards.Count;
        // EN: Ad claims allowed per day, one per configured ad reward.
        // VI: Số lượt nhận quảng cáo mỗi ngày, bằng số phần thưởng quảng cáo trong config.
        public int TotalAbleAdsClaim => Config.ads_rewards.Count;

        public static DailyRewardFeature Instance => ProfileManager.instance.Features.Get<DailyRewardFeature>();
        // EN: Reads the free reward claim count without exposing the model.
        // VI: Đọc số lần nhận thưởng miễn phí mà không lộ model.
        public int TotalFreeClaimed => Data.totalFreeClaimed;

        public UnityAction onClaimedAction;

        // EN: Returns the feature enable switch from its configuration.
        // VI: Trả trạng thái bật tính năng từ config.
        public override bool IsEnabledFeature()
        {
            return Config.isFeatureEnabled;
        }

        // EN: Resets today's claims and the free-reward cooldown, then notifies listeners so an open popup refreshes every stage.
        // VI: Đặt lại lượt nhận trong ngày và thời gian chờ của thưởng miễn phí, rồi báo listener để popup đang mở làm mới mọi stage.
        public void OnNewDay()
        {
            Data.Reset();
            onClaimedAction?.Invoke();
        }

        // EN: Returns whether a stage is already claimed (stage 0 = free; also blocked while its cooldown runs, with the remaining time in blockTime).
        // VI: Trả về stage đã nhận chưa (stage 0 = miễn phí; cũng bị khóa khi đang chờ, thời gian còn lại nằm trong blockTime).
        public bool IsClaimedStage(int stage, out string blockTime)
        {
            blockTime = "";
            if (stage == 0)
            {
                if (Data.totalFreeClaimed >= TotalAbleFreeClaim)
                {
                    return true;
                }
                double remaining = GetFreeCooldownSeconds();
                if (remaining > 0)
                {
                    blockTime = TimeUtil.TimeToString(remaining);
                    return true;
                }
                return false;
            }

            return Data.totalAdsClaimed >= stage;
        }

        // EN: Returns whether the given ad stage is the next one to claim.
        // VI: Trả về stage quảng cáo này có phải là stage kế tiếp được nhận không.
        public bool IsEnableClaimAds(int stage)
        {
            return Data.totalAdsClaimed == stage - 1;
        }

        // EN: Claims a stage only when it is claimable now (free stage off cooldown, or the next ad stage): records the claim, runs grantReward (whose single profile save also stores this claim) and notifies listeners. Returns false and grants nothing otherwise.
        // VI: Chỉ nhận stage khi đang nhận được (stage miễn phí hết thời gian chờ, hoặc stage quảng cáo kế tiếp): ghi lượt nhận, chạy grantReward (lần lưu hồ sơ duy nhất của nó lưu luôn lượt nhận này) và báo listener. Trả false và không cộng gì nếu không hợp lệ.
        public bool TryClaimStage(int stage, UnityAction grantReward)
        {
            bool claimable = stage == 0 ? !IsClaimedStage(0, out _) : IsEnableClaimAds(stage);
            if (!claimable) return false;
            RecordClaim(stage);
            grantReward.Invoke();
            onClaimedAction?.Invoke();
            return true;
        }

        // EN: Returns the reward config of a stage (stage 0 = the current free reward).
        // VI: Trả về config phần thưởng của stage (stage 0 = phần thưởng miễn phí hiện tại).
        public DailyRewardConfig GetConfig(int stage)
        {
            if (stage == 0) return Config.GetFreeReward(Data.totalFreeClaimed);
            return Config.ads_rewards.Find(x => x.stageID == stage);
        }

        // EN: Returns the ad-claim progress bar value in 0..1.
        // VI: Trả về giá trị thanh tiến trình nhận thưởng quảng cáo trong khoảng 0..1.
        public float GetProcessAdsClaim()
        {
            return Mathf.Clamp01((float)(Data.totalAdsClaimed - 1) / (float)(TotalAbleAdsClaim - 1));

        }

        // EN: Returns how many ad rewards were claimed today.
        // VI: Trả về số thưởng quảng cáo đã nhận hôm nay.
        public int GetTotalAdsClaimed()
        {
            return Data.totalAdsClaimed;
        }

        // EN: Returns whether any reward can be claimed now (free reward off cooldown, or ad rewards left).
        // VI: Trả về hiện có phần thưởng nào nhận được không (thưởng miễn phí hết thời gian chờ, hoặc còn thưởng quảng cáo).
        public bool IsGoodToClaim()
        {
            if (Data.totalFreeClaimed < TotalAbleFreeClaim && GetFreeCooldownSeconds() <= 0) return true;
            if (Data.totalAdsClaimed < TotalAbleAdsClaim) return true;
            return false;
        }

        // EN: Seconds left before the next free reward unlocks by the TimeManager clock; 0 or less when there is no cooldown.
        // VI: Số giây còn lại trước khi mở thưởng miễn phí kế tiếp theo đồng hồ TimeManager; 0 hoặc âm khi không phải chờ.
        private double GetFreeCooldownSeconds()
        {
            if (string.IsNullOrEmpty(Data.nextFreeClaimTime)) return 0;
            return (TimeUtil.StringToDateTime(Data.nextFreeClaimTime) - Services.Time.GetNow()).TotalSeconds;
        }

        // EN: Updates today's claim counters and starts the next free-reward cooldown when another free claim remains; does not save.
        // VI: Cập nhật bộ đếm lượt nhận trong ngày và bắt đầu thời gian chờ thưởng miễn phí kế tiếp khi còn lượt; không lưu.
        private void RecordClaim(int stage)
        {
            switch (stage)
            {
                case 0:
                    Data.totalFreeClaimed++;
                    if (Data.totalFreeClaimed < TotalAbleFreeClaim)
                    {
                        int cappingTimeFree = Config.free_rewards[Data.totalFreeClaimed].cappingTimeFree;
                        Data.nextFreeClaimTime = TimeUtil.DateTimeToString(Services.Time.GetNow().AddSeconds(cappingTimeFree));
                    }
                    break;

                case 1:
                case 2:
                case 3:
                case 4:
                    Data.totalAdsClaimed++;
                    break;
            }
        }
    }
}
