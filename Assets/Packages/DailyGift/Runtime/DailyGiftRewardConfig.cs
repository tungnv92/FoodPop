using UnityEngine;
using System.Collections.Generic;
namespace GamePackages
{
    [CreateAssetMenu(fileName = "DailyGiftRewardConfig", menuName = "Scriptable Objects/DailyGiftRewardConfig")]
    public class DailyGiftRewardConfig : ScriptableObject
    {
        [System.Serializable]
        public class ResourceIcon
        {
            [Tooltip("Loại tài nguyên.")]
            public ResourceType type;
            [Tooltip("Icon hiển thị cho loại tài nguyên này.")]
            public Sprite icon;
        }

        [Tooltip("Bật hoặc tắt tính năng.")]
        public bool isFeatureEnabled = true;
        public List<DailyGiftRewardStage> days = new();
        [Tooltip("Icon theo loại tài nguyên, nguồn icon duy nhất cho phần thưởng của cả 7 ngày.")]
        public List<ResourceIcon> resourceIcons = new();

        // EN: Returns the icon configured for a resource type, or null when the type has no entry.
        // VI: Trả icon đã cấu hình cho một loại tài nguyên, hoặc null nếu loại đó chưa có trong danh sách.
        public Sprite GetResourceIcon(ResourceType type)
        {
            ResourceIcon entry = resourceIcons.Find(x => x.type == type);
            return entry != null ? entry.icon : null;
        }

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
    }
}