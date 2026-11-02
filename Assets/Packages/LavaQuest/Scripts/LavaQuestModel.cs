using System;

namespace GamePackages
{
    /// <summary>
    /// Status of a LavaQuest attempt.
    /// </summary>
    public enum LavaQuestAttemptStatus
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3
    }

    /// <summary>
    /// Status of a fake bot participant in LavaQuest.
    /// </summary>
    public enum LavaQuestBotStatus
    {
        Active = 0,
        Failed = 1,
        Completed = 2
    }

    /// <summary>
    /// Represents the persistent simulation state of a fake bot competitor.
    /// </summary>
    [Serializable]
    public class LavaQuestBotData
    {
        public int botId;
        public int currentMilestone;
        public LavaQuestBotStatus status;
        public int failAtMilestone; // Milestone (1 to 6) at which this bot fails; -1 or >= 7 if winner

        public LavaQuestBotData()
        {
            botId = 0;
            currentMilestone = 0;
            status = LavaQuestBotStatus.Active;
            failAtMilestone = -1;
        }

        public LavaQuestBotData(int id, int failMilestone)
        {
            botId = id;
            currentMilestone = 0;
            status = LavaQuestBotStatus.Active;
            failAtMilestone = failMilestone;
        }
    }
}
