using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: "New booster" popup shown when a booster unlocks: big icon over slowly spinning rays, the booster's name and description from BoosterConfig and a "Got it" button. Runs on unscaled time because the level is paused while it is open.
    // VI: Popup "booster mới" hiện khi một booster được mở khóa: icon lớn trên chùm tia sáng xoay chậm, tên và mô tả booster lấy từ BoosterConfig cùng nút "Got it". Chạy theo thời gian không phụ thuộc tốc độ game vì màn chơi tạm dừng khi popup mở.
    [DisallowMultipleComponent]
    public sealed class CanvasBoosterIntro : LevelPopupView
    {
        [Header("Nội dung")]
        [Tooltip("Text tên booster.")]
        [SerializeField] private Text boosterName;
        [Tooltip("Text mô tả tác dụng của booster.")]
        [SerializeField] private Text description;
        [Tooltip("Hình icon booster; cũng là điểm bắt đầu khi icon bay xuống nút.")]
        [SerializeField] private Image boosterIcon;

        [Header("Chuyển động")]
        [Tooltip("Chùm tia sáng xoay phía sau icon.")]
        [SerializeField] private RectTransform rays;
        [Tooltip("Tốc độ xoay của chùm tia sáng, tính bằng độ mỗi giây.")]
        [SerializeField] private float raySpeed = 36f;
        [Tooltip("Độ nhấp nhô của icon, tính bằng đơn vị canvas.")]
        [SerializeField] private float bobHeight = 12f;
        [Tooltip("Thời gian một chu kỳ nhấp nhô của icon, tính bằng giây.")]
        [SerializeField, Min(0.1f)] private float bobPeriod = 2.4f;

        [Header("Xuất hiện")]
        [Tooltip("Lớp nền tối phía sau khung; mờ dần vào trước khi khung bật ra.")]
        [SerializeField] private Graphic scrim;
        [Tooltip("Thời gian nền tối mờ dần vào, tính bằng giây.")]
        [SerializeField, Min(0.01f)] private float scrimFade = 0.3f;
        [Tooltip("Thời điểm khung bắt đầu bật ra, tính từ lúc mở popup (giây).")]
        [SerializeField, Min(0f)] private float cardDelay = 0.15f;
        [Tooltip("Thời gian khung phóng từ nhỏ lên đúng cỡ (có vượt nhẹ), tính bằng giây.")]
        [SerializeField, Min(0.01f)] private float cardDuration = 0.45f;
        [Tooltip("Thời điểm icon booster nảy ra, tính từ lúc mở popup (giây).")]
        [SerializeField, Min(0f)] private float iconDelay = 0.45f;
        [Tooltip("Thời gian icon nảy từ 0 lên đúng cỡ, tính bằng giây.")]
        [SerializeField, Min(0.01f)] private float iconDuration = 0.4f;

        private Vector2 iconRest;
        private bool hasIconRest;
        private Vector3 cardRest;
        private float scrimAlpha;
        private bool hasEntranceRest;

        // EN: Spins the rays and bobs the icon with unscaled time.
        // VI: Xoay chùm tia sáng và cho icon nhấp nhô theo thời gian không phụ thuộc tốc độ game.
        private void Update()
        {
            if (rays != null)
                rays.Rotate(0f, 0f, -raySpeed * Time.unscaledDeltaTime);
            if (boosterIcon != null && hasIconRest)
            {
                float wave = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / bobPeriod);
                boosterIcon.rectTransform.anchoredPosition = iconRest + Vector2.up * wave * bobHeight;
                boosterIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, wave * 2f);
            }
        }

        // EN: Fills the popup for one booster and opens it; gotIt runs when the player presses "Got it" (the caller closes the popup).
        // VI: Điền nội dung cho một booster rồi mở popup; gotIt chạy khi người chơi bấm "Got it" (nơi gọi tự đóng popup).
        public void Show(Sprite icon, string title, string info, UnityAction gotIt)
        {
            if (boosterName != null) boosterName.text = title;
            if (description != null) description.text = info;
            if (boosterIcon != null)
            {
                if (!hasIconRest)
                {
                    iconRest = boosterIcon.rectTransform.anchoredPosition;
                    hasIconRest = true;
                }
                boosterIcon.sprite = icon;
                boosterIcon.enabled = icon != null;
            }
            Open(gotIt, null, true, false);
            StartCoroutine(Entrance());
        }

        // EN: Screen-space centre and height (pixels) of the icon, so the flying icon can start exactly where the popup shows it.
        // VI: Tâm và chiều cao (pixel) của icon trên màn hình, để icon bay bắt đầu đúng chỗ popup đang hiện.
        public bool TryGetIconScreenRect(out Vector2 center, out float height)
        {
            center = default;
            height = 0f;
            if (boosterIcon == null || !boosterIcon.enabled)
                return false;
            var corners = new Vector3[4];
            boosterIcon.rectTransform.GetWorldCorners(corners);
            Camera eventCamera = Canvas != null && Canvas.renderMode != RenderMode.ScreenSpaceOverlay ? Canvas.worldCamera : null;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
            center = (min + max) * 0.5f;
            height = Mathf.Abs(max.y - min.y);
            return true;
        }

        // EN: Staged entrance on unscaled time: the scrim fades in, then the card grows with a slight overshoot, then the icon pops; "Got it" stays inactive until the entrance ends so an early tap cannot skip it.
        // VI: Xuất hiện theo từng bước bằng thời gian không phụ thuộc tốc độ game: nền tối mờ dần vào, rồi khung phóng to có vượt nhẹ, rồi icon nảy ra; "Got it" chỉ bấm được khi xuất hiện xong để không bị chạm sớm bỏ qua.
        private IEnumerator Entrance()
        {
            if (!hasEntranceRest)
            {
                cardRest = card.localScale;
                scrimAlpha = scrim != null ? scrim.color.a : 0f;
                hasEntranceRest = true;
            }
            canvasGroup.interactable = false;
            float end = Mathf.Max(scrimFade, cardDelay + cardDuration, iconDelay + iconDuration);
            for (float elapsed = 0f; ; elapsed += Time.unscaledDeltaTime)
            {
                if (scrim != null)
                {
                    Color color = scrim.color;
                    color.a = scrimAlpha * Mathf.Clamp01(elapsed / scrimFade);
                    scrim.color = color;
                }
                card.localScale = cardRest * BackOut(Mathf.Clamp01((elapsed - cardDelay) / cardDuration));
                if (boosterIcon != null)
                    boosterIcon.rectTransform.localScale = Vector3.one * BackOut(Mathf.Clamp01((elapsed - iconDelay) / iconDuration));
                if (elapsed >= end)
                    break;
                yield return null;
            }
            canvasGroup.interactable = true;
        }

        // EN: Ease-out with a small overshoot (0 → ~1.1 → 1).
        // VI: Giảm tốc có vượt nhẹ (0 → ~1.1 → 1).
        private static float BackOut(float t)
        {
            const float c = 1.70158f;
            float u = t - 1f;
            return 1f + (c + 1f) * u * u * u + c * u * u;
        }

        // EN: Hides the popup icon (the flying copy takes over) before the popup closes.
        // VI: Ẩn icon của popup (bản bay thay thế) trước khi popup đóng.
        public void HideIcon()
        {
            if (boosterIcon != null) boosterIcon.enabled = false;
        }
    }
}
