using UnityEngine;

public class Avatar_Frame_Button : MonoBehaviour
{
    [SerializeField] GameObject selectObject;

    public void Select()
    {
        selectObject.SetActive(true);
    }

    public void UnSelect()
    {
        selectObject.SetActive(false);
    }

    public void OnClick()
    {
        Select();
    }
}
