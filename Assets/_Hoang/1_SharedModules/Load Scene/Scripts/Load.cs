using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using GamePackages;
using System.Diagnostics.Tracing;


#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEditor.SceneManagement;
#endif

public class Load : MonoBehaviour
{
    [SerializeField] Image fillImage;
    //[SerializeField] RectTransform spineRectTf;
    [SerializeField] CanvasScaler loadCanvas;
    //[SerializeField] AdsManager adsManager;

    // EN: Shows unscaled loading progress and waits for Internet and profile initialization before entering Home or gameplay.
    // VI: Hiện tiến độ tải bằng thời gian thực và chờ Internet cùng khởi tạo hồ sơ trước khi vào Home hoặc gameplay.
    IEnumerator Start()
    {
        float baseRatio = 1080f / 1920;
        float screenRatio = Screen.width * 1f / Screen.height;
        //loadCanvas.matchWidthOrHeight = (baseRatio > screenRatio) ? 1 : 0;

        // Fake load
        Tween tween = DOVirtual.Float(0, 0.8f, 2, progress =>
        {
            SetProgress(progress);
        }).SetUpdate(true);

        yield return null;
        // Init
        //AudioManager.Ins.Init();
        VibrationManager.Ins.Init();
        // Load scene
        yield return new WaitUntil(() => !tween.active);
        yield return new WaitUntil(() => ProfileManager.instance.IsReady);
        SceneId scene = (GameManager.Ins.Config.CanBackHome()) ? SceneId.Home : SceneId.Game;
        DOVirtual.Float(0.8f, 1f, 0.5f, progress =>
        {
            SetProgress(progress);
        }).OnComplete(() =>
        {
            LoadSceneManager.Ins.LoadScene(scene);
        });
    }

    void SetProgress(float progress)
    {
        fillImage.fillAmount = progress;
        //spineRectTf.ChangeAnchorPosX((progress - 0.5f) * 356 * 2);
    }
}
