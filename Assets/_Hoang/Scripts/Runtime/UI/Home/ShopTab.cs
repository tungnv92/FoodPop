using DG.Tweening;
using MonetToolkit;
using UnityEngine;

public class ShopTab : HomeTab
{
    [SerializeField] RectTransform _contentTf;
    [SerializeField] Shop _shop;
    public override void Start()
    {
        float ratio = Screen.height * 1f / Screen.width;
        if (ratio < BASE_RATIO)
        {
            _rectTf.ChangeSizeDeltaX(_rectTf.sizeDelta.x * BASE_RATIO / ratio);
        }
        else
        {
            //_rectTf.ChangeSizeDeltaX(_rectTf.sizeDelta.x * ratio / BASE_RATIO);
            _rectTf.ChangeSizeDeltaY(_rectTf.sizeDelta.y * ratio / BASE_RATIO);
        }
    }

    public override void OpenTab()
    {
        _contentTf.ChangeAnchorPosY(0);
        //_shop.Init();
    }
    public void ScrollToCoins()
    {
        _contentTf.DOAnchorPosY(_contentTf.sizeDelta.y, 1).SetDelay(0.25f).SetEase(Ease.OutQuad);
    }
}
