using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
namespace GamePackages
{
    public class UIProcessDailyReward : MonoBehaviour
    {
        public Image imgProcess;
        public List<GameObject> checkers;
        DailyRewardManager DailyRewardManager => ProfileManager.instance.GameFeatureContext.DailyRewardManager;
        private void OnEnable()
        {
            LoadProcess();
            DailyRewardManager.onClaimedAction += UpdateProcess;
        }
        private void OnDisable()
        {
            DailyRewardManager.onClaimedAction -= UpdateProcess;
        }
        void LoadProcess()
        {
            float process = DailyRewardManager.GetProcessAdsClaim();
            imgProcess.fillAmount = process;
            int totalClaimed = DailyRewardManager.GetTotalAdsClaimed();
            for (int i = 0; i < checkers.Count; i++)
            {
                checkers[i].gameObject.SetActive(i < totalClaimed);
            }
        }
        void UpdateProcess()
        {
            float process = DailyRewardManager.GetProcessAdsClaim();
            imgProcess.DOFillAmount(process, 1).SetSpeedBased(true).OnComplete(() =>
            {
                int totalClaimed = DailyRewardManager.GetTotalAdsClaimed();
                for (int i = 0; i < checkers.Count; i++)
                {
                    checkers[i].gameObject.SetActive(i < totalClaimed);
                }
            });

        }
    }
}