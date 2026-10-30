using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GamePackages
{
    public enum WinStreakRound
    {
        None = 0,
        Round1 = 1, // Friday 00:00:00 to Monday 23:59:59
        Round2 = 2  // Tuesday 00:00:00 to Thursday 23:59:59
    }

    [System.Serializable]
    public class WinStreakManager : ContextData<WinStreakData>, IFeatureNewDay, IFeatureUpdate
    {
        public static WinStreakManager Instance => ProfileManager.instance != null && ProfileManager.instance.GameFeatureContext != null
            ? ProfileManager.instance.GameFeatureContext.WinStreakManager
            : null;

        [FormerlySerializedAs("TreasureGlobalConfig")]
        public WinStreakGlobalConfig WinStreakGlobalConfig;
        public WinStreakGlobalConfig TreasureGlobalConfig
        {
            get => WinStreakGlobalConfig;
            set => WinStreakGlobalConfig = value;
        }

        public static readonly int[] Multipliers = { 1, 5, 10, 25, 100 };

        public Action onDataChanged;
        public Action<WinStreakMileStoneInfo> onMilestoneClaimed;
        public Action onNewDay;
        public Action OnNewDayAction { get => onNewDay; set => onNewDay = value; }
        public bool WasRoundEndedOnNewDay { get; private set; }

        public bool isCheatBetweenDays;
        public int claimAmountCheat;

        public int UnlockLevel
        {
            get
            {
                if (ProfileManager.instance != null && ProfileManager.instance.RemoteConfigBase != null)
                {
                    return ProfileManager.instance.RemoteConfigBase.level_unlock_treasure;
                }
                return WinStreakGlobalConfig != null ? WinStreakGlobalConfig.LevelActiveFeature : 26;
            }
        }

        public bool IsLevelUnlocked => ProfileManager.instance != null && ProfileManager.instance.PlayerData != null &&
                                       ProfileManager.instance.PlayerData.currentLevel >= UnlockLevel;

        public WinStreakRound CurrentRound => GetCurrentRound(DateTime.Now);

        public bool IsEventActive
        {
            get
            {
                if (WinStreakGlobalConfig != null && !WinStreakGlobalConfig.isFeatureEnabled) return false;
                if (isCheatBetweenDays) return true;
                return GetCurrentRound(DateTime.Now) != WinStreakRound.None;
            }
        }

        public bool isActive => (WinStreakGlobalConfig == null || WinStreakGlobalConfig.isFeatureEnabled) && IsLevelUnlocked && IsEventActive;

        public int CurrentKeys => Data != null ? Data.keyAmount : 0;
        public int PendingKeys => Data != null ? Data.pendingKeyAmount : 0;
        public int WinStreakTier => Data != null ? Data.numberWinsteak : 0;
        public int WinStreak => Data != null ? Data.winstreak : 0;

        public bool IsCompleted
        {
            get
            {
                if (Data == null) return false;
                if (!Data.isCompleted && GetMilestoneCount() > 0 && IsClaimedAllMilestones())
                {
                    Data.isCompleted = true;
                }
                return Data.isCompleted;
            }
            set
            {
                if (Data != null && Data.isCompleted != value)
                {
                    Data.isCompleted = value;
                    Save();
                    onDataChanged?.Invoke();
                }
            }
        }

        public bool IsClaimedAllMilestones()
        {
            int count = GetMilestoneCount();
            if (count == 0) return false;
            return IsMilestoneClaimed(count - 1);
        }

        public void SetCompleted(bool completed)
        {
            IsCompleted = completed;
        }

        public override void SetContext(WinStreakData data)
        {
            if (data == null)
            {
                data = new WinStreakData();
                if (ProfileManager.instance != null && ProfileManager.instance.PlayerData != null)
                {
                    ProfileManager.instance.PlayerData.WinStreakData = data;
                }
            }
            base.SetContext(data);
            CheckAndInitExpire();
        }

        public bool IsEnabledFeature()
        {
            return isActive;
        }

        public static bool IsDayInRange(DayOfWeek current, DayOfWeek start, DayOfWeek end)
        {
            if (start <= end)
            {
                return current >= start && current <= end;
            }
            // Wraps around Sunday (e.g. Friday to Monday)
            return current >= start || current <= end;
        }

        public int GetMultiplier()
        {
            int tier = Data != null ? Mathf.Clamp(Data.numberWinsteak, 0, Multipliers.Length - 1) : 0;
            return Multipliers[tier];
        }

        public void OnLevelWin(int customKeys = -1)
        {
            if (!IsEnabledFeature()) return;
            if (IsCompleted) return;

            int keysEarned = customKeys > 0 ? customKeys : (claimAmountCheat > 0 ? claimAmountCheat : GetMultiplier());
            claimAmountCheat = 0;
            Data.pendingKeyAmount += keysEarned;

            if (Data.numberWinsteak < Multipliers.Length - 1)
            {
                Data.numberWinsteak++;
            }

            Data.winstreak++;
            Save();
            onDataChanged?.Invoke();
        }

        public int CommitPendingKeys()
        {
            if (Data == null) return 0;
            int pending = Data.pendingKeyAmount;
            int maxKeys = GetAccumulateAllMileStoneInfos();
            if (maxKeys > 0)
            {
                Data.keyAmount = Mathf.Min(Data.keyAmount + pending, maxKeys);
            }
            else
            {
                Data.keyAmount += pending;
            }
            Data.pendingKeyAmount = 0;
            Save();
            onDataChanged?.Invoke();
            return pending;
        }

        public void OnLevelLose()
        {
            if (!IsEnabledFeature()) return;
            Data.numberWinsteak = 0;
            Data.winstreak = 0;
            Save();
            onDataChanged?.Invoke();
        }

        public int GetMilestoneCount()
        {
            return (WinStreakGlobalConfig != null && WinStreakGlobalConfig.winStreakMileStoneInfos != null)
                ? WinStreakGlobalConfig.winStreakMileStoneInfos.Count
                : 0;
        }

        public WinStreakMileStoneInfo GetMilestone(int index)
        {
            if (WinStreakGlobalConfig == null) return null;
            return WinStreakGlobalConfig.GetMilestone(index);
        }

        public int GetMilestoneTargetAccumulate(int milestoneIndex)
        {
            if (WinStreakGlobalConfig == null || WinStreakGlobalConfig.winStreakMileStoneInfos == null) return 0;
            int accumulate = 0;
            for (int i = 0; i <= milestoneIndex && i < WinStreakGlobalConfig.winStreakMileStoneInfos.Count; i++)
            {
                accumulate += WinStreakGlobalConfig.winStreakMileStoneInfos[i].screwAmount;
            }
            return accumulate;
        }

        public int GetMilestonePrevAccumulate(int milestoneIndex)
        {
            if (milestoneIndex <= 0) return 0;
            return GetMilestoneTargetAccumulate(milestoneIndex - 1);
        }

        public int GetMilestoneIndexAtKeys(int totalKeys)
        {
            if (WinStreakGlobalConfig == null || WinStreakGlobalConfig.winStreakMileStoneInfos == null || WinStreakGlobalConfig.winStreakMileStoneInfos.Count == 0)
                return 0;

            int accumulate = 0;
            for (int i = 0; i < WinStreakGlobalConfig.winStreakMileStoneInfos.Count; i++)
            {
                accumulate += WinStreakGlobalConfig.winStreakMileStoneInfos[i].screwAmount;
                if (totalKeys < accumulate)
                {
                    return i;
                }
            }
            return WinStreakGlobalConfig.winStreakMileStoneInfos.Count - 1;
        }

        public WinStreakMileStoneInfo GetMilestoneAtKeys(int totalKeys)
        {
            return GetMilestone(GetMilestoneIndexAtKeys(totalKeys));
        }

        public int GetPointsInMilestone(int totalKeys, int milestoneIndex)
        {
            var ms = GetMilestone(milestoneIndex);
            if (ms == null) return 0;
            int prev = GetMilestonePrevAccumulate(milestoneIndex);
            return Mathf.Clamp(totalKeys - prev, 0, ms.screwAmount);
        }

        public WinStreakMileStoneInfo GetCurrentMileStoneInfo()
        {
            return GetMilestoneAtKeys(CurrentKeys);
        }

        public WinStreakMileStoneInfo GetPreviousMileStoneInfo()
        {
            var current = GetCurrentMileStoneInfo();
            if (current == null || current.index == 0) return null;
            return GetMilestone(current.index - 1);
        }

        public WinStreakMileStoneInfo GetNextMileStoneInfo()
        {
            var current = GetCurrentMileStoneInfo();
            if (current == null || current.index >= GetMilestoneCount() - 1) return null;
            return GetMilestone(current.index + 1);
        }

        public int GetReduntdantAmount()
        {
            var current = GetCurrentMileStoneInfo();
            if (current == null) return 0;
            return GetPointsInMilestone(CurrentKeys, current.index);
        }

        public int GetAccumulateAllMileStoneInfos()
        {
            return WinStreakGlobalConfig != null ? WinStreakGlobalConfig.GetTotalKeys() : 0;
        }

        public bool IsClaimedAllRewards()
        {
            return CurrentKeys >= GetAccumulateAllMileStoneInfos();
        }

        public bool IsMilestoneClaimed(int index)
        {
            return Data != null && Data.claimedMilestoneIndexes != null && Data.claimedMilestoneIndexes.Contains(index);
        }

        public bool IsCanLoseStreak()
        {
            return isActive && WinStreakTier >= 1;
        }

        public bool ShouldWarningWhenLose()
        {
            return isActive && WinStreakTier > 0;
        }

        public bool ShouldShowIntroduce()
        {
            return isActive && Data != null && !Data.isShownFirstTut;
        }

        public bool IsGoodToClaim()
        {
            if (!isActive) return false;
            // Good to claim if there is an unclaimed milestone that has been reached
            if (WinStreakGlobalConfig == null) return false;
            int accumulate = 0;
            int keys = CurrentKeys;
            foreach (var ms in WinStreakGlobalConfig.winStreakMileStoneInfos)
            {
                accumulate += ms.screwAmount;
                if (keys >= accumulate && !IsMilestoneClaimed(ms.index))
                {
                    return true;
                }
            }
            return false;
        }

        // EN: Claims the specified WinStreak milestone, grants its rewards, saves progress, and notifies listeners.
        // VI: Nhận mốc phần thưởng WinStreak chỉ định, trao thưởng, lưu tiến độ và thông báo cho các listener.
        public void ClaimMilestone(int milestoneIndex)
        {
            if (WinStreakGlobalConfig == null) return;
            var ms = WinStreakGlobalConfig.GetMilestone(milestoneIndex);
            if (ms == null) return;

            if (Data.claimedMilestoneIndexes == null)
            {
                Data.claimedMilestoneIndexes = new List<int>();
            }

            bool isNewClaim = !Data.claimedMilestoneIndexes.Contains(milestoneIndex);
            if (isNewClaim)
            {
                Data.claimedMilestoneIndexes.Add(milestoneIndex);
            }

            if (ms.rewards != null)
            {
                foreach (var reward in ms.rewards)
                {
                    ClaimRewardResource(reward);
                }
            }

            if (milestoneIndex >= GetMilestoneCount() - 1)
            {
                Data.isCompleted = true;
            }

            Save();

            if (isNewClaim)
            {
                int level = ProfileManager.instance != null && ProfileManager.instance.PlayerData != null
                    ? ProfileManager.instance.PlayerData.currentLevel
                    : 0;
                //SDKBridge.TrackWinStreakClaim(level, GetMultiplier(), milestoneIndex, CurrentKeys);
            }

            onMilestoneClaimed?.Invoke(ms);
            onDataChanged?.Invoke();
            ProfileManager.instance?.GameFeatureContext?.MissionControlManager?.OnStepInWinstreak();
        }

        public static void ClaimRewardResource(ResourceGame reward, string placement = "WinStreak")
        {
            if (reward == null || ProfileManager.instance == null || ProfileManager.instance.PlayerDataManager == null) return;
            var pdm = ProfileManager.instance.PlayerDataManager;

            switch (reward.type)
            {
                case ResourceType.Coin:
                    pdm.OnAddCoin(reward.value, placement);
                    break;
                case ResourceType.UnlimitedLives:
                    pdm.AddUnlimitedLiveTime(reward.value, placement);
                    break;
                case ResourceType.BoosterAddSlot:
                case ResourceType.BoosterShuffle:
                case ResourceType.BoosterPickup:
                case ResourceType.BoosterSuperBear:
                    pdm.OnAddBooster(reward.type, reward.value, placement);
                    break;
                default:
                    pdm.OnAddResource(reward.type, reward.value, placement);
                    break;
            }
        }

        public void AddKeysDirect(int amount)
        {
            if (Data == null) return;
            int maxKeys = GetAccumulateAllMileStoneInfos();
            Data.keyAmount = Mathf.Min(Data.keyAmount + amount, maxKeys);
            Save();
            onDataChanged?.Invoke();
        }

        private float _lastUpdateCheckTime = 0f;

        public static WinStreakRound GetCurrentRound(DateTime time)
        {
            return GetRound(time.DayOfWeek);
        }

        public static WinStreakRound GetRound(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Friday:
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                case DayOfWeek.Monday:
                    return WinStreakRound.Round1;
                case DayOfWeek.Tuesday:
                case DayOfWeek.Wednesday:
                case DayOfWeek.Thursday:
                    return WinStreakRound.Round2;
                default:
                    return WinStreakRound.None;
            }
        }

        public static DateTime GetCurrentRoundStartTime(DateTime now)
        {
            WinStreakRound round = GetCurrentRound(now);
            if (round == WinStreakRound.Round1)
            {
                // Round 1 started on Friday 00:00:00
                int daysSinceFriday = ((int)now.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7;
                return now.Date.AddDays(-daysSinceFriday);
            }
            else if (round == WinStreakRound.Round2)
            {
                // Round 2 started on Tuesday 00:00:00
                int daysSinceTuesday = ((int)now.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
                return now.Date.AddDays(-daysSinceTuesday);
            }
            return now.Date;
        }

        public static DateTime GetCurrentRoundEndTime(DateTime now)
        {
            WinStreakRound round = GetCurrentRound(now);
            if (round == WinStreakRound.Round1)
            {
                // Round 1 ends on Monday 23:59:59
                int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
                DateTime monday = now.Date.AddDays(daysUntilMonday);
                return monday.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            else if (round == WinStreakRound.Round2)
            {
                // Round 2 ends on Thursday 23:59:59
                int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)now.DayOfWeek + 7) % 7;
                DateTime thursday = now.Date.AddDays(daysUntilThursday);
                return thursday.AddHours(23).AddMinutes(59).AddSeconds(59);
            }
            return now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        }

        public static string GetCurrentRoundId(DateTime now)
        {
            WinStreakRound round = GetCurrentRound(now);
            DateTime start = GetCurrentRoundStartTime(now);
            return $"R{(int)round}_{start:yyyyMMdd}";
        }

        public void CheckAndInitExpire()
        {
            if (Data == null) return;
            DateTime now = DateTime.Now;
            string targetRoundId = GetCurrentRoundId(now);
            bool roundExpired = string.IsNullOrEmpty(Data.treasureExpire)
                || !DateTime.TryParse(Data.treasureExpire, out DateTime expireTime)
                || now > expireTime;
            bool roundChanged = string.IsNullOrEmpty(Data.currentRoundId)
                || Data.currentRoundId != targetRoundId;

            if (roundExpired || roundChanged)
            {
                Resett();
            }
        }

        public void InitNewExpire()
        {
            DateTime now = DateTime.Now;
            DateTime expireTime = GetCurrentPhaseEndTime(now);
            if (Data != null)
            {
                Data.treasureExpire = expireTime.ToString("o");
                Data.currentRoundId = GetCurrentRoundId(now);
                Data.currentRound = (int)GetCurrentRound(now);
                Save();
            }
        }

        public DateTime GetCurrentPhaseEndTime(DateTime now)
        {
            return GetCurrentRoundEndTime(now);
        }

        public string GetCountDownString()
        {
            if (!isActive) return "---";
            if (Data == null || string.IsNullOrEmpty(Data.treasureExpire)) return "---";

            if (!DateTime.TryParse(Data.treasureExpire, out DateTime expireTime))
            {
                return "---";
            }

            DateTime now = DateTime.Now;
            if (now > expireTime || Data.currentRoundId != GetCurrentRoundId(now))
            {
                Resett();
                return "---";
            }

            TimeSpan timeRemaining = expireTime - now;
            if (timeRemaining.Days > 0)
            {
                return $"{timeRemaining.Days:D1}d {timeRemaining.Hours:D2}h";
            }
            if (timeRemaining.Hours > 0)
            {
                return $"{timeRemaining.Hours:D2}h {timeRemaining.Minutes:D2}m";
            }
            return $"{timeRemaining.Minutes}:{timeRemaining.Seconds:D2}";
        }

        public void Resett()
        {
            if (Data != null)
            {
                Data.Reset();
                InitNewExpire();
                Save();
            }
            onDataChanged?.Invoke();
        }

        public bool IsCurrentRoundEnded()
        {
            if (Data == null) return false;
            DateTime now = DateTime.Now;
            string targetRoundId = GetCurrentRoundId(now);
            bool roundExpired = string.IsNullOrEmpty(Data.treasureExpire)
                || !DateTime.TryParse(Data.treasureExpire, out DateTime expireTime)
                || now > expireTime;
            bool roundChanged = string.IsNullOrEmpty(Data.currentRoundId)
                || Data.currentRoundId != targetRoundId;

            return roundExpired || roundChanged;
        }

        public void OnNewDay()
        {
            WasRoundEndedOnNewDay = IsCurrentRoundEnded();
            CheckAndInitExpire();
            onNewDay?.Invoke();
        }

        public void OnUpdate()
        {
            if (Time.unscaledTime - _lastUpdateCheckTime < 1f) return;
            _lastUpdateCheckTime = Time.unscaledTime;
            CheckAndInitExpire();
        }

        public void CheatAddPendingPoint(int point)
        {
            Data.pendingKeyAmount += point;
            Save();
        }

        [Button]
        public void CheatNextRound()
        {
            Resett();
        }

        [Button]
        public void CheatReset()
        {
            Resett();
        }

        [Button]
        public void CheatComplete()
        {
            if (Data == null) return;
            int maxKeys = GetAccumulateAllMileStoneInfos();
            Data.keyAmount = maxKeys;
            Data.pendingKeyAmount = 0;
            int count = GetMilestoneCount();
            if (Data.claimedMilestoneIndexes == null) Data.claimedMilestoneIndexes = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (!Data.claimedMilestoneIndexes.Contains(i)) Data.claimedMilestoneIndexes.Add(i);
            }
            Data.isCompleted = true;
            Save();
            onDataChanged?.Invoke();
        }
    }

    [System.Serializable]
    public class TreasureManager : WinStreakManager
    {
    }
}
