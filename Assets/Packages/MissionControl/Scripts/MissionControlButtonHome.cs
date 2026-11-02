using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionControlButtonHome : MonoBehaviour
{
    [SerializeField] private GameObject unlockedObject;
    [SerializeField] private GameObject lockedObject;
    [SerializeField] private TMP_Text unlock_tmp;
    [SerializeField] private GameObject notiGo;
    [Header("TUT")]
    [SerializeField] private GameObject tutObj;
    [SerializeField] private GameObject maskObj;
    [SerializeField] private HandTutAnim handAnim;

    public TextMeshProUGUI countDownText;

    private MissionControlManager Manager => MissionControlManager.Instance;

    private void Start()
    {
        RefreshState();
    }

    private void OnEnable()
    {
        RefreshState();
    }

    public bool CheckShowTut()
    {
        if (Manager == null || !Manager.IsLevelUnlocked) return false;
        if (Manager.Data == null || Manager.Data.isDoneTutorial) return false;

        tutObj.SetActive(true);
        unlockedObject.SetActive(false);
        maskObj.SetActive(true);
        handAnim.ShowTouchAnim(transform.position, Vector2.zero, () =>
        {
            OnClick();
            tutObj.SetActive(false);
            unlockedObject.SetActive(true);
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
            countDownText.text = Manager.GetCountDownString();
        }

        if (notiGo != null)
        {
            notiGo.SetActive(Manager.IsNoti());
        }
    }

    public void RefreshState()
    {
        if (Manager == null)
        {
            gameObject.SetActive(false);
            return;
        }

        bool isUnlocked = Manager.IsLevelUnlocked;
        bool isBetweenDays = Manager.IsInEventDays();

        gameObject.SetActive(Manager.isActive || (isBetweenDays && !isUnlocked));

        if (unlockedObject != null) unlockedObject.SetActive(isUnlocked);
        if (lockedObject != null) lockedObject.SetActive(!isUnlocked);
        if (unlock_tmp != null) unlock_tmp.text = $"Level {Manager.UnlockLevel}";
        if (notiGo != null) notiGo.SetActive(Manager.IsNoti());
    }

    public void OnClick()
    {
        if (Manager == null || !Manager.isActive || !Manager.IsLevelUnlocked) return;
        if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.MissionControl);
        }
    }
}