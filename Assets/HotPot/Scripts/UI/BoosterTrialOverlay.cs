using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Full-screen overlay of the booster free try, on its own Screen Space Overlay canvas drawn above every other UI. Four black panels plus a square with a round hole always cover the whole screen except a circle fitted to the round button background; the Spine hand (scaled with the hole) and the "Tap to try!" text sit around the hole. Re-placed every frame so it stays on the button.
    // VI: Lớp phủ toàn màn hình của lượt dùng thử booster, nằm trên canvas Screen Space Overlay riêng vẽ trên mọi UI khác. Bốn tấm đen cùng một ô vuông có lỗ tròn luôn che kín toàn màn hình, chỉ chừa một vòng tròn khớp nền tròn của nút booster; bàn tay Spine (co giãn theo lỗ) và chữ "Tap to try!" nằm quanh lỗ. Được đặt lại mỗi khung hình để luôn bám đúng nút.
    [DisallowMultipleComponent]
    public sealed class BoosterTrialOverlay : MonoBehaviour
    {
        // EN: Radius of the clear circle in FocusHole.png as a fraction of half the sprite (middle of its soft edge).
        // VI: Bán kính vùng trong suốt của FocusHole.png tính theo nửa cạnh sprite (giữa mép mờ).
        private const float HoleRadiusRatio = 0.94f;

        [Header("Lớp tối")]
        [Tooltip("Tấm tối phía trên lỗ sáng (kéo tới mép trên màn hình).")]
        [SerializeField] private RectTransform dimTop;
        [Tooltip("Tấm tối phía dưới lỗ sáng (kéo tới mép dưới màn hình).")]
        [SerializeField] private RectTransform dimBottom;
        [Tooltip("Tấm tối bên trái lỗ sáng.")]
        [SerializeField] private RectTransform dimLeft;
        [Tooltip("Tấm tối bên phải lỗ sáng.")]
        [SerializeField] private RectTransform dimRight;
        [Tooltip("Ô vuông chứa lỗ sáng tròn; tự đặt đúng vị trí và kích thước để lỗ khớp nền tròn của nút booster.")]
        [SerializeField] private RectTransform focus;
        [Tooltip("Image lỗ sáng nằm trong Focus (sprite FocusHole: ô vuông có tâm tròn trong suốt).")]
        [SerializeField] private Image hole;
        [Tooltip("Màu chung của bốn tấm tối và lỗ sáng; gán lúc mở lớp phủ để các mảnh luôn cùng màu.")]
        [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.78f);
        [Tooltip("Đường kính lỗ sáng so với đường kính nền tròn của nút (1 = khít nền, 1.06 = rộng hơn 6% để thấy trọn viền).")]
        [SerializeField, Min(0.5f)] private float holePadding = 1.06f;

        [Header("Hướng dẫn")]
        [Tooltip("Nhóm chứa bàn tay; được thiết kế cho lỗ sáng cỡ guideReferenceSize và tự co giãn theo lỗ sáng thật.")]
        [SerializeField] private RectTransform guide;
        [Tooltip("Cỡ lỗ sáng (đơn vị canvas) mà nhóm bàn tay được thiết kế theo.")]
        [SerializeField, Min(1f)] private float guideReferenceSize = 240f;
        [Tooltip("Bật để bàn tay co giãn theo lỗ sáng; tắt để giữ kích thước cố định giữa các mục tiêu.")]
        [SerializeField] private bool scaleGuideWithFocus = true;
        [Tooltip("Text dòng nhắc phía trên lỗ sáng.")]
        [SerializeField] private Text tip;
        [Tooltip("Chữ hướng dẫn TextMeshPro khi prefab sử dụng TMP.")]
        [SerializeField] private TMP_Text promptLabel;

        private Camera worldCamera;
        private Vector3 targetCenter;
        private float targetRadius;

        // EN: Gives the four panels and the hole the same dim colour so no seam shows between them.
        // VI: Tô cùng một màu tối cho bốn tấm và lỗ sáng để không lộ đường nối giữa chúng.
        private void Awake()
        {
            hole.color = dimColor;
            foreach (RectTransform panel in new[] { dimTop, dimBottom, dimLeft, dimRight })
                panel.GetComponent<Graphic>().color = dimColor;
        }

        // EN: Keeps the hole on the button every frame (screen size or camera may change).
        // VI: Giữ lỗ sáng đúng nút mỗi khung hình (kích thước màn hình hoặc camera có thể đổi).
        private void LateUpdate()
        {
            Layout();
        }

        // EN: Targets the supplied world bounds and updates the authored legacy or TextMeshPro prompt.
        // VI: Đặt mục tiêu theo bao world được truyền và cập nhật chữ hướng dẫn thường hoặc TextMeshPro đã gán.
        public void Show(Bounds background, Camera cam, string message)
        {
            targetRadius = background.extents.x;
            targetCenter = new Vector3(background.center.x, background.max.y - targetRadius, background.center.z);
            worldCamera = cam;
            if (tip != null)
                tip.text = message;
            if (promptLabel != null)
                promptLabel.text = message;
            Layout();
        }

        // EN: Fits the hole and dim panels to the target, optionally scaling the hand with the hole or keeping its authored size.
        // VI: Đặt lỗ sáng và nền tối khớp mục tiêu, tùy chọn co bàn tay theo lỗ hoặc giữ kích thước thiết kế.
        private void Layout()
        {
            if (worldCamera == null)
                return;
            Vector2 center = worldCamera.WorldToScreenPoint(targetCenter);
            Vector2 edge = worldCamera.WorldToScreenPoint(targetCenter + Vector3.right * targetRadius);
            float half = Mathf.Abs(edge.x - center.x) * holePadding / HoleRadiusRatio;
            var screen = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            Vector2 lo = (center - new Vector2(half, half)) / screen;
            Vector2 hi = (center + new Vector2(half, half)) / screen;
            SetAnchors(focus, lo, hi);

            float x0 = Mathf.Clamp01(lo.x), x1 = Mathf.Clamp01(hi.x);
            float y0 = Mathf.Clamp01(lo.y), y1 = Mathf.Clamp01(hi.y);
            SetAnchors(dimTop, new Vector2(0f, y1), Vector2.one);
            SetAnchors(dimBottom, Vector2.zero, new Vector2(1f, y0));
            SetAnchors(dimLeft, new Vector2(0f, y0), new Vector2(x0, y1));
            SetAnchors(dimRight, new Vector2(x1, y0), new Vector2(1f, y1));

            if (guide != null && focus != null)
                guide.localScale = Vector3.one * (scaleGuideWithFocus ? focus.rect.width / guideReferenceSize : 1f);
        }

        // EN: Stretches a rect exactly over the given normalized anchors.
        // VI: Kéo một rect phủ đúng vùng anchor chuẩn hóa đã cho.
        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null)
                return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
