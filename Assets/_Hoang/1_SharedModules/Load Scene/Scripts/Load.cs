using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MonetToolkit;
using System.Diagnostics.Tracing;


#if UNITY_EDITOR
using Sirenix.OdinInspector;
using UnityEditor.SceneManagement;
#endif

public class Load : MonoBehaviour
{
    [SerializeField] Image fillImage;
    [SerializeField] RectTransform spineRectTf;
    [SerializeField] GameObject loadingText;
    [SerializeField] CanvasScaler loadCanvas;
    //[SerializeField] AdsManager adsManager;

    IEnumerator Start()
    {
        float baseRatio = 1080f / 1920;
        float screenRatio = Screen.width * 1f / Screen.height;
        //loadCanvas.matchWidthOrHeight = (baseRatio > screenRatio) ? 1 : 0;

        // Fake load
        Tween tween = DOVirtual.Float(0, 0.8f, 2, progress =>
        {
            SetProgress(progress);
        });

        yield return null;
        // Init
        //FirebaseManager.Ins.Init();
        //bool timeout = false;
        //Scheduler.DoTask(3, () =>
        //{
        //    timeout = true;
        //});
        //yield return new WaitUntil(() => FirebaseManager.Ins.FetchRemoteConfigSuccess || timeout);
        //AdsManager.Ins.Init();
        //yield return null;
        ////AppsFlyerManager.Ins.Init();
        //yield return null;
        IAPManager.Ins.Init();
        //yield return null;
        ////AdMobManager.Ins.Init();
        //yield return null;
        //MasterPassManager.Ins.Init();
        //ProfileManager.Ins.Init();
        //JourneyOfferManager.Ins.Init();
        //GalacticManager.Ins.Init();
        //AudioManager.Ins.Init();
        VibrationManager.Ins.Init();

        loadingText.SetActive(true);
        // Load scene
        yield return new WaitUntil(() => !tween.active);
        SceneId scene = (GameManager.Ins.Config.CanBackHome()) ? SceneId.Home : SceneId.Game;
        DOVirtual.Float(0.8f, 1f, 0.5f, progress =>
        {
            SetProgress(progress);
        }).OnComplete(() =>
        {
            LoadSceneManager.Ins.LoadScene(scene);
        });
        //StartCoroutine(AdMobManager.Ins.ShowAppOpenAd());
    }

    void SetProgress(float progress)
    {
        fillImage.fillAmount = progress;
        spineRectTf.ChangeAnchorPosX((progress - 0.5f) * 356 * 2);
    }
}
