using UnityEngine;
using UnityEngine.Events;

namespace GamePackages
{
    // Daily reward: one free reward with cooldown between claims plus a chain of ad rewards, reset every day. All time comes from TimeManager (Services.Time).
    // Thưởng hằng ngày: phần thưởng miễn phí có thời gian chờ giữa các lần nhận và chuỗi thưởng xem quảng cáo, đặt lại mỗi ngày. Mọi thời gian lấy từ TimeManager (Services.Time).
    public class DailyRewardFeature : FeatureManager<DailyRewardData, DailyRewardGlobalConfig>, IFeatureNewDay
    {
        public static int totalAbleFreeClaim = 3;
        public static int totalAbleAdsClaim = 4;

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

        // EN: Resets today's claims and the free-reward cooldown.
        // VI: Đặt lại lượt nhận trong ngày và thời gian chờ của thưởng miễn phí.
        public void OnNewDay()
        {
            Data.Reset();
        }

        // EN: Returns whether a stage is already claimed (stage 0 = free; also blocked while its cooldown runs, with the remaining time in blockTime).
        // VI: Trả về stage đã nhận chưa (stage 0 = miễn phí; cũng bị khóa khi đang chờ, thời gian còn lại nằm trong blockTime).
        public bool IsClaimedStage(int stage, out string blockTime)
        {
            blockTime = "";
            if (stage == 0)
            {
                if (Data.totalFreeClaimed >= totalAbleFreeClaim)
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

        // EN: Records a claim for the stage, starts the free-reward cooldown when another free claim remains, notifies listeners and saves.
        // VI: Ghi nhận lượt nhận của stage, bắt đầu thời gian chờ thưởng miễn phí khi còn lượt miễn phí, báo cho listener và lưu.
        public void OnClaimStage(int stage)
        {
            switch (stage)
            {
                case 0:
                    Data.totalFreeClaimed++;
                    if (Data.totalFreeClaimed < totalAbleFreeClaim && Data.totalFreeClaimed < Config.free_rewards.Count)
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
            onClaimedAction?.Invoke();
            Save();
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
            return Mathf.Clamp01((float)(Data.totalAdsClaimed - 1) / (float)(totalAbleAdsClaim - 1));

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
            if (Data.totalFreeClaimed < totalAbleFreeClaim && GetFreeCooldownSeconds() <= 0) return true;
            if (Data.totalAdsClaimed < totalAbleAdsClaim) return true;
            return false;
        }

        // EN: Seconds left before the next free reward unlocks by the TimeManager clock; 0 or less when there is no cooldown.
        // VI: Số giây còn lại trước khi mở thưởng miễn phí kế tiếp theo đồng hồ TimeManager; 0 hoặc âm khi không phải chờ.
        private double GetFreeCooldownSeconds()
        {
            if (string.IsNullOrEmpty(Data.nextFreeClaimTime)) return 0;
            return (TimeUtil.StringToDateTime(Data.nextFreeClaimTime) - Services.Time.GetNow()).TotalSeconds;
        }
    }
}
