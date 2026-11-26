using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: No ADS popup opened by FoodPop.UIManager: shows the Remove Ads IAP pack (price from its UIShopPackConfig) and closes itself once Remove Ads is bought.
    // VI: Popup No ADS do FoodPop.UIManager mở: hiện gói IAP Xóa quảng cáo (giá lấy từ UIShopPackConfig của gói) và tự đóng khi đã mua Xóa quảng cáo.
    [DisallowMultipleComponent]
    public sealed class PopupNoAds : UICanvasPrime
    {
        [Tooltip("Gói IAP Xóa quảng cáo hiển thị trong popup.")]
        [SerializeField] private UI_IAP_Pack pack;
        [Tooltip("Nút đóng popup.")]
        [SerializeField] private Button closeButton;

        // EN: Caches feedback managers, binds the close button and listens for the Remove Ads purchase.
        // VI: Cache manager phản hồi, nối nút đóng và lắng nghe sự kiện mua Xóa quảng cáo.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            closeButton.onClick.AddListener(OnClickClose);
            Observer.OnBoughtRemoveAdsAction += OnBought;
        }

        // EN: Removes the listeners added in OnEnable.
        // VI: Gỡ các listener đã thêm trong OnEnable.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            closeButton.onClick.RemoveListener(OnClickClose);
            Observer.OnBoughtRemoveAdsAction -= OnBought;
        }

        // EN: Activates the popup, shows the pack price and plays the shared open animation.
        // VI: Bật popup, hiện giá gói và chạy hiệu ứng mở dùng chung.
        public override void Open()
        {
            base.Open();
            pack.Init();
            AnimateOpen();
        }

        // EN: Closes the popup after Remove Ads has been bought.
        // VI: Đóng popup sau khi đã mua Xóa quảng cáo.
        private void OnBought()
        {
            if (closing) return;
            CloseWithAnimation();
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
