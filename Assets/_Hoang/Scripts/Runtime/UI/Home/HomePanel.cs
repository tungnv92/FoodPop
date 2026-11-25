using GamePackages;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HomePanel : Panel
{
    private const float BASE_RATIO = 1920f / 1080f;
    public CoinDisplay CoinDisplay => _coinDisplay;
    public RectTransform LifeDisplay => _lifeDisplay;
    public ShopTab Shop => _shop;

    [SerializeField] RectTransform _rectTf;
    [SerializeField] RectTransform _innerRectTf;
    [SerializeField] RectTransform _refTf;
    [SerializeField] ScrollSnap _scroller;
    [SerializeField] ShopTab _shop;
    [SerializeField] RectTransform _mainSpineTf;
    [SerializeField] HorizontalLayoutGroup _topHorizontal;
    [SerializeField] RectTransform topRectTf;

    private bool _isInnerResized;

    // EN: Waits for verified Internet and profile startup before resizing the home panel and starting its popup sequence.
    // VI: Chờ Internet đã xác thực và hồ sơ khởi tạo xong trước khi chỉnh kích thước Home và chạy chuỗi popup.
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => ProfileManager.instance.IsReady);
        ResizeInnerContainer();
        yield return new WaitForEndOfFrame();
        //AudioManager.Ins.PlayMusic(0, MusicType.Home);
        if (_mainSpineTf != null)
        {
            _mainSpineTf.anchoredPosition *= _rectTf.sizeDelta.y / 1920f;
        }
        StartCoroutine(RunHomeSequence());
        if (_topHorizontal != null)
        {
            _topHorizontal.enabled = false;
        }
    }

    // EN: Resizes the 1080x1920 inner container once to match the visible screen dimensions and safe area on any aspect ratio.
    // VI: Điều chỉnh kích thước khung chứa 1080x1920 bên trong một lần để khớp với màn hình hiển thị và vùng an toàn trên mọi tỉ lệ màn hình.
    public void ResizeInnerContainer()
    {
        if (_isInnerResized || _innerRectTf == null) return;
        _isInnerResized = true;

        float ratio = Screen.height * 1f / Screen.width;
        if (ratio < BASE_RATIO)
        {
            _innerRectTf.ChangeSizeDeltaX(_innerRectTf.sizeDelta.x * BASE_RATIO / ratio);
        }
        else
        {
            _innerRectTf.ChangeSizeDeltaY(_innerRectTf.sizeDelta.y * ratio / BASE_RATIO);
        }

        ApplySafeAreaToInner();
    }

    // EN: Briefly blocks Home input while it settles; no automatic IAP, feature popup or tutorial sequence runs.
    // VI: Chặn thao tác ở Home một lúc khi vừa vào; không tự chạy gợi ý IAP, chuỗi popup feature hay tutorial.
    public IEnumerator RunHomeSequence()
    {
        UIManager.Ins.EnableBlocker(true);
        yield return new WaitForSeconds(0.5f);
        //yield return CheckNewVersion();
        UIManager.Ins.EnableBlocker(false);
    }

    public void SnapToPage(int id)
    {
        _scroller.SnapToPage(id);
    }

    public void ResetCoinLife()
    {
        _coinDisplay.transform.SetParent(topRectTf);
        _lifeDisplay.transform.SetParent(topRectTf);
    }

    // EN: Applies Screen.safeArea normalized bounds (following SafeArea.cs) to the inner container's sizeDelta and anchoredPosition.
    // VI: Áp dụng giới hạn chuẩn hóa của Screen.safeArea (theo phương pháp của SafeArea.cs) vào sizeDelta và anchoredPosition của khung chứa bên trong.
    private void ApplySafeAreaToInner()
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        if (anchorMin.x >= 0f && anchorMin.y >= 0f && anchorMax.x >= 0f && anchorMax.y >= 0f)
        {
            Vector2 fullScreenSize = _innerRectTf.sizeDelta;
            Vector2 centerOffset = (anchorMin + anchorMax) * 0.5f - new Vector2(0.5f, 0.5f);

            _innerRectTf.sizeDelta = new Vector2(
                fullScreenSize.x * (anchorMax.x - anchorMin.x),
                fullScreenSize.y * (anchorMax.y - anchorMin.y));
            _innerRectTf.anchoredPosition = new Vector2(
                fullScreenSize.x * centerOffset.x,
                fullScreenSize.y * centerOffset.y);
        }
    }

    //IEnumerator CheckNewVersion()
    //{
    //    bool needUpdate = CheckAppVersion();
    //    if (needUpdate)
    //    {
    //        if (ProfileManager.instance.PlayerData.isFirstHomeOpenInDay)
    //        {
    //            PopupNewVersion popupNewVersion = (PopupNewVersion)UIManager.Ins.OpenPopup(PopupId.RequireUpdate);

    //            ProfileManager.instance.PlayerData.isFirstHomeOpenInDay = false;
    //            ProfileManager.instance.SaveData();
    //        }
    //        else yield break;
    //    }

    //bool CheckAppVersion()
    //{
    //    string app_version = Application.version;
    //    Debug.Log("App version: " + app_version);
    //    Version current = new Version(app_version);
    //    Debug.Log("Latest version: " + GameManager.Ins.Config.GameLatestVersion);
    //    Version latest = new Version(GameManager.Ins.Config.GameLatestVersion);

    //    if (current < latest)
    //    {
    //        Debug.Log("Update available");
    //        return true;
    //    }
    //    else
    //    {
    //        Debug.Log("Already latest");
    //        return false;
    //    }
    //}
}
