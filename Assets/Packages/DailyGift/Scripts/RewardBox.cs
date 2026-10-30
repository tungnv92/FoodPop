using DG.Tweening;
using UnityEngine;

namespace HomeFeatures.DailyLogin
{
    public class RewardBox : MonoBehaviour
    {
        [SerializeField] Transform rewardInfo;
        Tween showRewardTween;
        public void ShowReward()
        {
            if (showRewardTween != null) showRewardTween.Kill();

            rewardInfo.localScale = Vector3.zero;
            showRewardTween = rewardInfo.DOScale(1, 0.25f).SetEase(Ease.OutBack);
            HideReward();
        }

        public void HideReward()
        {
            rewardInfo.DOScale(0, 0.25f).SetDelay(2).OnComplete(() => showRewardTween = null);
        }
    }
}

