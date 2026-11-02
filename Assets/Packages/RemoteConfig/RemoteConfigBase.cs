using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace GamePackages
{
    [Serializable]
    public class RemoteConfigBase
    {
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
        public bool can_suggest_daily_login = false;
        public bool can_suggest_daily_reward = false;
        [Header("Economy")]
        public int coin_default = 1000;
        public int coin_win_normal = 20;
        public int coin_win_hard = 50;
        public int coin_win_superhard = 80;
        public int revive_price = 900;
        public int refill_life_price = 900;
        public int boosters_default = 1;
        public int booster_1_price = 500;
        public int booster_2_price = 1500;
        public int booster_3_price = 1900;
        public int booster_4_price = 2700;
        [Header("Unlock")]
        public int level_enable_multicoin = 10;
        public int back_home_on_win_level = 10;
        public int first_super_hard = 50;
        public int booster_1_unlock = 4;
        public int booster_2_unlock = 7;
        public int booster_3_unlock = 10;
        public int booster_4_unlock = 13;

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
            can_suggest_daily_login = data.can_suggest_daily_login;
            can_suggest_daily_reward = data.can_suggest_daily_reward;

            if (data.capping_reward_inter > 0) capping_reward_inter = data.capping_reward_inter;
            IsActiveRewardAds = data.IsActiveRewardAds;
            booster_1_unlock = data.booster_1_unlock;
            booster_2_unlock= data.booster_2_unlock;
            booster_3_unlock = data.booster_3_unlock;
            booster_4_unlock = data.booster_4_unlock;

            booster_1_price = data.booster_1_price;
            booster_2_price = data.booster_2_price;
            booster_3_price = data.booster_3_price;
            booster_4_price = data.booster_4_price;

            level_enable_multicoin = data.level_enable_multicoin;
            back_home_on_win_level = data.back_home_on_win_level;
            coin_win_normal = data.coin_win_normal;
            coin_win_hard = data.coin_win_hard;
            coin_win_superhard = data.coin_win_superhard;
            revive_price = data.revive_price;
            refill_life_price = data.refill_life_price;
            boosters_default = data.boosters_default;
            coin_default = data.coin_default;
            first_super_hard = data.first_super_hard;
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
