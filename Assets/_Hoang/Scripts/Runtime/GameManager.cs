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

    // EN: Coordinates IsHardLevel using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối IsHardLevel qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public bool IsHardLevel()
    {
        int level = ProfileManager.instance.PlayerDataManager.GetCurrentLevel();

        if (level < 15) return false;

        if (level < Config.FirstSuperHard)
        {
            return level % 5 == 0;
        } else
        {
            return level % 10 == 4 || level % 10 == 7;
        }
    }

    // EN: Coordinates IsSuperHardLevel using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối IsSuperHardLevel qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public bool IsSuperHardLevel()
    {
        int level = ProfileManager.instance.PlayerDataManager.GetCurrentLevel();

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
    // EN: Coordinates CanBackHome using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối CanBackHome qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public bool CanBackHome() => ProfileManager.instance.PlayerDataManager.GetCurrentLevel() > BackHomeAfterWinLevel;
    // EN: Coordinates CanShowMultiCoin using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối CanShowMultiCoin qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    public bool CanShowMultiCoin() => ProfileManager.instance.PlayerDataManager.GetCurrentLevel() >= LevelEnableMultiCoin;
}