using System;
using UnityEngine;

public class Observer
{
    public static Action OnNewday;
    public static Action OnLevelStart;
    public static Action OnLevelPause;
    public static Action OnLevelResume;
    public static Action OnLevelReset;
    public static Action OnGameWin;
    public static Action OnGameLose;
    public static Action OnChangeLanguage;
    public static Action OnBoughtRemoveAdsAction;
    public static Action OnBoughtStarterPackAction;
    public static Action OnBoughtRevivePackAction;
    public static Action<bool> SetOverideLayerPanelCoin;
    public static Action OnMasterPassTrigger;
    public static Action OnMasterPassActivePremium;
    public static Action OnMasterPassClaimed;
    public static Action<int> OnLockCanvasHome;
    public static Action OnOpenHome;
    public static Action OnUpdateUserProfile;
    public static Action TriggerMissingRewardMasterPass;
    public static Action OnPurchaseCoinSuccessAction;
    public static Action OnDisactiveInput;
    public static Action OnActiveInput;
    public static Action<bool> OnFadeCanvasGameplay;
}

