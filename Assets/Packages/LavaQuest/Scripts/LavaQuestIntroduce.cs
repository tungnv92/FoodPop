using GamePackages;
using Longvm.UIAnimator;
using System;
using TMPro;
using UnityEngine;

public class PopupLavaQuestIntroduce : Popup
{
    [SerializeField] UIAnimator openAnim;
    [SerializeField] UIAnimator closeAnim;
    public TMP_Text countDownText;
    public TMP_Text descriptionText;
    public GameObject buttonObject;
    public TMP_Text buttonTmp;
    public TMP_Text rejoinTmp;
    public GameObject buttonCloseObj;

    private LavaQuestManager Manager => LavaQuestManager.Instance;

    public override void Open(object args = null)
    {
        base.Open(args);
        if (openAnim != null) openAnim.InitAndPlay();
    }

    // EN: Plays the close animation if configured, closes the popup, and completes the LavaQuest tutorial.
    // VI: Phát hiệu ứng đóng nếu được cấu hình, đóng popup và hoàn tất hướng dẫn LavaQuest.
    public override void Close()
    {
        //if (AudioManager.Ins != null)
        //{
        //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
        //}
        if (closeAnim != null)
        {
            closeAnim.Play(() =>
            {
                base.Close();
                if (LavaQuestTutorial.Ins != null)
                {
                    LavaQuestTutorial.Ins.Done();
                }
            });
        }
        else
        {
            base.Close();
        }
    }

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

    public void ButtonContinue()
    {
        Close();

        if (Manager == null || !Manager.CanStartNewAttempt())
        {
            return;
        }

        if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.LavaQuestFinding);
        }
    }

    // EN: Coordinates ButtonClose using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ButtonClose qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void ButtonClose()
    {
        LavaQuestManager.Ins.CompleteTutorial();
        Close();
    }
}
