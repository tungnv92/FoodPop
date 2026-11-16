using DG.Tweening;
using GamePackages;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.Events;

namespace FoodPop
{
    public class UICanvasPrime : MonoBehaviour
    {
        // EN: Exposes the authored canvas without searching the hierarchy.
        // VI: Cung cấp canvas đã thiết kế mà không tìm kiếm cấu trúc con.
        public Canvas Canvas => popupCanvas;

        [Tooltip("Canvas của prefab giao diện.")]
        [SerializeField] private Canvas popupCanvas;
        [Tooltip("Nhóm hiển thị và tương tác của giao diện.")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [Tooltip("Khung nội dung dùng để chạy hiệu ứng xuất hiện.")]
        [SerializeField] protected RectTransform card;
        [Tooltip("Hủy instance khi đóng thay vì giữ để dùng lại.")]
        [SerializeField] protected bool isDestroyOnClose;
        [Tooltip("Thời gian phóng và hiện popup.")]
        [SerializeField, Min(0.01f)] private float openDuration = 0.24f;
        [Tooltip("Thời gian thu và mờ popup.")]
        [SerializeField, Min(0.01f)] private float closeDuration = 0.16f;

        public UnityAction onCloseAction;
        internal UIManager Owner;
        protected PlayerDataManager playerData;
        protected bool closing;
        private IngameSoundManager soundManager;
        private Sequence popupTween;
        private Vector3 restScale;
        private bool capturedScale;
        private bool ownsPause;
        private float previousTimeScale;

        // EN: Reports whether this canvas is active.
        // VI: Cho biết canvas này có đang được bật không.
        public virtual bool IsOpening()
        {
            return gameObject.activeSelf;
        }

        // EN: Captures the authored scale, activates the canvas and plays its opening sound.
        // VI: Ghi tỉ lệ thiết kế, bật canvas và phát âm thanh mở.
        public virtual void Open()
        {
            CapturePopupScale();
            gameObject.SetActive(true);
            OnOpenCanvas();
            PlayUISound(IngameSoundManager.Event.PopupShow);
        }

        // EN: Plays an immediate-close sound when needed, cancels transitions and notifies the manager before optional destruction.
        // VI: Phát âm đóng ngay nếu cần, hủy chuyển động và báo manager trước khi tùy chọn hủy instance.
        public virtual void Close()
        {
            if (!closing && gameObject.activeSelf) PlayUISound(IngameSoundManager.Event.PopupHide);
            ResetAnimation();
            UnityAction callback = onCloseAction;
            onCloseAction = null;
            gameObject.SetActive(false);
            OnCloseCanvas();
            if (Owner != null) Owner.OnCanvasClosed(this, isDestroyOnClose);
            callback?.Invoke();
            if (!isDestroyOnClose) return;
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        // EN: Captures the original scale once without requiring a card on canvases that have no scale animation.
        // VI: Ghi tỉ lệ gốc một lần, không bắt buộc card trên canvas không chạy hiệu ứng scale.
        private void CapturePopupScale()
        {
            if (capturedScale || card == null) return;
            restScale = card.localScale;
            capturedScale = true;
        }

        // EN: Enables interaction when the canvas opens.
        // VI: Cho phép tương tác khi canvas mở.
        protected virtual void OnOpenCanvas()
        {
            if (canvasGroup != null) canvasGroup.interactable = true;
        }

        // EN: Cancels tween callbacks and restores the authored scale and full opacity.
        // VI: Hủy callback tween và trả tỉ lệ đã thiết kế cùng độ rõ đầy đủ.
        protected void ResetAnimation()
        {
            popupTween?.Kill();
            popupTween = null;
            closing = false;
            if (capturedScale && card != null) card.localScale = restScale;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        // EN: Disables interaction after closing.
        // VI: Tắt tương tác sau khi đóng.
        protected virtual void OnCloseCanvas()
        {
            if (canvasGroup != null) canvasGroup.interactable = false;
        }

        // EN: Caches shared feedback managers from the concrete canvas's runtime OnEnable.
        // VI: Cache manager phản hồi dùng chung từ OnEnable runtime của canvas cụ thể.
        protected void CacheFeedbackManagers()
        {
            playerData = ProfileManager.instance.PlayerDataManager;
            soundManager = IngameSoundManager.Instance;
        }

        // EN: Plays click haptics and sound using the cached player preferences.
        // VI: Phát rung và âm thanh nhấn theo cài đặt người chơi đã cache.
        protected void PlayClickFeedback()
        {
            PlayHaptic();
            PlaySoundClick();
        }

        // EN: Plays haptics only when enabled by the cached player settings.
        // VI: Chỉ phát rung khi cài đặt người chơi đã cache cho phép.
        protected void PlayHaptic()
        {
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
        }

        // EN: Plays UI click sound through the shared effects source when enabled.
        // VI: Phát tiếng nhấn UI qua nguồn hiệu ứng chung khi được bật.
        protected void PlaySoundClick()
        {
            if (playerData.IsSoundOn) soundManager.PlayClick();
        }

        // EN: Pauses only running gameplay and retains the original speed once.
        // VI: Chỉ tạm dừng gameplay đang chạy và giữ tốc độ gốc một lần.
        protected void PauseGameplay()
        {
            if (!Application.isPlaying || Time.timeScale <= 0f) return;
            previousTimeScale = Time.timeScale;
            ownsPause = true;
            Time.timeScale = 0f;
        }

        // EN: Releases only the pause owned by this canvas, including on external disable.
        // VI: Chỉ nhả pause do canvas này giữ, kể cả khi bị tắt từ bên ngoài.
        protected void ReleaseGameplayPause()
        {
            if (!ownsPause) return;
            Time.timeScale = previousTimeScale;
            ownsPause = false;
        }

        // EN: Plays the optional unscaled opening spring and fade; previews stay fully visible.
        // VI: Chạy bật/hiện unscaled tùy chọn; xem trước giữ hiển thị đầy đủ.
        protected void AnimateOpen(bool animate = true)
        {
            CapturePopupScale();
            ResetAnimation();
            if (!animate || !Application.isPlaying || card == null || canvasGroup == null) return;
            card.localScale = restScale * 0.88f;
            canvasGroup.alpha = 0f;
            popupTween = DOTween.Sequence().SetUpdate(true)
                .Append(card.DOScale(restScale, openDuration).SetEase(Ease.OutBack))
                .Join(canvasGroup.DOFade(1f, openDuration * 0.75f));
        }

        // EN: Plays the exit cue once, animates with input locked and delegates completion to the concrete popup.
        // VI: Phát âm đóng một lần, chạy hiệu ứng khóa nhập và giao hoàn tất cho popup cụ thể.
        protected void AnimateClose(UnityAction completion)
        {
            closing = true;
            PlayUISound(IngameSoundManager.Event.PopupHide);
            canvasGroup.interactable = false;
            popupTween?.Kill();
            popupTween = DOTween.Sequence().SetUpdate(true)
                .Append(card.DOScale(restScale * 0.9f, closeDuration).SetEase(Ease.InQuad))
                .Join(canvasGroup.DOFade(0f, closeDuration))
                .OnComplete(() => FinishAnimation(completion));
        }

        // EN: Caches the optional scene effects manager for shared popup and outcome sounds, skipping editor previews.
        // VI: Cache manager hiệu ứng tùy chọn của scene cho âm popup/kết quả dùng chung, bỏ qua xem trước editor.
        protected void PlayUISound(IngameSoundManager.Event soundEvent)
        {
            if (!Application.isPlaying) return;
            if (soundManager == null) soundManager = IngameSoundManager.Instance;
            soundManager?.PlayEvent(soundEvent);
        }

        // EN: Clears tween ownership before a completion can close, reopen or replace the popup.
        // VI: Xóa quyền giữ tween trước khi callback có thể đóng, mở lại hoặc thay popup.
        private void FinishAnimation(UnityAction completion)
        {
            popupTween = null;
            completion.Invoke();
        }
        // EN: Provides the common exit-then-close path for nontransactional popup choices.
        // VI: Cung cấp luồng hiệu ứng rồi đóng dùng chung cho lựa chọn không có giao dịch.
        protected void CloseWithAnimation(UnityAction afterClose = null)
        {
            AnimateClose(() =>
            {
                Close();
                afterClose?.Invoke();
            });
        }

    }
}
