using UnityEngine;

public class SettingButton : MonoBehaviour
{
    public void OnClick()
    {
        UIManager.Ins.OpenPopup(PopupId.Settings);
    }
}
