using Longvm.RewardAnimator;
using UnityEngine;

public class StartingBoosterRewardIcon : FlyingIcon
{
    public override void Init(int amount)
    {
        if (amount > 60)
        {
            formatter.format = "{0}";
            formatter = new TimeFormatter();
        }
        else
        {
            formatter.format = "x{0}";
            formatter = new AmountFormatter();
        }

        base.Init(amount);
        amountText.gameObject.SetActive(true);
        iconTransform.localPosition = Vector3.zero;
        iconTransform.localScale = Vector3.one;
    }
}
