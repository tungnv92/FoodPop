using AssetKits.ParticleImage;
using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
//using SDK;
//using Spine.Unity;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LavaQuestClaim : Popup
{
    public TextMeshProUGUI goldAmountText;
    public Action OnClaim;
    public GameObject buttonClaimObj;
    public TextMeshProUGUI description;
    public List<LavaQuestFakeUser> fakeUsers;
    //public SkeletonGraphic rewardAnim;

    [Header("VFX:")]
    public ParticleImagePlayer coinAnim;

    private LavaQuestManager Manager => LavaQuestManager.Instance;

    // EN: Opens the LavaQuest claim popup, triggers reward and gold animations, and refreshes the UI.
    // VI: Mở popup nhận thưởng LavaQuest, kích hoạt hiệu ứng phần thưởng và số vàng, đồng thời làm mới giao diện.
    public override void Open(object args = null)
    {
        base.Open(args);
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
        if (description != null && Manager != null && Manager.IsInitialized)
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
        int frameId = ProfileManager.instance.PlayerDataManager.IsInitialized ? ProfileManager.instance.PlayerDataManager.FrameId : 0;
        int avatarId = ProfileManager.instance.PlayerDataManager.IsInitialized ? ProfileManager.instance.PlayerDataManager.AvatarId : 0;
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
        UIManager.Ins.EnableBlocker(true);
        int claimAmount = Manager != null ? Manager.GetGoldAmountClaim() : 1000;
        DOVirtual.Int(0, claimAmount, 1f, v =>
        {
            if (goldAmountText != null) goldAmountText.text = v.ToString();
        }).SetEase(Ease.OutSine).SetDelay(0.6f).OnComplete(() =>
        {
            UIManager.Ins.EnableBlocker(false);
            if (buttonClaimObj != null) buttonClaimObj.SetActive(true);
        });
    }

    // EN: Coordinates ButtonClaim using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ButtonClaim qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    // EN: Coordinates ButtonClaim using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ButtonClaim qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void ButtonClaim()
    {
        UIManager.Ins.EnableBlocker(true);
        if (buttonClaimObj != null) buttonClaimObj.SetActive(false);
        int goldAmountClaim = Manager != null ? Manager.GetGoldAmountClaim() : 1000;

        coinAnim.gameObject.SetActive(true);
        coinAnim.Init(goldAmountClaim);
        coinAnim.OnReceiveReward(() =>
        {
            if (Manager != null)
            {
                Manager.GrantReward();
            }
            else
            {
                ProfileManager.instance.PlayerDataManager.OnAddCoin(goldAmountClaim, "lava_quest");
                ProfileManager.instance.SaveData();
            }
        });
        coinAnim.OnComplete(() =>
        {
            UIManager.Ins.EnableBlocker(false);
            int currentLevel = ProfileManager.instance.PlayerDataManager.IsInitialized ? ProfileManager.instance.PlayerDataManager.GetCurrentLevel() : 1;
            //if (ABIAnalyticsManager.Instance != null)
            //{
            //    ABIAnalyticsManager.Instance.LavaQuestComplete(currentLevel, goldAmountClaim);
            //}
            OnClaim?.Invoke();
            OnClaim = null;

            Close();
            if (LavaQuest.Instance != null)
            {
                LavaQuest.Instance.ButtonClose();
            }
            else if (UIManager.Ins != null)
            {
                UIManager.Ins.ClosePopup(PopupId.LavaQuest);
            }
        });
        coinAnim.Play();

        //if (AudioManager.Ins != null)
        //{
        //    AudioManager.Ins.PlaySound(SoundType.CoinShow);
        //}
    }
}
