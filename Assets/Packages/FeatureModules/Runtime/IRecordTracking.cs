namespace GamePackages
{
    // EN: Optional statistics contract shared by gameplay, resource transactions and feature packages.
    // VI: Hợp đồng thống kê tùy chọn dùng chung cho gameplay, giao dịch tài nguyên và package tính năng.
    public interface IRecordTracking : IFeature
    {
        // EN: Returns elapsed retention days from the recorded first-open and last-play timestamps.
        // VI: Trả số ngày giữ chân từ thời điểm mở đầu và chơi gần nhất đã ghi.
        int GetDayUserRetention();

        // EN: Increments the recorded in-app purchase count.
        // VI: Tăng số lượng giao dịch mua trong ứng dụng được ghi nhận.
        void RecordIAP(int value = 1);

        // EN: Returns the recorded number of in-app purchases.
        // VI: Trả số lần mua hàng trong ứng dụng đã ghi.
        int GetIAPCount();

        // EN: Returns the current level retry count, or -1 before any defeat; keeps the existing level parameter contract.
        // VI: Trả số lần thử lại của màn hiện tại, hoặc -1 khi chưa thua; giữ hợp đồng tham số level hiện có.
        int GetRetryCount(int level);

        // EN: Adds the earned booster amount to the tracking data.
        // VI: Cộng số lượng booster nhận được vào dữ liệu theo dõi.
        void RecordBoosterEarn(int amount);

        // EN: Adds the spent booster amount to total and per-level tracking data.
        // VI: Cộng số lượng booster đã dùng vào dữ liệu theo dõi tổng và theo màn chơi.
        void RecordBoosterSpend(int amount = 1);

        // EN: Adds the earned coin amount to the tracking data.
        // VI: Cộng số xu nhận được vào dữ liệu theo dõi.
        void RecordCoinEarn(int value);

        // EN: Adds the spent coin amount to total and per-level tracking data.
        // VI: Cộng số xu đã tiêu vào dữ liệu theo dõi tổng và theo màn chơi.
        void RecordCoinSpend(int value);

        // EN: Increments the rewarded ad watch count.
        // VI: Tăng số lần xem quảng cáo nhận thưởng.
        void RecordAdsReward();

        // EN: Counts one rewarded ad in the current level.
        // VI: Đếm một quảng cáo thưởng trong màn hiện tại.
        void RecordAdsReward_Level();

        // EN: Increments the interstitial ad impression count.
        // VI: Tăng số lần hiển thị quảng cáo xen kẽ.
        void RecordInterShow();

        // EN: Marks the start checkpoint for the specified level if not yet logged.
        // VI: Đánh dấu mốc bắt đầu cho màn chơi chỉ định nếu chưa được ghi nhận.
        void RecordCheckPointStart(int level);

        // EN: Records the completion checkpoint for the specified level and duration.
        // VI: Ghi nhận mốc hoàn thành cho màn chơi và thời lượng chỉ định.
        void RecordCheckPointEnd(int level, int duration);

        // EN: Adds one win to the current gameplay win streak.
        // VI: Cộng một chiến thắng vào chuỗi thắng gameplay hiện tại.
        void RecordWinStreak();

        // EN: Returns the current gameplay win streak.
        // VI: Trả chuỗi thắng gameplay hiện tại.
        int GetWinStreak();

        // EN: Resets the win streak after any lost or abandoned attempt.
        // VI: Reset chuỗi thắng sau mọi lượt thua hoặc bỏ dở.
        void ResetWinStreak();

        // EN: Counts one real defeat of the current level; ResetWin clears it when the level is won.
        // VI: Đếm một lần thua thật ở màn hiện tại; ResetWin xóa số này khi thắng màn.
        void RecordRetry();

        // EN: Clears retry and per-level spending and ad counters after a win.
        // VI: Xóa số lần thử lại cùng bộ đếm chi tiêu và quảng cáo theo màn sau khi thắng.
        void ResetWin();
    }
}
