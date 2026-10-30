using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Gold : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI txt;

    public void ButtonAdd()
    {
        if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.Shop);
        }
    }
}
