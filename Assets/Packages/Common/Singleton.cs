using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GamePackages
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T instance;
        [SerializeField] private bool isDontDesTroyOnLoad;
        protected bool isAwakable;

        protected virtual void Awake()
        {
            if (instance == null)
            {
#if UNITY_EDITOR
                Debug.Log("SINGLETONS  " + gameObject.name);
#endif
                instance = this as T;

                if (isDontDesTroyOnLoad) DontDestroyOnLoad(this);
                isAwakable = true;
            } else
            {
                DestroyImmediate(gameObject);
                isAwakable = false;
            }
        }
    }
}