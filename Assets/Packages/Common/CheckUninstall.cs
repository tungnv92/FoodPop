//using AppsFlyerSDK;
//#if UNITY_ANDROID
//using Firebase.Messaging;
//#endif
//using UnityEngine;


//public class CheckUninstall : MonoBehaviour
//{
//#if UNITY_ANDROID
//    void Start()
//    {
//        Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
//    }

//    public void OnTokenReceived(object sender, TokenReceivedEventArgs token)
//    {
//        AppsFlyer.updateServerUninstallToken(token.Token);
//    }
//#endif
//}