using Sirenix.OdinInspector;
using System;
using System.Threading.Tasks;
using UnityEngine;
namespace GamePackages
{
    [System.Serializable]
    public class HeartManager : ContextData<HeartData>, IFeatureUpdate
    {
        public const int default_Max_Heart = 5;
        public int masterPass_Max_Heart = 8;
        public int timeRefillOnHeart = 30;
        public int refillGoldCost = 900;
        bool isInit;
        // EN: Initializes hearts in place so the manager and root profile retain the same saved object.
        // VI: Khởi tạo mạng tại chỗ để manager và hồ sơ gốc giữ cùng đối tượng được lưu.
        internal override void SetContext(HeartData data)
        {
            base.SetContext(data);
            if (!data.isInit)
            {
                // Keep the root profile and HeartManager bound to the same persisted object.
                data.isInit = true;
                data.currentHeartAmount = data.maxHeart = default_Max_Heart;
                data.timeToNewHeart = 0;
                data.unlimitedLivesExpireTime = 0;
                data.sLastTimeOnline = null;
            }
            isInit = true;
            Debug.Log("HeartManager");
            AsyncTime();
        }
       public void AsyncTime()
        {
            if (!isInit) return;
            double timeOff = 0;
            if (!string.IsNullOrEmpty(Data.sLastTimeOnline))
            {
                timeOff = (DateTime.Now - TimeUtil.StringToDateTime(Data.sLastTimeOnline)).TotalSeconds;
            }
            if (Data.unlimitedLivesExpireTime > 0)
            {
                Data.unlimitedLivesExpireTime -= (int)timeOff;
                if (Data.unlimitedLivesExpireTime < 0) Data.unlimitedLivesExpireTime = 0;
            }

            int heartAdd = (int)(timeOff / (timeRefillOnHeart * 60));
            if (heartAdd > 0)
            {
                AddHeart(heartAdd);
                timeOff -= heartAdd * timeRefillOnHeart * 60;
                if (Data.currentHeartAmount >= Data.maxHeart)
                {
                    Data.timeToNewHeart = 0;
                }
            }
            if (Data.timeToNewHeart > 0)
            {
                Data.timeToNewHeart -= (int)timeOff;
                if (Data.timeToNewHeart <= 0)
                {
                    AddHeart();
                    UpdateTimeAfterFillHeart();
                }
            }
            // check miss Data
            if (Data.currentHeartAmount < Data.maxHeart && Data.timeToNewHeart <= 0) Data.timeToNewHeart = timeRefillOnHeart * 60;
        }
        public void OnUpdate()
        {
            if (Data.unlimitedLivesExpireTime > 0)
            {
                Data.unlimitedLivesExpireTime -= Time.deltaTime;
                if (Data.unlimitedLivesExpireTime < 0) Data.unlimitedLivesExpireTime = 0;
            }
            if (Data.timeToNewHeart > 0)
            {
                Data.timeToNewHeart -= Time.deltaTime;
                if (Data.timeToNewHeart <= 0)
                {
                    AddHeart();
                    UpdateTimeAfterFillHeart();
                }
            }
            Data.sLastTimeOnline = TimeUtil.DateTimeToString(DateTime.Now);
        }
        public bool IsEnoughHeartToPlay()
        {
            if (IsUnlimitedLives()) return true;
            return Data.currentHeartAmount > 0;
        }
        public void AddHeart(int amount = 1, bool isLimitMax = true)
        {
            Data.currentHeartAmount += amount;
            if (isLimitMax)
            {
                Data.currentHeartAmount = Mathf.Clamp(Data.currentHeartAmount, 0, Data.maxHeart);
            }
        }
        [Button]
        public void OnFillFullHeart()
        {
            Data.currentHeartAmount = Data.maxHeart;
            Data.timeToNewHeart = 0;
        }
        private void UpdateTimeAfterFillHeart()
        {
            if (Data.currentHeartAmount >= Data.maxHeart) Data.timeToNewHeart = 0;
            else Data.timeToNewHeart = timeRefillOnHeart * 60;
        }
        public void UseHeart()
        {
            if (IsUnlimitedLives()) return;
            Data.currentHeartAmount--;
            if (Data.currentHeartAmount < 0) Data.currentHeartAmount = 0;
            if (Data.timeToNewHeart <= 0.5f && Data.currentHeartAmount < Data.maxHeart) Data.timeToNewHeart = timeRefillOnHeart * 60;

        }
        [Button(ButtonStyle.FoldoutButton)]
        public void AddUnlimitedLiveTime(int minute)
        {
            Data.unlimitedLivesExpireTime += minute * 60;
        }
        public bool IsUnlimitedLives()
        {
            return Data.unlimitedLivesExpireTime > 0;
        }
        public bool IsMaxHeart()
        {
            return Data.currentHeartAmount >= Data.maxHeart;
        }
        public void OnActiveMasterPass()
        {
            Data.maxHeart = masterPass_Max_Heart;
            UpdateTimeAfterFillHeart();
        }
        public void OnEndMasterPass()
        {
            Data.maxHeart = default_Max_Heart;
        }
        public (string, string) GetHeartDisplay()
        {
            if (IsUnlimitedLives())
            {
                return ("", GetTimeStringNextHeart());
            }
            if (Data.currentHeartAmount == Data.maxHeart) return (Data.maxHeart + "", "FULL");
            else
            {
                return ("" + Data.currentHeartAmount, GetTimeStringNextHeart());
            }
        }
        string GetTimeStringNextHeart()
        {
            if (Data.unlimitedLivesExpireTime > 0) return TimeUtil.TimeToString((int)Data.unlimitedLivesExpireTime);
            else if (Data.timeToNewHeart > 0) return TimeUtil.TimeToString((int)Data.timeToNewHeart);
            return "";
        }
        public int GetRemainHeart()
        {
            if (Data.unlimitedLivesExpireTime > 0) return -1;
            return Data.currentHeartAmount;
        }

    }
    [System.Serializable]
    public class HeartData
    {
        public bool isInit;
        public int currentHeartAmount;
        public int maxHeart;
        public float timeToNewHeart;
        public float unlimitedLivesExpireTime;
        public string sLastTimeOnline;
        public HeartData()
        {
            isInit = true;
            currentHeartAmount = maxHeart = HeartManager.default_Max_Heart;
        }
    }
}