using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneManager : MonoBehaviour
{
    public static LoadSceneManager Ins { get; private set; }
    public SceneId CurrentSceneId => currentSceneId;

    [SerializeField, ReadOnly] SceneId currentSceneId;
    [SerializeField, ReadOnly] SceneId loadingSceneId = SceneId.None;
    public SceneTransition sceneTransition;

    // EN: Registers the single scene loader (destroying duplicates) and tracks finished scene loads.
    // VI: Đăng ký bộ nạp scene duy nhất (hủy bản trùng) và theo dõi scene đã nạp xong.
    private void Awake()
    {
        if (Ins != null && Ins != this)
        {
            Destroy(gameObject);
            return;
        }
        Ins = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // EN: Releases the shared instance and stops tracking scene loads.
    // VI: Gỡ instance dùng chung và ngừng theo dõi việc nạp scene.
    private void OnDestroy()
    {
        if (Ins != this) return;
        Ins = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        if (loadingSceneId == SceneId.None) return;
        currentSceneId = loadingSceneId;
    }
    // EN: Kills running tweens before leaving the current scene.
    // VI: Dừng mọi tween đang chạy trước khi rời scene hiện tại.
    void CleanUpScene()
    {
        DOTween.KillAll();
    }

    #region LOAD SCENE
    [Button(ButtonStyle.FoldoutButton)]
    public void LoadScene(SceneId sceneId)
    {
        loadingSceneId = sceneId;
        StartCoroutine(LoadSceneCoroutine(sceneId));
    }

    public void LoadOverlayScene(SceneId sceneId)
    {
        loadingSceneId = sceneId;
        StartCoroutine(LoadOverlaySceneCoroutine(sceneId));
    }

    public void UnloadScene(SceneId sceneId)
    {
        StartCoroutine(UnloadOverlayScene(sceneId));
    }
    public IEnumerator UnloadOverlayScene(SceneId sceneId)
    {
        yield return SceneManager.UnloadSceneAsync((int)sceneId);
    }
    public void LoadScene(SceneId sceneId, Action onLoading, Action onComplete = null, float delay = 0f)  
    {
        loadingSceneId = sceneId;
        StartCoroutine(LoadSceneCoroutine(sceneId, onLoading, onComplete, delay));
    }

    IEnumerator LoadSceneCoroutine(SceneId sceneId)
    {
        CleanUpScene();

        sceneTransition.Close(sceneId, () =>
        {
            SceneManager.LoadSceneAsync((int)sceneId);
            Debug.Log($"Load to scene: {sceneId}");
        });

        yield return new WaitForSeconds(1f);

        sceneTransition.Open();
        Debug.Log("Open Transition");
    }

    IEnumerator LoadSceneCoroutine(SceneId sceneId, Action onLoading, Action onComplete, float delay)
    {
        yield return new WaitForSeconds(delay);
        CleanUpScene();
        if (currentSceneId != SceneId.Load)
        {
            bool closeDone = false;
            sceneTransition.Close(sceneId, () => closeDone = true);
            yield return new WaitUntil(() => closeDone);
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync((int)sceneId);
        operation.allowSceneActivation = false;

        DOVirtual.Float(0, 0.8f, 1.5f, (float value) =>
        {
            sceneTransition.SetProgress(value);
        });
        yield return new WaitForSeconds(1.5f);

        while (!operation.isDone)
        {
            onLoading?.Invoke();
            yield return null;
            if (operation.progress >= 0.9f && !operation.allowSceneActivation)
            {
                yield return null;
                operation.allowSceneActivation = true;
            }
        }
        yield return new WaitUntil(() => operation.isDone);
        DOVirtual.Float(0.8f, 1f, 0.5f, (float value) =>
        {
            sceneTransition.SetProgress(value);
        });
        yield return new WaitForSeconds(1f);
        sceneTransition.Open();
        onComplete?.Invoke();
    }

    IEnumerator LoadOverlaySceneCoroutine(SceneId sceneId)
    {
        CleanUpScene();
        sceneTransition.Close(sceneId, () =>
        {
            SceneManager.LoadSceneAsync((int)sceneId, LoadSceneMode.Additive);
            Debug.Log($"Load to scene: {sceneId}");
        });
        yield return new WaitForSeconds(1f);
        sceneTransition.Open();
        Debug.Log("Open Transition");
    }
    #endregion
}
