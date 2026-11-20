using GamePackages;
using Longvm.UIAnimator;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.STP;

public class PopupMoreLives : Popup
{
    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;
    [SerializeField] UIAnimator _openAnim;
    [SerializeField] UIAnimator _closeAnim;
    [SerializeField] TMP_Text _priceTmp;
    int price;
    public override void Open(object args = null)
    {
        base.Open(args);
        _openAnim.InitAndPlay();
        price = GameManager.Ins.Config.RefillLivesPrice;
        _priceTmp.text = price.ToString();
    }

    public override void Close()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        _closeAnim.Play(base.Close);
    }

    #region UI Events
    public void CloseButton()
    {
        Close();
    }

    // EN: Refills hearts only when the combined Coin/heart transaction commits successfully.
    // VI: Hồi đầy mạng chỉ khi giao dịch xu/mạng kết hợp được chốt thành công.
    public void BuyByCoin()
    {
        if (PlayerDataManager.TryBuyFullHearts(price, "more_live"))
        {
            Close();
        }
        else
        {
            UIManager.Ins.OpenPopup(PopupId.Shop);
        }
    }

    public void WatchAds()
    {
        //SDKBridge.TrackAdsReward_Click(GamePlacement.MoreLives.ToString(), ButtonPlacement.BtnMoreHeart.ToString(), PlayerDataManager.GetCurrentLevel());
        //SDKBridge.ShowReward(() => OnClaimAdsSuccess(), null);
    }

    // EN: After a rewarded ad, adds one heart through IHeart (skipped without the HeartSystem package), records the ad through optional IRecordTracking and closes.
    // VI: Sau khi xem quảng cáo thưởng, cộng một mạng qua IHeart (bỏ qua khi không có package HeartSystem), ghi nhận quảng cáo qua IRecordTracking tùy chọn và đóng popup.
    private void OnClaimAdsSuccess()
    {
        ProfileManager.instance.Features.Get<IHeart>()?.AddHeart();
        ProfileManager.instance.Features.Get<IRecordTracking>()?.RecordAdsReward();
        //SDKBridge.TrackAdsReward_Complete(GamePlacement.MoreLives.ToString(), ButtonPlacement.BtnMoreHeart.ToString(), PlayerDataManager.GetCurrentLevel(), new List<ResourceGame>() {});
        Close();
    }
    #endregion
}
