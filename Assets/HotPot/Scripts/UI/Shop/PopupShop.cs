using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Full-screen shop popup opened by FoodPop.UIManager: initializes its IAP packs, hides the Remove Ads section once owned, opens either at the top or directly at the Coin packs, and opens/closes instantly. Opened over the Home screen it acts as a taskbar tab: it draws under the Home taskbar and hides its close button.
    // VI: Popup cửa hàng toàn màn hình do FoodPop.UIManager mở: khởi tạo các gói IAP, ẩn mục Xóa quảng cáo khi đã sở hữu, mở ở đầu danh sách hoặc thẳng vào phần gói Coin, và mở/đóng ngay. Khi mở trên màn Home nó là một tab của thanh dưới: vẽ dưới thanh tab Home và ẩn nút đóng.
    [DisallowMultipleComponent]
    public sealed class PopupShop : UICanvasPrime
    {
        // EN: Raised with true when the shop becomes visible and false when it hides, so the Home taskbar can follow the selected tab.
        // VI: Phát true khi cửa hàng hiện và false khi ẩn, để thanh tab Home theo kịp tab đang chọn.
        public static event System.Action<bool> VisibleChanged;

        [Tooltip("Nội dung cuộn chứa các gói.")]
        [FormerlySerializedAs("_contentTf")]
        [SerializeField] private RectTransform contentTf;
        [Tooltip("ScrollRect của danh sách gói, dùng để dừng quán tính khi nhảy tới phần Coin.")]
        [SerializeField] private ScrollRect scrollRect;
        [Tooltip("Tiêu đề mục gói Coin, phải là con trực tiếp của nội dung cuộn; mở thẳng phần Coin sẽ cuộn để tiêu đề này nằm ở đầu khung nhìn.")]
        [SerializeField] private RectTransform coinSection;
        [Tooltip("Nút đóng popup.")]
        [SerializeField] private Button closeButton;
        [Tooltip("Các gói IAP trong cửa hàng, được khởi tạo mỗi lần mở popup.")]
        [SerializeField] private UI_IAP_Pack[] packs;
        [Tooltip("Nhóm gói Xóa quảng cáo, ẩn khi người chơi đã mua Xóa quảng cáo.")]
        [SerializeField] private GameObject removeAdsGroup;
        [Tooltip("Tiêu đề mục Xóa quảng cáo, ẩn cùng nhóm gói.")]
        [SerializeField] private GameObject removeAdsHeader;
        [Tooltip("Thứ tự vẽ của cửa hàng khi mở như một tab trên màn Home; phải nhỏ hơn thứ tự vẽ của thanh tab Home để thanh tab nằm trên.")]
        [SerializeField] private int tabSortingOrder = 10;

        private int popupSortingOrder;

        // EN: Caches feedback managers and the authored sorting order, binds the close button, listens for the Remove Ads purchase and announces that the shop is visible.
        // VI: Cache manager phản hồi và thứ tự vẽ gốc, nối nút đóng, lắng nghe sự kiện mua Xóa quảng cáo và báo cửa hàng đang hiện.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            if (popupSortingOrder == 0) popupSortingOrder = Canvas.sortingOrder;
            closeButton.onClick.AddListener(OnClickClose);
            Observer.OnBoughtRemoveAdsAction += RefreshRemoveAds;
            VisibleChanged?.Invoke(true);
        }

        // EN: Removes the listeners added in OnEnable and announces that the shop is hidden.
        // VI: Gỡ các listener đã thêm trong OnEnable và báo cửa hàng đã ẩn.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            closeButton.onClick.RemoveListener(OnClickClose);
            Observer.OnBoughtRemoveAdsAction -= RefreshRemoveAds;
            VisibleChanged?.Invoke(false);
        }

        // EN: Opens the shop normally, with the pack list at the top.
        // VI: Mở cửa hàng bình thường, danh sách gói ở đầu.
        public static PopupShop Show()
        {
            return UIManager.Instance.OpenUI<PopupShop>();
        }

        // EN: Opens the shop directly at the Coin packs section.
        // VI: Mở cửa hàng thẳng vào phần gói Coin.
        public static PopupShop ShowCoins()
        {
            PopupShop shop = Show();
            shop.JumpToCoins();
            return shop;
        }

        // EN: Activates the popup at the top of the list (as a Home tab when the Home screen is open), initializes every pack and hides Remove Ads when owned; as a full-screen popup it appears at once without the scale-in animation.
        // VI: Bật popup ở đầu danh sách (là một tab Home khi màn Home đang mở), khởi tạo mọi gói và ẩn Xóa quảng cáo nếu đã sở hữu; là popup toàn màn hình nên hiện ngay, không có hiệu ứng phóng to.
        public override void Open()
        {
            base.Open();
            ApplyTabMode(Owner.IsUICanvasOpened<PopupHome>());
            scrollRect.StopMovement();
            contentTf.anchoredPosition = new Vector2(contentTf.anchoredPosition.x, 0f);
            foreach (UI_IAP_Pack pack in packs)
            {
                pack.Init();
            }
            RefreshRemoveAds();
            AnimateOpen(false);
        }

        // EN: As a Home tab the shop draws under the Home taskbar and hides its close button (the taskbar switches tabs); otherwise it is a normal top popup with a close button.
        // VI: Khi là tab Home, cửa hàng vẽ dưới thanh tab Home và ẩn nút đóng (thanh tab dùng để chuyển tab); nếu không thì là popup bình thường trên cùng, có nút đóng.
        private void ApplyTabMode(bool tabMode)
        {
            Canvas.sortingOrder = tabMode ? tabSortingOrder : popupSortingOrder;
            closeButton.gameObject.SetActive(!tabMode);
        }

        // EN: Shows the Remove Ads section only while the player does not own Remove Ads.
        // VI: Chỉ hiện mục Xóa quảng cáo khi người chơi chưa sở hữu Xóa quảng cáo.
        private void RefreshRemoveAds()
        {
            bool show = !playerData.HasRemoveAds;
            removeAdsGroup.SetActive(show);
            removeAdsHeader.SetActive(show);
        }

        // EN: Rebuilds the layout and scrolls instantly so the Coin section header sits at the top of the viewport, clamped to the scrollable range.
        // VI: Dựng lại layout và cuộn ngay để tiêu đề phần Coin nằm ở đầu khung nhìn, giới hạn trong khoảng cuộn được.
        private void JumpToCoins()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentTf);
            scrollRect.StopMovement();
            float sectionTop = coinSection.localPosition.y + coinSection.rect.yMax;
            float offset = contentTf.rect.yMax - sectionTop;
            float maxOffset = Mathf.Max(0f, contentTf.rect.height - scrollRect.viewport.rect.height);
            contentTf.anchoredPosition = new Vector2(contentTf.anchoredPosition.x, Mathf.Clamp(offset, 0f, maxOffset));
        }

        // EN: Closes the full-screen popup immediately, without the shrink-out animation.
        // VI: Đóng popup toàn màn hình ngay, không có hiệu ứng thu nhỏ.
        private void OnClickClose()
        {
            if (closing) return;
            PlayHaptic();
            Close();
        }
    }
}
