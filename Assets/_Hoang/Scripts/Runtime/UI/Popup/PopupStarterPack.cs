using Longvm.UIAnimator;
using MonetToolkit;
using SharedModules.ED;
using UnityEngine;

public class PopupStarterPack : Popup
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;
    [SerializeField] private Pack _pack;

    public override void Open(object args = null)
    {
        EventDispatcher.RegisterListener(EventId.PurchaseStarterPack, OnPurchaseStarterPack);
        base.Open(args);
        _openAnim.InitAndPlay();
        _pack.Init();
    }

    public void OnPurchaseStarterPack(object arg = null)
    {
        CloseButton();
    }
    public override void Close()
    {
        EventDispatcher.UnregisterListener(EventId.PurchaseStarterPack, OnPurchaseStarterPack);
        base.Close();
    }

    public void CloseButton()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        _closeAnim.Play(Close);
    }
}
