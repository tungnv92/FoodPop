using DG.Tweening;
using System;
using UnityEngine;

namespace Longvm.UIAnimator
{
    [Serializable]
    public class FadeCanvasGroup : Animation
    {
        [SerializeField] float start;
        [SerializeField] float end;
        [SerializeField] CanvasGroup group;

        Tween tween;

        public override void Init()
        {
            if (tween == null)
            {
                group.alpha = start;
            }
            else
            {
                tween.Goto(0);
            }
        }

        public override void Play()
        {
            if (tween == null)
            {
                tween = group.DOFade(end, duration).SetAutoKill(false).SetUpdate(true);
            }
            else
            {
                tween.Restart();
            }
        }
    }
}
