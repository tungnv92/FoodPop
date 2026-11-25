using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GamePackages
{
    // Holds the feature module assets, creates their features and forwards lifecycle hooks.
    // Giữ danh sách asset module, tạo tính năng và chuyển tiếp các hook vòng đời.
    [System.Serializable]
    public class FeatureRegistry
    {

        [Tooltip("Các asset module tính năng. Thứ tự trong danh sách là thứ tự khởi tạo. Dùng menu FoodPop/Feature Modules/Collect Modules để tự gom.")]
        [SerializeField] private List<FeatureModule> modules = new();

        [ShowInInspector, LabelText("Registered (Play mode)")]
        [ListDrawerSettings(ListElementLabelName = "Id", IsReadOnly = true)]
        private readonly List<IFeature> features = new();
        private readonly List<IFeatureUpdate> updates = new();
        private readonly List<IFeatureNewDay> newDays = new();
        private readonly List<IFeatureTrackOpen> trackOpens = new();
        private readonly List<IFeatureLevelResult> levelResults = new();

        // EN: Registers valid modules, binds optional statistics to the core player service, then initializes features; empty slots and invalid ids are reported and skipped.
        // VI: Đăng ký module hợp lệ, gắn thống kê tùy chọn vào dịch vụ người chơi core rồi khởi tạo tính năng; báo và bỏ qua ô trống hoặc id không hợp lệ.
        public void InitializeFeatures(FeatureServices services)
        {
            HashSet<string> ids = new();
            foreach (FeatureModule module in modules)
            {
                if (module == null)
                {
                    Debug.LogWarning("[FeatureRegistry] Danh sách Modules có ô trống (asset đã bị xóa). Chạy FoodPop/Feature Modules/Collect Modules để dọn.");
                    continue;
                }
                if (string.IsNullOrEmpty(module.Id) || !ids.Add(module.Id))
                {
                    Debug.LogError($"[FeatureRegistry] Module '{module.name}' có id trống hoặc trùng '{module.Id}', bỏ qua để không ghi đè dữ liệu lưu.", module);
                    continue;
                }
                RegisterFeature(module.CreateFeature(services));
            }
            services.Player.SetTracking(Get<IRecordTracking>());
            foreach (IFeature feature in features)
                feature.Initialize();
        }

        // EN: Returns the registered feature of type T, or null when its module is not in the list.
        // VI: Trả về tính năng kiểu T đã đăng ký, hoặc null khi module của nó không có trong danh sách.
        public T Get<T>() where T : class, IFeature
        {
            foreach (IFeature feature in features)
            {
                if (feature is T typed) return typed;
            }
            return null;
        }

        // EN: Updates features implementing IFeatureUpdate, both for startup catch-up after time verification and during gameplay.
        // VI: Cập nhật tính năng cài IFeatureUpdate để bù thời gian sau xác thực lúc khởi động và trong gameplay.
        public void UpdateFeatures()
        {
            foreach (IFeatureUpdate feature in updates)
                feature.OnUpdate();
        }

        // EN: Forwards the new-day event to features that implement IFeatureNewDay.
        // VI: Chuyển sự kiện sang ngày mới tới các tính năng cài IFeatureNewDay.
        public void OnNewDay()
        {
            foreach (IFeatureNewDay feature in newDays)
                feature.OnNewDay();
        }

        // EN: Forwards open-app tracking to features that implement IFeatureTrackOpen.
        // VI: Chuyển sự kiện tracking mở ứng dụng tới các tính năng cài IFeatureTrackOpen.
        public void TrackAppOpen()
        {
            foreach (IFeatureTrackOpen feature in trackOpens)
                feature.OnTrackingOpenApp();
        }

        // EN: Forwards a level win to features that implement IFeatureLevelResult.
        // VI: Chuyển kết quả thắng màn tới các tính năng cài IFeatureLevelResult.
        public void OnLevelWin(string resultId)
        {
            foreach (IFeatureLevelResult feature in levelResults)
                feature.OnLevelWin(resultId);
        }

        // EN: Forwards a level loss to features that implement IFeatureLevelResult.
        // VI: Chuyển kết quả thua màn tới các tính năng cài IFeatureLevelResult.
        public void OnLevelLose(string resultId)
        {
            foreach (IFeatureLevelResult feature in levelResults)
                feature.OnLevelLose(resultId);
        }

        // EN: Stores the feature and adds it to every hook list whose interface it implements.
        // VI: Lưu tính năng và thêm vào mọi danh sách hook có interface mà nó cài đặt.
        private void RegisterFeature(IFeature feature)
        {
            features.Add(feature);
            if (feature is IFeatureUpdate update) updates.Add(update);
            if (feature is IFeatureNewDay newDay) newDays.Add(newDay);
            if (feature is IFeatureTrackOpen trackOpen) trackOpens.Add(trackOpen);
            if (feature is IFeatureLevelResult levelResult) levelResults.Add(levelResult);
        }
    }
}
