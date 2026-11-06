using System.Collections.Generic;
using AssetKits.ParticleImage;
using DG.Tweening;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayMode = AssetKits.ParticleImage.Enumerations.PlayMode;

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

    [Tooltip("Khối hiển thị tổng số dư Vé (Ticket Amount Holder), mặc định ẩn và chỉ bật lên khi bắt đầu phát hiệu ứng hút vé.")]
    [SerializeField] private GameObject ticketHolderObject;

    [Tooltip("Nhãn hiển thị tổng số lượng Vé hiện có của người chơi trên thanh Ticket Amount Holder.")]
    [SerializeField] private TMP_Text ticketHolderAmountText;

    [Tooltip("Transform biểu tượng Vé trên thanh Ticket Amount Holder để nhún nhảy khi hạt vé bay tới.")]
    [SerializeField] private Transform ticketHolderIconTf;

    [Tooltip("Hiệu ứng ParticleImage bắn các vé bay hút về thanh Ticket Amount Holder khi thẻ bị trùng.")]
    [SerializeField] private ParticleImage ticketParticleImage;

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

    [Tooltip("Thời gian chạy hiệu ứng tăng số lượng vé trên thanh Ticket Amount Holder (giây).")]
    [SerializeField] private float ticketCountTweenDuration = 0.35f;

    private readonly List<CardDropResult> _pendingDrops = new();
    private int _currentIndex;
    private bool _isFlipping;
    private Sequence _flipSequence;
    private Tween _ticketCountTween;
    private int _displayedTicketAmount;
    private bool _hasHandledFirstParticle;
    private bool _hasHandledLastParticle;
    private bool _cachedInitialPos;
    private Vector2 _initialCardAnchoredPos;

    private CardCollectionManager Manager => CardCollectionManager.Instance;

    // EN: Binds the continue button and ticket particle callbacks on awake.
    // VI: Gắn sự kiện cho nút Tiếp tục và các callback của hiệu ứng hạt vé khi khởi tạo.
    private void Awake()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ButtonContinue);
            continueButton.onClick.AddListener(ButtonContinue);
        }

        ConfigureTicketParticleImage();
    }

    // EN: Kills any active flip or ticket count animations when the popup is disabled.
    // VI: Hủy chuỗi hiệu ứng lật thẻ hoặc đếm vé đang chạy khi popup bị tắt.
    private void OnDisable()
    {
        KillFlipSequence();
        KillTicketEffects();
    }

    // EN: Configures the ticket ParticleImage so it plays only on demand after the duplicate conversion animation and binds its particle finish callbacks.
    // VI: Cấu hình ParticleImage vé để chỉ phát khi được gọi sau hiệu ứng quy đổi thẻ trùng và gắn các hàm callback khi hạt bay tới đích.
    private void ConfigureTicketParticleImage()
    {
        if (ticketParticleImage == null && duplicateTicketRoot != null)
        {
            ticketParticleImage = duplicateTicketRoot.GetComponentInChildren<ParticleImage>(true);
        }

        if (ticketParticleImage == null) return;

        ticketParticleImage.PlayMode = PlayMode.None;
        ticketParticleImage.onFirstParticleFinish.RemoveListener(FirstParticleFinished);
        ticketParticleImage.onFirstParticleFinish.AddListener(FirstParticleFinished);
        ticketParticleImage.onLastParticleFinish.RemoveListener(LastParticleFinished);
        ticketParticleImage.onLastParticleFinish.AddListener(LastParticleFinished);
    }

    // EN: Opens the Card Reward reveal popup for a single CardDropResult or a List<CardDropResult>.
    // VI: Mở popup nhận thẻ thưởng cho một thẻ đơn lẻ (CardDropResult) hoặc danh sách nhiều thẻ (List<CardDropResult>).
    public override void Open(object args = null)
    {
        base.Open(args);
        CacheInitialCardPositionIfNeeded();
        ConfigureTicketParticleImage();
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

        InitializeDisplayedTicketAmount();
        ShowCurrentCard();
    }

    // EN: Calculates and displays the player's ticket balance prior to converting the pending duplicate cards in this reward session.
    // VI: Tính toán và hiển thị số dư vé của người chơi trước khi cộng số vé quy đổi từ các thẻ trùng trong lượt mở thưởng này.
    private void InitializeDisplayedTicketAmount()
    {
        int currentTotal = Manager != null && Manager.Data != null ? Manager.Data.ticketAmount : 0;
        int pendingConvertedTotal = 0;
        for (int i = 0; i < _pendingDrops.Count; i++)
        {
            if (_pendingDrops[i].isDuplicate)
            {
                pendingConvertedTotal += Mathf.Max(0, _pendingDrops[i].ticketsConverted);
            }
        }

        _displayedTicketAmount = Mathf.Max(0, currentTotal - pendingConvertedTotal);
        SetTicketHolderAmountText(_displayedTicketAmount);
    }

    // EN: Updates the ticket holder text to the specified amount.
    // VI: Cập nhật nhãn số lượng vé trên thanh Ticket Amount Holder theo giá trị truyền vào.
    private void SetTicketHolderAmountText(int amount)
    {
        if (ticketHolderAmountText != null)
        {
            ticketHolderAmountText.text = amount.ToString();
        }
    }

    // EN: Resolves and toggles the active state of the Ticket Amount Holder GameObject.
    // VI: Xác định và bật/tắt trạng thái hiển thị của khối Ticket Amount Holder.
    private void SetTicketHolderActive(bool isActive)
    {
        GameObject holderObj = GetTicketHolderObject();
        if (holderObj != null)
        {
            holderObj.SetActive(isActive);
        }
    }

    // EN: Returns the Ticket Amount Holder GameObject (using ticketHolderObject or resolving from the parent of ticketHolderAmountText / ticketHolderIconTf).
    // VI: Trả về GameObject của Ticket Amount Holder (dùng ticketHolderObject hoặc suy ra từ đối tượng cha của ticketHolderAmountText / ticketHolderIconTf).
    private GameObject GetTicketHolderObject()
    {
        if (ticketHolderObject != null) return ticketHolderObject;
        if (ticketHolderIconTf != null && ticketHolderIconTf.parent != null) return ticketHolderIconTf.parent.gameObject;
        if (ticketHolderAmountText != null && ticketHolderAmountText.transform.parent != null) return ticketHolderAmountText.transform.parent.gameObject;
        return null;
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
        KillTicketEffects();
        _hasHandledFirstParticle = false;
        _hasHandledLastParticle = false;

        if (_currentIndex < 0 || _currentIndex >= _pendingDrops.Count)
        {
            Close();
            return;
        }

        CardDropResult drop = _pendingDrops[_currentIndex];
        CardCollectionCardInfo cardInfo = Manager != null && Manager.config != null
            ? Manager.config.GetCardInfo(drop.cardId)
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

        SetTicketHolderActive(false);
        SetTicketHolderAmountText(_displayedTicketAmount);
        PlayCardFlipAndDuplicateTransition(drop, frontSprite);
    }

    // EN: Runs the card pop-in and horizontal flip animation, followed by the duplicate layout transition and ticket particle attraction if the card is a duplicate.
    // VI: Chạy hiệu ứng xuất hiện và lật thẻ ngang, tiếp nối bằng hiệu ứng chuyển sang bố cục quy đổi vé và bắn hạt vé bay về đích nếu là thẻ trùng.
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
            if (drop.isDuplicate)
            {
                PlayTicketAttractionEffect();
            }
            else
            {
                _isFlipping = false;
                ShowContinueButton();
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
                if (ticketParticleImage != null)
                {
                    ticketParticleImage.PlayMode = PlayMode.None;
                    ticketParticleImage.Stop(true);
                }
            });
            _flipSequence.Append(duplicateTicketRoot.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
        }
    }

    // EN: Activates the ticket amount holder and automatically plays the ticket attraction ParticleImage effect after the duplicate conversion layout finishes appearing.
    // VI: Bật thanh số dư vé và tự động phát hiệu ứng ParticleImage hút vé sau khi hoạt ảnh quy đổi thẻ trùng hiển thị xong.
    private void PlayTicketAttractionEffect()
    {
        SetTicketHolderActive(true);
        SetTicketHolderAmountText(_displayedTicketAmount);

        if (ticketParticleImage != null)
        {
            ticketParticleImage.Stop(true);
            ticketParticleImage.Play();
        }
        else
        {
            FirstParticleFinished();
            LastParticleFinished();
        }
    }

    // EN: Callback invoked when the first ticket particle reaches the ticket amount holder, tweening the displayed ticket amount upward.
    // VI: Hàm callback được gọi khi hạt vé đầu tiên bay tới thanh số dư vé, chạy hiệu ứng tăng dần con số hiển thị.
    public void FirstParticleFinished()
    {
        if (_hasHandledFirstParticle) return;
        _hasHandledFirstParticle = true;

        int addedTickets = (_currentIndex >= 0 && _currentIndex < _pendingDrops.Count)
            ? Mathf.Max(0, _pendingDrops[_currentIndex].ticketsConverted)
            : 0;
        int fromAmount = _displayedTicketAmount;
        int toAmount = fromAmount + addedTickets;
        _displayedTicketAmount = toAmount;

        if (ticketHolderAmountText != null)
        {
            _ticketCountTween?.Kill();
            _ticketCountTween = DOVirtual.Int(fromAmount, toAmount, Mathf.Max(0.05f, ticketCountTweenDuration), val =>
            {
                SetTicketHolderAmountText(val);
            }).SetEase(Ease.Linear);
        }

        if (ticketHolderIconTf != null)
        {
            ticketHolderIconTf.DOKill();
            ticketHolderIconTf.localScale = Vector3.one;
            ticketHolderIconTf.DOPunchScale(Vector3.one * 0.2f, Mathf.Max(0.15f, ticketCountTweenDuration), 6, 0.5f);
        }
    }

    // EN: Callback invoked when the last ticket particle reaches the ticket amount holder, completing the reveal state and showing the Continue button.
    // VI: Hàm callback được gọi khi hạt vé cuối cùng bay tới thanh số dư vé, hoàn tất trạng thái mở thẻ và hiển thị nút Tiếp tục.
    public void LastParticleFinished()
    {
        if (_hasHandledLastParticle) return;
        _hasHandledLastParticle = true;

        if (_ticketCountTween != null && _ticketCountTween.IsActive())
        {
            _ticketCountTween.Complete(true);
        }
        SetTicketHolderAmountText(_displayedTicketAmount);

        if (ticketHolderIconTf != null)
        {
            ticketHolderIconTf.DOKill();
            ticketHolderIconTf.localScale = Vector3.one;
        }

        _isFlipping = false;
        ShowContinueButton();
    }

    // EN: Activates and plays a pop-in scale animation on the Continue button.
    // VI: Bật hoạt động và phát hiệu ứng phóng to xuất hiện cho nút Tiếp tục.
    private void ShowContinueButton()
    {
        if (continueButton == null) return;
        continueButton.gameObject.SetActive(true);
        //continueButton.transform.DOKill();
        //continueButton.transform.localScale = Vector3.zero;
        //continueButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
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

    // EN: Stops the ticket count tween and clears any active ticket particles.
    // VI: Dừng hiệu ứng đếm số vé và xóa các hạt vé đang bay nếu có.
    private void KillTicketEffects()
    {
        if (_ticketCountTween != null && _ticketCountTween.IsActive())
        {
            _ticketCountTween.Kill();
        }
        _ticketCountTween = null;

        if (ticketParticleImage != null)
        {
            ticketParticleImage.Stop(true);
        }

        if (ticketHolderIconTf != null)
        {
            ticketHolderIconTf.DOKill();
            ticketHolderIconTf.localScale = Vector3.one;
        }
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
        if (_isFlipping)
        {
            if (_flipSequence != null && _flipSequence.IsActive())
            {
                _flipSequence.Complete(true);
            }

            if (_currentIndex >= 0 && _currentIndex < _pendingDrops.Count && _pendingDrops[_currentIndex].isDuplicate)
            {
                SetTicketHolderActive(true);
                if (ticketParticleImage != null)
                {
                    ticketParticleImage.Stop(true);
                }
                FirstParticleFinished();
                LastParticleFinished();
            }
            else
            {
                _isFlipping = false;
                ShowContinueButton();
            }
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
