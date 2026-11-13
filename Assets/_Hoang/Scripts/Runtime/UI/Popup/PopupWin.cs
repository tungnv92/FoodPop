using AssetKits.ParticleImage;
using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
using Longvm.UIAnimator;
using MonetToolkit;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopupWin : Popup
{
    [SerializeField] RectTransform _boxTf;
    [SerializeField] float _normalHeight;
    [SerializeField] float _shortenHeight;
    [SerializeField] UIAnimator openAnim;
    [SerializeField] UIAnimator closeAnim;

    [SerializeField] RectTransform rewardCoin;
    [SerializeField] TMP_Text rewardCoinTmp;
    private RewardAnimator rewardAnimator;
    //[SerializeField] NewFeature newFeature;
    [SerializeField] RectTransform buttonGroup;
    [SerializeField] GameObject multiCoinButton;
    [SerializeField] TMP_Text continueTmp;
    [SerializeField] ParticleImagePlayer coinAnim;
    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;

    int _rewardAmount;
    // EN: Opens the view using manager queries and commands instead of mutable save models.
    // VI: Mở giao diện bằng truy vấn và lệnh manager thay vì model lưu có thể sửa.
    public override void Open(object args = null)
    {
        base.Open(args);
        openAnim.Init();

        bool isHard = GameManager.Ins.IsHardLevel();
        bool isSuperHard = GameManager.Ins.IsSuperHardLevel();
        _rewardAmount = GameManager.Ins.Config.WinRewardAmount;
        if (isHard) _rewardAmount = GameManager.Ins.Config.WinRewardAmountHard;
        if (isSuperHard) _rewardAmount = GameManager.Ins.Config.WinRewardAmountSuperHard;

        rewardCoinTmp.text = _rewardAmount.ToString();
        bool canBackHome = ProfileManager.instance.PlayerDataManager.GetCurrentLevel() >= GameManager.Ins.Config.BackHomeAfterWinLevel;
        UIManager.Ins.GetPanel().SetActiveCoin(!canBackHome);
        continueTmp.text = canBackHome ? "Continue" : "Claim";
        ProfileManager.instance.PlayerDataManager.AddPendingCoins(_rewardAmount);
        //newFeature.InitProgress();
        //_boxTf.ChangeSizeDeltaY(newFeature.gameObject.activeInHierarchy ? _normalHeight : _shortenHeight);
        //multiCoinButton.SetActive(GameManager.Ins.Config.CanShowMultiCoin() && SDKBridge.IsActiveRewardAds);
        // Events
        int retryCount = ProfileManager.instance.GameFeatureContext.RecordManager.GetRetryCount(ProfileManager.instance.PlayerDataManager.GetCurrentLevel());
        bool isFirstTry = retryCount <= 0;
        bool isHardLevel = isHard || isSuperHard;
        ProfileManager.instance.GameFeatureContext.WinStreakManager.OnLevelWin();
        ProfileManager.instance.GameFeatureContext.TowerRiseManager.OnWinLevel();
        ProfileManager.instance.GameFeatureContext.LavaQuestManager.OnLevelWin();
        ProfileManager.instance.GameFeatureContext.MissionControlManager.OnLevelWin(isFirstTry, isHardLevel);
        ProfileManager.instance.PlayerDataManager.IncreaseLevel();

        //multiCoinButton.SetActive(DataManager.Ins.Data.LevelIndex >= GameManager.Ins.Config.LevelEnableMultiCoin);
        openAnim.Play(() =>
        {
            //confestiFx.Play();
            StartCoroutine(OpenCoroutine());
        });
    }

    IEnumerator OpenCoroutine()
    {
        //rewardCoin.DOScale(1, 0.25f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.5f);
        //newFeature.RunProgress();
        yield return new WaitForSeconds(0.75f);
        //if (GalacticManager.Ins.Data.State == EventState.Joined)
        //{
        //    yield return new WaitForSeconds(1f);
        //    UIManager.Ins.OpenPopup(PopupId.Galactic);
        //}
        buttonGroup.DOScale(1, 0.25f).SetEase(Ease.OutBack);
    }

    public override void Close()
    {
        base.Close();
    }

    // EN: Continues the win flow and claims pending coins through the player manager at the reward callback.
    // VI: Tiếp tục luồng thắng và nhận xu chờ qua manager người chơi tại callback nhận thưởng.
    public void OnClickContinue()
    {
        //if (!NetworkManager.Ins.IsInternetAvailable())
        //{
        //    UIManager.Ins.OpenPopup(PopupId.NoInternet);
        //    return;
        //}

        UIManager.Ins.EnableBlocker(true);

        if (GameManager.Ins.Config.CanBackHome())
        {
            closeAnim.Play(() =>
            {
                if (LavaQuestManager.Instance != null && LavaQuestManager.Instance.ShouldShowWinFlow())
                {
                    //SDKBridge.ShowInter(GamePlacement.EndGame, InterPlacement.Win, GameplayManager.Ins.currentLevel, successCallback: () =>
                    //{
                    //    PlayLavaQuestAnim();
                    //});
                    PlayLavaQuestAnim();
                }
                else
                {
                    //SDKBridge.ShowInter(GamePlacement.EndGame, InterPlacement.Win, GameplayManager.Ins.currentLevel, successCallback: () =>
                    //{
                    //    LoadSceneManager.Ins.LoadScene(SceneId.Home, () => { });
                    //});

                    LoadSceneManager.Ins.LoadScene(SceneId.Home, () => { });
                }
            });
        }
        else
        {
            if (rewardAnimator == null) rewardAnimator = UIManager.Ins.GetPanel().RewardAnimator;

            coinAnim.gameObject.SetActive(true);
            coinAnim.Init(_rewardAmount);
            coinAnim.OnReceiveReward(() =>
            {
                ProfileManager.instance.PlayerDataManager.ClaimPendingCoins("ingame_win");
            });
            coinAnim.OnComplete(() =>
            {
                //SDKBridge.ShowInter(GamePlacement.EndGame, InterPlacement.Win, GameplayManager.Ins.currentLevel, successCallback: () => 
                //{
                //    LoadSceneManager.Ins.LoadScene(SceneId.Game, () => { });
                //});

                LoadSceneManager.Ins.LoadScene(SceneId.Game, () => { });
            });
            coinAnim.Play();
            //closeAnim.Play();
        }

    }

    // EN: Coordinates OnClickX2Coin using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối OnClickX2Coin qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public void OnClickX2Coin()
    {
        //if (!NetworkManager.Ins.IsInternetAvailable())
        //{
        //    UIManager.Ins.OpenPopup(PopupId.NoInternet);
        //    return;
        //}

        //UIManager.Ins.EnableBlocker(true);
        //SDKBridge.TrackAdsReward_Click(GamePlacement.Win.ToString(), ButtonPlacement.BtnClaimX2EndGame.ToString(), PlayerDataManager.GetCurrentLevel());
        //SDKBridge.ShowReward(successCallback: () =>
        //{
        //    ProfileManager.instance.PlayerDataManager.AddPendingCoins(_rewardAmount);
        //    ProfileManager.instance.GameFeatureContext.RecordManager.RecordAdsReward();
        //    SDKBridge.TrackAdsReward_Complete(GamePlacement.Win.ToString(), ButtonPlacement.BtnClaimX2EndGame.ToString(), PlayerDataManager.GetCurrentLevel(), new List<ResourceGame>() { new ResourceGame() {type= ResourceType.Coin, value = _rewardAmount * 2 } });

        //    if (GameManager.Ins.Config.CanBackHome())
        //    {
        //        closeAnim.Play(() =>
        //        {
        //            if (LavaQuestManager.Instance != null && LavaQuestManager.Instance.ShouldShowWinFlow())
        //            {
        //                PlayLavaQuestAnim();
        //            }
        //            else
        //            {
        //                LoadSceneManager.Ins.LoadScene(SceneId.Home, () => { });
        //            }
        //        });
        //    }
        //    else
        //    {
        //        if (rewardAnimator == null) rewardAnimator = UIManager.Ins.GetPanel().RewardAnimator;

        //        coinAnim.gameObject.SetActive(true);
        //        coinAnim.Init(_rewardAmount * 2);
        //        coinAnim.OnReceiveReward(() =>
        //        {
        //            ProfileManager.instance.PlayerDataManager.OnAddCoin(ProfileManager.instance.PlayerDataManager.PendingCoinAmount, "ingame_win");
        //            ProfileManager.instance.PlayerDataManager.ClearPendingCoins();
        //            ProfileManager.instance.SaveData();
        //        });
        //        coinAnim.OnComplete(() =>
        //        {
        //            LoadSceneManager.Ins.LoadScene(SceneId.Game, () => { });
        //        });
        //        coinAnim.Play();
        //        closeAnim.Play();
        //    }
        //}, 
        //failCallback: () =>
        //{
        //    UIManager.Ins.EnableBlocker(false);
        //});
    }
    public void PlayLavaQuestAnim()
    {
        UIManager.Ins.EnableBlocker(false);
        LavaQuestManager.Instance.ConsumeWinFlow();
        LavaQuest.isWin = true;
        LavaQuest popup = UIManager.Ins.OpenPopup(PopupId.LavaQuest) as LavaQuest;
        if (popup != null)
        {
            popup.OnClose = () =>
            {
                LoadSceneManager.Ins.LoadScene(SceneId.Home, () => { });
            };
        }
    }
}
