using DG.Tweening;
using System;
using UnityEngine;

namespace Longvm.UIAnimator
{
    [Serializable]
    public class MoveAnchorPositions : Animation
    {
        [SerializeField] Vector2 start;
        [SerializeField] Vector2 end;
        [SerializeField] Ease ease = Ease.OutQuad;
        [SerializeField] RectTransform[] rts;

        Sequence sequence;

        public override void Init()
        {
            if (sequence == null)
            {
                for (int i = 0; i < rts.Length; i++)
                {
                    rts[i].anchoredPosition = start;
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
                for (int i = 0; i < rts.Length; i++)
                {
                    sequence.Join(rts[i].DOAnchorPos(end, duration).SetEase(ease));
                }
            }
            else
            {
                sequence.Restart();
            }
        }
    } 
}
