using System;

namespace MonetToolkit
{
    [Serializable]
    public struct RewardConfig
    {
        public RewardType Type;
        public int Amount;

        public string GetAmountString()
        {
            switch (Type)
            {
                case RewardType.UnlimitedLife:
                    return TimeSpanExt.SecondsToString_HMS(Amount);
                case RewardType.Coin:
                    return Amount.ToString();
                default:
                    return $"x{Amount}";
            }
        }
    }
}