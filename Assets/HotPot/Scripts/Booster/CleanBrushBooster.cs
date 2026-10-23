using UnityEngine;

namespace FoodPop
{
    // EN: Configures food collection into the retractable holding tray; legacy broom fields remain for serialized compatibility.
    // VI: Cấu hình thu food vào khay giữ món kéo ra được; giữ field chổi cũ để tương thích dữ liệu đã lưu.
    public class CleanBrushBooster : Booster
    {
        public override BoosterType Type => BoosterType.CleanBrush;

        [Header("Thu món vào khay")]
        [Tooltip("Khoảng cách thời gian giữa hai food bắt đầu bay vào khay, tính bằng giây.")]
        [Min(0f)] public float collectInterval = 0.07f;
        [Tooltip("Thời gian food nhấc lên khỏi đĩa trước khi bay vào khay.")]
        [Min(0.01f)] public float liftDuration = 0.15f;
        [Tooltip("Vật liệu tạo viền sáng và nháy sáng food trước khi bay vào khay.")]
        public Material collectionHighlight;
        [Tooltip("Mức phóng nhẹ food trong nhịp chuẩn bị bay; kết thúc nhịp sẽ về kích thước gốc.")]
        [Range(0f, 0.3f)] public float collectPulse = 0.12f;
        [Tooltip("Thời gian đĩa rơi lại băng chuyền sau khi food bắt đầu bay vào khay, tính bằng giây.")]
        [Min(0.01f)] public float plateDropDuration = 0.2f;
        [Header("Hạt sáng thu món")]
        [Tooltip("Prefab vệt hạt sáng ngắn đi theo food khi bay vào khay.")]
        public ParticleEffect collectionTrail;
[Header("Hình ảnh + particle")]
        [Tooltip("Sprite cây chổi quét trên băng chuyền.")]
        public Sprite broomSprite;
        [Tooltip("Sprite bong bóng bọc quanh món.")]
        public Sprite bubbleSprite;
        [Tooltip("Prefab particle vệt bọt xà phòng phía sau chổi (lặp lại).")]
        public ParticleEffect foamTrail;
        [Tooltip("Prefab particle chùm bọt khi chổi xuất hiện, biến mất hoặc nhặt món.")]
        public ParticleEffect foamPuff;
        [Tooltip("Prefab particle bong bóng vỡ khi món rơi về board.")]
        public ParticleEffect bubblePop;
        [Tooltip("Thứ tự vẽ của hiệu ứng (cao hơn băng chuyền và board).")]
        public int sortingOrder = 500;

        [Header("Chổi")]
        [Tooltip("Chiều cao cây chổi (đơn vị world).")]
        [Min(0.1f)] public float broomSize = 1.1f;
        [Tooltip("Vị trí chổi so với slot đang quét (đơn vị world), để phần lông chổi chạm mặt băng.")]
        public Vector2 broomOffset = new Vector2(0.15f, 0.45f);
        [Tooltip("Thời gian (giây) chổi quét hết một vòng băng chuyền.")]
        [Min(0.2f)] public float sweepDuration = 1.5f;
        [Tooltip("Góc lắc qua lại của chổi (độ).")]
        [Min(0f)] public float wobbleAngle = 14f;
        [Tooltip("Tốc độ lắc của chổi (lần mỗi giây).")]
        [Min(0f)] public float wobbleSpeed = 2.3f;

        [Header("Bong bóng")]
        [Tooltip("Đường kính bong bóng (đơn vị world).")]
        [Min(0.1f)] public float bubbleSize = 0.95f;
        [Tooltip("Kích thước món bên trong bong bóng (đơn vị world).")]
        [Min(0.05f)] public float itemSize = 0.6f;
        [Tooltip("Độ cao (đơn vị world) món nổi lên khỏi băng khi vào bong bóng.")]
        [Min(0f)] public float liftHeight = 0.35f;
        [Tooltip("Thời gian (giây) bong bóng hình thành.")]
        [Min(0.01f)] public float bubbleFormDuration = 0.22f;
        [Tooltip("Thời gian (giây) bong bóng lơ lửng trước khi bay về board.")]
        [Min(0f)] public float hoverDuration = 0.25f;
        [Tooltip("Thời gian food bay vào khay giữ món, tính bằng giây.")]
        [Min(0.05f)] public float flyDuration = 0.5f;
        [Tooltip("Độ cao vòng cung khi food bay vào khay, tính bằng đơn vị world.")]
        [Min(0f)] public float flyArc = 1.2f;
    }
}
