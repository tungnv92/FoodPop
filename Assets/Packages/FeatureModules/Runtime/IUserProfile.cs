using UnityEngine;

namespace GamePackages
{
    // Player profile contract (name, avatar, frame) for features that must not depend on the UserProfile package; get it with ProfileManager.instance.Features.Get<IUserProfile>(), null when the package is absent.
    // Hợp đồng hồ sơ người chơi (tên, avatar, khung) cho các tính năng không được phụ thuộc package UserProfile; lấy bằng ProfileManager.instance.Features.Get<IUserProfile>(), null khi không có package.
    public interface IUserProfile : IFeature
    {
        // EN: Display name of the player.
        // VI: Tên hiển thị của người chơi.
        string Name { get; }

        // EN: Selected avatar index.
        // VI: Chỉ số avatar đã chọn.
        int AvatarId { get; }

        // EN: Selected frame index.
        // VI: Chỉ số khung đã chọn.
        int FrameId { get; }

        // EN: Changes and saves the name; empty or unchanged values are ignored.
        // VI: Đổi và lưu tên; giá trị rỗng hoặc không đổi bị bỏ qua.
        void SetName(string value);

        // EN: Changes and saves the avatar index (must be nonnegative).
        // VI: Đổi và lưu chỉ số avatar (không được âm).
        void SetAvatar(int value);

        // EN: Changes and saves the frame index (must be nonnegative).
        // VI: Đổi và lưu chỉ số khung (không được âm).
        void SetFrame(int value);

        // EN: Returns the avatar sprite at an index, or null when out of range.
        // VI: Trả về sprite avatar theo chỉ số, hoặc null khi ngoài phạm vi.
        Sprite GetAvatarSprite(int id);

        // EN: Returns the frame sprite at an index, or null when out of range.
        // VI: Trả về sprite khung theo chỉ số, hoặc null khi ngoài phạm vi.
        Sprite GetFrameSprite(int id);

        // EN: Returns a random avatar sprite (for bots), or null when there is none.
        // VI: Trả về một sprite avatar ngẫu nhiên (cho bot), hoặc null khi không có.
        Sprite GetRandomAvatarSprite();

        // EN: Returns a random frame sprite (for bots), or null when there is none.
        // VI: Trả về một sprite khung ngẫu nhiên (cho bot), hoặc null khi không có.
        Sprite GetRandomFrameSprite();
    }
}
