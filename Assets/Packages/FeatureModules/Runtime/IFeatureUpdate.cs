namespace GamePackages
{
    public interface IFeatureUpdate
    {
        // EN: Updates the feature during a playable frame.
        // VI: Cập nhật tính năng trong frame được phép chơi.
        void OnUpdate();
    }
}
