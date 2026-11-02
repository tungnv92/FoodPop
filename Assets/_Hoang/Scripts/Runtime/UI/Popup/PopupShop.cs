using System;
using DG.Tweening;
using Longvm.UIAnimator;
using MonetToolkit;
using UnityEngine;

public class PopupShop : Popup
{
    [SerializeField] UIAnimator _openAnim;
    [SerializeField] UIAnimator _closeAnim;
    [SerializeField] RectTransform _contentTf;
    [SerializeField] Shop _shop;

    Action onClosed;
    public override void Open(object args = null)
    {
        base.Open(args);
        _openAnim.InitAndPlay();
        //UIManager.Ins.GetPanel().SetActiveCoin(false);
        //UIManager.Ins.GetPanel().SetActiveLife(false);
        _shop.Init();
    }

    public override void Close()
    {
        base.Close();
        onClosed?.Invoke();
        onClosed = null;
        IAPManager.Ins.SetRewardAnimator(UIManager.Ins.GetPanel().RewardAnimator);
    }

    public void ScrollToCoins()
    {
        _contentTf.DOAnchorPosY(_contentTf.sizeDelta.y, 0.25f).SetDelay(0.25f);
    }

    public void OnClose(Action onClose) => this.onClosed = onClose;

    public void CloseButton()
    {
        _closeAnim.Play(Close);
        //UIManager.Ins.GetPanel().SetActiveCoin(true);
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
    }
}
