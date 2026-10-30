using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Frame : MonoBehaviour
{
    [SerializeField] RectTransform myRt;
    public RectTransform MyRt => myRt;
    [SerializeField] Image image;

    int index;
    PopupProfile popup;

    //private void OnEnable()
    //{
    //    MasterPassManager.Ins.OnFinishPass += Reload;
    //}

    //private void OnDisable()
    //{
    //    MasterPassManager.Ins.OnFinishPass = Reload;
    //}

    public void Init(int index, Sprite sprite, PopupProfile popup)
    {
        this.index = index;
        image.sprite = sprite;
        this.popup = popup;
        //lockIcon.SetActive(isGold && !MasterPassManager.Ins.Data.Activated);
        //cd_TMP.gameObject.SetActive(isGold && MasterPassManager.Ins.Data.Activated);
        //goldenFrameEffect.enabled = isGold;
    }

    public void Reload()
    {
        //lockIcon.SetActive(isGold && !MasterPassManager.Ins.Data.Activated);
        //cd_TMP.gameObject.SetActive(isGold && MasterPassManager.Ins.Data.Activated);
        //resetTime = MasterPassManager.Ins.Data.ResetTime.Value;
    }

    private void Update()
    {
        //if (isGold)
        //{
        //    if (deltaTime > 0)
        //    {
        //        deltaTime -= Time.deltaTime;
        //        return;
        //    }

        //    TimeSpan span = resetTime - GameManager.Ins.Now;

        //    deltaTime = 1;

        //    cd_TMP.text = span.ToString_DH_HMS();
        //}
    }
    #region UI EVENTS
    public void OnClick()
    {
        //if (isGold && !MasterPassManager.Ins.Data.Activated)
        //{
        //    UIManager.Ins.GetPanel<HomePanel>().DisplayMessage("purchase_pass_to_unlock");
        //    return;
        //}
        popup.OnSelectFrame(index);
    }
    #endregion
}
