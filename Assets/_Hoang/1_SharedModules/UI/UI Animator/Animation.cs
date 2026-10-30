using System;
using UnityEngine;

namespace Longvm.UIAnimator
{
    [Serializable]
    public abstract class Animation
    {
        [SerializeField] protected float duration = 0.5f;
        public float Duration => duration;
        public virtual void Init() { }
        public virtual void Play() { }
    }
}
