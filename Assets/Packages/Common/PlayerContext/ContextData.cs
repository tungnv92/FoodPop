using UnityEngine;
namespace GamePackages
{
    public abstract class ContextData<T> : IFeatureSave
    {
        protected T Data;
        protected SaveService SaveService;

        public virtual void SetContext(T data)
        {
            Data = data;
        }
        public void SetSaveService(SaveService saveService)
        {
            SaveService = saveService;
        }

        public void Save()
        {
            if (SaveService != null)
            {
                SaveService.SaveData();
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Save called but SaveService is not set.");
            }
        }
        public T GetData()
        {
            return Data;
        }
    }
    public interface IFeatureSave
    {
        public void SetSaveService(SaveService saveService);
    }
    public interface IFeatureUpdate
    {
        public void OnUpdate();
    }
    public interface IFeatureNewDay
    {
        public void OnNewDay();
    }
    public interface IFeatureTrackOpen
    {
        public void OnTrackingOpenApp();
    }
    public interface IRequireTime
    {
        void SetTimeManager(TimeManager manager);
    }
    public interface IRequirePlayerData
    {
        void SetPlayerDataManager(PlayerDataManager manager);
    }
}