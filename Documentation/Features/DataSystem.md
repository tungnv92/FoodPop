# DataSystem — PlayerDataManager

Quy ước bắt buộc: [AGENTS.md — Mandatory implementation simplicity](../../AGENTS.md#mandatory-implementation-simplicity). `ProfileManager.instance` và `PlayerDataManager` luôn tồn tại tại điểm gọi runtime — truy cập trực tiếp, không kiểm tra null/`?.`. Edit Mode không truy cập runtime.

## Thành phần
`GamePackages.PlayerDataManager` là dịch vụ core C# độc lập (không kế thừa lớp feature, không phải MonoBehaviour), cung cấp API tài nguyên, level, cờ mua hàng/cài đặt và sự kiện.

| File (`Assets/Packages/...`) | Trách nhiệm |
| --- | --- |
| `Common/PlayerContext/PlayerDataManager.cs` | API dữ liệu người chơi |
| `Common/PlayerContext/PlayerData.cs` | Model hồ sơ, `ResourceGame`, `ResourceType` |
| `Common/PlayerContext/ShopData.cs` | Nhóm trạng thái mua hàng trong PlayerData.shopData |
| `Common/PlayerContext/UserSettingData.cs` | Nhóm âm thanh/nhạc/rung trong PlayerData.userSettingData |
| `Common/PlayerContext/ProfileManager.cs` | Khởi tạo, nạp/lưu, vòng đời |
| `Common/PlayerContext/SaveService.cs` | JSON ↔ PlayerPrefs key `PLAYER_DATA` |
| `FeatureModules/` (package riêng) | Hệ tính năng module mới, xem [FeatureModules.md](FeatureModules.md) |
| `RecordTracking/Runtime/` | Module thống kê (`IRecordTracking`, lưu trong `PlayerData.features`) |
| `HeartSystem/Runtime/HeartFeature.cs` | Mạng và mạng vô hạn (module, truy cập qua `IHeart`) |

MissionControl, TowerRise, CardCollection đã tạm gỡ khỏi dự án; core không còn model hay callback của ba tính năng. DailyGift, DailyReward, UserProfile, Heart, LavaQuest đã chuyển sang hệ module (lưu trong `PlayerData.features`), xem [FeatureModules.md](FeatureModules.md). WinStreak cũng đã gỡ khỏi dự án (xem FeatureModules.md).

## Khởi tạo và lưu
0. `ProfileManager` có `[DefaultExecutionOrder(-10000)]` (thấp nhất dự án; EventSystem là -1000) nên `Awake`/`OnEnable`/`Start` của nó chạy trước mọi script khác trong cùng scene, tránh `ProfileManager.instance` / `PlayerDataManager` / feature còn null khi các UI như PopupHome, `UIHome_*` khởi tạo. Không đặt thêm giá trị trong Project Settings > Script Execution Order (giá trị đặt tay ở đó sẽ ghi đè attribute).
1. `ProfileManager.Awake` → `InitializeProfile()` → `RestoreCoreData()` → `SaveService.Load()`: chưa có key thì tạo `PlayerData.OnInit()` (level 1, 1.000 Coin, booster 0, bật âm thanh/nhạc/rung) và lưu; có key thì đọc JSON và bù nhóm `shopData`, `userSettingData` và danh sách `features` khi null.
2. `PlayerDataManager.SetContext` chuẩn hóa tài nguyên (bỏ null/type lạ, âm → 0, gộp trùng, giới hạn `int.MaxValue`) và lưu nếu đã sửa; `FeatureRegistry.InitializeFeatures` tạo mọi module và cache `IRecordTracking` tùy chọn vào PlayerDataManager trước khi chạy `OnInit`. Không còn GameFeatureContext hoặc ContextData.
3. `ProfileManager.Start` chờ Internet và giờ máy chủ đã xác thực (hoặc bật ProfileManager.TimeManager.cheatTime) (TimeManager tự kiểm tra ngày thưởng local cố định ngay trong `Synchronize()` trước khi kết nối báo sẵn sàng) → cập nhật bù thời gian cho feature → xử lý phiên chơi → ghi tracking mở app → `SaveData()` → bật `IsReady` cho Load/Home/gameplay. Lần mở đầu cộng trực tiếp 30 phút qua `IHeart` và đổi `isFirstAppOpen` trước lần lưu cuối khởi động, không lưu cờ trước phần thưởng. Nếu `isPlaying` còn true từ phiên trước: xóa cờ, trừ mạng qua `IHeart`, reset win streak qua `IRecordTracking` (không tăng retry), báo thua module qua `FeatureRegistry.OnLevelLose`. Thiếu Heart/Tracking vẫn hoàn tất khởi động. Chi tiết kết nối và đồng hồ xem [FeatureModules.md](FeatureModules.md).
4. `Save()` ghi JSON của từng tính năng module vào entry trong `PlayerData.features`, rồi serialize toàn bộ hồ sơ → `PlayerPrefs.Save()`. `ProfileManager.SaveData()` còn lưu TimeManager; gọi khi pause/quit. Đã gỡ AwaiterActionManager và object Awaiter trong Load; tiếp tục app không nạp lại bộ đếm phụ, InternetConnection xác thực lại giờ khi resume. Kiểm chứng sau khi gỡ: không còn tham chiếu code/scene/prefab/asset tới các kiểu Awaiter hoặc GUID script; `git diff --check` qua. Log Unity sau biên dịch không có lỗi C#; lệnh xác nhận trạng thái qua Unity MCP chưa trả kết quả, chưa kiểm thử Play Mode.

**Tên và trách nhiệm hàm:** `InitializeProfile` điều phối nạp core, khởi tạo feature và kết nối; `RestoreCoreData` chỉ khôi phục core/clock và gắn PlayerDataManager; `ApplySessionChanges` gọi `GrantFirstOpenGift` rồi `RecoverInterruptedLevel`. `HandleNewDay` là callback reset/lưu ngày. Callback Unity giữ theo thứ tự vòng đời; helper nằm sau public API, theo thứ tự được gọi lần đầu. API đổi level là `SetCurrentLevel`; nút xóa là `DeleteSavedData` vì chỉ xóa khóa đã lưu, không reset model đang chạy. Các field serialize, khóa lưu và hành vi giữ nguyên.

**Ngày mới:** `ProfileManager.HandleNewDay` reset core → chạy hết hook feature → lưu toàn bộ `PLAYER_DATA`; sau khi callback trả về, TimeManager mới lưu mốc ngày. Không phụ thuộc RecordTracking hoặc LavaQuest để lưu hộ. Hook ngày mới của hai module này không tự lưu; LavaQuest vẫn tự lưu ở các thao tác gameplay và `CheckExpire()` ngoài hook ngày mới.

**Lưu offline đơn giản:** áp dụng thay đổi đã kiểm tra → thử lưu một lần → phát sự kiện. `SaveService.SaveData` bắt lỗi và log cảnh báo; không snapshot, rollback hay giữ JSON cũ. Mỗi giao dịch mua gom mọi thay đổi vào một lần lưu; không ghép `OnRemoveCoin` + `OnAddBooster` để mua.

## Model
- `ResourceType` (giá trị số cố định): Coin 0, UnlimitedLives 1, BoosterPickup 2, BoosterClean 3, BoosterShuffle 4, BoosterAutoComplete 5. Mạng vô hạn thực tế nằm trong `HeartData`, không trong `l_ResourceGame`.
- ProfileManager đã bỏ ba field debug/giới hạn cũ cùng API giới hạn level không có nơi gọi; GetCurrentLevel chỉ đọc tiến trình đã lưu. Cheat thời gian nằm riêng trong TimeManager.
- `PlayerData.shopData` chứa `isRemoveAds`, `isBoughtStarterPack`, `isInitSaleStarterPack` (mặc định false). Ba cờ không còn nằm trực tiếp ở root PlayerData.
- `PlayerData.userSettingData` chứa `isSoundOn`, `isMusicOn`, `isVibrateOn` (hồ sơ mới mặc định true). Các lệnh cài đặt và mua hàng lưu hai nhóm trong cùng JSON PLAYER_DATA, phát sự kiện qua PlayerDataManager như cũ. Không thêm chuyển đổi field root cũ trong runtime theo quy ước game mới.
- API hồ sơ/cài đặt giữ nguyên: `HasRemoveAds`, `IsSoundOn`, `IsMusicOn`, `isVibrateOn`. Tên, avatar, khung đã chuyển sang package UserProfile (`IUserProfile`), xem [FeatureModules.md](FeatureModules.md).
- Không có getter public trả model/list; UI đọc giá trị đơn và đổi qua method nghiệp vụ. Lệnh đổi trạng thái tự lưu rồi phát `StateChanged`.

Thống kê Coin/booster đi qua `IRecordTracking` đã cache; các lệnh thống kê chỉ đổi model, giao dịch tài nguyên lưu số dư và entry thống kê cùng một lần. Thiếu package RecordTracking thì vẫn nhận/chi/mua bình thường, chỉ bỏ thống kê. `PlayerData` không giữ model thống kê riêng.

## API tài nguyên
| API | Hành vi |
| --- | --- |
| `OnAddCoin(amount, placement)` | Cộng, ghi thống kê, lưu, callback |
| `OnRemoveCoin(amount, placement)` → bool | Trừ đủ hoặc không trừ |
| `OnAddBooster` / `OnRemoveBooster(type, amount, placement)` | Chỉ 4 loại booster; trừ đủ hoặc không |
| `TryBuyBooster(type, amount, coinCost, placement)` | Kiểm tra Coin và sức chứa cả gói, một lần lưu |
| `TryBuyFullHearts(coinCost, placement)` | Trừ Coin, hồi đầy mạng; không mua khi đầy |
| `AddUnlimitedLiveTime(minutes, placement)` | Qua `IHeart`; bỏ qua khi không có package HeartSystem. `TryBuyFullHearts` trả false khi không có package |
| `GetResourceValue`, `IsEnoughResource`, `GetRemainBooster` | Đọc |
| `RegisterResourceChange` / `RemoveResourceChange` | Không gửi số dư ban đầu — UI tự đọc trong `OnEnable`, gỡ trong `OnDisable` |
| `SetSoundEnabled` / `SetMusicEnabled` / `SetVibrationEnabled` | Bỏ qua nếu không đổi; lưu một lần, phát `StateChanged` |
| `IncreaseLevel`, `SetCurrentLevel`, `GetCurrentLevel`, `SetPlaying`, `ResetStatePlaying` | Tiến trình |

Số âm → `ArgumentOutOfRangeException`. Bool trả về là kết quả nghiệp vụ, không xác nhận đã ghi đĩa. Listener lỗi không chặn listener khác.

## Tích hợp gameplay
- Đã bỏ xu chờ nhận ở Home và cờ thoát-thua cũ trong PlayerData/PlayerDataManager; xóa API thu xu chờ và các hàm gán/xóa cờ ở LevelController. Thưởng thắng được cộng trực tiếp bởi CanvasWin qua OnAddCoin; bộ đếm win_streak trong RecordTracking vẫn giữ.
- `Booster.Consume()` → `OnRemoveBooster(type, 1, "ingame")`; booster không chạy nếu trả false. Pickup chỉ thanh toán sau khi batch giữ đủ slot; lỗi thanh toán nhả slot.
- `CanvasBuyBooster` cache `PlayerDataManager` trong `OnEnable`, dùng cùng tham chiếu cho hiển thị Coin, mua (`TryBuyBooster(resource, 3, 900, "buy_booster")`) và gỡ listener.
- `LevelController.Start` nạp `Resources/Levels/LevelConfig_{n:00}` theo `GetCurrentLevel()`.
- Cài đặt âm thanh/rung áp dụng qua `PlayerSettingsController`, xem [UI.md](UI.md#âm-thanh-và-rung).

## Giới hạn
PlayerPrefs cục bộ, không mã hóa, không cloud, không version schema; chưa chống tràn bộ đếm thống kê.

**Kiểm chứng:** sau thống nhất FeatureRegistry ngày 20/11, 32 kiểm tra mã nguồn thật với engine/PlayerPrefs giả lập PASS (module, giao dịch, lỗi ghi, giữ số liệu, thiếu tracking và thứ tự OnInit); Unity compile khi gỡ/trả package và Play Mode lookup/lưu entry thống kê đã qua. Harness trước đó với PlayerPrefs giả lập PASS (giao dịch, lỗi ghi, chuẩn hóa); cài đặt Sound/Music/Vibration đã kiểm tra Play Mode. Chưa chạy lại toàn bộ UI/profile trong Play Mode hay trên thiết bị.

Kiểm chứng luồng ProfileManager sau chỉnh sửa (20/11/2026): 24 kiểm tra hồ sơ và 33 kiểm tra giao dịch/thống kê qua code production, engine/clock/network/storage giả lập đều PASS. Bao gồm chờ xác thực, quà lần đầu cùng cờ trong một lần ghi PLAYER_DATA, không nhận lại khi mở app, xử lý lượt bỏ dở có/không Heart/Tracking, reset ngày lưu đủ DailyGift/DailyReward khi thiếu Tracking/LavaQuest, Heart không tạo timer ở OnInit và bù giờ sau resume, LavaQuest giữ lượt đang chạy qua ngày và không lưu giữa các hook. Harness: `tmp/ProfileFlowValidation/`, `tmp/TrackingValidation/`. Unity xác nhận bản cuối `Compiling=False`, `compilationFailed=False`; `git diff --check` qua. Chưa kiểm thử UI/Play Mode hoặc thiết bị thật cho thay đổi này.

Kiểm chứng sau sắp xếp/đổi tên hàm (20/11/2026): 24 kiểm tra luồng hồ sơ, 30 clock, 12 kết nối và 33 giao dịch/thống kê đều PASS trên code thật với engine/storage giả lập; build harness sau đổi tên public API thành công, `git diff --check` qua. Các phép di chuyển member được đối chiếu tập dòng không trống trước/sau; không thay đổi hành vi. Lệnh kiểm tra Unity MCP chưa trả kết quả nên chưa xác nhận compile Unity của bản refactor cuối. Chưa chạy Play Mode hoặc thiết bị thật cho lần refactor này.

Kiểm chứng nhóm dữ liệu shop/cài đặt: 40 kiểm tra giao dịch/thống kê và 45 kiểm tra hồ sơ/feature PASS với code production và engine/storage giả lập. Gồm mặc định hai nhóm, JSON lồng không còn field root, lưu/mở lại đủ ba trạng thái shop và ba cài đặt, một lần lưu/sự kiện mỗi thay đổi, và khôi phục nhóm null. Scene Load/HotPotDemo đã đổi cấu trúc serialize, giữ giá trị từng cờ. Các phép sắp thứ tự helper được đối chiếu tập dòng trước/sau; chưa kiểm thử Inspector/Play Mode trong Unity.
