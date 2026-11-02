using UnityEngine;

public class VibrateButton : ToggleButton
{
    public override void InitState()
    {
        _isEnabled = VibrationManager.Ins.Enabled;
        base.InitState();
    }

    public override void SetLogicState(bool isEnable)
    {
        base.SetLogicState(isEnable);
        VibrationManager.Ins.ToggleVibration();
    }
}
