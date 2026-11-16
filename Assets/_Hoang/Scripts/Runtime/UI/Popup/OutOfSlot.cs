using GamePackages;
using Longvm.UIAnimator;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OutOfSlot : MonoBehaviour
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;
    [SerializeField] private TMP_Text _priceTmp;
    [SerializeField] private PopupRevive _popupRevive;
    [SerializeField] private GameObject _revive_AddSlot;
    [SerializeField] private GameObject _revive_ClearSlot;

    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;
    GameConfig _config => GameManager.Ins.Config;
    public void Open()
    {
        gameObject.SetActive(true);
        _openAnim.InitAndPlay();
        _priceTmp.text = _config.RevivePrice.ToString();
        //_revive_AddSlot.SetActive(!SlotController.Ins.IsActiveBonusSlot);
        //_revive_ClearSlot.SetActive(SlotController.Ins.IsActiveBonusSlot);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    #region UI Events
    public void ClickCloseButton()
    {
        _closeAnim.Play(() =>
        {
            Close();
            _popupRevive.OnCloseOutOfSlot();
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
