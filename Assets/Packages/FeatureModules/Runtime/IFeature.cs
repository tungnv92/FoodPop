namespace GamePackages
{
    // Contract shared by every feature created from a FeatureModule asset.
    // Hợp đồng chung cho mọi tính năng được tạo từ asset FeatureModule.
    public interface IFeature
    {
        // EN: Stable save key of the feature, taken from its module asset.
        // VI: Khóa lưu cố định của tính năng, lấy từ asset module.
        string Id { get; }

        // EN: Runs the feature's setup after every module feature is registered for saving and lookup.
        // VI: Chạy phần khởi tạo của tính năng sau khi mọi tính năng module đã được đăng ký lưu và tra cứu.
        void Initialize();

        // EN: Returns whether the current player meets the feature unlock conditions.
        // VI: Trả về người chơi hiện tại có đáp ứng điều kiện mở tính năng hay không.
        bool IsEnabledFeature();

        // EN: Serializes the feature's save model so SaveService can store it as one entry.
        // VI: Chuyển model lưu của tính năng thành JSON để SaveService ghi thành một entry.
        string ToJson();
    }
}
