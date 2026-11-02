using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Longvm.UIAnimator
{
    [Serializable]
    public class FadeGraphics : Animation
    {
        [SerializeField] float start;
        [SerializeField] float end;
        [SerializeField] Graphic[] graphics;

        Sequence sequence;

        public override void Init()
        {
            if (sequence == null)
            {
                for (int i = 0; i < graphics.Length; i++)
                {
                    graphics[i].ChangeAlpha(start);
                }
            }
            else
            {
                sequence.Goto(0);
            }
        }

        public override void Play()
        {
            if (sequence == null)
            {
                sequence = DOTween.Sequence().SetAutoKill(false);
                for (int i = 0; i < graphics.Length; i++)
                {
                    sequence.Join(graphics[i].DOFade(end, duration));
                }
            }
            else
            {
                sequence.Restart();
            }
        }
    } 
}
