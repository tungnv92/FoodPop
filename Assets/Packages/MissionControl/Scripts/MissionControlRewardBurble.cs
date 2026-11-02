using DG.Tweening;
using GamePackages;
using SharedModules.ED;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MissionControlRewardBurble : MonoBehaviour
{
    [SerializeField] Transform tf;
    [SerializeField] Transform containerTf;
    [SerializeField] MissionControlRewardUI rewardUiPrefab;

    void OnEnable()
    {
        EventDispatcher.RegisterListener(EventId.OnShowMissionControlRewardBurble, OnHide);
    }

    private void OnDisable()
    {
        EventDispatcher.UnregisterListener(EventId.OnShowMissionControlRewardBurble, OnHide);
    }
    MissionControlManager Manager => ProfileManager.instance.GameFeatureContext.MissionControlManager;
    public void OnShow(int chestId)
    {
        MissionControlRewardUI[] children = containerTf.GetComponentsInChildren<MissionControlRewardUI>();
        foreach (var child in children)
        {
            Destroy(child.gameObject);
        }

        List<ResourceGame> rewards = Manager.config.stageInfos[chestId].rewardInfos;
        foreach (ResourceGame reward in rewards)
        {
            var rewardUI = Instantiate(rewardUiPrefab, containerTf);
            rewardUI.Init(reward.GetRewardSprite(), reward.GetNormalValue());
        }

        gameObject.SetActive(true);
        tf.DOScale(1, 0.25f).SetEase(Ease.OutBack);

        Invoke(nameof(OnHide), 1.25f);
    }

    public void OnHide()
    {
        gameObject.SetActive(false);
        tf.localScale = Vector3.zero;
    }

    public void OnHide(object args = null)
    {
        gameObject.SetActive(false);
        tf.localScale = Vector3.zero;
    }
}
