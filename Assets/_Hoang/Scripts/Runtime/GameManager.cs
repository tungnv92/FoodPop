using GamePackages;
using System;
using UnityEngine;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    public GameConfig Config;
    private DateTime startupTime;
    public DateTime Now => startupTime + TimeSpan.FromSeconds(Time.realtimeSinceStartup);
    private void OnEnable()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Physics.simulationMode = SimulationMode.Update;
        SetStartupTime(DateTime.Now);
        Debug.Log("DOW: " + Now.DayOfWeek);
    }
    public void SetStartupTime(DateTime startupTime)
    {
        this.startupTime = startupTime;
    }

    public bool IsHardLevel()
    {
        int level = ProfileManager.instance.PlayerData.currentLevel;

        if (level < 15) return false;

        if (level < Config.FirstSuperHard)
        {
            return level % 5 == 0;
        } else
        {
            return level % 10 == 4 || level % 10 == 7;
        }
    }

    public bool IsSuperHardLevel()
    {
        int level = ProfileManager.instance.PlayerData.currentLevel;

        return level >= Config.FirstSuperHard && level % 10 == 0;
    }
}

[Serializable]
public class GameConfig
{
    public string GameLatestVersion = "1.3.0";
    public int BackHomeAfterWinLevel = 2;
    public int LevelEnableMultiCoin = 10;
    public int BoosterInitAmount = 3;
    [Header("Economy")]
    public int WinRewardAmount = 20;
    public int WinRewardAmountHard = 40;
    public int WinRewardAmountSuperHard = 60;
    public int RevivePrice = 1000;
    public int RefillLivesPrice = 1000;
    public int FirstSuperHard = 50;
    public bool CanBackHome() => ProfileManager.instance.PlayerData.currentLevel > BackHomeAfterWinLevel;
    public bool CanShowMultiCoin() => ProfileManager.instance.PlayerData.currentLevel >= LevelEnableMultiCoin;
}