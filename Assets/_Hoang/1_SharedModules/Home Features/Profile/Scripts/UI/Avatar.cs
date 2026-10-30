using UnityEngine;
using UnityEngine.UI;

public class Avatar : MonoBehaviour
{
	[SerializeField] RectTransform myRt;
	public RectTransform MyRt => myRt;
    [SerializeField] Image image;

	int index;
	PopupProfile popup;

	public void Init(int index, Sprite sprite, PopupProfile popup)
	{
		this.index = index;
		image.sprite = sprite;
		this.popup = popup;
	}

	#region UI EVENTS
	public void OnClick()
	{
		popup.OnSelectAvatar(index);
	}
	#endregion
}
