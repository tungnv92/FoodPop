using GamePackages;
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Coin block on the Home screen: shows the Coin balance, follows Coin changes while visible, and opens the shop at the Coin packs when any part of the block is tapped.
    // VI: Khối Coin trên màn Home: hiện số Coin, cập nhật theo thay đổi Coin khi đang hiện, và mở cửa hàng ở phần gói Coin khi bấm vào bất kỳ đâu trong khối.
    [DisallowMultipleComponent]
    public sealed class UIHome_Coin : MonoBehaviour
    {
        [Tooltip("Chữ số Coin hiện có.")]
        [SerializeField] private TMP_Text coinText;
        [Tooltip("Nút phủ cả khối Coin; bấm vào bất kỳ đâu trong khối sẽ mở phần gói Coin của cửa hàng.")]
        [SerializeField] private Button coinButton;

        private PlayerDataManager playerData;

        // EN: Caches player data, binds the block button, listens for Coin changes and shows the current balance.
        // VI: Cache dữ liệu người chơi, nối nút của khối, lắng nghe thay đổi Coin và hiện số dư hiện tại.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            playerData = ProfileManager.instance.PlayerDataManager;
            coinButton.onClick.AddListener(OnClickCoin);
            playerData.RegisterResourceChange(ResourceType.Coin, OnCoinChanged);
            OnCoinChanged(playerData.GetResourceValue(ResourceType.Coin));
        }

        // EN: Removes the block-button and Coin listeners added in OnEnable.
        // VI: Gỡ listener nút của khối và listener Coin đã thêm trong OnEnable.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            coinButton.onClick.RemoveListener(OnClickCoin);
            playerData.RemoveResourceChange(ResourceType.Coin, OnCoinChanged);
        }

        // EN: Shows the current Coin balance.
        // VI: Hiện số Coin hiện có.
        private void OnCoinChanged(int value)
        {
            coinText.text = value.ToString();
        }

        // EN: Plays haptics and opens the shop directly at the Coin packs.
        // VI: Phát rung và mở cửa hàng thẳng vào phần gói Coin.
        private void OnClickCoin()
        {
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
            PopupShop.ShowCoins();
        }
    }
}
