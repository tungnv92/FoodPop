namespace SharedModules.ED
{
    public enum EventId
    {
        NewDay,
        RemoveAds,
        PurchaseStarterPack,
        ChangeCoinAmount,
        ChangeBooster1Amount,
        ChangeBooster2Amount,
        ChangeBooster3Amount,
        ChangeBooster4Amount,
        Change_StartingBooster_Amount,
        ChangeLanguage,

        AddPointAchievement,
        CompleteQuestAchievement,
        DisableAchievement,
        PopupAchieveHide,
        ShowRewardChest,

        ActivateMasterPass,
        PopupMasterPassClosed,
        PopupDailyClosed,

        PurchaseRemoveAds,
        PurchaseRevivePack,

        Ingame_Update_Boundary_Cells,
        OnClaimDailyReward,

        OnHouseDisappear,
        OnHousePick,
        OnStartRotate,
        OnFinishRotate,

        PurchaseJourneyOffer,
        PopupJourneyOfferClosed,

        OnShowMissionControlRewardBurble,
    }
}
