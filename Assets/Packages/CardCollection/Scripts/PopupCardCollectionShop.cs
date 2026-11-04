using System.Collections.Generic;
using GamePackages;
using UnityEngine;

public class PopupCardCollectionShop : Popup
{
    [Tooltip("Danh sách 4 gói thẻ trong cửa hàng (Basic, Rare, Epic, Special).")]
    [SerializeField] private List<CardCollectionPackItemUI> packItems = new();

    private CardCollectionManager Manager => CardCollectionManager.Instance;

    // EN: Opens the Card Pack IAP Shop popup and initializes all 4 card pack items.
    // VI: Mở popup cửa hàng gói thẻ IAP và khởi tạo cả 4 gói thẻ (Basic, Rare, Epic, Special).
    public override void Open(object args = null)
    {
        base.Open(args);
        SetupPacks();
    }

    // EN: Binds each CardCollectionPackItemUI to its corresponding CardPackShopInfo entry in CardCollectionConfig.
    // VI: Gắn từng phần tử CardCollectionPackItemUI với cấu hình CardPackShopInfo tương ứng trong CardCollectionConfig.
    private void SetupPacks()
    {
        if (Manager == null || Manager.config == null || packItems == null) return;
        Manager.config.EnsureDefaultSetup();

        int count = Mathf.Min(packItems.Count, Manager.config.shopPacks.Count);
        for (int i = 0; i < count; i++)
        {
            if (packItems[i] != null)
            {
                packItems[i].Setup(i, Manager.config.shopPacks[i]);
            }
        }
    }

    // EN: Closes the Card Pack Shop popup.
    // VI: Đóng popup cửa hàng gói thẻ.
    public void ButtonClose()
    {
        Close();
    }
}

