using DG.Tweening;
using System;
using UnityEngine;

namespace Longvm.UIAnimator
{
    [Serializable]
    public class ScaleTransforms : Animation
    {
        [SerializeField] Vector3 start;
        [SerializeField] Vector3 end;
        [SerializeField] Ease ease = Ease.OutBack;
        [SerializeField] Transform[] transforms;

        Sequence sequence;

        public override void Init()
        {
            if (sequence == null)
            {
                for (int i = 0; i < transforms.Length; i++)
                {
                    transforms[i].localScale = start;
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
                for (int i = 0; i < transforms.Length; i++)
                {
                    sequence.Join(transforms[i].DOScale(end, duration).SetEase(ease));
                }
            }
            else
            {
                sequence.Restart();
            }
        }
    } 
}
