using GamePackages;
using HomeFeatures.DailyLogin;
using Longvm.RewardAnimator;
using MonetToolkit;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HomePanel : Panel
{
    [SerializeField] RectTransform _rectTf;
    [SerializeField] RectTransform _innerRectTf;
    [SerializeField] RectTransform _refTf;
    [SerializeField] ScrollSnap _scroller;
    [SerializeField] ShopTab _shop;
    [SerializeField] RectTransform _mainSpineTf;
    [SerializeField] HorizontalLayoutGroup _topHorizontal;
    [SerializeField] RectTransform topRectTf;
    [Space]
    [SerializeField] ButtonDailyLogin _buttonDailyLogin;
    [SerializeField] DailyRewardButton _buttonDailyReward;
    [SerializeField] WinStreakHome _winStreakHome;
    [SerializeField] LavaQuestButtonHome _lavaQuestButton;
    [SerializeField] MissionControlButtonHome _missionControlButton;
    [SerializeField] TowerRiseButtonHome _towerRiseButton;
    public CoinDisplay CoinDisplay => _coinDisplay;
    public LifeDisplay LifeDisplay => _lifeDisplay;
    public ShopTab Shop => _shop;
    public WinStreakHome WinStreakHome => _winStreakHome;
    private IEnumerator Start()
    {
        Debug.Log("Size: " + _refTf.rect.size);
        yield return new WaitForEndOfFrame();
        Debug.Log("Size: " + _refTf.rect.size);
        //AudioManager.Ins.PlayMusic(0, MusicType.Home);
        _innerRectTf.sizeDelta = _refTf.rect.size;
        _mainSpineTf.anchoredPosition *= _rectTf.sizeDelta.y / 1920f;
        StartCoroutine(RunHomeSequence());
        _topHorizontal.enabled = false;
    }
    public IEnumerator RunHomeSequence()
    {
        UIManager.Ins.EnableBlocker(true);
        yield return new WaitForSeconds(0.5f);
        //yield return CheckNewVersion();
        yield return CollectRemainsCoin();
        yield return CheckSuggestIapPacks();
        yield return CheckEventsProgress();
        yield return CheckSuggestLiveOps();
        yield return CheckShowTutLiveOps();
        UIManager.Ins.EnableBlocker(false);
    }

    IEnumerator CheckEventsProgress()
    {
        if (_missionControlButton == null)
        {
            _missionControlButton = GetComponentInChildren<MissionControlButtonHome>(true);
        }

        if (_missionControlButton != null)
        {
            _missionControlButton.RefreshState();
        }

        if (_winStreakHome == null)
        {
            _winStreakHome = GetComponentInChildren<WinStreakHome>(true);
        }

        if (_winStreakHome != null && _winStreakHome.HasPendingPoints())
        {
            yield return _winStreakHome.ClaimPendingPoint();
        }

        if (LavaQuestManager.Instance != null)
        {
            if (LavaQuestManager.Instance.ShouldShowWinFlow())
            {
                UIManager.Ins.EnableBlocker(false);
                LavaQuestManager.Instance.ConsumeWinFlow();
                LavaQuest.isWin = true;
                UIManager.Ins.OpenPopup(PopupId.LavaQuest);
                yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.LavaQuest) && !UIManager.Ins.IsPopupOpen(PopupId.LavaQuestClaim));
                yield return RewardAnimator.WaitUntilComplete;
            }
            else if (LavaQuestManager.Instance.ShouldShowLoseFlow())
            {
                UIManager.Ins.EnableBlocker(false);
                LavaQuestManager.Instance.ConsumeLoseFlow();
                LavaQuest.isFail = true;
                UIManager.Ins.OpenPopup(PopupId.LavaQuest);
                yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.LavaQuest));
                yield return RewardAnimator.WaitUntilComplete;
            }
        }

        _towerRiseButton.RunAnim();
        yield return new WaitUntil(() => !_towerRiseButton.IsRunningAnim);
    }

    //IEnumerator CheckNewVersion()
    //{
    //    bool needUpdate = CheckAppVersion();
    //    if (needUpdate)
    //    {
    //        if (ProfileManager.instance.PlayerData.isFirstHomeOpenInDay)
    //        {
    //            PopupNewVersion popupNewVersion = (PopupNewVersion)UIManager.Ins.OpenPopup(PopupId.RequireUpdate);

    //            ProfileManager.instance.PlayerData.isFirstHomeOpenInDay = false;
    //            ProfileManager.instance.SaveData();
    //        }
    //        else yield break;
    //    }

    //    yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.RequireUpdate));
    //}
    IEnumerator CollectRemainsCoin()
    {
        int pendingCoin = ProfileManager.instance.PlayerData.PendingCoinAmount;
        if (pendingCoin > 0)
        {
            bool animDone = false;
            UIManager.Ins.EnableBlocker(true);
            yield return new WaitForSeconds(1);
            RewardAnimator.Init();
            RewardAnimator.Add(0, pendingCoin, onReceiveReward: (() =>
            {
                ProfileManager.instance.PlayerDataManager.OnAddCoin(pendingCoin, "ingame_win");
                ProfileManager.instance.PlayerData.PendingCoinAmount = 0;
                ProfileManager.instance.SaveData();
            }), onAnimCompleted: () =>
            {
                animDone = true;
            });
            RewardAnimator.Play();

            yield return new WaitUntil(() => animDone);
            UIManager.Ins.EnableBlocker(false);
        }
    }

    IEnumerator CheckSuggestIapPacks()
    {
        UIManager.Ins.EnableBlocker(true);

        if (IAPManager.Ins.CheckSuggestRemoveAds())
        {
            UIManager.Ins.EnableBlocker(false);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.RemoveAds));
            yield return RewardAnimator.WaitUntilComplete;
        }

        if (IAPManager.Ins.CheckSuggestStarterPack())
        {
            UIManager.Ins.EnableBlocker(false);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.StarterPack));
            yield return RewardAnimator.WaitUntilComplete;
        }

        UIManager.Ins.EnableBlocker(false);
    }

    IEnumerator CheckSuggestLiveOps()
    {
        UIManager.Ins.EnableBlocker(true);
        if (_buttonDailyLogin.CheckSuggest())
        {
            UIManager.Ins.EnableBlocker(false);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.DailyLogin));
            yield return RewardAnimator.WaitUntilComplete;
        }

        if (_buttonDailyReward.CheckSuggest())
        {
            UIManager.Ins.EnableBlocker(false);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.DailyReward));
            yield return RewardAnimator.WaitUntilComplete;
        }

        UIManager.Ins.EnableBlocker(false);
    }

    IEnumerator CheckShowTutLiveOps()
    {
        UIManager.Ins.EnableBlocker(true);
        if (_winStreakHome.CheckShowTut())
        {
            _winStreakHome.ShowTut();
            yield return new WaitUntil(() => WinStreakManager.Instance.GetData().isShownFirstTut);
            yield return new WaitForSeconds(2);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.WinStreak));
        }

        if (_lavaQuestButton.CheckShowTut())
        {
            yield return new WaitUntil(() => LavaQuestManager.Ins.Data.isDoneTutorial);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.LavaQuest));
        }

        if (_missionControlButton.CheckShowTut())
        {
            yield return new WaitUntil(() => MissionControlManager.Ins.Data.isDoneTutorial);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.MissionControl));
        }

        if (_towerRiseButton.CheckShowTut())
        {
            yield return new WaitUntil(() => TowerRiseManager.Ins.Data.isDoneTutorial);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.TowerRiseIntroduce) && !UIManager.Ins.IsPopupOpen(PopupId.TowerRise));
        }

        UIManager.Ins.EnableBlocker(false);
    }
    public void SnapToPage(int id)
    {
        _scroller.SnapToPage(id);
    }

    public void ResetCoinLife()
    {
        _coinDisplay.transform.SetParent(topRectTf);
        _lifeDisplay.transform.SetParent(topRectTf);
    }

    //bool CheckAppVersion()
    //{
    //    string app_version = Application.version;
    //    Debug.Log("App version: " + app_version);
    //    Version current = new Version(app_version);
    //    Debug.Log("Latest version: " + GameManager.Ins.Config.GameLatestVersion);
    //    Version latest = new Version(GameManager.Ins.Config.GameLatestVersion);

    //    if (current < latest)
    //    {
    //        Debug.Log("Update available");
    //        return true;
    //    }
    //    else
    //    {
    //        Debug.Log("Already latest");
    //        return false;
    //    }
    //}
}
