using UnityEngine;

namespace GamePackages
{
    // Tuning of the lives system.
    // Thông số của hệ thống mạng chơi.
    [CreateAssetMenu(fileName = "HeartConfig", menuName = "Scriptable Objects/Heart/Heart Config")]
    public class HeartConfig : ScriptableObject
    {
        [Tooltip("Bật hoặc tắt tính năng.")]
        public bool isFeatureEnabled = true;
        [Tooltip("Số mạng tối đa khi có Master Pass.")]
        public int masterPassMaxHeart = 8;
        [Tooltip("Số phút để hồi một mạng.")]
        public int refillMinutesPerHeart = 30;
        [Tooltip("Giá coin để hồi đầy mạng.")]
        public int refillGoldCost = 900;
    }
}
