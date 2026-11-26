using GamePackages;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Starter pack button on the Home screen: hides itself once the starter pack is bought and opens the Starter Pack popup when tapped.
    // VI: Nút Starter pack trên màn Home: tự ẩn khi đã mua gói khởi đầu và mở popup Starter Pack khi bấm.
    [DisallowMultipleComponent]
    public sealed class UIHome_StarterPack : MonoBehaviour
    {
        [Tooltip("Nút Starter pack.")]
        [SerializeField] private Button starterPackButton;

        private PlayerDataManager playerData;

        // EN: Caches player data, binds the button, listens for the starter pack purchase and hides the button when already bought.
        // VI: Cache dữ liệu người chơi, nối nút, lắng nghe sự kiện mua gói khởi đầu và ẩn nút nếu đã mua.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            playerData = ProfileManager.instance.PlayerDataManager;
            starterPackButton.onClick.AddListener(OnClickStarterPack);
            Observer.OnBoughtStarterPackAction += RefreshVisible;
            RefreshVisible();
        }

        // EN: Removes the button and purchase listeners added in OnEnable.
        // VI: Gỡ listener nút và listener mua gói đã thêm trong OnEnable.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            starterPackButton.onClick.RemoveListener(OnClickStarterPack);
            Observer.OnBoughtStarterPackAction -= RefreshVisible;
        }

        // EN: Hides the button once the starter pack is bought.
        // VI: Ẩn nút khi đã mua gói khởi đầu.
        private void RefreshVisible()
        {
            gameObject.SetActive(!playerData.HasStarterPack);
        }

        // EN: Plays haptics and opens the Starter Pack popup.
        // VI: Phát rung và mở popup Starter Pack.
        private void OnClickStarterPack()
        {
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
            UIManager.Instance.OpenUI<PopupStarterPack>();
        }
    }
}
