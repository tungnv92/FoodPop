using DG.Tweening;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItem : MonoBehaviour
{
    public ResourceGame resourceGame;
    public Image img;
    public TextMeshProUGUI amountText;
    public CanvasGroup canvasGroup;
    public float duration = 1f;
    public RectTransform rectTransform;

    public void OnInitt(ResourceGame resource, Sprite sprite = null, bool isFlyUp = false, bool isGlow = false)
    {
        this.resourceGame = resource;

        if (this.rectTransform == null) this.rectTransform = GetComponent<RectTransform>();
        if (this.canvasGroup == null) this.canvasGroup = GetComponent<CanvasGroup>();
        if (this.img == null) this.img = GetComponentInChildren<Image>();
        if (this.amountText == null) this.amountText = GetComponentInChildren<TextMeshProUGUI>();

        if (sprite == null && WinStreakManager.Instance != null && WinStreakManager.Instance.WinStreakGlobalConfig != null)
        {
            sprite = WinStreakManager.Instance.WinStreakGlobalConfig.GetResourceSprite(resource.type);
        }

        if (this.img != null)
        {
            if (sprite != null)
            {
                this.img.sprite = sprite;
                this.img.enabled = true;
                //this.img.ResizeImgKeepHeight();
            }
            else
            {
                this.img.enabled = false;
            }
        }

        if (this.amountText != null)
        {
            this.amountText.text = resource != null ? resource.GetNormalValue() : "";
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (isFlyUp)
        {
            this.transform.localScale = Vector3.one * 0.7f;
            AnimFlyUp();
        }

        transform.localRotation = Quaternion.identity;
        duration = 1f;
    }

    private void OnDisable()
    {
        transform.localRotation = Quaternion.identity;
    }

    public void AnimFlyUp()
    {
        DOVirtual.DelayedCall(Time.deltaTime, () =>
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.DOFade(0f, duration * 0.25f).SetDelay(duration * 0.85f).SetEase(Ease.OutSine);
            }
            if (rectTransform != null)
            {
                rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + 50f, duration).SetEase(Ease.OutSine);
            }
            DOVirtual.DelayedCall(duration + 0.3f, () =>
            {
                Destroy(this.gameObject);
            });
        });
    }

    public void Fly(Vector2 dir, float distance, float delay)
    {
        if (this.rectTransform != null)
        {
            this.rectTransform.DOAnchorPos(this.rectTransform.anchoredPosition + dir.normalized * distance, 0.3f).SetEase(Ease.OutSine).SetDelay(delay);
        }
        this.transform.DOScale(0.8f, 0.3f).SetEase(Ease.OutSine).SetDelay(delay);
    }
}
