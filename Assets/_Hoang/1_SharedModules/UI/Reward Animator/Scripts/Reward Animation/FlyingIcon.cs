using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Longvm.RewardAnimator
{
    public class FlyingIcon : RewardAnimation
    {
        [SerializeField] bool playSoundOnComplete = true;
        [SerializeField] Transform destination;
        [SerializeField] float scale = 0.5f;
        [SerializeField] float duration;
        [SerializeField] protected RectTransform iconTransform;
        [SerializeField] protected UnityEvent onFinish;

        public override void SetDestination(Transform destination) => this.destination = destination;

        public override void Init(int amount)
        {
            base.Init(amount);
            amountText.gameObject.SetActive(true);
            iconTransform.localPosition = Vector3.zero;
            iconTransform.localScale = Vector3.one;
        }

        public override void Play()
        {
            amountText.gameObject.SetActive(false);
            iconTransform.DOScale(scale, duration).SetUpdate(true);
            iconTransform.DOMove(destination.position, duration).SetEase(Ease.InCubic).OnComplete(OnComplete).SetUpdate(true);

            void OnComplete()
            {
                if (playSoundOnComplete)
                {
                    //AudioManager.Ins.PlaySound(SoundType.CollectReward);
                    //VibrationManager.Ins.Vibrate(HapticType.Medium);
                }
                onReceiveReward?.Invoke();
                onComplete?.Invoke();
                onFinish?.Invoke();
            }
        }
    }
}
