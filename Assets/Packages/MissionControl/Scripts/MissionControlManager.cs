using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using SDK;

namespace GamePackages
{
    [Serializable]
    public class MissionControlManager : ContextData<MissionControlData>, IFeatureNewDay, IFeatureUpdate
    {
        // EN: Signals persisted UI-owned feature state changes without exposing the model.
        // VI: Báo thay đổi trạng thái tính năng do giao diện yêu cầu sau khi lưu mà không lộ model.
        public event Action StateChanged;

        // EN: Reads isDoneTutorial without exposing mutable feature data.
        // VI: Đọc isDoneTutorial mà không lộ dữ liệu tính năng có thể sửa.
        public bool HasCompletedTutorial => Data != null ? Data.isDoneTutorial : false;

        // EN: Persists isDoneTutorial when first marked by the feature UI.
        // VI: Lưu isDoneTutorial khi giao diện tính năng đánh dấu lần đầu.
        public void CompleteTutorial()
        {
            if (Data == null || Data.isDoneTutorial) return;
            Data.isDoneTutorial = true;
            Save();
            StateChanged?.Invoke();
        }
        // EN: Reads isShowedInfo without exposing mutable feature data.
        // VI: Đọc isShowedInfo mà không lộ dữ liệu tính năng có thể sửa.
        public bool HasShownInfo => Data != null ? Data.isShowedInfo : false;

        // EN: Persists isShowedInfo when first marked by the feature UI.
        // VI: Lưu isShowedInfo khi giao diện tính năng đánh dấu lần đầu.
        public void MarkInfoShown()
        {
            if (Data == null || Data.isShowedInfo) return;
            Data.isShowedInfo = true;
            Save();
            StateChanged?.Invoke();
        }
        // EN: Reads isClaimedGrandChest without exposing mutable feature data.
        // VI: Đọc isClaimedGrandChest mà không lộ dữ liệu tính năng có thể sửa.
        public bool HasClaimedGrandChest => Data != null ? Data.isClaimedGrandChest : false;

        // EN: Coordinates Instance using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối Instance qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public static MissionControlManager Instance => ProfileManager.instance.GameFeatureContext != null
            ? ProfileManager.instance.GameFeatureContext.MissionControlManager
            : null;

        public static MissionControlManager Ins => Instance;

        public MissionControlConfig MissionControlConfig;
        // EN: Coordinates config using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối config qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public MissionControlConfig config
        {
            get
            {
                if (MissionControlConfig == null)
                {
                    if (ProfileManager.instance.MissionControlConfig != null)
                    {
                        MissionControlConfig = ProfileManager.instance.MissionControlConfig;
                    }
                    else
                    {
                        MissionControlConfig = Resources.Load<MissionControlConfig>("MissionControlConfig");
                    }
                }
                return MissionControlConfig;
            }
            set => MissionControlConfig = value;
        }


        public bool isCheatBetweenFridayAndMonday;

        public int UnlockLevel
        {
            get
            {
                return config != null ? config.unlockLevelIndex : 36;
            }
        }

        public int unlockLevelIndex => UnlockLevel;

        // EN: Checks the unlock level through read-only player manager queries.
        // VI: Kiểm tra level mở khóa qua truy vấn chỉ đọc của manager người chơi.
        // EN: Checks the unlock level through read-only player manager queries.
        // VI: Kiểm tra level mở khóa qua truy vấn chỉ đọc của manager người chơi.
        public bool IsLevelUnlocked => ProfileManager.instance.PlayerDataManager.IsInitialized &&
                                       ProfileManager.instance.PlayerDataManager.GetCurrentLevel() >= UnlockLevel;

        public DayOfWeek StartDay => config != null ? config.startDay : DayOfWeek.Friday;
        public DayOfWeek EndDay => config != null ? config.endDay : DayOfWeek.Monday;

        /// <summary>Number of days from StartDay to EndDay (0 = single-day event).</summary>
        private int EventDurationDays => ((int)EndDay - (int)StartDay + 7) % 7;

        private int GetDaysSinceStart(DayOfWeek day) => ((int)day - (int)StartDay + 7) % 7;

        public bool IsInEventDays()
        {
            if (isCheatBetweenFridayAndMonday) return true;
            return GetDaysSinceStart(GetCurrentTime().DayOfWeek) <= EventDurationDays;
        }

        public bool isActive => (config == null || config.isFeatureEnabled) && IsLevelUnlocked && IsInEventDays();

        // EN: Binds the saved model through internal initialization and initializes this feature's state.
        // VI: Gắn model lưu qua khởi tạo nội bộ và khởi tạo trạng thái của tính năng này.
        internal override void SetContext(MissionControlData data)
        {
            base.SetContext(data ?? throw new System.ArgumentNullException(nameof(data)));
            CheckExpire();
            CheckMismatchData();
        }

        public void OnNewDay()
        {
            CheckExpire();
        }

        public void OnUpdate()
        {
            CheckExpire();
        }

        // EN: Coordinates GetCurrentTime using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối GetCurrentTime qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public DateTime GetCurrentTime()
        {
            if (ProfileManager.instance.TimeManager != null)
            {
                return ProfileManager.instance.TimeManager.GetNow();
            }
            return DateTime.Now;
        }

        public void CheckExpire()
        {
            if (Data == null) return;
            if (!IsInEventDays()) return;

            DateTime now = GetCurrentTime();
            string currentCycleId = GetCurrentCycleId(now);

            if (string.IsNullOrEmpty(Data.currentCycleId) || Data.currentCycleId != currentCycleId)
            {
                StartNewEvent(now, currentCycleId);
                return;
            }

            if (DateTime.TryParse(Data.expire, out DateTime expireTime))
            {
                if (now > expireTime)
                {
                    StartNewEvent(now, currentCycleId);
                }
            }
            else
            {
                StartNewEvent(now, currentCycleId);
            }
        }

        public string GetCurrentCycleId(DateTime now)
        {
            int daysSinceStart = GetDaysSinceStart(now.DayOfWeek);
            if (daysSinceStart > EventDurationDays) daysSinceStart = 0;
            return now.Date.AddDays(-daysSinceStart).ToString("yyyy-MM-dd");
        }

        private int GetDaysUntilEndDay(DayOfWeek currentDay)
        {
            int daysSinceStart = GetDaysSinceStart(currentDay);
            if (daysSinceStart > EventDurationDays) return EventDurationDays;
            return EventDurationDays - daysSinceStart;
        }

        public void StartNewEvent()
        {
            DateTime now = GetCurrentTime();
            StartNewEvent(now, GetCurrentCycleId(now));
        }

        public void StartNewEvent(DateTime now, string cycleId)
        {
            if (Data == null) return;
            int daysUntilEnd = GetDaysUntilEndDay(now.DayOfWeek);
            DateTime targetEndDay = now.Date.AddDays(daysUntilEnd);
            DateTime expire = targetEndDay.AddHours(23).AddMinutes(59).AddSeconds(59);

            Data.ClearDataNewEvent(cycleId, expire.ToString("o"));
            EnsureQuestCountsInitialized(Data);
            Save();
        }

        public void CheckMismatchData()
        {
            if (Data == null || config == null || config.stageInfos == null) return;
            bool isDataChanged = false;

            if (Data.questClaimeds != null)
            {
                int beforeCount = Data.questClaimeds.Count;
                Data.questClaimeds.RemoveAll(claimedQuest => !IsQuestExistInConfig(claimedQuest));
                if (Data.questClaimeds.Count != beforeCount)
                {
                    isDataChanged = true;
                }
            }

            if (EnsureQuestCountsInitialized(Data))
            {
                isDataChanged = true;
            }

            if (isDataChanged)
            {
                Save();
            }

            bool IsQuestExistInConfig(MissionControlDataQuestClaimed claimedQuest)
            {
                foreach (var stageInfo in config.stageInfos)
                {
                    foreach (var questInfo in stageInfo.questInfos)
                    {
                        if (questInfo.stageId == claimedQuest.stageId &&
                            questInfo.questType == claimedQuest.questType &&
                            questInfo.amount == claimedQuest.amount)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public bool EnsureQuestCountsInitialized(MissionControlData data)
        {
            if (data == null || config == null || config.stageInfos == null) return false;
            bool isAdded = false;

            foreach (var stageInfo in config.stageInfos)
            {
                foreach (var questInfo in stageInfo.questInfos)
                {
                    if (!data.questCounts.Any(x => x.questType == questInfo.questType))
                    {
                        data.questCounts.Add(new MissionControlDataQuestAmount(questInfo.questType, 0));
                        isAdded = true;
                    }
                }
            }

            return isAdded;
        }

        public string GetCountDownString()
        {
            if (!isActive || Data == null) return "---";

            if (DateTime.TryParse(Data.expire, out DateTime expire))
            {
                TimeSpan timeRemaining = expire - GetCurrentTime();
                if (timeRemaining.TotalSeconds <= 0) return "00:00";

                if (timeRemaining.Days > 0)
                {
                    return $"{timeRemaining.Days}d {timeRemaining.Hours:D2}h";
                }
                else if (timeRemaining.Hours > 0)
                {
                    return $"{timeRemaining.Hours}h {timeRemaining.Minutes:D2}m";
                }
                else
                {
                    return $"{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
                }
            }
            return "---";
        }

        public int GetStageToChoose()
        {
            if (config == null || config.stageInfos == null || config.stageInfos.Count == 0) return 0;

            // 1. Choose first stage that has claimable quests
            foreach (var stageInfo in config.stageInfos)
            {
                int id = stageInfo.id;
                if (IsNoti(id)) return id;
            }

            // 2. Choose first stage that is not completely claimed
            foreach (var stageInfo in config.stageInfos)
            {
                int id = stageInfo.id;
                foreach (var questInfo in stageInfo.questInfos)
                {
                    if (!GetIsClaimed(questInfo)) return id;
                }
            }

            // 3. Fallback to last stage if all claimed
            if (IsClaimedAllSmallChests())
            {
                return config.stageInfos.Count - 1;
            }
            return 0;
        }

        public string GetQuestTitle(MissionControlQuestInfo missionControlQuestInfo)
        {
            if (missionControlQuestInfo == null) return "---";
            switch (missionControlQuestInfo.questType)
            {
                case MissionControlQuestType.CompleteStepInWinstreak:
                    return missionControlQuestInfo.amount > 1
                        ? $"Complete {missionControlQuestInfo.amount} steps in Winstreak"
                        : "Complete 1 step in Winstreak";
                case MissionControlQuestType.EarnCoin:
                    return $"Earn {missionControlQuestInfo.amount} coins";
                case MissionControlQuestType.WinLevelsOnFirstTry:
                    return missionControlQuestInfo.amount > 1
                        ? $"Win {missionControlQuestInfo.amount} levels on first try"
                        : "Win 1 level on first try";
                case MissionControlQuestType.WinHardLevels:
                    return missionControlQuestInfo.amount > 1
                        ? $"Win {missionControlQuestInfo.amount} Hard Levels"
                        : "Win 1 Hard Level";
            }
            return "---";
        }

        public Sprite GetQuestIcon(MissionControlQuestType questType)
        {
            if (config == null || config.questIcons == null) return null;
            var item = config.questIcons.FirstOrDefault(x => x.questType == questType);
            return item != null ? item.icon : null;
        }

        [Button]
        public void UpdateQuest(MissionControlQuestType questType, int amount)
        {
            if (!isActive || Data == null) return;

            var questCount = Data.questCounts.FirstOrDefault(x => x.questType == questType);
            if (questCount == null)
            {
                questCount = new MissionControlDataQuestAmount(questType, 0);
                Data.questCounts.Add(questCount);
            }
            questCount.count += amount;
            Save();

            if (MissionControl.Instance != null && MissionControl.Instance.gameObject.activeInHierarchy)
            {
                MissionControl.Instance.Refresh();
            }
        }

        public int GetDataCount(MissionControlQuestInfo missionControlQuestInfo)
        {
            if (Data == null || missionControlQuestInfo == null) return 0;
            var questCount = Data.questCounts.FirstOrDefault(x => x.questType == missionControlQuestInfo.questType);
            if (questCount == null)
            {
                questCount = new MissionControlDataQuestAmount(missionControlQuestInfo.questType, 0);
                Data.questCounts.Add(questCount);
                Save();
            }
            return questCount.count;
        }

        public bool GetIsClaimed(MissionControlQuestInfo missionControlQuestInfo)
        {
            if (Data == null || Data.questClaimeds == null || missionControlQuestInfo == null) return false;
            return Data.questClaimeds.Any(x => x.stageId == missionControlQuestInfo.stageId &&
                                               x.questType == missionControlQuestInfo.questType &&
                                               x.amount == missionControlQuestInfo.amount);
        }

        public float GetFillAmount(MissionControlQuestInfo missionControlQuestInfo)
        {
            if (missionControlQuestInfo == null || missionControlQuestInfo.amount <= 0) return 0f;
            return Mathf.Clamp01((float)GetDataCount(missionControlQuestInfo) / missionControlQuestInfo.amount);
        }

        public string GetFillAmountText(MissionControlQuestInfo missionControlQuestInfo)
        {
            if (missionControlQuestInfo == null) return "0/0";
            int count = GetDataCount(missionControlQuestInfo);
            int displayCount = Mathf.Min(count, missionControlQuestInfo.amount);
            return $"{displayCount}/{missionControlQuestInfo.amount}";
        }

        public bool IsNoti(int stageId)
        {
            if (config == null || config.stageInfos == null || stageId < 0 || stageId >= config.stageInfos.Count) return false;
            foreach (var questInfo in config.stageInfos[stageId].questInfos)
            {
                if (IsCanClaim(questInfo)) return true;
            }
            if (IsCanClaimSmallChest(stageId) && !IsStageClaimed(stageId)) return true;
            return false;
        }

        public bool IsNoti()
        {
            if (!isActive || Data == null || config == null) return false;
            if (IsClaimedAllSmallChests() && !Data.isClaimedGrandChest) return true;

            foreach (var stageInfo in config.stageInfos)
            {
                if (IsNoti(stageInfo.id)) return true;
            }
            return false;
        }

        #region CLAIM
        // EN: Coordinates ClaimQuest using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối ClaimQuest qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        // EN: Coordinates ClaimQuest using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối ClaimQuest qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public void ClaimQuest(MissionControlQuestInfo missionControlQuestInfo)
        {
            if (Data == null || missionControlQuestInfo == null) return;
            if (!GetIsClaimed(missionControlQuestInfo))
            {
                Data.questClaimeds.Add(new MissionControlDataQuestClaimed(
                    missionControlQuestInfo.stageId,
                    missionControlQuestInfo.questType,
                    missionControlQuestInfo.amount));
                Save();

                int currentLevel = ProfileManager.instance.PlayerDataManager.IsInitialized
                    ? ProfileManager.instance.PlayerDataManager.GetCurrentLevel()
                    : 1;

                //if (ABIAnalyticsManager.Instance != null)
                //{
                //    ABIAnalyticsManager.Instance.ClaimMissionControlQuest(
                //        currentLevel,
                //        missionControlQuestInfo.stageId,
                //        missionControlQuestInfo.questType.ToString(),
                //        missionControlQuestInfo.amount);
                //}
            }

            if (MissionControl.Instance != null && MissionControl.Instance.gameObject.activeInHierarchy)
            {
                MissionControl.Instance.Refresh();
            }
        }

        public bool IsCanClaim(MissionControlQuestInfo missionControlQuestInfo)
        {
            if (missionControlQuestInfo == null) return false;
            if (GetIsClaimed(missionControlQuestInfo)) return false;
            return GetDataCount(missionControlQuestInfo) >= missionControlQuestInfo.amount;
        }

        public bool IsCanClaimSmallChest(int stageId)
        {
            if (Data == null || config == null) return false;
            var stage = config.stageInfos.FirstOrDefault(x => x.id == stageId);
            if (stage == null) return false;
            return Data.questClaimeds.Count(x => x.stageId == stageId) >= stage.questInfos.Count;
        }

        public bool IsStageClaimed(int stageId)
        {
            return Data != null && Data.claimedStageIds.Contains(stageId);
        }

        // EN: Coordinates ClaimStageRewards using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối ClaimStageRewards qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        // EN: Coordinates ClaimStageRewards using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối ClaimStageRewards qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public void ClaimStageRewards(int stageId)
        {
            if (Data == null || IsStageClaimed(stageId)) return;
            var stageInfo = config != null ? config.stageInfos.FirstOrDefault(x => x.id == stageId) : null;
            if (stageInfo == null) return;

            Data.claimedStageIds.Add(stageId);
            Save();

            foreach (var reward in stageInfo.rewardInfos)
            {
                ClaimReward(reward, $"MissionControl_Stage_{stageId + 1}");
            }

            int currentLevel = ProfileManager.instance.PlayerDataManager.IsInitialized
                ? ProfileManager.instance.PlayerDataManager.GetCurrentLevel()
                : 1;

            //if (ABIAnalyticsManager.Instance != null)
            //{
            //    ABIAnalyticsManager.Instance.ClaimMissionControlSmallChest(currentLevel, stageId);
            //}
        }

        public bool IsClaimedAllSmallChests()
        {
            if (Data == null || config == null || config.stageInfos == null) return false;
            int allQuestCount = 0;
            foreach (var stageInfo in config.stageInfos)
            {
                allQuestCount += stageInfo.questInfos.Count;
            }
            return Data.questClaimeds.Count >= allQuestCount;
        }

        // EN: Coordinates ClaimGrandPrize using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối ClaimGrandPrize qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        // EN: Coordinates ClaimGrandPrize using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối ClaimGrandPrize qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public void ClaimGrandPrize()
        {
            if (Data == null || Data.isClaimedGrandChest) return;
            Data.isClaimedGrandChest = true;
            Save();

            if (config != null && config.grandChestRewardInfos != null)
            {
                foreach (var reward in config.grandChestRewardInfos)
                {
                    ClaimReward(reward, "MissionControl_GrandPrize");
                }
            }

            int currentLevel = ProfileManager.instance.PlayerDataManager.IsInitialized
                ? ProfileManager.instance.PlayerDataManager.GetCurrentLevel()
                : 1;

            //if (ABIAnalyticsManager.Instance != null)
            //{
            //    ABIAnalyticsManager.Instance.ClaimMissionControlGrandChest(currentLevel);
            //}
        }

        // EN: Grants supported rewards through dedicated APIs and warns about unknown resource types.
        // VI: Cấp thưởng hợp lệ qua API chuyên biệt và cảnh báo loại tài nguyên không xác định.
        // EN: Coordinates ClaimReward using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối ClaimReward qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public void ClaimReward(ResourceGame reward, string placement = "MissionControl")
        {
            if (reward == null) return;
            var pdm = ProfileManager.instance.PlayerDataManager;

            switch (reward.type)
            {
                case ResourceType.Coin:
                    pdm.OnAddCoin(reward.value, placement);
                    break;
                case ResourceType.UnlimitedLives:
                    pdm.AddUnlimitedLiveTime(reward.value, placement);
                    break;
                case ResourceType.BoosterClean:
                case ResourceType.BoosterShuffle:
                case ResourceType.BoosterPickup:
                case ResourceType.BoosterAutoComplete:
                    pdm.OnAddBooster(reward.type, reward.value, placement);
                    break;
                default:
                    Debug.LogWarning($"Unsupported reward resource: {reward.type}");
                    break;
            }
        }

        public static int MapRewardToAnimatorType(ResourceType type)
        {
            return type switch
            {
                ResourceType.Coin => 0,             // Coin
                ResourceType.UnlimitedLives => 1,   // UnlimitedLife
                ResourceType.BoosterClean => 2,   // Booster_1
                ResourceType.BoosterShuffle => 3,   // Booster_2
                ResourceType.BoosterPickup => 4,    // Booster_3
                ResourceType.BoosterAutoComplete => 5, // Booster_4
                _ => 0
            };
        }
        #endregion

        #region QUESTS

        public void OnLevelWin(bool isFirstTry, bool isHard)
        {
            if (!isActive) return;
            if (isFirstTry)
            {
                UpdateQuest(MissionControlQuestType.WinLevelsOnFirstTry, 1);
            }
            if (isHard)
            {
                UpdateQuest(MissionControlQuestType.WinHardLevels, 1);
            }
        }

        public void OnStepInWinstreak()
        {
            if (!isActive) return;
            UpdateQuest(MissionControlQuestType.CompleteStepInWinstreak, 1);
        }

        public void OnEarnCoin(int amount)
        {
            if (!isActive) return;
            UpdateQuest(MissionControlQuestType.EarnCoin, amount);
        }
        #endregion

        #region TEST - CHEAT

        [Button]
        public void ClearAllData()
        {
            if (Data != null)
            {
                Data.ClearAllData();
                Save();
            }
        }

        [Button]
        public void CheatDoneAllQuests()
        {
            UpdateQuest(MissionControlQuestType.CompleteStepInWinstreak, 9999);
            UpdateQuest(MissionControlQuestType.EarnCoin, 9999);
            UpdateQuest(MissionControlQuestType.WinLevelsOnFirstTry, 9999);
            UpdateQuest(MissionControlQuestType.WinHardLevels, 9999);
        }

        public void CheatUpdateQuest(int questNumber, int amount)
        {
            int questId = questNumber - 1;
            MissionControlQuestType questType = (MissionControlQuestType)questId;
            if (!Data.questCounts.Any(x => x.questType == questType))
            {
                Data.questCounts.Add(new MissionControlDataQuestAmount(questType, 0));
            }
            Data.questCounts.FirstOrDefault(x => x.questType == questType).count = amount;
            Save();

            if (MissionControl.Instance != null && MissionControl.Instance.gameObject.activeInHierarchy)
            {
                MissionControl.Instance.Refresh();
            }
        }
        #endregion
    }
}
