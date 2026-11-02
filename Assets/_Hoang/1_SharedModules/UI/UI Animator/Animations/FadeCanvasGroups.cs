using DG.Tweening;
using System;
using UnityEngine;

namespace Longvm.UIAnimator
{
    [Serializable]
    public class FadeCanvasGroups : Animation
    {
        [SerializeField] float start;
        [SerializeField] float end;
        [SerializeField] CanvasGroup[] groups;

        Sequence sequence;

        public override void Init()
        {
            if (sequence == null)
            {
                for (int i = 0; i < groups.Length; i++)
                {
                    groups[i].alpha = start;
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
                for (int i = 0; i < groups.Length; i++)
                {
                    sequence.Join(groups[i].DOFade(end, duration).SetUpdate(true));
                }
            }
            else
            {
                sequence.Restart();
            }
        }
    } 
}
