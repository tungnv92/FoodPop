using System.Collections.Generic;

namespace GamePackages
{
    // Saved LavaQuest state: tutorial/info flags, the current attempt, its bots and the rejoin cooldown.
    // Trạng thái lưu của LavaQuest: cờ tutorial/info, lượt chơi hiện tại, các bot và thời gian chờ tham gia lại.
    [System.Serializable]
    public class LavaQuestData
    {
        public bool isShowedInfo;
        public bool isDoneTutorial;

        public int winStreak;
        public bool isCompeting;
        public int playersLeftAmount = 100;

        public string eventDate = "";
        public string attemptId = "";
        public string attemptDate = "";
        public string attemptStartTime = "";
        public string attemptEndTime = "";
        public int attemptStatus; // LavaQuestAttemptStatus
        public int currentMilestone;

        public string lastFailTime = "";
        // Moment (TimeManager clock, DateTime ticks) the player may join again after a loss; 0 = no cooldown.
        // Thời điểm (đồng hồ TimeManager, DateTime ticks) được tham gia lại sau khi thua; 0 = không phải chờ.
        public long retryAvailableTicks;

        public int targetFinalParticipants = 10;
        public int activeBotCount = 99;
        public int failedBotCount;
        public int completedBotCount;

        public bool isCompleted;
        public bool isFailed;
        public bool rewardGranted;
        public string rewardTransactionId = "";
        public string lastProcessedGameplayResultId = "";
        public string completedEventDate = "";

        public List<LavaQuestBotData> bots = new();

        // EN: Creates an empty state with no attempt.
        // VI: Tạo trạng thái trống, chưa có lượt chơi.
        public LavaQuestData()
        {
            Reset();
        }

        // EN: Clears every flag, the attempt, the cooldown and the bots.
        // VI: Xóa mọi cờ, lượt chơi, thời gian chờ và các bot.
        public void Reset()
        {
            isShowedInfo = false;
            isDoneTutorial = false;
            eventDate = "";
            lastFailTime = "";
            retryAvailableTicks = 0;
            completedEventDate = "";
            targetFinalParticipants = 10;
            ClearDataNewEvent();
        }

        // EN: Clears the current attempt and its bots, keeping flags, cooldown and the completed date.
        // VI: Xóa lượt chơi hiện tại và các bot, giữ cờ, thời gian chờ và ngày đã hoàn thành.
        public void ClearDataNewEvent()
        {
            winStreak = 0;
            isCompeting = false;
            playersLeftAmount = 100;

            attemptId = "";
            attemptDate = "";
            attemptStartTime = "";
            attemptEndTime = "";
            attemptStatus = (int)LavaQuestAttemptStatus.NotStarted;
            currentMilestone = 0;

            activeBotCount = 99;
            failedBotCount = 0;
            completedBotCount = 0;

            isCompleted = false;
            isFailed = false;
            rewardGranted = false;
            rewardTransactionId = "";
            lastProcessedGameplayResultId = "";

            bots.Clear();
        }
    }
}
