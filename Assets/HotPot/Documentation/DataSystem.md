# DataSystem — PlayerDataManager

Quy ước triển khai hiện hành (03/11/2026): xem [Mandatory implementation simplicity](../../../AGENTS.md#mandatory-implementation-simplicity). Áp dụng bảo đảm manager luôn tồn tại, cache tham chiếu dùng chung, binding bằng vòng đời/sự kiện và xử lý lưu offline đơn giản; không tự thêm cơ chế phòng vệ khi chưa có yêu cầu cụ thể.

Đối chiếu mã nguồn ngày 02/11/2026. Tài liệu mô tả trạng thái hiện tại, gồm thay đổi API tài nguyên và sửa thứ tự lưu/thống kê ngày 02/11/2026.

## 1. Vai trò và phạm vi

`GamePackages.PlayerDataManager` là class C# thông thường, kế thừa `ContextData<PlayerData>`, không phải `MonoBehaviour`. Class cung cấp API đọc/đổi tài nguyên, thông báo số dư, cập nhật level và một số cờ mua hàng/trạng thái chơi.

Class không tự nạp dữ liệu, không tự khởi tạo hồ sơ và không sở hữu vòng đời Unity. `ProfileManager` tạo instance và gắn dữ liệu cùng dịch vụ lưu. Các manager tính năng xử lý riêng Heart, DailyGift, DailyReward, WinStreak, LavaQuest, MissionControl và TowerRise.

### Các file liên quan

| File (tính từ gốc project) | Trách nhiệm |
| --- | --- |
| `Assets/Packages/Common/PlayerContext/PlayerDataManager.cs` | API thao tác dữ liệu người chơi |
| `Assets/Packages/Common/PlayerContext/PlayerData.cs` | Model hồ sơ, `ResourceGame`, `ResourceType`, dữ liệu một số tính năng |
| `Assets/Packages/Common/PlayerContext/ContextData.cs` | Gắn model/dịch vụ lưu nội bộ, cung cấp Save() và IsInitialized; không còn GetData() |
| `Assets/Packages/Common/PlayerContext/ProfileManager.cs` | Khởi tạo, nạp/lưu và điều phối vòng đời |
| `Assets/Packages/Common/PlayerContext/SaveService.cs` | Serialize JSON vào PlayerPrefs và đọc lại |
| `Assets/Packages/Common/PlayerContext/GameFeatureContext.cs` | Gắn model con và dependency cho các manager tính năng |
| `Assets/Packages/Common/RecordManager.cs` | `RecordTrackingManager` và số liệu thống kê |
| `Assets/Packages/HeartSystem/HeartManager.cs` | Quản lý mạng và thời gian mạng vô hạn |
| `Assets/Packages/MissionControl/Scripts/MissionControlManager.cs` | Tiến độ nhiệm vụ, gồm nhiệm vụ nhận Coin |

## 2. Khởi tạo và lưu/đọc

Luồng khởi tạo từ `ProfileManager.Awake()`:

1. `LoadData()` gọi `SaveService.Load()`.
2. Nếu chưa có key `PLAYER_DATA`: tạo `PlayerData`, gọi `OnInit()`, lưu ngay và trả về model.
3. Nếu đã có key: đọc JSON bằng `JsonUtility.FromJson<PlayerData>()`, bổ sung các model con còn null (gồm MissionControlData); tài nguyên được chuẩn hóa tại bước gắn PlayerDataManager.
4. Tạo `SaveService(PlayerData)` giữ cùng tham chiếu model vừa nạp.
5. Gọi TimeManager.LoadData(), rồi PlayerDataManager.SetSaveService(...) và SetContext(PlayerData). SetContext chuẩn hóa tài nguyên, tạo dictionary riêng và lưu hồ sơ đã sửa nếu dữ liệu không hợp lệ.
6. Tạo GameFeatureContext(this, PlayerData, SaveService) qua constructor internal; gắn dữ liệu con và các dịch vụ dùng chung; sau đó đăng ký/kiểm tra ngày mới và gắn cấu hình liên quan.

`PlayerDataManager.Save()` được kế thừa từ `ContextData<T>`:

```text
PlayerDataManager.Save()
  → SaveService.SaveData()
  → JsonUtility.ToJson(toàn bộ PlayerData)
  → PlayerPrefs.SetString("PLAYER_DATA", json)
  → PlayerPrefs.Save()
```

Mỗi lần lưu serialize toàn bộ hồ sơ, không chỉ tài nguyên vừa đổi. Giao dịch mua booster/hồi đầy mạng gom mọi thay đổi vào một lần lưu; không có debounce theo thời gian. SaveService chỉ thử ghi và log cảnh báo nếu lỗi; không giữ JSON cũ hoặc hoàn tác dữ liệu. Nếu chưa gắn `SaveService`, `Save()` chỉ ghi warning; thay đổi trong bộ nhớ không được lưu qua lời gọi đó.

Phân biệt `PlayerDataManager.Save()` với `ProfileManager.SaveData()`: hàm của ProfileManager còn yêu cầu `AwaiterActionManager` (nếu có) và `TimeManager` lưu dữ liệu của chúng. ProfileManager gọi lưu khi pause và quit; khi resume gọi `ReloadData()` để đồng bộ thời gian Heart và nạp AwaiterActionManager, không đọc lại toàn bộ `PLAYER_DATA`.

### Hồ sơ mới

`PlayerData.OnInit()` đặt level 1, tên dạng `User` + số ngẫu nhiên, 1.000 Coin, bốn booster mỗi loại 0, bật âm thanh/nhạc/rung và tạo dữ liệu tính năng. Các cờ không được gán riêng giữ mặc định của C#.

Trong `ProfileManager.Start()`, lần mở đầu tiên được đánh dấu hoàn tất rồi gọi `AddUnlimitedLiveTime(30, "first_app_open")`. Đơn vị đầu vào là phút. Nếu `isPlaying` còn true từ phiên trước, ProfileManager đặt lại false, gọi dùng Heart, báo thua cho WinStreak/LavaQuest rồi lưu.

## 3. Model dữ liệu

| Nhóm | Trường tiêu biểu | Cách sử dụng |
| --- | --- | --- |
| Tiến trình | `currentLevel`, `isPlaying`, `isFirstAppOpen` | PlayerDataManager cung cấp một phần API; ProfileManager xử lý lúc khởi động |
| Tài nguyên | `l_ResourceGame : List<ResourceGame>` | Mỗi phần tử chứa `type` và `value` |
| Mua hàng | `isRemoveAds`, `isBoughtStarterPack`, `isInitSaleStarterPack` | Hai cờ đầu có API đặt trong PlayerDataManager |
| Hồ sơ/cài đặt | `playerName`, avatar/frame, các cờ âm thanh, `isSetupProfile` | Tên/avatar/frame có getter giá trị và lệnh cập nhật qua PlayerDataManager; các field còn lại phục vụ model lưu |
| Phần thưởng chờ | PendingCoinAmount | Getter chỉ đọc; AddPendingCoins, ClearPendingCoins và ClaimPendingCoins quản lý thay đổi và lưu |
| Thoát khi chuỗi thua | isLoseStreakQuit | Property get/set (04/11/2026); set chỉ lưu và phát StateChanged khi giá trị đổi |
| Tính năng | DailyGift, DailyReward, RecordTracking, Heart, WinStreak, LavaQuest, MissionControl, TowerRise | Các manager riêng thao tác model con trong cùng hồ sơ |

`ResourceType` hiện có giá trị enum ngầm định theo thứ tự: `Coin = 0`, `UnlimitedLives = 1`, `BoosterPickup = 2`, `BoosterClean = 3`, `BoosterShuffle = 4`, `BoosterAutoComplete = 5`. Cần giữ định danh số ổn định khi thay đổi enum vì chúng thuộc dữ liệu đã serialize.

`UnlimitedLives` tồn tại trong enum nhưng API `AddUnlimitedLiveTime()` đổi `HeartData` qua HeartManager; không cộng vào `l_ResourceGame`. Không dùng số dư tài nguyên này để suy ra thời gian mạng vô hạn thực tế.

## 4. API tài nguyên chuyên biệt

OnAddResource và OnRemoveResource là **private**; caller bên ngoài dùng API nghiệp vụ:

| API public | Hành vi |
| --- | --- |
| OnAddCoin(amount, placement) | Cộng Coin, ghi lượng nhận thực tế, lưu, phát callback số dư, observer rồi nhiệm vụ nhận Coin nếu manager tồn tại |
| OnRemoveCoin(amount, placement) → bool | Chỉ trừ đủ amount; trả false nếu thiếu xu/dependency; lỗi lưu không hủy khoản chi; thành công lưu thống kê tổng/trong màn trước callback |
| OnAddBooster(type, amount, placement) | Chỉ nhận Pickup/Clean/Shuffle/AutoComplete; cộng và ghi lượng nhận thực tế, lưu rồi callback |
| OnRemoveBooster(type, amount, placement) → bool | Chỉ nhận bốn booster; trừ đủ hoặc không đổi, lưu lượng dùng tổng/trong màn trước callback |
| TryBuyBooster(type, amount, coinCost, placement) → bool | Kiểm tra đủ Coin và sức chứa cả gói, đổi Coin/booster cùng thống kê trong một lần lưu |
| TryBuyFullHearts(coinCost, placement) → bool | Trừ Coin, hồi đầy HeartData và ghi thống kê trong một lần lưu; không mua khi mạng đã đầy |
| AddUnlimitedLiveTime(value, placement) | Cộng số phút qua HeartManager rồi lưu; không phát callback tài nguyên |
| GetResourceValue(type) | Tra dictionary riêng theo type với chi phí trung bình O(1); thiếu trả 0 |
| IsEnoughResource(type, amout) | Chỉ trả true nếu yêu cầu không âm và số dư đủ; không giữ chỗ hoặc trừ |
| GetRemainBooster() | Tổng số lượt bốn loại booster |

API cộng/trừ từ chối số lượng âm bằng ArgumentOutOfRangeException. Loại không phải booster bị từ chối trong API booster. Yêu cầu trừ 0 trả true nếu dịch vụ sẵn sàng nhưng không lưu/phát callback; cộng 0 cũng không lưu/phát callback. Cộng giới hạn số dư tại int.MaxValue; thống kê dùng lượng thực nhận. Trừ vượt số dư bị từ chối hoàn toàn: có 100 Coin, trừ 900 trả false, vẫn giữ 100 Coin, không đổi thống kê, không lưu hoặc thông báo.

API cộng Coin/booster vẫn yêu cầu RecordManager và model thống kê sẵn sàng, thiếu thì ném InvalidOperationException. API trừ/mua trả false khi PlayerData, RecordManager hoặc SaveService chưa sẵn sàng. Các hàm tài nguyên nội bộ chỉ đổi model. Trừ/mua cập nhật trực tiếp số dư và thống kê, rồi thử lưu một lần; không chụp trạng thái hoặc hoàn tác. Bool báo kết quả nghiệp vụ (đủ điều kiện và đã áp dụng), không xác nhận dữ liệu đã ghi xuống đĩa:

~~~text
Kiểm tra loại/dependency/số dư/sức chứa → đổi số dư và thống kê → thử Save() một lần → callback(số dư mới)
Nếu ghi lỗi: SaveService log cảnh báo, giữ nguyên trạng thái gameplay, vẫn callback và trả true.
~~~

Callback tài nguyên chạy sau lưu và được bắt lỗi riêng từng listener; listener lỗi không ngăn listener tiếp theo hoặc biến giao dịch đã chốt thành false. Observer/nhiệm vụ trong đường nhận Coin độc lập vẫn dùng cơ chế gọi hiện có. RecordTrackingManager tự nó không gọi lưu; trách nhiệm lưu thuộc wrapper.

RegisterResourceChange ghép delegate theo type, không gửi số dư ban đầu; RemoveResourceChange gỡ delegate nhưng giữ key rỗng. Đăng ký trùng vẫn chạy lặp. UI cần đọc số dư ban đầu và gỡ đúng delegate khi đóng. Listener thuộc instance, không serialize.

OnPurchaseCoinSuccessAction vẫn chạy cho mọi OnAddCoin có lượng tăng thực tế, kể cả thưởng không phải mua hàng. placement giữ trong chữ ký API để tương thích; SDK tracking đang comment nên chưa có tác dụng runtime.

## 5. Trạng thái người chơi và mua hàng

| API | Hành vi |
| --- | --- |
| OnBoughtRemoveAds() | Đặt isRemoveAds, lưu; lệnh ẩn banner/observer đang comment |
| OnBoughtStarterPack() | Đặt isBoughtStarterPack, lưu; observer đang comment |
| OnBoughtRevivePack() | Hiện không có hành động runtime |
| IncreaseLevel() | Tăng level và lưu, không giới hạn tối đa |
| DecreaseLevel() | Giảm level, chặn tối thiểu 1 rồi lưu |
| SetPlaying() / ResetStatePlaying() | Đặt isPlaying true/false rồi lưu |
| IsLastPlayingOnQuit() | Đọc isPlaying, không tự kiểm tra quit |
| GetCurrentLevel() | Đọc currentLevel; `LevelController.Start` dùng giá trị này để nạp `Resources/Levels/LevelConfig_{n:00}` khi vào game (06/11/2026) |

## 6. Tích hợp gameplay

Booster.Consume() gọi OnRemoveBooster(type, 1, "ingame"), cập nhật số lượt và thống kê tiêu booster tổng/trong màn trong cùng lần lưu. Không gọi RecordBoosterSpend thêm lần nữa cho cùng lượt dùng. Consume trả bool; Shuffle/AutoComplete/Clean không bắt đầu nếu thanh toán thất bại. Pickup truyền settings.Consume vào TryPickDishAt: chỉ thanh toán sau khi batch đã kiểm tra và điền được mọi slot, trước khi bắt đầu bay hoặc gỡ đĩa. Thanh toán false/exception hoàn tác slot của batch; hủy hoặc lấy thất bại trước đó không mất lượt.

CanvasBuyBooster.Purchase() dùng TryBuyBooster(resource, 3, 900, "buy_booster"). Popup khóa bấm lặp trước khi gọi, chỉ đóng khi true; khi false giữ mở và mở lại khả năng thử. Gold là tên hiển thị của Coin. Cả hai số dư và thống kê được lưu trong cùng một JSON/một PlayerPrefs.Save; cả callback Coin và booster đều chạy sau đó. PopupMoreBooster cũ cũng dùng giao dịch này với giá 900 và ba lượt. PopupMoreLives dùng TryBuyFullHearts; các popup revive chỉ tiếp tục/đóng sau khi OnRemoveCoin trả true.

WinStreak, TowerRise và MissionControl cấp thưởng qua API Coin, booster hoặc mạng vô hạn riêng. Nhánh loại không hỗ trợ ghi warning và không cấp thưởng; không còn fallback gọi API tài nguyên chung.

Tham khảo: [Booster](../../../claude/Booster.md), [UI](../../../claude/UI.md).

## 7. Giới hạn còn lại

- Quyền truy cập internal chỉ giới hạn ở cấp assembly; code cùng assembly vẫn có thể gọi API khởi tạo nếu cố tình. Đây là đóng gói API để tránh sửa nhầm, không phải cơ chế bảo mật hoặc chống gian lận.
- Bộ đếm thống kê và phép đổi phút sang giây trong HeartManager chưa có chống tràn riêng.
- SaveService chưa có backup, version schema, try/catch JSON lỗi hoặc fallback nếu deserialize trả null. MissionControlData cùng các model con khác được bù null khi load; SetContext của manager sự kiện yêu cầu model không null. Danh sách null được chuẩn hóa thành rỗng và lưu khi gắn manager, không cấp lại mặc định 1.000 Coin.
- PlayerPrefs lưu cục bộ, chưa có mã hóa, kiểm tra toàn vẹn hoặc cloud; không phải nguồn xác thực giao dịch máy chủ.

## 8. Chuẩn hóa tài nguyên và phạm vi giao dịch (02/11/2026)

- Khi SetContext: bỏ null entry và ResourceType không được enum hiện tại hỗ trợ; giá trị âm về 0. Gộp các entry cùng type bằng tổng số không âm, giới hạn tại int.MaxValue. Giữ thứ tự type xuất hiện hợp lệ đầu tiên; không tự cấp thêm tài nguyên thiếu. Entry được sao chép vào danh sách chuẩn để không giữ alias tới entry đầu vào.
- Dictionary riêng và danh sách JSON cùng trỏ các entry chuẩn. Getter và thao tác số dư dùng dictionary; JSON vẫn dùng l_ResourceGame để giữ định dạng save. Bind lại xóa index cũ; chuẩn hóa lần nữa không đổi thì không lưu thêm. Tổng bốn booster cũng giới hạn tại int.MaxValue.
- Mỗi TryBuyBooster/TryBuyFullHearts là một giao dịch gameplay trong một snapshot JSON. Không còn khoảng giữa lần lưu trừ tiền và lần lưu cấp hàng. Nếu ghi lỗi, SaveService bắt lỗi và ghi cảnh báo; trạng thái RAM và kết quả nghiệp vụ vẫn giữ nguyên. HeartManager khởi tạo HeartData tại chỗ để luôn dùng cùng đối tượng với hồ sơ gốc.
- Đây không phải bảo đảm ổ đĩa chống mọi lỗi phần cứng hoặc JSON hỏng khi tiến trình/thiết bị tắt đúng lúc PlayerPrefs đang ghi. Các giao dịch mua mới phải dùng một method nghiệp vụ gom thay đổi tương tự; không ghép OnRemoveCoin rồi OnAddBooster để mua hàng. Mọi lần lưu hồ sơ dùng cùng cách xử lý lỗi tập trung; không có snapshot/rollback cho giao dịch hoặc bản sao JSON dự phòng.

## 9. Đóng gói model và chuyển caller (02/11/2026)

- ProfileManager.PlayerData và SaveService là field private có SerializeField, giữ nguyên tên field để bảo toàn dữ liệu serialized của Unity. Không có getter public trả model này.
- ContextData<T>.GetData() đã gỡ; UI kiểm tra IsInitialized thay vì nhận model. SetContext, SetSaveService và giao diện gắn dịch vụ lưu thuộc phạm vi internal; SaveService.Load và constructor gắn model cũng internal. GameFeatureContext nhận model/dịch vụ lưu từ ProfileManager trong quá trình khởi tạo, không đọc qua field public.
- Các manager WinStreak, LavaQuest, MissionControl và TowerRise không còn trả Data hoặc alias model public. UI đọc cờ tutorial/info, round, tiến trình và số bot qua getter giá trị. Danh sách claimedIndexes được giữ bên trong TowerRiseManager; UI đánh dấu qua MarkMilestoneClaimed.
- CompleteTutorial/MarkInfoShown/MarkTutorialShown lưu cờ trước khi thông báo; các manager sự kiện có StateChanged (WinStreak dùng onDataChanged đang có). Gọi lại khi đã đánh dấu không lưu/thông báo lần nữa.
- PlayerDataManager cung cấp PlayerName, AvatarId, FrameId, HasRemoveAds, PendingCoinAmount; lệnh SetPlayerName, SetAvatar, SetFrame, SetCurrentLevel, AddPendingCoins, ClearPendingCoins tự lưu và phát StateChanged sau lưu. Các lệnh mua hàng/đổi level/trạng thái chơi hiện có cũng phát StateChanged.
- ClaimPendingCoins chuyển phần xu thực tế có thể nhận từ pending sang Coin, ghi thống kê và lưu cả hai số dư một lần trước callback. Gọi lại khi hết pending không cấp thêm; nếu ví chạm int.MaxValue thì phần chưa nhận vẫn ở pending.
- Đã chuyển PopupProfile/ProfileHome, popup thắng/HomePanel, IAP, các UI sự kiện và popup mua booster sang truy vấn/lệnh cụ thể. Kiểm tra sẵn sàng dùng IsInitialized. ProfileManager vẫn sở hữu model riêng để điều phối nạp và vòng đời; JSON và định danh ResourceType không đổi.
- Khi bổ sung UI mới: chỉ đọc scalar hoặc snapshot bất biến; không thêm getter trả model/list đang dùng. Đổi dữ liệu qua method nghiệp vụ của manager sở hữu dữ liệu đó. Chưa cần snapshot vì caller hiện tại chỉ cần giá trị đơn lẻ.

## 10. Kiểm chứng ngày 02/11/2026

- Rà toàn bộ file C# trong Assets: không còn caller bên ngoài gọi OnAddResource/OnRemoveResource.
- Các kiểm tra ở tmp/ResourceApiValidation thuộc phiên bản trước khi đổi sang chi đủ/trả bool; không dùng kết quả cũ để xác nhận hợp đồng giao dịch mới.
- Unity đã refresh script và biên dịch/thực thi thành công lệnh gắn delegate vào OnRemoveBooster mà không đổi dữ liệu người chơi. Chưa chạy lại toàn bộ gameplay/Play Mode, migration hoặc lỗi đĩa. Kết quả Play Mode ghi trước đó trong tài liệu booster thuộc lượt kiểm tra cũ.

### Kiểm tra sau đóng gói model

- Unity biên dịch và thực thi thành công kiểm tra hợp đồng public: không có GetData/SetContext/SetSaveService public trên các manager đã kiểm tra, không có property public trả model người chơi/sự kiện, ProfileManager không có field PlayerData public; IsInitialized của instance chưa gắn context trả false. Console không có Error/Exception tại lần kiểm tra.
- Kiểm tra đóng gói bằng harness cũ đã pass ở phiên bản trước giao dịch. Các kiểm tra mới bên dưới thay thế phần xác minh chi tiêu/mua hàng.
- Chưa chạy lại toàn bộ UI/profile/tutorial/reward trong Play Mode; chưa kiểm tra migration hoặc lỗi lưu trên thiết bị.

### Kiểm tra phiên bản giao dịch có rollback (lịch sử, 02/11/2026)

- tmp/ResourceTransactionValidation liên kết trực tiếp PlayerDataManager, ContextData, SaveService, RecordTrackingManager, HeartManager và DishPickBatch thật; giả lập Unity/PlayerPrefs trong bộ nhớ, không đụng hồ sơ thật.
- PASS 69 assertions: chi đủ/thiếu Coin và booster, số lượng 0/âm/sai loại, biên 899/900 cho cả bốn booster, sức chứa cả gói, một lần lưu và callback sau đầy đủ trạng thái, gọi mua lặp khi thiếu tiền, lưu/đọc lại, lỗi lưu có/không key cũ, hoàn tác entry mới/index/thống kê/JSON đệm, null/âm/trùng/type lạ và chuẩn hóa lặp, rebind index, tổng booster lớn, listener lỗi, mua mạng/hoàn tác/giữ cùng HeartData, Pickup không thu phí trước khi đủ slot và hoàn tác slot khi thanh toán thất bại.
- Chưa chạy lại toàn bộ UI và gameplay trong Play Mode hoặc kiểm tra tắt thiết bị thật lúc ghi PlayerPrefs.
- Unity đã biên dịch/thực thi kiểm tra chữ ký bool của OnRemoveCoin/OnRemoveBooster và TryBuyBooster; thao tác trên manager chưa khởi tạo trả false. Console không có Error/Exception tại lần kiểm tra cuối, không thay đổi hồ sơ thật.

### Hợp đồng tồn tại manager (02/11/2026)

ProfileManager.instance và PlayerDataManager được người dùng bảo đảm luôn tồn tại tại các điểm gọi runtime. Code truy cập trực tiếp hai manager, không kiểm tra null hoặc dùng toán tử ?. cho chúng. Vẫn kiểm tra IsInitialized, feature context/RecordManager/SaveService khi cần, số dư, sức chứa và lỗi lưu. CanvasBuyBooster không truy cập runtime trong Edit Mode; cờ isObservingCoin chỉ theo dõi đăng ký sự kiện, không kiểm tra sự tồn tại của manager. Các kiểm tra cũ về thiếu ProfileManager không còn là nhánh được hỗ trợ theo hợp đồng mới.

## 11. Đơn giản hóa lưu offline (03/11/2026)

Theo yêu cầu người dùng, bỏ snapshot số dư/thống kê, các hàm CaptureResourceTracking/RestoreResourceTracking, rollback trạng thái mạng và khôi phục JSON cũ. Manager cập nhật trực tiếp qua API nghiệp vụ, gọi SaveResourceChanges để thử lưu một lần rồi phát sự kiện. SaveService.SaveData bắt lỗi serialize/PlayerPrefs và chỉ log cảnh báo, không ném lỗi cho gameplay.

OnRemoveCoin/OnRemoveBooster/TryBuyBooster/TryBuyFullHearts trả true khi nghiệp vụ đã áp dụng, kể cả lần ghi thất bại. Không tự gọi lại giao dịch để thử lưu vì sẽ áp dụng thêm lần nữa nếu vẫn đủ tài nguyên; lần SaveData thông thường sau đó có thể ghi trạng thái RAM hiện tại. Người dùng chấp nhận mất phần tiến trình chưa ghi được nếu thoát game trước một lần lưu thành công.

Vẫn giữ trừ đủ hoặc không trừ, kiểm tra đầu vào/sức chứa, gộp mua hàng trong một lần thử lưu, dictionary và chuẩn hóa dữ liệu. Hoàn tác slot của DishPickBatch khi nhóm/slot/thanh toán nghiệp vụ không hợp lệ vẫn giữ vì thuộc gameplay, không phải snapshot lưu trữ.

Đã chạy lại harness mã nguồn thật với Unity/PlayerPrefs giả lập: PASS 67 assertions, gồm lỗi ghi không chặn mua/tiêu booster/hồi mạng, giữ số dư và thống kê mới, vẫn thông báo, lần lưu sau ghi được trạng thái hiện tại, cùng hồi quy chuẩn hóa và ranh giới Pickup. Không chạm PlayerPrefs thật; chưa chạy lại toàn bộ Play Mode.

Cập nhật 03/11/2026 — vòng đời CoinBinding: CanvasBuyBooster đăng ký và đọc số dư trong OnEnable, gỡ đúng listener trong OnDisable. Đã bỏ Update và lời gọi binding trong Show; lúc đang mở chỉ nhận sự kiện đổi Coin, không polling hoặc tự đổi nguồn. Nếu dữ liệu chưa khởi tạo thì bật lại popup mới thử nối lại. Đã rà mã luồng Open bật GameObject và cleanup OnDisable; chưa chạy lại Play Mode cho thay đổi này.

Cập nhật 03/11/2026: CanvasBuyBooster cache PlayerDataManager vào field playerData tại OnEnable. Đăng ký/đọc Coin, mua hàng và gỡ listener đều dùng cùng field này; không lấy lại manager trong Purchase hoặc helper binding. Edit Mode không truy cập ProfileManager. Đã rà tham chiếu và diff; chưa chạy lại Play Mode.

## Cài đặt Sound/Music/Vibration (10/11/2026)

PlayerDataManager đọc IsSoundOn, IsMusicOn và isVibrateOn từ các bool hiện có của PlayerData. SetSoundEnabled/SetMusicEnabled/SetVibrationEnabled bỏ qua giá trị không đổi; khi đổi gọi SaveStateChange đúng một lần rồi phát StateChanged. CanvasPause và PlayerSettingsController cache cùng manager khi bật, đọc giá trị ban đầu và theo dõi sự kiện; không polling. Lỗi ghi vẫn xử lý tập trung tại SaveService theo hợp đồng offline. Đã xác nhận Play Mode các nút cập nhật trạng thái và bool được ghi vào PLAYER_DATA; cài đặt được trả về giá trị trước kiểm tra. Thành viên PlayerDataManager được sắp lại theo property/event → field → public API → helper; kiểm tra giữ nguyên đầy đủ các block khi di chuyển.
