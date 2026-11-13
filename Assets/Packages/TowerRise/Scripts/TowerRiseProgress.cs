using DG.Tweening;
using GamePackages;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerRiseProgress : MonoBehaviour
{
    [Header("BAR:")]
    public Image lineProgress;
    public RectTransform arrowRectf;
    public TMP_Text textProgress;
    [Header("MILESTONES:")]
    public List<TowerRiseMileStoneProgressUI> mileStoneProgressUIs = new();
    public List<Sprite> listChestSprites;
    public List<Sprite> listChestOpenedSprites;
    TowerRiseManager Manager => TowerRiseManager.Ins;
    RectTransform TrackRect => lineProgress != null ? lineProgress.rectTransform : null;

    void Awake()
    {
        if (lineProgress == null) return;

        lineProgress.type = Image.Type.Filled;
        lineProgress.fillMethod = Image.FillMethod.Vertical;
        lineProgress.fillOrigin = (int)Image.OriginVertical.Bottom;
    }

    // EN: Coordinates Refresh using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối Refresh qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void Refresh()
    {
        if (Manager == null) return;

        int count = Mathf.Min(mileStoneProgressUIs.Count, Manager.GetMileStoneCount());
        for (int i = 0; i < count; i++)
        {
            Sprite rewardSp = (listChestSprites != null && i < listChestSprites.Count) ? listChestSprites[i] : null;
            Sprite claimedSp = (listChestOpenedSprites != null && i < listChestOpenedSprites.Count) ? listChestOpenedSprites[i] : null;
            mileStoneProgressUIs[i].OnInitt(i, Manager.GetMilestoneInfo(i), rewardSp, claimedSp);
        }

        if (Manager.ShouldPlayEventWinAnim() || Manager.ShouldPlayEventFailAnim())
            ApplyVisualFromTotalProgress(Manager.LastViewedProgress);
        else
            ApplyVisualFromTotalProgress(Manager.GetTotalCurrentWins());
    }

    public void RefreshWithStep(int milestoneIndex, float step) =>
        ApplyVisual(milestoneIndex, step);

    public void ApplyVisual(int milestoneIndex, float step)
    {
        float totalProgress = GetTotalProgressFromSegment(milestoneIndex, step);
        int resolvedIndex = GetReachedMilestoneIndexAtProgress(totalProgress);
        float resolvedStep = GetStepFromTotalProgress(totalProgress);
        float worldY = ResolveProgressWorldY(resolvedIndex, resolvedStep, totalProgress);

        ApplyArrow(worldY, totalProgress > 0f);
        ApplyLineFill(worldY, totalProgress);
        ApplyProgressTextFromTotal(totalProgress);
        RefreshDots(refreshClaimState: !deferClaimStateRefresh);
    }

    bool deferClaimStateRefresh;
    float lastDotsAnimProgress = -1f;

    public void SetDeferClaimStateRefresh(bool defer) => deferClaimStateRefresh = defer;

    public void ResetDotsAnimTracking(float totalProgress) => lastDotsAnimProgress = totalProgress;

    public bool IsArrowVisible =>
        arrowRectf != null && arrowRectf.gameObject.activeSelf;

    public RectTransform GetArrowRect() => arrowRectf;

    public RectTransform GetStepTarget(int milestoneIndex, float step)
    {
        if (arrowRectf == null) return null;

        float totalProgress = GetTotalProgressFromSegment(milestoneIndex, step);
        int resolvedIndex = GetReachedMilestoneIndexAtProgress(totalProgress);
        float resolvedStep = GetStepFromTotalProgress(totalProgress);
        float worldY = ResolveProgressWorldY(resolvedIndex, resolvedStep, totalProgress);
        Vector3 pos = arrowRectf.position;
        arrowRectf.position = new Vector3(pos.x, worldY, pos.z);
        return arrowRectf;
    }

    public float GetStepAtTotalProgress(float totalProgress) => GetStepFromTotalProgress(totalProgress);

    public int GetMilestoneIndexAtTotalProgress(float totalProgress) =>
        GetReachedMilestoneIndexAtProgress(totalProgress);

    public Tween AnimateSteps(
        int milestoneIndex,
        float fromStep,
        float toStep,
        float duration,
        Action onComplete = null,
        Action onUpdate = null,
        Ease ease = Ease.InOutCubic)
    {
        ApplyVisual(milestoneIndex, fromStep);
        ResetDotsAnimTracking(GetTotalProgressFromSegment(milestoneIndex, fromStep));

        return DOVirtual.Float(fromStep, toStep, duration, v =>
            {
                ApplyVisual(milestoneIndex, v);
                RefreshDotsAtSegmentProgress(milestoneIndex, v, detectMilestoneCrossing: true);
                onUpdate?.Invoke();
            })
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void ApplyVisualFromTotalProgress(float totalProgress, bool detectMilestoneCrossing = false)
    {
        if (Manager == null) return;

        int milestoneIndex = GetReachedMilestoneIndexAtProgress(totalProgress);
        float step = GetStepFromTotalProgress(totalProgress);
        float worldY = ResolveProgressWorldY(milestoneIndex, step, totalProgress);

        ApplyArrow(worldY, totalProgress > 0f);
        ApplyLineFill(worldY, totalProgress);
        ApplyProgressTextFromTotal(totalProgress);
        RefreshDotsAtProgress(totalProgress, detectMilestoneCrossing);
    }

    public Tween AnimateTotalProgress(
        float fromTotal,
        float toTotal,
        float duration,
        Action onComplete = null,
        Action onUpdate = null,
        Ease ease = Ease.InOutCubic)
    {
        ApplyVisualFromTotalProgress(fromTotal);
        ResetDotsAnimTracking(fromTotal);

        return DOVirtual.Float(fromTotal, toTotal, duration, v =>
            {
                ApplyVisualFromTotalProgress(v, detectMilestoneCrossing: true);
                onUpdate?.Invoke();
            })
            .SetEase(ease)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    float GetStepFromTotalProgress(float totalProgress)
    {
        if (Manager == null) return 0f;

        int milestoneIndex = GetReachedMilestoneIndexAtProgress(totalProgress);
        int baseProgress = milestoneIndex > 0 && Manager.towerRiseConfig != null
            ? Manager.towerRiseConfig.GetCumulativeWinAmount(milestoneIndex - 1)
            : 0;
        return Mathf.Max(0f, totalProgress - baseProgress);
    }

    void ApplyProgressTextFromTotal(float totalProgress)
    {
        if (textProgress == null || Manager == null) return;

        bool show = totalProgress > 0f;
        textProgress.gameObject.SetActive(show);
        if (!show) return;

        int milestoneIndex = GetReachedMilestoneIndexAtProgress(totalProgress);
        float step = GetStepFromTotalProgress(totalProgress);
        textProgress.text = GetCumulativeProgress(milestoneIndex, step).ToString();
    }

    int GetReachedMilestoneIndexAtProgress(float totalProgress)
    {
        if (Manager == null || Manager.towerRiseConfig == null) return 0;

        int count = Manager.GetMileStoneCount();
        if (count <= 0) return 0;

        int progress = Mathf.Max(0, Mathf.FloorToInt(totalProgress));
        for (int i = 0; i < count; i++)
        {
            if (progress < Manager.towerRiseConfig.GetCumulativeWinAmount(i))
                return i;
        }

        return count;
    }

    // EN: Coordinates ShouldSnapProgressToTop using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ShouldSnapProgressToTop qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    bool ShouldSnapProgressToTop(float totalProgress)
    {
        if (Manager?.IsInitialized != true || totalProgress < 0f) return false;
        return Manager.TotalProgress > 0
            && totalProgress >= Manager.TotalProgress;
    }

    float ResolveProgressWorldY(int milestoneIndex, float step, float totalProgress = -1f)
    {
        if (ShouldSnapProgressToTop(totalProgress))
            return GetTrackEdgeWorldY(1f);

        float startY = GetSegmentStartWorldY(milestoneIndex);
        if (step <= 0f) return startY;

        float endY = GetSegmentEndWorldY(milestoneIndex);
        int segmentAmount = Manager?.GetSegmentWinAmountForMilestoneIndex(milestoneIndex) ?? 0;
        if (segmentAmount <= 0) return endY;

        return Mathf.Lerp(startY, endY, Mathf.Clamp01(step / segmentAmount));
    }

    float GetSegmentStartWorldY(int milestoneIndex)
    {
        if (milestoneIndex <= 0) return GetTrackEdgeWorldY(0f);

        var prev = GetMilestoneUI(milestoneIndex - 1);
        return prev != null ? prev.GetCheckpointWorldY() : GetTrackEdgeWorldY(0f);
    }

    float GetSegmentEndWorldY(int milestoneIndex)
    {
        var ui = GetMilestoneUI(milestoneIndex);
        return ui != null ? ui.GetCheckpointWorldY() : GetTrackEdgeWorldY(0f);
    }

    float GetTrackEdgeWorldY(float t)
    {
        if (TrackRect == null) return transform.position.y;

        float halfHeight = TrackRect.rect.height * 0.5f;
        return TrackRect.TransformPoint(new Vector3(0f, Mathf.Lerp(-halfHeight, halfHeight, t), 0f)).y;
    }

    float GetTrackFillFromWorldY(float worldY)
    {
        if (TrackRect == null) return 0f;

        float localY = TrackRect.InverseTransformPoint(new Vector3(TrackRect.position.x, worldY, TrackRect.position.z)).y;
        float halfHeight = TrackRect.rect.height * 0.5f;
        if (halfHeight <= 0f) return 0f;
        return Mathf.Clamp01(Mathf.InverseLerp(-halfHeight, halfHeight, localY));
    }

    void ApplyArrow(float worldY, bool visible)
    {
        if (arrowRectf == null) return;

        Vector3 pos = arrowRectf.position;
        arrowRectf.position = new Vector3(pos.x, worldY, pos.z);
        arrowRectf.gameObject.SetActive(visible);
    }

    void ApplyLineFill(float worldY, float totalProgress = -1f)
    {
        if (lineProgress == null) return;

        lineProgress.gameObject.SetActive(true);
        lineProgress.fillAmount = ShouldSnapProgressToTop(totalProgress)
            ? 1f
            : GetTrackFillFromWorldY(worldY);
    }

    void ApplyProgressText(int milestoneIndex, float step)
    {
        if (textProgress == null) return;

        bool show = step > 0f || milestoneIndex > 0;
        textProgress.gameObject.SetActive(show);
        if (!show) return;

        textProgress.text = GetCumulativeProgress(milestoneIndex, step).ToString();
    }

    int GetCumulativeProgress(int milestoneIndex, float step)
    {
        var config = Manager?.towerRiseConfig;
        if (config == null) return Mathf.Max(0, Mathf.FloorToInt(step));

        int baseProgress = milestoneIndex > 0
            ? config.GetCumulativeWinAmount(milestoneIndex - 1)
            : 0;
        return baseProgress + Mathf.Max(0, Mathf.FloorToInt(step));
    }

    public void RefreshDots(bool refreshClaimState = true)
    {
        if (Manager == null) return;

        float totalProgress = Manager.GetTotalCurrentWins();
        for (int i = 0; i < mileStoneProgressUIs.Count; i++)
            mileStoneProgressUIs[i]?.RefreshAtProgress(totalProgress, refreshClaimState);
    }

    public void RefreshDotsAtProgress(float totalProgress, bool detectMilestoneCrossing = false)
    {
        if (Manager == null) return;

        bool refreshClaim = !deferClaimStateRefresh;

        for (int i = 0; i < mileStoneProgressUIs.Count; i++)
        {
            var ui = mileStoneProgressUIs[i];
            if (ui == null) continue;

            bool playAnim = false;
            if (refreshClaim && detectMilestoneCrossing && lastDotsAnimProgress >= 0f)
            {
                bool wasReached = Manager.IsMilestoneReachedAtProgress(i, lastDotsAnimProgress);
                bool isReached = Manager.IsMilestoneReachedAtProgress(i, totalProgress);
                bool claimable = Manager.IsMilestoneClaimableAtProgress(i, totalProgress);
                playAnim = !wasReached && isReached && claimable;
            }

            ui.RefreshAtProgress(totalProgress, refreshClaimState: refreshClaim, playClaimRevealAnim: playAnim);
        }

        lastDotsAnimProgress = totalProgress;
    }

    void RefreshDotsAtSegmentProgress(int milestoneIndex, float step, bool detectMilestoneCrossing = false)
    {
        RefreshDotsAtProgress(GetTotalProgressFromSegment(milestoneIndex, step), detectMilestoneCrossing);
    }

    float GetTotalProgressFromSegment(int milestoneIndex, float step)
    {
        if (Manager == null) return Mathf.Max(0f, step);

        int baseProgress = milestoneIndex > 0 && Manager.towerRiseConfig != null
            ? Manager.towerRiseConfig.GetCumulativeWinAmount(milestoneIndex - 1)
            : 0;
        return baseProgress + Mathf.Max(0f, step);
    }

    public int GetMilestoneUICount() => mileStoneProgressUIs.Count;

    public RectTransform GetMilestoneCheckpointRect(int milestoneIndex)
    {
        return GetMilestoneUI(milestoneIndex)?.GetCheckpointRect();
    }

    TowerRiseMileStoneProgressUI GetMilestoneUI(int milestoneIndex)
    {
        if (milestoneIndex < 0 || milestoneIndex >= mileStoneProgressUIs.Count) return null;
        return mileStoneProgressUIs[milestoneIndex];
    }

    public float GetWorldYAtTotalProgress(float totalProgress)
    {
        if (Manager == null) return 0f;
        int milestoneIndex = GetReachedMilestoneIndexAtProgress(totalProgress);
        float step = GetStepFromTotalProgress(totalProgress);
        return ResolveProgressWorldY(milestoneIndex, step, totalProgress);
    }
}

public class HoneyRiseProgress : TowerRiseProgress
{
}
