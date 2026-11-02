using UnityEngine;

public class MusicButton : ToggleButton
{
    public override void InitState()
    {
        //_isEnabled = AudioManager.Ins.MusicEnabled;
        base.InitState();
    }

    public override void SetLogicState(bool isEnable)
    {
        base.SetLogicState(isEnable);
        //AudioManager.Ins.ToggleMusic();
    }
}
