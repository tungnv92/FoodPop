using DG.Tweening;
using Spine.Unity;
using AssetKits.ParticleImage;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class CanvasWin : UICanvasPrime
    {
        private const int WinReward = 100;
        private const int WatchReward = WinReward * 2;

        [Tooltip("Animation Spine cho tiêu đề chiến thắng.")]
        [SerializeField] private SkeletonGraphic titleAnimation;
        [Tooltip("Logo phát Start_End trước khi bắt đầu title chiến thắng.")]
        [SerializeField] private SkeletonGraphic logoAnimation;
        [Tooltip("Số giây cuối của logo bắt đầu chạy chồng với Win Start.")]
        [SerializeField, Min(0f)] private float titleOverlap = 0.2f;
        [Tooltip("Thời gian từ lúc Win Start bắt đầu đến khi hiện ví, ánh sáng, thưởng và nút, tính bằng giây unscaled.")]
        [SerializeField, Min(0f)] private float titleClaimDelay = 2f;
        [Tooltip("Ánh sáng hiện cùng phần thưởng sau khoảng chờ tính từ Win Start.")]
        [SerializeField] private GameObject glow;

        [Tooltip("Nút nhận thưởng và sang màn tiếp theo.")]
        [SerializeField] private Button nextButton;
        [Tooltip("Nút nhận thưởng gấp đôi qua quảng cáo.")]
        [SerializeField] private Button watchButton;
        [Tooltip("Chữ phần thưởng thắng cơ bản.")]
        [SerializeField] private TMP_Text rewardLabel;
        [Tooltip("Chữ phần thưởng sau khi xem quảng cáo.")]
        [SerializeField] private TMP_Text watchRewardLabel;

        [Tooltip("Nhóm phần thưởng sẽ ẩn khi bắt đầu nhận.")]
        [SerializeField] private GameObject rewardRoot;
        [Tooltip("Nhóm chứa các nút nhận thưởng, dùng để chạy hiệu ứng hiện mà không ảnh hưởng scale khi bấm nút.")]
        [SerializeField] private Transform claimButtons;
        [Tooltip("Thời gian phóng nhẹ khi hiện phần thưởng và các nút, tính bằng giây unscaled.")]
        [SerializeField, Min(0.01f)] private float claimRevealDuration = 0.3f;
        [Tooltip("Hiệu ứng xu bay về ví, hoàn tất rồi mới cộng xu.")]
        [SerializeField] private ParticleImage coinAttraction;
        [Tooltip("Bộ hiển thị ví tăng dần khi xu thưởng đang bay.")]
        [SerializeField] private UICoin rewardCoin;
        [Tooltip("Thời gian chờ sau khi xu bắt đầu bay rồi mới tăng số Coin, tính bằng giây unscaled.")]
        [SerializeField, Min(0f)] private float coinCountDelay = 0.4f;

        private UnityAction nextAction;
        private bool hasNextLevel;
        private bool claimed;
        private int pendingReward;
        private Tween claimDelay;
        private Spine.TrackEntry logoEntry;
        private bool winTitleStarted;
        private bool rewardReady;
        private Sequence claimReveal;
        private Vector3 rewardRestScale;
        private Vector3 buttonsRestScale;
        private bool capturedClaimScales;

        // EN: Caches feedback managers and binds reward buttons and particle completion while visible.
        // VI: Cache manager phản hồi, nối nút nhận thưởng và sự kiện hiệu ứng hoàn tất khi đang hiện.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            nextButton.onClick.AddListener(ClaimNext);
            watchButton.onClick.AddListener(WatchAds);
            coinAttraction.onLastParticleFinish.AddListener(CompleteReward);
        }

        // EN: Cancel popup/reveal tweens, logo/title callbacks, particles and pending reward/navigation.
        // VI: Hủy tween popup/hiện thưởng, sự kiện logo/title, hạt xu và thưởng/điều hướng còn chờ.
        private void OnDisable()
        {
            ResetClaimReveal();
            ResetAnimation();
            if (Application.isPlaying)
            {
                nextButton.onClick.RemoveListener(ClaimNext);
                watchButton.onClick.RemoveListener(WatchAds);
                coinAttraction.onLastParticleFinish.RemoveListener(CompleteReward);
                coinAttraction.Stop(true);
                StopTitleSequence();
            }
            nextAction = null;
            pendingReward = 0;
        }

        // EN: Restart logo then win title, keeping wallet and claims hidden until the configured delay from Win Start.
        // VI: Chạy lại logo rồi title thắng, ẩn ví và thưởng tới hết khoảng chờ tính từ Win Start.
        public void Show(UnityAction next, bool hasNextLevel = true, bool animate = true)
        {
            nextAction = next;
            this.hasNextLevel = hasNextLevel;
            claimed = false;
            pendingReward = 0;
            ResetClaimReveal();
            rewardReady = !Application.isPlaying;
            SetClaimVisible(rewardReady);
            if (Application.isPlaying) coinAttraction.Stop(true);
            rewardLabel.text = WinReward.ToString();
            watchRewardLabel.text = "+" + WatchReward;
            nextButton.interactable = rewardReady;
            watchButton.interactable = rewardReady;
            base.Open();
            if (Application.isPlaying) PlayLogoAnimation();
            else
            {
                logoAnimation.gameObject.SetActive(false);
                titleAnimation.gameObject.SetActive(true);
            }
            AnimateOpen(animate);
        }

        // EN: Accepts a successful rewarded ad once; temporarily called immediately until SDK wiring is added.
        // VI: Nhận quảng cáo thưởng thành công một lần; tạm gọi ngay tới khi nối SDK.
        public void OnWatchAdsSuccess()
        {
            ClaimReward(WatchReward);
        }

        // EN: Restore authored scales and cancel pending reveal callbacks on reopen or disable.
        // VI: Khôi phục scale thiết kế và hủy callback hiện thưởng còn chờ khi mở lại hoặc tắt.
        private void ResetClaimReveal()
        {
            if (!capturedClaimScales)
            {
                rewardRestScale = rewardRoot.transform.localScale;
                buttonsRestScale = claimButtons.localScale;
                capturedClaimScales = true;
            }
            claimReveal?.Kill();
            claimReveal = null;
            rewardRoot.transform.localScale = rewardRestScale;
            claimButtons.localScale = buttonsRestScale;
        }

        // EN: Show or hide the wallet, glow, reward and claim buttons together after Win Start.
        // VI: Hiện hoặc ẩn ví, ánh sáng, phần thưởng và các nút nhận thưởng cùng nhau sau Win Start.
        private void SetClaimVisible(bool visible)
        {
            rewardCoin.gameObject.SetActive(visible);
            glow.SetActive(visible);
            rewardRoot.SetActive(visible);
            nextButton.gameObject.SetActive(visible);
            watchButton.gameObject.SetActive(visible);
        }

        // EN: Start the authored logo once in unscaled time and watch its actual track for the title overlap.
        // VI: Phát logo đã dựng một lần theo thời gian unscaled và theo dõi track thật để chồng title.
        private void PlayLogoAnimation()
        {
            StopTitleSequence();
            winTitleStarted = false;
            titleAnimation.gameObject.SetActive(false);
            logoAnimation.gameObject.SetActive(true);
            logoAnimation.UnscaledTime = true;
            logoAnimation.Initialize(false);
            logoAnimation.AnimationState.ClearTracks();
            logoAnimation.Skeleton.SetToSetupPose();
            logoEntry = logoAnimation.AnimationState.SetAnimation(0, "Start_End", false);
            logoEntry.MixDuration = 0f;
            logoEntry.Complete += OnLogoComplete;
            logoAnimation.UpdateComplete += OnLogoUpdate;
        }

        // EN: Remove logo callbacks and cancel the pending claim delay before reopening or disabling.
        // VI: Gỡ callback logo và hủy khoảng chờ hiện thưởng khi mở lại hoặc tắt.
        private void StopTitleSequence()
        {
            logoAnimation.UpdateComplete -= OnLogoUpdate;
            if (logoEntry != null) logoEntry.Complete -= OnLogoComplete;
            claimDelay?.Kill();
            claimDelay = null;
            logoEntry = null;

        }

        // EN: Begin Win Start once when the logo reaches its final configured overlap interval.
        // VI: Bắt đầu Win Start một lần khi logo tới khoảng chồng nhịp cuối đã cấu hình.
        private void OnLogoUpdate(ISkeletonAnimation animated)
        {
            if (logoEntry.TrackTime < Mathf.Max(0f, logoEntry.Animation.Duration - titleOverlap)) return;
            logoAnimation.UpdateComplete -= OnLogoUpdate;
            if (isActiveAndEnabled && !closing) PlayTitleAnimation();
        }

        // EN: Play Win Start then Idle and schedule wallet/claim reveal from the actual start time.
        // VI: Phát Win Start rồi Idle và đặt lịch hiện ví/thưởng từ đúng lúc Start bắt đầu.
        private void PlayTitleAnimation()
        {
            winTitleStarted = true;
            titleAnimation.gameObject.SetActive(true);
            titleAnimation.UnscaledTime = true;
            titleAnimation.Initialize(false);
            titleAnimation.AnimationState.ClearTracks();
            titleAnimation.Skeleton.SetToSetupPose();
            titleAnimation.AnimationState.SetAnimation(0, "Start", false).MixDuration = 0f;
            titleAnimation.AnimationState.AddAnimation(0, "Idle", true, 0f);
            claimDelay = DOVirtual.DelayedCall(titleClaimDelay, RevealClaims).SetUpdate(true);
        }

        // EN: Hide the completed logo and ensure title startup if completion preceded the frame update callback.
        // VI: Ẩn logo đã xong và bắt đầu title nếu sự kiện hoàn tất đến trước callback cập nhật frame.
        private void OnLogoComplete(Spine.TrackEntry entry)
        {
            entry.Complete -= OnLogoComplete;
            logoAnimation.UpdateComplete -= OnLogoUpdate;
            logoEntry = null;
            if (isActiveAndEnabled && !closing && !winTitleStarted) PlayTitleAnimation();
            logoAnimation.gameObject.SetActive(false);
        }

        // EN: Reveal the wallet/glow and spring in claims after the configured unscaled delay from Win Start.
        // VI: Hiện ví/ánh sáng và bật nhẹ phần thưởng sau khoảng trễ unscaled tính từ Win Start.
        private void RevealClaims()
        {
            claimDelay = null;
            if (!isActiveAndEnabled || closing) return;
            SetClaimVisible(true);
            rewardRoot.transform.localScale = rewardRestScale * 0.8f;
            claimButtons.localScale = buttonsRestScale * 0.8f;
            claimReveal = DOTween.Sequence().SetUpdate(true)
                .Append(rewardRoot.transform.DOScale(rewardRestScale, claimRevealDuration).SetEase(Ease.OutBack))
                .Insert(0.08f, claimButtons.DOScale(buttonsRestScale, claimRevealDuration).SetEase(Ease.OutBack))
                .OnComplete(CompleteClaimReveal);
        }

        // EN: Enable claims only after both entrance tweens have finished.
        // VI: Chỉ cho nhận thưởng sau khi cả hai tween xuất hiện hoàn tất.
        private void CompleteClaimReveal()
        {
            claimReveal = null;
            rewardReady = true;
            nextButton.interactable = true;
            watchButton.interactable = true;
        }

        // EN: Plays click feedback and claims the normal win reward.
        // VI: Phát phản hồi nhấn và nhận thưởng thắng thường.
        private void ClaimNext()
        {
            if (!rewardReady || claimed || closing) return;
            PlayClickFeedback();
            ClaimReward(WinReward);
        }

        // EN: Plays feedback and temporarily forwards Watch to the public ad-success callback.
        // VI: Phát phản hồi và tạm chuyển Watch tới callback thành công quảng cáo công khai.
        private void WatchAds()
        {
            if (claimed || closing) return;
            PlayClickFeedback();
            OnWatchAdsSuccess();
        }

        // EN: Lock claims, start attraction and delay wallet count-up by the configured unscaled interval.
        // VI: Khóa nhận thưởng, chạy xu bay và tăng số ví sau khoảng trễ unscaled đã cấu hình.
        private void ClaimReward(int amount)
        {
            if (!rewardReady || claimed || closing || !isActiveAndEnabled || !Application.isPlaying) return;
            claimed = true;
            nextButton.interactable = false;
            watchButton.interactable = false;
            pendingReward = amount;
            rewardRoot.SetActive(false);
            PlayUISound(IngameSoundManager.Event.RewardCollect);
            coinAttraction.Play();
            rewardCoin.AnimateReward(amount, Mathf.Max(0.01f, coinAttraction.duration + coinAttraction.lifetime.constantMax - coinCountDelay), coinCountDelay);
        }

        // EN: Credits the selected total only after the last coin finishes, updating UICoin before navigation.
        // VI: Chỉ cộng tổng thưởng đã chọn khi xu cuối kết thúc, cập nhật UICoin trước khi chuyển màn.
        private void CompleteReward()
        {
            if (pendingReward == 0) return;
            int amount = pendingReward;
            pendingReward = 0;
            playerData.OnAddCoin(amount, "win_reward");
            if (hasNextLevel) CloseWithAnimation(nextAction);
        }
    }
}
