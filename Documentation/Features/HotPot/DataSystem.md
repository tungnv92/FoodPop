# DataSystem — PlayerDataManager

Quy ước bắt buộc: [AGENTS.md — Mandatory implementation simplicity](../../../AGENTS.md#mandatory-implementation-simplicity). `ProfileManager.instance` và `PlayerDataManager` luôn tồn tại tại điểm gọi runtime — truy cập trực tiếp, không kiểm tra null/`?.`. Edit Mode không truy cập runtime.

## Thành phần
`GamePackages.PlayerDataManager` là class C# (kế thừa `ContextData<PlayerData>`, không phải MonoBehaviour), cung cấp API tài nguyên, level, cờ mua hàng/cài đặt và sự kiện.

| File (`Assets/Packages/...`) | Trách nhiệm |
| --- | --- |
| `Common/PlayerContext/PlayerDataManager.cs` | API dữ liệu người chơi |
| `Common/PlayerContext/PlayerData.cs` | Model hồ sơ, `ResourceGame`, `ResourceType` |
| `Common/PlayerContext/ContextData.cs` | Gắn model/dịch vụ lưu (internal), `Save()`, `IsInitialized` |
| `Common/PlayerContext/ProfileManager.cs` | Khởi tạo, nạp/lưu, vòng đời |
| `Common/PlayerContext/SaveService.cs` | JSON ↔ PlayerPrefs key `PLAYER_DATA` |
| `Common/PlayerContext/GameFeatureContext.cs` | Gắn model con cho manager tính năng |
| `Common/RecordManager.cs` | `RecordTrackingManager`, thống kê |
| `HeartSystem/HeartManager.cs` | Mạng và mạng vô hạn |
| `MissionControl/Scripts/MissionControlManager.cs` | Nhiệm vụ |

Heart, DailyGift, DailyReward, WinStreak, LavaQuest, MissionControl, TowerRise do manager riêng xử lý trên model con.

## Khởi tạo và lưu
1. `ProfileManager.Awake` → `SaveService.Load()`: chưa có key thì tạo `PlayerData.OnInit()` (level 1, 1.000 Coin, booster 0, bật âm thanh/nhạc/rung) và lưu; có key thì đọc JSON và bù model con null.
2. `PlayerDataManager.SetContext` chuẩn hóa tài nguyên (bỏ null/type lạ, âm → 0, gộp trùng, giới hạn `int.MaxValue`) và lưu nếu đã sửa; tạo `GameFeatureContext`.
3. `ProfileManager.Start`: lần mở đầu cộng 30 phút mạng vô hạn; nếu `isPlaying` còn true từ phiên trước thì trừ mạng, báo thua WinStreak/LavaQuest rồi lưu.
4. `Save()` serialize toàn bộ hồ sơ → `PlayerPrefs.Save()`. `ProfileManager.SaveData()` còn lưu TimeManager/AwaiterActionManager; gọi khi pause/quit.

**Lưu offline đơn giản:** áp dụng thay đổi đã kiểm tra → thử lưu một lần → phát sự kiện. `SaveService.SaveData` bắt lỗi và log cảnh báo; không snapshot, rollback hay giữ JSON cũ. Mỗi giao dịch mua gom mọi thay đổi vào một lần lưu; không ghép `OnRemoveCoin` + `OnAddBooster` để mua.

## Model
- `ResourceType` (giá trị số cố định): Coin 0, UnlimitedLives 1, BoosterPickup 2, BoosterClean 3, BoosterShuffle 4, BoosterAutoComplete 5. Mạng vô hạn thực tế nằm trong `HeartData`, không trong `l_ResourceGame`.
- Hồ sơ/cài đặt: `PlayerName`, `AvatarId`, `FrameId`, `HasRemoveAds`, `IsSoundOn`, `IsMusicOn`, `isVibrateOn`, `PendingCoinAmount`, `isLoseStreakQuit`.
- Không có getter public trả model/list; UI đọc giá trị đơn và đổi qua method nghiệp vụ. Lệnh đổi trạng thái tự lưu rồi phát `StateChanged`.

## API tài nguyên
| API | Hành vi |
| --- | --- |
| `OnAddCoin(amount, placement)` | Cộng, ghi thống kê, lưu, callback |
| `OnRemoveCoin(amount, placement)` → bool | Trừ đủ hoặc không trừ |
| `OnAddBooster` / `OnRemoveBooster(type, amount, placement)` | Chỉ 4 loại booster; trừ đủ hoặc không |
| `TryBuyBooster(type, amount, coinCost, placement)` | Kiểm tra Coin và sức chứa cả gói, một lần lưu |
| `TryBuyFullHearts(coinCost, placement)` | Trừ Coin, hồi đầy mạng; không mua khi đầy |
| `AddUnlimitedLiveTime(minutes, placement)` | Qua HeartManager |
| `AddPendingCoins` / `ClaimPendingCoins` / `ClearPendingCoins` | Xu chờ nhận |
| `GetResourceValue`, `IsEnoughResource`, `GetRemainBooster` | Đọc |
| `RegisterResourceChange` / `RemoveResourceChange` | Không gửi số dư ban đầu — UI tự đọc trong `OnEnable`, gỡ trong `OnDisable` |
| `SetSoundEnabled` / `SetMusicEnabled` / `SetVibrationEnabled` | Bỏ qua nếu không đổi; lưu một lần, phát `StateChanged` |
| `IncreaseLevel`, `SetCurrentLevel`, `GetCurrentLevel`, `SetPlaying`, `ResetStatePlaying` | Tiến trình |

Số âm → `ArgumentOutOfRangeException`. Bool trả về là kết quả nghiệp vụ, không xác nhận đã ghi đĩa. Listener lỗi không chặn listener khác.

## Tích hợp gameplay
- `Booster.Consume()` → `OnRemoveBooster(type, 1, "ingame")`; booster không chạy nếu trả false. Pickup chỉ thanh toán sau khi batch giữ đủ slot; lỗi thanh toán nhả slot.
- `CanvasBuyBooster` cache `PlayerDataManager` trong `OnEnable`, dùng cùng tham chiếu cho hiển thị Coin, mua (`TryBuyBooster(resource, 3, 900, "buy_booster")`) và gỡ listener.
- `LevelController.Start` nạp `Resources/Levels/LevelConfig_{n:00}` theo `GetCurrentLevel()`.
- Cài đặt âm thanh/rung áp dụng qua `PlayerSettingsController`, xem [UI.md](../../Handoff/UI.md#âm-thanh-và-rung).

## Giới hạn
PlayerPrefs cục bộ, không mã hóa, không cloud, không version schema; chưa chống tràn bộ đếm thống kê.

**Kiểm chứng:** harness mã nguồn thật với PlayerPrefs giả lập PASS (giao dịch, lỗi ghi, chuẩn hóa); cài đặt Sound/Music/Vibration đã kiểm tra Play Mode. Chưa chạy lại toàn bộ UI/profile trong Play Mode hay trên thiết bị.
