using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TimeFetcher : MonoBehaviour
{
    [SerializeField] int timeout = 3;
    [SerializeField] string url = "https://www.google.com";

    public Coroutine FetchTimeFromServer(Action<DateTime> onComplete = null)
    {
        return StartCoroutine(FetchTimeFromServerCoroutine(onComplete));
    }

    IEnumerator FetchTimeFromServerCoroutine(Action<DateTime> onComplete)
    {
        DateTime time;
        UnityWebRequest request = new UnityWebRequest(url);
        request.timeout = timeout;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string date = request.GetResponseHeaders()["date"];
            time = DateTime.Parse(date);
        }
        else
        {
            Debug.LogWarning("USING LOCAL TIME");
            time = DateTime.Now;
        }

        onComplete?.Invoke(time);
    }
}
