using DG.Tweening;
using UnityEngine;

public class AnimOpenUI : MonoBehaviour
{
    public Tween scaleTween;

    private void OnEnable()
    {
        float smallScale = 0.7f;
        float duration = 0.25f;

        scaleTween?.Kill();
        transform.localScale = smallScale * Vector3.one;
        scaleTween = transform.DOScale(1f, duration).SetEase(Ease.OutBack);
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
    }
}
