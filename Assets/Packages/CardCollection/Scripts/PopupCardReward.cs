using System.Collections.Generic;
using DG.Tweening;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupCardReward : Popup
{
    [Tooltip("Gốc hiển thị khung thẻ vừa nhận được.")]
    [SerializeField] private Transform cardContainerTf;

    [Tooltip("Hình ảnh minh họa của thẻ vừa mở.")]
    [SerializeField] private Image cardArtImage;

    [Tooltip("Nhãn tên của thẻ vừa mở.")]
    [SerializeField] private TMP_Text cardNameText;

    [Tooltip("Nhãn tên bộ sưu tập chứa thẻ này.")]
    [SerializeField] private TMP_Text collectionNameText;

    [Tooltip("Danh sách các biểu tượng ngôi sao hiển thị độ hiếm (1 đến 4 sao).")]
    [SerializeField] private GameObject[] starObjects;

    [Tooltip("Nhãn 'NEW!' hiển thị khi thẻ là thẻ mới chưa từng sở hữu.")]
    [SerializeField] private GameObject newCardBadgeObject;

    [Tooltip("Khối hiển thị thông báo chuyển đổi thẻ trùng thành Vé quay.")]
    [SerializeField] private GameObject duplicateTicketRoot;

    [Tooltip("Nhãn hiển thị số lượng Vé nhận được khi thẻ bị trùng (ví dụ: +10).")]
    [SerializeField] private TMP_Text duplicateTicketAmountText;

    [Tooltip("Nhãn chỉ số thẻ hiện tại khi mở gói nhiều thẻ (ví dụ: 1/3).")]
    [SerializeField] private TMP_Text cardCounterText;

    private readonly List<CardDropResult> _pendingDrops = new();
    private int _currentIndex;

    private CardCollectionManager Manager => CardCollectionManager.Instance;

    // EN: Opens the Card Reward reveal popup for a single CardDropResult or a List<CardDropResult>.
    // VI: Mở popup lật thẻ thưởng cho một thẻ đơn lẻ (CardDropResult) hoặc danh sách nhiều thẻ (List<CardDropResult>).
    public override void Open(object args = null)
    {
        base.Open(args);
        _pendingDrops.Clear();
        _currentIndex = 0;

        if (args is CardDropResult singleResult)
        {
            _pendingDrops.Add(singleResult);
        }
        else if (args is List<CardDropResult> multiResults)
        {
            _pendingDrops.AddRange(multiResults);
        }
        else if (Manager != null && Manager.Data != null && Manager.Data.pendingDroppedCardId >= 0)
        {
            CardCollectionCardInfo info = Manager.config != null
                ? Manager.config.GetCardInfo(Manager.Data.pendingDroppedCardId)
                : null;
            if (info != null)
            {
                _pendingDrops.Add(new CardDropResult
                {
                    cardId = info.cardId,
                    collectionId = info.collectionId,
                    rarity = info.rarity,
                    isDuplicate = Manager.Data.pendingDroppedCardWasDuplicate,
                    ticketsConverted = Manager.Data.pendingDroppedTicketsEarned,
                });
            }
            Manager.Data.ClearPendingCardDrop();
            Manager.Save();
        }

        if (_pendingDrops.Count == 0)
        {
            Close();
            return;
        }

        ShowCurrentCard();
    }

    // EN: Displays the card at _currentIndex with a scale-in reveal animation and duplicate ticket conversion info.
    // VI: Hiển thị thẻ tại vị trí _currentIndex cùng hiệu ứng phóng to mở thẻ và thông tin quy đổi vé nếu trùng.
    private void ShowCurrentCard()
    {
        if (_currentIndex < 0 || _currentIndex >= _pendingDrops.Count)
        {
            Close();
            return;
        }

        CardDropResult drop = _pendingDrops[_currentIndex];
        CardCollectionCardInfo cardInfo = Manager != null && Manager.config != null
            ? Manager.config.GetCardInfo(drop.cardId)
            : null;
        CardCollectionSetInfo setInfo = Manager != null && Manager.config != null
            ? Manager.config.GetCollectionInfo(drop.collectionId)
            : null;

        if (cardNameText != null)
        {
            cardNameText.text = cardInfo != null ? cardInfo.cardName : $"Card #{drop.cardId + 1}";
        }

        if (collectionNameText != null)
        {
            collectionNameText.text = setInfo != null ? setInfo.collectionName : "";
        }

        if (cardArtImage != null && cardInfo != null && cardInfo.cardSprite != null)
        {
            cardArtImage.sprite = cardInfo.cardSprite;
        }

        int starCount = (int)drop.rarity;
        if (starObjects != null)
        {
            for (int i = 0; i < starObjects.Length; i++)
            {
                if (starObjects[i] != null)
                {
                    starObjects[i].SetActive(i < starCount);
                }
            }
        }

        if (newCardBadgeObject != null)
        {
            newCardBadgeObject.SetActive(!drop.isDuplicate);
        }

        if (duplicateTicketRoot != null)
        {
            duplicateTicketRoot.SetActive(drop.isDuplicate);
        }

        if (duplicateTicketAmountText != null && drop.isDuplicate)
        {
            duplicateTicketAmountText.text = $"+{drop.ticketsConverted}";
        }

        if (cardCounterText != null)
        {
            cardCounterText.gameObject.SetActive(_pendingDrops.Count > 1);
            cardCounterText.text = $"{_currentIndex + 1}/{_pendingDrops.Count}";
        }

        if (cardContainerTf != null)
        {
            cardContainerTf.DOKill();
            cardContainerTf.localScale = Vector3.one * 0.3f;
            cardContainerTf.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
    }

    // EN: Advances to the next card in the pack, or closes the popup if all cards have been shown.
    // VI: Chuyển sang thẻ tiếp theo trong gói, hoặc đóng popup nếu đã xem hết tất cả thẻ.
    public void ButtonNextOrClose()
    {
        _currentIndex++;
        if (_currentIndex < _pendingDrops.Count)
        {
            ShowCurrentCard();
        }
        else
        {
            Close();
        }
    }
}

