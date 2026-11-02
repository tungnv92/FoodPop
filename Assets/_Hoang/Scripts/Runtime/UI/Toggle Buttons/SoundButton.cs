using UnityEngine;

public class SoundButton : ToggleButton
{
    public override void InitState()
    {
        //_isEnabled = AudioManager.Ins.SoundEnabled;
        base.InitState();
    }

    public override void SetLogicState(bool isEnable)
    {
        base.SetLogicState(isEnable);
        //AudioManager.Ins.ToggleSound();
    }
}
