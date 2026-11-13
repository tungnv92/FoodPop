using AssetKits.ParticleImage;
using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinStreakHome : MonoBehaviour
{
    public GameObject unlockedObj;
    public GameObject lockedObj;
    public GameObject completedObj;
    public TMP_Text unlockTmp;
    [Space]
    public Image progressBarImg;
    public TMP_Text progressBarText;
    //public WinStreakRewardUI preReward;
    public WinStreakRewardUI nextReward;
    public RectTransform keyPointRootTf;
    public Transform homeCenterPoint;
    [Header("CLAIM ITEM")]
    public RectTransform claimPointObject;
    public Image claimPointImage;
    [Space]
    public TMP_Text countDownText;
    public Image multiPointImage;
    public ParticleImage claimFx;
    public Sprite[] multiPointSprites;
    [Header("TUT")]
    public GameObject tutObj;
    public GameObject maskObj;
    public HandTutAnim handAnim;
    private WinStreakManager Manager => WinStreakManager.Instance;

    private Coroutine _claimCoroutine;
    private bool _isAnimating;

    private void OnEnable()
    {
        if (Manager != null)
        {
            Manager.onDataChanged += InitProgress;
        }
        RefreshVisualState();
    }

    private void Start()
    {
        gameObject.SetActive(Manager != null && Manager.IsEventActive);
        RefreshVisualState();
        if (unlockTmp != null && Manager != null)
        {
            unlockTmp.text = $"Unlock at level {Manager.UnlockLevel}";
        }
        InitProgress();
    }

    // EN: Coordinates CheckShowTut using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối CheckShowTut qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public bool CheckShowTut()
    {
        return Manager.IsLevelUnlocked && !Manager.HasShownTutorial;
    }

    // EN: Coordinates ShowTut using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ShowTut qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void ShowTut()
    {
        maskObj.SetActive(true);
        handAnim.ShowTouchAnim(transform.position, new Vector2(300, -100), () =>
        {
            UIManager.Ins.EnableBlocker(true);
            Manager.MarkTutorialShown();
            StartCoroutine(PlayRevealAnim());
        });
    }

    IEnumerator PlayRevealAnim()
    {
        UIManager.Ins.EnableBlocker(true);
        transform.DOScale(0, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
        {
            maskObj.SetActive(false);
            RefreshVisualState();
        });
        yield return new WaitForSeconds(0.25f);
        transform.DOScale(1, 0.25f).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(0.5f);
        UIManager.Ins.EnableBlocker(false);
        UIManager.Ins.OpenPopup(PopupId.WinStreak);
    }

    // EN: Coordinates RefreshVisualState using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối RefreshVisualState qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void RefreshVisualState()
    {
        if (Manager == null) return;

        bool isCompleted = Manager.IsCompleted;
        if (isCompleted)
        {
            if (unlockedObj != null) unlockedObj.SetActive(false);
            if (lockedObj != null) lockedObj.SetActive(false);
            if (completedObj != null) completedObj.SetActive(true);
            tutObj.SetActive(false);
        }
        else
        {
            if (completedObj != null) completedObj.SetActive(false);
            bool isUnlocked = Manager.IsLevelUnlocked;
            bool isTut = !Manager.HasShownTutorial;
            tutObj.SetActive(isUnlocked && isTut);

            if (unlockedObj != null) unlockedObj.SetActive(isUnlocked && !tutObj.activeInHierarchy);
            if (lockedObj != null) lockedObj.SetActive(!isUnlocked && !tutObj.activeInHierarchy);
        }
    }

    private void OnDisable()
    {
        if (Manager != null)
        {
            Manager.onDataChanged -= InitProgress;
        }

        if (_claimCoroutine != null)
        {
            StopCoroutine(_claimCoroutine);
            _claimCoroutine = null;
            _isAnimating = false;
            if (UIManager.Ins != null)
            {
                UIManager.Ins.EnableBlocker(false);
            }
        }
    }

    private void Update()
    {
        if (Manager == null) return;

        if (countDownText != null)
        {
            countDownText.text = Manager.GetCountDownString();
        }
    }

    public bool HasPendingPoints()
    {
        return Manager != null && !Manager.IsCompleted && Manager.PendingKeys > 0;
    }

    public void InitProgress()
    {
        if (_isAnimating) return;
        if (Manager == null || Manager.WinStreakGlobalConfig == null) return;

        RefreshVisualState();

        if (Manager.IsCompleted) return;

        SetVisualProgressByKeys(Manager.CurrentKeys);
    }

    public void SetVisualProgressByKeys(int keys)
    {
        if (Manager == null || Manager.WinStreakGlobalConfig == null) return;

        int currentTier = Manager.WinStreakTier;
        if (multiPointImage != null && multiPointSprites != null && currentTier >= 0 && currentTier < multiPointSprites.Length)
        {
            multiPointImage.sprite = multiPointSprites[currentTier];
        }

        var config = Manager.WinStreakGlobalConfig;
        if (config == null || config.winStreakMileStoneInfos == null || config.winStreakMileStoneInfos.Count == 0) return;

        int milestoneIndex = Manager.GetMilestoneIndexAtKeys(keys);
        var milestone = Manager.GetMilestone(milestoneIndex);
        if (milestone == null) return;

        int currentPointsInMs = Manager.GetPointsInMilestone(keys, milestoneIndex);
        int requiredPoints = milestone.screwAmount;

        if (progressBarImg != null)
        {
            progressBarImg.DOKill();
            progressBarImg.fillAmount = requiredPoints > 0 ? (float)currentPointsInMs / requiredPoints : 1f;
        }

        if (progressBarText != null)
        {
            progressBarText.text = $"{currentPointsInMs}/{requiredPoints}";
        }

        if (nextReward != null)
        {
            nextReward.InitReward(milestone.rewards);
        }
    }

    #region Animation Reward

    /// <summary>
    /// Play claim anim when points are being awarded.
    /// </summary>
    // EN: Plays the flying point claim animation from the home center point to the key progress bar.
    // VI: Phát hiệu ứng bay điểm nhận được từ vị trí trung tâm màn hình chính tới thanh tiến trình chìa khóa.
    public virtual IEnumerator PlayClaimPendingPointAnim()
    {
        if (claimPointObject == null || (Manager != null && Manager.IsCompleted)) yield break;

        if (UIManager.Ins != null)
        {
            UIManager.Ins.EnableBlocker(true);
        }

        if (homeCenterPoint != null)
        {
            claimPointObject.position = homeCenterPoint.position;
        }

        claimPointObject.gameObject.SetActive(true);
        claimPointObject.localScale = Vector3.zero;

        int preStreak = Manager.WinStreak - 1;
        int preTier = Mathf.Clamp(preStreak, 0, 4);
        if (claimPointImage != null && Manager != null)
        {
            claimPointImage.sprite = multiPointSprites[preTier];
        }

        claimPointObject.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.5f);

        // claimPointObject fly to keyPointRootTf through a curve in 0.25f
        if (keyPointRootTf != null)
        {
            Vector3 startPos = claimPointObject.position;
            Vector3 endPos = keyPointRootTf.position;
            Vector3 dir = endPos - startPos;
            float dist = dir.magnitude;
            Vector3 normal = dist > 0.001f ? new Vector3(-dir.y, dir.x, 0f).normalized : Vector3.up;
            Vector3 midPoint = (startPos + endPos) * 0.5f + normal * (dist * 0.2f);

            Vector3[] path = new Vector3[] { midPoint, endPos };
            claimPointObject.DOScale(0.5f, 0.5f);
            claimPointObject.DOPath(path, 0.5f, PathType.CatmullRom).SetEase(Ease.InQuad).OnComplete(() =>
            {
                claimFx.Play();
                multiPointImage.transform.DOScale(1.1f, 0.15f).OnComplete(() => multiPointImage.transform.DOScale(1f, 0.15f));
            });
        }

        yield return new WaitForSeconds(0.5f);
        claimPointObject.localScale = Vector3.zero;
        claimPointObject.gameObject.SetActive(false);

        if (keyPointRootTf != null)
        {
            keyPointRootTf.DOPunchScale(Vector3.one * 0.2f, 0.2f, 1, 0.5f);
        }
        //if (AudioManager.Ins != null)
        {
        //    AudioManager.Ins.PlaySound(SoundType.CollectReward);
        }
    }

    /// <summary>
    /// Play claim reward anim when a milestone is completed.
    /// </summary>
    public virtual IEnumerator PlayClaimRewardAnim(WinStreakMileStoneInfo milestone = null, Action onClaimedReward = null)
    {
        if (nextReward == null || milestone == null || milestone.rewards == null || milestone.rewards.Count == 0 || (Manager != null && Manager.IsCompleted))
        {
            yield break;
        }

        if (UIManager.Ins != null)
        {
            UIManager.Ins.EnableBlocker(true);
        }

        Vector3 originPos = nextReward.transform.position;
        Vector3 originLocalPos = nextReward.transform.localPosition;

        // next reward fly to homeCenterPoint in a curve
        // oncomplete, DOScale to 0
        if (homeCenterPoint != null)
        {
            Vector3 startPos = originPos;
            Vector3 centerPos = homeCenterPoint.position;
            Vector3 dir = centerPos - startPos;
            float dist = dir.magnitude;
            Vector3 normal = dist > 0.001f ? new Vector3(-dir.y, dir.x, 0f).normalized : Vector3.up;
            Vector3 midPoint = (startPos + centerPos) * 0.5f + normal * (dist * 0.2f);

            Vector3[] path = new Vector3[] { midPoint, centerPos };
            nextReward.transform.DOPath(path, 0.5f, PathType.CatmullRom).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.5f);
            nextReward.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack);
            yield return new WaitForSeconds(0.25f);
        }
        else
        {
            nextReward.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack);
            yield return new WaitForSeconds(0.25f);
        }

        RewardAnimator rewardAnimator = UIManager.Ins.GetPanel() != null ? UIManager.Ins.GetPanel().RewardAnimator : null;
        if (rewardAnimator != null)
        {
            rewardAnimator.Init((milestone.rewards.Count > 1) ? ChestType.winstreak : ChestType.None);
            bool assignedOnReceive = false;
            foreach (var reward in milestone.rewards)
            {
                if (!assignedOnReceive)
                {
                    assignedOnReceive = true;
                    rewardAnimator.Add((int)reward.type, reward.value, onReceiveReward: () =>
                    {
                        onClaimedReward?.Invoke();
                    });
                }
                else
                {
                    rewardAnimator.Add((int)reward.type, reward.value);
                }
            }

            bool isRewardCompleted = false;
            rewardAnimator.OnComplete(() =>
            {
                isRewardCompleted = true;
            });
            rewardAnimator.Play();

            if (rewardAnimator.WaitUntilComplete != null)
            {
                yield return rewardAnimator.WaitUntilComplete;
            }
            else
            {
                yield return new WaitUntil(() => isRewardCompleted);
            }

            // RewardAnimator internally disables blocker on completion; re-enable to ensure inputs stay blocked until all animations finish
            if (UIManager.Ins != null)
            {
                UIManager.Ins.EnableBlocker(true);
            }
        }

        // on reward anim completed, setup for new next reward (sprite, amount)
        // DOScale next reward to 1
        // fly next reward to origin pos in curved
        var nextMilestone = Manager != null ? Manager.GetMilestone(milestone.index + 1) : null;
        if (nextMilestone != null && nextMilestone.rewards != null && nextMilestone.rewards.Count > 0)
        {
            nextReward.InitReward(nextMilestone.rewards);
            nextReward.gameObject.SetActive(true);

            // Keep state of moment in anim: reset progress bar to 0 for incoming next milestone
            if (progressBarImg != null)
            {
                progressBarImg.DOKill();
                progressBarImg.fillAmount = 0f;
            }
            if (progressBarText != null)
            {
                progressBarText.text = $"0/{nextMilestone.screwAmount}";
            }

            if (homeCenterPoint != null)
            {
                nextReward.transform.position = homeCenterPoint.position;
            }

            nextReward.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(0.25f);

            if (homeCenterPoint != null)
            {
                Vector3 returnStartPos = nextReward.transform.position;
                Vector3 returnEndPos = originPos;
                Vector3 returnDir = returnEndPos - returnStartPos;
                float returnDist = returnDir.magnitude;
                Vector3 returnNormal = returnDist > 0.001f ? new Vector3(returnDir.y, -returnDir.x, 0f).normalized : Vector3.up;
                Vector3 returnMid = (returnStartPos + returnEndPos) * 0.5f + returnNormal * (returnDist * 0.2f);

                Vector3[] returnPath = new Vector3[] { returnMid, returnEndPos };
                nextReward.transform.DOPath(returnPath, 0.5f, PathType.CatmullRom).SetEase(Ease.InOutQuad);
                yield return new WaitForSeconds(0.5f);
            }

            nextReward.transform.localPosition = originLocalPos;
            nextReward.transform.localScale = Vector3.one;
        }
        else
        {
            // Reached final milestone - no more rewards to show
            nextReward.transform.localPosition = originLocalPos;
            nextReward.transform.localScale = Vector3.zero;
            nextReward.gameObject.SetActive(false);
            if (progressBarImg != null)
            {
                progressBarImg.DOKill();
                progressBarImg.fillAmount = 1f;
            }
            if (progressBarText != null)
            {
                progressBarText.text = $"{milestone.screwAmount}/{milestone.screwAmount}";
            }
        }
    }

    #endregion

    public IEnumerator ClaimPendingPoint(Action onComplete = null)
    {
        yield return ClaimPendingPointRoutine(onComplete);
    }

    public Coroutine StartClaimPendingPoint(Action onComplete = null)
    {
        if (_claimCoroutine != null)
        {
            StopCoroutine(_claimCoroutine);
        }
        _claimCoroutine = StartCoroutine(ClaimPendingPointRoutine(onComplete));
        return _claimCoroutine;
    }

    public IEnumerator ClaimPendingPointRoutine(Action onComplete = null)
    {
        if (Manager == null || Manager.IsCompleted)
        {
            InitProgress();
            onComplete?.Invoke();
            yield break;
        }

        int pendingPoints = Manager.PendingKeys;
        if (pendingPoints <= 0)
        {
            InitProgress();
            onComplete?.Invoke();
            yield break;
        }

        _isAnimating = true;
        if (UIManager.Ins != null)
        {
            UIManager.Ins.EnableBlocker(true);
        }

        int startKeys = Manager.CurrentKeys;
        int totalKeys = startKeys + pendingPoints;

        // Keep state of moment before points are collected
        SetVisualProgressByKeys(startKeys);

        // 1. Play display anim and claim anim
        yield return PlayClaimPendingPointAnim();

        // 2. Add all pending key point to root key point, reset pending point to 0
        Manager.CommitPendingKeys();

        // 3. Play increase progress
        int currentVisualKeys = startKeys;

        while (currentVisualKeys < totalKeys)
        {
            int milestoneIndex = Manager.GetMilestoneIndexAtKeys(currentVisualKeys);
            var milestone = Manager.GetMilestone(milestoneIndex);
            if (milestone == null) break;

            int targetAccumulate = Manager.GetMilestoneTargetAccumulate(milestoneIndex);
            int prevAccumulate = Manager.GetMilestonePrevAccumulate(milestoneIndex);
            int requiredPoints = milestone.screwAmount;

            if (nextReward != null)
            {
                nextReward.InitReward(milestone.rewards);
            }

            int fromPoints = currentVisualKeys - prevAccumulate;

            // 2.1: If total key point >= required point of next reward:
            if (totalKeys >= targetAccumulate)
            {
                int toPoints = requiredPoints;
                yield return AnimateProgressBar(fromPoints, toPoints, requiredPoints);

                // Play claim reward anim
                yield return PlayClaimRewardAnim(milestone, () =>
                {
                    // Claim reward
                    Manager.ClaimMilestone(milestoneIndex);
                });

                if (UIManager.Ins != null)
                {
                    UIManager.Ins.EnableBlocker(true);
                }

                // Advance visual keys to the completed milestone
                currentVisualKeys = targetAccumulate;

                // Advance to next milestone if available
                if (milestoneIndex + 1 >= Manager.GetMilestoneCount())
                {
                    // Reached final milestone
                    if (progressBarText != null)
                    {
                        progressBarText.text = $"{requiredPoints}/{requiredPoints}";
                    }
                    if (progressBarImg != null) progressBarImg.fillAmount = 1f;

                    Manager.SetCompleted(true);
                    break;
                }
                // Loop continues to "increase to the rest points"
            }
            // 2.2: If total key point < required point of next reward:
            else
            {
                int toPoints = totalKeys - prevAccumulate;
                yield return AnimateProgressBar(fromPoints, toPoints, requiredPoints);
                currentVisualKeys = totalKeys;

                // End claim flow
                break;
            }
        }

        _isAnimating = false;
        InitProgress();
        _claimCoroutine = null;
        onComplete?.Invoke();
        if (UIManager.Ins != null)
        {
            UIManager.Ins.EnableBlocker(false);
        }
    }

    private IEnumerator AnimateProgressBar(int fromPoints, int toPoints, int maxPoints)
    {
        if (maxPoints <= 0) yield break;

        float startFill = Mathf.Clamp01((float)fromPoints / maxPoints);
        float targetFill = Mathf.Clamp01((float)toPoints / maxPoints);

        if (progressBarImg != null)
        {
            progressBarImg.DOKill();
            progressBarImg.fillAmount = startFill;
        }
        if (progressBarText != null) progressBarText.text = $"{fromPoints}/{maxPoints}";

        if (Mathf.Approximately(startFill, targetFill) && fromPoints == toPoints)
        {
            yield break;
        }

        float fillSpeed = Manager != null && Manager.WinStreakGlobalConfig != null
            ? Manager.WinStreakGlobalConfig.FillSpeed
            : 20f;
        if (fillSpeed <= 0f) fillSpeed = 20f;

        float duration = Mathf.Clamp((float)Mathf.Abs(toPoints - fromPoints) / fillSpeed, 0.25f, 1.0f);

        if (progressBarImg != null)
        {
            Tween fillTween = progressBarImg.DOFillAmount(targetFill, duration)
                .SetEase(Ease.InQuad)
                .OnUpdate(() =>
                {
                    if (progressBarText != null && progressBarImg != null)
                    {
                        int min = Mathf.Min(fromPoints, toPoints);
                        int max = Mathf.Max(fromPoints, toPoints);
                        int currentDisplay = Mathf.Clamp(Mathf.RoundToInt(progressBarImg.fillAmount * maxPoints), min, max);
                        progressBarText.text = $"{currentDisplay}/{maxPoints}";
                    }
                });

            yield return fillTween.WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }

        yield return new WaitForSeconds(0.5f);

        if (progressBarImg != null) progressBarImg.fillAmount = targetFill;
        if (progressBarText != null) progressBarText.text = $"{toPoints}/{maxPoints}";
    }

    public void OnClick()
    {
        if (_claimCoroutine != null) return;

        if (Manager.IsLevelUnlocked)
        {
            UIManager.Ins.OpenPopup(PopupId.WinStreak);
        }
    }
}
