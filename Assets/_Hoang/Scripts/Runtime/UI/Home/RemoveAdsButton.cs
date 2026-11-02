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

    private void Start()
    {
        gameObject.SetActive(!ProfileManager.instance.PlayerData.isRemoveAds);
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
