using UnityEngine;

namespace GamePackages
{
    // Module asset of the lives system; holds HeartConfig and creates HeartFeature.
    // Asset module của hệ thống mạng; giữ HeartConfig và tạo HeartFeature.
    [CreateAssetMenu(fileName = "HeartModule", menuName = "Scriptable Objects/Feature Modules/HeartModule")]
    public class HeartModule : FeatureModule<HeartFeature, HeartData, HeartConfig>
    {
    }
}
