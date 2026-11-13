using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class CanvasWarningQuit : UICanvasPrime
    {
        [Tooltip("Nút Confirm dùng chung cho về Home hoặc chơi lại.")]
        [SerializeField] private Button confirmButton;
        [Tooltip("Nút hủy thoát, quay lại Pause.")]
        [SerializeField] private Button cancelButton;
        [Tooltip("Chữ cảnh báo mất một mạng, luôn hiển thị.")]
        [SerializeField] private TMP_Text infoLabel;
        [Tooltip("Hình cảnh báo mất một mạng, luôn hiển thị.")]
        [SerializeField] private GameObject lifeCostIcon;
        [Tooltip("Cảnh báo mất một mạng, luôn hiển thị dù đang có mạng vô hạn.")]
        [SerializeField] private string quitMessage = "You will lose 1 life!";
        private UnityAction confirmAction;
        private UnityAction cancelAction;

        // EN: Caches shared feedback managers and binds the two authored buttons while visible.
        // VI: Cache manager phản hồi dùng chung và nối hai nút đã thiết kế khi đang hiện.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            confirmButton.onClick.AddListener(Confirm);
            cancelButton.onClick.AddListener(Cancel);
        }

        // EN: Cancels pending choices, removes owned listeners and releases only this canvas's pause.
        // VI: Hủy lựa chọn chờ, gỡ listener sở hữu và chỉ nhả pause của canvas này.
        private void OnDisable()
        {
            ResetAnimation();
            if (Application.isPlaying)
            {
                confirmButton.onClick.RemoveListener(Confirm);
                cancelButton.onClick.RemoveListener(Cancel);
            }
            confirmAction = null;
            cancelAction = null;
            ReleaseGameplayPause();
        }

        // EN: Always shows the one-life warning and retains the current game speed; actual life rules apply on confirmation.
        // VI: Luôn hiện cảnh báo mất một mạng và giữ tốc độ game; quy tắc mất mạng áp dụng khi xác nhận.
        public void Show(UnityAction confirm, UnityAction cancel)
        {
            confirmAction = confirm;
            cancelAction = cancel;
            infoLabel.text = quitMessage;
            lifeCostIcon.SetActive(true);
            PauseGameplay();
            base.Open();
            AnimateOpen();
        }

        // EN: Plays feedback and confirms once only after the exit animation.
        // VI: Phát phản hồi và xác nhận một lần sau hiệu ứng đóng.
        private void Confirm()
        {
            if (closing) return;
            PlayClickFeedback();
            CloseWithAnimation(confirmAction);
        }

        // EN: Plays haptics and returns to Pause without click audio or life loss.
        // VI: Phát rung và trở lại Pause không phát tiếng nhấn hoặc mất mạng.
        private void Cancel()
        {
            if (closing) return;
            PlayHaptic();
            CloseWithAnimation(cancelAction);
        }

    }
}
