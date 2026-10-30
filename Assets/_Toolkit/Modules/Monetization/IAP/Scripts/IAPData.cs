using System;

namespace MonetToolkit
{
    [Serializable]
    public class IAPData
    {
        public bool FirstPurchase = true;
        public bool IsSuggestedRemoveAds = false;
        public bool IsFirstInterShownToday = false;
        public SerializableDateTime StarterPackFirstOffer;
        public bool IsFirstOfferStarterPack = true;
        //public bool StarterPackSuggested = false;
        public bool StarterPackPurchased;
        public bool RemoveAdsPurchased;
        public int RevivePackTriggerCountdown;
    }

    [Serializable]
    public class IAPConfig
    {
        public int StarterPackOfferLevel = 19;
        public int RevivePackEnableLevel = 11;
        public int RemoveAdsPackSuggestInterCount;
    }
}
