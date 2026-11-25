namespace GamePackages
{
    // Saved state of the daily reward for the current day.
    // Trạng thái lưu của thưởng hằng ngày trong ngày hiện tại.
    [System.Serializable]
    public class DailyRewardData
    {
        public int totalAdsClaimed;
        public int totalFreeClaimed;
        // Time (TimeUtil format, TimeManager clock) when the next free reward unlocks; empty = no cooldown.
        // Thời điểm (định dạng TimeUtil, đồng hồ TimeManager) mở lại thưởng miễn phí kế tiếp; rỗng = không phải chờ.
        public string nextFreeClaimTime;
        // EN: Creates an empty day with nothing claimed.
        // VI: Tạo một ngày mới chưa nhận gì.
        public DailyRewardData()
        {
            Reset();
        }
        // EN: Clears every claim and the free-reward cooldown for a new day.
        // VI: Xóa mọi lượt nhận và thời gian chờ thưởng miễn phí cho ngày mới.
        public void Reset()
        {
            totalAdsClaimed = 0;
            totalFreeClaimed = 0;
            nextFreeClaimTime = "";
        }
    }
}
