using DG.Tweening;
using Spine.Unity;
using GamePackages;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class CanvasLose : UICanvasPrime
    {
        [Tooltip("Title Spine của popup thua, phát một lần trước khi hiện lựa chọn.")]
        [SerializeField] private SkeletonGraphic titleAnimation;
        [Tooltip("Nhóm chữ thông báo mất mạng.")]
        [SerializeField] private Transform lossMessage;
        [Tooltip("Nhóm chứa hai nút Home và Retry.")]
        [SerializeField] private Transform choiceButtons;
        [Tooltip("Thời gian bật nhẹ thông báo và nút sau title, tính bằng giây unscaled.")]
        [SerializeField, Min(0.01f)] private float choiceRevealDuration = 0.3f;

        [Tooltip("Nút về màn hình chính sau khi thua.")]
        [SerializeField] private Button homeButton;
        [Tooltip("Nút chơi lại màn vừa thua.")]
        [SerializeField] private Button retryButton;

        private UnityAction replayAction;
        private Spine.TrackEntry titleEntry;
        private Sequence choiceReveal;
        private Vector3 messageRestScale;
        private Vector3 buttonsRestScale;
        private bool capturedScales;
        private bool choicesReady;

        // EN: Caches shared feedback and binds the authored loss buttons while visible.
        // VI: Cache phản hồi dùng chung và nối các nút thua đã dựng khi đang hiện.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            homeButton.onClick.AddListener(Home);
            retryButton.onClick.AddListener(Retry);
        }

        // EN: Cancel popup/reveal transitions and title completion, remove listeners and clear pending replay.
        // VI: Hủy chuyển động popup/hiện lựa chọn và sự kiện title, gỡ listener và xóa hành động chơi lại còn chờ.
        private void OnDisable()
        {
            ResetChoiceReveal();
            if (titleEntry != null) titleEntry.Complete -= OnTitleComplete;
            titleEntry = null;
            choicesReady = false;
            ResetAnimation();
            if (Application.isPlaying)
            {
                homeButton.onClick.RemoveListener(Home);
                retryButton.onClick.RemoveListener(Retry);
            }
            replayAction = null;
        }

        // EN: Restart the authored title before revealing loss details and navigation; editor previews show the full layout.
        // VI: Chạy lại title đã dựng trước khi hiện thông báo thua và điều hướng; editor hiện đủ bố cục.
        public void Show(UnityAction replay, bool animate = true)
        {
            replayAction = replay;
            ResetChoiceReveal();
            choicesReady = !Application.isPlaying;
            SetChoicesVisible(choicesReady);
            homeButton.interactable = choicesReady;
            retryButton.interactable = choicesReady;
            base.Open();
            if (Application.isPlaying) PlayTitleAnimation();
            AnimateOpen(animate);
        }

        // EN: Restore authored scales and cancel pending reveal callbacks on reopen or disable.
        // VI: Khôi phục scale thiết kế và hủy callback hiện lựa chọn khi mở lại hoặc tắt.
        private void ResetChoiceReveal()
        {
            if (!capturedScales)
            {
                messageRestScale = lossMessage.localScale;
                buttonsRestScale = choiceButtons.localScale;
                capturedScales = true;
            }
            choiceReveal?.Kill();
            choiceReveal = null;
            lossMessage.localScale = messageRestScale;
            choiceButtons.localScale = buttonsRestScale;
        }

        // EN: Show or hide only the message and navigation while leaving the title and wallet visible.
        // VI: Chỉ hiện/ẩn thông báo và điều hướng, giữ title và ví hiển thị.
        private void SetChoicesVisible(bool visible)
        {
            lossMessage.gameObject.SetActive(visible);
            choiceButtons.gameObject.SetActive(visible);
        }

        // EN: Play the authored loss animation once using unscaled time and listen to its actual completion.
        // VI: Phát animation thua đã dựng một lần bằng thời gian unscaled và nghe lúc thực sự hoàn tất.
        private void PlayTitleAnimation()
        {
            titleAnimation.UnscaledTime = true;
            titleAnimation.Initialize(false);
            if (titleEntry != null) titleEntry.Complete -= OnTitleComplete;
            titleAnimation.AnimationState.ClearTracks();
            titleAnimation.Skeleton.SetToSetupPose();
            titleEntry = titleAnimation.AnimationState.SetAnimation(0, "animation", false);
            titleEntry.MixDuration = 0f;
            titleEntry.Complete += OnTitleComplete;
        }

        // EN: Spring in the message and buttons after the title finishes, keeping navigation locked until both finish.
        // VI: Bật nhẹ thông báo và nút sau khi title xong, giữ điều hướng khóa tới khi cả hai hoàn tất.
        private void OnTitleComplete(Spine.TrackEntry entry)
        {
            entry.Complete -= OnTitleComplete;
            titleEntry = null;
            if (!isActiveAndEnabled || closing) return;
            SetChoicesVisible(true);
            lossMessage.localScale = messageRestScale * 0.8f;
            choiceButtons.localScale = buttonsRestScale * 0.8f;
            choiceReveal = DOTween.Sequence().SetUpdate(true)
                .Append(lossMessage.DOScale(messageRestScale, choiceRevealDuration).SetEase(Ease.OutBack))
                .Insert(0.08f, choiceButtons.DOScale(buttonsRestScale, choiceRevealDuration).SetEase(Ease.OutBack))
                .OnComplete(CompleteChoiceReveal);
        }

        // EN: Unlock Home and Retry after their entrance finishes.
        // VI: Mở khóa Home và Retry sau khi hiệu ứng xuất hiện của chúng hoàn tất.
        private void CompleteChoiceReveal()
        {
            choiceReveal = null;
            choicesReady = true;
            homeButton.interactable = true;
            retryButton.interactable = true;
        }

        // EN: Requests Home through the same one-time loss confirmation as Retry.
        // VI: Yêu cầu về Home qua cùng luồng xác nhận mất mạng một lần như Retry.
        private void Home()
        {
            ConfirmChoice(ReturnHome);
        }

        // EN: Confirms the loss and invokes the existing retry only after the exit animation.
        // VI: Xác nhận thua và chỉ gọi chơi lại hiện có sau hiệu ứng đóng.
        private void Retry()
        {
            ConfirmChoice(replayAction);
        }

        // EN: Accept only revealed choices, block repeats, charge one finite life and save before exit/navigation.
        // VI: Chỉ nhận lựa chọn đã hiện, chặn nhấn lặp, trừ một mạng hữu hạn và lưu trước khi đóng/điều hướng.
        private void ConfirmChoice(UnityAction action)
        {
            if (!choicesReady || closing) return;
            PlayClickFeedback();
            ProfileManager.instance.GameFeatureContext.HeartManager.UseHeart();
            playerData.ResetStatePlaying();
            CloseWithAnimation(action);
        }

        // EN: Loads Home after confirmed loss without recording the same defeat twice.
        // VI: Nạp Home sau khi xác nhận thua, không ghi nhận cùng thất bại hai lần.
        private void ReturnHome()
        {
            SceneManager.LoadScene("Home");
        }
    }
}
