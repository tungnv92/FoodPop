using System.Collections.Generic;
using DG.Tweening;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupCardReward : Popup
{
    [Tooltip("Gốc Transform thực hiện hiệu ứng phóng to, lật thẻ và dịch chuyển khi trùng (nếu để trống sẽ dùng Transform của cardArtImage).")]
    [SerializeField] private Transform cardContainerTf;

    [Tooltip("Hình ảnh hiển thị mặt sau (BackCard theo số sao) rồi lật sang mặt trước của thẻ nhận được.")]
    [SerializeField] private Image cardArtImage;

    [Tooltip("Nhãn tên của thẻ vừa mở (tùy chọn).")]
    [SerializeField] private TMP_Text cardNameText;

    [Tooltip("Nhãn 'NEW!' hiển thị sau khi lật nếu thẻ là thẻ mới chưa từng sở hữu.")]
    [SerializeField] private GameObject newCardBadgeObject;

    [Tooltip("Khối hiển thị thông báo chuyển đổi thẻ trùng thành Vé quay (bao gồm mũi tên và vé, hoặc chỉ cụm vé).")]
    [SerializeField] private GameObject duplicateTicketRoot;

    [Tooltip("Transform mũi tên chuyển đổi sang vé khi thẻ trùng (tùy chọn nếu tách riêng khỏi duplicateTicketRoot).")]
    [SerializeField] private Transform duplicateArrowTf;

    [Tooltip("Nhãn hiển thị số lượng Vé nhận được khi thẻ bị trùng (ví dụ: 100).")]
    [SerializeField] private TMP_Text duplicateTicketAmountText;

    [Tooltip("Nhãn chỉ số thẻ hiện tại khi mở gói nhiều thẻ (ví dụ: 1/3).")]
    [SerializeField] private TMP_Text cardCounterText;

    [Tooltip("Nút Tiếp tục chỉ hiển thị sau khi toàn bộ hiệu ứng lật thẻ (và chuyển đổi thẻ trùng) hoàn tất.")]
    [SerializeField] private Button continueButton;

    [Header("Flip & Duplicate Transition Settings")]
    [Tooltip("Thời gian dừng hiển thị mặt sau (BackCard) trước khi bắt đầu lật thẻ (giây).")]
    [SerializeField] private float delayBeforeFlip = 0.25f;

    [Tooltip("Thời gian cho mỗi nửa quá trình lật thẻ (thu hẹp ngang và mở rộng mặt trước).")]
    [SerializeField] private float flipHalfDuration = 0.18f;

    [Tooltip("Thời gian dừng sau khi lật lộ mặt trước thẻ trước khi chuyển sang bố cục thẻ trùng (giây).")]
    [SerializeField] private float delayBeforeDuplicateTransition = 0.3f;

    [Tooltip("Thời gian diễn hoạt dịch chuyển thẻ trùng sang trái và thu nhỏ (giây).")]
    [SerializeField] private float duplicateTransitionDuration = 0.3f;

    [Tooltip("Vị trí AnchoredPosition của thẻ sau khi dịch sang trái ở bố cục thẻ trùng.")]
    [SerializeField] private Vector2 duplicateCardAnchoredPos = new Vector2(-260f, 0f);

    [Tooltip("Tỉ lệ thu nhỏ của thẻ sau khi dịch sang trái ở bố cục thẻ trùng.")]
    [SerializeField] private float duplicateCardScale = 0.7f;

    private readonly List<CardDropResult> _pendingDrops = new();
    private int _currentIndex;
    private bool _isFlipping;
    private Sequence _flipSequence;
    private bool _cachedInitialPos;
    private Vector2 _initialCardAnchoredPos;

    private CardCollectionManager Manager => CardCollectionManager.Instance;

    // EN: Binds the continue button click listener on awake.
    // VI: Gắn sự kiện nhấn cho nút Tiếp tục khi khởi tạo.
    private void Awake()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ButtonContinue);
            continueButton.onClick.AddListener(ButtonContinue);
        }
    }

    // EN: Kills any active flip animation sequence when the popup is disabled.
    // VI: Hủy chuỗi hiệu ứng lật thẻ đang chạy khi popup bị tắt.
    private void OnDisable()
    {
        KillFlipSequence();
    }

    // EN: Opens the Card Reward reveal popup for a single CardDropResult or a List<CardDropResult>.
    // VI: Mở popup nhận thẻ thưởng cho một thẻ đơn lẻ (CardDropResult) hoặc danh sách nhiều thẻ (List<CardDropResult>).
    public override void Open(object args = null)
    {
        base.Open(args);
        CacheInitialCardPositionIfNeeded();
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

    // EN: Caches the default centered anchored position of the card container on first use.
    // VI: Lưu lại vị trí neo ở giữa mặc định của khung chứa thẻ trong lần chạy đầu tiên.
    private void CacheInitialCardPositionIfNeeded()
    {
        if (_cachedInitialPos) return;
        Transform targetTf = GetCardTransform();
        if (targetTf is RectTransform rt)
        {
            _initialCardAnchoredPos = rt.anchoredPosition;
            _cachedInitialPos = true;
        }
    }

    // EN: Returns the Transform used to animate the card (cardContainerTf or cardArtImage.transform).
    // VI: Trả về Transform dùng để diễn hoạt thẻ (cardContainerTf hoặc cardArtImage.transform).
    private Transform GetCardTransform()
    {
        return cardContainerTf != null
            ? cardContainerTf
            : (cardArtImage != null ? cardArtImage.transform : transform);
    }

    // EN: Displays the BackCard sprite at the center, then plays the flip animation and optional duplicate transition.
    // VI: Hiển thị sprite mặt sau (BackCard) ở chính giữa, sau đó phát hiệu ứng lật thẻ và chuyển đổi bố cục nếu thẻ trùng.
    private void ShowCurrentCard()
    {
        KillFlipSequence();

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

        Sprite backSprite = Manager != null && Manager.config != null
            ? Manager.config.GetBackCardSprite(drop.rarity)
            : null;
        Sprite frontSprite = cardInfo != null ? cardInfo.cardSprite : null;

        Transform targetTf = GetCardTransform();
        if (targetTf is RectTransform cardRt && _cachedInitialPos)
        {
            cardRt.anchoredPosition = _initialCardAnchoredPos;
        }

        if (cardArtImage != null)
        {
            cardArtImage.enabled = true;
            cardArtImage.sprite = backSprite != null ? backSprite : frontSprite;
        }

        if (cardNameText != null)
        {
            cardNameText.gameObject.SetActive(false);
            cardNameText.text = cardInfo != null ? cardInfo.cardName : $"Card #{drop.cardId + 1}";
        }

        if (newCardBadgeObject != null)
        {
            newCardBadgeObject.SetActive(false);
        }

        if (duplicateArrowTf != null)
        {
            duplicateArrowTf.gameObject.SetActive(false);
        }

        if (duplicateTicketRoot != null)
        {
            duplicateTicketRoot.SetActive(false);
        }

        if (duplicateTicketAmountText != null && drop.isDuplicate)
        {
            duplicateTicketAmountText.text = drop.ticketsConverted.ToString();
        }

        if (cardCounterText != null)
        {
            cardCounterText.gameObject.SetActive(_pendingDrops.Count > 1);
            cardCounterText.text = $"{_currentIndex + 1}/{_pendingDrops.Count}";
        }

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }

        PlayCardFlipAndDuplicateTransition(drop, frontSprite);
    }

    // EN: Runs the card pop-in and horizontal flip animation, followed by the duplicate layout transition if the card is a duplicate.
    // VI: Chạy hiệu ứng xuất hiện và lật thẻ ngang, tiếp nối bằng hiệu ứng chuyển sang bố cục quy đổi vé nếu là thẻ trùng.
    private void PlayCardFlipAndDuplicateTransition(CardDropResult drop, Sprite frontSprite)
    {
        Transform targetTf = GetCardTransform();

        _isFlipping = true;
        targetTf.DOKill();
        targetTf.localScale = Vector3.one * 0.4f;

        _flipSequence = DOTween.Sequence();
        _flipSequence.Append(targetTf.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
        _flipSequence.AppendInterval(Mathf.Max(0f, delayBeforeFlip));
        _flipSequence.Append(targetTf.DOScaleX(0f, Mathf.Max(0.05f, flipHalfDuration)).SetEase(Ease.InQuad));
        _flipSequence.AppendCallback(() =>
        {
            if (cardArtImage != null && frontSprite != null)
            {
                cardArtImage.sprite = frontSprite;
            }
            RevealCardFrontInfo(drop);
        });
        _flipSequence.Append(targetTf.DOScaleX(1f, Mathf.Max(0.05f, flipHalfDuration)).SetEase(Ease.OutBack));

        if (drop.isDuplicate)
        {
            AppendDuplicateLayoutTransition(targetTf);
        }

        _flipSequence.OnComplete(() =>
        {
            _isFlipping = false;
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.transform.DOKill();
                continueButton.transform.localScale = Vector3.zero;
                continueButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            }
        });
    }

    // EN: Shows the card name and NEW badge when the card flips to reveal its front face.
    // VI: Hiển thị tên thẻ và nhãn NEW ngay khi thẻ lật sang mặt trước.
    private void RevealCardFrontInfo(CardDropResult drop)
    {
        if (cardNameText != null)
        {
            cardNameText.gameObject.SetActive(true);
        }

        if (newCardBadgeObject != null)
        {
            newCardBadgeObject.SetActive(!drop.isDuplicate);
        }
    }

    // EN: Appends the animation that shifts and scales down the revealed duplicate card to the left while popping in the arrow and ticket reward on the right.
    // VI: Thêm chuỗi hiệu ứng dịch chuyển và thu nhỏ thẻ trùng sang bên trái đồng thời bật mũi tên cùng số lượng vé thưởng ở bên phải.
    private void AppendDuplicateLayoutTransition(Transform targetTf)
    {
        _flipSequence.AppendInterval(Mathf.Max(0f, delayBeforeDuplicateTransition));

        float moveDuration = Mathf.Max(0.05f, duplicateTransitionDuration);
        if (targetTf is RectTransform cardRt)
        {
            _flipSequence.Append(cardRt.DOAnchorPos(duplicateCardAnchoredPos, moveDuration).SetEase(Ease.OutCubic));
            _flipSequence.Join(targetTf.DOScale(Vector3.one * duplicateCardScale, moveDuration).SetEase(Ease.OutCubic));
        }
        else
        {
            _flipSequence.Append(targetTf.DOScale(Vector3.one * duplicateCardScale, moveDuration).SetEase(Ease.OutCubic));
        }

        if (duplicateArrowTf != null)
        {
            _flipSequence.AppendCallback(() =>
            {
                duplicateArrowTf.gameObject.SetActive(true);
                duplicateArrowTf.localScale = Vector3.zero;
            });
            _flipSequence.Append(duplicateArrowTf.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack));
        }

        if (duplicateTicketRoot != null)
        {
            _flipSequence.AppendCallback(() =>
            {
                duplicateTicketRoot.SetActive(true);
                duplicateTicketRoot.transform.localScale = Vector3.zero;
            });
            _flipSequence.Append(duplicateTicketRoot.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
        }
    }

    // EN: Stops and clears the active DOTween flip sequence if one is running.
    // VI: Dừng và xóa chuỗi hiệu ứng lật thẻ DOTween nếu đang chạy.
    private void KillFlipSequence()
    {
        if (_flipSequence != null && _flipSequence.IsActive())
        {
            _flipSequence.Kill();
        }
        _flipSequence = null;
        _isFlipping = false;
    }

    // EN: Triggered when the player clicks the Continue button after the card reveal animation completes.
    // VI: Được gọi khi người chơi nhấn nút Tiếp tục sau khi hiệu ứng mở thẻ hoàn tất.
    public void ButtonContinue()
    {
        if (_isFlipping) return;
        ButtonNextOrClose();
    }

    // EN: Completes the reveal/transition animation immediately if still playing, otherwise advances to the next card or closes the popup.
    // VI: Hoàn tất ngay hiệu ứng lật/chuyển đổi thẻ nếu đang chạy, ngược lại chuyển sang thẻ tiếp theo hoặc đóng popup.
    public void ButtonNextOrClose()
    {
        if (_isFlipping && _flipSequence != null && _flipSequence.IsActive())
        {
            _flipSequence.Complete(true);
            _isFlipping = false;
            return;
        }

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
