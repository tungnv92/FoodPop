using DG.Tweening;
using UnityEngine;

namespace Longvm.RewardAnimator
{
    public class BackgroundAnimation1 : BackgroundAnimation
    {
        [SerializeField] float duration = 0.5f;
        [SerializeField] CanvasGroup canvasGroup;

        Tween openTween;
        Tween closeTween;

        public override void Init()
        {
            canvasGroup.alpha = 0f;
        }

        public override void Open()
        {
            if (openTween != null) openTween.Restart();
            else openTween = canvasGroup.DOFade(1f, duration).SetAutoKill(false);
        }

        public override void Close()
        {
            if (closeTween != null) closeTween.Restart(); 
            else closeTween = canvasGroup.DOFade(0f, duration).SetAutoKill(false);
        }
    } 
}
