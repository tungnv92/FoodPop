using System;
using System.Collections.Generic;
using UnityEngine;
using GamePackages;

[CreateAssetMenu(fileName = "MissionControlConfig", menuName = "Scriptable Objects/MissionControlConfig")]
public class MissionControlConfig : ScriptableObject
{
    public bool isFeatureEnabled = true;
    public int unlockLevelIndex = 36;

    [Header("Event Schedule (Day Of Week)")]
    [Tooltip("Day of week the event starts (00:00:00).")]
    public DayOfWeek startDay = DayOfWeek.Friday;
    [Tooltip("Day of week the event ends (23:59:59). Wraps around the week if earlier than startDay.")]
    public DayOfWeek endDay = DayOfWeek.Monday;

    public List<MissionControlStageInfo> stageInfos = new();
    public List<MissionControlQuestIcon> questIcons = new();
    public List<MissionControlGemIcon> gemIcons = new();    
    public List<ResourceGame> grandChestRewardInfos = new();
}

[Serializable]
public class MissionControlStageInfo
{
    public int id;
    public List<ResourceGame> rewardInfos = new();
    public List<MissionControlQuestInfo> questInfos = new();
}

[Serializable]
public class MissionControlQuestInfo
{
    public int stageId
    {
        get
        {
            var cfg = MissionControlManager.Ins != null ? MissionControlManager.Ins.config : null;
            if (cfg != null && cfg.stageInfos != null)
            {
                foreach (var stageInfo in cfg.stageInfos)
                {
                    if (stageInfo.questInfos != null && stageInfo.questInfos.Contains(this))
                    {
                        return stageInfo.id;
                    }
                }
            }
            return 0;
        }
    }
    public MissionControlQuestType questType;
    public int amount;
}

[Serializable]
public class MissionControlQuestIcon
{
    public MissionControlQuestType questType;
    public Sprite icon;
}

[Serializable]
public class MissionControlGemIcon
{
    public int stageId;
    public Sprite icon;
}

public enum MissionControlQuestType
{
    CompleteStepInWinstreak = 0,
    EarnCoin = 1,
    WinLevelsOnFirstTry = 2,
    WinHardLevels = 3,
}
