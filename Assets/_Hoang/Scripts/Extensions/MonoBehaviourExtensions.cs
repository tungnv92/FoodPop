using System;
using System.Collections;
using UnityEngine;

public static class MonoBehaviourExtensions
{
    public static IEnumerator Invoke(this MonoBehaviour monoBehaviour, float delay, Action action)
    {
        if (monoBehaviour == null || action == null) return null;
        IEnumerator coroutine = InvokeCoroutine(delay, action);
        monoBehaviour.StartCoroutine(coroutine);
        return coroutine;
    }

    static IEnumerator InvokeCoroutine(float delay, Action action)
    {
        yield return WaitFor.Seconds(delay);
        TryInvoke(action);
    }

    public static IEnumerator InvokeNextFrame(this MonoBehaviour monoBehaviour, Action action)
    {
        if (monoBehaviour == null || action == null) return null;
        IEnumerator coroutine = InvokeNextFrameCoroutine(action);
        monoBehaviour.StartCoroutine(coroutine);
        return coroutine;
    }

    static IEnumerator InvokeNextFrameCoroutine(Action action)
    {
        yield return null;
        TryInvoke(action);
    }

    public static IEnumerator InvokeEndOfFrame(this MonoBehaviour monoBehaviour, Action action)
    {
        if (monoBehaviour == null || action == null) return null;
        IEnumerator coroutine = InvokeEndOfFrameCoroutine(action);
        monoBehaviour.StartCoroutine(coroutine);
        return coroutine;
    }

    static IEnumerator InvokeEndOfFrameCoroutine(Action action)
    {
        yield return WaitFor.EndOfFrame;
        TryInvoke(action);
    }

    public static IEnumerator InvokeAfterFrames(this MonoBehaviour monoBehaviour, int frameCount, Action action)
    {
        if (monoBehaviour == null || action == null || frameCount < 1) return null;
        IEnumerator coroutine = InvokeAfterFramesCoroutine(frameCount, action);
        monoBehaviour.StartCoroutine(coroutine);
        return coroutine;
    }

    static IEnumerator InvokeAfterFramesCoroutine(int frameCount, Action action)
    {
        for (int i = 0; i < frameCount; i++)
        {
            yield return null;
        }
        TryInvoke(action);
    }

    static void TryInvoke(Action action)
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public static void Cancel(this MonoBehaviour monoBehaviour, IEnumerator coroutine)
    {
        if (monoBehaviour == null || coroutine == null) return;
        monoBehaviour.StopCoroutine(coroutine);
    }
}
