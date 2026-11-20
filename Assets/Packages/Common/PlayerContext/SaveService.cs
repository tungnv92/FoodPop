using System;
using System.Collections.Generic;
using UnityEngine;
namespace GamePackages
{
    [System.Serializable]
    public class SaveService
    {
        private PlayerData _playerData;
        private readonly List<IFeature> _features = new();
        // EN: Creates the persistence service for internal profile initialization.
        // VI: Tạo dịch vụ lưu phục vụ khởi tạo hồ sơ nội bộ.
        internal SaveService() { }
        // EN: Creates the persistence service for internal profile initialization.
        // VI: Tạo dịch vụ lưu phục vụ khởi tạo hồ sơ nội bộ.
        internal SaveService(PlayerData playerData)
        {
            _playerData = playerData;
        }

        //  Load
        // EN: Loads or creates the core profile, ensures shop/settings and feature entries exist, and leaves resource repair and module restoration to their binding steps.
        // VI: Nạp hoặc tạo hồ sơ core, bảo đảm có nhóm shop/cài đặt và entry tính năng; bước gắn dịch vụ xử lý sửa tài nguyên và khôi phục module.
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
                data.shopData ??= new();
                data.userSettingData ??= new();
                data.features ??= new();
            }
            return data;
        }

        // EN: Returns a module feature's saved model from its entry, or a new model when there is none. An unreadable entry is kept as "<id>.unreadable" for manual recovery and the feature restarts from a fresh model that is saved normally.
        // VI: Trả về model lưu của tính năng module từ entry, hoặc model mới nếu chưa có. Entry không đọc được được giữ lại dưới tên "<id>.unreadable" để khôi phục tay, tính năng chạy lại từ model mới và được lưu bình thường.
        internal TData LoadFeature<TData>(string id) where TData : class, new()
        {
            FeatureSaveEntry entry = _playerData.features.Find(x => x.id == id);
            if (entry == null) return new TData();

            TData data = ReadEntry<TData>(entry);
            if (data != null) return data;

            SetAsideUnreadableEntry(entry);
            return new TData();
        }

        // EN: Adds a module feature whose state is written into PlayerData on every save.
        // VI: Thêm tính năng module có trạng thái được ghi vào PlayerData mỗi lần lưu.
        internal void RegisterFeature(IFeature feature)
        {
            _features.Add(feature);
        }

        // EN: Writes every module feature entry, then attempts to save the profile and logs failures without interrupting offline gameplay.
        // VI: Ghi entry của mọi tính năng module, rồi thử lưu hồ sơ và ghi log lỗi mà không làm gián đoạn gameplay offline.
        public void SaveData()
        {
            try
            {
                WriteFeatureEntries();
                SaveToDisk(_playerData);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[SaveService] Không lưu được dữ liệu: {exception.Message}");
            }
        }

        // EN: Parses an entry's JSON, returning null when it is empty or malformed.
        // VI: Đọc JSON của entry, trả về null khi rỗng hoặc sai định dạng.
        private static TData ReadEntry<TData>(FeatureSaveEntry entry) where TData : class
        {
            if (string.IsNullOrEmpty(entry.json)) return null;
            try
            {
                return JsonUtility.FromJson<TData>(entry.json);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        // EN: Renames an unreadable entry to "<id>.unreadable" (replacing an older one) so its raw JSON is kept while the feature saves fresh progress under its own id.
        // VI: Đổi tên entry không đọc được thành "<id>.unreadable" (thay bản cũ hơn) để giữ JSON gốc, còn tính năng lưu tiến trình mới dưới id của nó.
        private void SetAsideUnreadableEntry(FeatureSaveEntry entry)
        {
            string backupId = entry.id + ".unreadable";
            _playerData.features.RemoveAll(x => x.id == backupId);
            Debug.LogError($"[SaveService] Không đọc được dữ liệu tính năng '{entry.id}'. Đã giữ JSON gốc trong entry '{backupId}' và khởi tạo lại tính năng.");
            entry.id = backupId;
        }

        // EN: Copies each registered feature's JSON into its PlayerData entry, adding the entry when missing.
        // VI: Chép JSON của từng tính năng đã đăng ký vào entry trong PlayerData, thêm entry nếu chưa có.
        private void WriteFeatureEntries()
        {
            foreach (IFeature feature in _features)
            {
                FeatureSaveEntry entry = _playerData.features.Find(x => x.id == feature.Id);
                if (entry == null)
                {
                    entry = new FeatureSaveEntry { id = feature.Id };
                    _playerData.features.Add(entry);
                }
                entry.json = feature.ToJson();
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


    }
}
