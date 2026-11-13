using TMPro;
using UnityEngine;

namespace FoodPop
{
    // EN: PickupHand booster: hides the booster root, shows a "pick a dish" prompt with a cancel button and lights up (white outline) every dish that can be taken (not frozen and the conveyor has enough free slots for all of its portions). Tapping one lifts it (opening its lid if hidden) and sends it to the conveyor like a normal pick. Holds the effect tuning and the prompt references; BoosterController runs the logic.
    // VI: Booster PickupHand: ẩn root booster, hiện thông báo "chọn đĩa" kèm nút hủy và làm sáng (viền trắng) mọi đĩa lấy được (không đóng băng và băng chuyền đủ slot trống cho mọi phần của đĩa). Chạm một đĩa thì đĩa được nhấc lên (mở nắp nếu đĩa đang ẩn) rồi đưa lên băng chuyền như chọn thông thường. Giữ thông số hiệu ứng và tham chiếu thông báo; BoosterController chạy logic.
    public class PickupHandBooster : Booster
    {
        public override BoosterType Type => BoosterType.PickupHand;

        // EN: Object holding the prompt (used to measure how much of the screen bottom it covers).
        // VI: Object chứa thông báo (dùng để đo phần đáy màn hình bị thông báo che).
        public GameObject PromptRoot => promptRoot;

        [Header("Thông báo chọn đĩa")]
        [Tooltip("Object chứa thông báo và nút hủy; hiện khi đang chọn đĩa. Không được đặt bên trong root booster.")]
        [SerializeField] private GameObject promptRoot;
        [Tooltip("Text hiển thị thông báo.")]
        [SerializeField] private TMP_Text promptText;
        [Tooltip("Sprite nút hủy, cũng là vùng chạm để hủy chọn.")]
        [SerializeField] private SpriteRenderer cancelButton;
        [Tooltip("Vùng chạm của nút hủy; để trống sẽ tự lấy BoxCollider2D trên nút hủy, không có thì tự thêm.")]
        [SerializeField] private BoxCollider2D cancelCollider;

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
        [Min(1)] public int maxVisibleRows = 7;
        [Tooltip("Thời gian (giây) camera trượt xuống khi vào chế độ chọn và trượt về khi xong.")]
        [Min(0f)] public float cameraMoveDuration = 0.35f;
        [Tooltip("Khoảng trống (đơn vị world) giữa hàng thấp nhất được hiện và mép trên của thông báo chọn đĩa.")]
        [Min(0f)] public float boardBottomPadding = 0.2f;

        // EN: Shows or hides the "pick a dish" prompt (text from the BoosterConfig PickupPrompt short message) and its cancel button.
        // VI: Hiện hoặc ẩn thông báo "chọn đĩa" (nội dung lấy từ thông báo ngắn PickupPrompt trong BoosterConfig) cùng nút hủy.
        public void ShowPrompt(bool visible)
        {
            if (promptText != null && visible)
                promptText.text = IngameConfig.Instance.Boosters.GetShortDes(BoosterConfig.ShortDesType.PickupPrompt);
            if (promptRoot != null)
                promptRoot.SetActive(visible);
        }

        // EN: True when the world point is inside the cancel button's BoxCollider2D while the button is shown.
        // VI: True khi điểm world nằm trong BoxCollider2D của nút hủy lúc nút đang hiện.
        public bool CancelContains(Vector2 worldPoint)
        {
            BoxCollider2D area = CancelArea;
            return area != null && area.enabled && area.gameObject.activeInHierarchy && area.OverlapPoint(worldPoint);
        }

        // EN: The cancel button's click area: the assigned collider, otherwise the BoxCollider2D on the cancel button, added as a trigger (sized to the sprite) when missing.
        // VI: Vùng chạm của nút hủy: collider đã gán, nếu không thì BoxCollider2D trên nút hủy, tự thêm dạng trigger (khớp cỡ sprite) khi chưa có.
        private BoxCollider2D CancelArea
        {
            get
            {
                if (cancelCollider == null && cancelButton != null)
                {
                    cancelCollider = cancelButton.GetComponent<BoxCollider2D>();
                    if (cancelCollider == null)
                    {
                        cancelCollider = cancelButton.gameObject.AddComponent<BoxCollider2D>();
                        cancelCollider.isTrigger = true;
                    }
                }
                return cancelCollider;
            }
        }
    }
}
