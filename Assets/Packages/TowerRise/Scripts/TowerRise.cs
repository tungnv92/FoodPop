using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TowerRise : Popup
{
    const float BarAnimDuration = 0.3f;
    const string FinishedTitleText = "Tower Rise is finished!";
    const string TowerRiseTitleText = "Win levels to rise up!";
    const string ClaimRewardsTitleText = "You reached the top!";
    const string ReachedTopTitleText = "You reached the top!";
    const string FinishedCountDownText = "Finished!";

    public Action OnClose = () => { };
    public Transform safeAreaTf;
    public TextMeshProUGUI titleText;
    public GameObject btnClaimAllRewards;
    public GameObject btnContinue;
    [Header("BLOCK:")]
    public GameObject blockObj;
    public bool isDoingAnim;
    [Header("TIME:")]
    public TextMeshProUGUI countDownText;
    [Header("TOWER:")]
    public RectTransform contentRectTF;
    public ScrollRect scrollRect;
    [Tooltip("Delay trước khi scroll bắt đầu follow khi bay. 0 = follow ngay.")]
    public float scrollFollowDelay = 0.12f;
    [Tooltip("Độ lag camera follow (SmoothDamp). 0 = bám sát ngay như cũ.")]
    public float scrollFollowSmoothTime = 0.4f;
    [Tooltip("Thời gian scroll trượt mượt về đúng mốc khi kết thúc bay. 0 = dùng scrollFollowSmoothTime.")]
    public float scrollFollowSettleDuration = 0.3f;
    [Header("PLAYER:")]
    public TowerRisePlayer player;
    public Transform playerParent;
    [Tooltip("Bật: cố định X theo playerFixedAnchor. Tắt: theo vị trí progress.")]
    public bool useFixedPlayerAnchor = false;
    public RectTransform playerFixedAnchor;
    [Tooltip("Thời gian bay 1 mạch khi replay win (kể cả qua nhiều checkpoint).")]
    public float continuousWinFlyDuration = 0.85f;
    [Tooltip("Nhịp bay: chậm -> nhanh -> chậm.")]
    public Ease continuousWinFlyEase = Ease.InOutCubic;
    [Tooltip("Tốc độ bay theo world Y (unit/giây).")]
    public float continuousWinFlyWorldSpeed = 420f;
    public float minContinuousWinFlyDuration = 1.1f;
    public float maxContinuousWinFlyDuration = 6f;
    [Header("OPEN ANIM:")]
    [Tooltip("Delay trước khi chạy anim di chuyển khi mở popup (win/fail).")]
    public float openMoveAnimDelay = 0.3f;
    [Header("INTRODUCE:")]
    public float introduceDuration = 2f;
    public float introduceDelay = 0.5f;
    public Ease introduceEase = Ease.OutSine;

    [Header("PROGRESS:")]
    [FormerlySerializedAs("honeyRiseProgress")]
    public TowerRiseProgress towerRiseProgress;
    public TowerRiseProgress honeyRiseProgress => towerRiseProgress;

    TowerRiseManager Manager => TowerRiseManager.Ins;

    int winAnimPlatformIndex;
    float winAnimToStep;
    float winAnimToTotal;
    bool winAnimChainSkipTakeoff;
    float flyScrollElapsed;
    float scrollFollowVelocity;
    Tween openMoveAnimDelayTween;

    public override void Open(object args = null)
    {
        base.Open(args);
        isDoingAnim = false;
        if (blockObj != null) blockObj.SetActive(false);
        RefreshTitleText();
        Manager?.OnInitt();
        RefreshTower();

        if (Manager == null) return;

        if (Manager.towerRiseData != null && !Manager.towerRiseData.isDoneTutorial)
        {
            Manager.towerRiseData.isDoneTutorial = true;
            Manager.towerRiseData.SaveData();
        }

        if (Manager.IsEventFinishedPending())
        {
            OpenFinishedMode();
            return;
        }

        HideFinishedButtons();

        if (Manager.ShouldPlayEventFailAnim())
        {
            PrepareEventDisplay();
            RunAnimFail();
        }
        else if (Manager.ShouldPlayEventWinAnim())
        {
            winAnimChainSkipTakeoff = false;
            towerRiseProgress?.SetDeferClaimStateRefresh(true);
            PrepareEventDisplay(reinitPlayer: true);
            RunAnimWin();
        }
        else
        {
            Canvas.ForceUpdateCanvases();
            if (Manager.pendingIntroduceScroll)
            {
                ScrollToIntroduceTop();
            }
            else
            {
                ScrollToCurrentProgress(false);
            }
            EnsurePlayerReady(syncAnim: true);
            SyncPlayerToArrow();
            Canvas.ForceUpdateCanvases();
            Manager.SnapLastEventDisplayToCurrent();
        }
    }

    void OpenFinishedMode()
    {
        StopActiveAnim();
        Canvas.ForceUpdateCanvases();
        ScrollToCurrentProgress(false);
        EnsurePlayerReady(syncAnim: true);
        SyncPlayerToArrow();
        RefreshFinishedPresentation();
    }

    public void OnEventFinished()
    {
        if (Manager == null || !Manager.IsEventFinishedPending()) return;

        StopActiveAnim();
        RefreshFinishedPresentation();
    }

    public void RefreshFinishedPresentation()
    {
        if (Manager == null || !Manager.IsEventFinishedPending()) return;

        RefreshTitleText();
        if (countDownText != null) countDownText.text = FinishedCountDownText;

        bool hasRewards = Manager.HasFinishedRewardsToClaim();
        if (btnClaimAllRewards != null) btnClaimAllRewards.SetActive(hasRewards);
        if (btnContinue != null) btnContinue.SetActive(!hasRewards);
        if (blockObj != null) blockObj.SetActive(false);
    }

    void StopActiveAnim()
    {
        isDoingAnim = false;
        winAnimChainSkipTakeoff = false;
        openMoveAnimDelayTween?.Kill();
        openMoveAnimDelayTween = null;
        scrollRect?.DOKill();
        towerRiseProgress?.SetDeferClaimStateRefresh(false);
        if (blockObj != null) blockObj.SetActive(false);
    }

    void StartOpenMoveAnimWithDelay(Action onStart)
    {
        openMoveAnimDelayTween?.Kill();
        if (openMoveAnimDelay > 0f)
        {
            openMoveAnimDelayTween = DOVirtual.DelayedCall(openMoveAnimDelay, () =>
            {
                openMoveAnimDelayTween = null;
                onStart?.Invoke();
            }).SetUpdate(true);
            return;
        }

        onStart?.Invoke();
    }

    void HideFinishedButtons()
    {
        if (btnClaimAllRewards != null) btnClaimAllRewards.SetActive(false);
        if (btnContinue != null) btnContinue.SetActive(false);
    }

    void PrepareEventDisplay(bool reinitPlayer = true)
    {
        ApplyEventDisplayState();
        Canvas.ForceUpdateCanvases();
        ScrollToEventDisplay(false);
        EnsurePlayerReady(reinitPlayer, syncAnim: false);
        SyncPlayerToArrow();
        Canvas.ForceUpdateCanvases();
    }

    void RunContinuousWinAnim(bool playTakeoff)
    {
        if (Manager == null || Manager.towerRiseData == null) return;

        float fromTotal = Manager.towerRiseData.lastMilestone;
        float toTotal = Manager.towerRiseData.currentMilestone;

        if (fromTotal >= toTotal)
        {
            OnWinAnimComplete();
            return;
        }

        winAnimPlatformIndex = GetWinAnimPlatformIndex(toTotal);
        winAnimToStep = GetWinAnimStepInMilestone(toTotal);
        winAnimToTotal = toTotal;

        float duration = GetContinuousWinFlyDurationByWorldDistance(fromTotal, toTotal);

        EnsurePlayerReady(reinit: playTakeoff, syncAnim: false);
        if (playTakeoff)
            player?.PlayWin();
        towerRiseProgress?.ApplyVisualFromTotalProgress(fromTotal);
        SyncPlayerToArrow();
        BeginScrollDuringFly(duration, winAnimPlatformIndex, true, playTakeoff);

        if (towerRiseProgress == null)
        {
            FinishProgressAnimEnd(OnWinAnimComplete);
            return;
        }

        Tween progressTween = towerRiseProgress.AnimateTotalProgress(
            fromTotal,
            toTotal,
            duration,
            null,
            () =>
            {
                Canvas.ForceUpdateCanvases();
                SyncPlayerToArrow();
            },
            continuousWinFlyEase);

        if (progressTween == null)
            FinishProgressAnimEnd(OnWinAnimComplete);
        else
            progressTween.OnComplete(() => FinishProgressAnimEnd(OnWinAnimComplete));
    }

    float GetContinuousWinFlyDurationByWorldDistance(float fromTotal, float toTotal)
    {
        if (towerRiseProgress == null)
            return GetContinuousWinFlyDuration();

        Canvas.ForceUpdateCanvases();

        float startY = towerRiseProgress.GetWorldYAtTotalProgress(fromTotal);
        float endY = towerRiseProgress.GetWorldYAtTotalProgress(toTotal);
        float distance = Mathf.Abs(endY - startY);

        if (distance <= 0.001f)
            return minContinuousWinFlyDuration;

        float duration = distance / continuousWinFlyWorldSpeed;
        return Mathf.Clamp(duration, minContinuousWinFlyDuration, maxContinuousWinFlyDuration);
    }

    private void Update()
    {
        if (Manager == null) return;

        if (Manager.IsEventFinishedPending())
        {
            RefreshFinishedPresentation();
            SyncPlayerToArrow();
            return;
        }

        if (countDownText != null) countDownText.text = Manager.GetCountDownString();
        RefreshTitleText();
        RefreshPlatformProgress();
        SyncPlayerToArrow();
    }

    void RefreshPlatformProgress()
    {
        if (isDoingAnim) return;

        if (Manager.ShouldPlayEventWinAnim() || Manager.ShouldPlayEventFailAnim())
        {
            towerRiseProgress?.ApplyVisualFromTotalProgress(Manager.towerRiseData.lastMilestone);
            return;
        }

        towerRiseProgress?.ApplyVisualFromTotalProgress(Manager.GetTotalCurrentWins());
    }

    public void RefreshTower()
    {
        towerRiseProgress?.Refresh();
        RefreshTitleText();
    }

    void RefreshTitleText()
    {
        if (titleText == null || Manager == null) return;

        if (Manager.IsEventFinishedPending())
        {
            titleText.text = FinishedTitleText;
            return;
        }

        if (Manager.IsCompleteAllMileStones())
        {
            titleText.text = HasUnclaimedMilestones()
                ? ClaimRewardsTitleText
                : ReachedTopTitleText;
            return;
        }

        titleText.text = TowerRiseTitleText;
    }

    bool HasUnclaimedMilestones()
    {
        if (Manager == null) return false;

        int count = Manager.GetMileStoneCount();
        for (int i = 0; i < count; i++)
        {
            if (Manager.IsMilestoneClaimable(i)) return true;
        }

        return false;
    }

    RectTransform GetMilestoneFocusRect(int milestoneIndex, float step)
    {
        if (towerRiseProgress == null) return null;

        RectTransform focus = towerRiseProgress.GetStepTarget(milestoneIndex, Mathf.Max(step, 0f));
        if (focus != null) return focus;

        return towerRiseProgress.GetMilestoneCheckpointRect(milestoneIndex);
    }

    int GetMilestoneFocusCount()
    {
        if (towerRiseProgress != null)
            return towerRiseProgress.GetMilestoneUICount();
        return Manager != null ? Manager.GetMileStoneCount() : 0;
    }

    public RectTransform GetScrollFocusRectTF(int platformIndex, bool useProgressFocus)
    {
        float step = useProgressFocus
            && Manager != null
            && !Manager.IsCompleteAllMileStones()
            ? Manager.currentStepinMilestone
            : 0f;

        return GetMilestoneFocusRect(platformIndex, step);
    }

    void ApplyEventDisplayState()
    {
        if (towerRiseProgress == null || Manager == null || Manager.towerRiseData == null) return;

        if (Manager.ShouldPlayEventWinAnim() || Manager.ShouldPlayEventFailAnim())
        {
            towerRiseProgress.ApplyVisualFromTotalProgress(Manager.towerRiseData.lastMilestone);
            return;
        }

        towerRiseProgress.RefreshWithStep(
            Manager.GetEventDisplayPlatformIndex(),
            Manager.GetEventDisplayStep());
    }

    int GetWinAnimPlatformIndex(float totalProgress)
    {
        int count = GetMilestoneFocusCount();
        if (count <= 0) return 0;

        int index = towerRiseProgress != null
            ? towerRiseProgress.GetMilestoneIndexAtTotalProgress(totalProgress)
            : Manager.GetReachedMilestoneIndexFromProgress(Mathf.FloorToInt(totalProgress));
        return Mathf.Clamp(index, 0, count - 1);
    }

    float GetWinAnimStepInMilestone(float totalProgress)
    {
        if (towerRiseProgress != null)
            return towerRiseProgress.GetStepAtTotalProgress(totalProgress);

        return Manager != null ? Manager.GetStepInMilestoneFromProgress(Mathf.FloorToInt(totalProgress)) : 0f;
    }

    void SyncPlayerAnim()
    {
        if (player == null || Manager == null) return;

        if (Manager.currentStepinMilestone > 0)
            player.ClearPreferIdle();

        bool hasProgress = Manager.currentStepinMilestone > 0 || Manager.GetTotalCurrentWins() > 0;
        player.SyncAnimState(hasProgress);
    }

    bool EnsurePlayerReady(bool reinit = true, bool syncAnim = false)
    {
        if (player == null) return false;

        if (reinit)
            player.OnInitt();
        else
            EnsurePlayerParented();

        Transform parent = playerParent != null ? playerParent : transform;
        player.transform.SetParent(parent, false);
        player.transform.SetAsLastSibling();

        if (syncAnim && !isDoingAnim)
            SyncPlayerAnim();

        return true;
    }

    void EnsurePlayerParented()
    {
        if (player == null) return;

        Transform parent = playerParent != null ? playerParent : transform;
        if (player.transform.parent != parent)
        {
            player.transform.SetParent(parent, false);
            player.transform.SetAsLastSibling();
        }
    }

    void SyncPlayerToArrow()
    {
        if (player == null || towerRiseProgress == null) return;

        RectTransform arrow = towerRiseProgress.GetArrowRect();
        if (arrow == null) return;

        Vector3 pos = arrow.position;
        if (useFixedPlayerAnchor && playerFixedAnchor != null)
        {
            pos.x = playerFixedAnchor.position.x;
            pos.z = playerFixedAnchor.position.z;
        }

        player.SyncWorldPosition(pos);
    }

    float GetContinuousWinFlyDuration()
    {
        if (continuousWinFlyDuration > 0f) return continuousWinFlyDuration;
        if (player != null && player.RiseDuration > 0f) return player.RiseDuration;
        return BarAnimDuration;
    }

    public void ScrollToCurrentProgress(bool isAnim)
    {
        if (Manager == null) return;
        ScrollToTotalProgress(Manager.GetTotalCurrentWins(), isAnim, isAnim ? 0.45f : 0f);
    }

    void ScrollToTotalProgress(float totalProgress, bool isAnim, float duration, Action onComplete = null)
    {
        if (towerRiseProgress != null && totalProgress > 0f)
        {
            towerRiseProgress.ApplyVisualFromTotalProgress(totalProgress);
            Canvas.ForceUpdateCanvases();

            RectTransform focus = towerRiseProgress.GetArrowRect();
            if (focus != null)
            {
                ApplyScrollNormalized(GetScrollNormalizedByFocusRect(focus), isAnim, duration, onComplete);
                return;
            }
        }

        if (Manager != null)
        {
            ScrollToPlatform(Manager.GetPlayerPlatformIndex(), isAnim, duration > 0f ? duration : 0.45f, true, onComplete);
        }
    }

    public void Introduce(Action onComplete = null)
    {
        if (scrollRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        isDoingAnim = true;
        if (blockObj != null) blockObj.SetActive(true);

        ScrollToIntroduceTop();
        Canvas.ForceUpdateCanvases();
        SyncPlayerToArrow();

        float target = Manager != null ? GetScrollNormalizedByPlatformIndex(Manager.GetPlayerPlatformIndex(), true) : 0f;
        float duration = introduceDuration > 0f ? introduceDuration : 1f;

        scrollRect.DOKill();
        scrollRect.DOVerticalNormalizedPos(target, duration)
            .SetDelay(introduceDelay)
            .SetEase(introduceEase)
            .SetUpdate(true)
            .OnUpdate(() =>
            {
                Canvas.ForceUpdateCanvases();
                SyncPlayerToArrow();
                if (blockObj != null) blockObj.SetActive(true);
            })
            .OnComplete(() =>
            {
                isDoingAnim = false;
                if (blockObj != null) blockObj.SetActive(false);
                onComplete?.Invoke();
            });
    }

    void ScrollToIntroduceTop()
    {
        int topPlatformIndex = Mathf.Max(0, GetMilestoneFocusCount() - 1);
        RectTransform focusTarget = towerRiseProgress != null
            ? towerRiseProgress.GetMilestoneCheckpointRect(topPlatformIndex)
            : null;

        float normalized = focusTarget != null
            ? GetScrollNormalizedByFocusRect(focusTarget)
            : GetScrollNormalizedByPlatformIndex(topPlatformIndex, false);

        ApplyScrollNormalized(normalized, false, 0f);
    }

    public void ScrollToPlatform(int platformIndex, bool isAnim, float duration, bool useProgressFocus = false, Action onComplete = null)
    {
        if (scrollRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        ApplyScrollNormalized(
            GetScrollNormalizedByPlatformIndex(platformIndex, useProgressFocus),
            isAnim,
            duration,
            onComplete);
    }

    void ScrollToEventDisplay(bool isAnim, float duration = 0.45f, Action onComplete = null)
    {
        if (scrollRect == null || Manager == null)
        {
            onComplete?.Invoke();
            return;
        }

        int platformIndex = Manager.GetEventDisplayPlatformIndex();
        RectTransform focusTarget = towerRiseProgress != null
            && (Manager.ShouldPlayEventWinAnim() || Manager.ShouldPlayEventFailAnim())
            ? towerRiseProgress.GetArrowRect()
            : GetMilestoneFocusRect(platformIndex, Manager.GetEventDisplayStep());
        if (focusTarget == null)
        {
            onComplete?.Invoke();
            return;
        }

        ApplyScrollNormalized(GetScrollNormalizedByFocusRect(focusTarget), isAnim, duration, onComplete);
    }

    void BeginScrollDuringFly(float totalFlyDuration, int platformIndex, bool followProgressArrow, bool playTakeoff = true, Action onComplete = null)
    {
        if (scrollRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (playTakeoff)
            player?.PlayTakeoffScale();

        scrollFollowVelocity = 0f;
        flyScrollElapsed = 0f;
        scrollRect.DOKill();
        DOVirtual.Float(0f, 1f, totalFlyDuration, t =>
            {
                flyScrollElapsed = t * totalFlyDuration;
                Canvas.ForceUpdateCanvases();
                FollowScrollToLiveFocus(platformIndex, followProgressArrow);
            })
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    void FollowScrollToLiveFocus(int platformIndex, bool followProgress)
    {
        if (scrollRect == null) return;

        RectTransform focusTarget;
        if (followProgress && towerRiseProgress != null && towerRiseProgress.GetArrowRect() != null)
            focusTarget = towerRiseProgress.GetArrowRect();
        else
            focusTarget = GetMilestoneFocusRect(platformIndex, 0f);

        if (focusTarget == null) return;

        float target = GetScrollNormalizedByFocusRect(focusTarget);

        if (scrollFollowDelay > 0f && flyScrollElapsed < scrollFollowDelay)
            return;

        if (scrollFollowSmoothTime <= 0f)
        {
            scrollRect.verticalNormalizedPosition = target;
            return;
        }

        float current = scrollRect.verticalNormalizedPosition;
        scrollRect.verticalNormalizedPosition = Mathf.SmoothDamp(
            current,
            target,
            ref scrollFollowVelocity,
            scrollFollowSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
    }

    void SmoothScrollToMilestoneStep(int milestoneIndex, float step, Action onComplete = null)
    {
        if (scrollRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        Canvas.ForceUpdateCanvases();
        float target = GetScrollNormalizedByMilestoneStep(milestoneIndex, step);
        float duration = GetScrollFollowSettleDuration();
        scrollFollowVelocity = 0f;

        if (duration <= 0f)
            return;

        scrollRect.DOKill();
        scrollRect.DOVerticalNormalizedPos(target, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    float GetScrollFollowSettleDuration()
    {
        if (scrollFollowSettleDuration > 0f) return scrollFollowSettleDuration;
        if (scrollFollowSmoothTime > 0f) return scrollFollowSmoothTime;
        return 0f;
    }

    float GetScrollNormalizedByMilestoneStep(int milestoneIndex, float step)
    {
        RectTransform focusTarget = GetMilestoneFocusRect(milestoneIndex, step);
        if (focusTarget == null) return 0f;
        return GetScrollNormalizedByFocusRect(focusTarget);
    }

    void ApplyScrollNormalized(float target, bool isAnim, float duration, Action onComplete = null)
    {
        if (scrollRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (isAnim && contentRectTF != null)
        {
            scrollRect.DOKill();
            contentRectTF.DOAnchorPosY(0, duration).SetEase(introduceEase).OnUpdate(() =>
            {
                if (blockObj != null) blockObj.SetActive(true);
            }).OnComplete(() =>
            {
                if (blockObj != null) blockObj.SetActive(false);
                onComplete?.Invoke();
            });
        }
        else
        {
            scrollRect.verticalNormalizedPosition = target;
            onComplete?.Invoke();
        }
    }

    float GetScrollNormalizedByFocusRect(RectTransform focusTarget)
    {
        if (focusTarget == null || contentRectTF == null) return 0f;
        if (scrollRect == null || scrollRect.viewport == null) return 0f;

        float contentHeight = contentRectTF.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;
        float scrollRange = contentHeight - viewportHeight;
        if (scrollRange <= 0f) return 0f;

        float targetFromTop = -contentRectTF.InverseTransformPoint(focusTarget.position).y;
        float targetFromBottom = contentHeight - targetFromTop;

        float viewportFocusFromBottom;
        if (useFixedPlayerAnchor && playerFixedAnchor != null)
        {
            float anchorFromViewportTop = -scrollRect.viewport.InverseTransformPoint(playerFixedAnchor.position).y;
            viewportFocusFromBottom = viewportHeight - anchorFromViewportTop;
        }
        else
        {
            viewportFocusFromBottom = viewportHeight * 0.5f;
        }

        return Mathf.Clamp01((targetFromBottom - viewportFocusFromBottom) / scrollRange);
    }

    float GetScrollNormalizedByPlatformIndex(int platformIndex, bool useProgressFocus = false)
    {
        int milestoneCount = GetMilestoneFocusCount();
        if (milestoneCount <= 0 || platformIndex < 0 || contentRectTF == null) return 0f;
        if (scrollRect == null || scrollRect.viewport == null) return 0f;

        platformIndex = Mathf.Clamp(platformIndex, 0, milestoneCount - 1);
        RectTransform focusTarget = GetScrollFocusRectTF(platformIndex, useProgressFocus);
        if (focusTarget == null) return 0f;

        return GetScrollNormalizedByFocusRect(focusTarget);
    }

    Tween AnimateProgressWithPlayer(
        int milestoneIndex,
        float fromStep,
        float toStep,
        float duration,
        bool playTakeoff = true,
        bool isWinRise = true,
        Action onComplete = null)
    {
        if (towerRiseProgress == null)
        {
            onComplete?.Invoke();
            return null;
        }

        EnsurePlayerReady(reinit: playTakeoff, syncAnim: false);
        if (playTakeoff)
        {
            if (isWinRise) player?.PlayWin();
            else player?.PlayLose();
        }
        towerRiseProgress.RefreshWithStep(milestoneIndex, fromStep);
        SyncPlayerToArrow();
        BeginScrollDuringFly(duration, milestoneIndex, true, playTakeoff);

        return towerRiseProgress.AnimateSteps(
            milestoneIndex,
            fromStep,
            toStep,
            duration,
            onComplete,
            () =>
            {
                Canvas.ForceUpdateCanvases();
                SyncPlayerToArrow();
            },
            continuousWinFlyEase);
    }

    void FinishProgressAnimEnd(Action onComplete) =>
        FinishProgressAnimEndAtTotal(winAnimToTotal, onComplete);

    void FinishProgressAnimEndAtTotal(float totalProgress, Action onComplete)
    {
        towerRiseProgress?.ApplyVisualFromTotalProgress(totalProgress);
        Canvas.ForceUpdateCanvases();
        SyncPlayerToArrow();
        SmoothScrollToTotalProgress(totalProgress, onComplete);
    }

    void SmoothScrollToTotalProgress(float totalProgress, Action onComplete = null)
    {
        if (scrollRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        RectTransform focusTarget = towerRiseProgress?.GetArrowRect();
        if (focusTarget == null)
        {
            onComplete?.Invoke();
            return;
        }

        float target = GetScrollNormalizedByFocusRect(focusTarget);
        float duration = GetScrollFollowSettleDuration();
        scrollFollowVelocity = 0f;

        if (duration <= 0f)
        {
            scrollRect.verticalNormalizedPosition = target;
            onComplete?.Invoke();
            return;
        }

        scrollRect.DOKill();
        scrollRect.DOVerticalNormalizedPos(target, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void RunAnimWin()
    {
        if (Manager == null || !Manager.ShouldPlayEventWinAnim())
        {
            FinishEventAnim();
            return;
        }

        isDoingAnim = true;
        if (blockObj != null) blockObj.SetActive(true);
        towerRiseProgress?.SetDeferClaimStateRefresh(true);

        bool playTakeoff = !winAnimChainSkipTakeoff;
        winAnimChainSkipTakeoff = true;

        StartOpenMoveAnimWithDelay(() => RunContinuousWinAnim(playTakeoff));
    }

    void OnWinAnimComplete()
    {
        if (Manager == null) return;

        List<int> claimableMilestones = GetWinAnimClaimableMilestones();
        if (claimableMilestones.Count > 0)
        {
            AutoCollectMilestones(claimableMilestones, () =>
            {
                FinalizeWinAnim();
            });
            return;
        }

        FinalizeWinAnim();
    }

    void FinalizeWinAnim()
    {
        if (Manager == null) return;
        towerRiseProgress?.SetDeferClaimStateRefresh(false);
        towerRiseProgress?.RefreshDots(refreshClaimState: true);
        Manager.isWin = false;
        Manager.ClearLastCompletedMilestoneIndex();
        Manager.SnapLastEventDisplayToCurrent();
        FinishWinAnimSequence();
    }

    List<int> GetWinAnimClaimableMilestones()
    {
        var list = new List<int>();
        if (Manager == null || Manager.towerRiseData == null) return list;

        int count = Manager.GetMileStoneCount();
        for (int i = 0; i < count; i++)
        {
            if (Manager.IsMilestoneClaimableAtProgress(i, winAnimToTotal))
            {
                list.Add(i);
            }
        }

        if (Manager.lastCompletedMilestoneIndex >= 0
            && Manager.IsMilestoneClaimable(Manager.lastCompletedMilestoneIndex)
            && !list.Contains(Manager.lastCompletedMilestoneIndex))
        {
            list.Add(Manager.lastCompletedMilestoneIndex);
        }

        return list;
    }

    // EN: Automatically claims completed milestone rewards and plays the reward presentation animation.
    // VI: Tự động nhận phần thưởng của các mốc đã hoàn thành và phát hiệu ứng trình diễn phần thưởng.
    void AutoCollectMilestones(List<int> milestoneIndexes, Action onComplete)
    {
        var allRewards = new List<ResourceGame>();
        foreach (int idx in milestoneIndexes)
        {
            var info = Manager.GetMilestoneInfo(idx);
            if (info?.rewards != null && info.rewards.Count > 0)
            {
                allRewards.AddRange(info.rewards);
            }
            if (!Manager.towerRiseData.claimedIndexes.Contains(idx))
            {
                Manager.towerRiseData.claimedIndexes.Add(idx);
            }
            if (Manager.IsEventFinishedPending())
            {
                Manager.OnFinishedMilestoneClaimed(idx);
            }
        }
        Manager.Save();

        // Refresh dots now that milestones are marked claimed:
        // claim buttons are skipped completely!
        towerRiseProgress?.SetDeferClaimStateRefresh(false);
        towerRiseProgress?.RefreshDots(refreshClaimState: true);

        if (allRewards.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        RewardAnimator rwAnimator = UIManager.Ins != null && UIManager.Ins.GetPanel() != null
            ? UIManager.Ins.GetPanel().RewardAnimator
            : null;

        //if (AudioManager.Ins != null)
        {
            //AudioManager.Ins.PlaySound(SoundType.CollectReward);
        }

        if (rwAnimator != null)
        {
            UIManager.Ins.GetPanel<HomePanel>().CoinDisplay.transform.SetParent(safeAreaTf, true);
            UIManager.Ins.GetPanel<HomePanel>().LifeDisplay.transform.SetParent(safeAreaTf, true);

            rwAnimator.Init(ChestType.None);
            foreach (var reward in allRewards)
            {
                var r = reward;
                rwAnimator.Add(TowerRiseManager.MapRewardToAnimatorType(r.type), r.value, onReceiveReward: () =>
                {
                    TowerRiseManager.ClaimRewardResource(r, "TowerRise");
                });
            }
            rwAnimator.OnComplete(() =>
            {
                onComplete?.Invoke();
            });
            rwAnimator.Play();
        }
        else
        {
            foreach (var reward in allRewards)
            {
                TowerRiseManager.ClaimRewardResource(reward, "TowerRise");
            }
            onComplete?.Invoke();
        }
    }

    void FinishWinAnimSequence()
    {
        winAnimChainSkipTakeoff = false;
        towerRiseProgress?.SetDeferClaimStateRefresh(false);
        towerRiseProgress?.RefreshDots(refreshClaimState: true);
        Canvas.ForceUpdateCanvases();
        EnsurePlayerReady(reinit: true, syncAnim: false);
        if (Manager != null)
        {
            towerRiseProgress?.ApplyVisualFromTotalProgress(Manager.GetTotalCurrentWins());
            SyncPlayerToArrow();
            Manager.SnapLastEventDisplayToCurrent();
        }
        FinishEventAnim();
        player?.PlayWin();
    }

    void FinishEventAnim()
    {
        isDoingAnim = false;
        winAnimChainSkipTakeoff = false;
        towerRiseProgress?.SetDeferClaimStateRefresh(false);
        if (blockObj != null) blockObj.SetActive(false);
    }

    public void RunAnimFail()
    {
        if (Manager == null || !Manager.ShouldPlayEventFailAnim())
        {
            RefreshTower();
            Manager?.SnapLastEventDisplayToCurrent();
            return;
        }

        isDoingAnim = true;
        if (blockObj != null) blockObj.SetActive(true);
        towerRiseProgress?.SetDeferClaimStateRefresh(true);

        int fromStep = Manager.isFail
            ? Manager.failAnimFromStep
            : Manager.GetLastViewedStepInMilestone();

        if (fromStep <= 0 || towerRiseProgress == null)
        {
            FinishFailAnim();
            return;
        }

        float duration = player != null ? player.RiseDuration : BarAnimDuration;

        StartOpenMoveAnimWithDelay(() => AnimateProgressWithPlayer(
            Manager.currentMilestone,
            fromStep,
            0,
            duration,
            playTakeoff: true,
            isWinRise: false,
            FinishFailAnim));
    }

    void FinishFailAnim()
    {
        if (Manager == null) return;
        int settleMilestone = Manager.currentMilestone;
        float settleStep = Manager.currentStepinMilestone;

        Manager.isFail = false;
        Manager.failAnimFromStep = 0;

        player?.TransitionLoseToIdle();

        SmoothScrollToMilestoneStep(settleMilestone, settleStep, FinishFailAnimSequence);
    }

    void FinishFailAnimSequence()
    {
        towerRiseProgress?.SetDeferClaimStateRefresh(false);
        towerRiseProgress?.RefreshDots(refreshClaimState: true);
        Canvas.ForceUpdateCanvases();
        EnsurePlayerReady(reinit: false, syncAnim: false);
        RefreshTower();
        if (Manager != null)
        {
            towerRiseProgress?.ApplyVisual(Manager.currentMilestone, Manager.currentStepinMilestone);
            SyncPlayerToArrow();
            Manager.SnapLastEventDisplayToCurrent();
        }
        FinishEventAnim();
    }

    public void ButtonClaimAllRewards()
    {
        if (Manager == null || !Manager.IsEventFinishedPending() || !Manager.HasFinishedRewardsToClaim()) return;

        HideFinishedButtons();
        Close();
        Manager.BeginFinishedClaimChain();
    }

    public void ButtonContinue()
    {
        if (Manager == null || !Manager.IsEventFinishedPending() || Manager.HasFinishedRewardsToClaim()) return;

        HideFinishedButtons();
        Manager.ResetEvent();
        CloseTowerRise();
    }

    public void ButtonInfo()
    {
        UIManager.Ins.OpenPopup(PopupId.TowerRiseInfo);
    }

    public void ButtonClose()
    {
        if (isDoingAnim) return;

        if (Manager != null && Manager.IsEventFinishedPending() && !Manager.HasFinishedRewardsToClaim())
        {
            Manager.ResetEvent();
        }

        CloseTowerRise();
    }

    void CloseTowerRise()
    {
        if (!isDoingAnim && Manager != null)
            Manager.SnapLastEventDisplayToCurrent();

        UIManager.Ins.GetPanel<HomePanel>().ResetCoinLife();
        Close();
        OnClose?.Invoke();
        OnClose = () => { };
    }
}

public class PopupTowerRise : TowerRise
{
}

public class HoneyRise : TowerRise
{
}

public class PopupHoneyRise : TowerRise
{
}
