using DG.Tweening;
using GamePackages;
//using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TowerRiseClaimBox : Popup
{
    //public SkeletonGraphic skeletonGraphic;
    public GameObject buttonClaimObj;
    public float delayShowRewards = 0.7f;
    public GridLayoutGroup grid;
    public List<RewardItem> rewardItems = new();
    readonly List<ResourceGame> rewards = new();

    [SerializeField] private RewardItem rewardItemPrefab;

    public Action OnClaimComplete;

    // EN: Opens the TowerRise claim box popup, clears old reward items, and starts the delayed reward display animation.
    // VI: Mở popup nhận rương TowerRise, xóa các vật phẩm thưởng cũ và bắt đầu hiệu ứng hiển thị phần thưởng sau độ trễ.
    public override void Open(object args = null)
    {
        base.Open(args);
        DespawnOldRewardItems();
        rewards.Clear();
        if (buttonClaimObj != null) buttonClaimObj.SetActive(false);

        //if (AudioManager.Ins != null)
        //{
        //    AudioManager.Ins.PlaySound(SoundType.CollectReward);
        //}

        DOVirtual.DelayedCall(delayShowRewards, RunAnimShowRewards);
    }

    void DespawnOldRewardItems()
    {
        foreach (var rewardItem in rewardItems)
        {
            if (rewardItem != null)
                Destroy(rewardItem.gameObject);
        }
        rewardItems.Clear();
    }

    public void OnInitt(List<ResourceGame> rewardInfos, int milestoneIndex)
    {
        this.rewards.Clear();
        if (rewardInfos != null)
        {
            foreach (var reward in rewardInfos)
            {
                this.rewards.Add(new ResourceGame { type = reward.type, value = reward.value });
            }
        }
        SetSkin(milestoneIndex);
    }

    // EN: Updates the chest Spine skin corresponding to the specified milestone index.
    // VI: Cập nhật giao diện Spine của rương tương ứng với chỉ số mốc chỉ định.
    public void SetSkin(int milestoneIndex)
    {
        //if (skeletonGraphic == null) return;

        //var skeleton = skeletonGraphic.Skeleton;
        //if (skeleton == null || skeleton.Data == null) return;

        //string targetSkinName = (milestoneIndex + 1).ToString();
        //if (targetSkinName.Length == 1) targetSkinName = "0" + targetSkinName;

        //if (skeleton.Data.FindSkin(targetSkinName) != null)
        //{
        //    skeleton.SetSkin(targetSkinName);
        //    skeleton.SetSlotsToSetupPose();
        //    skeletonGraphic.Update(0);
        //}
        //else
        //{
        //    Debug.LogWarning($"[TowerRiseClaimBox] Skin {targetSkinName} not found in Spine data.");
        //}
    }

    void RunAnimShowRewards()
    {
        StartCoroutine(IERunAnimShowRewards());
    }

    IEnumerator IERunAnimShowRewards()
    {
        UIManager.Ins.EnableBlocker(true);

        if (rewardItemPrefab == null)
        {
            rewardItemPrefab = Resources.Load<RewardItem>("RewardItem");
        }

        Transform parent = grid != null ? grid.transform : transform;

        foreach (var reward in rewards)
        {
            RewardItem rewardItem = rewardItemPrefab != null
                ? Instantiate(rewardItemPrefab, parent)
                : new GameObject("RewardItem").AddComponent<RewardItem>();
            rewardItem.OnInitt(reward, null, false, false);
            rewardItem.transform.localScale = Vector3.one * 0.01f;
            rewardItem.transform.DOScale(0.82f, 0.5f).SetEase(Ease.OutBack);
            rewardItems.Add(rewardItem);
            yield return new WaitForSeconds(0.3f);
        }

        UIManager.Ins.EnableBlocker(false);
        yield return new WaitForSeconds(0.25f);
        if (buttonClaimObj != null) buttonClaimObj.SetActive(true);
    }

    public void ButtonClaim()
    {
        foreach (var reward in rewards)
        {
            TowerRiseManager.ClaimRewardResource(reward, "TowerRise");
        }

        var onComplete = OnClaimComplete;
        OnClaimComplete = null;
        Close();
        onComplete?.Invoke();
    }
}

public class PopupTowerRiseClaimBox : TowerRiseClaimBox
{
}

public class HoneyRiseClaimBox : TowerRiseClaimBox
{
}

public class PopupHoneyRiseClaimBox : TowerRiseClaimBox
{
}
