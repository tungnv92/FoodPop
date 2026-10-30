using System;
using System.Collections.Generic;
using UnityEngine;

namespace Longvm.RewardAnimator
{
    [Serializable]
    public abstract class RewardArranger
    {
        protected bool isComplete;
        protected List<RectTransform> rts;

        public WaitUntil WaitUntilComplete { get; private set; }

        public virtual void Init(List<RectTransform> rts)
        {
            this.rts = rts;
            isComplete = false;
            if (WaitUntilComplete == null) WaitUntilComplete = new WaitUntil(() => isComplete);
        }

        public abstract void Play();
    }
}
