using System.Collections.Generic;
using GamePackages;
using MonetToolkit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardCollectionPackItemUI : MonoBehaviour
{
    [Tooltip("Chỉ số gói thẻ trong cửa hàng (0: Basic, 1: Rare, 2: Epic, 3: Special).")]
    [SerializeField] private int packIndex;

    [Tooltip("Thành phần nút mua IAP tiêu chuẩn (tùy chọn).")]
    [SerializeField] private ButtonPurchase buttonPurchase;

    [Tooltip("Hình ảnh gói thẻ hiển thị.")]
    [SerializeField] private Image packImage;

    [Tooltip("Nhãn tên gói thẻ (ví dụ: Basic Pack, Rare Pack).")]
    [SerializeField] private TMP_Text packNameText;

    [Tooltip("Nhãn hiển thị thông tin chi tiết gói thẻ khi nhấn nút chữ i (tùy chọn).")]
    [SerializeField] private GameObject infoTooltipObject;

    [Tooltip("Nhãn nội dung chi tiết số lượng thẻ và sao đảm bảo của gói.")]
    [SerializeField] private TMP_Text infoTooltipText;

    // EN: Initializes the card pack shop item with its configuration and IAP purchase button.
    // VI: Khởi tạo ô gói thẻ trong cửa hàng với dữ liệu cấu hình và nút thanh toán IAP.
    public void Setup(int index, CardPackShopInfo packInfo)
    {
        packIndex = index;

        if (buttonPurchase != null)
        {
            buttonPurchase.Init();
        }

        if (packInfo != null)
        {
            if (packNameText != null) packNameText.text = packInfo.packName;
            if (packImage != null && packInfo.packSprite != null)
            {
                packImage.sprite = packInfo.packSprite;
            }
            if (infoTooltipText != null)
            {
                int stars = (int)packInfo.guaranteedRarity;
                infoTooltipText.text =
                    $"{packInfo.totalCardsCount} Cards\nGuaranteed {packInfo.guaranteedCount}x {stars}-Star+";
            }
        }

        if (infoTooltipObject != null)
        {
            infoTooltipObject.SetActive(false);
        }
    }

    // EN: Called when the IAP purchase succeeds (or when clicked in test mode) to open the pack and reveal the cards.
    // VI: Được gọi khi thanh toán IAP thành công (hoặc khi nhấn mua trong chế độ kiểm thử) để mở gói và lật thẻ.
    public void OnPurchasePackSuccess()
    {
        CardCollectionManager manager = CardCollectionManager.Instance;
        if (manager == null) return;

        List<CardDropResult> openedCards = manager.OpenCardPack(packIndex);
        if (openedCards != null && openedCards.Count > 0 && UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.CardCollectionReward, openedCards);
        }
    }

    // EN: Toggles the pack odds/contents tooltip when the top-left info (!) icon is clicked.
    // VI: Bật hoặc tắt bảng thông tin tỉ lệ/nội dung gói thẻ khi nhấn biểu tượng (!) ở góc trên bên trái.
    public void OnClickPackInfo()
    {
        if (infoTooltipObject != null)
        {
            infoTooltipObject.SetActive(!infoTooltipObject.activeSelf);
        }
    }
}

