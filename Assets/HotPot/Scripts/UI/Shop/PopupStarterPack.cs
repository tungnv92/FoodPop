using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Starter Pack popup opened by FoodPop.UIManager: shows the starter IAP pack (price and reward amounts from its UIShopPackConfig) and closes itself once the pack is bought.
    // VI: Popup Starter Pack do FoodPop.UIManager mở: hiện gói IAP khởi đầu (giá và số lượng thưởng lấy từ UIShopPackConfig của gói) và tự đóng khi đã mua gói.
    [DisallowMultipleComponent]
    public sealed class PopupStarterPack : UICanvasPrime
    {
        [Tooltip("Gói IAP Starter Pack hiển thị trong popup.")]
        [SerializeField] private UI_IAP_Pack pack;
        [Tooltip("Nút đóng popup.")]
        [SerializeField] private Button closeButton;

        // EN: Caches feedback managers, binds the close button and listens for the starter pack purchase.
        // VI: Cache manager phản hồi, nối nút đóng và lắng nghe sự kiện mua gói khởi đầu.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            closeButton.onClick.AddListener(OnClickClose);
            Observer.OnBoughtStarterPackAction += OnBought;
        }

        // EN: Removes the listeners added in OnEnable.
        // VI: Gỡ các listener đã thêm trong OnEnable.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            closeButton.onClick.RemoveListener(OnClickClose);
            Observer.OnBoughtStarterPackAction -= OnBought;
        }

        // EN: Activates the popup, shows the pack price and rewards, and plays the shared open animation.
        // VI: Bật popup, hiện giá và phần thưởng của gói, rồi chạy hiệu ứng mở dùng chung.
        public override void Open()
        {
            base.Open();
            pack.Init();
            AnimateOpen();
        }

        // EN: Closes the popup after the starter pack has been bought.
        // VI: Đóng popup sau khi đã mua gói khởi đầu.
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
