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

    public void RefreshDescription()
    {
        if (description != null && Manager != null && Manager.Data != null)
        {
            int others = Mathf.Max(0, Manager.Data.completedBotCount);
            description.text = $"You are sharing the reward with {others} other winners!";
        }
    }

    public void RefreshUsers()
    {
        int frameId = ProfileManager.instance != null && ProfileManager.instance.PlayerData != null ? ProfileManager.instance.PlayerData.frame_avatar_id : 0;
        int avatarId = ProfileManager.instance != null && ProfileManager.instance.PlayerData != null ? ProfileManager.instance.PlayerData.avatar_id : 0;
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

    // EN: Claims the LavaQuest gold reward, plays the coin particle animation, grants coins, and closes the popup.
    // VI: Nhận phần thưởng vàng LavaQuest, phát hiệu ứng hạt xu, cộng xu và đóng popup.
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
            else if (ProfileManager.instance != null && ProfileManager.instance.PlayerDataManager != null)
            {
                ProfileManager.instance.PlayerDataManager.OnAddCoin(goldAmountClaim, "lava_quest");
                ProfileManager.instance.SaveData();
            }
        });
        coinAnim.OnComplete(() =>
        {
            UIManager.Ins.EnableBlocker(false);
            int currentLevel = ProfileManager.instance != null && ProfileManager.instance.PlayerData != null ? ProfileManager.instance.PlayerData.currentLevel : 1;
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
