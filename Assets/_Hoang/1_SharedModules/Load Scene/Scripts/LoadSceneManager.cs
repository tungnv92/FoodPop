using DG.Tweening;
using SharedModules.ED;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneManager : SingletonMonoBehaviour<LoadSceneManager>
{
    [SerializeField, ReadOnly] SceneId currentSceneId;
    [SerializeField, ReadOnly] SceneId loadingSceneId = SceneId.None;
    public SceneTransition sceneTransition;
    public SceneId CurrentSceneId => currentSceneId;

    protected override void Awake()
    {
        base.Awake();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        if (loadingSceneId == SceneId.None) return;
        currentSceneId = loadingSceneId;
    }

    void CleanUpScene()
    {
        DOTween.KillAll();
        EventDispatcher.ClearAllListeners();
    }

    #region LOAD SCENE
    [Button(ButtonStyle.FoldoutButton)]
    public void LoadScene(SceneId sceneId)
    {
        loadingSceneId = sceneId;
        StartCoroutine(LoadSceneCoroutine(sceneId));
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

    public void LoadOverlayScene(SceneId sceneId)
    {
        loadingSceneId = sceneId;
        StartCoroutine(LoadOverlaySceneCoroutine(sceneId));
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
    #endregion
}
