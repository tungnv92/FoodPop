using DG.Tweening;
using GamePackages;
//using SDK;
//using Spine.Unity;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LavaQuestClaim : FoodPop.UICanvasPrime
{
    public TextMeshProUGUI goldAmountText;
    public Action OnClaim;
    public GameObject buttonClaimObj;
    public TextMeshProUGUI description;
    public List<LavaQuestFakeUser> fakeUsers;
    //public SkeletonGraphic rewardAnim;

    private LavaQuestFeature Manager => LavaQuestFeature.Instance;

    // EN: Opens the LavaQuest claim popup, triggers reward and gold animations, and refreshes the UI.
    // VI: Mở popup nhận thưởng LavaQuest, kích hoạt hiệu ứng phần thưởng và số vàng, đồng thời làm mới giao diện.
    public override void Open()
    {
        base.Open();
        AnimateOpen();
        if (buttonClaimObj != null) buttonClaimObj.SetActive(false);
        PlayRewardAnim();
        RunAnimGoldAmount();
        //if (AudioManager.Ins != null)
        //{
        //    AudioManager.Ins.PlaySound(SoundType.CollectReward);
        //}
        RefreshDescription();
        RefreshUsers();
        if (buttonClaimObj != null) buttonClaimObj.SetActive(true);
    }

    // EN: Plays the Spine reward animation sequence when the claim popup opens.
    // VI: Phát chuỗi hiệu ứng hoạt ảnh phần thưởng Spine khi popup nhận thưởng mở ra.
    public void PlayRewardAnim()
    {
        //if (rewardAnim == null) return;
        //rewardAnim.Initialize(true);
        //if (rewardAnim.AnimationState != null)
        //{
        //    rewardAnim.AnimationState.SetAnimation(0, "Start", false);
        //    rewardAnim.AnimationState.AddAnimation(0, "Idle", true, 0f);
        //}
    }

    // EN: Coordinates RefreshDescription using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối RefreshDescription qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void RefreshDescription()
    {
        if (description != null && Manager != null)
        {
            int others = Mathf.Max(0, Manager.CompletedBotCount);
            description.text = $"You are sharing the reward with {others} other winners!";
        }
    }

    // EN: Coordinates RefreshUsers using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối RefreshUsers qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    // EN: Coordinates RefreshUsers using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối RefreshUsers qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void RefreshUsers()
    {
        IUserProfile profile = ProfileManager.instance.Features.Get<IUserProfile>();
        int frameId = profile != null ? profile.FrameId : 0;
        int avatarId = profile != null ? profile.AvatarId : 0;
        if (fakeUsers != null)
        {
            foreach (var fakeUser in fakeUsers)
            {
                if (fakeUser != null) fakeUser.RandomFrameAndAvatar();
            }
        }
    }

    public void RunAnimGoldAmount()
    {
        if (goldAmountText != null) goldAmountText.text = "0";
        canvasGroup.interactable = false;
        int claimAmount = Manager != null ? Manager.GetGoldAmountClaim() : 1000;
        DOVirtual.Int(0, claimAmount, 1f, v =>
        {
            if (goldAmountText != null) goldAmountText.text = v.ToString();
        }).SetEase(Ease.OutSine).SetDelay(0.6f).OnComplete(() =>
        {
            canvasGroup.interactable = true;
            if (buttonClaimObj != null) buttonClaimObj.SetActive(true);
        });
    }

    // EN: Grants the LavaQuest gold immediately (no reward fly animation), notifies the claim listener and closes the claim and LavaQuest popups.
    // VI: Cộng vàng LavaQuest ngay (không có hiệu ứng bay thưởng), báo listener nhận thưởng và đóng popup nhận thưởng cùng popup LavaQuest.
    public void ButtonClaim()
    {
        if (buttonClaimObj != null) buttonClaimObj.SetActive(false);
        int goldAmountClaim = Manager != null ? Manager.GetGoldAmountClaim() : 1000;
        if (Manager != null)
        {
            Manager.GrantReward();
        }
        else
        {
            ProfileManager.instance.PlayerDataManager.OnAddCoin(goldAmountClaim, "lava_quest");
            ProfileManager.instance.SaveData();
        }
        OnClaim?.Invoke();
        OnClaim = null;

        Close();
        if (LavaQuest.Instance != null)
        {
            LavaQuest.Instance.ButtonClose();
        }
        else
        {
            FoodPop.UIManager.Instance.CloseUI<LavaQuest>();
        }
    }
}
