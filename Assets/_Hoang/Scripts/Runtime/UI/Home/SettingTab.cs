using UnityEngine;

public class SettingTab : HomeTab
{
    //[SerializeField] ToggleButton[] buttons;
    public override void Start()
    {
        float ratio = Screen.height * 1f / Screen.width;
        if (ratio < BASE_RATIO)
        {
            _rectTf.ChangeSizeDeltaX(_rectTf.sizeDelta.x * BASE_RATIO / ratio);
        }
        else
        {
            //_rectTf.ChangeSizeDeltaX(_rectTf.sizeDelta.x * ratio / BASE_RATIO);
            _rectTf.ChangeSizeDeltaY(_rectTf.sizeDelta.y * ratio / BASE_RATIO);
        }

        //foreach (var button in buttons)
        //    button.InitState();
    }
}
