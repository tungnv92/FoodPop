namespace GamePackages
{
    // Lives contract for gameplay, UI and other features; get it with ProfileManager.instance.Features.Get<IHeart>(), null when the HeartSystem package is absent (play is then unlimited).
    // Hợp đồng mạng chơi cho gameplay, UI và tính năng khác; lấy bằng ProfileManager.instance.Features.Get<IHeart>(), null khi không có package HeartSystem (khi đó chơi không giới hạn).
    public interface IHeart : IFeature
    {
        // EN: True when the player may start a level (unlimited lives or at least one heart).
        // VI: True khi người chơi được vào màn (đang có mạng vô hạn hoặc còn ít nhất một mạng).
        bool IsEnoughHeartToPlay();

        // EN: Spends one heart unless lives are unlimited, starting the refill timer when needed.
        // VI: Trừ một mạng trừ khi đang vô hạn, bắt đầu đếm hồi mạng khi cần.
        void UseHeart();

        // EN: Adds hearts, capped at the maximum unless isLimitMax is false.
        // VI: Cộng mạng, giới hạn ở mức tối đa trừ khi isLimitMax là false.
        void AddHeart(int amount = 1, bool isLimitMax = true);

        // EN: Refills hearts to the maximum and stops the refill timer.
        // VI: Hồi đầy mạng và dừng bộ đếm hồi mạng.
        void FillFullHeart();

        // EN: Adds minutes of unlimited lives.
        // VI: Cộng số phút mạng vô hạn.
        void AddUnlimitedLiveTime(int minute);

        // EN: True while unlimited lives are active.
        // VI: True khi đang có mạng vô hạn.
        bool IsUnlimitedLives();

        // EN: True when hearts are at the maximum.
        // VI: True khi mạng đang ở mức tối đa.
        bool IsMaxHeart();

        // EN: Remaining hearts, or -1 while lives are unlimited.
        // VI: Số mạng còn lại, hoặc -1 khi đang có mạng vô hạn.
        int GetRemainHeart();

        // EN: Text for the lives widget: (heart count, timer or "FULL").
        // VI: Chữ cho ô hiển thị mạng: (số mạng, thời gian hoặc "FULL").
        (string, string) GetHeartDisplay();
    }
}
