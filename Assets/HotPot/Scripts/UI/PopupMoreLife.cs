using System.Collections;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Out-of-lives popup opened by FoodPop.UIManager in Home and in game: shows the heart count and next-life timer, refills hearts with Coin or adds one heart after a rewarded ad.
    // VI: Popup hết mạng do FoodPop.UIManager mở ở Home và trong màn: hiện số mạng và thời gian hồi mạng kế tiếp, hồi đầy mạng bằng Coin hoặc cộng một mạng sau khi xem quảng cáo.
    [DisallowMultipleComponent]
    public sealed class PopupMoreLife : UICanvasPrime
    {
        [Tooltip("Chữ số mạng hiện có, đặt trên hình trái tim.")]
        [SerializeField] private TMP_Text heartCountText;
        [Tooltip("Chữ thời gian còn lại tới khi hồi mạng kế tiếp.")]
        [SerializeField] private TMP_Text cooldownText;
        [Tooltip("Chữ giá Coin để hồi đầy mạng.")]
        [SerializeField] private TMP_Text priceText;
        [Tooltip("Nút xem quảng cáo để nhận thêm một mạng.")]
        [SerializeField] private Button watchButton;
        [Tooltip("Nút hồi đầy mạng bằng Coin.")]
        [SerializeField] private Button coinButton;
        [Tooltip("Nút đóng popup.")]
        [SerializeField] private Button closeButton;

        private HeartFeature heart;
        private int price;
        private Coroutine refreshRoutine;

        // EN: Caches the profile and heart feature, binds the three buttons and starts the once-per-second heart/timer refresh while visible.
        // VI: Cache hồ sơ và tính năng mạng, nối ba nút và bắt đầu làm mới số mạng/thời gian mỗi giây khi popup đang hiện.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            heart = HeartFeature.Instance;
            watchButton.onClick.AddListener(WatchAds);
            coinButton.onClick.AddListener(BuyByCoin);
            closeButton.onClick.AddListener(OnClickClose);
            refreshRoutine = StartCoroutine(RefreshEverySecond());
        }

        // EN: Stops the refresh and removes the button listeners added in OnEnable.
        // VI: Dừng làm mới và gỡ các listener nút đã thêm trong OnEnable.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            StopCoroutine(refreshRoutine);
            watchButton.onClick.RemoveListener(WatchAds);
            coinButton.onClick.RemoveListener(BuyByCoin);
            closeButton.onClick.RemoveListener(OnClickClose);
        }

        // EN: Activates the popup, shows the configured refill price and current hearts, then plays the shared open animation.
        // VI: Bật popup, hiện giá hồi mạng trong config và số mạng hiện tại, rồi chạy hiệu ứng mở dùng chung.
        public override void Open()
        {
            base.Open();
            price = heart.Config.refillGoldCost;
            priceText.text = price.ToString();
            RefreshHeart();
            AnimateOpen();
        }

        // EN: Accepts a successful rewarded ad: adds one heart, records the ad and closes; temporarily called directly until the SDK is wired.
        // VI: Nhận quảng cáo thưởng thành công: cộng một mạng, ghi nhận quảng cáo và đóng; tạm gọi thẳng tới khi nối SDK.
        public void OnWatchAdsSuccess()
        {
            if (closing) return;
            heart.AddHeart();
            ProfileManager.instance.Features.Get<IRecordTracking>()?.RecordAdsReward();
            CloseWithAnimation();
        }

        // EN: Updates the heart count and the next-life timer every second in unscaled time so it keeps counting while gameplay is paused.
        // VI: Cập nhật số mạng và thời gian hồi mạng mỗi giây theo thời gian thực để vẫn đếm khi gameplay tạm dừng.
        private IEnumerator RefreshEverySecond()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                yield return wait;
                RefreshHeart();
            }
        }

        // EN: Shows the heart count and timer from HeartFeature.GetHeartDisplay.
        // VI: Hiện số mạng và thời gian lấy từ HeartFeature.GetHeartDisplay.
        private void RefreshHeart()
        {
            (string count, string timer) = heart.GetHeartDisplay();
            heartCountText.text = count;
            cooldownText.text = timer;
        }

        // EN: Plays haptics and temporarily forwards Watch to the ad-success callback.
        // VI: Phát rung và tạm chuyển Watch tới callback quảng cáo thành công.
        private void WatchAds()
        {
            if (closing) return;
            PlayHaptic();
            OnWatchAdsSuccess();
        }

        // EN: Refills all hearts when the Coin purchase commits and closes; otherwise shows a not-enough-Coin message.
        // VI: Hồi đầy mạng khi giao dịch Coin thành công rồi đóng; nếu không thì hiện thông báo thiếu Coin.
        private void BuyByCoin()
        {
            if (closing) return;
            PlayHaptic();
            if (playerData.TryBuyFullHearts(price, "more_live"))
            {
                PlayUISound(IngameSoundManager.Event.ItemBought);
                CloseWithAnimation();
            }
            else
            {
                Owner.ShowShortMessage($"Not enough gold. You need {price:N0} gold.");
            }
        }

        // EN: Closes the popup with the shared exit animation.
        // VI: Đóng popup bằng hiệu ứng thoát dùng chung.
        private void OnClickClose()
        {
            if (closing) return;
            PlayHaptic();
            CloseWithAnimation();
        }
    }
}
