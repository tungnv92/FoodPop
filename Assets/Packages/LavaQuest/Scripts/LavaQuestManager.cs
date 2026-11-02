using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePackages
{
    /// <summary>
    /// Manages the LavaQuest daily event lifecycle, participant simulation, progression, and rewards.
    /// Follows the specifications in LavaQuest_Event_Rules.md.
    /// </summary>
    [Serializable]
    public class LavaQuestManager : ContextData<LavaQuestData>, IFeatureNewDay, IFeatureUpdate
    {
        public static LavaQuestManager Instance => ProfileManager.instance != null && ProfileManager.instance.GameFeatureContext != null
            ? ProfileManager.instance.GameFeatureContext.LavaQuestManager
            : null;

        public static LavaQuestManager Ins => Instance;

        public LavaQuestConfig LavaQuestConfig;
        public LavaQuestConfig lavaQuestConfig
        {
            get => LavaQuestConfig;
            set => LavaQuestConfig = value;
        }

        public new LavaQuestData Data => base.Data ?? (ProfileManager.instance != null && ProfileManager.instance.PlayerData != null ? ProfileManager.instance.PlayerData.LavaQuestData : null);
        public LavaQuestData lavaQuestData => Data;

        private bool _pendingWinAnimation;
        private bool _pendingLoseAnimation;

        public int UnlockLevel
        {
            get
            {
                if (ProfileManager.instance != null && ProfileManager.instance.RemoteConfigBase != null)
                {
                    return ProfileManager.instance.RemoteConfigBase.level_unlock_busquest;
                }
                return LavaQuestConfig != null ? LavaQuestConfig.LevelActiveFeature : 45;
            }
        }

        public int unlockLevelIndex => UnlockLevel;

        public static bool IsTemporarilyDisabled = false;

        public bool IsLevelUnlocked => ProfileManager.instance != null && ProfileManager.instance.PlayerData != null &&
                                       ProfileManager.instance.PlayerData.currentLevel >= UnlockLevel;

        public bool isActive => IsLevelUnlocked && LavaQuestConfig.isFeatureEnabled;

        public bool IsEnabled => LavaQuestConfig.isFeatureEnabled;
        public int MaxMilestones => LavaQuestConfig != null ? LavaQuestConfig.MAX_MILESTONE_AMOUNT : 7;

        public int CurrentMilestone => Data != null ? Data.currentMilestone : 0;

        public bool IsAttemptActive => isActive && Data != null && Data.attemptStatus == (int)LavaQuestAttemptStatus.InProgress;

        /// <summary>
        /// Checks if user is currently in progress of an attempt and will fail upon losing.
        /// </summary>
        public bool CanFailProgress => isActive && IsAttemptActive && Data != null && Data.currentMilestone < MaxMilestones;

        public bool IsCanFailProgress() => CanFailProgress;

        public bool IsCompletedToday => Data != null && !string.IsNullOrEmpty(Data.completedEventDate) && Data.completedEventDate == GetCurrentDateString();

        public int RemainingParticipants
        {
            get
            {
                if (Data == null) return 100;
                if (Data.attemptStatus == (int)LavaQuestAttemptStatus.Completed)
                {
                    return 1 + Data.completedBotCount;
                }
                if (Data.attemptStatus == (int)LavaQuestAttemptStatus.InProgress)
                {
                    return 1 + Data.activeBotCount;
                }
                return 100;
            }
        }

        public override void SetContext(LavaQuestData data)
        {
            if (data == null)
            {
                data = new LavaQuestData();
                if (ProfileManager.instance != null && ProfileManager.instance.PlayerData != null)
                {
                    ProfileManager.instance.PlayerData.LavaQuestData = data;
                }
            }
            base.SetContext(data);
            CheckExpire();
        }

        public void OnNewDay()
        {
            CheckExpire();
        }

        public void OnUpdate()
        {
            CheckExpire();
        }

        /// <summary>
        /// Returns authoritative time (server time if available, otherwise device local time).
        /// </summary>
        public DateTime GetCurrentTime()
        {
            if (ProfileManager.instance != null && ProfileManager.instance.TimeManager != null)
            {
                return ProfileManager.instance.TimeManager.GetNow();
            }
            return DateTime.Now;
        }

        /// <summary>
        /// Returns authoritative date string formatted as YYYY-MM-DD.
        /// </summary>
        public string GetCurrentDateString()
        {
            return GetCurrentTime().ToString("yyyy-MM-dd");
        }

        public float GetBarFillAmount()
        {
            int max = MaxMilestones;
            return max > 0 && Data != null ? (float)Data.currentMilestone / max : 0f;
        }

        public string GetWinStreakString()
        {
            int max = MaxMilestones;
            int current = CurrentMilestone;
            return $"{current}/{max}";
        }

        /// <summary>
        /// Determines whether the user can start a new LavaQuest attempt.
        /// </summary>
        public bool CanStartNewAttempt()
        {
            if (!isActive || Data == null) return false;
            if (IsAttemptActive) return false;
            if (IsCompletedToday) return false;
            if (IsWaitingToRejoin()) return false;
            return true;
        }

        /// <summary>
        /// Starts a new LavaQuest attempt with exactly 100 participants (1 user + 99 fake bots).
        /// Guaranteed final participants between 9 and 14 upon milestone 7 completion.
        /// </summary>
        public void StartNewAttempt()
        {
            if (Data == null) return;
            if (!CanStartNewAttempt() && Data.attemptStatus == (int)LavaQuestAttemptStatus.InProgress) return;

            DateTime now = GetCurrentTime();
            string todayStr = GetCurrentDateString();

            int minBots = LavaQuestConfig != null ? LavaQuestConfig.minFinalParticipants : 9;
            int maxBots = LavaQuestConfig != null ? LavaQuestConfig.maxFinalParticipants : 14;
            int targetFinalBotWinners = UnityEngine.Random.Range(minBots, maxBots + 1); // 9 to 14 bots reach step 7
            int targetFinalParticipants = 1 + targetFinalBotWinners; // 10 to 15 total winners

            int totalBotFailures = 99 - targetFinalBotWinners; // 85 to 90 bots fail across milestones 1 to 6

            // Distribute failures progressively across intermediate milestones 1 through 6
            int[] failCounts = DistributeBotFailures(totalBotFailures, MaxMilestones - 1);

            // Generate 99 bot competitors
            List<LavaQuestBotData> bots = new List<LavaQuestBotData>(99);
            for (int i = 0; i < targetFinalBotWinners; i++)
            {
                bots.Add(new LavaQuestBotData(0, -1)); // Winner bot: never fails (-1)
            }

            for (int m = 1; m < MaxMilestones; m++)
            {
                int countForMilestone = failCounts[m - 1];
                for (int i = 0; i < countForMilestone; i++)
                {
                    bots.Add(new LavaQuestBotData(0, m));
                }
            }

            // Shuffle bots and assign 1-based IDs
            ShuffleList(bots);
            for (int i = 0; i < bots.Count; i++)
            {
                bots[i].botId = i + 1;
                bots[i].currentMilestone = 0;
                bots[i].status = LavaQuestBotStatus.Active;
            }

            // Update persistent attempt state
            Data.bots = bots;
            Data.attemptId = Guid.NewGuid().ToString();
            Data.attemptDate = todayStr;
            Data.eventDate = todayStr;
            Data.attemptStartTime = now.ToString("o");
            Data.attemptEndTime = "";
            Data.attemptStatus = (int)LavaQuestAttemptStatus.InProgress;
            Data.currentMilestone = 0;
            Data.winStreak = 0;
            Data.isCompeting = true;
            Data.isCompleted = false;
            Data.isFailed = false;
            Data.rewardGranted = false;
            Data.rewardTransactionId = "";
            Data.lastProcessedGameplayResultId = "";

            Data.targetFinalParticipants = targetFinalParticipants;
            Data.activeBotCount = 99;
            Data.failedBotCount = 0;
            Data.completedBotCount = 0;
            Data.playersLeftAmount = 100;

            InitNewExpire();
            LogState("LAVAQUEST_STARTED", $"AttemptId={Data.attemptId}, TargetFinalParticipants={targetFinalParticipants}");
            Save();
        }

        public void StartNewJourney()
        {
            StartNewAttempt();
        }

        /// <summary>
        /// Handles gameplay level win. Advances milestone by 1, eliminates scheduled bots,
        /// and completes attempt if milestone 7 is reached. Idempotent via gameplayResultId.
        /// </summary>
        public void OnLevelWin(string gameplayResultId = null)
        {
            if (!isActive || Data == null || !IsAttemptActive) return;

            if (!string.IsNullOrEmpty(gameplayResultId))
            {
                if (Data.lastProcessedGameplayResultId == gameplayResultId) return;
                Data.lastProcessedGameplayResultId = gameplayResultId;
            }

            LogState("LAVAQUEST_GAMEPLAY_WON", $"CurrentMilestone={Data.currentMilestone}");

            Data.currentMilestone++;
            Data.winStreak = Data.currentMilestone;
            LogState("LAVAQUEST_MILESTONE_INCREASED", $"NewMilestone={Data.currentMilestone}");

            // Progressively fail bots scheduled for this milestone
            if (Data.bots != null)
            {
                for (int i = 0; i < Data.bots.Count; i++)
                {
                    var bot = Data.bots[i];
                    if (bot.status == LavaQuestBotStatus.Active)
                    {
                        // Bots only fail at intermediate milestones (1 to MaxMilestones - 1). Winner bots (-1 or >= MaxMilestones) never fail.
                        if (bot.failAtMilestone == Data.currentMilestone && Data.currentMilestone < MaxMilestones && bot.failAtMilestone > 0)
                        {
                            bot.status = LavaQuestBotStatus.Failed;
                        }
                        else
                        {
                            bot.currentMilestone = Data.currentMilestone;
                        }
                    }
                }

                Data.activeBotCount = Data.bots.FindAll(b => b.status == LavaQuestBotStatus.Active).Count;
                Data.failedBotCount = Data.bots.FindAll(b => b.status == LavaQuestBotStatus.Failed).Count;
                Data.playersLeftAmount = 1 + Data.activeBotCount;
                LogState("LAVAQUEST_BOTS_UPDATED", $"ActiveBots={Data.activeBotCount}, FailedBots={Data.failedBotCount}");
            }

            // Check completion at milestone 7
            if (Data.currentMilestone >= MaxMilestones)
            {
                if (Data.bots != null)
                {
                    for (int i = 0; i < Data.bots.Count; i++)
                    {
                        var bot = Data.bots[i];
                        if (bot.status == LavaQuestBotStatus.Active)
                        {
                            bot.status = LavaQuestBotStatus.Completed;
                            bot.currentMilestone = MaxMilestones;
                        }
                    }
                    Data.activeBotCount = 0;
                    Data.completedBotCount = Data.bots.FindAll(b => b.status == LavaQuestBotStatus.Completed).Count;
                }

                Data.playersLeftAmount = 1 + Data.completedBotCount;
                Data.attemptStatus = (int)LavaQuestAttemptStatus.Completed;
                Data.isCompleted = true;
                Data.isCompeting = false;
                Data.attemptEndTime = GetCurrentTime().ToString("o");

                // If attempt started today, mark completed for today
                string todayStr = GetCurrentDateString();
                if (Data.attemptDate == todayStr)
                {
                    Data.completedEventDate = todayStr;
                }

                LogState("LAVAQUEST_COMPLETED", $"TotalWinners={Data.playersLeftAmount}, BotWinners={Data.completedBotCount}");

                // Transactional reward granting
                //GrantReward();
            }

            _pendingWinAnimation = true;
            Save();
        }

        /// <summary>
        /// Handles gameplay level loss. Fails current attempt immediately and starts a 30-minute cooldown.
        /// Idempotent via gameplayResultId.
        /// </summary>
        public void OnLevelLose(string gameplayResultId = null)
        {
            if (!isActive || Data == null || !IsAttemptActive) return;

            if (!string.IsNullOrEmpty(gameplayResultId))
            {
                if (Data.lastProcessedGameplayResultId == gameplayResultId) return;
                Data.lastProcessedGameplayResultId = gameplayResultId;
            }

            LogState("LAVAQUEST_GAMEPLAY_LOST", $"Milestone={Data.currentMilestone}");

            DateTime now = GetCurrentTime();
            int cooldownMinutes = LavaQuestConfig != null ? LavaQuestConfig.rejoinCooldownMinutes : 30;
            DateTime retryAvailableAt = now.AddMinutes(cooldownMinutes);

            Data.attemptStatus = (int)LavaQuestAttemptStatus.Failed;
            Data.isFailed = true;
            Data.isCompeting = false;
            Data.winStreak = 0;
            Data.lastFailTime = now.ToString("o");
            Data.retryAvailableAt = retryAvailableAt.ToString("o");
            Data.rejoinExpire = Data.retryAvailableAt;
            Data.attemptEndTime = now.ToString("o");

            LogState("LAVAQUEST_FAILED", $"RetryAvailableAt={Data.retryAvailableAt}");
            _pendingLoseAnimation = true;
            Save();
        }

        /// <summary>
        /// Grants reward to player transactionally and idempotently.
        /// Shared equally between 1 real user and bot winners (total 10,000 Coins pool).
        /// </summary>
        public void GrantReward()
        {
            if (Data == null || Data.rewardGranted) return;

            int winnerCount = 1 + Data.completedBotCount;
            if (winnerCount <= 0)
            {
                winnerCount = Data.targetFinalParticipants > 0 ? Data.targetFinalParticipants : 10;
            }

            int totalPool = LavaQuestConfig != null ? LavaQuestConfig.goldAmount : 10000;
            int rewardPerWinner = totalPool / winnerCount;

            Data.rewardGranted = true;
            Data.rewardTransactionId = Guid.NewGuid().ToString();

            if (ProfileManager.instance != null && ProfileManager.instance.PlayerDataManager != null)
            {
                ProfileManager.instance.PlayerDataManager.OnAddCoin(rewardPerWinner, "lava_quest");
                ProfileManager.instance.SaveData();
            }

            LogState("LAVAQUEST_REWARD_GRANTED", $"RewardPerWinner={rewardPerWinner}, WinnerCount={winnerCount}, TxId={Data.rewardTransactionId}");
            Save();
        }

        public int GetGoldAmountClaim()
        {
            int total = LavaQuestConfig != null ? LavaQuestConfig.goldAmount : 10000;
            int winners = (Data != null && Data.completedBotCount > 0)
                ? (1 + Data.completedBotCount)
                : (Data != null && Data.targetFinalParticipants > 0 ? Data.targetFinalParticipants : 10);
            return total / winners;
        }

        public bool ShouldShowWinFlow() => _pendingWinAnimation;
        public bool ShouldShowLoseFlow() => _pendingLoseAnimation;

        public void ConsumeWinFlow()
        {
            _pendingWinAnimation = false;
        }

        public void ConsumeLoseFlow()
        {
            _pendingLoseAnimation = false;
        }

        public void InitNewExpire()
        {
            if (Data == null) return;
            DateTime now = GetCurrentTime();
            DateTime endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
            Data.expire = endOfDay.ToString("o");
            Save();
        }

        /// <summary>
        /// Daily reset and expiration check.
        /// CRITICAL RULE: An active attempt that crosses midnight is NEVER reset!
        /// </summary>
        public void CheckExpire()
        {
            if (Data == null) return;

            DateTime now = GetCurrentTime();
            string todayStr = GetCurrentDateString();

            if (Data.attemptStatus == (int)LavaQuestAttemptStatus.InProgress)
            {
                // Active attempt crossing midnight: DO NOT RESET!
                if (Data.eventDate != todayStr)
                {
                    Data.eventDate = todayStr;
                    LogState("LAVAQUEST_CROSS_DAY_CONTINUED", $"AttemptDate={Data.attemptDate}, EventDate={todayStr}");
                    Save();
                }
                return;
            }

            // Not currently in progress
            if (Data.eventDate != todayStr)
            {
                Data.eventDate = todayStr;
                InitNewExpire();
                Save();
            }
        }

        public bool IsWaitingToRejoin()
        {
            if (Data == null || string.IsNullOrEmpty(Data.retryAvailableAt)) return false;
            if (DateTime.TryParse(Data.retryAvailableAt, out DateTime retryAt))
            {
                return (retryAt - GetCurrentTime()).TotalSeconds > 0;
            }
            return false;
        }

        public string GetCountDownRejoinString()
        {
            if (!isActive || Data == null || string.IsNullOrEmpty(Data.retryAvailableAt)) return "---";

            if (DateTime.TryParse(Data.retryAvailableAt, out DateTime retryAt))
            {
                TimeSpan remaining = retryAt - GetCurrentTime();
                if (remaining.TotalSeconds <= 0) return "00:00";

                if (remaining.Days > 0)
                {
                    return $"{remaining.Days}d {remaining.Hours:D2}h";
                }
                if (remaining.Hours > 0)
                {
                    return $"{remaining.Hours}h {remaining.Minutes:D2}m";
                }
                return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }

            return "---";
        }

        public string GetCountDownString()
        {
            if (!isActive || Data == null) return "---";

            DateTime now = GetCurrentTime();
            DateTime endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
            TimeSpan timeRemaining = endOfDay - now;
            if (timeRemaining.TotalSeconds <= 0) return "00:00";

            if (timeRemaining.Days > 0)
            {
                return $"{timeRemaining.Days}d {timeRemaining.Hours:D2}h";
            }
            if (timeRemaining.Hours > 0)
            {
                return $"{timeRemaining.Hours}h {timeRemaining.Minutes:D2}m";
            }
            return $"{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
        }

        public bool IsNoti()
        {
            return isActive && CanStartNewAttempt();
        }

        public bool ShouldWarningWhenLose()
        {
            return CanFailProgress;
        }

        public void LogState(string eventName, string details = "")
        {
            if (string.IsNullOrEmpty(details))
            {
                Debug.Log($"[LavaQuest] {eventName}");
            }
            else
            {
                Debug.Log($"[LavaQuest] {eventName} - {details}");
            }
        }

        private static int[] DistributeBotFailures(int totalFailures, int milestonesCount)
        {
            if (milestonesCount <= 0) return Array.Empty<int>();
            int[] failCounts = new int[milestonesCount];
            int baseCount = totalFailures / milestonesCount;
            int remainder = totalFailures % milestonesCount;

            for (int i = 0; i < milestonesCount; i++)
            {
                failCounts[i] = baseCount + (i < remainder ? 1 : 0);
            }

            // Apply controlled random jitter between adjacent milestones while preserving exact total
            for (int i = 0; i < milestonesCount - 1; i++)
            {
                int jitter = UnityEngine.Random.Range(-2, 3);
                if (failCounts[i] + jitter >= 5 && failCounts[i + 1] - jitter >= 5)
                {
                    failCounts[i] += jitter;
                    failCounts[i + 1] -= jitter;
                }
            }

            ShuffleList(failCounts);
            return failCounts;
        }

        private static void ShuffleList<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        #region Cheat / Debug
        [Button]
        public void AddWinStreak()
        {
            OnLevelWin();
            UIManager.Ins.OpenPopup(PopupId.LavaQuest);
            ConsumeWinFlow();
            LavaQuest.isWin = true;
        }

        [Button]
        public void CheatWinStreak(int milestone)
        {
            if (Data == null) return;
            Data.currentMilestone = Mathf.Clamp(milestone, 0, MaxMilestones);
            Data.winStreak = Data.currentMilestone;
            Save();
        }

        [Button]
        public void CheatFail()
        {
            OnLevelLose();
        }

        [Button]
        public void CheatWinToFinal()
        {
            if (Data == null) return;
            if (!IsAttemptActive) StartNewAttempt();
            while (IsAttemptActive && Data.currentMilestone < MaxMilestones)
            {
                OnLevelWin();
            }

            UIManager.Ins.OpenPopup(PopupId.LavaQuest);
        }

        [Button]
        public void CheatClearCooldown()
        {
            if (Data == null) return;
            Data.retryAvailableAt = "";
            Data.rejoinExpire = "";
            Data.lastFailTime = "";
            Save();
        }

        [Button]
        public void ClearDataNewEvent()
        {
            Data?.ClearDataNewEvent();
            Save();
        }

        [Button]
        public void ClearAllData()
        {
            Data?.ClearAllData();
            Save();
        }
        #endregion
    }
}