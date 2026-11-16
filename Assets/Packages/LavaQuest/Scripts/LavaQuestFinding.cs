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

    // EN: Coordinates AnimShowUsers using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối AnimShowUsers qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    // EN: Coordinates AnimShowUsers using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối AnimShowUsers qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
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

        int frameId = ProfileManager.instance.PlayerDataManager.IsInitialized ? ProfileManager.instance.PlayerDataManager.FrameId : 0;
        int avatarId = ProfileManager.instance.PlayerDataManager.IsInitialized ? ProfileManager.instance.PlayerDataManager.AvatarId : 0;
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

    // EN: Coordinates ButtonContinue using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ButtonContinue qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    // EN: Coordinates ButtonContinue using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối ButtonContinue qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void ButtonContinue()
    {
        Close();
        if (Manager != null)
        {
            Manager.StartNewAttempt();
        }

        int currentLevel = ProfileManager.instance.PlayerDataManager.IsInitialized ? ProfileManager.instance.PlayerDataManager.GetCurrentLevel() : 1;
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

        if (Manager != null && Manager.IsInitialized && !Manager.HasShownInfo)
        {
            UIManager.Ins.OpenPopup(PopupId.LavaQuestInfo);
        }
    }
}
