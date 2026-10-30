using GamePackages;
using Longvm.UIAnimator;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AreYouSure : MonoBehaviour
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;
    [SerializeField] private TMP_Text _priceTmp;
    [SerializeField] private PopupRevive _popupRevive;
    [SerializeField] private GameObject _loseLifeObj;

    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;
    GameConfig _config => GameManager.Ins.Config;
    public void Open()
    {
        gameObject.SetActive(true);
        _openAnim.InitAndPlay();
        _priceTmp.text = _config.RevivePrice.ToString();

        GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
        bool canLoseHeart = !context.HeartManager.IsUnlimitedLives();

        _loseLifeObj.SetActive(canLoseHeart);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    #region UI Events
    public void ClickCloseButton()
    {
        GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
        bool canLoseStreak = context.WinStreakManager.IsCanLoseStreak();
        bool canFailLavaQuest = context.LavaQuestManager.CanFailProgress;

        if (canLoseStreak || canFailLavaQuest)
        {
            _popupRevive.OnCloseAreYouSure();
        }
        else
        {
            _popupRevive.RefuseRevive();
        }

        _closeAnim.Play(() =>
        {
            Close();
        });
    }

    public void ReviveByAdsButton()
    {
        //SDKBridge.TrackAdsReward_Click(GamePlacement.Revive.ToString(), ButtonPlacement.BtnRevive.ToString(), PlayerDataManager.GetCurrentLevel());
        //SDKBridge.ShowReward(() => OnWatchedAdsSuccess(), null);
    }

    private void OnWatchedAdsSuccess()
    {
        //ProfileManager.instance.GameFeatureContext.RecordManager.RecordAdsReward();
        //SDKBridge.TrackAdsReward_Complete(GamePlacement.Revive.ToString(), ButtonPlacement.BtnRevive.ToString(), PlayerDataManager.GetCurrentLevel(), new List<ResourceGame>() { });

        //_closeAnim.Play(() =>
        //{
        //    Close();
        //    GameplayManager.Ins.OnRevive();
        //});
        //_popupRevive.ReviveByAds();
    }

    public void ReviveByCoinButton()
    {
        if (ProfileManager.instance.PlayerDataManager.IsEnoughResource(ResourceType.Coin, _config.RevivePrice))
        {
            _closeAnim.Play(() =>
            {
                Close();
            });

            _popupRevive.ReviveByCoin();
        }
        else
        {
            PopupShop popup = UIManager.Ins.OpenPopup(PopupId.Shop) as PopupShop;
            //popup.OnClose(() => UIManager.Ins.GetPanel().SetActiveLife(true));
        }
    }
    #endregion
}
