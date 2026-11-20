using DG.Tweening;
using GamePackages;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GiftBubble : MonoBehaviour
{
    public Image giftImg;
    public RectTransform bubble;
    public RectTransform bubbleIndicator;
    public HorizontalLayoutGroup horizontalLayoutGroup;
    public List<ResourceGame> rewards = new();
    public List<RewardItem> rewardItems = new();

    public void OnInit(List<ResourceGame> rewardList)
    {
        this.rewards = rewardList != null ? new List<ResourceGame>(rewardList) : new List<ResourceGame>();
        if (rewards.Count > 2)
        {
            //SetBubbleAlign(align);
        }
        else
        {
            //SetBubbleAlign(0);
        }
        ShowBubble(false);
    }


    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ShowBubble(false);
        }
    }

    [SerializeField] private RewardItem rewardItemPrefab;

    public void InitRewardItems(List<ResourceGame> list)
    {
        ReturnRewardItemsToPool();
        if (list == null) return;

        if (rewardItemPrefab == null)
        {
            rewardItemPrefab = Resources.Load<RewardItem>("RewardItem");
        }

        foreach (var reward in list)
        {
            RewardItem rewardItem = rewardItemPrefab != null 
                ? Instantiate(rewardItemPrefab, horizontalLayoutGroup.transform) 
                : new GameObject("RewardItem").AddComponent<RewardItem>();
            rewardItem.OnInitt(reward, null, false, false);
            //rewardItem.transform.localScale = Vector3.one * 0.33f;
            //if (rewardItem.rectTransform != null)
            //{
            //    rewardItem.rectTransform.sizeDelta = new Vector2(467, 418);
            //}
            rewardItems.Add(rewardItem);
        }
    }

    public void ReturnRewardItemsToPool()
    {
        foreach (var rewardItem in rewardItems)
        {
            if (rewardItem != null)
            {
                Destroy(rewardItem.gameObject);
            }
        }
        rewardItems.Clear();
    }

    public void OnClick()
    {
        DOVirtual.DelayedCall(Time.deltaTime, () =>
        {
            ShowBubble(true);
        });
    }

    public void ShowBubble(bool isShow)
    {
        if (bubble != null) bubble.gameObject.SetActive(isShow);
        if (bubbleIndicator != null) bubbleIndicator.gameObject.SetActive(isShow);
        if (isShow)
        {
            InitRewardItems(rewards);
            SetBubbleWidth(rewards.Count);
        }
        else
        {
            ReturnRewardItemsToPool();
        }
    }

    public void SetBubbleWidth(int rewardAmount)
    {
        if (bubble == null) return;
        float bubbleWidth = bubble.sizeDelta.x;
        if (rewardAmount == 1) bubbleWidth = 180;
        else if (rewardAmount == 2) bubbleWidth = 260;
        else if (rewardAmount == 3) bubbleWidth = 384;
        else if (rewardAmount == 4) bubbleWidth = 503;
        else if (rewardAmount >= 5) bubbleWidth = 597;

        bubble.sizeDelta = new Vector2(bubbleWidth, bubble.sizeDelta.y);
    }

    public void SetBubbleAlign(int align)
    {
        if (bubble == null) return;
        if (align == 1)
        {
            bubble.anchorMin = new Vector2(1, 0.5f);
            bubble.anchorMax = new Vector2(1, 0.5f);
            bubble.pivot = new Vector2(1, 0.5f);
            bubble.anchoredPosition = new Vector2(40, bubble.anchoredPosition.y);
        }
        else if (align == -1)
        {
            bubble.anchorMin = new Vector2(0, 0.5f);
            bubble.anchorMax = new Vector2(0, 0.5f);
            bubble.pivot = new Vector2(0, 0.5f);
            bubble.anchoredPosition = new Vector2(-40, bubble.anchoredPosition.y);
        }
        else
        {
            bubble.anchorMin = new Vector2(0.5f, 0.5f);
            bubble.anchorMax = new Vector2(0.5f, 0.5f);
            bubble.pivot = new Vector2(0.5f, 0.5f);
            bubble.anchoredPosition = new Vector2(0, bubble.anchoredPosition.y);
        }
    }
}
