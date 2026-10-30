using System;
using System.Collections.Generic;

namespace GamePackages
{
    [Serializable]
    public class MissionControlDataQuestAmount
    {
        public MissionControlQuestType questType;
        public int count;

        public MissionControlDataQuestAmount() { }

        public MissionControlDataQuestAmount(MissionControlQuestType type, int count)
        {
            this.questType = type;
            this.count = count;
        }
    }

    [Serializable]
    public class MissionControlDataQuestClaimed
    {
        public int stageId;
        public MissionControlQuestType questType;
        public int amount;

        public MissionControlDataQuestClaimed() { }

        public MissionControlDataQuestClaimed(int stageId, MissionControlQuestType type, int amount)
        {
            this.stageId = stageId;
            this.questType = type;
            this.amount = amount;
        }
    }

    [Serializable]
    public class MissionControlData
    {
        public string currentCycleId = "";
        public string expire = "";
        public bool isClaimedGrandChest;
        public bool isShowedInfo;
        public bool isDoneTutorial;
        public List<int> claimedStageIds = new();
        public List<MissionControlDataQuestAmount> questCounts = new();
        public List<MissionControlDataQuestClaimed> questClaimeds = new();

        public MissionControlData()
        {
            Reset();
        }

        public void Reset()
        {
            currentCycleId = "";
            expire = "";
            isClaimedGrandChest = false;
            isShowedInfo = false;
            isDoneTutorial = false;
            claimedStageIds.Clear();
            questCounts.Clear();
            questClaimeds.Clear();
        }

        public void ClearDataNewEvent(string newCycleId, string newExpire)
        {
            currentCycleId = newCycleId;
            expire = newExpire;
            isClaimedGrandChest = false;
            isShowedInfo = false;
            claimedStageIds.Clear();
            questCounts.Clear();
            questClaimeds.Clear();
        }

        public void ClearAllData()
        {
            Reset();
        }

        public void SaveData()
        {
            ProfileManager.instance?.SaveData();
        }
    }
}
