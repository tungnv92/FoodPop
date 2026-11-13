using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Binds gameplay actions to an authored popup prefab without rebuilding its editable hierarchy.
    // VI: Nối hành động gameplay với prefab popup đã dựng mà không tạo lại cấu trúc có thể chỉnh sửa.
    [DisallowMultipleComponent]
    public abstract class LevelPopupView : UICanvasPrime
    {
        [Tooltip("Nút chính: sang màn, chơi lại hoặc hồi sinh tùy prefab.")]
        [SerializeField] private Button primaryButton;
        [Tooltip("Nút phụ từ chối hồi sinh; để trống với popup thắng và thua.")]
        [SerializeField] private Button secondaryButton;
        [Tooltip("Dòng trạng thái được thay khi không thể thực hiện hành động chính.")]
        [SerializeField] private Text statusLabel;
        [Tooltip("Thông báo khi không còn màn kế tiếp hoặc chưa thể hồi sinh.")]
        [TextArea] [SerializeField] private string unavailableStatus;

        private UnityAction primaryAction;
        private UnityAction secondaryAction;
        private Coroutine entrance;
        private string availableStatus;
        private Vector3 restScale;
        private bool capturedDefaults;

        // EN: Opens the authored popup without gameplay callbacks for generic access.
        // VI: Mở popup đã thiết kế không kèm callback gameplay cho API tổng quát.
        public override void Open()
        {
            Open(null, null, true, true);
        }

        // EN: Opens a concrete popup while retaining authored text and scale and binding only this opening's actions.
        // VI: Mở popup cụ thể, giữ chữ và tỉ lệ đã thiết kế, chỉ nối hành động cho lần mở hiện tại.
        protected void Open(UnityAction primary, UnityAction secondary, bool available, bool animate)
        {
            if (!capturedDefaults)
            {
                availableStatus = statusLabel != null ? statusLabel.text : string.Empty;
                restScale = card != null ? card.localScale : Vector3.one;
                capturedDefaults = true;
            }
            ReleaseRuntimeState();
            base.Open();
            primaryAction = available ? primary : null;
            secondaryAction = secondary;
            if (primaryButton != null)
            {
                primaryButton.interactable = available;
                if (primaryAction != null) primaryButton.onClick.AddListener(primaryAction);
            }
            if (secondaryButton != null && secondaryAction != null)
                secondaryButton.onClick.AddListener(secondaryAction);
            if (statusLabel != null)
                statusLabel.text = available || string.IsNullOrEmpty(unavailableStatus) ? availableStatus : unavailableStatus;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (card != null) card.localScale = restScale;
            if (animate && Application.isPlaying && card != null && canvasGroup != null)
                entrance = StartCoroutine(AnimateEntrance());
        }

        // EN: Removes runtime callbacks owned by this view while preserving events assigned in the prefab Inspector.
        // VI: Gỡ callback lúc chạy do view quản lý, giữ nguyên sự kiện được gán trong Inspector của prefab.
        private void ClearActions()
        {
            if (primaryButton != null && primaryAction != null) primaryButton.onClick.RemoveListener(primaryAction);
            if (secondaryButton != null && secondaryAction != null) secondaryButton.onClick.RemoveListener(secondaryAction);
            primaryAction = null;
            secondaryAction = null;
        }

        // EN: Animates the authored card with unscaled time so the popup works while the level is paused.
        // VI: Chạy hoạt cảnh khung đã dựng bằng thời gian độc lập để popup hoạt động khi màn đang dừng.
        private IEnumerator AnimateEntrance()
        {
            float elapsed = 0f;
            while (elapsed < 0.34f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / 0.34f);
                float back = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
                card.localScale = restScale * Mathf.LerpUnclamped(0.88f, 1f, back);
                canvasGroup.alpha = Mathf.Clamp01(t * 2f);
                yield return null;
            }
            card.localScale = restScale;
            canvasGroup.alpha = 1f;
            entrance = null;
        }

        // EN: Releases animation and callbacks before hiding, including during Edit Mode previews.
        // VI: Giải phóng hoạt cảnh và callback trước khi ẩn, kể cả lúc xem thử trong chế độ chỉnh sửa.
        public void Hide()
        {
            Close();
        }

        // EN: Cleans popup callbacks and animation before the common canvas close lifecycle.
        // VI: Dọn callback và hoạt cảnh popup trước vòng đời đóng canvas chung.
        public override void Close()
        {
            ReleaseRuntimeState();
            base.Close();
        }

        // EN: Clears transient animation and event ownership independently of Unity lifecycle callback timing.
        // VI: Xóa trạng thái hoạt cảnh và quyền giữ sự kiện tạm, không phụ thuộc thời điểm callback vòng đời Unity.
        private void ReleaseRuntimeState()
        {
            if (entrance != null) StopCoroutine(entrance);
            entrance = null;
            ClearActions();
        }

        // EN: Releases common popup state on disable; specialized canvases can extend cleanup for their subscriptions.
        // VI: Giải phóng trạng thái popup chung khi tắt; canvas cụ thể có thể bổ sung dọn đăng ký sự kiện.
        protected virtual void OnDisable()
        {
            ReleaseRuntimeState();
        }
    }
}
