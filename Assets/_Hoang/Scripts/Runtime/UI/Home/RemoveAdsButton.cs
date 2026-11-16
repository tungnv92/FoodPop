using GamePackages;
using SharedModules.ED;
using UnityEngine;

public class RemoveAdsButton : MonoBehaviour
{
    private void OnEnable()
    {
        EventDispatcher.RegisterListener(EventId.PurchaseRemoveAds, OnPurchaseRemoveAds);   
    }

    private void OnDisable()
    {
        EventDispatcher.UnregisterListener(EventId.PurchaseRemoveAds, OnPurchaseRemoveAds);
    }

    // EN: Coordinates Start using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối Start qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    private void Start()
    {
        gameObject.SetActive(!ProfileManager.instance.PlayerDataManager.HasRemoveAds);
    }

    public void OnPurchaseRemoveAds(object args = null)
    {
        gameObject.SetActive(false);
    }
    public void OnClick()
    {
        UIManager.Ins.OpenPopup(PopupId.RemoveAds);
    }
}
