using HomeFeatures.Profile;
using UnityEngine;

namespace GamePackages
{
    // Config of the user profile feature: the avatar and frame sprite lists.
    // Config của tính năng hồ sơ người chơi: danh sách sprite avatar và khung.
    [CreateAssetMenu(fileName = "UserProfileConfig", menuName = "Scriptable Objects/Profile/User Profile Config")]
    public class UserProfileConfig : ScriptableObject
    {
        [Tooltip("Bật hoặc tắt tính năng.")]
        public bool isFeatureEnabled = true;
        [Tooltip("Danh sách sprite avatar.")]
        public AvatarConfig avatarConfig;
        [Tooltip("Danh sách sprite khung avatar.")]
        public FrameConfig frameConfig;
    }
}
