using UnityEngine;
namespace GamePackages
{
    public abstract class ContextData<T> : IFeatureSave
    {
        protected T Data;
        protected SaveService SaveService;

        // EN: Binds the saved model through internal initialization and initializes this feature's state.
        // VI: Gắn model lưu qua khởi tạo nội bộ và khởi tạo trạng thái của tính năng này.
        internal virtual void SetContext(T data)
        {
            Data = data;
        }
        // EN: Binds persistence internally without exposing the saved model to callers.
        // VI: Gắn dịch vụ lưu nội bộ mà không lộ model lưu cho nơi gọi.
        internal void SetSaveService(SaveService saveService)
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
        // EN: Reports readiness without exposing the mutable save model.
        // VI: Báo trạng thái sẵn sàng mà không lộ model lưu có thể sửa.
        public bool IsInitialized => Data != null;

        // EN: Wires persistence through the internal feature contract.
        // VI: Gắn dịch vụ lưu qua giao diện tính năng nội bộ.
        void IFeatureSave.SetSaveService(SaveService saveService)
        {
            SetSaveService(saveService);
        }
    }
    internal interface IFeatureSave
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