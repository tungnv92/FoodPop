using UnityEngine;

namespace GamePackages
{
    [CreateAssetMenu(fileName = "RecordTrackingConfig", menuName = "FoodPop/Record Tracking Config")]
    public sealed class RecordTrackingConfig : ScriptableObject
    {
        [Tooltip("Bật hoặc tắt tính năng.")]
        public bool isFeatureEnabled = true;
    }
}
