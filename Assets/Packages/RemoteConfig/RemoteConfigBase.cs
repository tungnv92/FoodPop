using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace GamePackages
{
    [Serializable]
    public class RemoteConfigBase
    {
         [Header("Ads")]
        public int level_showinter_win = 15;
        public int level_showinter_lose = 26;
        public int capping_inter = 120;
        public int capping_reward_inter = 40;
        public bool IsActiveRewardAds = true;
        public bool banner_active = false;
        [Header("Events")]
        public int level_unlock_treasure = 18;
        public int level_unlock_busquest = 6;
        public int level_unlock_missoncontrol = 36;
    
        [Header("Unlock")]
        public int back_home_on_win_level = 10;
        public string latest_version = "1.6.0";
        public static string defaultValue = "{\"level_showinter_win\":10,\"level_showinter_lose\":26,\"capping_inter\":120,\"capping_reward_inter\":40,\"IsActiveRewardAds\":true,\"banner_active\":false,\"level_unlock_treasure\":18,\"level_unlock_busquest\":26,\"level_unlock_missoncontrol\":36}";
        [Button]
        public string CVToString()
        {
            string s = JsonUtility.ToJson(this);
            Debug.Log(s);
            return JsonUtility.ToJson(this);
        }

        // EN: Parses remote configuration JSON and applies the loaded settings to game managers.
        // VI: Phân tích chuỗi JSON cấu hình từ xa và áp dụng các thiết lập đã tải cho các trình quản lý game.
        public void ReadData(string json)
        {
            var data = JsonUtility.FromJson<RemoteConfigBase>(json);
            level_showinter_win = data.level_showinter_win;
            level_showinter_lose = data.level_showinter_lose;
            capping_inter = data.capping_inter;
            banner_active = data.banner_active;

            level_unlock_treasure = data.level_unlock_treasure;
            level_unlock_busquest = data.level_unlock_busquest;
            level_unlock_missoncontrol = data.level_unlock_missoncontrol;

            if (data.capping_reward_inter > 0) capping_reward_inter = data.capping_reward_inter;
            IsActiveRewardAds = data.IsActiveRewardAds;
            back_home_on_win_level = data.back_home_on_win_level;
            latest_version = data.latest_version;

            //SDKBridge.level_showinter_win = level_showinter_win;
            //SDKBridge.level_showinter_lose = level_showinter_lose;
            //SDKBridge.capping_inter = capping_inter;
            //SDKBridge.banner_active = banner_active;
            //SDKBridge.capping_reward_inter = capping_reward_inter;
            //SDKBridge.IsActiveRewardAds = IsActiveRewardAds;
            //SDKBridge.level_unlock_busquest = level_unlock_busquest;
            //SDKBridge.level_unlock_treasure = level_unlock_treasure;
            //SDKBridge.level_unlock_missoncontrol = level_unlock_missoncontrol;
        }

        // EN: Compares the current application version against the server version and stores the update flag.
        // VI: So sánh phiên bản ứng dụng hiện tại với phiên bản máy chủ và lưu cờ cập nhật.
        public void ReadGameVersion(string serverVer)
        {
            string currentVersion = Application.version;
            if (string.IsNullOrEmpty(serverVer))
            {
                serverVer = Application.version;
                //SDKBridge.serverVer = serverVer;
            } else
            {
                //SDKBridge.serverVer = currentVersion;
            }
            int result = VersionUtils.CompareVersions(currentVersion, serverVer);
            if (result < 0 /*|| SDKBridge.IsCheatAds()*/)
            {
                PlayerPrefs.SetInt("IsEnableUpdateNewVer", 1);
            } else
            {
                PlayerPrefs.SetInt("IsEnableUpdateNewVer", 0);
            }
            PlayerPrefs.Save();
        }
    }

}
