using DG.Tweening;
using GamePackages;
using Longvm.UIAnimator;
using MonetToolkit;
using SharedModules.ED;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

public class PopupRevive : Popup, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;
    [SerializeField] private OutOfSlot _outOfSlot;
    [SerializeField] private AreYouSure _areYouSure;
    [SerializeField] private LoseEvent _loseEvent;
    [SerializeField] private GameObject[] _adsButtons;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _safeAreaRectTf;
    [SerializeField] private Pack beginnerPack;
    [SerializeField] private Pack revivePack;
    [SerializeField] private Pack smallPack;

    GameConfig _config => GameManager.Ins.Config;
    Tween tween;
    public override void Open(object args = null)
    {
        base.Open(args);
        _outOfSlot.gameObject.SetActive(true);
        _areYouSure.gameObject.SetActive(false);
        _loseEvent.gameObject.SetActive(false);

        _openAnim.InitAndPlay();

        //foreach (var button in _adsButtons)
        //{
        //    button.SetActive(!GameplayManager.Ins.IsWatchedAdsRevive && SDKBridge.IsActiveRewardAds);
        //}
        //AudioManager.Ins.PlaySound(SoundType.OutOfSlot);
        _outOfSlot.Open();
        UIManager.Ins.GetPanel().SetActiveCoin(true);
        InitPacks();
        IAPManager.Ins.OnLoseLevel();

        EventDispatcher.RegisterListener(EventId.PurchaseStarterPack, OnPurchaseBeginnerPack);
        EventDispatcher.RegisterListener(EventId.PurchaseRevivePack, OnPurchaseRevivePack);
    }

    void InitPacks()
    {
        beginnerPack.Init();
        revivePack.Init();
        smallPack.Init();

        beginnerPack.gameObject.SetActive(!IAPManager.Ins.Data.StarterPackPurchased);

        bool isShowRevivePack = IAPManager.Ins.Data.StarterPackPurchased && (ProfileManager.instance.GetCurrentLevel() >= IAPManager.Ins.Config.RevivePackEnableLevel) && (IAPManager.Ins.Data.RevivePackTriggerCountdown == 0);
        revivePack.gameObject.SetActive(isShowRevivePack);
        smallPack.gameObject.SetActive(!beginnerPack.gameObject.activeInHierarchy && !revivePack.gameObject.activeInHierarchy);
    }
    public override void Close()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        _closeAnim.Play(base.Close);
        //UIManager.Ins.GetPanel().SetActiveCoin(false);

        EventDispatcher.UnregisterListener(EventId.PurchaseStarterPack, OnPurchaseBeginnerPack);
        EventDispatcher.UnregisterListener(EventId.PurchaseRevivePack, OnPurchaseRevivePack);
    }

    public void ReviveByCoin()
    {
        int level = ProfileManager.instance.PlayerDataManager.GetCurrentLevel();
        int retryCount = ProfileManager.instance.GameFeatureContext.RecordManager.GetRetryCount(level);
        ProfileManager.instance.PlayerDataManager.OnRemoveCoin(_config.RevivePrice, "popup_revive");
        //GameplayManager.Ins.OnRevive();
        //SDKBridge.TrackRevive(level, retryCount, "coin");
        Close();

        UIManager.Ins.GetPanel().SetActiveLife(false);
    }

    public void ReviveByAds()
    {
        UIManager.Ins.GetPanel().SetActiveLife(false);
        //GameplayManager.Ins.IsWatchedAdsRevive = true;
        Close();
    }

    public void RefuseRevive()
    {
        Close();
        UIManager.Ins.OpenPopup(PopupId.Lose, (int)LoseType.OutOfSlot);
        //GameplayManager.Ins.OnGameLose(LoseType.OutOfSlot);
        int level = ProfileManager.instance.PlayerDataManager.GetCurrentLevel();
        int retryCount = ProfileManager.instance.GameFeatureContext.RecordManager.GetRetryCount(level);
        //SDKBridge.TrackRevive(level, retryCount, "lose");
    }

    public void OnCloseOutOfSlot()
    {
        GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
        bool canLoseHeart = !context.HeartManager.IsUnlimitedLives();
        bool canLoseStreak = context.WinStreakManager.IsCanLoseStreak();
        bool canFailLavaQuest = context.LavaQuestManager.CanFailProgress;
        bool canLoseTower = context.TowerRiseManager.ShouldWarningWhenLose();

        if (canLoseHeart)
        {
            //AudioManager.Ins.PlaySound(SoundType.AreYouSure);
            _areYouSure.Open();
            //UIManager.Ins.GetPanel<GamePanel>().LifeDisplay.gameObject.SetActive(true);
            //UIManager.Ins.GetPanel<GamePanel>().LifeDisplay.transform.SetParent(_safeAreaRectTf);
        }
        else if (canLoseStreak || canFailLavaQuest || canLoseTower)
        {
            //AudioManager.Ins.PlaySound(SoundType.AreYouSure);
            _loseEvent.Open();
            //UIManager.Ins.GetPanel<GamePanel>().LifeDisplay.gameObject.SetActive(true);
            //UIManager.Ins.GetPanel<GamePanel>().LifeDisplay.transform.SetParent(_safeAreaRectTf);
        }
        else
        {
            RefuseRevive();
        }
    }
    public void OnCloseAreYouSure()
    {
        GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
        bool canLoseStreak = context.WinStreakManager.IsCanLoseStreak();
        bool canFailLavaQuest = context.LavaQuestManager.CanFailProgress;
        bool canLoseTower = context.TowerRiseManager.ShouldWarningWhenLose();

        if (canLoseStreak || canFailLavaQuest || canLoseTower)
        {
            //AudioManager.Ins.PlaySound(SoundType.AreYouSure);
            _loseEvent.Open();
            //UIManager.Ins.GetPanel<GamePanel>().LifeDisplay.gameObject.SetActive(true);
            //UIManager.Ins.GetPanel<GamePanel>().LifeDisplay.transform.SetParent(_safeAreaRectTf);
        }
        else
        {
            RefuseRevive();
        }
    }

    public void OpenLoseEvent()
    {

    }

    /// <summary>
    /// Revives via the free/ads path, used by <see cref="AutoPlayBot"/> to keep playing without stopping.
    /// </summary>
    public void AutoRevive()
    {
        if (_outOfSlot != null && _outOfSlot.gameObject.activeSelf)
            _outOfSlot.ReviveByAdsButton();
        else if (_areYouSure != null && _areYouSure.gameObject.activeSelf)
            _areYouSure.ReviveByAdsButton();
    }

    public void OnPurchaseBeginnerPack(object args = null)
    {
        beginnerPack.gameObject.SetActive(false);
        revivePack.gameObject.SetActive(true);
    }

    public void OnPurchaseRevivePack(object args = null)
    {
        Close();
        UIManager.Ins.GetPanel().SetActiveLife(false);
        //GameplayManager.Ins.OnRevive();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        tween?.Pause();
        tween = _canvasGroup.DOFade(0, 0.3f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        tween?.Pause();
        tween = _canvasGroup.DOFade(1, 0.3f);
    }
}
