using GamePackages;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: No ads button on the Home screen: hides itself once Remove Ads is owned and opens the No ADS popup when tapped.
    // VI: Nút No ads trên màn Home: tự ẩn khi đã sở hữu Xóa quảng cáo và mở popup No ADS khi bấm.
    [DisallowMultipleComponent]
    public sealed class UIHome_NoAds : MonoBehaviour
    {
        [Tooltip("Nút No ads.")]
        [SerializeField] private Button noAdsButton;

        private PlayerDataManager playerData;

        // EN: Caches player data, binds the button, listens for the Remove Ads purchase and hides the button when already owned.
        // VI: Cache dữ liệu người chơi, nối nút, lắng nghe sự kiện mua Xóa quảng cáo và ẩn nút nếu đã sở hữu.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            playerData = ProfileManager.instance.PlayerDataManager;
            noAdsButton.onClick.AddListener(OnClickNoAds);
            Observer.OnBoughtRemoveAdsAction += RefreshVisible;
            RefreshVisible();
        }

        // EN: Removes the button and Remove Ads listeners added in OnEnable.
        // VI: Gỡ listener nút và listener Xóa quảng cáo đã thêm trong OnEnable.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            noAdsButton.onClick.RemoveListener(OnClickNoAds);
            Observer.OnBoughtRemoveAdsAction -= RefreshVisible;
        }

        // EN: Hides the button once Remove Ads is owned.
        // VI: Ẩn nút khi đã sở hữu Xóa quảng cáo.
        private void RefreshVisible()
        {
            gameObject.SetActive(!playerData.HasRemoveAds);
        }

        // EN: Plays haptics and opens the No ADS popup.
        // VI: Phát rung và mở popup No ADS.
        private void OnClickNoAds()
        {
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
            UIManager.Instance.OpenUI<PopupNoAds>();
        }
    }
}
