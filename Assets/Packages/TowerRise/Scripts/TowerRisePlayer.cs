using DG.Tweening;
//using Spine.Unity;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;
//using TrackEntry = Spine.TrackEntry;

public class TowerRisePlayer : MonoBehaviour
{
    const string IdleAnim = "char2/fly";
    const string WinAnim = "char2/fly";
    const string LoseAnimStart = "char2/FallStart";
    const string LoseAnimEnd = "char2/FallEnd";
    const float LoseToIdleMixDuration = 0.4f;

    //public SkeletonGraphic skeletonGraphic;

    [Header("ANIM:")]
    public float progressRiseDuration = 0.55f;
    public float takeoffSquashScale = 0.92f;
    public float takeoffAnticipation = 0.1f;

    public float RiseDuration => progressRiseDuration;

    RectTransform rectTransform;
    Sequence scaleSequence;
    Sequence loseSequence;
    string activeAnim;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        ResolveSkeletonGraphic();
    }

    public void OnInitt()
    {
        if (!EnsureRectTransform()) return;

        gameObject.SetActive(true);
        KillMoveTweens();
        ResetPose();
    }

    bool preferIdle;

    public void ClearPreferIdle() => preferIdle = false;

    public void SyncAnimState(bool hasProgress)
    {
        if (preferIdle || !hasProgress)
            PlayIdle();
        else
            PlayWin();
    }

    public void PlayIdle()
    {
        loseSequence?.Kill();
        loseSequence = null;
        PlaySpineLoop(IdleAnim);
    }

    public void PlayWin()
    {
        loseSequence?.Kill();
        loseSequence = null;
        preferIdle = false;
        PlaySpineLoop(WinAnim);
    }

    public void PlayLose()
    {
        preferIdle = false;
        loseSequence?.Kill();
        loseSequence = null;
        PlaySpineLoop(LoseAnimStart);
    }

    public void PlayLose(float fallDuration, System.Action onComplete = null)
    {
        PlayLose();
        if (fallDuration > 0f)
        {
            loseSequence = DOTween.Sequence().SetUpdate(true);
            loseSequence.AppendInterval(fallDuration);
            loseSequence.AppendCallback(() => PlayLoseEnd(onComplete));
        }
    }

    // EN: Plays the end-of-fall animation and transitions back to idle before invoking completion.
    // VI: Phát hiệu ứng kết thúc rơi và chuyển về trạng thái nghỉ trước khi gọi callback hoàn tất.
    public void PlayLoseEnd(System.Action onComplete = null)
    {
        loseSequence?.Kill();
        loseSequence = null;

        ResolveSkeletonGraphic();
        //if (skeletonGraphic == null)
        //{
        //    onComplete?.Invoke();
        //    return;
        //}

        preferIdle = true;
        PrepareSkeleton();

        //if (skeletonGraphic.AnimationState == null)
        //{
        //    onComplete?.Invoke();
        //    return;
        //}

        //var stateData = skeletonGraphic.SkeletonDataAsset?.GetAnimationStateData();
        //if (stateData != null)
        //{
        //    stateData.SetMix(LoseAnimStart, LoseAnimEnd, 0.1f);
        //    stateData.SetMix(LoseAnimEnd, IdleAnim, LoseToIdleMixDuration);
        //}

        //TrackEntry entry = skeletonGraphic.AnimationState.SetAnimation(0, LoseAnimEnd, false);
        activeAnim = LoseAnimEnd;

        //skeletonGraphic.AnimationState.AddAnimation(0, IdleAnim, true, 0f);

        //if (entry != null)
        //{
        //    entry.Complete += (trackEntry) =>
        //    {
        //        activeAnim = IdleAnim;
        //        onComplete?.Invoke();
        //    };
        //}
        //else
        //{
        //    PlayIdle();
        //    onComplete?.Invoke();
        //}

        //skeletonGraphic.Update(0);
        onComplete?.Invoke();
    }

    public void TransitionLoseToIdle() => PlayLoseEnd();
    public void FinishFall(System.Action onComplete = null) => PlayLoseEnd(onComplete);
    public void OnFinishFall(System.Action onComplete = null) => PlayLoseEnd(onComplete);

    public void KillMoveTweens()
    {
        loseSequence?.Kill();
        loseSequence = null;
        scaleSequence?.Kill();
        scaleSequence = null;
        rectTransform?.DOKill();
    }

    public void SyncWorldPosition(Vector3 worldPos)
    {
        if (!EnsureRectTransform()) return;
        rectTransform.position = worldPos;
    }

    public void PlayTakeoffScale()
    {
        if (!EnsureRectTransform()) return;

        KillMoveTweens();
        rectTransform.localScale = Vector3.one;

        scaleSequence = DOTween.Sequence().SetUpdate(true);
        scaleSequence.Append(rectTransform.DOScale(takeoffSquashScale, takeoffAnticipation).SetEase(Ease.InOutSine));
        scaleSequence.Append(rectTransform.DOScale(1f, takeoffAnticipation * 0.5f).SetEase(Ease.OutSine));
        scaleSequence.OnKill(() => scaleSequence = null);
        scaleSequence.OnComplete(() => scaleSequence = null);
    }

    bool EnsureRectTransform()
    {
        if (rectTransform == null) rectTransform = transform as RectTransform;
        return rectTransform != null;
    }

    void ResetPose()
    {
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;
    }

    // EN: Resolves the child SkeletonGraphic reference when not already assigned.
    // VI: Tìm tham chiếu SkeletonGraphic ở các đối tượng con khi chưa được gán.
    void ResolveSkeletonGraphic()
    {
        //if (skeletonGraphic == null)
        //    skeletonGraphic = GetComponentInChildren<SkeletonGraphic>(true);
    }

    // EN: Plays the specified looping Spine animation on the player character.
    // VI: Phát hiệu ứng hoạt ảnh Spine lặp lại được chỉ định cho nhân vật người chơi.
    void PlaySpineLoop(string animName)
    {
        ResolveSkeletonGraphic();
        //if (skeletonGraphic == null || string.IsNullOrEmpty(animName)) return;

        if (activeAnim == animName)
        {
            ShowSkeleton();
            return;
        }

        PrepareSkeleton();
        //skeletonGraphic.AnimationState.SetAnimation(0, animName, true);
        activeAnim = animName;
        //skeletonGraphic.Update(0);
    }

    // EN: Enables and initializes the Spine skeleton graphic with unscaled time.
    // VI: Bật và khởi tạo SkeletonGraphic với thời gian không phụ thuộc timeScale.
    void PrepareSkeleton()
    {
        ShowSkeleton();
        //skeletonGraphic.enabled = true;
        //skeletonGraphic.UnscaledTime = true;
        //skeletonGraphic.Initialize(true);
    }

    // EN: Ensures the skeleton graphic GameObject is active.
    // VI: Đảm bảo GameObject của SkeletonGraphic đang được bật.
    void ShowSkeleton()
    {
        //if (skeletonGraphic != null) skeletonGraphic.gameObject.SetActive(true);
    }
}

public class HoneyRisePlayer : TowerRisePlayer
{
}
