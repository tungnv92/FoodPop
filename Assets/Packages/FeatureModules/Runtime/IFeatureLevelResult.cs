namespace GamePackages
{
    // Optional hook for features that react to a level win or loss; resultId lets a feature ignore the same result twice.
    // Hook tùy chọn cho tính năng phản ứng khi thắng hoặc thua màn; resultId giúp tính năng bỏ qua cùng một kết quả lặp lại.
    public interface IFeatureLevelResult
    {
        // EN: Called once per won level.
        // VI: Gọi một lần cho mỗi màn thắng.
        void OnLevelWin(string resultId);

        // EN: Called once per lost level (including a session killed mid-level).
        // VI: Gọi một lần cho mỗi màn thua (kể cả phiên bị tắt giữa màn).
        void OnLevelLose(string resultId);
    }
}
