using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Longvm.UIAnimator
{
    [Serializable]
    public class FadeGraphic : Animation
    {
        [SerializeField] float start;
        [SerializeField] float end;
        [SerializeField] Graphic graphic;

        Tween tween;

        public override void Init()
        {
            if (tween == null)
            {
                graphic.ChangeAlpha(start);
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
                tween = graphic.DOFade(end, duration).SetUpdate(true).SetAutoKill(true);
            }
            else
            {
                tween.Restart();
            }
        }
    } 
}
