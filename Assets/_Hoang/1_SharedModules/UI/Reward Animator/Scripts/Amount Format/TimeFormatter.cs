using System;

namespace Longvm.RewardAnimator
{
    [Serializable]
    public class TimeFormatter : AmountFormatter
    {
        public override string Format(int minute)
        {
            int seconds = minute * 60;
            format = "{0}";
            return string.Format(format, seconds.SecondsToString_HMS());
        }
    }
}
