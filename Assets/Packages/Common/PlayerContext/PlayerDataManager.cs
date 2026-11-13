using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace GamePackages
{
    public class PlayerDataManager : ContextData<PlayerData>
    {
        // EN: Signals persisted changes to player profile, progress and pending rewards.
        // VI: Báo thay đổi hồ sơ, tiến trình và thưởng chờ sau khi lưu.
        public event System.Action StateChanged;
        // EN: Reads profile values without returning the live model.
        // VI: Đọc giá trị hồ sơ mà không trả model đang dùng.
        public string PlayerName => Data.playerName;
        // EN: Reads the selected avatar identifier.
        // VI: Đọc mã ảnh đại diện đã chọn.
        public int AvatarId => Data.avatar_id;
        // EN: Reads the selected frame identifier.
        // VI: Đọc mã khung đại diện đã chọn.
        public int FrameId => Data.frame_avatar_id;
        // EN: Reads remove-ads ownership.
        // VI: Đọc quyền sở hữu gói gỡ quảng cáo.
        public bool HasRemoveAds => Data.isRemoveAds;
        // EN: Reads coins awaiting collection without exposing their storage.
        // VI: Đọc xu chờ nhận mà không lộ nơi lưu dữ liệu.
        public int PendingCoinAmount => Data.PendingCoinAmount;
        // EN: Reads the saved vibration preference used by gameplay haptics.
        // VI: Đọc cài đặt rung đã lưu dùng cho phản hồi gameplay.
        public bool isVibrateOn => Data.isVibrateOn;
        // EN: Reads whether sound effects are enabled in the saved profile.
        // VI: Đọc trạng thái bật hiệu ứng âm thanh trong hồ sơ đã lưu.
        public bool IsSoundOn => Data.isSoundOn;
        // EN: Reads whether background music is enabled in the saved profile.
        // VI: Đọc trạng thái bật nhạc nền trong hồ sơ đã lưu.
        public bool IsMusicOn => Data.isMusicOn;
        // EN: Reads or persists whether the player quit a level during a lose streak; saving and StateChanged happen only when the value changes.
        // VI: Đọc hoặc lưu trạng thái người chơi thoát màn khi đang chuỗi thua; chỉ lưu và phát StateChanged khi giá trị thay đổi.
        public bool isLoseStreakQuit
        {
            get => Data.isLoseStreakQuit;
            set
            {
                if (Data.isLoseStreakQuit == value) return;
                Data.isLoseStreakQuit = value;
                SaveStateChange();
            }
        }
        private readonly Dictionary<ResourceType, ResourceGame> resourceIndex = new();
        Dictionary<ResourceType, UnityAction<int>> dic_OnResourceChange = new Dictionary<ResourceType, UnityAction<int>>();

        // EN: Persists a changed sound preference once and notifies interested views.
        // VI: Lưu một lần khi cài đặt âm thanh đổi và báo cho giao diện theo dõi.
        public void SetSoundEnabled(bool enabled)
        {
            if (Data.isSoundOn == enabled) return;
            Data.isSoundOn = enabled;
            SaveStateChange();
        }

        // EN: Persists a changed music preference once and notifies interested views.
        // VI: Lưu một lần khi cài đặt nhạc đổi và báo cho giao diện theo dõi.
        public void SetMusicEnabled(bool enabled)
        {
            if (Data.isMusicOn == enabled) return;
            Data.isMusicOn = enabled;
            SaveStateChange();
        }

        // EN: Persists a changed vibration preference once and notifies interested views.
        // VI: Lưu một lần khi cài đặt rung đổi và báo cho giao diện theo dõi.
        public void SetVibrationEnabled(bool enabled)
        {
            if (Data.isVibrateOn == enabled) return;
            Data.isVibrateOn = enabled;
            SaveStateChange();
        }

        // EN: Updates and persists the profile name.
        // VI: Cập nhật và lưu tên hồ sơ.
        public void SetPlayerName(string value)
        {
            if (string.IsNullOrEmpty(value) || Data.playerName == value) return;
            Data.playerName = value;
            SaveStateChange();
        }

        // EN: Updates and persists a nonnegative avatar identifier.
        // VI: Cập nhật và lưu mã ảnh đại diện không âm.
        public void SetAvatar(int value)
        {
            if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
            if (Data.avatar_id == value) return;
            Data.avatar_id = value;
            SaveStateChange();
        }

        // EN: Updates and persists a nonnegative frame identifier.
        // VI: Cập nhật và lưu mã khung đại diện không âm.
        public void SetFrame(int value)
        {
            if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
            if (Data.frame_avatar_id == value) return;
            Data.frame_avatar_id = value;
            SaveStateChange();
        }

        // EN: Adds pending coins with overflow validation and persists the new pending balance.
        // VI: Cộng xu chờ nhận có kiểm tra tràn số và lưu số dư chờ mới.
        public void AddPendingCoins(int amount)
        {
            if (amount < 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;
            Data.PendingCoinAmount = checked(Data.PendingCoinAmount + amount);
            SaveStateChange();
        }

        // EN: Clears pending coins and persists the change.
        // VI: Xóa xu chờ nhận và lưu thay đổi.
        public void ClearPendingCoins()
        {
            if (Data.PendingCoinAmount == 0) return;
            Data.PendingCoinAmount = 0;
            SaveStateChange();
        }

        // EN: Transfers pending coins into the wallet and saves both balances with tracking before notifications.
        // VI: Chuyển xu chờ nhận vào ví và lưu cả hai số dư cùng thống kê trước khi thông báo.
        public void ClaimPendingCoins(string placement = "ingame_win")
        {
            int pending = Data.PendingCoinAmount;
            if (pending <= 0) return;
            var record = RequireRecordManager();
            int added = OnAddResource(ResourceType.Coin, pending);
            if (added == 0) return;
            Data.PendingCoinAmount -= added;
            record.RecordCoinEarn(added);
            SaveResourceChanges(ResourceType.Coin);
            StateChanged?.Invoke();
            Observer.OnPurchaseCoinSuccessAction?.Invoke();
            MissionControlManager.Ins?.OnEarnCoin(added);
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

        // EN: Normalizes saved resources and builds a unique constant-time index during internal binding.
        // VI: Chuẩn hóa tài nguyên lưu và tạo bảng tra cứu duy nhất thời gian hằng khi gắn nội bộ.
        internal override void SetContext(PlayerData data)
        {
            base.SetContext(data ?? throw new System.ArgumentNullException(nameof(data)));
            bool repaired = NormalizeResources();
            if (repaired && SaveService != null) Save();
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

        // EN: Adds coins and saves the actual gain with tracking before notifying listeners and missions.
        // VI: Cộng xu và lưu lượng tăng thực tế cùng thống kê trước khi thông báo listener và nhiệm vụ.
        public void OnAddCoin(int _amount, string placement = "")
        {
            var record = RequireRecordManager();
            int added = OnAddResource(ResourceType.Coin, _amount);
            if (added == 0) return;
            record.RecordCoinEarn(added);
            SaveResourceChanges(ResourceType.Coin);
            //if (!Application.isEditor) SDKBridge.TrackEventEarnCurrency(GetCurrentLevel(), _amount, ResourceType.Coin.ToString(), placement);
            Observer.OnPurchaseCoinSuccessAction?.Invoke();
            MissionControlManager.Ins?.OnEarnCoin(added);
        }

        // EN: Spends the full coin amount and attempts one save; false means insufficient funds or unavailable data, not a save failure.
        // VI: Chi đủ số xu và thử lưu một lần; false nghĩa là thiếu tiền hoặc dữ liệu, không phải lỗi lưu.
        public bool OnRemoveCoin(int _amount, string placement = "")
        {
            if (_amount < 0) throw new System.ArgumentOutOfRangeException(nameof(_amount));
            var record = ProfileManager.instance.GameFeatureContext?.RecordManager;
            if (!IsInitialized || record == null || !record.IsInitialized || SaveService == null || !IsEnoughResource(ResourceType.Coin, _amount)) return false;
            if (_amount == 0) return true;
            OnRemoveResource(ResourceType.Coin, _amount);
            record.RecordCoinSpend(_amount);
            SaveResourceChanges(ResourceType.Coin);
            return true;
        }

        // EN: Adds nonnegative unlimited-life minutes through HeartManager and saves the duration.
        // VI: Cộng số phút mạng vô hạn không âm qua HeartManager và lưu thời lượng.
        public void AddUnlimitedLiveTime(int value, string placement = "")
        {
            if (value < 0) throw new System.ArgumentOutOfRangeException(nameof(value));
            if (value == 0) return;
            ProfileManager.instance.GameFeatureContext.HeartManager.AddUnlimitedLiveTime(value);
            Save();
            //SDKBridge.TrackEventEarnCurrency(GetCurrentLevel(), value, ResourceType.UnlimitedLives.ToString(), placement);
        }

        // EN: Adds a supported booster and saves actual gain with tracking before notifications.
        // VI: Cộng booster hợp lệ và lưu lượng nhận thực tế cùng thống kê trước khi thông báo.
        public void OnAddBooster(ResourceType type, int _amount, string placement = "")
        {
            ValidateBoosterType(type);
            var record = RequireRecordManager();
            int added = OnAddResource(type, _amount);
            if (added == 0) return;
            record.RecordBoosterEarn(added);
            SaveResourceChanges(type);
            //SDKBridge.TrackEventEarnBooster(type.ToString(), _amount, placement, GetCurrentLevel());
        }
        // EN: Spends the full booster amount, records usage and attempts one save without rollback.
        // VI: Chi đủ lượt booster, ghi thống kê và thử lưu một lần, không hoàn tác.
        public bool OnRemoveBooster(ResourceType type, int _amount, string placement = "")
        {
            ValidateBoosterType(type);
            if (_amount < 0) throw new System.ArgumentOutOfRangeException(nameof(_amount));
            var record = ProfileManager.instance.GameFeatureContext?.RecordManager;
            if (!IsInitialized || record == null || !record.IsInitialized || SaveService == null || !IsEnoughResource(type, _amount)) return false;
            if (_amount == 0) return true;
            OnRemoveResource(type, _amount);
            record.RecordBoosterSpend(_amount);
            SaveResourceChanges(type);
            return true;
        }

        // EN: Exchanges coins for the full booster offer, updates tracking and attempts one save before notifications.
        // VI: Đổi xu lấy đủ gói booster, cập nhật thống kê và thử lưu một lần trước thông báo.
        public bool TryBuyBooster(ResourceType type, int amount, int coinCost, string placement = "")
        {
            ValidateBoosterType(type);
            if (amount <= 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
            if (coinCost < 0) throw new System.ArgumentOutOfRangeException(nameof(coinCost));
            var record = ProfileManager.instance.GameFeatureContext?.RecordManager;
            if (!IsInitialized || record == null || !record.IsInitialized || SaveService == null ||
                !IsEnoughResource(ResourceType.Coin, coinCost) || GetResourceValue(type) > int.MaxValue - amount) return false;
            OnRemoveResource(ResourceType.Coin, coinCost);
            OnAddResource(type, amount);
            record.RecordCoinSpend(coinCost);
            record.RecordBoosterEarn(amount);
            SaveResourceChanges(ResourceType.Coin, type);
            return true;
        }

        // EN: Deducts coins, fills hearts and updates tracking before one save attempt; gameplay continues on save errors.
        // VI: Trừ xu, hồi đầy mạng và cập nhật thống kê trước một lần thử lưu; gameplay tiếp tục khi lưu lỗi.
        public bool TryBuyFullHearts(int coinCost, string placement = "")
        {
            if (coinCost < 0) throw new System.ArgumentOutOfRangeException(nameof(coinCost));
            var features = ProfileManager.instance.GameFeatureContext;
            var record = features?.RecordManager;
            if (!IsInitialized || Data.HeartData == null || features?.HeartManager?.IsInitialized != true || record?.IsInitialized != true ||
                SaveService == null || !IsEnoughResource(ResourceType.Coin, coinCost) || features.HeartManager.IsMaxHeart()) return false;
            OnRemoveResource(ResourceType.Coin, coinCost);
            features.HeartManager.OnFillFullHeart();
            record.RecordCoinSpend(coinCost);
            SaveResourceChanges(ResourceType.Coin);
            return true;
        }

        // EN: Persists remove-ads ownership and notifies state listeners.
        // VI: Lưu quyền sở hữu gói gỡ quảng cáo và thông báo listener trạng thái.
        public void OnBoughtRemoveAds()
        {
            Data.isRemoveAds = true;
            //SDKBridge.HideBanner();
            //Observer.OnBoughtRemoveAdsAction?.Invoke();
            SaveStateChange();
        }
        // EN: Persists starter-pack ownership and notifies state listeners.
        // VI: Lưu quyền sở hữu gói khởi đầu và thông báo listener trạng thái.
        public void OnBoughtStarterPack()
        {
            Data.isBoughtStarterPack = true;
            //Observer.OnBoughtStarterPackAction?.Invoke();
            SaveStateChange();
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
        // EN: Saves a state change before publishing it to interested views.
        // VI: Lưu thay đổi trạng thái trước khi thông báo cho giao diện theo dõi.
        private void SaveStateChange()
        {
            Save();
            StateChanged?.Invoke();
        }

        // EN: Requires initialized tracking using a readiness query before changing resources.
        // VI: Yêu cầu thống kê đã khởi tạo qua truy vấn sẵn sàng trước khi đổi tài nguyên.
        private RecordTrackingManager RequireRecordManager()
        {
            var record = ProfileManager.instance.GameFeatureContext?.RecordManager;
            if (record == null || !record.IsInitialized)
                throw new System.InvalidOperationException("Player resource tracking is not initialized.");
            return record;
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

        // EN: Attempts one save for all completed changes, then notifies each affected resource regardless of save errors.
        // VI: Thử lưu một lần cho toàn bộ thay đổi đã hoàn tất, rồi thông báo từng tài nguyên dù có lỗi lưu.
        private void SaveResourceChanges(params ResourceType[] changedTypes)
        {
            Save();
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
