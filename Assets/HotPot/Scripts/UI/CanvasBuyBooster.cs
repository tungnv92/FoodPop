using System;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class CanvasBuyBooster : UICanvasPrime
    {
        [Serializable]
        private sealed class Presentation
        {
            public BoosterType type;
            public Sprite icon;
        }

        public const int AmountPerPurchase = 3;
        public const int AmountPerAd = 1;
        // EN: Identifies the booster offered by the current opening.
        // VI: Xác định booster được đề nghị trong lần mở hiện tại.
        public BoosterType SelectedType { get; private set; }

        [Tooltip("Tên booster đang được mua.")]
        [SerializeField] private TMP_Text boosterName;
        [Tooltip("Mô tả tác dụng của booster.")]
        [SerializeField] private TMP_Text description;
        [Tooltip("Icon booster đang chọn.")]
        [SerializeField] private Image boosterIcon;
        [Tooltip("Chữ giá Coin trên nút mua.")]
        [SerializeField] private TMP_Text priceLabel;
        [Tooltip("Chữ số lượt booster nhận được.")]
        [SerializeField] private TMP_Text amountLabel;
        [Tooltip("Chữ số dư Coin nếu thiết kế có sử dụng; để trống khi không hiển thị ví.")]
        [SerializeField] private TMP_Text goldBalanceLabel;
        [Tooltip("Bật để hiện nút nhận một lượt booster qua quảng cáo; tắt chỉ hiện nút mua Coin.")]
        [SerializeField] private bool activeAdsBooster;
        [Tooltip("Chữ nút quảng cáo, hiển thị rõ phần thưởng một lượt.")]
        [SerializeField] private TMP_Text watchRewardLabel;
        [Tooltip("Nút mua bằng Coin.")]
        [SerializeField] private Button buyButton;
        [Tooltip("Nút nhận booster bằng quảng cáo.")]
        [SerializeField] private Button watchButton;
        [Tooltip("Nút đóng popup.")]
        [SerializeField] private Button closeButton;
        [Tooltip("Icon cho từng booster.")]
        [SerializeField] private Presentation[] presentations;

        private UnityAction closeAction;
        private Vector2 buyPosition;
        private bool capturedLayout;
        private bool isObservingCoin;
        private bool purchaseInProgress;
        private int price;

        // EN: Caches shared feedback managers and binds the three buttons and Coin while visible.
        // VI: Cache manager phản hồi dùng chung và nối ba nút cùng Coin khi đang hiện.
        private void OnEnable()
        {
            CaptureLayout();
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            buyButton.onClick.AddListener(Purchase);
            watchButton.onClick.AddListener(WatchAds);
            closeButton.onClick.AddListener(RequestClose);
            playerData.RegisterResourceChange(ResourceType.Coin, OnCoinChanged);
            isObservingCoin = true;
            OnCoinChanged(playerData.GetResourceValue(ResourceType.Coin));
        }

        // EN: Cancels pending animation and removes only the listeners owned by this opening.
        // VI: Hủy hoạt cảnh còn chờ và chỉ gỡ listener do lần mở này sở hữu.
        private void OnDisable()
        {
            ResetAnimation();
            if (!isObservingCoin) return;
            buyButton.onClick.RemoveListener(Purchase);
            watchButton.onClick.RemoveListener(WatchAds);
            closeButton.onClick.RemoveListener(RequestClose);
            playerData.RemoveResourceChange(ResourceType.Coin, OnCoinChanged);
            isObservingCoin = false;
        }

        // EN: Fills the user's TMP layout from shared booster configuration and opens without runtime profile access in previews.
        // VI: Điền bố cục TMP từ cấu hình booster dùng chung và mở, không truy cập hồ sơ runtime khi xem trước.
        public void Show(BoosterType type, UnityAction close, bool animate = true)
        {
            SelectedType = type;
            closeAction = close;
            purchaseInProgress = false;
            BoosterConfig config = IngameConfig.Instance.Boosters;
            price = config.GetPrice(type);
            boosterName.text = config.GetName(type);
            description.text = config.GetInfo(type);
            priceLabel.text = price.ToString("N0");
            amountLabel.text = $"x{AmountPerPurchase}";
            watchButton.gameObject.SetActive(activeAdsBooster);
            watchRewardLabel.text = $"Watch +{AmountPerAd}";
            CaptureLayout();
            ((RectTransform)buyButton.transform).anchoredPosition = activeAdsBooster ? buyPosition : new Vector2(0f, buyPosition.y);
            Presentation item = Array.Find(presentations, entry => entry.type == type);
            boosterIcon.sprite = item.icon;
            base.Open();
            AnimateOpen(animate);
        }

        // EN: Grants one enabled ad reward, plays its collect cue and closes; WatchAds temporarily invokes success directly.
        // VI: Cấp một lượt thưởng Ads khi bật, phát âm nhận và đóng; WatchAds tạm gọi thành công trực tiếp.
        public void OnWatchAdsSuccess()
        {
            if (!activeAdsBooster || closing || purchaseInProgress || !isActiveAndEnabled || !TryGetResource(SelectedType, out ResourceType resource)) return;
            if (playerData.GetResourceValue(resource) > int.MaxValue - AmountPerAd)
            {
                ShowPurchaseFailed();
                return;
            }
            purchaseInProgress = true;
            playerData.OnAddBooster(resource, AmountPerAd, "watch_ads_booster");
            PlayUISound(IngameSoundManager.Event.RewardCollect);
            AnimateClose(closeAction);
        }

        // EN: Maps offered types to the same resources used by inventory and consumption.
        // VI: Ánh xạ loại booster sang tài nguyên dùng chung cho kho và tiêu hao.
        public static bool TryGetResource(BoosterType type, out ResourceType resource)
        {
            switch (type)
            {
                case BoosterType.PickupHand: resource = ResourceType.BoosterPickup; return true;
                case BoosterType.CleanBrush: resource = ResourceType.BoosterClean; return true;
                case BoosterType.Shuffle: resource = ResourceType.BoosterShuffle; return true;
                case BoosterType.AutoComplete: resource = ResourceType.BoosterAutoComplete; return true;
                default: resource = default; return false;
            }
        }

        // EN: Captures the authored two-button purchase position once, including previews.
        // VI: Ghi vị trí nút mua của bố cục hai nút một lần, gồm cả xem trước.
        private void CaptureLayout()
        {
            if (capturedLayout) return;
            buyPosition = ((RectTransform)buyButton.transform).anchoredPosition;
            capturedLayout = true;
        }

        // EN: Buys the full three-use pack, plays the purchase cue and closes only after success.
        // VI: Mua đủ gói ba lượt, phát âm mua và chỉ đóng sau khi thành công.
        private void Purchase()
        {
            if (closing || purchaseInProgress || !TryGetResource(SelectedType, out ResourceType resource)) return;
            PlayClickFeedback();
            if (playerData.GetResourceValue(ResourceType.Coin) < price)
            {
                Owner.ShowShortMessage(string.Format(IngameConfig.Instance.Boosters.GetShortDes(BoosterConfig.ShortDesType.NotEnoughCoin), price));
                return;
            }
            purchaseInProgress = true;
            if (!playerData.TryBuyBooster(resource, AmountPerPurchase, price, "buy_booster"))
            {
                purchaseInProgress = false;
                ShowPurchaseFailed();
                return;
            }
            PlayUISound(IngameSoundManager.Event.ItemBought);
            AnimateClose(closeAction);
        }

        // EN: Plays feedback and immediately simulates ad success as explicitly requested until an SDK is assigned.
        // VI: Phát phản hồi và tạm gọi thành công quảng cáo ngay theo yêu cầu cho tới khi gán SDK.
        private void WatchAds()
        {
            if (!activeAdsBooster || closing || purchaseInProgress) return;
            PlayClickFeedback();
            OnWatchAdsSuccess();
        }

        // EN: Starts an animated dismissal without changing resources.
        // VI: Bắt đầu đóng có hiệu ứng mà không thay đổi tài nguyên.
        private void RequestClose()
        {
            if (closing) return;
            PlayClickFeedback();
            AnimateClose(closeAction);
        }

        // EN: Updates the optional Coin balance from the same wallet used for both purchase paths.
        // VI: Cập nhật số Coin tùy chọn từ cùng ví dùng cho hai luồng nhận booster.
        private void OnCoinChanged(int value)
        {
            if (goldBalanceLabel != null) goldBalanceLabel.text = value.ToString("N0");
        }

        // EN: Displays the configured transaction failure without closing the offer.
        // VI: Hiện thông báo lỗi giao dịch đã cấu hình mà không đóng đề nghị.
        private void ShowPurchaseFailed()
        {
            Owner.ShowShortMessage(IngameConfig.Instance.Boosters.GetShortDes(BoosterConfig.ShortDesType.PurchaseFailed));
        }

    }
}
