using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace GamePackages
{
    public class PlayerDataManager
    {
        // EN: Signals persisted changes to player settings, ownership and level progress.
        // VI: Báo thay đổi cài đặt, quyền sở hữu và tiến trình level sau khi lưu.
        public event System.Action StateChanged;
        // EN: Reads remove-ads ownership.
        // VI: Đọc quyền sở hữu gói gỡ quảng cáo.
        public bool HasRemoveAds => Data.shopData.isRemoveAds;
        // EN: Whether the starter pack has been bought.
        // VI: Người chơi đã mua gói khởi đầu hay chưa.
        public bool HasStarterPack => Data.shopData.isBoughtStarterPack;
        // EN: Reads the saved vibration preference used by gameplay haptics.
        // VI: Đọc cài đặt rung đã lưu dùng cho phản hồi gameplay.
        public bool isVibrateOn => Data.userSettingData.isVibrateOn;
        // EN: Reads whether sound effects are enabled in the saved profile.
        // VI: Đọc trạng thái bật hiệu ứng âm thanh trong hồ sơ đã lưu.
        public bool IsSoundOn => Data.userSettingData.isSoundOn;
        // EN: Reads whether background music is enabled in the saved profile.
        // VI: Đọc trạng thái bật nhạc nền trong hồ sơ đã lưu.
        public bool IsMusicOn => Data.userSettingData.isMusicOn;
        // EN: Reports core data readiness without exposing the save model.
        // VI: Báo dữ liệu core đã sẵn sàng mà không lộ model lưu.
        public bool IsInitialized => Data != null;

        private PlayerData Data;
        private SaveService SaveService;
        private IRecordTracking recordTracking;
        private readonly Dictionary<ResourceType, ResourceGame> resourceIndex = new();
        Dictionary<ResourceType, UnityAction<int>> dic_OnResourceChange = new Dictionary<ResourceType, UnityAction<int>>();

        // EN: Binds the core save service once during profile initialization.
        // VI: Gắn dịch vụ lưu core một lần khi khởi tạo hồ sơ.
        internal void SetSaveService(SaveService saveService)
        {
            SaveService = saveService;
        }

        // EN: Binds the core player model, normalizes resources and builds its lookup before module initialization.
        // VI: Gắn model người chơi core, chuẩn hóa tài nguyên và tạo bảng tra cứu trước khi khởi tạo module.
        internal void SetContext(PlayerData data)
        {
            Data = data ?? throw new System.ArgumentNullException(nameof(data));
            bool repaired = NormalizeResources();
            if (repaired && SaveService != null) SaveService.SaveData();
        }
        // EN: Caches optional statistics after every module is registered; gameplay does not require the tracking package.
        // VI: Cache thống kê tùy chọn sau khi đăng ký mọi module; gameplay không bắt buộc package thống kê.
        internal void SetTracking(IRecordTracking tracking)
        {
            recordTracking = tracking;
        }

        // EN: Commits the shared profile after callers change feature state directly, preserving the existing core save command.
        // VI: Lưu hồ sơ dùng chung sau khi nơi gọi đổi trạng thái tính năng trực tiếp, giữ lệnh lưu core hiện có.
        public void Save()
        {
            SaveService.SaveData();
        }

        // EN: Persists a changed sound preference once and notifies interested views.
        // VI: Lưu một lần khi cài đặt âm thanh đổi và báo cho giao diện theo dõi.
        public void SetSoundEnabled(bool enabled)
        {
            if (Data.userSettingData.isSoundOn == enabled) return;
            Data.userSettingData.isSoundOn = enabled;
            SaveStateChange();
        }

        // EN: Persists a changed music preference once and notifies interested views.
        // VI: Lưu một lần khi cài đặt nhạc đổi và báo cho giao diện theo dõi.
        public void SetMusicEnabled(bool enabled)
        {
            if (Data.userSettingData.isMusicOn == enabled) return;
            Data.userSettingData.isMusicOn = enabled;
            SaveStateChange();
        }

        // EN: Persists a changed vibration preference once and notifies interested views.
        // VI: Lưu một lần khi cài đặt rung đổi và báo cho giao diện theo dõi.
        public void SetVibrationEnabled(bool enabled)
        {
            if (Data.userSettingData.isVibrateOn == enabled) return;
            Data.userSettingData.isVibrateOn = enabled;
            SaveStateChange();
        }

        // EN: Sets a valid level and persists the progress change.
        // VI: Đặt level hợp lệ và lưu thay đổi tiến trình.
        public void SetCurrentLevel(int level)
        {
            if (level < 1) throw new System.ArgumentOutOfRangeException(nameof(level));
            if (Data.currentLevel == level) return;
            Data.currentLevel = level;
            SaveStateChange();
        }

        // EN: Reads a normalized resource balance through the private index; missing resources return zero.
        // VI: Đọc số dư đã chuẩn hóa qua bảng tra cứu riêng; tài nguyên thiếu trả về không.
        public int GetResourceValue(ResourceType type)
        {
            return resourceIndex.TryGetValue(type, out var resource) ? resource.value : 0;
        }

        // EN: Checks a nonnegative requirement without changing the balance.
        // VI: Kiểm tra yêu cầu không âm mà không đổi số dư.
        public bool IsEnoughResource(ResourceType type, int amout)
        {
            int value = GetResourceValue(type);
            return amout >= 0 && value >= amout;
        }
        public void RegisterResourceChange(ResourceType type, UnityAction<int> action)
        {
            if (dic_OnResourceChange.ContainsKey(type))
            {
                dic_OnResourceChange[type] += action;
            } else dic_OnResourceChange.Add(type, action);
        }
        public void RemoveResourceChange(ResourceType type, UnityAction<int> action)
        {
            if (dic_OnResourceChange.ContainsKey(type))
            {
                dic_OnResourceChange[type] -= action;
            }
        }

        // EN: Adds coins and saves the actual gain with optional tracking before notifying listeners.
        // VI: Cộng xu và lưu lượng tăng thực tế cùng thống kê tùy chọn trước khi thông báo listener.
        public void OnAddCoin(int _amount, string placement = "")
        {
            int added = OnAddResource(ResourceType.Coin, _amount);
            if (added == 0) return;
            recordTracking?.RecordCoinEarn(added);
            SaveResourceChanges(ResourceType.Coin);
            //if (!Application.isEditor) SDKBridge.TrackEventEarnCurrency(GetCurrentLevel(), _amount, ResourceType.Coin.ToString(), placement);
            Observer.OnPurchaseCoinSuccessAction?.Invoke();
        }

        // EN: Spends the full coin amount with optional tracking and attempts one save; false means insufficient funds or unavailable data, not a save failure.
        // VI: Chi đủ số xu với thống kê tùy chọn và thử lưu một lần; false nghĩa là thiếu tiền hoặc dữ liệu, không phải lỗi lưu.
        public bool OnRemoveCoin(int _amount, string placement = "")
        {
            if (_amount < 0) throw new System.ArgumentOutOfRangeException(nameof(_amount));
            if (!IsInitialized || SaveService == null || !IsEnoughResource(ResourceType.Coin, _amount)) return false;
            if (_amount == 0) return true;
            OnRemoveResource(ResourceType.Coin, _amount);
            recordTracking?.RecordCoinSpend(_amount);
            SaveResourceChanges(ResourceType.Coin);
            return true;
        }

        // EN: Adds nonnegative unlimited-life minutes through IHeart and saves; ignored when the HeartSystem package is absent.
        // VI: Cộng số phút mạng vô hạn không âm qua IHeart và lưu; bỏ qua khi không có package HeartSystem.
        public void AddUnlimitedLiveTime(int value, string placement = "")
        {
            if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
            IHeart heart = ProfileManager.instance.Features.Get<IHeart>();
            if (value == 0 || heart == null) return;
            heart.AddUnlimitedLiveTime(value);
            SaveService.SaveData();
            //SDKBridge.TrackEventEarnCurrency(GetCurrentLevel(), value, ResourceType.UnlimitedLives.ToString(), placement);
        }

        // EN: Adds a supported booster and saves actual gain with optional tracking before notifications.
        // VI: Cộng booster hợp lệ và lưu lượng nhận thực tế cùng thống kê tùy chọn trước khi thông báo.
        public void OnAddBooster(ResourceType type, int _amount, string placement = "")
        {
            ValidateBoosterType(type);
            int added = OnAddResource(type, _amount);
            if (added == 0) return;
            recordTracking?.RecordBoosterEarn(added);
            SaveResourceChanges(type);
            //SDKBridge.TrackEventEarnBooster(type.ToString(), _amount, placement, GetCurrentLevel());
        }
        // EN: Spends the full booster amount, optionally records usage and attempts one save without rollback.
        // VI: Chi đủ lượt booster, ghi thống kê nếu có và thử lưu một lần, không hoàn tác.
        public bool OnRemoveBooster(ResourceType type, int _amount, string placement = "")
        {
            ValidateBoosterType(type);
            if (_amount < 0) throw new System.ArgumentOutOfRangeException(nameof(_amount));
            if (!IsInitialized || SaveService == null || !IsEnoughResource(type, _amount)) return false;
            if (_amount == 0) return true;
            OnRemoveResource(type, _amount);
            recordTracking?.RecordBoosterSpend(_amount);
            SaveResourceChanges(type);
            return true;
        }

        // EN: Exchanges coins for the full booster offer, updates optional tracking and attempts one save before notifications.
        // VI: Đổi xu lấy đủ gói booster, cập nhật thống kê nếu có và thử lưu một lần trước thông báo.
        public bool TryBuyBooster(ResourceType type, int amount, int coinCost, string placement = "")
        {
            ValidateBoosterType(type);
            if (amount <= 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
            if (coinCost < 0) throw new System.ArgumentOutOfRangeException(nameof(coinCost));
            if (!IsInitialized || SaveService == null ||
                !IsEnoughResource(ResourceType.Coin, coinCost) || GetResourceValue(type) > int.MaxValue - amount) return false;
            OnRemoveResource(ResourceType.Coin, coinCost);
            OnAddResource(type, amount);
            recordTracking?.RecordCoinSpend(coinCost);
            recordTracking?.RecordBoosterEarn(amount);
            SaveResourceChanges(ResourceType.Coin, type);
            return true;
        }

        // EN: Deducts coins, fills hearts and updates optional tracking before one save attempt; gameplay continues on save errors.
        // VI: Trừ xu, hồi đầy mạng và cập nhật thống kê nếu có trước một lần thử lưu; gameplay tiếp tục khi lưu lỗi.
        public bool TryBuyFullHearts(int coinCost, string placement = "")
        {
            if (coinCost < 0) throw new System.ArgumentOutOfRangeException(nameof(coinCost));
            IHeart heart = ProfileManager.instance.Features.Get<IHeart>();
            if (!IsInitialized || heart == null ||
                SaveService == null || !IsEnoughResource(ResourceType.Coin, coinCost) || heart.IsMaxHeart()) return false;
            OnRemoveResource(ResourceType.Coin, coinCost);
            heart.FillFullHeart();
            recordTracking?.RecordCoinSpend(coinCost);
            SaveResourceChanges(ResourceType.Coin);
            return true;
        }

        // EN: Persists remove-ads ownership and notifies state listeners.
        // VI: Lưu quyền sở hữu gói gỡ quảng cáo và thông báo listener trạng thái.
        public void OnBoughtRemoveAds()
        {
            Data.shopData.isRemoveAds = true;
            //SDKBridge.HideBanner();
            SaveStateChange();
            Observer.OnBoughtRemoveAdsAction?.Invoke();
        }
        // EN: Persists starter-pack ownership and notifies state listeners.
        // VI: Lưu quyền sở hữu gói khởi đầu và thông báo listener trạng thái.
        public void OnBoughtStarterPack()
        {
            Data.shopData.isBoughtStarterPack = true;
            SaveStateChange();
            Observer.OnBoughtStarterPackAction?.Invoke();
        }
        public void OnBoughtRevivePack()
        {
            //Observer.OnBoughtRevivePackAction?.Invoke();
        }
        // EN: Increments the level, saves progress and notifies state listeners.
        // VI: Tăng level, lưu tiến trình và thông báo listener trạng thái.
        public void IncreaseLevel()
        {
            Data.currentLevel++;
            SaveStateChange();
        }
        // EN: Decrements the level with a minimum of one, saves and notifies state listeners.
        // VI: Giảm level với giới hạn tối thiểu một, lưu và thông báo listener trạng thái.
        public void DecreaseLevel()
        {
            Data.currentLevel--;
            if (Data.currentLevel <= 1) Data.currentLevel = 1;
            SaveStateChange();
        }
        // EN: Clears the playing flag, saves and notifies state listeners.
        // VI: Xóa cờ đang chơi, lưu và thông báo listener trạng thái.
        public void ResetStatePlaying()
        {
            Data.isPlaying = false;
            SaveStateChange();
        }
        // EN: Persists the playing flag and notifies state listeners.
        // VI: Lưu cờ đang chơi và thông báo listener trạng thái.
        public void SetPlaying()
        {
            Data.isPlaying = true;
            SaveStateChange();
        }
        public bool IsLastPlayingOnQuit()
        {
            return Data.isPlaying;
        }
        // EN: Totals normalized booster balances with saturation so large repaired balances cannot produce a negative total.
        // VI: Cộng số dư booster đã chuẩn hóa có giới hạn để số dư lớn sau sửa không làm tổng thành số âm.
        public int GetRemainBooster()
        {
            long total = (long)GetResourceValue(ResourceType.BoosterPickup) + GetResourceValue(ResourceType.BoosterShuffle) +
                GetResourceValue(ResourceType.BoosterClean) + GetResourceValue(ResourceType.BoosterAutoComplete);
            return (int)System.Math.Min(int.MaxValue, total);
        }

        public int GetCurrentLevel()
        {
            return Data.currentLevel;
        }

        // EN: Drops null/unknown entries, clamps negatives and merges duplicate balances with saturation in first-seen order.
        // VI: Bỏ phần tử null/loại lạ, chặn số âm và gộp số dư trùng có giới hạn theo thứ tự xuất hiện đầu tiên.
        private bool NormalizeResources()
        {
            resourceIndex.Clear();
            var normalized = new List<ResourceGame>();
            bool changed = Data.l_ResourceGame == null;
            if (Data.l_ResourceGame != null)
            {
                foreach (var entry in Data.l_ResourceGame)
                {
                    if (entry == null || !System.Enum.IsDefined(typeof(ResourceType), entry.type))
                    {
                        changed = true;
                        continue;
                    }
                    int value = System.Math.Max(0, entry.value);
                    if (entry.value < 0) changed = true;
                    if (resourceIndex.TryGetValue(entry.type, out var existing))
                    {
                        existing.value = (int)System.Math.Min(int.MaxValue, (long)existing.value + value);
                        changed = true;
                    }
                    else
                    {
                        var resource = new ResourceGame { type = entry.type, value = value };
                        normalized.Add(resource);
                        resourceIndex.Add(entry.type, resource);
                    }
                }
            }
            Data.l_ResourceGame = normalized;
            return changed;
        }

        // EN: Saves a core state change through SaveService before notifying interested views.
        // VI: Lưu thay đổi trạng thái core qua SaveService trước khi báo giao diện theo dõi.
        private void SaveStateChange()
        {
            SaveService.SaveData();
            StateChanged?.Invoke();
        }

        // EN: Adds internally with an integer balance cap; the owning transaction handles persistence and notifications.
        // VI: Cộng nội bộ có giới hạn số nguyên; giao dịch sở hữu thao tác xử lý lưu và thông báo.
        private int OnAddResource(ResourceType type, int amount)
        {
            if (amount < 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return 0;
            if (!resourceIndex.TryGetValue(type, out var target))
            {
                target = new ResourceGame { type = type };
                resourceIndex.Add(type, target);
                Data.l_ResourceGame.Add(target);
            }
            int added = System.Math.Min(amount, int.MaxValue - target.value);
            target.value += added;
            return added;
        }

        // EN: Uses core SaveService once for all completed changes, then notifies affected resources regardless of save errors.
        // VI: Dùng SaveService core một lần cho mọi thay đổi đã hoàn tất, rồi báo tài nguyên dù có lỗi lưu.
        private void SaveResourceChanges(params ResourceType[] changedTypes)
        {
            SaveService.SaveData();
            foreach (var type in changedTypes) NotifyResourceChange(type);
        }

        // EN: Notifies each listener after commit; presentation failures cannot turn a committed purchase into a failed result.
        // VI: Thông báo từng listener sau khi chốt; lỗi hiển thị không biến giao dịch đã lưu thành kết quả thất bại.
        private void NotifyResourceChange(ResourceType type)
        {
            if (!dic_OnResourceChange.TryGetValue(type, out var callback) || callback == null) return;
            int value = GetResourceValue(type);
            foreach (var listener in callback.GetInvocationList())
            {
                try { ((UnityAction<int>)listener).Invoke(value); }
                catch (System.Exception exception) { Debug.LogException(exception); }
            }
        }


        // EN: Removes exactly the requested amount internally or leaves the balance unchanged when insufficient.
        // VI: Trừ nội bộ đúng số lượng yêu cầu hoặc giữ nguyên số dư khi không đủ.
        private bool OnRemoveResource(ResourceType type, int amount)
        {
            if (amount < 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
            if (GetResourceValue(type) < amount) return false;
            if (amount > 0) resourceIndex[type].value -= amount;
            return true;
        }

        // EN: Rejects non-booster types in dedicated booster operations.
        // VI: Từ chối loại không phải booster trong thao tác booster chuyên biệt.
        private static void ValidateBoosterType(ResourceType type)
        {
            if (type != ResourceType.BoosterPickup && type != ResourceType.BoosterClean &&
                type != ResourceType.BoosterShuffle && type != ResourceType.BoosterAutoComplete)
                throw new System.ArgumentOutOfRangeException(nameof(type));
        }


    }
}
