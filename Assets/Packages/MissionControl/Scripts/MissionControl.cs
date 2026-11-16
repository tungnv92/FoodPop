using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
using Sirenix.OdinInspector;
//using Spine;
//using Spine.Unity;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionControl : Popup
{
    public static MissionControl Instance { get; private set; }
    public Transform safeAreaTf;
    [SerializeField] private MissionControlQuest questPrefab;
    public List<MissionControlSmallChest> smallChests = new();
    public List<MissionControlQuest> quests = new();
    public List<MissionControlButtonStage> buttonStages = new();
    public RectTransform verticalRectTF;
    public int currentStageId = -1;
    public TextMeshProUGUI countdownText;

    [Header("SCALER GRAND CHEST:")]
    //public SkeletonGraphic skeletonGraphic;
    //public BoneFollowerGraphic boneFollowerGraphic;
    public GameObject buttonClaimGrandChest;
    public CanvasGroup buttonClaimGrandChestBackgroundCanvasGroup;
    //public SkeletonGraphic grandChestSkeletonGraphic;
    //public MissionControlGrandChestShaderController grandChestShaderController;

    private MissionControlManager Manager => MissionControlManager.Instance;

    protected virtual void Awake()
    {
        Instance = this;
    }

    // EN: Opens the view using manager queries and commands instead of mutable save models.
    // VI: Mở giao diện bằng truy vấn và lệnh manager thay vì model lưu có thể sửa.
    public override void Open(object args = null)
    {
        base.Open(args);
        Instance = this;
        currentStageId = -1;

        UIManager.Ins.GetPanel<HomePanel>().CoinDisplay.transform.SetParent(safeAreaTf, true);
        UIManager.Ins.GetPanel<HomePanel>().LifeDisplay.transform.SetParent(safeAreaTf, true);

        if (Manager != null && Manager.config != null)
        {
            ChooseStage(Manager.GetStageToChoose());
        }

        bool isGrandChestClaimed = Manager != null && Manager.IsInitialized && Manager.HasClaimedGrandChest;

        foreach (var smallChest in smallChests)
        {
            if (smallChest != null)
            {
                smallChest.OnInitt();
                smallChest.gameObject.SetActive(!isGrandChestClaimed);
            }
        }

        if (Manager != null && Manager.IsInitialized)
        {
            bool needSave = false;
            if (!Manager.HasCompletedTutorial)
            {
                Manager.CompleteTutorial();
                needSave = true;
            }

            if (!Manager.HasShownInfo)
            {
                Manager.MarkInfoShown();
                needSave = true;
                DOVirtual.DelayedCall(Time.deltaTime, () =>
                {
                    if (UIManager.Ins != null)
                    {
                        UIManager.Ins.OpenPopup(PopupId.MissionControlInfo);
                    }
                });
            }

            if (needSave)
            {
                Manager.Save();
            }
        }

        bool canClaimGrand = Manager != null && Manager.IsClaimedAllSmallChests() && !isGrandChestClaimed;
        if (buttonClaimGrandChest != null) buttonClaimGrandChest.SetActive(canClaimGrand);
        if (buttonClaimGrandChestBackgroundCanvasGroup != null)
        {
            buttonClaimGrandChestBackgroundCanvasGroup.gameObject.SetActive(canClaimGrand);
            buttonClaimGrandChestBackgroundCanvasGroup.alpha = canClaimGrand ? 1 : 0;
        }

        //if (skeletonGraphic != null) skeletonGraphic.Initialize(true);
        //if (grandChestSkeletonGraphic != null) grandChestSkeletonGraphic.Initialize(true);
        //if (boneFollowerGraphic != null) boneFollowerGraphic.Initialize();

        if (isGrandChestClaimed)
        {
            //if (skeletonGraphic != null && skeletonGraphic.AnimationState != null)
            //{
            //    skeletonGraphic.AnimationState.SetAnimation(0, "OpenIdle", false);
            //}
            //if (grandChestSkeletonGraphic != null)
            //{
            //    grandChestSkeletonGraphic.gameObject.SetActive(true);
            //    if (grandChestSkeletonGraphic.AnimationState != null)
            //    {
            //        grandChestSkeletonGraphic.AnimationState.SetAnimation(0, "OpenIdle", false);
            //    }
            //}
            //if (grandChestShaderController != null) grandChestShaderController.SetDissolveAmount(1);
        }
        else
        {
            //if (skeletonGraphic != null && skeletonGraphic.AnimationState != null)
            //{
            //    skeletonGraphic.AnimationState.SetAnimation(0, "CloseIdle", false);
            //}
            //if (grandChestSkeletonGraphic != null)
            //{
            //    //grandChestSkeletonGraphic.gameObject.SetActive(false);
            //    if (grandChestSkeletonGraphic.AnimationState != null)
            //    {
            //        grandChestSkeletonGraphic.AnimationState.SetAnimation(0, "CloseIdle", false);
            //    }
            //}
            //if (grandChestShaderController != null) grandChestShaderController.SetDissolveAmount(-1);
        }
    }

    public override void Close()
    {
        base.Close();
        if (Instance == this) Instance = null;
        UIManager.Ins.GetPanel<HomePanel>().ResetCoinLife();
    }

    private void Update()
    {
        if (countdownText != null && Manager != null)
        {
            countdownText.text = Manager.GetCountDownString();
        }
    }

    public void Refresh()
    {
        foreach (var buttonStage in buttonStages)
        {
            if (buttonStage != null) buttonStage.Refresh();
        }
        foreach (var quest in quests)
        {
            if (quest != null) quest.Refresh();
        }
        foreach (var smallChest in smallChests)
        {
            if (smallChest != null) smallChest.Refresh();
        }
    }

    public void ChooseStage(int id)
    {
        if (currentStageId == id) return;
        currentStageId = id;
        RefreshButtonStages(id);
        RefreshQuests(id);
        Refresh();
    }

    public void RefreshButtonStages(int id)
    {
        for (int i = 0; i < buttonStages.Count; i++)
        {
            if (buttonStages[i] != null)
            {
                buttonStages[i].OnInitt(i);
                buttonStages[i].SetChoose(i == id);
            }
        }
    }

    public void RefreshQuests(int id)
    {
        if (Manager == null || Manager.config == null || Manager.config.stageInfos == null) return;
        var stageInfo = Manager.config.stageInfos.FirstOrDefault(x => x.id == id);
        if (stageInfo == null) return;

        if (questPrefab == null)
        {
            questPrefab = Resources.Load<MissionControlQuest>("MissionControlQuest");
            if (questPrefab == null)
            {
                var go = Resources.Load<GameObject>("MissionControlQuest");
                if (go != null) questPrefab = go.GetComponent<MissionControlQuest>();
            }
        }

        while (quests.Count < stageInfo.questInfos.Count)
        {
            MissionControlQuest newQuest = null;
            if (questPrefab != null)
            {
                newQuest = Instantiate(questPrefab, verticalRectTF);
            }
            else if (quests.Count > 0 && quests[0] != null)
            {
                newQuest = Instantiate(quests[0], verticalRectTF);
            }
            if (newQuest != null)
            {
                newQuest.transform.localScale = Vector3.one;
                quests.Add(newQuest);
            }
            else break;
        }

        for (int i = 0; i < quests.Count; i++)
        {
            if (i < stageInfo.questInfos.Count)
            {
                quests[i].gameObject.SetActive(true);
                quests[i].OnInitt(stageInfo.questInfos[i]);
            }
            else
            {
                quests[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnClaimQuest(MissionControlQuest missionControlQuest)
    {
        if (missionControlQuest == null) return;
        int stageId = missionControlQuest.missionControlQuestInfo.stageId;
        if (stageId >= 0 && stageId < smallChests.Count && smallChests[stageId] != null)
        {
            MissionControlSmallChest chest = smallChests[stageId];
            foreach (var ngan in chest.ngans)
            {
                if (ngan != null && ngan.gemImg != null && !ngan.gemImg.gameObject.activeInHierarchy)
                {
                    ngan.Fly(missionControlQuest.gemImg.rectTransform);
                    break;
                }
            }
        }
    }

    // EN: Claims the grand chest prize, hides small chests, and triggers the grand chest reward presentation.
    // VI: Nhận phần thưởng rương lớn, ẩn các rương nhỏ và kích hoạt hiệu ứng trình diễn phần thưởng rương lớn.
    [Button]
    public void ClaimGrandChest()
    {
        if (Manager == null) return;
        UIManager.Ins.EnableBlocker(true);
        Manager.ClaimGrandPrize();

        foreach (var smallChest in smallChests)
        {
            if (smallChest != null) smallChest.gameObject.SetActive(false);
        }

        //if (grandChestSkeletonGraphic != null)
        //{
        //    grandChestSkeletonGraphic.Initialize(true);
        //    grandChestSkeletonGraphic.freeze = false;
        //    TrackEntry entry2 = grandChestSkeletonGraphic.AnimationState.SetAnimation(0, "Open", false);
        //    entry2.Complete += (OnAnim2) =>
        //    {
        //        grandChestSkeletonGraphic.AnimationState.SetAnimation(0, "OpenIdle", false);
        //    };
        //}
        DOVirtual.DelayedCall(0.5f, () =>
        {
            ShowGrandChestRewardAnimation();
        });

        //if (skeletonGraphic != null)
        //{
        //    skeletonGraphic.Initialize(true);
        //    if (boneFollowerGraphic != null) boneFollowerGraphic.Initialize();
        //    TrackEntry entry = skeletonGraphic.AnimationState.SetAnimation(0, "Open", false);
        //    entry.Complete += (OnAnimationComplete) =>
        //    {
        //        skeletonGraphic.AnimationState.SetAnimation(0, "OpenIdle", true);
        //        if (grandChestSkeletonGraphic != null)
        //        {
        //            grandChestSkeletonGraphic.gameObject.SetActive(true);
        //            grandChestSkeletonGraphic.Initialize(true);
        //            grandChestSkeletonGraphic.AnimationState.SetAnimation(0, "CloseIdle", false);
        //            grandChestSkeletonGraphic.freeze = true;
        //        }
        //        if (grandChestShaderController != null)
        //        {
        //            grandChestShaderController.DoDissolve(1, () =>
        //            {
        //                if (grandChestSkeletonGraphic != null)
        //                {
        //                    grandChestSkeletonGraphic.Initialize(true);
        //                    grandChestSkeletonGraphic.freeze = false;
        //                    TrackEntry entry2 = grandChestSkeletonGraphic.AnimationState.SetAnimation(0, "Open", false);
        //                    entry2.Complete += (OnAnim2) =>
        //                    {
        //                        grandChestSkeletonGraphic.AnimationState.SetAnimation(0, "OpenIdle", false);
        //                    };
        //                }
        //                DOVirtual.DelayedCall(0.5f, () =>
        //                {
        //                    ShowGrandChestRewardAnimation();
        //                });
        //            });
        //        }
        //        else
        //        {
        //            DOVirtual.DelayedCall(0.5f, () =>
        //            {
        //                ShowGrandChestRewardAnimation();
        //            });
        //        }
        //    };
        //}
        //else
        //{
        //    ShowGrandChestRewardAnimation();
        //}

        UIManager.Ins.EnableBlocker(false);
    }

    private void ShowGrandChestRewardAnimation()
    {
        RewardAnimator rwAnimator = UIManager.Ins.GetPanel() != null ? UIManager.Ins.GetPanel().RewardAnimator : null;
        if (rwAnimator != null && Manager != null && Manager.config != null)
        {
            rwAnimator.Init(ChestType.mission_control);
            foreach (var reward in Manager.config.grandChestRewardInfos)
            {
                rwAnimator.Add(MissionControlManager.MapRewardToAnimatorType(reward.type), reward.value);
            }
            rwAnimator.Play();
        }
    }

    public void ButtonClaimGrandChest()
    {
        if (buttonClaimGrandChest != null) buttonClaimGrandChest.SetActive(false);
        if (buttonClaimGrandChestBackgroundCanvasGroup != null)
        {
            buttonClaimGrandChestBackgroundCanvasGroup.DOFade(0, 0.1f).SetEase(Ease.OutSine);
        }
        ClaimGrandChest();
    }

    public void ButtonInfo()
    {
        if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.MissionControlInfo);
        }
    }

    public void ButtonClose()
    {
        if (MissionControlSmallChestNgan.isFlyingStatic) return;
        if (smallChests.Any(x => x != null && x.isClaimingRewards)) return;
        Close();
    }
}

public class PopupMissionControl : MissionControl
{
}
