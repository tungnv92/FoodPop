using GamePackages;
using Longvm.UIAnimator;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.STP;

public class PopupMoreLives : Popup
{
    [SerializeField] UIAnimator _openAnim;
    [SerializeField] UIAnimator _closeAnim;
    [SerializeField] TMP_Text _priceTmp;
    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;
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

    public void BuyByCoin()
    {
        if (ProfileManager.instance.PlayerDataManager.IsEnoughResource(ResourceType.Coin, price))
        {
            ProfileManager.instance.PlayerDataManager.OnRemoveCoin(price, "more_live");
            ProfileManager.instance.GameFeatureContext.HeartManager.OnFillFullHeart();
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

    private void OnClaimAdsSuccess()
    {
        ProfileManager.instance.GameFeatureContext.HeartManager.AddHeart();
        ProfileManager.instance.GameFeatureContext.RecordManager.RecordAdsReward();
        //SDKBridge.TrackAdsReward_Complete(GamePlacement.MoreLives.ToString(), ButtonPlacement.BtnMoreHeart.ToString(), PlayerDataManager.GetCurrentLevel(), new List<ResourceGame>() {});
        Close();
    }
    #endregion
}
