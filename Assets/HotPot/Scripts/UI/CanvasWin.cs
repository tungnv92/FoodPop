using DG.Tweening;
using GamePackages;
using Spine.Unity;
using AssetKits.ParticleImage;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class CanvasWin : UICanvasPrime
    {
        // EN: Reads the win coin rewards (normal and watch-ads) from IngameConfig.
        // VI: Lấy số xu thưởng thắng (thường và xem quảng cáo) từ IngameConfig.
        private static IngameConfig.WinRewardSettings Reward => IngameConfig.Instance.winReward;

        [Tooltip("Animation Spine cho tiêu đề chiến thắng.")]
        [SerializeField] private SkeletonGraphic titleAnimation;
        [Tooltip("Băng curtain chạy animation Close cùng lúc đầu bếp thắng; đóng xong thì hiện nền đen rồi chạy title chiến thắng.")]
        [FormerlySerializedAs("logoAnimation")]
        [SerializeField] private SkeletonGraphic curtainAnimation;
        [Tooltip("Thời gian giữ rèm đã đóng trước khi nền đen bắt đầu hiện, tính bằng giây unscaled.")]
        [SerializeField, Min(0f)] private float curtainHoldDelay = 0.5f;
        [Tooltip("Nền đen hiện mờ dần sau khi rèm đóng, rồi mới chạy animation chiến thắng.")]
        [SerializeField] private Image blackBackground;
        [Tooltip("Thời gian nền đen hiện mờ dần, tính bằng giây unscaled.")]
        [SerializeField, Min(0f)] private float blackFadeDuration = 0.3f;
        [Tooltip("Thời gian từ lúc Win Start bắt đầu đến khi hiện ví, ánh sáng, thưởng và nút, tính bằng giây unscaled.")]
        [SerializeField, Min(0f)] private float titleClaimDelay = 2f;
        [Tooltip("Ánh sáng hiện khi title chiến thắng chuyển sang animation Idle.")]
        [FormerlySerializedAs("glow")]
        [SerializeField] private GameObject glowRoot;
        [Tooltip("Thời gian ánh sáng hiện mờ dần và phóng nhẹ từ 0.8 lên kích thước gốc, tính bằng giây unscaled.")]
        [SerializeField, Min(0.01f)] private float glowRevealDuration = 0.3f;

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
        [FormerlySerializedAs("claimButtons")]
        [SerializeField] private Transform buttonsRoot;
        [Tooltip("Thời gian phóng nhẹ khi hiện phần thưởng và các nút, tính bằng giây unscaled.")]
        [SerializeField, Min(0.01f)] private float claimRevealDuration = 0.3f;
        [Tooltip("Hiệu ứng xu bay về ví, hoàn tất rồi mới cộng xu.")]
        [SerializeField] private ParticleImage coinAttraction;
        [Tooltip("Bộ hiển thị ví tăng dần khi xu thưởng đang bay.")]
        [SerializeField] private UICoin rewardCoin;
        [Tooltip("Thời gian chờ sau khi xu bắt đầu bay rồi mới tăng số Coin, tính bằng giây unscaled.")]
        [SerializeField, Min(0f)] private float coinCountDelay = 0.4f;

        private UnityAction nextAction;
        private UnityAction startAction;
        private bool hasNextLevel;
        private bool claimed;
        private Tween curtainHold;
        private Tween claimDelay;
        private Tween blackFade;
        private Tween rewardFlight;
        private Sequence glowReveal;
        private Graphic glowGraphic;
        private float glowAlpha;
        private Vector3 glowRestScale;
        private float blackAlpha;
        private bool capturedBlackAlpha;
        private bool rewardReady;
        private Sequence claimReveal;
        private Vector3 rewardRestScale;
        private Vector3 buttonsRestScale;
        private bool capturedClaimScales;

        // EN: Caches feedback managers and binds reward buttons while visible.
        // VI: Cache manager phản hồi và nối nút nhận thưởng khi đang hiện.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            nextButton.onClick.AddListener(ClaimNext);
            watchButton.onClick.AddListener(WatchAds);
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
                rewardFlight?.Kill();
                rewardFlight = null;
                coinAttraction.Stop(true);
                StopTitleSequence();
            }
            nextAction = null;
            startAction = null;
        }

        // EN: Stores the next-level load and the start action run after the curtain opens, then restarts curtain Close (with the chef win already playing) and the win title, keeping wallet and claims hidden until the configured delay from Win Start.
        // VI: Lưu hành động nạp màn kế tiếp và hành động bắt đầu chạy sau khi mở rèm, rồi chạy lại curtain Close (đầu bếp đã phát thắng) và title thắng, ẩn ví và thưởng tới hết khoảng chờ tính từ Win Start.
        public void Show(UnityAction next, UnityAction start, bool hasNextLevel = true, bool animate = true)
        {
            nextAction = next;
            startAction = start;
            this.hasNextLevel = hasNextLevel;
            claimed = false;
            ResetClaimReveal();
            rewardReady = !Application.isPlaying;
            SetClaimVisible(rewardReady);
            glowRoot.SetActive(rewardReady);
            if (Application.isPlaying) coinAttraction.Stop(true);
            rewardLabel.text = Reward.coin.ToString();
            watchRewardLabel.text = "+" + Reward.watchAdsCoin;
            nextButton.interactable = rewardReady;
            watchButton.interactable = rewardReady;
            base.Open();
            if (Application.isPlaying) PlayCurtainClose();
            else
            {
                curtainAnimation.gameObject.SetActive(false);
                blackBackground.gameObject.SetActive(true);
                titleAnimation.gameObject.SetActive(true);
            }
            AnimateOpen(animate);
        }

        // EN: Accepts a successful rewarded ad once; temporarily called immediately until SDK wiring is added.
        // VI: Nhận quảng cáo thưởng thành công một lần; tạm gọi ngay tới khi nối SDK.
        public void OnWatchAdsSuccess()
        {
            ClaimReward(Reward.watchAdsCoin);
        }

        // EN: Restore authored scales and cancel pending reveal callbacks on reopen or disable.
        // VI: Khôi phục scale thiết kế và hủy callback hiện thưởng còn chờ khi mở lại hoặc tắt.
        private void ResetClaimReveal()
        {
            if (!capturedClaimScales)
            {
                rewardRestScale = rewardRoot.transform.localScale;
                buttonsRestScale = buttonsRoot.localScale;
                capturedClaimScales = true;
            }
            claimReveal?.Kill();
            claimReveal = null;
            rewardRoot.transform.localScale = rewardRestScale;
            buttonsRoot.localScale = buttonsRestScale;
        }

        // EN: Show or hide the wallet, reward and claim buttons together after Win Start; the glow follows the title Idle instead.
        // VI: Hiện hoặc ẩn ví, phần thưởng và các nút nhận thưởng cùng nhau sau Win Start; ánh sáng đi theo Idle của title.
        private void SetClaimVisible(bool visible)
        {
            rewardCoin.gameObject.SetActive(visible);
            rewardRoot.SetActive(visible);
            nextButton.gameObject.SetActive(visible);
            watchButton.gameObject.SetActive(visible);
        }

        // EN: Play curtain Close once in unscaled time while the chef victory (started by LevelController just before) runs, then fade in the black background as soon as the curtain Close animation completes.
        // VI: Phát curtain Close một lần theo thời gian unscaled trong lúc đầu bếp thắng (LevelController vừa bắt đầu) đang chạy, rồi hiện mờ nền đen ngay khi animation đóng rèm chạy xong.
        private void PlayCurtainClose()
        {
            StopTitleSequence();
            titleAnimation.gameObject.SetActive(false);
            if (!capturedBlackAlpha)
            {
                blackAlpha = blackBackground.color.a;
                capturedBlackAlpha = true;
            }
            blackBackground.gameObject.SetActive(false);
            curtainAnimation.gameObject.SetActive(true);
            curtainAnimation.UnscaledTime = true;
            curtainAnimation.Initialize(false);
            curtainAnimation.AnimationState.ClearTracks();
            curtainAnimation.Skeleton.SetToSetupPose();
            Spine.TrackEntry close = curtainAnimation.AnimationState.SetAnimation(0, "Close", false);
            close.MixDuration = 0f;
            curtainAnimation.Update(0f);
            curtainAnimation.LateUpdate();
            close.Complete += OnCurtainClosed;
        }

        // EN: Fires when the curtain Close animation has fully finished, then holds the closed curtain for curtainHoldDelay (unscaled) before the black background step.
        // VI: Chạy khi animation đóng rèm đã kết thúc hẳn, rồi giữ rèm đóng thêm curtainHoldDelay (unscaled) trước bước hiện nền đen.
        private void OnCurtainClosed(Spine.TrackEntry entry)
        {
            entry.Complete -= OnCurtainClosed;
            if (!isActiveAndEnabled || closing) return;
            curtainHold = DOVirtual.DelayedCall(curtainHoldDelay, ShowBlackBackground).SetUpdate(true);
        }

        // EN: After the curtain hold, fade the black background in to its authored alpha in unscaled time, then start the win title; ignored if the popup closed meanwhile.
        // VI: Sau khi giữ rèm đóng, hiện mờ nền đen tới độ trong suốt đã thiết kế theo thời gian unscaled, rồi chạy title thắng; bỏ qua nếu popup đã đóng trong lúc chờ.
        private void ShowBlackBackground()
        {
            curtainHold = null;
            if (!isActiveAndEnabled || closing) return;
            blackBackground.gameObject.SetActive(true);
            Color color = blackBackground.color;
            color.a = 0f;
            blackBackground.color = color;
            blackFade = blackBackground.DOFade(blackAlpha, blackFadeDuration).SetUpdate(true).OnComplete(PlayTitleAnimation);
        }

        // EN: Cancel the pending curtain hold, black background fade, glow reveal or claim delay before reopening or disabling.
        // VI: Hủy khoảng giữ rèm, hiệu ứng hiện nền đen, hiệu ứng hiện ánh sáng hoặc khoảng chờ hiện thưởng còn lại khi mở lại hoặc tắt.
        private void StopTitleSequence()
        {
            glowReveal?.Kill();
            glowReveal = null;
            curtainHold?.Kill();
            curtainHold = null;
            claimDelay?.Kill();
            claimDelay = null;
            blackFade?.Kill();
            blackFade = null;
        }

        // EN: Play Win Start then Idle (the glow appears when Idle begins) and schedule wallet/claim reveal from the actual start time; ignored if the popup closed meanwhile.
        // VI: Phát Win Start rồi Idle (ánh sáng hiện khi Idle bắt đầu) và đặt lịch hiện ví/thưởng từ đúng lúc Start bắt đầu; bỏ qua nếu popup đã đóng trong lúc chờ.
        private void PlayTitleAnimation()
        {
            claimDelay = null;
            blackFade = null;
            if (!isActiveAndEnabled || closing) return;
            titleAnimation.gameObject.SetActive(true);
            titleAnimation.UnscaledTime = true;
            titleAnimation.Initialize(false);
            titleAnimation.AnimationState.ClearTracks();
            titleAnimation.Skeleton.SetToSetupPose();
            titleAnimation.AnimationState.SetAnimation(0, "Start", false).MixDuration = 0f;
            titleAnimation.AnimationState.AddAnimation(0, "Idle", true, 0f).Start += OnTitleIdleStarted;
            claimDelay = DOVirtual.DelayedCall(titleClaimDelay, RevealClaims).SetUpdate(true);
        }

        // EN: Once the title enters Idle, fades the glow in from 0 to its authored alpha while scaling it from 0.8 to its authored scale (OutBack, unscaled); ignored if the popup closed meanwhile.
        // VI: Khi title chuyển sang Idle, hiện mờ dần ánh sáng từ 0 tới alpha thiết kế đồng thời phóng từ 0.8 lên scale thiết kế (OutBack, unscaled); bỏ qua nếu popup đã đóng trong lúc chờ.
        private void OnTitleIdleStarted(Spine.TrackEntry entry)
        {
            entry.Start -= OnTitleIdleStarted;
            if (!isActiveAndEnabled || closing) return;
            if (glowGraphic == null)
            {
                glowGraphic = glowRoot.GetComponent<Graphic>();
                glowAlpha = glowGraphic.color.a;
                glowRestScale = glowRoot.transform.localScale;
            }
            Color color = glowGraphic.color;
            color.a = 0f;
            glowGraphic.color = color;
            glowRoot.transform.localScale = glowRestScale * 0.8f;
            glowRoot.SetActive(true);
            glowReveal = DOTween.Sequence().SetUpdate(true)
                .Join(glowGraphic.DOFade(glowAlpha, glowRevealDuration))
                .Join(glowRoot.transform.DOScale(glowRestScale, glowRevealDuration).SetEase(Ease.OutBack))
                .OnComplete(() => glowReveal = null);
        }

        // EN: Reveal the wallet/glow and spring in claims after the configured unscaled delay from Win Start.
        // VI: Hiện ví/ánh sáng và bật nhẹ phần thưởng sau khoảng trễ unscaled tính từ Win Start.
        private void RevealClaims()
        {
            claimDelay = null;
            if (!isActiveAndEnabled || closing) return;
            SetClaimVisible(true);
            rewardRoot.transform.localScale = rewardRestScale * 0.8f;
            buttonsRoot.localScale = buttonsRestScale * 0.8f;
            claimReveal = DOTween.Sequence().SetUpdate(true)
                .Append(rewardRoot.transform.DOScale(rewardRestScale, claimRevealDuration).SetEase(Ease.OutBack))
                .Insert(0.08f, buttonsRoot.DOScale(buttonsRestScale, claimRevealDuration).SetEase(Ease.OutBack))
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
            ClaimReward(Reward.coin);
        }

        // EN: Plays feedback and temporarily forwards Watch to the public ad-success callback.
        // VI: Phát phản hồi và tạm chuyển Watch tới callback thành công quảng cáo công khai.
        private void WatchAds()
        {
            if (claimed || closing) return;
            PlayClickFeedback();
            OnWatchAdsSuccess();
        }

        // EN: Lock claims and credit/save the reward at once so quitting during the coin flight cannot lose it, then start attraction, count the wallet up from the old balance after the configured unscaled delay and schedule navigation at the end of the flight.
        // VI: Khóa nhận thưởng và cộng/lưu thưởng ngay để thoát game lúc xu đang bay không bị mất, rồi chạy xu bay, tăng số ví từ số dư cũ sau khoảng trễ unscaled đã cấu hình và hẹn chuyển màn khi xu bay xong.
        private void ClaimReward(int amount)
        {
            if (!rewardReady || claimed || closing || !isActiveAndEnabled || !Application.isPlaying) return;
            claimed = true;
            nextButton.interactable = false;
            watchButton.interactable = false;
            int previousCoin = playerData.GetResourceValue(ResourceType.Coin);
            playerData.OnAddCoin(amount, "win_reward");
            rewardRoot.SetActive(false);
            PlayUISound(IngameSoundManager.Event.RewardCollect);
            coinAttraction.Play();
            float flightDuration = coinAttraction.duration + coinAttraction.lifetime.constantMax;
            rewardCoin.AnimateReward(previousCoin, Mathf.Max(0.01f, flightDuration - coinCountDelay), coinCountDelay);
            rewardFlight = DOVirtual.DelayedCall(flightDuration, CompleteReward).SetUpdate(true);
        }

        // EN: Runs after the coin flight duration (unscaled timer, independent of particle events); when the player continues to the next level it starts the curtain Open transition. The reward was already credited at claim.
        // VI: Chạy khi hết thời lượng xu bay (hẹn giờ unscaled, không phụ thuộc sự kiện particle); nếu chơi tiếp màn kế tiếp thì bắt đầu chuyển cảnh mở rèm. Phần thưởng đã được cộng lúc nhận.
        private void CompleteReward()
        {
            rewardFlight = null;
            if (hasNextLevel) PlayCurtainOpen();
        }

        // EN: Hides everything except the closed curtain, loads the next level behind it (LevelController keeps this popup open), then plays curtain Open once in unscaled time to reveal the new level.
        // VI: Ẩn mọi thứ trừ rèm đang đóng, nạp màn kế tiếp phía sau rèm (LevelController giữ popup này mở), rồi phát curtain Open một lần theo thời gian unscaled để lộ ra màn mới.
        private void PlayCurtainOpen()
        {
            StopTitleSequence();
            SetClaimVisible(false);
            glowRoot.SetActive(false);
            titleAnimation.gameObject.SetActive(false);
            blackBackground.gameObject.SetActive(false);
            nextAction.Invoke();
            Spine.TrackEntry open = curtainAnimation.AnimationState.SetAnimation(0, "Open", false);
            open.MixDuration = 0f;
            open.Complete += OnCurtainOpened;
        }

        // EN: Once curtain Open has finished, closes the WinPopup without the generic close sound and then starts the new level (gameplay stays paused until here).
        // VI: Khi mở rèm xong thì đóng WinPopup không phát âm đóng chung, rồi bắt đầu màn mới (gameplay tạm dừng tới lúc này).
        private void OnCurtainOpened(Spine.TrackEntry entry)
        {
            entry.Complete -= OnCurtainOpened;
            if (!isActiveAndEnabled) return;
            UnityAction start = startAction;
            closing = true;
            Close();
            start.Invoke();
        }
    }
}
