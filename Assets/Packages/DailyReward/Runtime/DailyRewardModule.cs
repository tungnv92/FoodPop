using UnityEngine;

namespace GamePackages
{
    // Module asset of the daily reward; holds DailyRewardGlobalConfig and creates DailyRewardFeature.
    // Asset module của thưởng hằng ngày; giữ DailyRewardGlobalConfig và tạo DailyRewardFeature.
    [CreateAssetMenu(fileName = "DailyRewardModule", menuName = "Scriptable Objects/Feature Modules/DailyRewardModule")]
    public class DailyRewardModule : FeatureModule<DailyRewardFeature, DailyRewardData, DailyRewardGlobalConfig>
    {
    }
}
