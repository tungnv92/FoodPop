using System.Collections.Generic;
using UnityEngine;

namespace GamePackages
{
    // Saved state of the daily gift. Field names must not change: they are the keys of existing saves.
    // Trạng thái lưu của quà hằng ngày. Không đổi tên field vì đó là khóa của save đã có.
    [System.Serializable]
    public class DailyGiftData
    {
        [field: SerializeField] public int day { get; set; }
        [field: SerializeField] public bool isEnableClaim { get; set; }
        public List<bool> l_ClaimedChecker = new();
        // EN: Creates a fresh gift cycle starting at day one.
        // VI: Tạo chu kỳ quà mới bắt đầu từ ngày một.
        public DailyGiftData()
        {
            Reset();
        }
        // EN: Restarts the cycle at day one with every day unclaimed.
        // VI: Bắt đầu lại chu kỳ từ ngày một với mọi ngày chưa nhận.
        public void Reset()
        {
            day = 1;
            isEnableClaim = true;
            l_ClaimedChecker.Clear();
            for (int i = 0; i < DailyGiftFeature.MAX_DAY; i++)
            {
                l_ClaimedChecker.Add(false);
            }
        }
    }
}
