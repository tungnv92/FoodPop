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
    [SerializeField] CardCollectionButtonHome _cardCollectionButton;
    public CoinDisplay CoinDisplay => _coinDisplay;
    public LifeDisplay LifeDisplay => _lifeDisplay;
    public ShopTab Shop => _shop;
    public WinStreakHome WinStreakHome => _winStreakHome;

    private const float BASE_RATIO = 1920f / 1080f;
    private bool _isInnerResized;

    // EN: Initializes the home panel, resizes the inner container to fit the screen aspect ratio, and starts the home sequence.
    // VI: Khởi tạo panel màn hình chính, điều chỉnh kích thước khung chứa bên trong theo tỉ lệ màn hình và chạy chuỗi sự kiện mở đầu.
    private IEnumerator Start()
    {
        ResizeInnerContainer();
        yield return new WaitForEndOfFrame();
        //AudioManager.Ins.PlayMusic(0, MusicType.Home);
        if (_mainSpineTf != null)
        {
            _mainSpineTf.anchoredPosition *= _rectTf.sizeDelta.y / 1920f;
        }
        StartCoroutine(RunHomeSequence());
        if (_topHorizontal != null)
        {
            _topHorizontal.enabled = false;
        }
    }

    // EN: Resizes the 1080x1920 inner container once to match the visible screen dimensions and safe area on any aspect ratio.
    // VI: Điều chỉnh kích thước khung chứa 1080x1920 bên trong một lần để khớp với màn hình hiển thị và vùng an toàn trên mọi tỉ lệ màn hình.
    public void ResizeInnerContainer()
    {
        if (_isInnerResized || _innerRectTf == null) return;
        _isInnerResized = true;

        float ratio = Screen.height * 1f / Screen.width;
        if (ratio < BASE_RATIO)
        {
            _innerRectTf.ChangeSizeDeltaX(_innerRectTf.sizeDelta.x * BASE_RATIO / ratio);
        }
        else
        {
            _innerRectTf.ChangeSizeDeltaY(_innerRectTf.sizeDelta.y * ratio / BASE_RATIO);
        }

        ApplySafeAreaToInner();
    }

    // EN: Applies Screen.safeArea normalized bounds (following SafeArea.cs) to the inner container's sizeDelta and anchoredPosition.
    // VI: Áp dụng giới hạn chuẩn hóa của Screen.safeArea (theo phương pháp của SafeArea.cs) vào sizeDelta và anchoredPosition của khung chứa bên trong.
    private void ApplySafeAreaToInner()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        if (anchorMin.x >= 0f && anchorMin.y >= 0f && anchorMax.x >= 0f && anchorMax.y >= 0f)
        {
            Vector2 fullScreenSize = _innerRectTf.sizeDelta;
            Vector2 centerOffset = (anchorMin + anchorMax) * 0.5f - new Vector2(0.5f, 0.5f);

            _innerRectTf.sizeDelta = new Vector2(
                fullScreenSize.x * (anchorMax.x - anchorMin.x),
                fullScreenSize.y * (anchorMax.y - anchorMin.y));
            _innerRectTf.anchoredPosition = new Vector2(
                fullScreenSize.x * centerOffset.x,
                fullScreenSize.y * centerOffset.y);
        }
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

    // EN: Refreshes event home buttons and runs any pending win/lose or card-drop sequences upon returning to Home.
    // VI: Cập nhật các nút sự kiện trên màn hình chính và chạy các chuỗi thắng/thua hoặc nhận thẻ đang chờ khi về Home.
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

        if (_towerRiseButton != null)
        {
            _towerRiseButton.RunAnim();
            yield return new WaitUntil(() => !_towerRiseButton.IsRunningAnim);
        }

        if (_cardCollectionButton == null)
        {
            _cardCollectionButton = GetComponentInChildren<CardCollectionButtonHome>(true);
        }

        if (_cardCollectionButton != null)
        {
            _cardCollectionButton.RefreshState();
            if (_cardCollectionButton.HasPendingCardDrop())
            {
                yield return _cardCollectionButton.PresentPendingCardDrop();
            }
        }
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

    // EN: Checks and displays first-time unlock tutorials for active LiveOps events on the Home screen.
    // VI: Kiểm tra và hiển thị hướng dẫn mở khóa lần đầu cho các sự kiện LiveOps đang hoạt động trên màn hình chính.
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

        if (_towerRiseButton != null && _towerRiseButton.CheckShowTut())
        {
            yield return new WaitUntil(() => TowerRiseManager.Ins.Data.isDoneTutorial);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.TowerRiseIntroduce) && !UIManager.Ins.IsPopupOpen(PopupId.TowerRise));
        }

        if (_cardCollectionButton != null && _cardCollectionButton.CheckShowTut())
        {
            yield return new WaitUntil(() => CardCollectionManager.Ins.Data.isDoneTutorial);
            yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.CardCollectionInfo) && !UIManager.Ins.IsPopupOpen(PopupId.CardCollection));
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
