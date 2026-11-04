using System.Collections.Generic;
using GamePackages;
using Longvm.RewardAnimator;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupCardCollectionAlbum : Popup
{
    [Header("Top Header & Album Rewards")]
    [Tooltip("Biểu tượng đại diện cho chủ đề bộ sưu tập hiện tại.")]
    [SerializeField] private Image collectionIconImage;

    [Tooltip("Nhãn tên của bộ sưu tập hiện tại (ví dụ: Treats).")]
    [SerializeField] private TMP_Text collectionNameText;

    [Tooltip("Nhãn tiến độ số thẻ đã thu thập trong bộ hiện tại (ví dụ: 0/9).")]
    [SerializeField] private TMP_Text progressText;

    [Tooltip("Thanh tiến độ số thẻ đã thu thập trong bộ hiện tại.")]
    [SerializeField] private Image progressFillImage;

    [Tooltip("Danh sách các ô hiển thị phần thưởng hoàn thành bộ sưu tập.")]
    private List<CardCollectionRewardUI> rewardUIs = new();

    [Tooltip("Transform chứa các ô phần thưởng hoàn thành bộ sưu tập (tùy chọn khi sinh tự động).")]
    [SerializeField] private Transform rewardContainer;

    [Tooltip("Prefab ô phần thưởng hoàn thành bộ sưu tập (tùy chọn khi sinh tự động).")]
    [SerializeField] private CardCollectionRewardUI rewardUiPrefab;

    [Header("3x3 Card Grid (9 Cards)")]
    [Tooltip("Danh sách 9 ô thẻ trong lưới 3x3 của bộ sưu tập.")]
    [SerializeField] private List<CardCollectionCardSlotUI> cardSlots = new();

    [Header("Bottom Pagination (1/6)")]
    [Tooltip("Nhãn hiển thị trang bộ sưu tập hiện tại (ví dụ: 1/6).")]
    [SerializeField] private TMP_Text pageIndexText;

    private int _currentCollectionId;

    private CardCollectionManager Manager => CardCollectionManager.Instance;

    // EN: Opens the Collection Album popup for the specified collection index (0 to 5, defaults to 0).
    // VI: Mở popup Album chi tiết cho bộ sưu tập theo chỉ số được truyền vào (0 đến 5, mặc định là 0).
    public override void Open(object args = null)
    {
        base.Open(args);

        if (args is int id)
        {
            _currentCollectionId = Mathf.Clamp(id, 0, CardCollectionManager.TOTAL_COLLECTIONS - 1);
        }
        else
        {
            _currentCollectionId = 0;
        }

        RefreshAlbumUI();
    }

    // EN: Updates the album header, completion rewards, 3x3 card slots, and pagination indicator for the current collection.
    // VI: Cập nhật tiêu đề album, phần thưởng hoàn thành, lưới 9 ô thẻ 3x3 và chỉ số trang cho bộ sưu tập hiện tại.
    public void RefreshAlbumUI()
    {
        if (Manager == null || Manager.config == null) return;
        Manager.config.EnsureDefaultSetup();

        CardCollectionSetInfo setInfo = Manager.config.GetCollectionInfo(_currentCollectionId);
        if (setInfo != null)
        {
            if (collectionNameText != null) collectionNameText.text = setInfo.collectionName;
            if (collectionIconImage != null && setInfo.collectionIcon != null)
            {
                collectionIconImage.sprite = setInfo.collectionIcon;
            }
            RefreshSetRewardsPreview(setInfo.rewards);
        }

        int ownedCount = Manager.GetOwnedCountInCollection(_currentCollectionId);
        int totalCount = CardCollectionManager.CARDS_PER_COLLECTION;

        if (progressText != null)
        {
            progressText.text = $"{ownedCount}/{totalCount}";
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = totalCount > 0 ? (float)ownedCount / totalCount : 0f;
        }

        RefreshCardGrid();

        if (pageIndexText != null)
        {
            pageIndexText.text = $"{_currentCollectionId + 1}/{CardCollectionManager.TOTAL_COLLECTIONS}";
        }
    }

    // EN: Populates the collection completion reward UI items in the top header using CardCollectionRewardUI.
    // VI: Hiển thị các phần thưởng hoàn thành bộ sưu tập trên thanh tiêu đề bằng CardCollectionRewardUI.
    private void RefreshSetRewardsPreview(List<ResourceGame> rewards)
    {
        if (rewards == null) return;
        rewardUIs ??= new List<CardCollectionRewardUI>();

        if (rewardUIs.Count == 0 && rewardContainer != null)
        {
            rewardUIs.AddRange(rewardContainer.GetComponentsInChildren<CardCollectionRewardUI>(true));
        }

        while (rewardUIs.Count < rewards.Count)
        {
            CardCollectionRewardUI template = rewardUiPrefab != null
                ? rewardUiPrefab
                : (rewardUIs.Count > 0 ? rewardUIs[0] : null);
            Transform parent = rewardContainer != null
                ? rewardContainer
                : (rewardUIs.Count > 0 && rewardUIs[0] != null ? rewardUIs[0].transform.parent : null);

            if (template == null || parent == null) break;

            CardCollectionRewardUI item = Instantiate(template, parent);
            item.transform.localScale = Vector3.one;
            rewardUIs.Add(item);
        }

        for (int i = 0; i < rewardUIs.Count; i++)
        {
            if (rewardUIs[i] == null) continue;
            bool hasReward = i < rewards.Count;
            rewardUIs[i].gameObject.SetActive(hasReward);
            if (hasReward)
            {
                rewardUIs[i].SetReward(rewards[i]);
            }
        }
    }

    // EN: Populates the 9 card slots in the 3x3 grid with the cards belonging to the current collection.
    // VI: Cập nhật 9 ô thẻ trong lưới 3x3 với danh sách thẻ thuộc bộ sưu tập hiện tại.
    private void RefreshCardGrid()
    {
        if (cardSlots == null || Manager == null) return;
        List<CardCollectionCardInfo> setCards = Manager.GetCardsInCollection(_currentCollectionId);

        int count = Mathf.Min(cardSlots.Count, setCards.Count);
        for (int i = 0; i < count; i++)
        {
            if (cardSlots[i] != null)
            {
                bool isOwned = Manager.Data != null && Manager.Data.HasCard(setCards[i].cardId);
                cardSlots[i].Setup(setCards[i], isOwned);
            }
        }
    }

    // EN: Switches to the previous collection (wrapping from 1/6 to 6/6).
    // VI: Chuyển sang bộ sưu tập trước đó (quay vòng từ 1/6 sang 6/6).
    public void ButtonPrevCollection()
    {
        _currentCollectionId =
            (_currentCollectionId - 1 + CardCollectionManager.TOTAL_COLLECTIONS) %
            CardCollectionManager.TOTAL_COLLECTIONS;
        RefreshAlbumUI();
    }

    // EN: Switches to the next collection (wrapping from 6/6 to 1/6).
    // VI: Chuyển sang bộ sưu tập kế tiếp (quay vòng từ 6/6 về 1/6).
    public void ButtonNextCollection()
    {
        _currentCollectionId = (_currentCollectionId + 1) % CardCollectionManager.TOTAL_COLLECTIONS;
        RefreshAlbumUI();
    }

    // EN: Claims the completion reward for the current collection when all 9 cards are owned.
    // VI: Nhận phần thưởng hoàn thành cho bộ sưu tập hiện tại khi đã thu thập đủ 9 thẻ.
    public void ButtonClaimSetReward()
    {
        if (Manager == null || !Manager.CanClaimCollectionReward(_currentCollectionId)) return;

        List<ResourceGame> rewards = Manager.ClaimCollectionReward(_currentCollectionId);
        RefreshAlbumUI();

        if (rewards != null && rewards.Count > 0 && UIManager.Ins != null)
        {
            Panel panel = UIManager.Ins.GetPanel();
            RewardAnimator rwAnimator = panel != null ? panel.RewardAnimator : null;
            if (rwAnimator != null)
            {
                rwAnimator.Init();
                for (int i = 0; i < rewards.Count; i++)
                {
                    rwAnimator.Add(
                        MissionControlManager.MapRewardToAnimatorType(rewards[i].type),
                        rewards[i].value);
                }
                rwAnimator.Play();
            }
        }
    }

    // EN: Closes the Collection Album popup.
    // VI: Đóng popup Album bộ sưu tập.
    public void ButtonClose()
    {
        Close();
    }
}

