using GamePackages;
using TMPro;
using UnityEngine;

public class ButtonWinStreak : MonoBehaviour
{
    [SerializeField] GameObject unlockedObject;
    [SerializeField] GameObject lockedObject;
    [SerializeField] TMP_Text unlock_tmp;

    WinStreakManager manager => ProfileManager.instance.GameFeatureContext.WinStreakManager;

    private void Start()
    {
        unlockedObject.SetActive(manager.IsLevelUnlocked);
        lockedObject.SetActive(!manager.IsLevelUnlocked);
        unlock_tmp.text = $"Level {manager.WinStreakGlobalConfig.LevelActiveFeature}";
    }

    public void OnClick()
    {
        if (!manager.IsLevelUnlocked) return;
        UIManager.Ins.OpenPopup(PopupId.WinStreak);
    }
}
