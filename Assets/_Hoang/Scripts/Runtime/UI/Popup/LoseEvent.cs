using GamePackages;
using Longvm.UIAnimator;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoseEvent : MonoBehaviour
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;
    [SerializeField] private TMP_Text _priceTmp;
    [SerializeField] private PopupRevive _popupRevive;
    [SerializeField] private GameObject _loseStreakObj;
    [SerializeField] private GameObject _loseLavaObj;
    [SerializeField] private GameObject _loseTowerObj;
    [SerializeField] private Image _loseStreakImage;
    [SerializeField] private Sprite[] _loseStreakSprites;
    GameConfig _config => GameManager.Ins.Config;
    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;

    public void Open()
    {
        gameObject.SetActive(true);
        _openAnim.InitAndPlay();
        _priceTmp.text = _config.RevivePrice.ToString();

        GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
        bool canLoseStreak = context.WinStreakManager.IsCanLoseStreak();
        bool canFailLavaQuest = context.LavaQuestManager.CanFailProgress;
        bool canLoseTower = context.TowerRiseManager.ShouldWarningWhenLose();

        _loseLavaObj.SetActive(canFailLavaQuest);
        _loseStreakObj.SetActive(canLoseStreak);
        _loseTowerObj.SetActive(canLoseTower);
        if (canLoseStreak)
        {
            _loseStreakImage.sprite = _loseStreakSprites[context.WinStreakManager.WinStreakTier];
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    #region UI Events
    public void ClickCloseButton()
    {
        _popupRevive.RefuseRevive();

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

    // EN: Closes the revive prompt only after the full Coin payment succeeds.
    // VI: Chỉ đóng lời nhắc hồi sinh sau khi thanh toán đủ xu thành công.
    public void ReviveByCoinButton()
    {
        if (_popupRevive.ReviveByCoin())
        {
            _closeAnim.Play(() =>
            {
                Close();
            });

        }
        else
        {
            PopupShop popup = UIManager.Ins.OpenPopup(PopupId.Shop) as PopupShop;
            //popup.OnClose(() => UIManager.Ins.GetPanel().SetActiveLife(true));
        }
    }
    #endregion
}
