using System;
using TMPro;
using UnityEngine;

namespace Longvm.RewardAnimator
{
    public abstract class RewardAnimation : MonoBehaviour
    {
        [SerializeField] protected RectTransform myRt;
        public RectTransform MyRt => myRt;
        [SerializeField] protected TMP_Text amountText;
        [SerializeReference] protected AmountFormatter formatter;

        protected Action onReceiveReward;
        protected Action onComplete;

        int amount;
        private void OnValidate()
        {
            if (myRt == null) myRt = transform as RectTransform;
        }

        public abstract void SetDestination(Transform destination);

        public virtual void Init(int amount)
        {
            this.amount = amount;
            amountText.text = formatter.Format(this.amount);
        }

        public virtual void Add(int amount)
        {
            this.amount += amount;
            amountText.text = formatter.Format(this.amount);
        }
        public void OnReceiveReward(Action onReceiveReward) => this.onReceiveReward = onReceiveReward;

        public void OnComplete(Action onComplete)
        {
            this.onComplete = onComplete;
        }

        public virtual void Play() { }
    }
}
