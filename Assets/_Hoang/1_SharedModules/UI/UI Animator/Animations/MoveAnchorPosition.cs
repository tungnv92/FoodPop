using DG.Tweening;
using System;
using UnityEngine;

namespace Longvm.UIAnimator
{
    [Serializable]
    public class MoveAnchorPosition : Animation
    {
        [SerializeField] Vector2 start;
        [SerializeField] Vector2 end;
        [SerializeField] Ease ease = Ease.OutQuad;
        [SerializeField] RectTransform rt;

        Tween tween;

        public override void Init()
        {
            if (tween == null)
            {
                rt.anchoredPosition = start;
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
                tween = rt.DOAnchorPos(end, duration).SetEase(ease).SetUpdate(true).SetAutoKill(false);
            }
            else
            {
                tween.Restart();
            }
        }
    }
}
