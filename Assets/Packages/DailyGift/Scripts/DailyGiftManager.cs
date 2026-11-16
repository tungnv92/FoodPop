using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.STP;
namespace GamePackages
{
    [System.Serializable]
    public class DailyGiftManager : ContextData<DailyGiftData>, IFeatureNewDay
    {
        public const int MAX_DAY = 7;
        public DailyGiftRewardConfig DailyGiftGlobalConfig;
        // EN: Binds the saved model through internal initialization and initializes this feature's state.
        // VI: Gắn model lưu qua khởi tạo nội bộ và khởi tạo trạng thái của tính năng này.
        internal override void SetContext(DailyGiftData data)
        {
            base.SetContext(data);
        }

        public void OnNewDay()
        {
            if (Data.day >= MAX_DAY && IsClaimed(MAX_DAY))
            {
                Data.Reset();
                Debug.Log("Daily Gift Reset");
            }
            OnValidateDay();
            if (!IsClaimed(Data.day)) return;
            Data.day++;
            OnValidateDay();
            Data.isEnableClaim = true;
            Data.isSuggested = false;
        }
        // EN: Coordinates IsEnabledFeature using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối IsEnabledFeature qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public bool IsEnabledFeature()
        {
            return ProfileManager.instance.PlayerDataManager.GetCurrentLevel() >= DailyGiftGlobalConfig.LevelActiveFeature;
        }
        public bool IsGoodToClaim()
        {
            return Data.isEnableClaim;
        }

        public bool CheckSuggest()
        {
            if (Data.isSuggested) return false;

            Data.isSuggested = true;
            UIManager.Ins.OpenPopup(PopupId.DailyLogin);
            ProfileManager.instance.SaveData();
            return true;
        }


        public bool IsClaimed(int day)
        {
            if (day > Data.l_ClaimedChecker.Count)
                return true;
            return Data.l_ClaimedChecker[day - 1];
        }
        public void OnClaimComplete()
        {
            OnValidateDay();
            int day = Data.day;
            Data.isEnableClaim = false;
            Data.l_ClaimedChecker[day - 1] = true;
            ProfileManager.instance.SaveData();
        }

        public int GetCurrentDayId()
        {
            OnValidateDay();
            return Data.day;
        }
        void OnValidateDay()
        {
            Data.day = Mathf.Clamp(Data.day, 1, MAX_DAY);
        }
    }

    [System.Serializable]
    public class DailyGiftData
    {
        [field: SerializeField] public int day { get; set; }
        [field: SerializeField] public bool isEnableClaim { get; set; }
        [field: SerializeField] public bool isSuggested { get; set; }
        public List<bool> l_ClaimedChecker = new();
        public DailyGiftData()
        {
            Reset();
        }
        public void Reset()
        {
            day = 1;
            isEnableClaim = true;
            isSuggested = false;
            l_ClaimedChecker.Clear();
            for (int i = 0; i < DailyGiftManager.MAX_DAY; i++)
            {
                l_ClaimedChecker.Add(false);
            }
        }

    }
}