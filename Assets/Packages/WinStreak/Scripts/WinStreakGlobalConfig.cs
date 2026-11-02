using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GamePackages
{
    [CreateAssetMenu(fileName = "WinStreakGlobalConfig", menuName = "Scriptable Objects/WinStreakGlobalConfig")]
    public class WinStreakGlobalConfig : ScriptableObject
    {
        [Title("UNLOCK & SCHEDULE:")]
        [Tooltip("Toggle to enable or disable the WinStreak event")]
        public bool isFeatureEnabled = false;
        public int LevelActiveFeature = 26;

        [Title("ROUND 1 (Weekend):")]
        [Tooltip("Round 1 start: Friday 00:00:00")]
        public DayOfWeek Round1StartDay = DayOfWeek.Friday;
        [Tooltip("Round 1 end: Monday 23:59:59")]
        public DayOfWeek Round1EndDay = DayOfWeek.Monday;

        [Title("ROUND 2 (Midweek):")]
        [Tooltip("Round 2 start: Tuesday 00:00:00")]
        public DayOfWeek Round2StartDay = DayOfWeek.Tuesday;
        [Tooltip("Round 2 end: Thursday 23:59:59")]
        public DayOfWeek Round2EndDay = DayOfWeek.Thursday;

        // Backward compatibility
        public DayOfWeek StartDayOfWeek
        {
            get => Round1StartDay;
            set => Round1StartDay = value;
        }
        public DayOfWeek EndDayOfWeek
        {
            get => Round1EndDay;
            set => Round1EndDay = value;
        }

        public float FillSpeed = 16f;

        [Title("RESOURCE SPRITES:")]
        public List<ResourceSpriteEntry> resourceSprites = new();

        [Title("MILESTONES:")]
        [FormerlySerializedAs("treasureMileStoneInfos")]
        public List<WinStreakMileStoneInfo> winStreakMileStoneInfos = new();
        public List<WinStreakMileStoneInfo> treasureMileStoneInfos => winStreakMileStoneInfos;

        public WinStreakMileStoneInfo GetMilestone(int index)
        {
            if (index >= 0 && index < winStreakMileStoneInfos.Count)
                return winStreakMileStoneInfos[index];
            return null;
        }

        public int GetTotalKeys()
        {
            int total = 0;
            if (winStreakMileStoneInfos != null)
            {
                foreach (var ms in winStreakMileStoneInfos)
                {
                    total += ms.screwAmount;
                }
            }
            return total;
        }

        public Sprite GetResourceSprite(ResourceType type)
        {
            if (resourceSprites != null)
            {
                var entry = resourceSprites.Find(x => x.type == type);
                if (entry != null) return entry.sprite;
            }
            return null;
        }
    }

    [Serializable]
    public class ResourceSpriteEntry
    {
        public ResourceType type;
        [PreviewField(50), HideLabel]
        public Sprite sprite;
    }

    [Serializable]
    public class WinStreakMileStoneInfo
    {
        [HorizontalGroup("Split", Width = 0.3f)]
        public int index;

        [HorizontalGroup("Split")]
        [VerticalGroup("Split/Right")]
        public int screwAmount;

        [VerticalGroup("Split/Right")]
        public List<ResourceGame> rewards = new();

        public WinStreakMileStoneInfo() { }

        public WinStreakMileStoneInfo(int index, int screwAmount, List<ResourceGame> rewards)
        {
            this.index = index;
            this.screwAmount = screwAmount;
            this.rewards = rewards;
        }

        public string GetRewardsStr()
        {
            string str = "";
            if (rewards != null)
            {
                foreach (var r in rewards)
                {
                    str += $"{r.type} {r.GetNormalValue()} ";
                }
            }
            return str;
        }
    }

    [Serializable]
    public class TreasureMileStoneInfo : WinStreakMileStoneInfo
    {
        public TreasureMileStoneInfo() : base() { }
        public TreasureMileStoneInfo(int index, int screwAmount, List<ResourceGame> rewards) : base(index, screwAmount, rewards) { }
    }
}
