using UnityEngine;

namespace FoodPop
{
    // EN: Shuffle booster: every dish lights up, the open dishes close their lids, the lids shake with a rainbow shimmer, the ingredients swap places (each board position keeps its own hidden/frozen state), then the lids of the open positions open again. Holds the effect tuning; BoosterController runs the logic.
    // VI: Booster xáo trộn: mọi đĩa sáng lên, các đĩa đang mở đóng nắp, nắp rung kèm ánh cầu vồng, nguyên liệu đổi chỗ cho nhau (mỗi vị trí giữ nguyên trạng thái ẩn/đóng băng của mình) rồi các vị trí đang mở sẽ mở nắp lại. Giữ thông số hiệu ứng; BoosterController chạy logic.
    public class ShuffleBooster : Booster
    {
        public override BoosterType Type => BoosterType.Shuffle;

        [Header("Sáng lên")]
        [Tooltip("Thời gian (giây) các đĩa sáng lên và phồng nhẹ.")]
        [Min(0f)] public float glowDuration = 0.2f;
        [Tooltip("Độ phồng của đĩa khi sáng lên (1.08 = to hơn 8%).")]
        [Min(1f)] public float glowScale = 1.08f;

        [Header("Đóng nắp")]
        [Tooltip("Thời gian (giây) nắp rơi xuống đóng lại.")]
        [Min(0f)] public float lidCloseDuration = 0.22f;
        [Tooltip("Độ cao (đơn vị world) nắp bắt đầu rơi xuống.")]
        [Min(0f)] public float lidDropHeight = 0.35f;

        [Header("Rung nắp + cầu vồng")]
        [Tooltip("Material cầu vồng gắn lên nắp trong lúc rung và đổi chỗ (shader FoodPop/SpriteRainbow).")]
        public Material rainbowMaterial;
        [Tooltip("Thời gian (giây) nắp rung trước khi đổi chỗ.")]
        [Min(0f)] public float shakeDuration = 0.5f;
        [Tooltip("Góc rung tối đa của nắp (độ).")]
        [Min(0f)] public float shakeAngle = 7f;
        [Tooltip("Số lần rung mỗi giây.")]
        [Min(0f)] public float shakeFrequency = 9f;

        [Header("Đổi chỗ")]
        [Tooltip("Thời gian (giây) các đĩa bay đổi chỗ cho nhau.")]
        [Min(0.05f)] public float swapDuration = 0.45f;
        [Tooltip("Độ cao vòng cung (đơn vị world) khi đĩa bay đổi chỗ.")]
        [Min(0f)] public float swapArcHeight = 0.5f;

        [Header("Mở nắp")]
        [Tooltip("Thời gian (giây) nắp mờ dần khi mở lại.")]
        [Min(0f)] public float lidOpenDuration = 0.3f;
        [Tooltip("Độ cao (đơn vị world) nắp được nhấc lên khi mở.")]
        [Min(0f)] public float lidOpenLift = 0.25f;
        [Tooltip("Tỉ lệ phóng to của nắp khi mở.")]
        [Min(1f)] public float lidOpenScale = 1.15f;
    }
}
