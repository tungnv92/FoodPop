using MonetToolkit;
using SharedModules.ED;
using UnityEngine;

public class StarterPackButton : MonoBehaviour
{
    private void OnEnable()
    {
        EventDispatcher.RegisterListener(EventId.PurchaseStarterPack, OnPurchaseStarterPack);
    }

    private void OnDisable()
    {
        EventDispatcher.UnregisterListener(EventId.PurchaseStarterPack, OnPurchaseStarterPack);
    }
    private void Start()
    {
        gameObject.SetActive(!IAPManager.Ins.Data.StarterPackPurchased);
    }

    public void OnPurchaseStarterPack(object arg = null)
    {
        gameObject.SetActive(false);
    }
    public void OnClick()
    {
        UIManager.Ins.OpenPopup(PopupId.StarterPack);
    }
}
