using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
namespace GamePackages
{
    [Serializable]
    public class TimeManager : ISaveData<TimeManager>
    {
        string saveKey = "TimeManager";
        public string sLastOnlineTime;
        string sLastOnlineTimeSave;
        bool isNetworkConnection;
        #region Private Fields
        private DateTime currentTime = DateTime.Now;
        private DateTime lastTime = DateTime.Now;
        private DateTime cachedTime = DateTime.Now;
        private DateTime lastLocalTime = DateTime.Now;
        private bool isFirstTime = true;
        public bool isInitialized = false;
        #endregion
        #region Callback Dictionaries
        private readonly Dictionary<string, Action> dayCallbacks = new Dictionary<string, Action>();
        #endregion
        public void LoadData()
        {
            TimeManager data = OnLoadData(saveKey);

            if (data is default(TimeManager) || data == null)
            {
                sLastOnlineTime = TimeUtil.DateTimeToString(DateTime.Now);
            } else
            {
                sLastOnlineTime = data.sLastOnlineTime;

            }
            if (string.IsNullOrEmpty(sLastOnlineTime))
            {
                sLastOnlineTime = TimeUtil.DateTimeToString(DateTime.Now);
            }
            InitializeTimeLocal();
            SyncGoogleTimeAsync();
            sLastOnlineTimeSave = sLastOnlineTime;
            SaveData();
        }
        private void InitializeTimeLocal()
        {
            lastTime = DateTime.Now;
            cachedTime = lastTime;
            lastLocalTime = DateTime.Now;
        }
        private async void SyncGoogleTimeAsync()
        {
            var googleTime = await GetGoogleTimeAsync();
            if (ProfileManager.instance.IsCheatLevel) googleTime = DateTime.Now;
            lastTime = googleTime;
            cachedTime = googleTime;
            lastLocalTime = DateTime.Now;
            isInitialized = true;
        }
        public void SaveData()
        {
            OnSaveData(saveKey);
            PlayerPrefs.Save();
        }
        DateTime lastTimeSave=DateTime.Now;
        public async void CheckNewDayWhenLogin()
        {
            while (!isInitialized)
            {
                await Task.Delay(100);
            }
            if(!string.IsNullOrEmpty(sLastOnlineTimeSave))
            lastTimeSave = TimeUtil.StringToDateTime(sLastOnlineTimeSave);
            Debug.Log("Last time save 2:" + sLastOnlineTimeSave);
            CheckTimeCallbacks();
        }
        public static async Task<DateTime> GetGoogleTimeAsync()
        {
            try
            {
                using var request = UnityWebRequest.Head("https://www.google.com");
                request.timeout = 3;
                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string dateHeader = request.GetResponseHeader("Date");

                    if (!string.IsNullOrEmpty(dateHeader))
                    {
                        return DateTime.Parse(dateHeader)
                            .ToLocalTime();
                    }
                }
            } catch (Exception e)
            {
                Debug.LogError($"GetGoogleTimeAsync Error: {e}");
            }

            return DateTime.UtcNow.ToLocalTime();
        }

        public void UpdateTime()
        {
            CheckNetworkConnection();
            if (!isNetworkConnection) return;
            if (!isInitialized || isFirstTime)
            {
                isFirstTime = false;
                return;
            }
            // Tính thời gian hiện tại = thời gian cache + delta time local
            TimeSpan deltaTime = DateTime.Now - lastLocalTime;
            TimeSpan timeDifference = lastTime - currentTime;
            if (timeDifference.TotalSeconds >= 1)
            {
                CheckTimeCallbacks();
                currentTime = lastTime;
            }
            lastTime = cachedTime + deltaTime;
            sLastOnlineTime = TimeUtil.DateTimeToString(GetNow());
        }


        private void CheckTimeCallbacks()
        {

            if (lastTimeSave.Date != lastTime.Date)
            {
                Debug.Log("Last time save 1:" + sLastOnlineTimeSave);
                Debug.Log("Last time save:" + lastTimeSave);
                Debug.Log("Last time save date:" + lastTimeSave.Date);
                Debug.Log("Last time:" + lastTime);
                Debug.Log("Last time date:" + lastTime.Date);
                InvokeCallbacks(dayCallbacks);
                sLastOnlineTime = TimeUtil.DateTimeToString(lastTime);
                lastTimeSave = lastTime;
                Observer.OnNewday?.Invoke();
                SaveData();
            }
        }
        private void InvokeCallbacks(Dictionary<string, Action> callbacks)
        {
            foreach (var callback in callbacks.Values)
            {
                callback?.Invoke();
            }
        }
        public void AddDayCallback(string key, Action callback)
        {
            if (!dayCallbacks.ContainsKey(key))
            {
                dayCallbacks[key] = callback;
            }
        }

        public void RemoveDayCallback(string key)
        {
            dayCallbacks.Remove(key);
        }
        bool isErr = false;

        void CheckNetworkConnection()
        {
            //if (PlayerProfile.instance.PlayerData.isRemoveAds)
            //{
            //    isNetworkConnection = true;
            //    UIManager.instance.CloseUI<CanvasNetworkError>();
            //    return;
            //}
            bool hasConnect = false;
            NetworkReachability reachability = Application.internetReachability;
            if (reachability == NetworkReachability.NotReachable)
            {
                hasConnect = false;
                isNetworkConnection = false;
            } else
            {
                hasConnect = true;
                isNetworkConnection = true;
            }
            if (hasConnect)
            {
                if (isErr)
                {
                    isErr = false;
                    //UIManager.instance.CloseUI<CanvasNetworkError>();
                }
                isNetworkConnection = true;
            } else if (!hasConnect)
            {
                isErr = true;
                isNetworkConnection = false;
                //if (!UIManager.instance.IsUICanvasOpened<CanvasNetworkError>())
                //    UIManager.instance.OpenUI<CanvasNetworkError>();
            }
        }
        public bool IsBlockByNetwork()
        {
            return !isNetworkConnection;
        }

        [Button]
        public void ClearData()
        {
            PlayerPrefs.SetString("TimeManager", "");
        }
        public DateTime GetNow() => lastTime.ToLocalTime();

    }
}