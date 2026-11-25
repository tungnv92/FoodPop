using UnityEngine;

namespace GamePackages
{
    // Module asset of the daily login gift; holds DailyGiftRewardConfig and creates DailyGiftFeature.
    // Asset module của quà đăng nhập hằng ngày; giữ DailyGiftRewardConfig và tạo DailyGiftFeature.
    [CreateAssetMenu(fileName = "DailyGiftModule", menuName = "Scriptable Objects/Feature Modules/DailyGiftModule")]
    public class DailyGiftModule : FeatureModule<DailyGiftFeature, DailyGiftData, DailyGiftRewardConfig>
    {
    }
}
