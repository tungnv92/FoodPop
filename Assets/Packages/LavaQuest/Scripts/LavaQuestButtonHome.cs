using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LavaQuestButtonHome : MonoBehaviour
{
    [SerializeField] private GameObject unlockedObject;
    [SerializeField] private GameObject lockedObject;
    [SerializeField] private TMP_Text unlock_tmp;
    [SerializeField] private GameObject notiGo;
    [Header("TUT")]
    [SerializeField] private GameObject tutObj;
    [SerializeField] private GameObject notiTutObj;
    [SerializeField] private GameObject maskObj;
    [SerializeField] private HandTutAnim handAnim;

    public TextMeshProUGUI countDownText;

    private LavaQuestManager Manager => LavaQuestManager.Instance;

    private void Start()
    {
        if (Manager == null || !Manager.IsEnabled)
        {
            gameObject.SetActive(false);
            return;
        }
        RefreshState();
    }

    public bool CheckShowTut()
    {
        if (!Manager.IsLevelUnlocked) return false;
        if (Manager.Data.isDoneTutorial) return false;

        tutObj.SetActive(true);
        notiTutObj.SetActive(true);
        maskObj.SetActive(true);
        handAnim.ShowTouchAnim(transform.position, Vector2.zero, () =>
        {
            OnClick();
            tutObj.SetActive(false);
            notiTutObj.SetActive(false);
            maskObj.SetActive(false);
            UIManager.Ins.EnableBlocker(false);
        });

        return true;
    }

    private void Update()
    {
        if (Manager == null || !Manager.isActive) return;

        if (countDownText != null)
        {
            if (Manager.IsAttemptActive)
            {
                countDownText.text = Manager.GetWinStreakString();
            }
            else if (Manager.IsCompletedToday)
            {
                countDownText.text = "Finished";
            }
            else if (Manager.IsWaitingToRejoin())
            {
                countDownText.text = Manager.GetCountDownRejoinString();
            }
            else
            {
                countDownText.text = Manager.GetCountDownString();
            }
        }

        if (notiGo != null)
        {
            notiGo.SetActive(Manager.IsNoti());
        }
    }

    public void RefreshState()
    {
        if (Manager == null || !Manager.IsEnabled)
        {
            gameObject.SetActive(false);
            return;
        }
        bool unlocked = Manager.IsLevelUnlocked;
        if (unlockedObject != null) unlockedObject.SetActive(unlocked);
        if (lockedObject != null) lockedObject.SetActive(!unlocked);
        if (unlock_tmp != null) unlock_tmp.text = $"Level {Manager.UnlockLevel}";
    }

    public void OnClick()
    {
        if (Manager == null || !Manager.isActive || !Manager.IsLevelUnlocked) return;

        if (Manager.IsAttemptActive)
        {
            UIManager.Ins.OpenPopup(PopupId.LavaQuest);
        }
        else
        {
            UIManager.Ins.OpenPopup(PopupId.LavaQuestIntroduce);
        }
    }
}