using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
using SharedModules.ED;
using Sirenix.OdinInspector;
//using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissionControlSmallChest : MonoBehaviour
{
    public int stageId;
    public GameObject khayObj;
    public List<MissionControlSmallChestNgan> ngans = new();
    //public SkeletonGraphic skeletonGraphic;
    public MissionControlStageInfo missionControlStageInfo => MissionControlManager.Ins != null && MissionControlManager.Ins.config != null
        ? MissionControlManager.Ins.config.stageInfos.FirstOrDefault(x => x.id == stageId)
        : null;

    public MissionControlRewardBurble rewardBurble;

    [Header("SPAWN REWARD ITEMS:")]
    public float spawnRewardItemsDelay = 0.5f;
    public float spawnRewardItemsSpacing;
    public float spawnRewardItemsOffsetY;
    public float spawnRewardItemsInterval;
    public float spawnRewardItemsScale = 1f;
    public bool isClaimingRewards;

    // EN: Initializes the small chest gem slots and visual state based on claimed quest count.
    // VI: Khởi tạo các ô đá quý của rương nhỏ và trạng thái hiển thị dựa trên số nhiệm vụ đã nhận.
    public void OnInitt()
    {
        for (int i = 0; i < ngans.Count; i++)
        {
            if (ngans[i] != null)
            {
                ngans[i].OnInitt(stageId);
                if (ngans[i].gemImg != null)
                {
                    ngans[i].gemImg.gameObject.SetActive(i < GetClaimedCount());
                }
            }
        }

        //if (skeletonGraphic != null)
        //{
        //    skeletonGraphic.Initialize(true);
        //    if (missionControlStageInfo != null && GetClaimedCount() >= missionControlStageInfo.questInfos.Count)
        //    {
        //        if (skeletonGraphic.AnimationState != null)
        //        {
        //            skeletonGraphic.AnimationState.SetAnimation(0, GetFinalAnimName(MissionControlSmallChestAnimType.IdleOpen), true);
        //        }
        //        //if (khayObj != null) khayObj.SetActive(false);
        //    }
        //    else
        //    {
        //        if (skeletonGraphic.AnimationState != null)
        //        {
        //            skeletonGraphic.AnimationState.SetAnimation(0, GetFinalAnimName(MissionControlSmallChestAnimType.IdleClose), true);
        //        }
        //        //if (khayObj != null) khayObj.SetActive(true);
        //    }
        //}
    }

    public int GetClaimedCount()
    {
        if (missionControlStageInfo == null || MissionControlManager.Ins == null) return 0;
        int claimedCount = 0;
        foreach (var questInfo in missionControlStageInfo.questInfos)
        {
            if (MissionControlManager.Ins.GetIsClaimed(questInfo))
            {
                claimedCount += 1;
            }
        }
        return claimedCount;
    }

    public void Refresh()
    {
    }

    // EN: Claims the stage rewards, reveals the grand chest claim button if all stages are done, and plays the reward animation.
    // VI: Nhận phần thưởng giai đoạn, hiển thị nút nhận rương lớn nếu đã hoàn tất mọi giai đoạn và phát hiệu ứng phần thưởng.
    [Button]
    public void ClaimRewards()
    {
        if (ngans.Any(x => x != null && x.isFlying)) return;
        if (missionControlStageInfo == null || MissionControlManager.Ins == null) return;

        MissionControlManager.Ins.ClaimStageRewards(stageId);

        if (MissionControlManager.Ins.IsClaimedAllSmallChests())
        {
            UIManager.Ins.EnableBlocker(true);
            DOVirtual.DelayedCall(1.2f, () =>
            {
                UIManager.Ins.EnableBlocker(false);
                if (MissionControl.Instance != null)
                {
                    if (MissionControl.Instance.buttonClaimGrandChest != null)
                    {
                        MissionControl.Instance.buttonClaimGrandChest.transform.localScale = Vector3.zero;
                        MissionControl.Instance.buttonClaimGrandChest.SetActive(true);
                        MissionControl.Instance.buttonClaimGrandChest.transform.DOScale(1, 0.25f).SetEase(Ease.OutBack);
                    }
                    if (MissionControl.Instance.buttonClaimGrandChestBackgroundCanvasGroup != null)
                    {
                        MissionControl.Instance.buttonClaimGrandChestBackgroundCanvasGroup.gameObject.SetActive(true);
                        MissionControl.Instance.buttonClaimGrandChestBackgroundCanvasGroup.DOFade(1, 0.1f).SetEase(Ease.OutSine);
                    }
                }
            });
        }

        //if (khayObj != null) khayObj.SetActive(false);
        //if (skeletonGraphic != null)
        //{
        //    skeletonGraphic.Initialize(true);
        //    if (skeletonGraphic.AnimationState != null)
        //    {
        //        skeletonGraphic.AnimationState.SetAnimation(0, GetFinalAnimName(MissionControlSmallChestAnimType.Open), false);
        //    }
        //}

        isClaimingRewards = true;
        DOVirtual.DelayedCall(spawnRewardItemsDelay, () =>
        {
            ShowStageRewardAnimation();
        });
    }

    private void ShowStageRewardAnimation()
    {
        if (missionControlStageInfo == null)
        {
            isClaimingRewards = false;
            return;
        }

        RewardAnimator rwAnimator = UIManager.Ins.GetPanel() != null ? UIManager.Ins.GetPanel().RewardAnimator : null;
        if (rwAnimator != null)
        {
            rwAnimator.Init(ChestType.None);
            foreach (var reward in missionControlStageInfo.rewardInfos)
            {
                rwAnimator.Add(MissionControlManager.MapRewardToAnimatorType(reward.type), reward.value);
            }
            rwAnimator.OnComplete(() => isClaimingRewards = false);
            rwAnimator.Play();
        }

        if (VibrationManager.Ins != null)
        {
            VibrationManager.Ins.Vibrate(HapticType.Light);
        }
        
    }

    public string GetFinalAnimName(MissionControlSmallChestAnimType animType)
    {
        string pre = stageId switch
        {
            0 => "Yellow",
            1 => "Green",
            2 => "Violet",
            3 => "Red",
            _ => "Yellow"
        };

        return animType switch
        {
            MissionControlSmallChestAnimType.Open => pre + "Open",
            MissionControlSmallChestAnimType.IdleOpen => pre + "IdleOpen",
            MissionControlSmallChestAnimType.IdleClose => pre + "IdleClose",
            _ => pre + "IdleClose"
        };
    }

    public void ShowReward()
    {
        EventDispatcher.DispatchEvent(EventId.OnShowMissionControlRewardBurble);
        rewardBurble.OnShow(stageId);
    }
}

public enum MissionControlSmallChestAnimType
{
    Open,
    IdleOpen,
    IdleClose,
}
