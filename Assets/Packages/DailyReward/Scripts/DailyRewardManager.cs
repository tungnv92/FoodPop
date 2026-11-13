using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
namespace GamePackages
{
    [System.Serializable]
    public class DailyRewardManager : ContextData<DailyRewardData>, IFeatureNewDay
    {
        // EN: Reads the free reward claim count without exposing the model.
        // VI: Đọc số lần nhận thưởng miễn phí mà không lộ model.
        public int TotalFreeClaimed => Data != null ? Data.totalFreeClaimed : 0;
        public DailyRewardGlobalConfig DailyRewardGlobalConfig;
        public UnityAction onClaimedAction;
        // Start is called before the first frame update
        public static int totalAbleFreeClaim = 3;
        public static int totalAbleAdsClaim = 4;
        AwaiterActionManager AwaiterActionManager => AwaiterActionManager.Instance;

        public void OnNewDay()
        {
            Data.Reset();
            ClearAwaiterActionManager();
        }
        async void ClearAwaiterActionManager()
        {
            while (AwaiterActionManager == null)
            {
                await Task.Delay(200);
            }
            while (!AwaiterActionManager.IsInit())
            {
                await Task.Delay(200);
            }
            AwaiterActionManager.RemoveKey(AwaiterActionKey.AdsFreeDailyReward);
        }
        public bool IsClaimedStage(int stage, out string blockTime)
        {
            blockTime = "";
            if (stage == 0)
            {
                if (Data.totalFreeClaimed >= totalAbleFreeClaim)
                {
                    return true;
                }
                AwaiterAction action = AwaiterActionManager.LoadAwaiterAction(AwaiterActionKey.AdsFreeDailyReward);
                if (action != null)
                {
                    blockTime = action.TimeToString();
                    return true;
                }
                return false;
            }

            return Data.totalAdsClaimed >= stage;
        }
        public bool IsEnableClaimAds(int stage)
        {
            return Data.totalAdsClaimed == stage - 1;
        }

        public void OnClaimStage(int stage)
        {
            switch (stage)
            {
                case 0:
                    Data.totalFreeClaimed++;
                    if (Data.totalFreeClaimed < totalAbleFreeClaim && Data.totalFreeClaimed < DailyRewardGlobalConfig.free_rewards.Count)
                    {

                        int cappingTimeFree = DailyRewardGlobalConfig.free_rewards[Data.totalFreeClaimed].cappingTimeFree;
                        AwaiterActionManager.RegistryAwaiterAction(AwaiterActionKey.AdsFreeDailyReward, cappingTimeFree, null);
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
            ProfileManager.instance.SaveData();
        }
        public DailyRewardConfig GetConfig(int stage)
        {
            if (stage == 0) return DailyRewardGlobalConfig.GetFreeReward(Data.totalFreeClaimed);
            return DailyRewardGlobalConfig.ads_rewards.Find(x => x.stageID == stage);
        }
        public float GetProcessAdsClaim()
        {
            return Mathf.Clamp01((float)(Data.totalAdsClaimed - 1) / (float)(totalAbleAdsClaim - 1));

        }
        public int GetTotalAdsClaimed()
        {
            return Data.totalAdsClaimed;
        }


        public bool IsGoodToClaim()
        {
            if (AwaiterActionManager == null) return false;
            if (Data.totalFreeClaimed < totalAbleFreeClaim)
            {
                AwaiterAction action = AwaiterActionManager.LoadAwaiterAction(AwaiterActionKey.AdsFreeDailyReward);

                if (action == null)
                {
                    return true;
                }
            }
            if (Data.totalAdsClaimed < totalAbleAdsClaim) return true;
            return false;
        }

        public bool CheckSuggest()
        {
            if (Data.isSuggested) return false;

            Data.isSuggested = true;
            UIManager.Ins.OpenPopup(PopupId.DailyReward);
            return true;
        }
    }
    [System.Serializable]
    public class DailyRewardData
    {
        public int totalAdsClaimed;
        public int totalFreeClaimed;
        public bool isSuggested;
        public DailyRewardData()
        {
            Reset();
        }
        public void Reset()
        {
            totalAdsClaimed = 0;
            totalFreeClaimed = 0;
            isSuggested = false;
        }
    }
}