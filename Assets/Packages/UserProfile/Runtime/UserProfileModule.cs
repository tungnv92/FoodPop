using UnityEngine;

namespace GamePackages
{
    // Module asset of the user profile; holds UserProfileConfig and creates UserProfileFeature.
    // Asset module của hồ sơ người chơi; giữ UserProfileConfig và tạo UserProfileFeature.
    [CreateAssetMenu(fileName = "UserProfileModule", menuName = "Scriptable Objects/Feature Modules/UserProfileModule")]
    public class UserProfileModule : FeatureModule<UserProfileFeature, UserProfileData, UserProfileConfig>
    {
    }
}
