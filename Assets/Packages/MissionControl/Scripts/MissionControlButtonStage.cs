using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionControlButtonStage : MonoBehaviour
{
    public RectTransform rectTransform;
    public int id;
    public TextMeshProUGUI txt;
    public Image img;
    public Sprite onSprite;
    public Sprite offSprite;
    public GameObject notiObj;

    public void OnInitt(int id)
    {
        this.id = id;
        txt.text = "Stage " + (id + 1).ToString();
        SetChoose(false);
        Refresh();
    }

    public void Refresh()
    {
        notiObj.SetActive(MissionControlManager.Ins.IsNoti(id));
    }

    public void SetChoose(bool isChoose)
    {
        img.sprite = isChoose ? onSprite : offSprite;
        //rectTransform.sizeDelta = isChoose ? new Vector2(rectTransform.sizeDelta.x, 156) : new Vector2(rectTransform.sizeDelta.x, 146);
    }

    public void OnClick()
    {
        if (MissionControlSmallChestNgan.isFlyingStatic) return;
        if (MissionControl.Instance != null)
        {
            MissionControl.Instance.ChooseStage(id);
        }
    }
}