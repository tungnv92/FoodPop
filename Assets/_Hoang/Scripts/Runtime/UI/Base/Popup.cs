using UnityEngine;

/// <summary>
/// Base class for all popup UI elements.
/// Subclass this for each popup type (e.g. PopupSettings, PopupWin).
/// Prefab naming convention: "Popup" + <see cref="PopupId"/> name.
/// </summary>
public abstract class Popup : MonoBehaviour
{
    [SerializeField] protected RectTransform myRt;

    protected virtual void OnValidate()
    {
        if (myRt == null)
        {
            myRt = transform as RectTransform;
        }
    }

    /// <summary>
    /// Opens the popup. Called by <see cref="UIManager"/>.
    /// Override to handle custom initialization with <paramref name="args"/>.
    /// </summary>
    /// <param name="args">Optional data to pass to the popup on open.</param>
    // EN: Opens the popup, notifies UIManager, activates the popup GameObject, and brings it to the front.
    // VI: Mở popup, thông báo cho UIManager, kích hoạt GameObject của popup và đưa lên lớp hiển thị trên cùng.
    public virtual void Open(object args = null)
    {
        UIManager.Ins.OnPopupOpen();
        //AudioManager.Ins.PlaySound(SoundType.PopupShow);
        gameObject.SetActive(true);
        myRt.SetAsLastSibling();
    }

    /// <summary>
    /// Closes the popup. Called by <see cref="UIManager"/> or directly.
    /// Override to handle cleanup or animations before hiding.
    /// </summary>
    public virtual void Close()
    {
        UIManager.Ins.OnPopupClose();
        gameObject.SetActive(false);
    }
}
