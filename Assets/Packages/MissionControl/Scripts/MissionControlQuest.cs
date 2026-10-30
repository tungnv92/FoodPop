using DG.Tweening;
using GamePackages;
using Sirenix.OdinInspector;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionControlQuest : MonoBehaviour
{
    public MissionControlQuestInfo missionControlQuestInfo;

    public Image iconImg;
    public TMP_Text[] questDescTmp;
    public Image gemImg;
    [Space]
    public Image fillImg;
    public TextMeshProUGUI fillText;
    public float fillTarget;
    public float fillDuration;
    public Tween fillTween;
    [Header("Claim")]
    public GameObject claimObject;
    [Header("Done")]
    public GameObject doneObject;

    public void OnInitt(MissionControlQuestInfo questInfo)
    {
        this.missionControlQuestInfo = questInfo;
        this.iconImg.sprite = MissionControlManager.Ins.GetQuestIcon(questInfo.questType);
        foreach (var questDesc in questDescTmp)
        {
            questDesc.text = MissionControlManager.Ins.GetQuestTitle(questInfo);
        }
        InitFill(MissionControlManager.Ins.GetFillAmount(questInfo));
        this.fillText.text = MissionControlManager.Ins.GetFillAmountText(questInfo);
        gemImg.sprite = MissionControlManager.Ins.config.gemIcons[questInfo.stageId].icon;
        Refresh();
    }

    public void InitFill(float targetFill)
    {
        fillTween?.Kill();
        fillTarget = targetFill;
        this.fillImg.fillAmount = fillTarget * 80 / 100;
        fillTween = this.fillImg.DOFillAmount(fillTarget, fillDuration).SetEase(Ease.OutSine);
    }

    public void Refresh()
    {
        int dataCount = MissionControlManager.Ins.GetDataCount(this.missionControlQuestInfo);
        int amount = missionControlQuestInfo.amount;

        if (dataCount < amount)
        {
            //Debug.Log($"[Refresh] {name} -> CanNotClaim (Progress: {dataCount}/{amount})");
            OnCanNotClaim();
        }
        else
        {
            bool isClaimed = MissionControlManager.Ins.GetIsClaimed(this.missionControlQuestInfo);
            if (isClaimed)
            {
                //Debug.Log($"[Refresh] {name} -> Claimed (Progress: {dataCount}/{amount})");
                OnClaimed();
            }
            else
            {
                //Debug.Log($"[Refresh] {name} -> CanClaim (Progress: {dataCount}/{amount})");
                OnCanClaim();
            }
        }
    }

    [Button]
    public void OnCanNotClaim()
    {
        claimObject.SetActive(false);
        doneObject.SetActive(false);
    }

    [Button]
    public void OnCanClaim()
    {
        claimObject.SetActive(true);
        doneObject.SetActive(false);
    }

    [Button]
    public void OnClaimed()
    {
        claimObject.SetActive(false);
        doneObject.SetActive(true);
    }

    public void OnClick()
    {
        if (claimObject.activeInHierarchy)
        {
            MissionControlManager.Ins.ClaimQuest(this.missionControlQuestInfo);
            if (MissionControl.Instance != null)
            {
                MissionControl.Instance.OnClaimQuest(this);
            }
            Refresh();
        }
    }
}