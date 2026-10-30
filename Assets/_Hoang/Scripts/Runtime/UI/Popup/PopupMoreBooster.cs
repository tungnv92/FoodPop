using FoodPop;
using GamePackages;
using Longvm.RewardAnimator;
using Longvm.UIAnimator;
using MonetToolkit;
using SharedModules.ED;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupMoreBooster : Popup
{
    [SerializeField] UIAnimator openAnim;
    [SerializeField] UIAnimator closeAnim;
    [SerializeField] TMP_Text titleTmp;
    [SerializeField] TMP_Text amountTmp;
    [SerializeField] Image boosterIcon;
    [SerializeField] TMP_Text descTmp;
    [SerializeField] TMP_Text priceTmp;
    [SerializeField] Pack _boosterPack;
    [SerializeField] GameObject _adsButtonObj;

    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;

    BoosterType _boostertype;
    //BoosterConfig _boosterConfig;
    int price;
    public override void Open(object args = null)
    {
        if (args == null)
        {
            Debug.LogError("No Booster ID");
            args = 0;
        }

        //_boosterConfig = BoosterController.Instance.BoosterConfig;
        _boostertype = (BoosterType)(int.Parse(args.ToString()));

        //_adsButtonObj.SetActive(!GameplayManager.Ins.IsWatchedAdsBoosters[(int)_boostertype] && SDKBridge.IsActiveRewardAds);

        //titleTmp.text = _boosterConfig.Boosters[(int)_boostertype].Name.ToString();
        //boosterIcon.sprite = _boosterConfig.Boosters[(int)_boostertype].Icon;
        //boosterIcon.SetNativeSize();
        //descTmp.text = _boosterConfig.Boosters[(int)_boostertype].Description.ToString();

        amountTmp.text = "x3";
        //price = _boosterConfig.Boosters[(int)_boostertype].Price;
        //priceTmp.text = price.ToString();
        base.Open(args);

        _boosterPack.Init();
        _boosterPack.gameObject.SetActive(ProfileManager.instance.GetCurrentLevel() >= 13);
        openAnim.InitAndPlay();
    }

    public override void Close()
    {
        base.Close();
        //UIManager.Ins.GetPanel().SetActiveCoin(false);

    }

    #region UI Events
    public void CloseButton()
    {
        closeAnim.Play(Close);
    }
    public void ClickBuyButton()
    {
        if (ProfileManager.instance.PlayerDataManager.IsEnoughResource(ResourceType.Coin, price))
        {
            int amount = 3;
            ProfileManager.instance.PlayerDataManager.OnRemoveCoin(price, $"popup_more_booster_{_boostertype}");
            //BoosterController.Instance.AddBooster(_boostertype, amount, $"popup_more_booster_{_boostertype}");
            RewardAnimator rwAnimator = UIManager.Ins.GetPanel().RewardAnimator;
            rwAnimator.Init();
            rwAnimator.Add((int)_boostertype + 2, amount, onAnimCompleted: () =>
            {
            });

            closeAnim.Play(() =>
            {
                Close();
                rwAnimator.Play();
            });
        } else
        {
            UIManager.Ins.OpenPopup(PopupId.Shop);
        }
    }

    public void ClickWatchAds()
    {
        //SDKBridge.TrackAdsReward_Click($"popup_more_booster_{_boostertype}", ButtonPlacement.BtnBuyBoosterAds.ToString(), PlayerDataManager.GetCurrentLevel());
        //SDKBridge.ShowReward(() =>
        //{
        //    int amount = 1;
        //    BoosterController.Ins.AddBooster(_boostertype, amount, $"popup_more_booster_{_boostertype}");

        //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
        //    closeAnim.Play(() =>
        //    {
        //        Close();
        //        IngameBoosterUI.Ins.Boosters[(int)_boostertype].Execute(1);
        //    });

        //    GameplayManager.Ins.IsWatchedAdsBoosters[(int)_boostertype] = true;

        //    ProfileManager.instance.GameFeatureContext.RecordManager.RecordAdsReward();
        //    SDKBridge.TrackAdsReward_Complete($"popup_more_booster_{_boostertype}", 
        //        ButtonPlacement.BtnBuyBoosterAds.ToString(), 
        //        PlayerDataManager.GetCurrentLevel(), 
        //        new List<ResourceGame>() { new() { type = ResourceType.BoosterAddSlot + (int)_boostertype, value = 1 } });
        //}, null);
    }
    #endregion
}
