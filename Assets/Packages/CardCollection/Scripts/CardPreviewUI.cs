using DG.Tweening;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardPreviewUI : MonoBehaviour
{
    [SerializeField] CanvasGroup _canvasGroup;
    [SerializeField] RectTransform _cardTf;
    [SerializeField] Image _cardImage;
    [SerializeField] TMP_Text _cardName;
    [SerializeField] GameObject _closeButton;

    public void ShowPreview(CardCollectionCardInfo cardInfo)
    {
        _canvasGroup.alpha = 0;
        _cardTf.localScale = Vector3.zero;
        gameObject.SetActive(true);
        _closeButton.SetActive(false);

        _cardImage.sprite = cardInfo.cardSprite;
        _cardName.text = cardInfo.cardName;
        _cardTf.DOScale(1, 0.25f).SetEase(Ease.OutBack).OnComplete(() => _closeButton.SetActive(true));
        _canvasGroup.DOFade(1, 0.35f);
    }

    public void OnClickClose()
    {
        gameObject.SetActive(false);
    }
}
