using GamePackages;
using System;
using TMPro;
using UnityEngine;

public class PopupLavaQuestIntroduce : FoodPop.UICanvasPrime
{
    public TMP_Text countDownText;
    public TMP_Text descriptionText;
    public GameObject buttonObject;
    public TMP_Text buttonTmp;
    public TMP_Text rejoinTmp;
    public GameObject buttonCloseObj;

    private LavaQuestFeature Manager => LavaQuestFeature.Instance;

    private void Update()
    {
        if (Manager == null) return;

        if (countDownText != null)
        {
            countDownText.text = Manager.GetCountDownString();
        }

        int max = Manager.MaxMilestones;
        if (descriptionText != null)
        {
            if (Manager.IsCompletedToday)
            {
                buttonObject.SetActive(true);
                rejoinTmp.gameObject.SetActive(false);
                buttonTmp.text = "OK";
                descriptionText.text = "You completed today's challenge!\nCome back tomorrow to join again!";
            }
            else if (Manager.IsWaitingToRejoin())
            {
                buttonObject.SetActive(false);
                rejoinTmp.gameObject.SetActive(true);
                rejoinTmp.text = Manager.GetCountDownRejoinString();
                descriptionText.text = "You failed the challenge!\nNext attempt available in:";
            }
            else
            {
                buttonObject.SetActive(true);
                rejoinTmp.gameObject.SetActive(false);
                buttonTmp.text = "Join";
                descriptionText.text = $"<color=#FFD700>Cavern Quest</color> has started!\nBeat {max} levels to complete the challenge!";
            }
        }
    }

    // EN: Opens the introduce popup with the shared open animation.
    // VI: Mở popup giới thiệu với hiệu ứng mở dùng chung.
    public override void Open()
    {
        base.Open();
        AnimateOpen();
    }

    // EN: Closes the popup and finishes the LavaQuest home tutorial if it is running.
    // VI: Đóng popup và kết thúc hướng dẫn LavaQuest ở Home nếu đang chạy.
    public override void Close()
    {
        base.Close();
        if (LavaQuestTutorial.Ins != null) LavaQuestTutorial.Ins.Done();
    }

    public void ButtonContinue()
    {
        Close();

        if (Manager == null || !Manager.CanStartNewAttempt())
        {
            return;
        }

        FoodPop.UIManager.Instance.OpenUI<PopupLavaQuestFinding>();
    }

    // EN: Coordinates ButtonClose using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ButtonClose qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void ButtonClose()
    {
        LavaQuestFeature.Ins.CompleteTutorial();
        Close();
    }
}
