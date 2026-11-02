using DG.Tweening;
using GamePackages;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerRiseButtonHome : MonoBehaviour
{
    const float BarAnimDuration = 0.3f;
    const float FillResetDurationRatio = 0.35f;

    [Header("BAR:")]
    public Image fillImg;
    public TextMeshProUGUI countDownText;
    public GameObject lockedObject;
    public GameObject unlockedObject;
    public TMP_Text unlock_tmp;
    [Header("FLY KEY:")]
    public CanvasGroup flyKeyCanvasGroup;
    public RectTransform flyKeyRectTF;
    public TextMeshProUGUI flyKeyAmountText;
    [Header("TUT")]
    [SerializeField] private GameObject tutObj;
    [SerializeField] private GameObject maskObj;
    [SerializeField] private HandTutAnim handAnim;

    private TowerRiseManager Manager => TowerRiseManager.Ins;

    public bool IsRunningAnim { get; private set; }
    Tween scaleTween;
    Tween barStepTween;

    float displayStep = -1f;
    int animSegmentAmount = -1;
    float fillOverride = -1f;

    struct WinAnimRange
    {
        public int fromStep;
        public int toStep;
        public int segmentAmount;
        public bool completedMilestone;
    }

    void OnEnable()
    {
        if (flyKeyCanvasGroup != null) flyKeyCanvasGroup.alpha = 0;
        RefreshState();
    }

    void Start()
    {
        RefreshState();
    }

    void Update()
    {
        ShowDisplay();
    }

    public void RefreshState()
    {
        var manager = TowerRiseManager.Ins;
        if (manager == null || !manager.IsEnabled)
        {
            gameObject.SetActive(false);
            return;
        }

        bool show = manager.ShouldShowHomeButton() || manager.ShouldShowHomeLock();
        gameObject.SetActive(show);

        bool unlocked = manager.IsLevelUnlocked;
        if (unlockedObject != null) unlockedObject.SetActive(unlocked);
        if (lockedObject != null) lockedObject.SetActive(!unlocked);
        if (unlock_tmp != null) unlock_tmp.text = $"Level {manager.UnlockLevel}";
    }

    public bool CheckShowTut()
    {
        if (!gameObject.activeInHierarchy) return false;
        if (Manager == null || !Manager.IsLevelUnlocked) return false;
        if (Manager.Data == null || Manager.Data.isDoneTutorial) return false;
        
        unlockedObject.SetActive(false);
        tutObj.SetActive(true);
        maskObj.SetActive(true);
        handAnim.ShowTouchAnim(transform.position, Vector2.zero, () =>
        {
            OnClick();
            tutObj.SetActive(false);
            unlockedObject.SetActive(true);
            maskObj.SetActive(false);
            UIManager.Ins.EnableBlocker(false);
        });

        return true;
    }

    public void OnClick()
    {
        var manager = TowerRiseManager.Ins;
        if (manager == null || !manager.isActive) return;

        if (manager.IsEventStarted())
        {
            UIManager.Ins.OpenPopup(PopupId.TowerRise);
        }
        else
        {
            UIManager.Ins.OpenPopup(PopupId.TowerRiseIntroduce);
        }
    }

    WinAnimRange GetWinAnimRange(TowerRiseManager manager)
    {
        var range = new WinAnimRange
        {
            completedMilestone = manager.lastCompletedMilestoneIndex >= 0
                && manager.currentStepinMilestone == 0
        };

        if (manager.currentStepinMilestone > 0)
        {
            range.segmentAmount = manager.GetCurrentSegmentWinAmount();
            range.fromStep = manager.currentStepinMilestone - 1;
            range.toStep = manager.currentStepinMilestone;
            return range;
        }

        if (range.completedMilestone)
        {
            var completedInfo = manager.GetMilestoneInfo(manager.lastCompletedMilestoneIndex);
            range.segmentAmount = completedInfo?.winAmount ?? manager.GetCurrentSegmentWinAmount();
            range.fromStep = Mathf.Max(0, range.segmentAmount - 1);
            range.toStep = range.segmentAmount;
            return range;
        }

        range.segmentAmount = manager.GetCurrentSegmentWinAmount();
        return range;
    }

    float GetDisplayStep(TowerRiseManager manager)
    {
        if (displayStep >= 0f) return displayStep;
        if (manager.isWin) return GetWinAnimRange(manager).fromStep;
        if (manager.isFail) return manager.failAnimFromStep;
        return manager.currentStepinMilestone;
    }

    int GetSegmentAmountForDisplay(TowerRiseManager manager)
    {
        if (animSegmentAmount > 0) return animSegmentAmount;
        if (manager.isWin) return GetWinAnimRange(manager).segmentAmount;
        return manager.GetCurrentSegmentWinAmount();
    }

    int GetDisplayNotiNumber(TowerRiseManager manager)
    {
        int noti = manager.GetNotiNumber();
        if (manager.isWin
            && manager.lastCompletedMilestoneIndex >= 0
            && manager.IsMilestoneClaimable(manager.lastCompletedMilestoneIndex))
        {
            noti = Mathf.Max(0, noti - 1);
        }
        return noti;
    }

    float GetSegmentBarFill(TowerRiseManager manager, float step)
    {
        int amount = GetSegmentAmountForDisplay(manager);
        return amount > 0 ? Mathf.Clamp01(step / amount) : 0f;
    }

    void ShowDisplay()
    {
        var manager = TowerRiseManager.Ins;
        if (manager == null) return;

        if (countDownText != null)
        {
            countDownText.text = manager.IsEventFinishedPending()
                ? manager.GetCountDownString()
                : manager.IsEventStarted() || manager.ShouldShowHomeButton()
                    ? manager.GetCountDownString()
                    : "Tower Rise";
        }

        if (fillImg != null)
        {
            if (!manager.ShouldShowHomeButton())
                fillImg.fillAmount = 0f;
            else if (manager.IsEventFinishedPending())
                fillImg.fillAmount = GetSegmentBarFill(manager, manager.currentStepinMilestone);
            else if (fillOverride >= 0f)
                fillImg.fillAmount = fillOverride;
            else
                fillImg.fillAmount = GetSegmentBarFill(manager, GetDisplayStep(manager));
        }
    }

    public void RunAnim()
    {
        var manager = TowerRiseManager.Ins;
        if (manager == null || !manager.IsEventStarted()) return;

        IsRunningAnim = true;
        if (manager.isWin)
        {
            RunAnimWin();
            return;
        }

        if (manager.isFail)
        {
            RunAnimFail();
            return;
        }

        IsRunningAnim = false;
    }

    void RunAnimWin()
    {
        var manager = TowerRiseManager.Ins;
        if (manager == null || !manager.isWin)
        {
            IsRunningAnim = false;
            return;
        }

        WinAnimRange range = GetWinAnimRange(manager);
        if (range.toStep <= range.fromStep)
        {
            if (manager.IsWinAnimCompletingMilestone())
                PlayFillResetAnim(() => FinishWinAnim(manager, true));
            else
                FinishWinAnim(manager, false);
            return;
        }

        FlyWin(() => PlaySegmentBarAnim(range, () => FinishWinAnim(manager, range.completedMilestone)));
    }

    void FinishWinAnim(TowerRiseManager manager, bool completedMilestone)
    {
        manager.isWin = false;
        IsRunningAnim = false;
        if (completedMilestone) Scale();
    }

    void RunAnimFail()
    {
        var manager = TowerRiseManager.Ins;
        if (manager == null || !manager.isFail)
        {
            IsRunningAnim = false;
            return;
        }

        int fromStep = manager.failAnimFromStep;
        if (fromStep <= 0)
        {
            FinishFailAnim(manager);
            return;
        }

        Shake();
        PlayFailBarAnim(fromStep, manager.GetCurrentSegmentWinAmount(), () => FinishFailAnim(manager));
    }

    void FinishFailAnim(TowerRiseManager manager)
    {
        manager.isFail = false;
        manager.failAnimFromStep = 0;
        IsRunningAnim = false;
    }

    void PlayFailBarAnim(int fromStep, int segmentAmount, Action onComplete)
    {
        barStepTween?.Kill();
        displayStep = fromStep;
        animSegmentAmount = segmentAmount;
        fillOverride = -1f;

        UIManager.Ins.EnableBlocker(true);
        barStepTween = DOVirtual.Float(fromStep, 0f, BarAnimDuration, v => displayStep = v)
            .SetEase(Ease.InSine)
            .OnComplete(() => EndBarAnim(onComplete));
    }

    void PlaySegmentBarAnim(WinAnimRange range, Action onComplete)
    {
        barStepTween?.Kill();
        displayStep = range.fromStep;
        animSegmentAmount = range.segmentAmount;
        fillOverride = -1f;

        UIManager.Ins.EnableBlocker(true);
        barStepTween = DOVirtual.Float(range.fromStep, range.toStep, BarAnimDuration, v => displayStep = v)
            .SetEase(Ease.OutSine)
            .OnComplete(() =>
            {
                var manager = TowerRiseManager.Ins;
                if (range.completedMilestone
                    || (manager != null && manager.IsCompleteAllMileStones()))
                {
                    PlayFillResetAnim(onComplete);
                    return;
                }

                EndBarAnim(onComplete);
            });
    }

    void PlayFillResetAnim(Action onComplete)
    {
        var manager = TowerRiseManager.Ins;
        animSegmentAmount = manager != null ? manager.GetCurrentSegmentWinAmount() : animSegmentAmount;
        displayStep = 0;
        fillOverride = 1f;

        barStepTween = DOVirtual.Float(1f, 0f, BarAnimDuration * FillResetDurationRatio, v => fillOverride = v)
            .SetEase(Ease.InSine)
            .OnComplete(() => EndBarAnim(onComplete));
    }

    void EndBarAnim(Action onComplete)
    {
        displayStep = -1f;
        animSegmentAmount = -1;
        fillOverride = -1f;
        barStepTween = null;
        UIManager.Ins.EnableBlocker(false);
        onComplete?.Invoke();
    }

    void FlyWin(Action onComplete)
    {
        if (flyKeyCanvasGroup == null || flyKeyRectTF == null)
        {
            Scale();
            onComplete?.Invoke();
            return;
        }

        UIManager.Ins.EnableBlocker(true);

        if (flyKeyAmountText != null)
            flyKeyAmountText.text = "+1";

        flyKeyRectTF.anchoredPosition = new Vector2(-200, 0);
        flyKeyCanvasGroup.alpha = 1;

        const float scaleDuration = 0.4f;
        const float moveDuration = 0.2f;
        Vector3 smallScale = Vector3.one * 0.7f;

        flyKeyRectTF.localScale = smallScale;
        flyKeyRectTF.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutBack).OnComplete(() =>
        {
            flyKeyRectTF.DOAnchorPos(Vector2.zero, moveDuration).SetEase(Ease.InQuad).OnComplete(() =>
            {
                Scale();
                UIManager.Ins.EnableBlocker(false);
                onComplete?.Invoke();
            });
            flyKeyRectTF.DOScale(smallScale, moveDuration).SetEase(Ease.Linear);
            flyKeyCanvasGroup.DOFade(0f, 0.02f).SetDelay(moveDuration - 0.02f);
        });
    }

    void Scale()
    {
        scaleTween?.Kill();
        transform.localScale = Vector3.one;
        scaleTween = transform.DOScale(1.08f, 0.1f).SetLoops(2, LoopType.Yoyo);
    }

    void Shake()
    {
        scaleTween?.Kill();
        transform.localScale = Vector3.one;
        scaleTween = transform.DOShakeScale(0.35f, 0.12f, 12, 90f, true).OnComplete(() =>
        {
            transform.localScale = Vector3.one;
        });
    }
}

public class HoneyRiseButtonHome : TowerRiseButtonHome
{
}
