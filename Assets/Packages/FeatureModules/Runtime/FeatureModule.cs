using UnityEngine;

namespace GamePackages
{
    // Asset that represents one feature: holds its save id and creates its runtime feature.
    // Asset đại diện cho một tính năng: giữ id lưu và tạo tính năng lúc chạy.
    public abstract class FeatureModule : ScriptableObject
    {
        public string Id => id;

        [Tooltip("Khóa lưu dữ liệu của tính năng trong PlayerData. Không được trống, không trùng module khác và không đổi sau khi phát hành.")]
        [SerializeField] private string id;

        // EN: Creates the feature with its save data and registers it with SaveService; FeatureRegistry initializes it afterwards.
        // VI: Tạo tính năng với dữ liệu lưu và đăng ký nó với SaveService; FeatureRegistry khởi tạo nó sau đó.
        public abstract IFeature CreateFeature(FeatureServices services);
    }

    // Typed module: creates TFeature with its saved TData and this asset's TConfig.
    // Module có kiểu: tạo TFeature với TData đã lưu và TConfig của asset này.
    public abstract class FeatureModule<TFeature, TData, TConfig> : FeatureModule
        where TFeature : FeatureManager<TData, TConfig>, new()
        where TData : class, new()
        where TConfig : ScriptableObject
    {
        [Tooltip("Config của tính năng.")]
        [SerializeField] private TConfig config;

        // EN: Creates the feature from its saved data (or a new model), binds it and registers it for saving.
        // VI: Tạo tính năng từ dữ liệu đã lưu (hoặc model mới), gắn dữ liệu và đăng ký để được lưu.
        public override IFeature CreateFeature(FeatureServices services)
        {
            TFeature feature = new TFeature();
            TData data = services.Save.LoadFeature<TData>(Id);
            feature.Bind(Id, data, config, services);
            services.Save.RegisterFeature(feature);
            return feature;
        }
    }
}
