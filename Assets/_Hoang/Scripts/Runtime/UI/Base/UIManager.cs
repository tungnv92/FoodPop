using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central UI management singleton. Handles popup lifecycle (instantiate, open, close, reuse)
/// and panel access. One instance per scene.
/// </summary>
public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [Header("Panel")]
    [SerializeField] private Panel _panel;

    [Header("Popup Parents")]
    [SerializeField] private RectTransform _popupBackParent;
    [SerializeField] private RectTransform _popupFrontParent;

    [Header("Blocker")]
    [SerializeField] private GameObject _blocker;

    [Header("Banner")]
    [SerializeField] private int _bannerHeight;

    private int _openPopupCount;
    private readonly Dictionary<PopupId, Popup> _popupById = new();

    public int GetBannerHeight()
    {
        return _bannerHeight;
    }
    /// <summary>
    /// Returns true if any popup is currently open.
    /// </summary>
    public bool IsAnyPopupOpen => _openPopupCount > 0;

    #region POPUP

    /// <summary>
    /// Called by <see cref="Popup.Open"/> to track open count. Do not call directly.
    /// </summary>
    public void OnPopupOpen()
    {
        _openPopupCount++;
    }

    /// <summary>
    /// Called by <see cref="Popup.Close"/> to track open count. Do not call directly.
    /// </summary>
    public void OnPopupClose()
    {
        _openPopupCount--;
    }

    /// <summary>
    /// Checks whether a popup of the given type has been instantiated (may be inactive).
    /// </summary>
    public bool IsPopupInstantiated(PopupId id)
    {
        return _popupById.ContainsKey(id) && _popupById[id] != null;
    }

    /// <summary>
    /// Checks whether a popup of the given type is currently active and visible.
    /// </summary>
    public bool IsPopupOpen(PopupId id)
    {
        return IsPopupInstantiated(id) && _popupById[id].gameObject.activeSelf;
    }

    [Button]
    /// <summary>
    /// Opens or creates a popup by its <see cref="PopupId"/>.
    /// If the popup has not been instantiated, it is loaded from Resources
    /// (prefab name: "Popup" + id). If already instantiated but hidden, it is reopened.
    /// </summary>
    /// <param name="id">The popup identifier.</param>
    /// <param name="args">Optional data passed to <see cref="Popup.Open"/>.</param>
    /// <returns>The opened popup instance, or null if the prefab was not found.</returns>
    public Popup OpenPopup(PopupId id, object args = null)
    {
        RectTransform popupParent = (id == PopupId.Settings) ? _popupBackParent : _popupFrontParent;

        if (!IsPopupInstantiated(id))
        {
            Popup prefab = Resources.Load<Popup>("Popup" + id.ToString());
            if (prefab == null)
            {
                prefab = Resources.Load<Popup>(id.ToString());
            }
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] Popup prefab not found: Popup{id} or {id}");
                return null;
            }

            _popupById[id] = Instantiate(prefab, popupParent);
            _popupById[id].Open(args);
        }
        else if (!_popupById[id].gameObject.activeInHierarchy)
        {
            _popupById[id].Open(args);
        }

        return _popupById[id];
    }

    /// <summary>
    /// Opens a popup and returns it cast to a specific <see cref="Popup"/> subclass.
    /// </summary>
    public T OpenPopup<T>(PopupId id, object args = null) where T : Popup
    {
        Popup popup = OpenPopup(id, args);
        if (popup == null) return null;
        return popup as T;
    }

    /// <summary>
    /// Gets a reference to an already-instantiated popup without opening it.
    /// Returns null if the popup has not been instantiated yet.
    /// </summary>
    public T GetPopup<T>(PopupId id) where T : Popup
    {
        if (!IsPopupInstantiated(id)) return null;
        return _popupById[id] as T;
    }

    /// <summary>
    /// Closes a specific popup if it is currently open.
    /// </summary>
    public void ClosePopup(PopupId id)
    {
        if (IsPopupOpen(id))
        {
            _popupById[id].Close();
        }
    }

    /// <summary>
    /// Closes all currently open popups.
    /// </summary>
    public void CloseAllPopups()
    {
        if (!IsAnyPopupOpen) return;

        foreach (Popup popup in _popupById.Values)
        {
            if (popup != null && popup.gameObject.activeInHierarchy)
            {
                popup.Close();
            }
        }
    }

    /// <summary>
    /// Returns a <see cref="WaitUntil"/> that completes when the specified popup is closed.
    /// Useful for coroutine flows that need to wait for user dismissal.
    /// </summary>
    public WaitUntil WaitUntilClose(PopupId id)
    {
        return new WaitUntil(() => !IsPopupOpen(id));
    }

    #endregion

    #region BLOCKER

    /// <summary>
    /// Enables or disables the fullscreen input blocker overlay.
    /// Use during transitions or async operations to prevent user input.
    /// </summary>
    public void EnableBlocker(bool enable)
    {
        _blocker?.SetActive(enable);
    }

    #endregion

    #region PANEL

    /// <summary>
    /// Gets the current scene's panel.
    /// </summary>
    public Panel GetPanel()
    {
        return _panel;
    }

    /// <summary>
    /// Gets the current scene's panel cast to a specific <see cref="Panel"/> subclass.
    /// </summary>
    public T GetPanel<T>() where T : Panel
    {
        return _panel as T;
    }

    /// <summary>
    /// Enables or disables the current scene's panel canvas.
    /// </summary>
    public void EnablePanel(bool enable)
    {
        if (_panel != null)
        {
            _panel.EnableCanvas(enable);
        }
    }

    #endregion
}
