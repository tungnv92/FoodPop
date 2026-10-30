using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Displays one nonblocking short message with an unscaled slide/fade entrance and exit.
    // VI: Hiện một thông báo ngắn không chặn thao tác, có trượt và mờ khi hiện/ẩn bằng thời gian độc lập.
    public sealed class ShortMessageView : MonoBehaviour
    {
        [Tooltip("Nhóm hiển thị của thanh thông báo.")]
        [SerializeField] private CanvasGroup group;
        [Tooltip("Khung thông báo dùng để chạy hiệu ứng trượt.")]
        [SerializeField] private RectTransform card;
        [Tooltip("Nội dung thông báo ngắn.")]
        [SerializeField] private Text label;
        [Tooltip("Thời gian thông báo đứng yên trước khi tự ẩn, tính bằng giây.")]
        [SerializeField, Min(0f)] private float holdDuration = 1.5f;
        [Tooltip("Thời gian hiệu ứng hiện thông báo, tính bằng giây.")]
        [SerializeField, Min(0.01f)] private float showDuration = 0.18f;
        [Tooltip("Thời gian hiệu ứng ẩn thông báo, tính bằng giây.")]
        [SerializeField, Min(0.01f)] private float hideDuration = 0.2f;
        [Tooltip("Khoảng trượt theo chiều dọc khi hiện hoặc ẩn, tính bằng đơn vị UI.")]
        [SerializeField] private float slideDistance = 28f;
        [SerializeField, HideInInspector] private Vector2 restPosition;
        private float elapsed;
        private bool showing;

        public bool IsVisible => showing;

        // EN: Starts hidden and ensures the message never blocks gameplay pointer input.
        // VI: Bắt đầu ở trạng thái ẩn và bảo đảm thông báo không chặn thao tác gameplay.
        private void Awake()
        {
            HideImmediate();
            if (group != null)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        // EN: Replaces the current message and restarts its entrance rather than stacking repeated taps.
        // VI: Thay thông báo hiện tại và bắt đầu lại hiệu ứng hiện, không xếp chồng khi bấm lặp.
        public void Show(string message)
        {
            if (group == null || card == null || label == null || string.IsNullOrWhiteSpace(message))
                return;
            label.text = message;
            elapsed = 0f;
            showing = true;
            ApplyPose(0f, -slideDistance);
        }

        // EN: Animates entrance, hold and exit with unscaled time, then resets the hidden pose.
        // VI: Chạy hiện, đứng yên và ẩn bằng thời gian độc lập, rồi đặt lại tư thế ẩn.
        private void Update()
        {
            if (!showing)
                return;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < showDuration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / showDuration);
                ApplyPose(t, Mathf.Lerp(-slideDistance, 0f, t));
            }
            else if (elapsed < showDuration + holdDuration)
                ApplyPose(1f, 0f);
            else
            {
                float t = Mathf.Clamp01((elapsed - showDuration - holdDuration) / hideDuration);
                ApplyPose(1f - t, slideDistance * t * 0.5f);
                if (t >= 1f) HideImmediate();
            }
        }

        // EN: Applies message opacity and position without modifying the authored resting size.
        // VI: Áp dụng độ rõ và vị trí thông báo mà không thay đổi kích thước thiết kế.
        private void ApplyPose(float alpha, float offset)
        {
            group.alpha = alpha;
            card.anchoredPosition = restPosition + Vector2.up * offset;
        }

        // EN: Immediately dismisses feedback for scene reset or popup transitions.
        // VI: Bỏ thông báo ngay khi đặt lại scene hoặc chuyển popup.
        public void HideImmediate()
        {
            showing = false;
            elapsed = 0f;
            if (group != null) group.alpha = 0f;
            if (card != null) card.anchoredPosition = restPosition;
        }

        // EN: Clears transient message animation when its UI is disabled.
        // VI: Xóa hoạt cảnh thông báo tạm khi UI bị tắt.
        private void OnDisable()
        {
            HideImmediate();
        }

#if UNITY_EDITOR
        // EN: Assigns the authored message hierarchy and captures its resting anchor.
        // VI: Gán cây thông báo đã dựng và ghi nhớ vị trí neo lúc nghỉ.
        public void Configure(CanvasGroup canvasGroup, RectTransform messageCard, Text messageLabel)
        {
            group = canvasGroup;
            card = messageCard;
            label = messageLabel;
            restPosition = card.anchoredPosition;
            HideImmediate();
        }
#endif
    }
}
