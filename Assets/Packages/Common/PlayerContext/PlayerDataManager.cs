using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace GamePackages
{
    public class PlayerDataManager : ContextData<PlayerData>
    {
        Dictionary<ResourceType, UnityAction<int>> dic_OnResourceChange = new Dictionary<ResourceType, UnityAction<int>>();
        RecordTrackingManager RecordManager => ProfileManager.instance.GameFeatureContext.RecordManager;
        public int GetResourceValue(ResourceType type)
        {
            var target = Data.l_ResourceGame.Find(x => x.type == type);
            if (target == null)
            {
                return 0;
            }
            return target.value;
        }
        public void OnAddResource(ResourceType _type, int _amount, string placement = "")
        {
            var target = Data.l_ResourceGame.Find(x => x.type == _type);
            if (target == null)
            {
                target = new ResourceGame() { type = _type, value = 0 };
                Data.l_ResourceGame.Add(target);
            }
            int value = target.value;
            value += _amount;
            if (value <= 0) value = 0;
            Data.l_ResourceGame.Find(x => x.type == _type).value = value;
            if (dic_OnResourceChange.ContainsKey(_type))
                dic_OnResourceChange[_type]?.Invoke(value);
            Save();
        }
        public void OnRemoveResource(ResourceType _type, int _amount, string placement = "")
        {
            var target = Data.l_ResourceGame.Find(x => x.type == _type);
            if (target == null)
            {
                target = new ResourceGame() { type = _type, value = 0 };
                Data.l_ResourceGame.Add(target);
            }
            int value = target.value;
            value -= _amount;
            if (value <= 0) value = 0;
            Data.l_ResourceGame.Find(x => x.type == _type).value = value;
            if (dic_OnResourceChange.ContainsKey(_type))
                dic_OnResourceChange[_type]?.Invoke(value);
            Save();

        }
        public bool IsEnoughResource(ResourceType type, int amout)
        {
            int value = GetResourceValue(type);
            return value >= amout;
        }
        public void RegisterResourceChange(ResourceType type, UnityAction<int> action)
        {
            if (dic_OnResourceChange.ContainsKey(type))
            {
                dic_OnResourceChange[type] += action;
            } else dic_OnResourceChange.Add(type, action);
        }
        public void RemoveResourceChange(ResourceType type, UnityAction<int> action)
        {
            if (dic_OnResourceChange.ContainsKey(type))
            {
                dic_OnResourceChange[type] -= action;
            }
        }

        // EN: Adds coins to player data, updates tracking records, and notifies listeners and MissionControl.
        // VI: Cộng xu vào dữ liệu người chơi, cập nhật bản ghi theo dõi và thông báo cho các listener cùng MissionControl.
        public void OnAddCoin(int _amount, string placement = "")
        {
            OnAddResource(ResourceType.Coin, _amount);
            RecordManager.RecordCoinEarn(_amount);
            //if (!Application.isEditor) SDKBridge.TrackEventEarnCurrency(GetCurrentLevel(), _amount, ResourceType.Coin.ToString(), placement);
            Observer.OnPurchaseCoinSuccessAction?.Invoke();
            MissionControlManager.Ins.OnEarnCoin(_amount);
        }

        // EN: Deducts coins from player data and records the expenditure.
        // VI: Trừ xu khỏi dữ liệu người chơi và ghi nhận khoản chi tiêu.
        public void OnRemoveCoin(int _amount, string placement = "")
        {
            OnRemoveResource(ResourceType.Coin, _amount);
            RecordManager.RecordCoinSpend(_amount);
            //if (!Application.isEditor) SDKBridge.TrackEventSpendCurrency(ProfileManager.instance.PlayerData.currentLevel, _amount, ResourceType.Coin.ToString(), placement);
        }

        // EN: Adds unlimited life duration through HeartManager.
        // VI: Cộng thêm thời gian mạng vô hạn thông qua HeartManager.
        public void AddUnlimitedLiveTime(int value, string placement = "")
        {
            ProfileManager.instance.GameFeatureContext.HeartManager.AddUnlimitedLiveTime(value);
            //SDKBridge.TrackEventEarnCurrency(GetCurrentLevel(), value, ResourceType.UnlimitedLives.ToString(), placement);
        }

        // EN: Adds boosters of the specified type to player data and records the gain.
        // VI: Cộng booster theo loại chỉ định vào dữ liệu người chơi và ghi nhận số lượng nhận được.
        public void OnAddBooster(ResourceType type, int _amount, string placement = "")
        {
            OnAddResource(type, _amount);
            RecordManager.RecordBoosterEarn(_amount);
            //SDKBridge.TrackEventEarnBooster(type.ToString(), _amount, placement, GetCurrentLevel());
        }

        // EN: Marks remove-ads as purchased and saves player data.
        // VI: Đánh dấu đã mua gói gỡ quảng cáo và lưu dữ liệu người chơi.
        public void OnBoughtRemoveAds()
        {
            Data.isRemoveAds = true;
            //SDKBridge.HideBanner();
            //Observer.OnBoughtRemoveAdsAction?.Invoke();
            Save();
        }
        public void OnBoughtStarterPack()
        {
            Data.isBoughtStarterPack = true;
            //Observer.OnBoughtStarterPackAction?.Invoke();
            Save();
        }
        public void OnBoughtRevivePack()
        {
            //Observer.OnBoughtRevivePackAction?.Invoke();
        }
        public void IncreaseLevel()
        {
            Data.currentLevel++;
            Save();
        }
        public void DecreaseLevel()
        {
            Data.currentLevel--;
            if (Data.currentLevel <= 1) Data.currentLevel = 1;
            Save();
        }
        public void ResetStatePlaying()
        {
            Data.isPlaying = false;
            Save();
        }
        public void SetPlaying()
        {
            Data.isPlaying = true;
            Save();
        }
        public bool IsLastPlayingOnQuit()
        {
            return Data.isPlaying;
        }
        public int GetRemainBooster()
        {
            return GetResourceValue(ResourceType.BoosterPickup) + GetResourceValue(ResourceType.BoosterShuffle) + GetResourceValue(ResourceType.BoosterAddSlot) + GetResourceValue(ResourceType.BoosterSuperBear);
        }

        public int GetCurrentLevel()
        {
            return Data.currentLevel;
        }
    }
}
