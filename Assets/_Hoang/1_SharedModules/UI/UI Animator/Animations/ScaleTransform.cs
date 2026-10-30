using DG.Tweening;
using System;
using UnityEngine;

namespace Longvm.UIAnimator
{
    [Serializable]
    public class ScaleTransform : Animation
    {
        [SerializeField] Vector3 start;
        [SerializeField] Vector3 end;
        [SerializeField] Ease ease = Ease.OutBack;
        [SerializeField] Transform transform;

        Tween tween;

        public override void Init()
        {
            if (tween == null)
            {
                transform.localScale = start;
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
                tween = transform.DOScale(end, duration).SetUpdate(true).SetEase(ease).SetAutoKill(false);
            }
            else
            {
                tween.Restart();
            }
        }
    } 
}
