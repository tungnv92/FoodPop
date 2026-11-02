using System.Collections.Generic;

namespace MonetToolkit
{
    public class ProductId
    {
        const string Prefix = "com.pixelblast.";

        public const string RemoveAds       = Prefix + "removeads";
        public const string RemoveAdsPack   = Prefix + "removeadspack";

        public const string MiniCoin        = Prefix + "1kgold";
        public const string SmallCoin       = Prefix + "5kgold";
        public const string MediumCoin      = Prefix + "10kgold";
        public const string BigCoin         = Prefix + "25kgold";
        public const string SuperCoin       = Prefix + "50kgold";
        public const string MegaCoin        = Prefix + "100kgold";

        public const string BeginnerPack    = Prefix + "beginnerpack";
        //public const string WelcomePackSale = Prefix + "welcomepack3";
        public const string MasterPass      = Prefix + "masterpass";

        //public const string TraineeBundle   = Prefix + "minipack";
        public const string SmallPack       = Prefix + "smallpack";
        public const string MediumPack      = Prefix + "mediumpack";
        public const string BigPack         = Prefix + "bigpack";
        //public const string MasterBundle    = Prefix + "superpack";
        //public const string LegendBundle    = Prefix + "megapack";

        public const string RevivePack = Prefix + "revivepack";
        public const string BoosterPack = Prefix + "boosterpack";

        //public const string JourneyOffer = Prefix + "jouneyoffer";
        //public const string JourneyOfferSale = Prefix + "jouneyoffer2";

        //public const string Worldcup_1 = Prefix + "26football1";
        //public const string Worldcup_2 = Prefix + "26football2";
        //public const string Worldcup_3 = Prefix + "26football3";

        //public const string RemoveAds_5 = Prefix + "removeads5";
        //public const string RemoveAds_3 = Prefix + "removeads3";

        public static string[] Ids =
        {
            RemoveAds,
            RemoveAdsPack,

            MiniCoin,
            SmallCoin,
            MediumCoin,
            BigCoin,
            SuperCoin,
            MegaCoin,

            BeginnerPack,
            //WelcomePackSale,
            MasterPass,

            SmallPack,
            MediumPack,
            BigPack,

            RevivePack,
            BoosterPack,

            //JourneyOffer,
            //JourneyOfferSale,

            //Worldcup_1,
            //Worldcup_2, 
            //Worldcup_3,

            //RemoveAds_5,
            //RemoveAds_3,
        };

        public static Dictionary<string, string> NameById = new()
        {
            { RemoveAds,        "Remove Ads" },
            { RemoveAdsPack,    "Remove Ads Pack" },

            { MiniCoin,         "1K Gold" },
            { SmallCoin,        "5K Gold" },
            { MediumCoin,       "10K Gold" },
            { BigCoin,          "25K Gold" },
            { SuperCoin,        "50K Gold" },
            { MegaCoin,         "100K Gold" },

            { BeginnerPack,      "Beginner Pack" },
            //{ WelcomePackSale,  "Welcome Pack Sale" },
            { MasterPass,       "Master Pass" },

            //{ TraineeBundle,    "Mini Pack " },
            { SmallPack,    "Small Pack" },
            { MediumPack,        "Medium Pack" },
            { BigPack,   "Big Pack" },
            //{ MasterBundle,     "Super Pack" },
            //{ LegendBundle,     "Mega Pack" },

            { RevivePack,        "Revive Pack"},  
            { BoosterPack,       "Booster Pack"},
            
            //{ JourneyOffer,      "Journey Offer"},  
            //{ JourneyOfferSale,  "Journey Offer 2"},
            
            //{ Worldcup_1,        "Worldcup 2026 - 1"},   
            //{ Worldcup_2,        "Worldcup 2026 - 2"},   
            //{ Worldcup_3,        "Worldcup 2026 - 3"},     
            
            //{ RemoveAds_5,        "Remmove Ads Sale 1"},              
            //{ RemoveAds_3,        "Remmove Ads Sale 2"},              
        };
    }
}
