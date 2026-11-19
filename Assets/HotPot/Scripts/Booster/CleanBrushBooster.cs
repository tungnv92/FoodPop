using UnityEngine;

namespace FoodPop
{
    // EN: Configures food collection from the conveyor into the holding bag.
    // VI: Cấu hình thu food từ băng chuyền vào túi giữ món.
    public class CleanBrushBooster : Booster
    {
        public override BoosterType Type => BoosterType.CleanBrush;

        [Header("Thu món vào túi")]
        [Tooltip("Số slot tối đa trên băng chuyền mà một lần dùng Clean dọn đi: ô tủ còn trống được lấp trước (ưu tiên món thừa, thiếu thì món đang cần), phần còn lại về board (món thừa trước). Hồi sinh luôn dọn sạch băng.")]
        [Min(1)] public int maxClearSlots = 4;
        [Tooltip("Khoảng cách thời gian giữa hai food bắt đầu bay vào túi, tính bằng giây.")]
        [Min(0f)] public float collectInterval = 0.07f;
        [Tooltip("Thời gian food nhấc lên khỏi đĩa trước khi bay vào túi.")]
        [Min(0.01f)] public float liftDuration = 0.15f;
        [Tooltip("Độ cao (đơn vị world) đĩa và food nhấc lên khỏi băng trước khi bay vào túi.")]
        [Min(0f)] public float liftHeight = 0.35f;
        [Tooltip("Vật liệu tạo viền sáng và nháy sáng food trước khi bay vào túi.")]
        public Material collectionHighlight;
        [Tooltip("Mức phóng nhẹ food trong nhịp chuẩn bị bay; kết thúc nhịp sẽ về kích thước gốc.")]
        [Range(0f, 0.3f)] public float collectPulse = 0.12f;
        [Tooltip("Thời gian đĩa rơi lại băng chuyền sau khi food bắt đầu bay vào túi, tính bằng giây.")]
        [Min(0.01f)] public float plateDropDuration = 0.2f;
        [Tooltip("Thời gian food bay vào túi giữ món, tính bằng giây.")]
        [Min(0.05f)] public float flyDuration = 0.5f;
        [Tooltip("Độ cao vòng cung khi food bay vào túi, tính bằng đơn vị world.")]
        [Min(0f)] public float flyArc = 1.2f;

        [Header("Hiệu ứng")]
        [Tooltip("Prefab vệt hạt sáng ngắn đi theo food khi bay vào túi.")]
        public ParticleEffect collectionTrail;
        [Tooltip("Thứ tự vẽ của food bay và vệt sáng (cao hơn băng chuyền và board).")]
        public int sortingOrder = 500;
    }
}
