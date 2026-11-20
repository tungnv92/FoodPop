namespace GamePackages
{
    // Shared services injected once into every module feature.
    // Các dịch vụ dùng chung được inject một lần vào mọi tính năng module.
    public class FeatureServices
    {
        public readonly SaveService Save;
        public readonly PlayerDataManager Player;
        public readonly TimeManager Time;
        public readonly RemoteConfigBase RemoteConfig;

        // EN: Bundles the profile services that features need instead of reaching ProfileManager.instance.
        // VI: Gom các dịch vụ hồ sơ mà tính năng cần, thay cho việc gọi ProfileManager.instance.
        public FeatureServices(SaveService save, PlayerDataManager player, TimeManager time, RemoteConfigBase remoteConfig)
        {
            Save = save;
            Player = player;
            Time = time;
            RemoteConfig = remoteConfig;
        }
    }
}
