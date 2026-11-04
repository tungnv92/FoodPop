using GamePackages;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CardCollectionCardSlotUI : MonoBehaviour
{
    [Tooltip("Hình ảnh hiển thị mặt trước của thẻ khi đã nhận hoặc mặt sau (BackCard) theo độ hiếm khi chưa nhận.")]
    [FormerlySerializedAs("cardArtImage")]
    [SerializeField] private Image cardImage;

    public int CardId { get; private set; }

    // EN: Updates the card slot sprite to the card's configured sprite when owned, or the rarity's BackCard sprite (1 to 4 stars) when unowned.
    // VI: Cập nhật sprite của ô thẻ bằng sprite của thẻ trong config khi đã nhận, hoặc sprite BackCard tương ứng độ hiếm (1 tới 4 sao) khi chưa nhận.
    public void Setup(CardCollectionCardInfo cardInfo, bool isOwned)
    {
        if (cardInfo == null) return;
        CardId = cardInfo.cardId;

        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }
        if (cardImage == null) return;

        cardImage.enabled = true;
        if (isOwned)
        {
            cardImage.sprite = cardInfo.cardSprite;
        }
        else if (CardCollectionManager.Instance != null && CardCollectionManager.Instance.config != null)
        {
            cardImage.sprite = CardCollectionManager.Instance.config.GetBackCardSprite(cardInfo.rarity);
        }
    }
}
