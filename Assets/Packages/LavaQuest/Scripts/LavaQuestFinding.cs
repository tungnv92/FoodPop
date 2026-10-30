using DG.Tweening;
using GamePackages;
//using SDK;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupLavaQuestFinding : Popup
{
    public TextMeshProUGUI playersText;
    public LavaQuestFakeUser realUser;
    public List<LavaQuestFakeUser> fakeUsers;
    public GameObject buttonContinueObj;
    public float duration = 2.5f;

    private LavaQuestManager Manager => LavaQuestManager.Instance;

    public override void Open(object args = null)
    {
        base.Open(args);
        if (buttonContinueObj != null) buttonContinueObj.SetActive(false);
        AnimText();
        AnimShowUsers();
    }

    public void AnimShowUsers()
    {
        if (fakeUsers != null)
        {
            foreach (LavaQuestFakeUser fakeUser in fakeUsers)
            {
                if (fakeUser != null)
                {
                    fakeUser.RandomFrameAndAvatar();
                    fakeUser.gameObject.SetActive(false);
                    fakeUser.rectTransform.localScale = Vector3.zero;
                }
            }
        }

        int frameId = ProfileManager.instance != null && ProfileManager.instance.PlayerData != null ? ProfileManager.instance.PlayerData.frame_avatar_id : 0;
        int avatarId = ProfileManager.instance != null && ProfileManager.instance.PlayerData != null ? ProfileManager.instance.PlayerData.avatar_id : 0;
        if (realUser != null)
        {
            realUser.OnInitt(frameId, avatarId);
            realUser.gameObject.SetActive(false);
        }

        int count = fakeUsers != null ? fakeUsers.Count : 0;
        float delayEachFakeUser = count > 0 ? (duration * 0.8f / count) : 0.1f;
        StartCoroutine(IEAnimShowUsers());

        IEnumerator IEAnimShowUsers()
        {
            yield return new WaitForSeconds(0.3f);
            if (realUser != null) realUser.gameObject.SetActive(true);
            yield return new WaitForSeconds(delayEachFakeUser);
            if (fakeUsers != null)
            {
                fakeUsers.Reverse();
                foreach (LavaQuestFakeUser fakeUser in fakeUsers)
                {
                    if (fakeUser != null)
                    {
                        fakeUser.gameObject.SetActive(true);
                        fakeUser.rectTransform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                    }
                    yield return new WaitForSeconds(delayEachFakeUser);
                }
            }
        }
    }

    public void AnimText()
    {
        int playerAmount = 100;
        DOVirtual.Int(0, playerAmount, duration, v =>
        {
            if (playersText != null) playersText.text = $"{v}/{playerAmount}";
        }).SetDelay(0.3f).SetEase(Ease.OutSine).OnComplete(() =>
        {
            if (buttonContinueObj != null) buttonContinueObj.SetActive(true);
        });
    }

    // EN: Closes the matchmaking popup, starts a new LavaQuest attempt, and opens or refreshes the LavaQuest view.
    // VI: Đóng popup tìm người chơi, bắt đầu lượt thử LavaQuest mới và mở hoặc làm mới giao diện LavaQuest.
    public void ButtonContinue()
    {
        Close();
        if (Manager != null)
        {
            Manager.StartNewAttempt();
        }

        int currentLevel = ProfileManager.instance != null && ProfileManager.instance.PlayerData != null ? ProfileManager.instance.PlayerData.currentLevel : 1;
        //if (ABIAnalyticsManager.Instance != null)
        //{
        //    ABIAnalyticsManager.Instance.LavaQuestStart(currentLevel);
        //}

        if (LavaQuest.Instance != null)
        {
            LavaQuest.Instance.RefreshMileStones();
            LavaQuest.Instance.RefreshPlayerLeftText();
            LavaQuest.Instance.RunAnimWinStreak();
            LavaQuest.Instance.SpawnUsers(false, false);
        }
        else
        {
            UIManager.Ins.OpenPopup(PopupId.LavaQuest);
        }

        if (Manager != null && Manager.Data != null && !Manager.Data.isShowedInfo)
        {
            UIManager.Ins.OpenPopup(PopupId.LavaQuestInfo);
        }
    }
}
