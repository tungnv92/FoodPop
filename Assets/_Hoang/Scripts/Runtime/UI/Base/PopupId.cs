using UnityEngine;

/// <summary>
/// Identifies popup types for loading via <c>Resources.Load</c>.
/// Popup prefabs must be named "Popup" + enum name (e.g. PopupSettings.prefab)
/// and placed in <c>Assets/_Game/Prefabs/UI/Popup/Resources/</c>.
/// </summary>
public enum PopupId
{
    [HideInInspector]
    None = -1,
    Settings,
    Pause,
    Revive,
    Lose,
    Win,
    Shop,
    UnlockBooster,
    MoreBooster,
    MoreLife,
    LoseLife,
    LoseEvent,
    Profile,
    DailyReward,
    NewTrap,

    AdsCaution,
    PurchaseFail,
    PurchaseSuccess,
    NoInternet,
    StarterPack,
    RemoveAds,
    RemoveAdsPack,

    MasterPass,
    MasterPassEnd,
    MasterPassInfo,
    ActivatePass,

    JourneyOffer,

    Galactic,
    JoinGalactic,
    SearchGalactic,
    GalacticInfo,
    GalacticWin,
    GalacticLose,

    DailyLogin,

    RequireUpdate,

    WinStreak,
    WinStreakInfo,
    WinStreakClaimChest,

    LavaQuest,
    LavaQuestClaim,
    LavaQuestFinding,
    LavaQuestInfo,
    LavaQuestIntroduce,
    LavaQuestFail,

    MissionControl,
    MissionControlInfo,

    TowerRise,
    TowerRiseClaimBox,
    TowerRiseInfo,
    TowerRiseIntroduce,

    CardCollection,
    CardCollectionInfo,
    CardCollectionSpin,
    CardCollectionShop,
    CardCollectionAlbum,
    CardCollectionReward,
}
