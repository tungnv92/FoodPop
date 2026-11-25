using Sirenix.OdinInspector;
using UnityEngine;

namespace GamePackages
{
    // Base class of every module feature: owns its save model, config and shared services.
    // Lớp cơ sở của mọi tính năng module: giữ model lưu, config và dịch vụ dùng chung.
    public abstract class FeatureManager<TData, TConfig> : IFeature
        where TData : class, new()
        where TConfig : ScriptableObject
    {
        [ShowInInspector, ReadOnly, PropertyOrder(-1)]
        public string Id { get; private set; }
        public TConfig Config { get; private set; }
        // Shown live in the ProfileManager Inspector (Feature Registry > Registered) during Play mode; editable for testing.
        // Hiển thị trực tiếp trong Inspector của ProfileManager (Feature Registry > Registered) khi Play; sửa tay được để test.
        [ShowInInspector]
        protected TData Data { get; private set; }
        protected FeatureServices Services { get; private set; }

        // EN: Runs feature setup through OnInit; called by FeatureRegistry once every feature is registered.
        // VI: Chạy khởi tạo tính năng qua OnInit; FeatureRegistry gọi sau khi mọi tính năng đã đăng ký.
        public void Initialize()
        {
            OnInit();
        }

        // EN: Requires every feature to expose its configured unlock condition.
        // VI: Yêu cầu mọi tính năng cung cấp điều kiện mở theo cấu hình.
        public abstract bool IsEnabledFeature();

        // EN: Serializes the save model for the feature's entry in PlayerData.
        // VI: Chuyển model lưu thành JSON cho entry của tính năng trong PlayerData.
        public string ToJson()
        {
            return JsonUtility.ToJson(Data);
        }

        // EN: Binds the module id, loaded save model, config and services; setup runs later in Initialize.
        // VI: Gắn id module, model lưu đã nạp, config và dịch vụ; phần khởi tạo chạy sau trong Initialize.
        internal void Bind(string id, TData data, TConfig config, FeatureServices services)
        {
            Id = id;
            Data = data;
            Config = config;
            Services = services;
        }

        // EN: Optional setup hook; data, config and services are bound and every feature is already registered for saving.
        // VI: Điểm khởi tạo tùy chọn; data, config và dịch vụ đã gắn và mọi tính năng đã được đăng ký lưu.
        protected virtual void OnInit()
        {
        }

        // EN: Saves the whole profile once; SaveService writes every feature entry before saving.
        // VI: Lưu toàn bộ hồ sơ một lần; SaveService ghi entry của mọi tính năng trước khi lưu.
        protected void Save()
        {
            Services.Save.SaveData();
        }

        // EN: Inspector button (Play mode) that saves the profile after editing Data by hand for testing.
        // VI: Nút trong Inspector (khi Play) để lưu hồ sơ sau khi sửa tay Data để test.
        [Button("Lưu dữ liệu"), PropertyOrder(1)]
        private void SaveFromInspector()
        {
            Save();
        }
    }
}
