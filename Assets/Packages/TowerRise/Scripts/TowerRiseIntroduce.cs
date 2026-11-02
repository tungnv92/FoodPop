using GamePackages;
using System;
using TMPro;
using UnityEngine;

public class TowerRiseIntroduce : Popup
{
    [Header("TIME:")]
    public TextMeshProUGUI countDownText;
    [Header("TEXT:")]
    public TextMeshProUGUI descriptionText;
    public Action OnClose = () => { };

    public override void Open(object args = null)
    {
        base.Open(args);
        OnClose = () =>
        {
            if (TowerRiseTutorial.Ins != null) TowerRiseTutorial.Ins.Done();
            if (TowerRiseManager.Ins != null && TowerRiseManager.Ins.towerRiseData != null)
            {
                TowerRiseManager.Ins.towerRiseData.isDoneTutorial = true;
                TowerRiseManager.Ins.towerRiseData.SaveData();
            }
        };
    }

    private void Update()
    {
        if (TowerRiseManager.Ins == null) return;
        if (countDownText != null) countDownText.text = TowerRiseManager.Ins.GetCountDownString();
    }

    public void ButtonStartEvent()
    {
        if (TowerRiseManager.Ins != null)
        {
            TowerRiseManager.Ins.StartEvent();
        }

        Close();
        UIManager.Ins.OpenPopup(PopupId.TowerRise);
        OnClose?.Invoke();
        OnClose = null;

        if (TowerRiseManager.Ins != null && !TowerRiseManager.Ins.towerRiseData.isShowedInfo)
        {
            var info = UIManager.Ins.OpenPopup<TowerRiseInfo>(PopupId.TowerRiseInfo);
            if (info != null)
            {
                info.transform.SetAsLastSibling();
                info.OnClose = PlayIntroduceScrollAfterInfo;
            }
            else
            {
                PlayIntroduceScrollAfterInfo();
            }
        }
        else
        {
            PlayIntroduceScrollAfterInfo();
        }
    }

    void PlayIntroduceScrollAfterInfo()
    {
        if (TowerRiseManager.Ins == null || !TowerRiseManager.Ins.pendingIntroduceScroll) return;
        if (UIManager.Ins == null || !UIManager.Ins.IsPopupOpen(PopupId.TowerRise)) return;

        TowerRiseManager.Ins.pendingIntroduceScroll = false;
        var tower = UIManager.Ins.GetPopup<TowerRise>(PopupId.TowerRise);
        tower?.Introduce(TowerRiseManager.Ins.SnapLastEventDisplayToCurrent);
    }

    public void ButtonClose()
    {
        Close();
        OnClose?.Invoke();
        OnClose = null;
    }
}

public class PopupTowerRiseIntroduce : TowerRiseIntroduce
{
}

public class HoneyRiseIntroduce : TowerRiseIntroduce
{
}

public class PopupHoneyRiseIntroduce : TowerRiseIntroduce
{
}
