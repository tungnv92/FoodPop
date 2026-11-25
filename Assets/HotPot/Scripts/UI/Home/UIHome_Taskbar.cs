using DG.Tweening;
using GamePackages;
using Lofelt.NiceVibrations;
using UnityEngine;

namespace FoodPop
{
    // EN: Home taskbar drawn above the shop: Shop opens the shop as a tab (the shop hides its close button), Home closes the shop, and the selected tab follows the shop's visibility. Leaderboard, Journey and Events have no screen yet and only play haptics.
    // VI: Thanh tab Home vẽ trên cửa hàng: Shop mở cửa hàng như một tab (cửa hàng ẩn nút đóng), Home đóng cửa hàng, và tab đang chọn theo trạng thái hiện của cửa hàng. Leaderboard, Journey và Events chưa có màn nên chỉ phát rung.
    [DisallowMultipleComponent]
    public sealed class UIHome_Taskbar : MonoBehaviour
    {
        [Tooltip("Tab cửa hàng.")]
        [SerializeField] private UIHome_Tab shopTab;
        [Tooltip("Tab bảng xếp hạng.")]
        [SerializeField] private UIHome_Tab leaderboardTab;
        [Tooltip("Tab Home, mặc định được chọn.")]
        [SerializeField] private UIHome_Tab homeTab;
        [Tooltip("Tab hành trình.")]
        [SerializeField] private UIHome_Tab journeyTab;
        [Tooltip("Tab sự kiện.")]
        [SerializeField] private UIHome_Tab eventsTab;
        [Tooltip("Nền chọn dùng chung trượt ngang giữa các tab; chỉ đổi chiều rộng, giữ chiều cao thiết kế.")]
        [SerializeField] private RectTransform selectionBackground;
        [Tooltip("Thời gian chuyển tab, tính bằng giây không phụ thuộc tốc độ game.")]
        [SerializeField, Min(0f)] private float transitionDuration = 0.22f;

        private PlayerDataManager playerData;
        private UIHome_Tab selectedTab;
        private RectTransform selectedTabRect;
        private RectTransform taskbarRect;
        private Tween selectionTween;
        private float selectionProgress;
        private float selectionStartX;
        private float selectionStartWidth;

        // EN: Binds tab actions and a post-layout indicator update, then places the initial selection immediately.
        // VI: Nối hành động tab và cập nhật nền chọn sau layout, rồi đặt trạng thái chọn ban đầu ngay.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            playerData = ProfileManager.instance.PlayerDataManager;
            taskbarRect = (RectTransform)transform;
            shopTab.Button.onClick.AddListener(OnClickShop);
            homeTab.Button.onClick.AddListener(OnClickHome);
            leaderboardTab.Button.onClick.AddListener(OnClickUnavailable);
            journeyTab.Button.onClick.AddListener(OnClickUnavailable);
            eventsTab.Button.onClick.AddListener(OnClickUnavailable);
            PopupShop.VisibleChanged += OnShopVisibleChanged;
            Canvas.willRenderCanvases += UpdateSelectionBackground;
            selectedTab = null;
            Select(UIManager.Instance.IsUICanvasOpened<PopupShop>() ? shopTab : homeTab, false);
        }

        // EN: Removes listeners and cancels the shared background transition when the taskbar hides.
        // VI: Gỡ listener và hủy chuyển động nền chọn dùng chung khi thanh tab bị ẩn.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            shopTab.Button.onClick.RemoveListener(OnClickShop);
            homeTab.Button.onClick.RemoveListener(OnClickHome);
            leaderboardTab.Button.onClick.RemoveListener(OnClickUnavailable);
            journeyTab.Button.onClick.RemoveListener(OnClickUnavailable);
            eventsTab.Button.onClick.RemoveListener(OnClickUnavailable);
            PopupShop.VisibleChanged -= OnShopVisibleChanged;
            Canvas.willRenderCanvases -= UpdateSelectionBackground;
            selectionTween?.Kill();
            selectionTween = null;
        }

        // EN: Selects the Shop tab while the shop is visible, otherwise the Home tab.
        // VI: Chọn tab Shop khi cửa hàng đang hiện, nếu không thì chọn tab Home.
        private void OnShopVisibleChanged(bool visible)
        {
            Select(visible ? shopTab : homeTab);
        }

        // EN: Animates tab visuals together and slides one shared background from its current position toward the new tab.
        // VI: Chuyển động đồng thời giao diện tab và trượt một nền chọn chung từ vị trí hiện tại tới tab mới.
        private void Select(UIHome_Tab selected, bool animate = true)
        {
            if (selectedTab == selected) return;
            selectedTab = selected;
            selectedTabRect = (RectTransform)selected.transform;
            float duration = animate ? transitionDuration : 0f;
            shopTab.SetSelected(shopTab == selected, duration);
            leaderboardTab.SetSelected(leaderboardTab == selected, duration);
            homeTab.SetSelected(homeTab == selected, duration);
            journeyTab.SetSelected(journeyTab == selected, duration);
            eventsTab.SetSelected(eventsTab == selected, duration);
            selectionTween?.Kill();
            selectionTween = null;
            selectionStartX = selectionBackground.anchoredPosition.x;
            selectionStartWidth = selectionBackground.rect.width;
            selectionProgress = duration > 0f ? 0f : 1f;
            if (duration > 0f)
                selectionTween = DOVirtual.Float(0f, 1f, duration, value => selectionProgress = value)
                    .SetEase(Ease.OutCubic).SetUpdate(true)
                    .OnComplete(() => selectionTween = null);
            UpdateSelectionBackground();
        }

        // EN: Follows the selected tab after layout, interpolating only X and width while retaining the authored background height and Y.
        // VI: Bám tab đích sau layout, chỉ nội suy X và chiều rộng, giữ nguyên chiều cao và Y thiết kế của nền.
        private void UpdateSelectionBackground()
        {
            if (selectedTabRect == null) return;
            Vector3 center = taskbarRect.InverseTransformPoint(selectedTabRect.TransformPoint(selectedTabRect.rect.center));
            Vector2 position = selectionBackground.anchoredPosition;
            position.x = Mathf.Lerp(selectionStartX, center.x - taskbarRect.rect.center.x, selectionProgress);
            selectionBackground.anchoredPosition = position;
            selectionBackground.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Lerp(selectionStartWidth, selectedTabRect.rect.width, selectionProgress));
        }

        // EN: Plays haptics and opens the shop as a tab unless it is already shown.
        // VI: Phát rung và mở cửa hàng như một tab nếu chưa hiện.
        private void OnClickShop()
        {
            PlayHaptic();
            if (!UIManager.Instance.IsUICanvasOpened<PopupShop>()) PopupShop.Show();
        }

        // EN: Plays haptics and returns to Home by closing the shop when it is shown.
        // VI: Phát rung và quay về Home bằng cách đóng cửa hàng nếu đang hiện.
        private void OnClickHome()
        {
            PlayHaptic();
            UIManager.Instance.CloseUI<PopupShop>();
        }

        // EN: Plays haptics for tabs whose screen does not exist yet.
        // VI: Phát rung cho các tab chưa có màn.
        private void OnClickUnavailable()
        {
            PlayHaptic();
        }

        // EN: Plays the tap haptic when vibration is enabled.
        // VI: Phát rung khi bấm nếu người chơi bật rung.
        private void PlayHaptic()
        {
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
        }
    }
}
