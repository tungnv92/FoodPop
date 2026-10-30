using DG.Tweening;
using GamePackages;
using UnityEngine;
using UnityEngine.UI;

public class MissionControlSmallChestNgan : MonoBehaviour
{
    public int stageId;
    public Image gemImg;
    public float flyScale = 1.3f;
    public float flyDuration = 0.6f;
    public RectTransform targetRectTF;
    public Tween moveTweenX;
    public Tween moveTweenY;
    public Tween scaleTween;
    public static bool isFlyingStatic;
    public bool isFlying;
    public AnimationCurve curveX;
    public AnimationCurve curveY;
    public AnimationCurve curveScale;

    public void OnInitt(int stageId)
    {
        this.stageId = stageId;
        moveTweenX?.Kill();
        moveTweenY?.Kill();
        scaleTween?.Kill();
        gemImg.transform.SetParent(this.transform);
        targetRectTF.transform.SetParent(this.transform);
        gemImg.rectTransform.anchoredPosition = Vector2.zero;
        gemImg.transform.localScale = Vector3.one;
        isFlying = false;

        if (MissionControlManager.Ins != null && MissionControlManager.Ins.config != null &&
            stageId >= 0 && stageId < MissionControlManager.Ins.config.gemIcons.Count)
        {
            gemImg.sprite = MissionControlManager.Ins.config.gemIcons[stageId].icon;
        }
    }

    // EN: Animates the quest gem flying from the quest item to this small chest slot and triggers chest claim when full.
    // VI: Diễn hoạt viên đá quý bay từ mục nhiệm vụ về ô rương nhỏ này và kích hoạt nhận thưởng rương khi đầy.
    public void Fly(RectTransform fromRectTF)
    {
        Transform flyParent = MissionControl.Instance != null ? MissionControl.Instance.transform : transform.parent;
        isFlying = true;
        gemImg.gameObject.SetActive(true);
        gemImg.rectTransform.position = fromRectTF.position;
        gemImg.transform.SetParent(flyParent);
        targetRectTF.anchoredPosition = Vector2.zero;
        targetRectTF.transform.SetParent(flyParent);
        gemImg.transform.localScale = Vector3.one * flyScale;

        Ease easeX = curveX != null && curveX.length > 0 ? Ease.Unset : Ease.InOutQuad;
        Ease easeY = curveY != null && curveY.length > 0 ? Ease.Unset : Ease.InOutQuad;

        if (curveX != null && curveX.length > 0)
            moveTweenX = gemImg.rectTransform.DOAnchorPosX(targetRectTF.anchoredPosition.x, flyDuration).SetEase(curveX);
        else
            moveTweenX = gemImg.rectTransform.DOAnchorPosX(targetRectTF.anchoredPosition.x, flyDuration).SetEase(Ease.InOutQuad);

        if (curveY != null && curveY.length > 0)
            moveTweenY = gemImg.rectTransform.DOAnchorPosY(targetRectTF.anchoredPosition.y, flyDuration).SetEase(curveY);
        else
            moveTweenY = gemImg.rectTransform.DOAnchorPosY(targetRectTF.anchoredPosition.y, flyDuration).SetEase(Ease.InOutQuad);

        scaleTween = gemImg.transform.DOScale(1f, flyDuration).OnUpdate(() =>
        {
            isFlyingStatic = true;
        }).OnComplete(() =>
        {
            gemImg.transform.SetParent(this.transform);
            targetRectTF.transform.SetParent(this.transform);
            isFlying = false;
            isFlyingStatic = false;

            gemImg.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5);

            //if (AudioManager.Ins != null)
            //{
            //    AudioManager.Ins.PlaySound(SoundType.CollectReward);
            //}
            if (VibrationManager.Ins != null)
            {
                VibrationManager.Ins.Vibrate(HapticType.Light);
            }

            DOVirtual.DelayedCall(Time.deltaTime, () =>
            {
                if (MissionControlManager.Ins != null && MissionControlManager.Ins.IsCanClaimSmallChest(stageId))
                {
                    if (MissionControl.Instance != null && stageId < MissionControl.Instance.smallChests.Count &&
                        MissionControl.Instance.smallChests[stageId] != null)
                    {
                        MissionControl.Instance.smallChests[stageId].ClaimRewards();
                    }
                }
            });
        });
    }
}
