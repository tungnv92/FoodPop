using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GamePackages
{
    /// <summary>
    /// LavaQuest daily event: the player races 99 bots through milestones by winning levels in a row; one loss fails the attempt and starts a rejoin cooldown.
    /// All time comes from TimeManager (Services.Time).
    /// </summary>
    // Sự kiện LavaQuest hằng ngày: người chơi đua với 99 bot qua các mốc bằng cách thắng liên tiếp; thua một màn là hỏng lượt và phải chờ để tham gia lại. Mọi thời gian lấy từ TimeManager (Services.Time).
    public class LavaQuestFeature : FeatureManager<LavaQuestData, LavaQuestConfig>,
        IFeatureNewDay, IFeatureUpdate, IFeatureLevelResult
    {
        public static LavaQuestFeature Instance => ProfileManager.instance.Features.Get<LavaQuestFeature>();
        public static LavaQuestFeature Ins => Instance;

        public bool HasCompletedTutorial => Data.isDoneTutorial;
        public bool HasShownInfo => Data.isShowedInfo;
        public int CompletedBotCount => Data.completedBotCount;
        public int CurrentMilestone => Data.currentMilestone;
        public int MaxMilestones => Config.MAX_MILESTONE_AMOUNT;

        // EN: Reads the remote unlock level when remote configuration exists, otherwise uses the local feature configuration.
        // VI: Đọc level mở khóa từ remote khi có cấu hình remote, nếu không dùng config local của tính năng.
        public int UnlockLevel => Services.RemoteConfig != null ? Services.RemoteConfig.level_unlock_busquest : Config.LevelActiveFeature;
        public bool IsAttemptActive => IsEnabledFeature() && Data.attemptStatus == (int)LavaQuestAttemptStatus.InProgress;

        public bool IsCompletedToday => !string.IsNullOrEmpty(Data.completedEventDate) && Data.completedEventDate == GetCurrentDateString();

        public int RemainingParticipants
        {
            get
            {
                if (Data.attemptStatus == (int)LavaQuestAttemptStatus.Completed) return 1 + Data.completedBotCount;
                if (Data.attemptStatus == (int)LavaQuestAttemptStatus.InProgress) return 1 + Data.activeBotCount;
                return 100;
            }
        }


        // EN: Checks the current player level against the remote or local unlock level.
        // VI: Kiểm tra level người chơi hiện tại so với level mở từ remote hoặc config local.
        public override bool IsEnabledFeature()
        {
            return Services.Player.GetCurrentLevel() >= UnlockLevel;
        }

        // EN: Updates the event date without saving; ProfileManager commits after all new-day hooks finish.
        // VI: Cập nhật ngày sự kiện không tự lưu; ProfileManager lưu sau khi mọi hook ngày mới hoàn tất.
        public void OnNewDay()
        {
            UpdateEventDate();
        }

        // EN: Keeps the event date in sync every frame.
        // VI: Đồng bộ ngày sự kiện mỗi frame.
        public void OnUpdate()
        {
            CheckExpire();
        }

        // EN: Persists that the tutorial was completed (first time only).
        // VI: Lưu rằng đã xong tutorial (chỉ lần đầu).
        public void CompleteTutorial()
        {
            if (Data.isDoneTutorial) return;
            Data.isDoneTutorial = true;
            Save();
        }

        // EN: Persists that the info popup was shown (first time only).
        // VI: Lưu rằng đã hiện popup thông tin (chỉ lần đầu).
        public void MarkInfoShown()
        {
            if (Data.isShowedInfo) return;
            Data.isShowedInfo = true;
            Save();
        }

        // EN: Current time on the TimeManager clock.
        // VI: Thời gian hiện tại theo đồng hồ TimeManager.
        public DateTime GetCurrentTime()
        {
            return Services.Time.GetNow();
        }

        /// <summary>
        /// Current date formatted as yyyy-MM-dd.
        /// </summary>
        // EN: Current date on the TimeManager clock formatted as yyyy-MM-dd.
        // VI: Ngày hiện tại theo đồng hồ TimeManager, định dạng yyyy-MM-dd.
        public string GetCurrentDateString()
        {
            return GetCurrentTime().ToString("yyyy-MM-dd");
        }

        // EN: Milestone progress text "current/max".
        // VI: Chữ tiến trình mốc "hiện tại/tối đa".
        public string GetWinStreakString()
        {
            return $"{CurrentMilestone}/{MaxMilestones}";
        }

        /// <summary>
        /// Determines whether the user can start a new LavaQuest attempt.
        /// </summary>
        // EN: True when the feature is active and no attempt is running, today is not completed and no rejoin cooldown is running.
        // VI: True khi tính năng đang hoạt động, không có lượt đang chạy, chưa hoàn thành hôm nay và không đang chờ tham gia lại.
        public bool CanStartNewAttempt()
        {
            if (!IsEnabledFeature()) return false;
            if (IsAttemptActive) return false;
            if (IsCompletedToday) return false;
            if (IsWaitingToRejoin()) return false;
            return true;
        }

        /// <summary>
        /// Starts a new LavaQuest attempt with exactly 100 participants (1 user + 99 fake bots); between minFinalParticipants and maxFinalParticipants bots reach the last milestone.
        /// </summary>
        // EN: Starts a new attempt with 99 generated bots only when CanStartNewAttempt allows it (unlocked, enabled, no attempt in progress, not completed today, no rejoin cooldown), then saves.
        // VI: Bắt đầu lượt mới với 99 bot chỉ khi CanStartNewAttempt cho phép (đã mở, đang bật, không có lượt đang chạy, chưa hoàn thành hôm nay, không đang chờ tham gia lại), rồi lưu.
        public void StartNewAttempt()
        {
            if (!CanStartNewAttempt()) return;

            DateTime now = GetCurrentTime();
            string todayStr = GetCurrentDateString();

            int targetFinalBotWinners = UnityEngine.Random.Range(Config.minFinalParticipants, Config.maxFinalParticipants + 1);
            int targetFinalParticipants = 1 + targetFinalBotWinners;

            int totalBotFailures = 99 - targetFinalBotWinners;

            // Distribute failures progressively across intermediate milestones
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

            LogState("LAVAQUEST_STARTED", $"AttemptId={Data.attemptId}, TargetFinalParticipants={targetFinalParticipants}");
            Save();
        }

        /// <summary>
        /// Handles gameplay level win. Advances milestone by 1, eliminates scheduled bots,
        /// and completes the attempt at the last milestone. Idempotent via resultId.
        /// </summary>
        // EN: On a level win during an attempt, advances one milestone, fails the bots scheduled for it, completes the attempt at the last milestone, saves the result; a repeated resultId is ignored.
        // VI: Khi thắng màn trong lượt chơi, tiến một mốc, loại các bot đã hẹn thua ở mốc đó, hoàn thành lượt ở mốc cuối, lưu kết quả; resultId lặp lại bị bỏ qua.
        public void OnLevelWin(string resultId)
        {
            if (!IsAttemptActive) return;

            if (!string.IsNullOrEmpty(resultId))
            {
                if (Data.lastProcessedGameplayResultId == resultId) return;
                Data.lastProcessedGameplayResultId = resultId;
            }

            LogState("LAVAQUEST_GAMEPLAY_WON", $"CurrentMilestone={Data.currentMilestone}");

            Data.currentMilestone++;
            Data.winStreak = Data.currentMilestone;
            LogState("LAVAQUEST_MILESTONE_INCREASED", $"NewMilestone={Data.currentMilestone}");

            // Progressively fail bots scheduled for this milestone
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

            // Check completion at the last milestone
            if (Data.currentMilestone >= MaxMilestones)
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
            }

            Save();
        }

        /// <summary>
        /// Handles gameplay level loss. Fails the current attempt immediately and starts the rejoin cooldown.
        /// Idempotent via resultId.
        /// </summary>
        // EN: On a level loss during an attempt, fails it, starts the rejoin cooldown, saves the result; a repeated resultId is ignored.
        // VI: Khi thua màn trong lượt chơi, đánh hỏng lượt, bắt đầu thời gian chờ tham gia lại, lưu kết quả; resultId lặp lại bị bỏ qua.
        public void OnLevelLose(string resultId)
        {
            if (!IsAttemptActive) return;

            if (!string.IsNullOrEmpty(resultId))
            {
                if (Data.lastProcessedGameplayResultId == resultId) return;
                Data.lastProcessedGameplayResultId = resultId;
            }

            LogState("LAVAQUEST_GAMEPLAY_LOST", $"Milestone={Data.currentMilestone}");

            DateTime now = GetCurrentTime();
            Data.attemptStatus = (int)LavaQuestAttemptStatus.Failed;
            Data.isFailed = true;
            Data.isCompeting = false;
            Data.winStreak = 0;
            Data.lastFailTime = now.ToString("o");
            Data.retryAvailableTicks = now.AddMinutes(Config.rejoinCooldownMinutes).Ticks;
            Data.attemptEndTime = now.ToString("o");

            LogState("LAVAQUEST_FAILED", $"RetryAvailableAt={new DateTime(Data.retryAvailableTicks):o}");
            Save();
        }

        /// <summary>
        /// Grants the reward once: the gold pool is shared equally between the player and the bot winners.
        /// </summary>
        // EN: Adds the player's share of the gold pool and saves once; ignored when already granted.
        // VI: Cộng phần vàng của người chơi trong quỹ thưởng và lưu một lần; bỏ qua nếu đã nhận.
        public void GrantReward()
        {
            if (Data.rewardGranted) return;

            int rewardPerWinner = GetGoldAmountClaim();
            Data.rewardGranted = true;
            Data.rewardTransactionId = Guid.NewGuid().ToString();

            Services.Player.OnAddCoin(rewardPerWinner, "lava_quest");

            LogState("LAVAQUEST_REWARD_GRANTED", $"RewardPerWinner={rewardPerWinner}, WinnerCount={1 + Data.completedBotCount}, TxId={Data.rewardTransactionId}");
            Save();
        }

        // EN: The player's share of the gold pool (pool / winners; uses the planned winner count before completion).
        // VI: Phần vàng của người chơi (quỹ / số người thắng; dùng số người thắng dự kiến khi chưa hoàn thành).
        public int GetGoldAmountClaim()
        {
            int winners = Data.completedBotCount > 0 ? 1 + Data.completedBotCount : Data.targetFinalParticipants;
            return Config.goldAmount / winners;
        }

        /// <summary>
        /// Daily event-date sync. An active attempt that crosses midnight is never reset.
        /// </summary>
        // EN: Moves the event date to today when the day changes (an attempt in progress keeps running) and saves.
        // VI: Chuyển ngày sự kiện sang hôm nay khi đổi ngày (lượt đang chạy vẫn tiếp tục) và lưu.
        public void CheckExpire()
        {
            if (UpdateEventDate()) Save();
        }

        // EN: True while the rejoin cooldown after a loss is still running.
        // VI: True khi thời gian chờ tham gia lại sau khi thua vẫn đang chạy.
        public bool IsWaitingToRejoin()
        {
            return Data.retryAvailableTicks > GetCurrentTime().Ticks;
        }

        // EN: Remaining rejoin cooldown text, "00:00" when over, "---" when inactive or no cooldown.
        // VI: Chữ thời gian chờ tham gia lại, "00:00" khi đã hết, "---" khi chưa mở hoặc không có thời gian chờ.
        public string GetCountDownRejoinString()
        {
            if (!IsEnabledFeature() || Data.retryAvailableTicks == 0) return "---";
            return FormatRemaining(new DateTime(Data.retryAvailableTicks) - GetCurrentTime());
        }

        // EN: Time left until the end of today, "---" when inactive.
        // VI: Thời gian còn lại tới hết hôm nay, "---" khi chưa mở.
        public string GetCountDownString()
        {
            if (!IsEnabledFeature()) return "---";
            DateTime now = GetCurrentTime();
            DateTime endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
            return FormatRemaining(endOfDay - now);
        }

        // EN: Shows the Home notification dot when a new attempt can start.
        // VI: Hiện chấm thông báo ở Home khi bắt đầu được lượt mới.
        public bool IsNoti()
        {
            return CanStartNewAttempt();
        }

        // EN: Writes a tagged LavaQuest log line.
        // VI: Ghi một dòng log có gắn nhãn LavaQuest.
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

        // EN: Changes only the event date and reports whether it changed, preserving an active attempt.
        // VI: Chỉ đổi ngày sự kiện và báo có thay đổi hay không, giữ nguyên lượt đang chạy.
        private bool UpdateEventDate()
        {
            string todayStr = GetCurrentDateString();
            if (Data.eventDate == todayStr) return false;

            Data.eventDate = todayStr;
            if (Data.attemptStatus == (int)LavaQuestAttemptStatus.InProgress)
                LogState("LAVAQUEST_CROSS_DAY_CONTINUED", $"AttemptDate={Data.attemptDate}, EventDate={todayStr}");
            return true;
        }

        // EN: Splits bot failures across intermediate milestones with small random jitter, keeping the exact total.
        // VI: Chia số bot thua cho các mốc giữa kèm dao động ngẫu nhiên nhỏ, giữ đúng tổng.
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

        // EN: Fisher-Yates shuffle in place.
        // VI: Xáo trộn tại chỗ theo Fisher-Yates.
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

        // EN: Formats a remaining duration as "Dd HHh", "Hh MMm" or "MM:SS"; "00:00" when over.
        // VI: Định dạng thời gian còn lại thành "Dd HHh", "Hh MMm" hoặc "MM:SS"; "00:00" khi đã hết.
        private static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining.TotalSeconds <= 0) return "00:00";
            if (remaining.Days > 0) return $"{remaining.Days}d {remaining.Hours:D2}h";
            if (remaining.Hours > 0) return $"{remaining.Hours}h {remaining.Minutes:D2}m";
            return $"{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        #region Cheat / Debug
        // EN: Cheat: wins one level and opens the LavaQuest popup with the win animation.
        // VI: Cheat: thắng một màn và mở popup LavaQuest với hiệu ứng thắng.
        [Button]
        public void AddWinStreak()
        {
            OnLevelWin(null);
            UIManager.Ins.OpenPopup(PopupId.LavaQuest);
            LavaQuest.isWin = true;
        }

        // EN: Cheat: sets the current milestone.
        // VI: Cheat: đặt mốc hiện tại.
        [Button]
        public void CheatWinStreak(int milestone)
        {
            Data.currentMilestone = Mathf.Clamp(milestone, 0, MaxMilestones);
            Data.winStreak = Data.currentMilestone;
            Save();
        }

        // EN: Cheat: loses the current attempt.
        // VI: Cheat: thua lượt hiện tại.
        [Button]
        public void CheatFail()
        {
            OnLevelLose(null);
        }

        // EN: Cheat: starts an attempt if needed (clearing the cooldown and today's completion) and wins every remaining milestone.
        // VI: Cheat: bắt đầu lượt nếu cần (xóa thời gian chờ và trạng thái đã hoàn thành hôm nay) và thắng mọi mốc còn lại.
        [Button]
        public void CheatWinToFinal()
        {
            if (!IsAttemptActive)
            {
                Data.retryAvailableTicks = 0;
                Data.completedEventDate = "";
                StartNewAttempt();
            }
            while (IsAttemptActive && Data.currentMilestone < MaxMilestones)
            {
                OnLevelWin(null);
            }

            UIManager.Ins.OpenPopup(PopupId.LavaQuest);
        }

        // EN: Cheat: clears the rejoin cooldown.
        // VI: Cheat: xóa thời gian chờ tham gia lại.
        [Button]
        public void CheatClearCooldown()
        {
            Data.retryAvailableTicks = 0;
            Data.lastFailTime = "";
            Save();
        }

        // EN: Cheat: clears the current attempt.
        // VI: Cheat: xóa lượt chơi hiện tại.
        [Button]
        public void ClearDataNewEvent()
        {
            Data.ClearDataNewEvent();
            Save();
        }

        // EN: Cheat: clears all LavaQuest data.
        // VI: Cheat: xóa toàn bộ dữ liệu LavaQuest.
        [Button]
        public void ClearAllData()
        {
            Data.Reset();
            Save();
        }
        #endregion
    }
}
