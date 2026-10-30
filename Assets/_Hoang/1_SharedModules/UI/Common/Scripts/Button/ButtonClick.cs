using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] bool playAnimation = true;
    [SerializeField] bool playSound = true;
    [SerializeField] bool vibrate = true;
    [SerializeField] RectTransform buttonRt;
    [SerializeField, ExposedScriptableObject] ButtonClickConfig config;

    float _defaultScale;
    Tween clickTween;
    Tween releaseTween;

    private void OnValidate()
    {
        if (buttonRt == null)
        {
            buttonRt = transform as RectTransform;
        }
    }

    private void Start()
    {
        _defaultScale = buttonRt.localScale.x;
    }
    // EN: Plays the button click scale animation, sound effect, and haptic feedback based on configuration.
    // VI: Phát hiệu ứng thu phóng, âm thanh và rung khi nhấn nút dựa trên cấu hình.
    public void PlayEffectClick()
    {
        if (playAnimation)
        {
            if (clickTween == null)
            {
                clickTween = buttonRt.DOScale(config.Scale * buttonRt.localScale, config.Duration).SetEase(config.ClickCurve).SetAutoKill(false);
            }

            clickTween.Restart();
        }

        if (playSound)
        {
            //AudioManager.Ins.PlaySound(SoundType.Click);
        }

        if (vibrate)
        {
            VibrationManager.Ins.Vibrate();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PlayEffectClick();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (playAnimation)
        {
            if (releaseTween == null)
            {
                releaseTween = buttonRt.DOScale(_defaultScale, config.Duration).SetEase(config.ReleaseCurve).SetAutoKill(false);
            }

            releaseTween.Restart();
        }
    }
}
