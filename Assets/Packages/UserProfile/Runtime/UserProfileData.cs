namespace GamePackages
{
    // Saved player profile: display name, avatar index and frame index.
    // Hồ sơ người chơi được lưu: tên hiển thị, chỉ số avatar và chỉ số khung.
    [System.Serializable]
    public class UserProfileData
    {
        public string playerName;
        public int avatarId;
        public int frameId;
    }
}
