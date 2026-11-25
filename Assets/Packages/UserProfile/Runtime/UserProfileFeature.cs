using UnityEngine;

namespace GamePackages
{
    // User profile (name, avatar, frame). Other features use it only through IUserProfile.
    // Hồ sơ người chơi (tên, avatar, khung). Tính năng khác chỉ dùng qua IUserProfile.
    public class UserProfileFeature : FeatureManager<UserProfileData, UserProfileConfig>, IUserProfile
    {
        public static UserProfileFeature Instance => ProfileManager.instance.Features.Get<UserProfileFeature>();
        public string Name => Data.playerName;
        public int AvatarId => Data.avatarId;
        public int FrameId => Data.frameId;

        // EN: Returns the feature enable switch from its configuration.
        // VI: Trả trạng thái bật tính năng từ config.
        public override bool IsEnabledFeature()
        {
            return Config.isFeatureEnabled;
        }

        // EN: Changes and saves the name; empty or unchanged values are ignored.
        // VI: Đổi và lưu tên; giá trị rỗng hoặc không đổi bị bỏ qua.
        public void SetName(string value)
        {
            if (string.IsNullOrEmpty(value) || Data.playerName == value) return;
            Data.playerName = value;
            Save();
        }

        // EN: Changes and saves the avatar index (must be nonnegative).
        // VI: Đổi và lưu chỉ số avatar (không được âm).
        public void SetAvatar(int value)
        {
            if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
            if (Data.avatarId == value) return;
            Data.avatarId = value;
            Save();
        }

        // EN: Changes and saves the frame index (must be nonnegative).
        // VI: Đổi và lưu chỉ số khung (không được âm).
        public void SetFrame(int value)
        {
            if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
            if (Data.frameId == value) return;
            Data.frameId = value;
            Save();
        }

        // EN: Returns the avatar sprite at an index, or null when out of range.
        // VI: Trả về sprite avatar theo chỉ số, hoặc null khi ngoài phạm vi.
        public Sprite GetAvatarSprite(int id)
        {
            return GetSprite(Config.avatarConfig.AvatarSprites, id);
        }

        // EN: Returns the frame sprite at an index, or null when out of range.
        // VI: Trả về sprite khung theo chỉ số, hoặc null khi ngoài phạm vi.
        public Sprite GetFrameSprite(int id)
        {
            return GetSprite(Config.frameConfig.FrameSprites, id);
        }

        // EN: Returns a random avatar sprite (for bots), or null when there is none.
        // VI: Trả về một sprite avatar ngẫu nhiên (cho bot), hoặc null khi không có.
        public Sprite GetRandomAvatarSprite()
        {
            Sprite[] sprites = Config.avatarConfig.AvatarSprites;
            return GetSprite(sprites, Random.Range(0, sprites.Length));
        }

        // EN: Returns a random frame sprite (for bots), or null when there is none.
        // VI: Trả về một sprite khung ngẫu nhiên (cho bot), hoặc null khi không có.
        public Sprite GetRandomFrameSprite()
        {
            Sprite[] sprites = Config.frameConfig.FrameSprites;
            return GetSprite(sprites, Random.Range(0, sprites.Length));
        }

        // EN: Gives a profile without a name the default "User" + device id.
        // VI: Đặt tên mặc định "User" + device id cho hồ sơ chưa có tên.
        protected override void OnInit()
        {
            if (string.IsNullOrEmpty(Data.playerName))
                Data.playerName = "User" + SystemInfo.deviceUniqueIdentifier;
        }

        // EN: Returns sprites[id], or null when the index is outside the array.
        // VI: Trả về sprites[id], hoặc null khi chỉ số nằm ngoài mảng.
        private static Sprite GetSprite(Sprite[] sprites, int id)
        {
            return id >= 0 && id < sprites.Length ? sprites[id] : null;
        }
    }
}
