using DG.Tweening;
using GamePackages;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PopupWinStreak : Popup
{
    [Title("TOP:")]
    public RectTransform topRectTF;
    [Title("COUNTDOWN:")]
    public TMP_Text countDownText;
    [Title("WINSTREAK:")]
    public RectTransform winStreakRectTF;
    public TMP_Text winStreak_TMP;
    public List<int> winStreakMultis = new();
    [Title("PROGRESS:")]
    public Image progressBarImg;
    public TMP_Text progressBarText;
    public WinStreakRewardUI preReward;
    public WinStreakRewardUI nextReward;

    [Title("REWARDS:")]
    [FormerlySerializedAs("treasureItems")]
    public List<WinStreakItem> winStreakItems = new List<WinStreakItem>();
    public List<WinStreakItem> treasureItems => winStreakItems;
    public RectTransform contentRectTF;
    public float topHeight = 850;
    public float paddingBottom = 420f;
    public float spacing = 280f;
    public RectTransform lineTf;

    private WinStreakManager Manager => WinStreakManager.Instance;
    private string _openedRoundId;
    private int _lastNewDayFrame = -1;

    private void OnEnable()
    {
        if (Manager != null)
        {
            Manager.onDataChanged += OnDataChangedHandler;
            Manager.onNewDay += OnNewDay;
        }
        Observer.OnNewday += OnNewDay;
    }

    private void OnDisable()
    {
        if (Manager != null)
        {
            Manager.onDataChanged -= OnDataChangedHandler;
            Manager.onNewDay -= OnNewDay;
        }
        Observer.OnNewday -= OnNewDay;
    }

    /// <summary>
    /// Callback invoked when a new day occurs.
    /// If the current round has ended, closes the popup to reset the UI for the new round.
    /// </summary>
    // EN: Coordinates OnNewDay using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối OnNewDay qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void OnNewDay()
    {
        if (Time.frameCount == _lastNewDayFrame) return;
        _lastNewDayFrame = Time.frameCount;

        if (Manager == null) return;

        bool isRoundEnd = Manager.WasRoundEndedOnNewDay
            || Manager.IsCurrentRoundEnded()
            || (!string.IsNullOrEmpty(_openedRoundId) && Manager.IsInitialized && Manager.SavedRoundId != _openedRoundId);

        if (isRoundEnd)
        {
            ClosePopupForNewRound();
        }
        else
        {
            OnDataChangedHandler();
        }
    }

    private void ClosePopupForNewRound()
    {
        if (UIManager.Ins != null && UIManager.Ins.IsPopupOpen(PopupId.WinStreakInfo))
        {
            UIManager.Ins.ClosePopup(PopupId.WinStreakInfo);
        }
        Close();
    }

    // EN: Coordinates OnDataChangedHandler using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối OnDataChangedHandler qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    private void OnDataChangedHandler()
    {
        // If data changed because round ended and reset, close popup to reset UI for new round
        if (Manager != null && Manager.IsInitialized && !string.IsNullOrEmpty(_openedRoundId) && Manager.SavedRoundId != _openedRoundId)
        {
            ClosePopupForNewRound();
            return;
        }

        RefreshWinStreak();
        InitWinStreakItems();
        InitContentHeightAndPos();
        InitProgress();
    }

    // EN: Opens the view using manager queries and commands instead of mutable save models.
    // VI: Mở giao diện bằng truy vấn và lệnh manager thay vì model lưu có thể sửa.
    public override void Open(object args = null)
    {
        base.Open();
        _openedRoundId = Manager != null && Manager.IsInitialized ? Manager.SavedRoundId : string.Empty;
        RefreshWinStreak();
        InitWinStreakItems();
        InitContentHeightAndPos();
        InitProgress();

        if (Manager != null && Manager.IsInitialized && !Manager.HasShownInfo)
        {
            StartCoroutine(ShowFirstInfoRoutine());
            Manager.MarkInfoShown();
        }
    }

    IEnumerator ShowFirstInfoRoutine()
    {
        SetContentPosAtIndex(25);
        yield return new WaitForSeconds(0.5f);
        contentRectTF.DOAnchorPosY(0, 2f).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(2.5f);
        UIManager.Ins.OpenPopup(PopupId.WinStreakInfo);
    }

    private void Update()
    {
        if (Manager == null) return;

        if (countDownText != null)
        {
            countDownText.text = Manager.GetCountDownString();
        }
    }

    public void RefreshWinStreak()
    {
        int streak = Manager != null ? Manager.WinStreakTier : 0;
        if (winStreakRectTF != null)
        {
            float posX = -310f + streak * 155f;
            winStreakRectTF.anchoredPosition = new Vector2(posX, 0f);
        }
        if (winStreak_TMP != null && winStreakMultis != null && streak >= 0 && streak < winStreakMultis.Count)
        {
            winStreak_TMP.text = $"x{winStreakMultis[streak]}";
        }
    }

    [FormerlySerializedAs("treasureItemPrefab")]
    [SerializeField] private WinStreakItem winStreakItemPrefab;
    public WinStreakItem treasureItemPrefab => winStreakItemPrefab;

    public void InitWinStreakItems()
    {
        foreach (var item in winStreakItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        winStreakItems.Clear();

        if (Manager == null || Manager.WinStreakGlobalConfig == null || Manager.WinStreakGlobalConfig.winStreakMileStoneInfos == null)
            return;

        if (winStreakItemPrefab == null)
        {
            winStreakItemPrefab = Resources.Load<WinStreakItem>("WinStreakItem");
        }

        var milestones = Manager.WinStreakGlobalConfig.winStreakMileStoneInfos;
        for (int i = 0; i < milestones.Count; i++)
        {
            WinStreakMileStoneInfo info = milestones[i];
            if (winStreakItemPrefab == null) break;
            WinStreakItem item = Instantiate(winStreakItemPrefab, contentRectTF);
            item.transform.localScale = Vector3.one;
            item.rectTransform.anchoredPosition = new Vector2(0, paddingBottom + spacing * winStreakItems.Count);
            item.OnInitt(i, info.rewards);
            winStreakItems.Add(item);
        }

        ReverseChildList(winStreakItems.Select(x => x.transform).ToList());
    }

    public void InitTreasureItems() => InitWinStreakItems();

    public static void ReverseChildList(List<Transform> children)
    {
        var indices = new int[children.Count];
        for (int i = 0; i < children.Count; i++)
        {
            indices[i] = children[i].GetSiblingIndex();
        }
        for (int i = 0; i < children.Count; i++)
        {
            children[i].SetSiblingIndex(indices[children.Count - 1 - i]);
        }
    }

    public void InitContentHeightAndPos()
    {
        if (contentRectTF == null) return;
        float screenHeight = myRt != null ? myRt.rect.height : (transform as RectTransform).rect.height;
        if (screenHeight <= 0) screenHeight = 1920f;
        float botHeight = screenHeight - topHeight;
        float hiddenItemsHeight = paddingBottom + spacing * winStreakItems.Count - botHeight;
        contentRectTF.sizeDelta = new Vector2(contentRectTF.sizeDelta.x, screenHeight + hiddenItemsHeight);

        int currentIndex = Manager != null && Manager.GetCurrentMileStoneInfo() != null ? Manager.GetCurrentMileStoneInfo().index : 0;
        contentRectTF.anchoredPosition = new Vector2(contentRectTF.anchoredPosition.x, -currentIndex * spacing);
        if (contentRectTF.anchoredPosition.y < -hiddenItemsHeight)
        {
            contentRectTF.anchoredPosition = new Vector2(contentRectTF.anchoredPosition.x, -hiddenItemsHeight);
        }

        lineTf.transform.position = winStreakItems[0].MilestoneTextTf.position;
        lineTf.sizeDelta = new Vector2(lineTf.sizeDelta.x, spacing * (winStreakItems.Count - 1));
    }

    public void SetContentPosAtIndex(int index)
    {
        if (contentRectTF == null) return;
        float screenHeight = myRt != null ? myRt.rect.height : (transform as RectTransform).rect.height;
        if (screenHeight <= 0) screenHeight = 1920f;
        float botHeight = screenHeight - topHeight;
        float hiddenItemsHeight = paddingBottom + spacing * winStreakItems.Count - botHeight;

        contentRectTF.anchoredPosition = new Vector2(contentRectTF.anchoredPosition.x, -index * spacing);
        if (contentRectTF.anchoredPosition.y < -hiddenItemsHeight)
        {
            contentRectTF.anchoredPosition = new Vector2(contentRectTF.anchoredPosition.x, -hiddenItemsHeight);
        }
    }

    public void InitProgress()
    {
        if (Manager == null) return;
        WinStreakMileStoneInfo pre_milestone = Manager.GetPreviousMileStoneInfo();
        WinStreakMileStoneInfo upcoming_milestone = Manager.GetCurrentMileStoneInfo();
        if (upcoming_milestone == null) return;

        int total_length = upcoming_milestone.screwAmount;
        int current = Manager.GetPointsInMilestone(Manager.CurrentKeys, upcoming_milestone.index);
        progressBarImg.fillAmount = total_length > 0 ? current * 1f / total_length : 1f;
        progressBarText.text = $"{current}/{total_length}";

        preReward.gameObject.SetActive(false);
        if (pre_milestone != null)
        {
            preReward.InitReward(pre_milestone.rewards);
        }

        nextReward.InitReward(upcoming_milestone.rewards);
    }

    public void ButtonInfo()
    {
        UIManager.Ins.OpenPopup(PopupId.WinStreakInfo);
    }

    public void ButtonClose()
    {
        Close();
    }
}

public class PopupTreasure : PopupWinStreak
{
}
