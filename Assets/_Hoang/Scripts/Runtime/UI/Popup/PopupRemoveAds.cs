using Longvm.UIAnimator;
using UnityEngine;

public class PopupRemoveAds : Popup
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;
    [SerializeField] private FoodPop.UI_IAP_Pack _pack;
    [SerializeField] private GameObject _closeButton;

    // EN: Plays the open animation, shows the pack price, listens for the Remove Ads purchase and reveals the close button after one second.
    // VI: Chạy hiệu ứng mở, hiện giá gói, lắng nghe sự kiện mua Xóa quảng cáo và hiện nút đóng sau một giây.
    public override void Open(object args = null)
    {
        Observer.OnBoughtRemoveAdsAction += OnPurchaseRemoveAds;
        base.Open(args);
        _openAnim.InitAndPlay();
        _pack.Init();
        _closeButton.SetActive(false);
        Invoke(nameof(ShowCloseButton), 1f);
    }

    // EN: Reveals the delayed close button.
    // VI: Hiện nút đóng sau khi trễ.
    public void ShowCloseButton()
    {
        _closeButton.SetActive(true);
    }

    // EN: Closes the popup once Remove Ads has been bought.
    // VI: Đóng popup khi đã mua Xóa quảng cáo.
    public void OnPurchaseRemoveAds()
    {
        CloseButton();
    }

    // EN: Closes the popup and stops listening for the Remove Ads purchase.
    // VI: Đóng popup và ngừng lắng nghe sự kiện mua Xóa quảng cáo.
    public override void Close()
    {
        base.Close();
        Observer.OnBoughtRemoveAdsAction -= OnPurchaseRemoveAds;
    }

    // EN: Plays the close animation, then closes the popup.
    // VI: Chạy hiệu ứng đóng rồi đóng popup.
    public void CloseButton()
    {
        _closeAnim.Play(Close);
    }
}
