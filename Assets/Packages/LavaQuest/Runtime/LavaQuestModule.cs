using UnityEngine;

namespace GamePackages
{
    // Module asset of LavaQuest; holds LavaQuestConfig and creates LavaQuestFeature.
    // Asset module của LavaQuest; giữ LavaQuestConfig và tạo LavaQuestFeature.
    [CreateAssetMenu(fileName = "LavaQuestModule", menuName = "Scriptable Objects/Feature Modules/LavaQuestModule")]
    public class LavaQuestModule : FeatureModule<LavaQuestFeature, LavaQuestData, LavaQuestConfig>
    {
    }
}
