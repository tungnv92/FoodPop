using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
namespace GamePackages
{
    public class AwaiterActionManager : MonoBehaviour
    {
        public static AwaiterActionManager Instance;
        public AwaiterActionData data = new();
        public List<AwaiterAction> awaiterActions => data.awaiterActions;
        bool isInitialized = false;
        DateTime lastTimeOnline;
        public bool IsInit()
        {
            return isInitialized;
        }
        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        private void Start()
        {
            LoadData();
        }
        public void LoadData()
        {
            Debug.Log("LoadData AwaiterActionManager");
            string s = PlayerPrefs.GetString("AwaiterActionData", "");
            if (!string.IsNullOrEmpty(s))
            {
                AwaiterActionData saver = JsonUtility.FromJson<AwaiterActionData>(s);
                data.awaiterActions = saver.awaiterActions;
                data.sLastTimeOnline = saver.sLastTimeOnline;
            }
            double offTime = 0;
            if (string.IsNullOrEmpty(data.sLastTimeOnline)) lastTimeOnline = DateTime.Now;
            else
            {
                offTime = (DateTime.Now - TimeUtil.StringToDateTime(data.sLastTimeOnline)).TotalSeconds;
            }
            for (int i = awaiterActions.Count - 1; i >= 0; i--)
            {
                awaiterActions[i].time -= (float)offTime;
            }
            isInitialized = true;

        }
        public void SaveData()
        {
            PlayerPrefs.SetString("AwaiterActionData", JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
        private void Update()
        {
            if (!isInitialized) return;
            for (int i = awaiterActions.Count - 1; i >= 0; i--)
            {
                awaiterActions[i].time -= Time.deltaTime;
                if (awaiterActions[i].time <= 0)
                {
                    string callbackName = awaiterActions[i].callbackName;
                    if (!string.IsNullOrEmpty(callbackName)) ExcuteCallback(callbackName);
                    awaiterActions.Remove(awaiterActions[i]);
                    SaveData();
                }
            }
            data.sLastTimeOnline = TimeUtil.DateTimeToString(DateTime.Now);
        }
        public AwaiterAction RegistryAwaiterAction(string _key, int _time, UnityAction action)
        {

            AwaiterAction awaiter = awaiterActions.Where(x => x.key == _key).FirstOrDefault();
            if (awaiter == null)
            {
                awaiter = new AwaiterAction
                {
                    key = _key
                };
                awaiterActions.Add(awaiter);
            }
            awaiter.time += _time;
            if (action != null) awaiter.callbackName = action.GetMethodInfo().Name;
            SaveData();
            return awaiter;
        }
        public AwaiterAction LoadAwaiterAction(string _key)
        {
            AwaiterAction awaiter = awaiterActions.Where(x => x.key == _key).FirstOrDefault();
            return awaiter;
        }
        public void ExcuteCallback(string callbackName)
        {
            // Get the type of the current instance
            Type type = this.GetType();
            // Find the method by name
            MethodInfo methodInfo = type.GetMethod(callbackName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo != null)
            {
                // Create a delegate for the method
                Action action = (Action)Delegate.CreateDelegate(typeof(Action), this, methodInfo);
                action();
            }
        }

        public void RemoveKey(string key)
        {
            var action = awaiterActions.Find(x => x.key == key);
            if (awaiterActions.Contains(action))
            {
                awaiterActions.Remove(action);
            }
        }
    }

    [Serializable]
    public class AwaiterAction
    {
        public string key;
        public float time;
        public string callbackName;
        public bool IsEnd() => time <= 0;
        public string TimeToString() => TimeUtil.TimeToString(time);

    }
    public class AwaiterActionKey
    {
        public static string AdsFreeDailyReward = "AdsFreeDailyReward";
        public static string StarterPackSale_6 = "StarterPackSale_6";
        public static string StarterPackSale_4 = "StarterPackSale_4";
    }
    [Serializable]
    public class AwaiterActionData
    {
        public List<AwaiterAction> awaiterActions = new();
        public string sLastTimeOnline;
    }
}