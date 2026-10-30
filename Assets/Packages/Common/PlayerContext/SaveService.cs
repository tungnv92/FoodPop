using UnityEngine;
namespace GamePackages
{
    [System.Serializable]
    public class SaveService
    {
        private PlayerData _playerData;
        public SaveService() { }
        public SaveService(PlayerData playerData)
        {
            _playerData = playerData;
        }
        //  Save ngay 
        public void SaveData()
        {
            SaveToDisk(_playerData);
        }
        private void SaveToDisk(PlayerData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("PLAYER_DATA", json);
            PlayerPrefs.Save();
#if UNITY_EDITOR
            Debug.Log($"[SaveService] Saved");
#endif
        }

        //  Load
        public PlayerData Load()
        {
            if (!PlayerPrefs.HasKey("PLAYER_DATA"))
            {
                _playerData = new();
                _playerData.OnInit();

                SaveData();
                return _playerData;
            }
            string json = PlayerPrefs.GetString("PLAYER_DATA");

            PlayerData data = JsonUtility.FromJson<PlayerData>(json);
            if (data != null)
            {
                data.DailyGiftData ??= new();
                data.DailyRewardData ??= new();
                data.RecordTrackingData ??= new();
                data.HeartData ??= new();
                data.WinStreakData ??= new();
                data.LavaQuestData ??= new();
                data.TowerRiseData ??= new();
                if (data.l_ResourceGame == null) data.l_ResourceGame = new();
            }
            return data;
        }
    }
}
