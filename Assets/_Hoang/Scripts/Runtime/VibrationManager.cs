using Lofelt.NiceVibrations;
using UnityEngine;

public class VibrationManager : SingletonMonoBehaviour<VibrationManager>
{
    const string Key = "Vibration";

    bool isEnabled = true;
    public bool Enabled => isEnabled;

    public void Init()
    {
        isEnabled = PlayerPrefs.GetInt(Key, 1) == 1;
        ReloadVibration();
    }

    public void ToggleVibration()
    {
        isEnabled = !isEnabled;
        PlayerPrefs.SetInt(Key, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ReloadVibration();
    }

    static void ReloadVibration()
    {
//MMVibrationManager.SetHapticsActive(isEnabled);
    }

    public void Vibrate(HapticType type = HapticType.Medium)
    {
        if (isEnabled)
        {
            switch (type)
            {
                case HapticType.Strong:
                    HapticPatterns.PlayEmphasis(0.8f, 0.5f);
                    break;
                case HapticType.Medium:
                    HapticPatterns.PlayEmphasis(0.6f, 0.4f);
                    break;
                case HapticType.Light:
                    HapticPatterns.PlayEmphasis(0.4f, 0.2f);
                    break;
            }
        }
    }
}

public enum HapticType
{
    Light = 0,
    Medium,
    Strong,
}
