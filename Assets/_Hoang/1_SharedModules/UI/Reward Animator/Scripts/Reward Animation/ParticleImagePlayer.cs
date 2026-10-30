using AssetKits.ParticleImage;
using DG.Tweening;
using SharedModules.ED;
using UnityEngine;

namespace Longvm.RewardAnimator
{
    public class ParticleImagePlayer : RewardAnimation
    {
        [SerializeField] RectTransform iconTransform;
        [SerializeField] ParticleImage particleImage;

        public override void SetDestination(Transform destination) => particleImage.attractorTarget = destination;

        public override void Init(int amount)
        {
            base.Init(amount);
            iconTransform.localScale = Vector3.one;
        }

        public override void Play()
        {
            iconTransform.DOScale(0f, 0.5f).SetUpdate(true);
            particleImage.Play();
        }

        public void OnFirstParticleFinish()
        {
            onReceiveReward?.Invoke();
            EventDispatcher.DispatchEvent(EventId.ChangeCoinAmount);
        }
        public void OnLastParticleFinish()
        {
            onComplete?.Invoke();
        }
        public void OnEachParticleFinish()
        {
            //AudioManager.Ins.PlaySound(SoundType.CoinClaim);
        }
    }
}
