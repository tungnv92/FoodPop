using UnityEngine;

namespace FoodPop
{
    // EN: PickupHand booster: hides the booster root, shows a "pick a dish" prompt with a cancel button and lights up (white outline) every dish that can be taken (not frozen and the conveyor has enough free slots for all of its portions). Tapping one lifts it (opening its lid if hidden) and sends it to the conveyor like a normal pick. Holds the effect tuning and a reference to its PickupPrompt (which owns the prompt text and cancel button); BoosterController runs the logic.
    // VI: Booster PickupHand: ẩn root booster, hiện thông báo "chọn đĩa" kèm nút hủy và làm sáng (viền trắng) mọi đĩa lấy được (không đóng băng và băng chuyền đủ slot trống cho mọi phần của đĩa). Chạm một đĩa thì đĩa được nhấc lên (mở nắp nếu đĩa đang ẩn) rồi đưa lên băng chuyền như chọn thông thường. Giữ thông số hiệu ứng và tham chiếu tới PickupPrompt (tự quản lý chữ thông báo và nút hủy); BoosterController chạy logic.
    public class PickupHandBooster : Booster
    {
        public override BoosterType Type => BoosterType.PickupHand;

        // EN: The "pick a dish" prompt with its cancel button, shown while selecting.
        // VI: Thông báo "chọn đĩa" kèm nút hủy, hiện khi đang chọn đĩa.
        public PickupPrompt Prompt => prompt;

        // EN: Bottom bar reserved for a banner ad, shown while Pickup is in use; selectable rows sit above its top edge.
        // VI: Thanh dưới đáy dành chỗ cho quảng cáo banner, hiện khi đang dùng Pickup; các hàng được chọn nằm phía trên mép trên của thanh.
        public GameObject BottomBanner => bottomBanner;

        [Header("Thông báo chọn đĩa")]
        [Tooltip("Thông báo chọn đĩa kèm nút hủy; hiện khi đang chọn đĩa. Không được đặt bên trong root booster.")]
        [SerializeField] private PickupPrompt prompt;
        [Tooltip("Thanh dưới đáy màn hình dành chỗ cho quảng cáo banner; hiện khi đang dùng Pickup và ẩn khi camera trượt về. Các hàng được chọn nằm phía trên mép trên của thanh này.")]
        [SerializeField] private GameObject bottomBanner;

        [Header("Nhấc đĩa")]
        [Tooltip("Thứ tự vẽ cộng thêm để đĩa đang nhấc nằm trên các đĩa khác.")]
        public int sortingBoost = 40;
        [Tooltip("Độ cao (đơn vị world) đĩa được nhấc lên trước khi bay lên băng.")]
        [Min(0f)] public float liftHeight = 0.35f;
        [Tooltip("Thời gian (giây) nhấc đĩa lên.")]
        [Min(0.01f)] public float liftDuration = 0.2f;
        [Tooltip("Độ phồng của đĩa khi được nhấc (1.08 = to hơn 8%).")]
        [Min(1f)] public float liftScale = 1.08f;
        [Tooltip("Thời gian (giây) mở nắp đĩa đang ẩn trong lúc nhấc.")]
        [Min(0f)] public float revealDuration = 0.2f;

        [Header("Camera khi chọn đĩa")]
        [Tooltip("Số hàng board tối đa được kéo vào màn hình khi đang chọn đĩa; board ít hàng hơn thì chỉ kéo đủ số hàng đang có.")]
        [Min(1)] public int maxVisibleRows = 6;
        [Tooltip("Thời gian (giây) camera trượt xuống khi vào chế độ chọn và trượt về khi xong.")]
        [Min(0f)] public float cameraMoveDuration = 0.35f;
        [Tooltip("Khoảng trống (đơn vị world) giữa hàng thấp nhất được hiện và mép trên của thanh banner (hoặc mép dưới màn hình nếu không có banner).")]
        [Min(0f)] public float boardBottomPadding = 0.2f;
    }
}
