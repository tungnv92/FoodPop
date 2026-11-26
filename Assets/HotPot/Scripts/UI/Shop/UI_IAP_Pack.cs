using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: One IAP pack in the shop UI: shows the store price and reward amounts from its IAPProductConfig and starts the purchase when its button is clicked.
    // VI: Một gói IAP trên UI cửa hàng: hiện giá store và số lượng thưởng từ IAPProductConfig của gói, và bắt đầu mua khi bấm nút.
    public class UI_IAP_Pack : MonoBehaviour
    {
        // EN: Pairs one reward type with the text that shows its amount.
        // VI: Ghép một loại phần thưởng với chữ hiện số lượng của nó.
        [System.Serializable]
        private struct RewardSlot
        {
            public GamePackages.ResourceType type;
            public TMP_Text amountText;
        }

        [Tooltip("Config riêng của gói: id IAP, loại sản phẩm và phần thưởng.")]
        [SerializeField] private IAPProductConfig config;
        [Tooltip("Nút mua của gói.")]
        [SerializeField] private Button purchaseButton;
        [Tooltip("Chữ hiện giá theo store; khi store chưa sẵn sàng thì hiện giá USD trong config.")]
        [SerializeField] private TMP_Text priceText;
        [Tooltip("Chữ tên gói, lấy từ packName trong config; để trống nếu gói không hiện tên.")]
        [SerializeField] private TMP_Text packNameText;
        [Tooltip("Vị trí mua, dùng để ghi nhận nơi người chơi mua gói.")]
        [SerializeField] private LocationType locationType;
        [Tooltip("Ô hiện số lượng của từng loại phần thưởng; ghép với phần thưởng cùng loại trong config, không phụ thuộc thứ tự.")]
        [SerializeField] private RewardSlot[] rewardSlots;

        // EN: Listens for clicks on the purchase button while the pack is active.
        // VI: Lắng nghe nút mua khi gói đang bật.
        private void OnEnable()
        {
            purchaseButton.onClick.AddListener(OnClickPurchase);
        }

        // EN: Removes the purchase-button listener added in OnEnable.
        // VI: Gỡ listener nút mua đã thêm trong OnEnable.
        private void OnDisable()
        {
            purchaseButton.onClick.RemoveListener(OnClickPurchase);
        }

        // EN: Shows the pack name, the store price (or the config USD price before the store is ready) and the reward amounts.
        // VI: Hiện tên gói, giá store (hoặc giá USD trong config khi store chưa sẵn sàng) và số lượng phần thưởng.
        public void Init()
        {
            if (packNameText != null) packNameText.text = config.packName;
            priceText.text = IAPManager.Instance.GetPrice(config);
            ShowRewardAmounts();
        }
        // EN: Fills each slot with the amount of the config reward of the same type, regardless of order; hides slots whose type the config does not grant.
        // VI: Ghi vào mỗi ô số lượng của phần thưởng cùng loại trong config, không phụ thuộc thứ tự; ẩn ô có loại mà config không có.
        private void ShowRewardAmounts()
        {
            foreach (RewardSlot slot in rewardSlots)
            {
                GamePackages.ResourceGame reward = config.rewards.Find(r => r.type == slot.type);
                slot.amountText.gameObject.SetActive(reward != null);
                if (reward != null) slot.amountText.text = reward.GetNormalValue();
            }
        }

        // EN: Starts the IAP purchase of this pack's product.
        // VI: Bắt đầu mua IAP sản phẩm của gói này.
        private void OnClickPurchase()
        {
            IAPManager.Instance.Buy(config, locationType);
        }
    }
}
