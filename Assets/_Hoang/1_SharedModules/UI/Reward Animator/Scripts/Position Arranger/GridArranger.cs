using System.Collections.Generic;
using DG.Tweening;
using Longvm.RewardAnimator;
using UnityEngine;

namespace Longvm.RewardAnimator
{
    public class GridArranger : RewardArranger
    {
        [SerializeField] Vector2 centerAnchor = new(0, 300);
        [SerializeField] int columnAmount = 3;
        [SerializeField] Vector2 space = new Vector2(300, 300);
        [SerializeField] bool showTogether = true;
        [SerializeField] float duration = 0.25f;

        public override void Init(List<RectTransform> rts)
        {
            base.Init(rts);
            foreach (RectTransform t in rts)
            {
                t.anchoredPosition = centerAnchor;
                t.localScale = Vector3.zero;
                t.gameObject.SetActive(true);
            }
        }
        public override void Play()
        {
            // Pre-calculate position
            int count = rts.Count;
            Vector2[] targetAnchorPos = new Vector2[count];

            int rowAmount = Mathf.CeilToInt(rts.Count * 1f/ columnAmount);
            int amountInLastRow = count - (rowAmount - 1) * columnAmount;
            for (int rowId = 0; rowId < rowAmount; rowId++)
            {
                float y_axis_space_unit = (rowAmount - 1) / 2f;
                int amountInRow;
                if (rowId < rowAmount - 1 || amountInLastRow == columnAmount)  // Not the last row
                {
                    amountInRow = columnAmount;
                }
                else    // The last row (with amount < columnAmount)
                {
                    amountInRow = amountInLastRow;
                }

                float x_axis_space_unit = (amountInRow - 1) / 2f;
                for (int colId = 0; colId < amountInRow; colId++)
                {
                    int rectTf_Id = rowId * columnAmount + colId;
                    float anchorPosX = centerAnchor.x + (colId - x_axis_space_unit) * space.x;
                    float anchorPosY = centerAnchor.y + (y_axis_space_unit - rowId) * space.y;
                    targetAnchorPos[rectTf_Id] = new Vector2(anchorPosX, anchorPosY);
                    if (showTogether) rts[rectTf_Id].anchoredPosition = targetAnchorPos[rectTf_Id];
                }
            }

            // Play Show
            for (int i = 0; i < rts.Count; i++)
            {
                if (i < rts.Count - 1) rts[i].DOScale(1f, duration).SetEase(Ease.OutBack);
                else rts[i].DOScale(1f, duration).SetEase(Ease.OutBack).OnComplete(() => isComplete = true);
            }
        }
    }
}

