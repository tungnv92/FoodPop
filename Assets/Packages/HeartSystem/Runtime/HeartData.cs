namespace GamePackages
{
    // Saved lives state. Timers are moments on the TimeManager clock (DateTime ticks, 0 = none).
    // Trạng thái mạng được lưu. Bộ đếm là các mốc thời gian theo đồng hồ TimeManager (DateTime ticks, 0 = không có).
    [System.Serializable]
    public class HeartData
    {
        public int currentHeartAmount;
        public int maxHeart;
        // Moment the next heart is refilled.
        // Thời điểm hồi mạng kế tiếp.
        public long nextHeartTicks;
        // Moment unlimited lives end.
        // Thời điểm hết mạng vô hạn.
        public long unlimitedUntilTicks;
        // EN: Creates a full-hearts state for a new player.
        // VI: Tạo trạng thái đầy mạng cho người chơi mới.
        public HeartData()
        {
            currentHeartAmount = maxHeart = HeartFeature.DefaultMaxHeart;
        }
    }
}
