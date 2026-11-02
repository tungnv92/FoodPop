using UnityEngine;
using System.Collections.Generic;
namespace GamePackages
{
    [CreateAssetMenu(fileName = "DailyGiftRewardConfig", menuName = "Scriptable Objects/DailyGiftRewardConfig")]
    public class DailyGiftRewardConfig : ScriptableObject
    {
        public int LevelActiveFeature = 5;
        public List<DailyGiftRewardStage> days = new();
        public List<DailyGiftRewardStage> GetListDailyGifts()
        {
            return new List<DailyGiftRewardStage>(days);
        }
        public DailyGiftRewardStage GetRewardStage(int index)
        {
            return days[index];
        }
        public List<ResourceGame> GetRewardsByDay(int dayCheck)
        {
            DailyGiftRewardStage stage = days.Find(x => x.dayID == dayCheck);
            if (stage == null) return days[0].gifts;
            return stage.gifts;
        }
        public Sprite GetIconDailyGiftByDayNum(int dayCheck)
        {
            DailyGiftRewardStage stage = days.Find(x => x.dayID == dayCheck);
            if (stage == null) return days[0].giftIcon;
            return stage.giftIcon;
        }
    }
}