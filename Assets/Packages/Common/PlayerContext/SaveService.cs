using UnityEngine;
namespace GamePackages
{
    [System.Serializable]
    public class SaveService
    {
        private PlayerData _playerData;
        // EN: Creates the persistence service for internal profile initialization.
        // VI: Tạo dịch vụ lưu phục vụ khởi tạo hồ sơ nội bộ.
        internal SaveService() { }
        // EN: Creates the persistence service for internal profile initialization.
        // VI: Tạo dịch vụ lưu phục vụ khởi tạo hồ sơ nội bộ.
        internal SaveService(PlayerData playerData)
        {
            _playerData = playerData;
        }
        // EN: Attempts to save the profile and logs failures without interrupting offline gameplay.
        // VI: Thử lưu hồ sơ và ghi log lỗi mà không làm gián đoạn gameplay offline.
        public void SaveData()
        {
            try
            {
                SaveToDisk(_playerData);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[SaveService] Không lưu được dữ liệu: {exception.Message}");
            }
        }

        // EN: Writes the complete current profile in one PlayerPrefs save without backups or rollback.
        // VI: Ghi toàn bộ hồ sơ hiện tại trong một lần lưu PlayerPrefs, không sao lưu hoặc hoàn tác.
        private void SaveToDisk(PlayerData data)
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("PLAYER_DATA", json);
            PlayerPrefs.Save();
        }

        //  Load
        // EN: Loads or creates the profile and restores missing feature models; resource repair is persisted during PlayerDataManager binding.
        // VI: Nạp hoặc tạo hồ sơ và bổ sung model tính năng thiếu; tài nguyên được sửa và lưu khi gắn PlayerDataManager.
        internal PlayerData Load()
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
                data.MissionControlData ??= new();
            }
            return data;
        }
    }
}
