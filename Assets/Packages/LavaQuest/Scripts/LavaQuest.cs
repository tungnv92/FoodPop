using DG.Tweening;
using GamePackages;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main popup controller for the LavaQuest event.
/// Displays progress across 7 milestones, remaining participants, and executes jump/fall animations.
/// </summary>
public class LavaQuest : Popup
{
    public static LavaQuest Instance { get; private set; }

    public GameObject blockObj;
    public static bool isWin;
    public static bool isFail;
    public Action OnClose;
    public GameObject buttonContinueObj;
    public Transform usersParent;
    public bool isDoingAnim;

    [Header("HEADER:")]
    public TextMeshProUGUI countDownText;
    public TextMeshProUGUI levelsText;
    public TextMeshProUGUI playersText;
    public RectTransform topRectTF;

    [Header("STATE PANELS:")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject infoPanel;

    [Header("MILESTONES:")]
    public RectTransform start;
    public List<LavaQuestMileStone> lavaQuestMileStones;

    [Header("PARTICIPANTS DISPLAY:")]
    public float randomX = 55f;
    public float randomY = 20f;
    public List<LavaQuestFakeUser> lavaQuestFakeUsers = new();
    public LavaQuestFakeUser lavaQuestRealUser;
    public float delayJump = 0.03f;

    private LavaQuestManager Manager => LavaQuestManager.Instance;

    protected virtual void Awake()
    {
        Instance = this;
        EnsurePanels();
    }

    public override void Open(object args = null)
    {
        base.Open(args);
        Instance = this;
        isDoingAnim = false;
        if (blockObj != null) blockObj.SetActive(false);

        if (!Manager.Data.isDoneTutorial)
        {
            Manager.Data.isDoneTutorial = true;
            Manager.Save();
        }
        EnsurePanels();

        bool playWin = isWin;
        bool playFail = isFail;
        isWin = false;
        isFail = false;

        if (playWin)
        {
            SetPanelState(isWinState: true, isLoseState: false);
            RefreshMileStones(isWinMode: true);
            RunAnimWinStreak();
            RefreshPlayerLeftText();
            SpawnUsers(isWinMode: true, isFailMode: false);
            AnimTopRectTF();
            OnWin();
        }
        else if (playFail)
        {
            SetPanelState(isWinState: false, isLoseState: true);
            RefreshMileStones(isWinMode: false);
            RunAnimWinStreak();
            RefreshPlayerLeftText();
            SpawnUsers(isWinMode: false, isFailMode: true);
            AnimTopRectTF();
            OnFail();
        }
        else
        {
            SetPanelState(isWinState: false, isLoseState: false);
            RefreshMileStones(isWinMode: false);
            RunAnimWinStreak();
            RefreshPlayerLeftText();
            SpawnUsers(isWinMode: false, isFailMode: false);
            AnimTopRectTF();
            if (buttonContinueObj != null) buttonContinueObj.SetActive(true);
        }
    }

    public override void Close()
    {
        if (OnClose != null)
        {
            OnClose.Invoke();
            OnClose = null;
        }
        base.Close();
    }
    private void Update()
    {
        if (countDownText != null && Manager != null)
        {
            countDownText.text = Manager.GetCountDownString();
        }
    }

    private void EnsurePanels()
    {
        if (topRectTF == null) return;
        if (winPanel == null)
        {
            var t = topRectTF.Find("win");
            if (t != null) winPanel = t.gameObject;
        }
        if (losePanel == null)
        {
            var t = topRectTF.Find("lose");
            if (t != null) losePanel = t.gameObject;
        }
        if (infoPanel == null)
        {
            var t = topRectTF.Find("Info");
            if (t != null) infoPanel = t.gameObject;
        }
    }

    private void SetPanelState(bool isWinState, bool isLoseState)
    {
        EnsurePanels();
        if (winPanel != null) winPanel.SetActive(isWinState);
        if (losePanel != null) losePanel.SetActive(isLoseState);
        if (infoPanel != null) infoPanel.SetActive(!isWinState && !isLoseState);
    }

    [Button]
    public void TestJumpToNextMileStone()
    {
        if (Manager != null) Manager.AddWinStreak();
        OnWin();
    }

    public void OnWin()
    {
        RunAnimParticipantsJumpWin();
    }

    public void OnFail()
    {
        RunAnimParticipantsJumpFail();
    }

    public void SpawnUsers(bool isWinMode, bool isFailMode)
    {
        if (Manager == null || Manager.Data == null) return;

        int currentMilestone = Manager.CurrentMilestone;
        int spawnMileStoneIndex;

        if (isWinMode)
        {
            // Spawn at the previous stone position before jumping
            spawnMileStoneIndex = currentMilestone - 2;
        }
        else if (isFailMode)
        {
            // Spawn at current milestone stone before falling
            spawnMileStoneIndex = currentMilestone - 1;
        }
        else
        {
            spawnMileStoneIndex = currentMilestone - 1;
        }

        SpawnParicipantsJump(spawnMileStoneIndex);
    }

    public void RunAnimWinStreak()
    {
        if (levelsText != null && Manager != null)
        {
            levelsText.text = Manager.GetWinStreakString();
        }
    }

    public void RefreshPlayerLeftText()
    {
        if (playersText != null && Manager != null)
        {
            playersText.text = $"{Manager.RemainingParticipants}/100";
        }
    }

    public void AnimTopRectTF()
    {
        if (topRectTF == null) return;
        UIManager.Ins.EnableBlocker(true);
        Vector2 oldTopPos = new Vector2(topRectTF.anchoredPosition.x, topRectTF.anchoredPosition.y);
        topRectTF.anchoredPosition += new Vector2(0, 300);
        topRectTF.DOAnchorPos(oldTopPos, 0.3f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            UIManager.Ins.EnableBlocker(false);
        });
    }

    public void RefreshMileStones(bool isWinMode = false)
    {
        if (Manager == null || Manager.Data == null || lavaQuestMileStones == null) return;
        int currentMileStoneIndex = Manager.CurrentMilestone - 1;
        if (isWinMode)
        {
            currentMileStoneIndex -= 1;
        }

        for (int i = 0; i < lavaQuestMileStones.Count; i++)
        {
            int index = i;
            LavaQuestMileStone mileStone = lavaQuestMileStones[index];
            if (mileStone != null)
            {
                mileStone.Refresh(currentMileStoneIndex <= index, currentMileStoneIndex <= index);
            }
        }
    }

    public void RunAnimParticipantsJumpWin()
    {
        if (Manager == null || Manager.Data == null) return;
        isDoingAnim = true;
        if (buttonContinueObj != null) buttonContinueObj.SetActive(false);

        int targetStoneIndex = Manager.CurrentMilestone - 1;
        DOVirtual.DelayedCall(0.5f, () =>
        {
            ParticipantsJumpToMileStone(targetStoneIndex, true);
        });
    }

    public void RunAnimParticipantsJumpFail()
    {
        if (Manager == null || Manager.Data == null) return;
        isDoingAnim = true;
        if (buttonContinueObj != null) buttonContinueObj.SetActive(false);

        int targetStoneIndex = Manager.CurrentMilestone;
        DOVirtual.DelayedCall(0.5f, () =>
        {
            ParticipantsJumpToMileStone(targetStoneIndex, false);
        });
    }

    [Button]
    public void SpawnParicipantsJump(int mileStoneIndex)
    {
        if (lavaQuestFakeUsers == null) lavaQuestFakeUsers = new();
        while (lavaQuestFakeUsers.Count < 10 && lavaQuestRealUser != null)
        {
            LavaQuestFakeUser fake = Instantiate(lavaQuestRealUser, usersParent != null ? usersParent : transform);
            lavaQuestFakeUsers.Add(fake);
        }

        RectTransform spawnRectTF = (lavaQuestMileStones != null && mileStoneIndex >= 0 && mileStoneIndex < lavaQuestMileStones.Count)
            ? lavaQuestMileStones[mileStoneIndex].targetJumpRectTF
            : start;

        for (int i = 0; i < lavaQuestFakeUsers.Count; i++)
        {
            var fakeUser = lavaQuestFakeUsers[i];
            if (fakeUser != null)
            {
                fakeUser.gameObject.SetActive(true);
                fakeUser.OnInitt(0, 0);
                if (usersParent != null) fakeUser.transform.SetParent(usersParent);
                fakeUser.transform.localScale = Vector3.one * 0.6f;
                fakeUser.RandomFrameAndAvatar();
                fakeUser.SpawnAt(spawnRectTF, true);
            }
        }

        if (lavaQuestRealUser != null)
        {
            lavaQuestRealUser.gameObject.SetActive(true);
            int frameId = ProfileManager.instance != null && ProfileManager.instance.PlayerData != null ? ProfileManager.instance.PlayerData.frame_avatar_id : 0;
            int avatarId = ProfileManager.instance != null && ProfileManager.instance.PlayerData != null ? ProfileManager.instance.PlayerData.avatar_id : 0;
            lavaQuestRealUser.OnInitt(frameId, avatarId);
            if (usersParent != null) lavaQuestRealUser.transform.SetParent(usersParent);
            lavaQuestRealUser.transform.SetAsLastSibling();
            lavaQuestRealUser.transform.localScale = Vector3.one * 0.8f;
            lavaQuestRealUser.SpawnAt(spawnRectTF, false);
        }
    }

    [Button]
    public void ParticipantsJumpToMileStone(int mileStoneIndex, bool isWinJump)
    {
        StartCoroutine(IEParticipantsJumpToMileStone());

        IEnumerator IEParticipantsJumpToMileStone()
        {
            if (isWinJump)
            {
                if (lavaQuestRealUser != null)
                {
                    lavaQuestRealUser.JumpToMileStoneIndex(mileStoneIndex, false);
                }
                yield return new WaitForSeconds(delayJump);

                int successfulJumpCount = (mileStoneIndex == 0) ? lavaQuestFakeUsers.Count : Mathf.Max(0, lavaQuestFakeUsers.Count - 2);

                for (int i = 0; i < successfulJumpCount; i++)
                {
                    if (i < lavaQuestFakeUsers.Count && lavaQuestFakeUsers[i] != null)
                    {
                        lavaQuestFakeUsers[i].JumpToMileStoneIndex(mileStoneIndex, true);
                        if (mileStoneIndex > 0 && lavaQuestMileStones != null && mileStoneIndex - 1 < lavaQuestMileStones.Count)
                        {
                            lavaQuestMileStones[mileStoneIndex - 1].Jump();
                        }
                        yield return new WaitForSeconds(delayJump);
                    }
                }

                if (mileStoneIndex > 0)
                {
                    yield return new WaitForSeconds(0.3f);

                    for (int i = Mathf.Max(0, lavaQuestFakeUsers.Count - 2); i < lavaQuestFakeUsers.Count; i++)
                    {
                        if (lavaQuestFakeUsers[i] != null)
                        {
                            lavaQuestFakeUsers[i].Fall(i % 2 == 0);
                        }
                    }
                    if (lavaQuestMileStones != null && mileStoneIndex - 1 < lavaQuestMileStones.Count)
                    {
                        lavaQuestMileStones[mileStoneIndex - 1].ShowTire(false, true);
                    }
                }

                yield return new WaitForSeconds(0.35f);
                RefreshPlayerLeftText();
                if (buttonContinueObj != null) buttonContinueObj.SetActive(true);

                int max = Manager != null ? Manager.MaxMilestones : 7;
                if (mileStoneIndex >= max - 1)
                {
                    UIManager.Ins.OpenPopup(PopupId.LavaQuestClaim);
                }
            }
            else
            {
                int successfulJumpCount = Mathf.Max(0, lavaQuestFakeUsers.Count - 1);

                for (int i = 0; i < successfulJumpCount; i++)
                {
                    if (i < lavaQuestFakeUsers.Count && lavaQuestFakeUsers[i] != null)
                    {
                        if (mileStoneIndex > 0 && lavaQuestMileStones != null && mileStoneIndex - 1 < lavaQuestMileStones.Count)
                        {
                            lavaQuestMileStones[mileStoneIndex - 1].Jump();
                        }
                        lavaQuestFakeUsers[i].JumpToMileStoneIndex(mileStoneIndex, true);
                        yield return new WaitForSeconds(delayJump);
                    }
                }

                yield return new WaitForSeconds(0.4f);

                if (lavaQuestFakeUsers.Count > 0)
                {
                    var lastFake = lavaQuestFakeUsers.LastOrDefault(u => u != null);
                    if (lastFake != null) lastFake.Fall(false);
                }
                if (lavaQuestRealUser != null)
                {
                    lavaQuestRealUser.Fall(true);
                }

                if (mileStoneIndex > 0 && lavaQuestMileStones != null && mileStoneIndex - 1 < lavaQuestMileStones.Count)
                {
                    lavaQuestMileStones[mileStoneIndex - 1].ShowTire(false, true);
                }

                yield return new WaitForSeconds(0.4f);
                UIManager.Ins.OpenPopup(PopupId.LavaQuestFail);
                if (buttonContinueObj != null) buttonContinueObj.SetActive(true);
            }

            isDoingAnim = false;
            yield return null;
        }
    }

    public void ButtonInfo()
    {
        UIManager.Ins.OpenPopup(PopupId.LavaQuestInfo);
    }

    public void ButtonClose()
    {
        if (isDoingAnim) return;
        Close();
    }
}