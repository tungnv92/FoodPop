# UI màn chơi

Cập nhật: 05/11/2026.

## Cấu trúc và nguyên tắc

- `SafeAreaCamera` (`Assets/HotPot/Scripts/UI/SafeAreaCamera.cs`, đổi tên từ `SaveAreaCamera`, giữ GUID) kiểm tra hai vùng nội dung world XY theo thứ tự: `cameraBounds` (ưu tiên, khung xanh), rồi `secondaryBounds` (tối thiểu, khung vàng). Mỗi lần kiểm tra bắt đầu từ `defaultOrthographicSize` (11.7); nếu vùng đầu vừa thì phóng to (giảm size) để chiều ngang vùng 1 lấp kín màn hình, nhưng không nhỏ hơn size chuẩn quá `maxZoomInPercent` (mặc định 5% → size tối thiểu 11.115); nếu không thì thử vùng hai, vùng hai vừa vẫn giữ size chuẩn. Chỉ khi cả hai không vừa mới tăng size đủ chứa vùng hai. Giữ nguyên toàn bộ camera position, kể cả X = 0, không dịch camera theo bound. Size cần cho mỗi vùng = max(khoảng cách mép xa nhất theo Y, khoảng cách mép xa nhất theo X / aspect). Sai số kiểm tra 0.0001 đơn vị size để tránh nhiễu số thực. Vùng 1: tâm (0,0), rộng 10.797363, cao 23.4, tương ứng 945×2048 và size 11.7, vừa khít ở tỉ lệ khoảng 9:19.5 (aspect 0.4614). Vùng 2 (cả mặc định code và `HotPotDemo.unity`): x −5.2, y −11.7, rộng 10.4, cao 23.4, chứa khung băng chuyền `middle_bar_1a` (±5.01), HUD `ic_coin` (tới −5.20) và `btn_setting` (tới 4.98). Ở màn dọc chỉ chiều ngang quyết định size (nửa cao các vùng không vượt 11.7), nên vùng 2 phải hẹp hơn vùng 1 theo X mới có tác dụng và nên đối xứng quanh camera (camera đứng yên nên mép xa nhất quyết định). Kết quả: aspect > 0.4614 phóng to, size = 5.3987 / aspect, chặn dưới 11.115 từ aspect khoảng 0.4857 (≈ 9:18.5) trở lên, ví dụ 9:16 → 11.115; khi đó cắt bớt trên/dưới (tối đa 0.585 mỗi phía), HUD bám mép đi theo camera, còn hai bên lộ thêm nền ngoài vùng 1. `maxZoomInPercent` chưa được lưu trong `HotPotDemo.unity` nên đang dùng mặc định 5; đặt 0 để tắt phóng to. Aspect đúng 0.4614 giữ 11.7 và thấy đủ vùng 1; 0.4444–0.4614 (khoảng 9:19.5–9:20.25) giữ 11.7, cắt bớt tối đa 0.2 mỗi bên phần trang trí nền; hẹp hơn thì size = 5.2 / aspect (9:21 → 12.13 thay vì 12.60). Lưu ý: `PickupPrompt/CancelButton` lệch phải tới 5.41, có thể bị cắt mép khi rơi vào khoảng cắt; chưa kiểm tra trực quan trên thiết bị. Rect Position là góc trái dưới. Camera quay về size chuẩn khi đổi lại tỉ lệ chứa đủ một vùng. Nền cần phủ khung nhìn mở rộng nếu phải fit vùng hai. HUD tiếp tục xử lý safe area thiết bị riêng. Chạy cả Edit Mode, cập nhật khi viewport, một trong hai vùng, size chuẩn hoặc vị trí camera thay đổi; dành cho camera 2D không xoay. Camera perspective không bị thay đổi.

- `TopPanel` trong `HotPotDemo.unity` dùng `ScreenTopAnchor` (`Assets/HotPot/Scripts/UI/ScreenTopAnchor.cs`) để bám mép trên vùng an toàn của camera. Gán camera scene qua `targetCamera`; `topOffset` là khoảng cách theo đơn vị world xuống dưới mép trên. Component giữ nguyên X/Z và cập nhật trong `LateUpdate`, cả Edit Mode, tương ứng với `ScreenBottomAnchor` ở mép dưới.
- UIManager dùng object UiManager đã có trong scene; không đặt sẵn các popup dưới object này.
- Asset danh sách: `Assets/HotPot/Config/UI/UIRef.asset`, trường `uiCanvasRefList` chứa prefab Win/Lose/Revive/BuyBooster/Pause/BoosterIntro/WarningQuit. Mỗi class đăng ký một prefab.
- Kế thừa: `CanvasBoosterIntro → LevelPopupView → UICanvasPrime; CanvasWin, CanvasLose, CanvasPause, CanvasBuyBooster, CanvasRevive và CanvasWarningQuit → UICanvasPrime trực tiếp`.
- Manager có `uiRefSO` và `uiCanvasParentTrans`; tạo prefab khi Get/Open lần đầu rồi lưu instance theo Type. Close không tạo instance. Mỗi lần mở đóng popup khác.
- UICanvasPrime cung cấp Open/Close/IsOpening, OnOpenCanvas/OnCloseCanvas, onCloseAction và tùy chọn isDestroyOnClose. Đóng trực tiếp cũng thông báo manager để cập nhật trạng thái, nhả pause shop và bỏ instance tự hủy khỏi cache.
- Thiết kế prefab và tham chiếu Canvas/CanvasGroup/card được giữ nguyên. LevelPopupView quản lý callback nút và hoạt cảnh unscaled hiện có; không đưa phụ thuộc âm thanh của project mẫu sang project này.
- ShortMessage tiếp tục là thông báo độc lập trong scene.

## File và prefab

| Thành phần | Vị trí |
| --- | --- |
| Manager scene | `Assets/HotPot/Scripts/UI/UIManager.cs` |
| Popup thắng | `Assets/HotPot/Scripts/UI/CanvasWin.cs` |
| Popup thua | `Assets/HotPot/Scripts/UI/CanvasLose.cs` |
| Popup hồi sinh | `Assets/HotPot/Scripts/UI/CanvasRevive.cs` |
| Popup mua booster | `Assets/HotPot/Scripts/UI/CanvasBuyBooster.cs` |
| Popup booster mở khóa | `Assets/HotPot/Scripts/UI/CanvasBoosterIntro.cs` |
| Lớp phủ dùng thử booster | `Assets/HotPot/Scripts/UI/BoosterTrialOverlay.cs` |
| Canvas cơ sở | `Assets/HotPot/Scripts/UI/UICanvasPrime.cs` |
| Class danh sách prefab | `Assets/HotPot/Scripts/UI/UIRef.cs` |
| Asset danh sách prefab | `Assets/HotPot/Config/UI/UIRef.asset` |
| Lớp cơ sở dùng chung | `Assets/HotPot/Scripts/UI/LevelPopupView.cs` |
| Logic trạng thái màn | `Assets/HotPot/Scripts/gamebase/LevelController.cs` |
| Prefab thắng | `Assets/HotPot/Prefabs/UI/LevelResult/WinPopup.prefab` |
| Prefab thua | `Assets/HotPot/Prefabs/UI/LevelResult/LosePopup.prefab` |
| Prefab hồi sinh | `Assets/HotPot/Prefabs/UI/LevelResult/RevivePopup.prefab` |
| Prefab mua booster | `Assets/HotPot/Prefabs/UI/Booster/BuyBoosterPopup.prefab` |
| Prefab booster mở khóa | `Assets/HotPot/Prefabs/UI/Booster/BoosterIntroPopup.prefab` |
| Prefab lớp phủ dùng thử booster | `Assets/HotPot/Prefabs/UI/Booster/BoosterTrialOverlay.prefab` |
| Sprite dùng chung | `Assets/HotPot/Art/Sprites/UI/LevelResult/` |
| Công cụ gán tham chiếu | `Assets/HotPot/Editor/LevelResultPrefabSetup.cs` |
| Công cụ xem thử | `Assets/HotPot/Editor/LevelResultPreview.cs` |
| Tài liệu chi tiết | `Assets/HotPot/Documentation/LevelResultUI.md` |

## Trách nhiệm và API

### UIManager

- Giữ asset `uiRefSO` chứa bốn prefab, không giữ tham chiếu popup scene. `shortMessage` là thông báo ngắn độc lập, không phải popup chặn thao tác.
- `Instance` tìm manager có sẵn, không tự tạo object quản lý mới.
- `ShowWin(next, hasNextLevel, animate)` mở `CanvasWin`.
- `ShowLose(replay, animate)` mở `CanvasLose`.
- `ShowRevive(revive, decline, available, animate)` mở `CanvasRevive`.
- `ShowBuyBooster(type)` mở đề nghị mua booster theo loại với giá lấy từ BoosterConfig (mặc định 900 gold), trả về `CanvasBuyBooster` trực tiếp; trả `null` khi loại booster không hợp lệ hoặc chưa gán popup.
- `ShowBoosterIntro(icon, title, info, gotIt)` mở popup "booster mới" và trả về `CanvasBoosterIntro`; trả `null` khi popup không chuẩn bị được. `gotIt` chạy khi bấm "Got it". Bên gọi tự đóng popup bằng `CloseUI<CanvasBoosterIntro>()`.
- `ShowShortMessage(message)` hiện phản hồi ngắn, không chặn thao tác. Thông báo của booster lấy từ BoosterConfig `shortDes` (tiếng Anh), không viết cứng trong popup.
- `HideAll()` đóng bốn popup, nhả tạm dừng do cửa hàng giữ, ẩn thông báo ngắn và xóa trạng thái popup đang mở.
- `ActivePopup` và `ActiveCanvas` phục vụ kiểm tra và dựng ảnh xem thử.
- Khi khởi tạo, chuẩn bị EventSystem nếu scene chưa có và ẩn mọi popup. Khi manager bị tắt, đóng mọi popup.

### Các class popup

- `CanvasWin`: nhận hành động Next và trạng thái còn màn kế tiếp.
- `CanvasLose`: nhận hành động chơi lại màn hiện tại.
- `CanvasRevive`: kế thừa trực tiếp UICanvasPrime, nhận hành động hồi sinh/từ chối; có Coin/Watch tùy chọn/nút X, từ chối vẫn khả dụng khi hồi sinh bị khóa.
- `CanvasBuyBooster`: hiển thị booster được chọn, giá đọc từ `IngameConfig.Instance.Boosters.GetPrice(type)` khi `Show(type)` và lưu vào field `price`, xử lý mua thêm ba lượt và đóng qua UIManager.
- `CanvasBoosterIntro`: popup "booster mới" của màn giới thiệu mở khóa. `Show(icon, title, info, gotIt)` điền icon, tên và mô tả rồi mở popup. `TryGetIconScreenRect(out center, out height)` trả tâm và chiều cao icon trên màn hình để icon bay bắt đầu đúng chỗ; `HideIcon()` ẩn icon của popup trước khi đóng.
- `LevelPopupView` là lớp trừu tượng dùng chung cho hoạt cảnh, trạng thái nút và quản lý callback. Không gắn trực tiếp lớp cơ sở này thay cho các class popup cụ thể.
- Các trường Inspector kế thừa gồm Canvas, CanvasGroup, khung nội dung, nút chính/phụ, chữ trạng thái và `unavailableStatus`.
- Hoạt cảnh dùng `Time.unscaledDeltaTime` để vẫn chạy khi gameplay tạm dừng.
- Đóng hoặc mở lại popup phải gỡ callback runtime cũ, tránh một lần bấm chạy nhiều lần. Giữ các sự kiện nút được gán sẵn trong Inspector.
- Giữ chữ, hình và bố cục đã thiết kế. Chỉ cập nhật trạng thái cần thiết, như khóa nút và thông báo chưa thể thực hiện hành động.

## Luồng gameplay

```text
LevelController
    → UIManager.ShowWin / ShowLose / ShowRevive
    → CanvasWin / CanvasLose / CanvasRevive
```

### Thắng

1. `CustomerManager.AllServed` báo đã phục vụ hết khách.
2. `LevelController` chuyển sang `Won`, tạm dừng gameplay và gọi `UIManager.ShowWin()`.
3. Nút **NEXT** gọi `LevelController.NextLevel()` để nạp level số kế tiếp qua luồng đặt lại màn đầy đủ.
4. (11/11/2026) Level nạp theo số từ `Assets/HotPot/Config/Resources/Levels/LevelConfig_NN.asset` bằng `LevelConfig.Load(n)` (`Resources.Load`, tên `LevelConfig_{n:00}`, ví dụ 01, 15, 120). Hiện có 01 → 25. `LevelController.LoadLevel` chọn cấu hình theo `difficultyMode`: Hard và retry >= 2 dùng `LevelConfig_NN_Easy` nếu có; thiếu bản Easy thì dùng cấu hình Hard gốc, giữ nguyên số hiển thị và tiến trình. Runtime không suy ra Hard từ số level; asset 15/20/25 hiện được đánh dấu Hard và đã có ba bản Easy. Replay tăng bộ đếm trước khi nạp; Retry sau thua dùng lượt đã được ghi nhận. Hồi sinh giữ nguyên màn đang chơi. Đã bỏ field `nextLevel` và field `levelConfig` serialize trên `LevelController`.
5. Khi vào game, `LevelController.Start` gọi `LoadSavedLevel()`: nạp level bằng `PlayerDataManager.GetCurrentLevel()`; nếu số đã lưu vượt quá level cuối (đã thắng hết) thì lùi dần tới level cao nhất còn tồn tại. Thắng màn tăng `currentLevel` qua `PlayerDataManager.IncreaseLevel()` trong `TrackingWin`, nên lần mở game sau vào đúng màn kế tiếp.
6. Khi hiện popup thắng, `ShowResult` nạp sẵn `LevelConfig.Load(levelNumber + 1)`; không có file thì vẫn nhận thưởng một lần nhưng không chuyển màn; sau khi nhận, cả hai nút bị khóa.
   `NextLevel()` bỏ tham chiếu level kế tiếp đã nạp sẵn, gọi `LoadLevel` rồi gọi `Resources.UnloadUnusedAssets()` để giải phóng asset của màn vừa xong. Chỉ Next gọi hàm này; chơi lại và lần nạp đầu không gọi. Hàm quét toàn bộ asset nên có thể khựng nhẹ một frame lúc chuyển màn. Chưa đo trên thiết bị.
7. Thêm level mới: tạo bằng Level Editor (tự lưu vào `Config/Resources/Levels` với tên và `levelNumber` đúng số). Tên file và `levelNumber` phải khớp; không cần nối level nào.
8. Kiểm chứng 06/11/2026: Unity biên dịch không lỗi; Play Mode nạp level từ Resources thành công (board và khách hiển thị, không có cảnh báo thiếu LevelConfig). Chưa kiểm tra luồng thắng → Next trong Play Mode.

### Pre-win — tăng tốc cuối màn (09/11/2026)

1. Mỗi frame khi màn ở `Playing` và game không tạm dừng, sau khi kiểm tra thắng và bế tắc, `LevelController.IsPreWin()` so tổng số phần món còn trên board (`Food.TotalAmount` của từng đĩa, đĩa đôi tính một lần, gồm cả đĩa đóng băng/ẩn) với `ChefManager.CountEmptySlots()`. Khi số phần ≤ số slot trống thì vào pre-win.
2. `EnterPreWin()` ghi tốc độ hiện tại rồi đặt `Time.timeScale` = tốc độ đó × `IngameConfig.preWin.speedMultiplier` (mặc định 1.3, tức nhanh hơn 30%). Pre-win giữ nguyên tới hết màn, không tự tắt khi board đổi.
3. Popup tạm dừng (pause, mua booster, hồi sinh, thắng/thua) lưu và trả lại tốc độ 1.3 như bình thường. `LoadLevel` (chơi lại, Next, nạp màn) và `OnDisable` gọi `ExitPreWin()` sau `RestoreTimeScale()` để về tốc độ gốc.
4. Vì tăng `Time.timeScale`, mọi thứ chạy theo thời gian game cùng nhanh hơn 30%: băng chuyền, đầu bếp, hiệu ứng và cả thanh chờ của khách.
5. Màn có board nhỏ hơn số slot trống ngay từ đầu sẽ vào pre-win ngay khi bắt đầu.
5b. Console ghi log khi vào pre-win (level, số phần còn trên board, số slot trống, tốc độ trước → sau) và khi thoát (tốc độ trả về), prefix `[LevelController]`.
6. Chưa kiểm tra Play Mode.

### Bế tắc và hồi sinh

1. (Cập nhật 11/11/2026) Không cần băng đầy tuyệt đối: sau khi ổn định, nếu không món trên băng phục vụ được và không có nhóm đĩa lộ nào lấy được theo DishPickPlan thì mời hồi sinh. Luật lấy N≤S hoặc N=S+1 với đúng một phần có thể qua đầu bếp rảnh tới order. Duyệt đĩa đôi/nhóm nối theo tổng phần thực, không đếm trùng footprint. Hoãn kiểm tra khi chef bận, món đang bay, khách có pending service, board đang dồn/mở nắp/khóa hoặc booster đang chạy/pause. Giữ luồng Revive trước, từ chối mới Lost.
2. `LevelController` vào `ReviveOffered`, tạm dừng gameplay và gọi `UIManager.ShowRevive()`; chưa xác nhận thua hoặc phát `onLost`.
3. **HỒI SINH** gọi `ReviveLevel()`: đóng popup qua `UIManager.HideAll()`, khôi phục thời gian và vào `Reviving`.
4. Dùng lại cơ chế Clean qua `BoosterController.CanCleanForRevive()` / `TryCleanForRevive()`: thu món trên băng vào khay giữ cùng hiệu ứng hiện có, giữ tiến độ khách và nguyên liệu, không trừ lượt booster.
5. Thu xong, nhả khóa thao tác và các cờ tạm dừng, quay về `Playing`. Kho trả lại nguyên liệu theo nhu cầu phục vụ hiện có.
6. Nếu lượt thu bị ngắt hoặc thất bại, mở lại lời mời hồi sinh. Chơi lại hủy lượt thu đang chờ qua cơ chế dọn hiện có.
7. Hiện không đặt giới hạn số lần hồi sinh trong một màn.

### Từ chối và thua

1. **KHÔNG, CẢM ƠN** gọi `DeclineRevive()`.
2. Chỉ lúc này chuyển sang `Lost`, phát `onLost` và gọi `UIManager.ShowLose()`.
3. **CHƠI LẠI** gọi `ReplayLevel()`, đóng popup qua UIManager và nạp lại màn hiện tại.

### Mua booster — bố cục mới (10/11/2026)

- CanvasBuyBooster trực tiếp kế thừa UICanvasPrime giống CanvasPause. TMP_Text nối tên/mô tả/giá/số lượng, Image nối icon, card nối Root. Nút riêng buyButton/watchButton/closeButton. UIManager.ShowBuyBooster trả CanvasBuyBooster, giữ pause đến khi đóng xong.
- BoosterConfig cung cấp GetName/GetInfo/GetPrice. Không ép giá 900: asset hiện tại là 10 cho bốn loại. presentations chỉ giữ icon. TextAmount hiển thị x3.
- Buy Coin nhận đủ 3 lượt (AmountPerPurchase=3), dùng TryBuyBooster như cũ. Thiếu Coin hoặc giao dịch bị từ chối giữ popup và hiện thông báo BoosterConfig. Không tự kích hoạt booster.
- activeAdsBooster trên prefab mặc định false: ẩn Watch và chặn callback, chỉ hiện Coin ở giữa. Bật thì trả vị trí hai nút đã thiết kế, Watch ghi Watch +1 và thưởng riêng AmountPerAd=1, không trừ Coin. Kiểm tra đủ sức chứa toàn bộ thưởng và chặn cấp lặp.
- Theo yêu cầu tạm của người dùng, WatchAds gọi thẳng callback công khai OnWatchAdsSuccess, chưa có SDK/quảng cáo thực. Callback cộng qua OnAddBooster(resource,1,watch_ads_booster) để lưu/thống kê, rồi đóng. Người dùng sẽ tự gán callback SDK sau.
- OnEnable cache cùng PlayerDataManager cho giao dịch/thưởng/Coin, nối ba nút và listener Coin, đọc số dư ban đầu; OnDisable gỡ đúng listener. Thiết kế mới không có ô ví nên goldBalanceLabel là TMP tùy chọn, để trống. HUD UICoin vẫn hiển thị ví. Không polling; Edit Mode không truy cập ProfileManager.
- Ba nút có UIButtonScaleEffect, PlayHaptic/PlaySoundClick theo cài đặt. Mở phóng 88%→gốc/fade 0→1 trong 0.24 giây, đóng thu 90%/fade 0 trong 0.16 giây unscaled. Khóa nhập khi đóng, giữ pause tới hết rồi callback UIManager một lần. Manager Close/HideAll hủy tween và đóng ngay.
- Giữ thiết kế người dùng và đường dẫn Prefabs/UI/Booster/BuyBoosterPopup.prefab, đăng ký UIRef; không dựng lại bằng công cụ cũ.

## Popup booster mở khóa (05/11/2026)

- Prefab `Assets/HotPot/Prefabs/UI/Booster/BoosterIntroPopup.prefab`, đăng ký trong `UIRef.asset`; không đặt sẵn trong scene. Script `CanvasBoosterIntro` kế thừa `LevelPopupView` và chạy theo unscaled time vì màn chơi đang tạm dừng khi popup mở.
- Bố cục từ dưới lên: lớp nền tối (scrim), khung card; phía sau icon có chùm tia sáng xoay, quầng sáng và các ngôi sao lấp lánh; icon lớn nhấp nhô; băng "NEW BOOSTER!"; tên booster; khung thông tin với mô tả; nút "Got it".
- Popup không có dòng level mở khóa. Tên và mô tả lấy từ `boosterName` và `boosterInfo` trong BoosterConfig.
- Luồng: bấm "Got it" → popup gọi `gotIt` → `BoosterUnlockIntro` gọi `HideIcon()`, đóng popup bằng `CloseUI<CanvasBoosterIntro>()` rồi cho icon bay từ vị trí lấy bằng `TryGetIconScreenRect` tới nút booster. Phần bay và lượt dùng thử nằm trong `claude/Booster.md`.
- Tham chiếu Inspector: `boosterName`, `description`, `boosterIcon`, `rays`; tinh chỉnh `raySpeed` (36 độ/giây), `bobHeight`, `bobPeriod`.
- Xuất hiện theo bước (05/11/2026): `Show` gọi `Open(gotIt, null, true, false)` (tắt hoạt cảnh chung 0,34 giây của `LevelPopupView`) rồi chạy `Entrance()` bằng thời gian thực: nền `scrim` mờ dần vào trong `scrimFade` (0,3 giây); khung `card` phóng từ 0 lên đúng cỡ có vượt nhẹ từ `cardDelay` (0,15) trong `cardDuration` (0,45); icon nảy từ 0 từ `iconDelay` (0,45) trong `iconDuration` (0,4). Trong lúc xuất hiện `canvasGroup.interactable` = false nên "Got it" chưa bấm được. Field `scrim` đã gán Image Scrim trong prefab.
- Chưa chạy thử trong Unity: bố cục prefab, nhịp nhấp nhô và điểm bắt đầu của icon bay.

## Lớp phủ dùng thử booster (05/11/2026)

- Script `Assets/HotPot/Scripts/UI/BoosterTrialOverlay.cs`; prefab `Assets/HotPot/Prefabs/UI/Booster/BoosterTrialOverlay.prefab`; sprite `Assets/HotPot/Art/Sprites/UI/FocusHole.png` (ô vuông trắng, tâm tròn trong suốt có mép mờ, mesh full-rect).
- Canvas riêng, Render Mode Screen Space Overlay, `sortingOrder` 32767: cao nhất, nằm trên mọi UI kể cả `BoosterIntroPopup` (30000). CanvasScaler 1080×1920, có GraphicRaycaster. Các Image tấm tối bật raycast nên UI khác không thể chạm lúc dùng thử.
- Nút booster là object trong world; `BoosterController` đọc thao tác qua Input System nên vẫn nhận chạm qua lỗ sáng.
- Cấu trúc:
  - `Dim Top`, `Dim Bottom`, `Dim Left`, `Dim Right`: màu đen, alpha 0,78.
  - `Focus`: ô vuông chứa lỗ sáng; con `Hole` dùng sprite `FocusHole`, cùng màu.
  - `Guide`: nhóm chuẩn 240×240; con `Hand` là SkeletonGraphic (`Hand_SkeletonData`, material `SkeletonGraphicDefault-Straight`, animation `animation` lặp, unscaled time, tắt PMA vertex colors).
  - `Tip`: tùy chọn; prefab hiện đã bỏ node này (`tip` = None) nên không có dòng nhắc. Gán lại một Text vào `tip` nếu muốn hiện "Tap to try!".
- Bố cục: các tấm tối và lỗ dùng anchor chuẩn hóa (0..1), chung mép và được kẹp trong 0..1, nên màu đen phủ kín màn hình trừ hình tròn. Trong prefab, `Dim Top` được kéo kín màn hình để che đủ ngay cả trước lần layout đầu. Layout chạy lại mỗi `LateUpdate`.
- Lỗ tròn khớp nền tròn của nút: đường kính vùng trong suốt = `holePadding` (1,06) × đường kính nền. `Show` nhận bao world của SpriteRenderer nền booster: đường kính = chiều ngang bao, vòng tròn chạm mép trên nên bỏ qua bóng đổ phía dưới. Vùng trong suốt của `FocusHole.png` có bán kính 0,94 nửa cạnh sprite (hằng `HoleRadiusRatio`; mép mờ từ 0,91 tới 0,97), nên ô `Focus` = đường kính × `holePadding` / 0,94. `Guide` co giãn theo `holeSize / guideReferenceSize` (240), nên bàn tay đi theo kích thước nút; chỉnh vị trí và cỡ bàn tay trong prefab ở cỡ tham chiếu 240.
- Field: `dimTop`, `dimBottom`, `dimLeft`, `dimRight`, `focus`, `hole` (Image lỗ sáng, sprite FocusHole, gán sẵn trong prefab), `dimColor` (đen, alpha 0,78; Awake tô màu này cho bốn tấm tối và `hole`), `holePadding` (1,06), `guide`, `guideReferenceSize` (240), `tip`. `BoosterUnlockIntro` gọi `Show(Bounds background, Camera cam, string message)` sau khi tạo prefab để đặt lớp phủ quanh nút.
- Chưa chạy thử trong Unity.

## Setup và chỉnh sửa

1. Giữ object `UiManager` hoạt động trong scene, gắn component `UIManager`.
2. Gán UIRef.asset vào uiRefSO và cha chứa instance vào uiCanvasParentTrans; không đặt bốn popup sẵn trong scene.
3. Gán cùng UIManager vào trường `uiManager` của `LevelController`.
4. Chỉnh thiết kế trong từng prefab; giữ hoặc cập nhật các tham chiếu hiển thị của class popup khi đổi cấu trúc con.
5. `Tools → HotPot → Setup Result Popup References` sửa tham chiếu hiển thị còn thiếu trên prefab, không dựng lại hình hoặc bố cục.
6. `Tools → HotPot → Setup Scene UIManager` dùng lại object manager đã có, gán UIRef và bỏ popup đặt sẵn sau khi lưu chỉnh sửa về prefab, nối controller và lưu scene đang mở.
7. `Tools → HotPot → Preview Win And Lose UI` dựng cả ba trạng thái qua một UIManager xem thử riêng. Ảnh lưu ngoài Assets tại `output/LevelResultUI/win.png`, `lose.png`, `revive.png`.
8. Công cụ `Tools → HotPot → Setup Buy Booster Popup` (`BuyBoosterPopupSetup.cs`) đã xóa khỏi cây làm việc, không còn menu này. Prefab mua booster vẫn đăng ký trong UIRef; chỉnh thiết kế trực tiếp trên prefab. Setup Scene UIManager cũng gán popup mua booster.

Cơ chế `LevelResultPanel` và bộ cấu hình Resources `LevelResultPrefabs` cũ đã được bỏ. Không đưa lại cơ chế tự tạo manager hoặc dựng popup bằng code khi mở.

## Kiểm tra đã thực hiện

- Biên dịch runtime và editor thành công.
- Scene dùng một `UiManager`; ban đầu có ba canvas kết quả, nay thêm `CanvasBuyBooster` thành bốn popup con đúng class, giữ liên kết prefab và tham chiếu controller.
- Play Mode: Next chuyển đúng màn; màn cuối khóa Next; từ chối hồi sinh mở CanvasLose; chơi lại đóng popup và tiếp tục gameplay.
- Hồi sinh qua CanvasRevive và UIManager thu đủ 10/10 phần món vào khay, làm trống 10 slot tại thời điểm thu hoàn tất, trở lại Playing và nhả các khóa.
- Đã khôi phục Edit Mode sau kiểm tra.

Kiểm tra 02/11/2026 trước khi đổi gói từ một sang ba lượt (các số lượng dưới đây là kết quả phiên bản cũ):

- Unity biên dịch script và chạy Play Mode; prefab và tham chiếu scene hợp lệ, đủ icon cho bốn loại.
- Dùng ví riêng trong bộ nhớ, không ghi/tiêu vàng hồ sơ thật: cả bốn loại trừ đúng 900 và cộng đúng một lượt, không cộng nhầm loại, không trừ lặp khi bấm lại.
- 899 gold bị từ chối; đúng 900 gold mua được và còn 0. Để sau không giao dịch; đóng khôi phục tốc độ 0.75 đã đặt trước khi mở.
- Thiếu hồ sơ được xử lý an toàn; loại booster không hợp lệ không mở popup; tạm dừng có sẵn được giữ nguyên; `Booster.RequestBuy()` mở đúng loại.
- Đã dựng và xem ảnh thật từ prefab tại `output/BuyBoosterUI/BuyBooster.png`.
- Kiểm tra đồng bộ Coin bổ sung: số dư ban đầu 1.500; cộng 500 hiện 2.000; trừ 100 hiện 1.900; mua 900 còn 1.000. Đóng không còn nhận sự kiện, mở lại đọc số mới; đổi ví gỡ nguồn cũ, thiếu/nạp muộn hồ sơ xử lý an toàn. Tất cả dùng ví tạm trong bộ nhớ và giữ giá 900 của bản lúc đó, không tiêu vàng thật.
- Lệnh `dotnet build --no-dependencies` chưa xác nhận được vì thiếu các DLL phụ thuộc trong `Temp/bin/Debug`; đây không phải kết quả build thành công. Kiểm chứng chạy ở trên dùng trình biên dịch Unity.

Kiểm tra sau thay đổi gói ba lượt và API chuyên biệt (02/11/2026):

- Đã chạy Play Mode bằng ví và RecordTrackingManager riêng trong bộ nhớ, khôi phục tham chiếu gốc sau kiểm tra, không tiêu vàng hoặc sửa thống kê thật.
- Cả bốn loại: 1.800 Coin và 7 lượt ban đầu → còn 900 Coin và 10 lượt của đúng booster; loại khác không tăng. Mô tả/status trên popup hiển thị ba lượt, giá vẫn 900 GOLD và số dư Coin đồng bộ.
- Bấm lặp không bị tính hai lần. Mỗi giao dịch qua `OnRemoveCoin` / `OnAddBooster` ghi tăng thống kê chi Coin tổng và trong màn thêm 900, booster nhận thêm 3.
- 899 Coin không giao dịch; đúng 900 nhận 3 lượt và còn 0. Thiếu RecordManager không bị trừ trước; hủy mua không đổi tài nguyên hoặc thống kê.

Khi thêm hoặc sửa hàm, tiếp tục tuân thủ comment EN/VI; mọi Tooltip chỉ viết bằng tiếng Việt và mọi asset mới phải đặt đúng thư mục tính năng.

### Giao dịch mới ngày 02/11/2026

Luồng mua hiện tại dùng TryBuyBooster một lần lưu, từ chối khi thiếu tiền hoặc không nhận được cả gói và giữ trạng thái gameplay nếu lưu báo lỗi. Lỗi listener hiển thị không đổi kết quả giao dịch đã chốt. Các kết quả Play Mode ghi phía trên thuộc bản dùng hai lệnh riêng; lượt sửa mới xác minh bằng 69 assertions trong harness mã nguồn thật với PlayerPrefs giả lập, chưa chạy lại toàn bộ UI trong Play Mode.

Hợp đồng 02/11/2026: ProfileManager và PlayerDataManager luôn tồn tại tại nơi gọi runtime, không kiểm tra null cho hai manager. Vẫn kiểm tra dữ liệu đã khởi tạo và RecordManager/SaveService cần cho giao dịch. Đăng ký Coin dùng cờ isObservingCoin để gỡ đúng listener; Edit Mode không truy cập runtime. Kiểm tra thiếu profile cũ là lịch sử, không phải nhánh hiện tại.

Cập nhật 03/11/2026: Lỗi ghi được SaveService bắt và log cảnh báo; không hoàn tác xu, booster hoặc thống kê, giao dịch vẫn trả true và UI/gameplay tiếp tục. Mỗi lần mua vẫn thử lưu một lần, kiểm tra đủ tiền/sức chứa và không cấp thiếu gói. Bỏ snapshot/rollback dữ liệu lưu; rollback slot Pickup khi thao tác không hợp lệ vẫn giữ. Harness cập nhật pass 67 assertions với PlayerPrefs giả lập, chưa chạy lại toàn bộ Play Mode; kết quả 69 assertions trước đó thuộc bản có rollback lưu.

Cập nhật 03/11/2026 — vòng đời CoinBinding: CanvasBuyBooster đăng ký và đọc số dư trong OnEnable, gỡ đúng listener trong OnDisable. Đã bỏ Update và lời gọi binding trong Show; lúc đang mở chỉ nhận sự kiện đổi Coin, không polling hoặc tự đổi nguồn. Nếu dữ liệu chưa khởi tạo thì bật lại popup mới thử nối lại. Đã rà mã luồng Open bật GameObject và cleanup OnDisable; chưa chạy lại Play Mode cho thay đổi này.

Cập nhật 03/11/2026: CanvasBuyBooster cache PlayerDataManager vào field playerData tại OnEnable. Đăng ký/đọc Coin, mua hàng và gỡ listener đều dùng cùng field này; không lấy lại manager trong Purchase hoặc helper binding. Edit Mode không truy cập ProfileManager. Đã rà tham chiếu và diff; chưa chạy lại Play Mode.



## Chuyển sang UIRef (03/11/2026)

Danh sách canvas hiện được lưu duy nhất trong UIRef ScriptableObject, thay cho canvasPrefabs hoặc tham chiếu instance scene của phiên bản trước. Setup Scene UIManager cập nhật UIRef, lưu chỉnh sửa của các instance popup về prefab rồi bỏ instance đặt sẵn; giữ object manager, thông báo ngắn và tham chiếu LevelController. Generic API dùng UICanvasPrime làm ràng buộc. Các hàm Show tiếp tục truyền callback/dữ liệu gameplay.

API theo kiểu: GetUICanvas<T>, OpenUI<T>, CloseUI<T>, IsUICanvasOpened<T>, CloseAllUI(), CloseAllUI<T>() với T : UICanvasPrime. Các hàm ShowWin/ShowLose/ShowRevive vẫn nối callback gameplay cụ thể.

Kiểm chứng UIRef 03/11/2026: Unity biên dịch thành công; asset chứa đủ bốn prefab, scene giữ manager và không có popup đặt sẵn; các tham chiếu Canvas/CanvasGroup/card kế thừa còn hợp lệ. Edit Mode pass tạo khi cần, cache, Next/Replay/từ chối, chuyển popup độc quyền, giữ kiểu, Close trực tiếp, callback đóng một lần và hủy/tạo lại instance. Play Mode pass mở shop từ UIRef, số dư Coin hiện tại, dùng lại instance, giữ pause và nhả pause qua Close/hủy; tốc độ 0.75 → 0 → 0.75, pause có trước được giữ. Không bấm mua hoặc sửa ví trong lượt thử này. Đã thoát Play Mode và xóa object tạm.

UIButtonClick: component phản hồi nhấn độc lập tại `Assets/HotPot/Scripts/UI/UIButtonClick.cs` (đổi tên từ BoosterClick, giữ GUID và tham chiếu scene). Hiệu ứng dùng thời gian unscaled, không gọi logic kích hoạt booster.
Kiểm chứng đổi tên: Unity biên dịch thành công, MonoScript nhận đúng UIButtonClick, GUID không đổi và cả 5 component trong scene resolve đúng class mới.

## TopPanel — UICoin, UICustomer, UILevel (03/11/2026)

Ba component nằm trong Assets/HotPot/Scripts/UI, gắn lên các object Coin/Customer/Level đã có dưới TopPanel, dùng TMP_Text hiện có và giữ nguyên bố cục cùng ScreenTopAnchor. Đây là HUD trong scene, không phải popup trong UIRef.

- UICoin cache PlayerDataManager trong OnEnable, đọc ResourceType.Coin và đăng ký RegisterResourceChange; OnDisable gỡ khỏi cùng manager. Hiển thị phân nhóm hàng nghìn. Không polling.
- UICustomer đăng ký CustomerManager.RemainingCustomersChanged và đọc RemainingCustomers khi bật. Hiển thị số khách còn lại, gồm khách chờ, khách đang phục vụ và khách chưa rời quầy xong. LoadCustomers đặt lại số đếm; mỗi FinishCustomer hoàn tất rời quầy giảm một khách và phát sự kiện.
- UILevel đăng ký LevelController.LevelLoaded, đọc CurrentLevel khi bật và hiển thị Level N từ LevelConfig.levelNumber. Các config 01–10 được gán số tương ứng. Replay/Next/Load cập nhật qua sự kiện, không đổi tiến trình lưu trong hồ sơ.
- Các component không truy cập runtime khi xem Edit Mode. Các field TMP_Text và manager được gán trực tiếp trong Inspector.

Kiểm chứng TopPanel 03/11/2026: Unity biên dịch và Play Mode pass số Coin ban đầu/đọc lại khi bật, số khách còn lại, Level 2 khi LoadLevel, reset khi Replay và bật lại HUD sau khi ẩn. Dùng một order rỗng chủ ý để chạy luồng bỏ qua/rời quầy thật: RemainingCustomers và chữ giảm 1→0; hàng rỗng hiện 0; Replay trả đúng tổng khách. Không sửa ví hoặc mua hàng trong lượt thử; chưa mô phỏng sự kiện cộng/trừ Coin trực tiếp cho UICoin. Đã thoát Play Mode. UICoin dùng DefaultExecutionOrder(100) để OnEnable chạy sau ProfileManager.Awake khởi tạo ví, vẫn dùng đăng ký/gỡ sự kiện trong OnEnable/OnDisable.

## CanvasPause / Setting (10/11/2026)

- Prefab do người dùng cập nhật: `Assets/HotPot/Prefabs/UI/Pause/PausePopup.prefab`, lấy từ UIRef, tạo khi cần. Giữ nguyên thiết kế mới, các chữ TMP và các đường dẫn Root/popbg.
- Setting ngoài HUD vẫn gọi UIManager.ShowPause. Popup lưu tốc độ hiện tại, tạm dừng game; OnDisable nhả riêng pause mà popup giữ. Hoạt cảnh dùng unscaled time.
- Home (object Resume, homeButton): đóng Pause bằng hiệu ứng rồi mở WarningQuitPopup qua UIManager.ShowWarningQuit; chưa ghi nhận thoát hoặc trừ mạng. Chỉ xác nhận Home trong cảnh báo mới UseHeart theo luật mạng vô hạn, BackHome tracking, ResetStatePlaying và nạp Home. Hủy quay lại Pause, game vẫn dừng. Home.unity đã bật trong Build Settings.
- Restart (Replay, replayButton): mở WarningQuitPopup với nút Confirm chung; chỉ xác nhận mới áp dụng luật mất một mạng, lưu rồi ReplayLevel. Hủy quay lại Pause, không reset hoặc mất mạng.
- Nút đóng riêng Root/Button: chỉ đóng popup và tiếp tục, không gọi Home hoặc reset.
- Ba hàng Sound/Music/Vibarate có Button trong suốt trên root để nhấn cả hàng. SettingControl trong CanvasPause giữ Button và hai object On/Off; mỗi thời điểm chỉ hiện một trạng thái. Giữ tên Vibarate theo asset người dùng.
- OnEnable cache PlayerDataManager và LevelController; nối nút, đọc cài đặt và nghe StateChanged. OnDisable gỡ đúng callback, không polling và không nhân đôi listener khi mở lại. Edit Mode không truy cập hồ sơ.
- PlayerDataManager cung cấp IsSoundOn, IsMusicOn, isVibrateOn và SetSoundEnabled/SetMusicEnabled/SetVibrationEnabled. Chỉ khi giá trị đổi mới lưu một lần qua SaveStateChange và phát StateChanged. Dùng các bool sẵn có trong PlayerData, không thêm ví/cấu hình lưu riêng.
- PlayerSettingsController trên object IngameSoundManager (thứ tự 100, sau khởi tạo ProfileManager) áp dụng cài đặt khi bật và khi StateChanged: mute nguồn SFX riêng, mute nguồn Background Music riêng, HapticController.hapticsEnabled cho rung toàn cục. Tắt âm thanh không tắt nhạc và ngược lại. Nguồn nhạc loop đã nối nhưng chưa có clip nhạc nền trong project; gán clip mong muốn trên AudioSource Background Music để phát nhạc. Không tự chọn nhạc demo của thư viện.
- Các công cụ PausePopupSetup của phiên bản 03/11 không còn trong cây làm việc. Sửa trực tiếp prefab và field Inspector của thiết kế mới; không dùng lại công cụ sinh layout cũ.

Kiểm chứng 10/11/2026: Unity biên dịch thành công. Play Mode nhấn ba nút hàng thật (Button.onClick): trạng thái đổi, On/Off loại trừ nhau, SFX/music mute độc lập và cờ haptics đúng. Đọc lại PLAYER_DATA xác nhận lưu đủ ba bool. Đóng/mở lại đọc đúng trạng thái, không nhân đôi listener; đóng trả tốc độ 0.75; Restart về Playing. Home thực sự chuyển tới Home.unity, tốc độ 1 và isPlaying đã false. Các test Restart/Home dùng RecordTrackingManager riêng, không reset thống kê thật; cài đặt đã trả về giá trị trước kiểm tra. Đã trở về Edit Mode. Chưa kiểm tra rung vật lý trên thiết bị và chưa nghe nhạc vì chưa có clip nền được gán.

Đã dựng và kiểm tra trực quan giao diện mới tại `output/PauseUI/Settings.png`: giữ thiết kế người dùng, đủ ba công tắc và Home/Restart/nút đóng. Kiểm tra cuối sau sắp xếp code: Unity biên dịch, prefab có đủ sáu Button, PlayerSettingsController được gán và scene HotPotDemo đã trở lại Edit Mode.

CanvasPause kế thừa trực tiếp UICanvasPrime (10/11/2026): không dùng LevelPopupView hoặc callback primary/secondary/status nữa; giữ Canvas/CanvasGroup/card của base. Home và Restart có field homeButton/replayButton riêng, dùng FormerlySerializedAs để chuyển dữ liệu cũ và đã gán lại trên prefab. OnEnable/OnDisable tự nối/gỡ cả sáu nút; Open gọi base.Open; trạng thái setting và pause giữ nguyên.

Cả sáu hành động gọi PlayHaptic và PlaySoundClick. Các nút điều hướng phát trước khi đóng; công tắc phát theo trạng thái sau thay đổi: tắt Sound không phát click, bật Sound phát ngay; tắt Vibration không rung, bật lại cho phép rung. PlayHaptic dùng playerData đã cache và HapticPatterns.PlayEmphasis(0.8f, 0.5f). PlaySoundClick kiểm tra IsSoundOn rồi gọi IngameSoundManager.PlayClick qua SFX source dùng chung, nên âm thanh không bị cắt khi chỉ đóng popup. Clip đã gán: Assets/HotPot/Audio/SFX/UIClick.wav, sao chép Button1.wav từ ApplicationUX của NiceVibrations vào thư mục audio game; không đổi asset thư viện gốc.

Kiểm chứng: Unity biên dịch; Play Mode xác nhận base trực tiếp, sáu hành động, AudioSource.isPlaying khi nhấn lúc timeScale=0, tắt Sound không phát/bật lại phát, công tắc rung, đóng/Restart trả tốc độ 0.75, mở lại không nhân đôi callback và Home thật nạp với tốc độ 1. Cài đặt gốc được trả lại, thống kê test tách riêng, đã thoát Play Mode. Chưa kiểm tra cảm nhận rung trên thiết bị thật.

## Hiệu ứng CanvasPause và nút (10/11/2026)

CanvasPause vẫn kế thừa trực tiếp UICanvasPrime. Khi mở, card tăng từ 88% lên tỉ lệ đã thiết kế bằng OutBack trong openDuration (0.24 giây), CanvasGroup alpha tăng 0→1. Nhấn nút đóng/Home/Restart khóa tương tác và chạy thu card về 90% cùng mờ alpha về 0 trong closeDuration (0.16 giây). Chỉ sau khi đóng xong mới nhả pause và tiếp tục/Replay/chuyển Home. Nhấn lặp khi đang đóng được bỏ qua; manager Close/HideAll trực tiếp hủy tween cùng callback điều hướng chờ và đóng ngay để không cản luồng reset/chuyển popup. OnDisable hủy tween, trả alpha/tỉ lệ gốc và nhả pause.

Chỉ ba nút Home, Restart và đóng trên prefab có UIButtonScaleEffect; Music, Sound và Vibration không có hiệu ứng scale. Hiệu ứng của ba nút điều hướng: pointer down co xuống 94% trong 0.06 giây; thả/ra ngoài trả về; click (kể cả submit bàn phím) nhún và bật OutBack trong 0.21 giây. Component chỉ quản lý scale và listener của nó, không gọi logic gameplay. Khi tắt, hủy tween và trả scale đã thiết kế. Tất cả tween dùng SetUpdate(true), hoạt động cả lúc Time.timeScale=0.

Kiểm chứng hiệu ứng: Unity biên dịch; kiểm tra theo thời gian thực trong Play Mode pass mở scale/fade khi timeScale=0, co nút qua pointer down, giữ pause/khóa nhập trong lúc đóng, reset scale sau tắt, Restart đúng một lần sau tween, hủy Home chờ bằng HideAll và Home thật sau tween đóng. Kết quả tại output/PauseUI/AnimationCheck.txt; đã trở về Edit Mode. Không thay đổi thiết kế hình ảnh của prefab.

Đã gỡ UIButtonScaleEffect khỏi ba hàng Music/Sound/Vibarate. Xác minh prefab lưu đủ sáu Button, chỉ còn ba component scale trên các nút điều hướng. Không thay đổi callback bật/tắt, tiếng click, rung hoặc hoạt cảnh mở/đóng popup; chưa chạy lại Play Mode cho việc gỡ component này.

Kiểm chứng shop mới 10/11/2026: Unity biên dịch; ví/stat tạm kiểm tra bốn loại TMP/tên/giá và x3; flag tắt ẩn Watch và chặn callback trực tiếp; bật flag cộng đúng +1 không trừ Coin, không nhận lặp; thiếu Coin giữ mở, HideAll nhả pause 0.75. Không tiêu tài nguyên/thống kê thật. Chưa chạy lại mua Coin thành công qua UI mới. Edit Mode dựng hai chế độ không truy cập hồ sơ: output/BuyBoosterUI/BuyBooster.png và BuyBoosterAds.png.

Kiểm chứng timing cuối: output/BuyBoosterUI/AnimationCheck.txt PASS bố cục Coin-only ở giữa và hai nút khi bật ads, mở/đóng thực sự hoàn tất khi timeScale=0, nhả pause sau exit và Watch chỉ +1 một lần/no Coin. Đã trở về Edit Mode, cập nhật ảnh xem trước trạng thái cuối.

## RevivePopup (10/11/2026)

- CanvasRevive trực tiếp kế thừa UICanvasPrime, giữ API Show(revive,decline,available,animate). Nối TMP title/description/price, card Root, ba nút Coin/Watch/X và UIButtonScaleEffect, giữ GUID và thiết kế prefab người dùng.
- Trong mỗi lượt chơi: khi chưa hồi sinh thành công, hiện cả Watch Ads và Coin giá 900. Sau lần thành công đầu (bằng Ads hoặc Coin), ẩn Watch; chỉ hiện Coin ở giữa. Callback Ads bị chặn ở các lần sau, kể cả gọi trực tiếp.
- Giá Coin = 900 + 1000 × tổng số lần hồi sinh thành công trong màn. Ví dụ Ads đầu → Coin 1900 → Coin 2900 → Coin 3900; nếu Coin đầu thì 900 → 1900 → 2900. Ads cũng tăng giá Coin. Chỉ tăng khi hoàn tất thành công, không tăng vì mở popup, thiếu tiền, từ chối hoặc hủy tween.
- Bộ đếm runtime ở canvas cache, reset khi LevelLoaded (gồm Replay và Next) hoặc StateChanged Won/Lost. Đăng ký sự kiện ở Awake và gỡ OnDestroy để vẫn nhận reset khi popup đang ẩn; không lưu tiến độ này vào hồ sơ. isDestroyOnClose của prefab giữ false để dùng lại instance theo UIRef.
- Coin kiểm tra đủ tiền trước khi đóng; sau exit mới OnRemoveCoin(price,revive), tăng bộ đếm và gọi callback ReviveLevel. Hủy qua manager trước khi tween xong không mất Coin hoặc tiến độ. Nếu chi bị từ chối ở cuối, popup hiện lại, cho chọn tiếp.
- Watch tạm gọi thẳng OnWatchAdsSuccess theo yêu cầu trước đó, chưa SDK thực; không trừ Coin hoặc kho booster, nhưng tăng tổng số lần hồi sinh khi thành công. Sau này thay WatchAds bằng lời gọi SDK và nối callback này.
- X từ chối sau hiệu ứng đóng và gọi DeclineRevive để xác nhận thua. available=false khóa Coin/Watch nhưng vẫn cho từ chối. Cả hai nhánh thành công dùng Clean hồi sinh hiện có của LevelController, không reset màn. LevelController tiếp tục giữ pause; canvas không tự nhả TimeScale.
- Mở phóng/fade 0.24s, đóng thu/fade 0.16s unscaled; phản hồi scale/âm thanh/rung trên ba nút như CanvasPause. Khóa lựa chọn lặp trong lúc đóng. Manager Close/HideAll đóng ngay và hủy callback còn chờ.

Kiểm chứng logic giá: harness độc lập tại tmp/ReviveValidation dùng mã CanvasRevive thật (chỉ mở quyền truy cập cho test), stub hiển thị/tween và ví giả; pass 25 assertions cho cả lựa chọn đầu, Ads một lần/tăng giá lên1900, Coin2900/3900, trừ sau exit và một lần, reset khi nạp/thắng/thua lúc ẩn, thiếu tiền/hủy không mất tiền hoặc tăng số lượt. Đây là kiểm tra logic, không phải chi tiền thật hoặc toàn bộ Clean trong Unity. Kết quả UI/timing của phiên bản trước ghi ở output/ReviveUI/Check.txt là lịch sử trước quy tắc giá mới.

Kiểm chứng Unity sau quy tắc cuối: output/ReviveUI/ProgressionCheck.txt PASS lần đầu cả Ads+Coin900, Ads hoàn tất đúng một lần sau tween và làm Coin lên1900/ẩn Ads, callback Ads sau đó bị chặn, LoadLevel reset lúc ẩn về hai lựa chọn900, X từ chối sau exit và Coin thật không đổi. Đã về Edit Mode. Harness 25 assertions kiểm tra thêm giá2900/3900 và reset Won/Lost với ví/tween giả.

## WarningQuitPopup (10/11/2026)

- Prefab người dùng chuyển từ Assets/WarningQuitPopup.prefab tới Assets/HotPot/Prefabs/UI/WarningQuit/WarningQuitPopup.prefab bằng AssetDatabase.MoveAsset, giữ GUID 82617ceef4f76274fa1fa10b4000eb37. Giữ hình, bố cục và chữ TMP; thay component Revive sao chép nhầm bằng CanvasWarningQuit trực tiếp kế thừa UICanvasPrime. Đăng ký trong UIRef, không đặt instance sẵn trong scene.
- CanvasPause.Home chỉ mở cảnh báo sau exit. UIManager.ShowWarningQuit(confirm,cancel) quản lý độc quyền. Warning giữ pause bằng tốc độ trước khi mở; đóng xong nhả pause trước callback, đồng bộ trong cùng frame nên không có frame gameplay chạy giữa Pause↔Warning.
- Nút Home cảnh báo xác nhận đúng một lần sau exit: CanvasPause.ReturnHome gọi HeartManager.UseHeart (mất một mạng, không âm; mạng vô hạn không mất), LevelController.BackHome ghi nhận thoát, ResetStatePlaying lưu hồ sơ và tránh xử lý quit lại khi khởi động, rồi nạp Home. Không trừ mạng hoặc sửa tiến độ khi chỉ mở/hủy cảnh báo.
- Nút X hủy sau exit và mở lại Pause. Đóng Pause sau đó tiếp tục với đúng tốc độ cũ, gồm pre-win. Manager HideAll/Close trực tiếp hủy callback chờ và trả tốc độ, không xác nhận thoát.
- infoLabel luôn hiển thị You will lose 1 life! và icon_lose_life luôn bật, kể cả khi mạng vô hạn. Khi xác nhận vẫn gọi HeartManager.UseHeart nên mạng vô hạn không bị trừ.
- Hai nút có UIButtonScaleEffect, PlayHaptic/PlaySoundClick theo cài đặt; mở 0.24s và đóng0.16s unscaled. Khóa nhấn lặp, callback sau animation. OnEnable/OnDisable nối/gỡ listener, cache manager, dọn tween và pause. Edit Mode không truy cập hồ sơ.

Kiểm chứng Play Mode trước yêu cầu luôn hiện icon/message mất mạng: Pause Home mở cảnh báo trước khi chuyển scene; mở/hủy không mất mạng; hủy quay Pause timeScale0, đóng lại trả0.75; mạng vô hạn dùng cảnh báo tiến độ. Xác nhận lặp chỉ trừ một mạng trên HeartManager tạm (3→2), lưu cờ isPlaying=false và chuyển Home thật, giữ tốc độ0.75. Dùng thống kê/mạng riêng, không tiêu mạng hoặc reset thống kê thật. Kết quả output/WarningQuitUI/Check.txt; đã về Edit Mode và xem ảnh output/WarningQuitUI/WarningQuit.png.

Kiểm tra lịch sử trước thay đổi hiển thị: output/WarningQuitUI/FinalCheck.txt PASS ẩn icon mất mạng khi vô hạn và HideAll hủy xác nhận còn chờ (không gọi quit, trả tốc độ0.75). Unity biên dịch và đã về Edit Mode.

## Tối ưu phần dùng chung của popup (10/11/2026)

UICanvasPrime giữ một triển khai duy nhất cho các phần thực sự giống nhau: CacheFeedbackManagers, PlayClickFeedback (gọi PlayHaptic/PlaySoundClick theo cài đặt), PauseGameplay/ReleaseGameplayPause, CapturePopupScale, ResetAnimation, AnimateOpen, AnimateClose và CloseWithAnimation. openDuration/closeDuration chuyển lên base nhưng giữ nguyên tên field và giá trị prefab0.24/0.16. CanvasPause, CanvasBuyBooster, CanvasRevive, CanvasWarningQuit vẫn trực tiếp kế thừa UICanvasPrime; không thêm tầng class hoặc bộ cấu hình trung gian.

- CanvasPause/WarningQuit dùng pause sở hữu theo instance ở base; OnDisable gọi ReleaseGameplayPause. Chuyển Pause↔Warning giữ đúng tốc độ trước đó.
- CloseWithAnimation dành cho đóng rồi mới điều hướng (Pause/Warning); AnimateClose chỉ hoàn tất hiệu ứng rồi gọi callback để BuyBooster vẫn đóng qua callback manager và Revive vẫn có thể kiểm tra chi tiền cuối/giữ popup khi thất bại. Close() ở base luôn hủy tween và callback chờ trước khi đóng, báo Owner.OnCanvasClosed như trước.
- Từng canvas chỉ giữ field hiển thị, nối/gỡ nút, vị trí bố cục riêng, nội dung và logic nghiệp vụ. BuyBooster vẫn cache cùng playerData từ OnEnable qua helper của base để mua/thưởng/listener Coin; không polling. Revive giữ bộ đếm Ads/Coin và listener reset màn. Warning luôn hiện icon/message mất1life dù unlimited; chỉ lúc confirm mới dùng HeartManager.UseHeart và luật unlimited vẫn không trừ mạng.
- Các coroutine mở riêng của LevelPopupView/CanvasBoosterIntro giữ nguyên; helper animation dùng chung không tự chạy trên chúng khi base.Open được gọi. Các thành viên được sắp theo vòng đời; những lần đổi thứ tự helper đã đối chiếu giữ đầy đủ block gốc. Không dựng lại giao diện hoặc đổi GUID prefab.

Kiểm chứng refactor: bốn prefab vẫn có tham chiếu card/CanvasGroup và thời gian0.24/0.16 kế thừa. Harness dùng mã CanvasRevive cùng UICanvasPrime thật và stub hiển thị/ví pass25 assertions về giá900→1900→2900/3900, Ads tính giá, chi một lần sau tween, reset/hủy/thất bại. Unity Play Mode pass mở/đóng unscaled, booster +1 riêng không lặp, hồi sinh Ads/giá/reset/từ chối, settings không nhân đôi, không scale ba switch, Pause↔Warning và trả0.75, callback Win/Lose, cảnh báo icon/message1life luôn hiện khi unlimited và confirm vềHome không trừ mạng unlimited. Dùng ví/mạng/stat riêng, không tiêu tài nguyên thật; đã trở về Edit Mode. Kết quả output/PopupRefactor/Check.txt. Chưa kiểm tra rung vật lý hoặc nghe nhạc nền.

## Confirm dùng chung cho Home/Replay (10/11/2026)

WarningQuitPopup giữ một nút Confirm (không đổi theo Home/Replay) và nút X. CanvasPause.Home truyền ReturnHome; CanvasPause.Replay truyền ConfirmReplay vào cùng UIManager.ShowWarningQuit. Confirm chạy đúng callback của lần mở sau hiệu ứng đóng, chống nhấn lặp. X luôn quay lại Pause và giữ game dừng.

ConfirmReplay gọi HeartManager.UseHeart rồi playerData.Save và LevelController.ReplayLevel; mất một mạng hữu hạn, không mất khi unlimited theo API hiện có. Chỉ confirm mới mất mạng/reset màn, dùng lại đầy đủ tracking Replay và reset tiến độ hồi sinh qua LevelLoaded. Confirm Home giữ nguyên flow trước: UseHeart, BackHome tracking, ResetStatePlaying, nạp Home. Cảnh báo vẫn luôn hiện icon và message mất1life ở cả hai trường hợp/unlimited.

Kiểm chứng: Unity Play Mode pass nút Confirm chung, Replay mở cảnh báo trước khi reset/trừ mạng, hủy quay Pause không mất mạng, nhấn Confirm lặp chỉ replay cùng màn một lần và trừ một mạng hữu hạn, unlimited không mất mạng; Confirm từ Home vẫn chuyển Home. Test dùng HeartManager/stat riêng; đã về Edit Mode. Kết quả output/WarningQuitUI/ConfirmRoutes.txt.


## Cập nhật cảnh báo thoát và âm thanh (10/11/2026)
Quy tắc hiện hành thay thế mô tả luôn mở cảnh báo trước đây: Home/Replay từ Pause chỉ hiện WarningQuit nếu CustomerManager.HasCompletedOrder=true (đã giao đủ một order trong lượt chơi). Chưa có order hoàn tất: đi thẳng, không trừ life. Sau order đầu: Confirm mới gọi UseHeart; unlimited không trừ. LoadCustomers reset cờ khi tải/chơi lại màn. Giao một phần hoặc bỏ qua order rỗng không tính.

IngameSoundManager dùng 10 clip trong Assets/HotPot/Audio/Sounds: click=SFX_INGAME_CLICK; mở/đóng popup=sfx_ui_popup_show/sfx_ui_popup_disappear; mua thành công=SFX_INGAME_BUY_ITEM; thưởng Watch booster=SFX_INGAME_REWARD_COLLECT; đề nghị revive=SFX_INGAME_OUT_OF_SPACE; giới thiệu booster=SFX_INGAME_NEW_ITEM_UNLOCK; thắng/thua=SFX_INGAME_GAME_WIN/SFX_INGAME_GAME_FAILED; hồi sinh Ads hoặc Coin thành công=sfx_game_revive. UICanvasPrime phát âm vòng đời popup, UIManager phát kết quả màn. Mọi clip dùng nguồn SFX chung nên tuân theo Sound, hoạt động khi pause. Giữ âm chế biến/phục vụ hiện có; thư mục này không cung cấp nhạc nền mới.

Kiểm chứng: Unity biên dịch thay đổi và đã gán/lưu đủ 10 clip trong scene, mỗi clip có thời lượng >0. Chưa kiểm tra nghe trực tiếp hoặc toàn bộ luồng chơi sau thay đổi này.

Kiểm tra bổ sung: Play Mode phát được cả 9 sự kiện và click khi timeScale=0; cờ order ban đầu=false. UIButtonClick phát tiếng click trước callback. Đã thoát Play Mode. Chưa kiểm thử đầy đủ hai nhánh Home/Replay và nghe chất lượng âm trực tiếp.

### Tiếng nhấn khi mở popup
Nút mở popup không phát SFX_INGAME_CLICK; giữ rung/hiệu ứng nút và âm popup. UIButtonClick so sánh ActivePopup trước/sau callback để chỉ bỏ tiếng nhấn khi thực sự mở popup (booster đủ số lượng vẫn có tiếng nhấn). Các chuyển popup sau hiệu ứng đóng như Pause→WarningQuit, WarningQuit→Pause và Revive→Lose chỉ phát rung ở nút. Nút đổi setting, đóng popup, mua/thưởng và điều hướng trực tiếp vẫn giữ tiếng nhấn.


## WinPopup nhận thưởng (10/11/2026)
CanvasWin kế thừa trực tiếp UICanvasPrime, giữ API ShowWin/Show(next, hasNextLevel, animate). Prefab WinPopup dùng Root để chạy hiệu ứng mở/đóng chung, hai nút Next (object BtnReviveCoin giữ tên thiết kế) và Watch Ads, Reward/Text_Amount, UICoin/Text_Coin và CoinAttraction có sẵn. UICoin đã gắn component và theo dõi ResourceType.Coin qua sự kiện. UIRef tiếp tục nạp/cache prefab này.

Next chọn tổng thưởng 100 Coin. Watch chọn tổng thưởng 200 Coin (gấp đôi, không cộng thêm 100). Watch tạm gọi thẳng OnWatchAdsSuccess; sau này nối SDK vào callback này. Mỗi lần hiện thắng chỉ chọn một lần; nhấn/callback lặp bị chặn. Khi claim: khóa hai nút, ẩn Reward, phát âm nhận thưởng và chạy CoinAttraction một lượt unscaled về UICoin/icon. Chỉ onLastParticleFinish mới gọi playerData.OnAddCoin với placement win_reward; UICoin tăng dần số thưởng sau khi CoinAttraction bắt đầu0.4giây; sự kiện Coin thật chốt số dư khi nhận thưởng hoàn tất. Sau đó popup đóng bằng tween và gọi Next nếu còn level. Màn cuối vẫn nhận thưởng nhưng không gọi Next, giữ popup với hai nút khóa. Tắt popup hủy hiệu ứng và callback còn chờ.

Kiểm chứng Play Mode: output/WinUI/Check.txt PASS cả Next +100, Watch +200, khóa nhấn/callback lặp, Reward ẩn ngay, hiệu ứng chạy khi timeScale=0, ví và nhãn UICoin không đổi trong lúc bay, chỉ cập nhật sau khi xu hoàn tất, callback Next đúng một lần sau nhận thưởng. Dùng ví/thống kê riêng, không đổi tài nguyên thật; đã về Edit Mode. Chưa kiểm tra SDK Ads thực tế.


## LosePopup cập nhật (10/11/2026)
CanvasLose kế thừa trực tiếp UICanvasPrime. UIManager.ShowLose(replay, animate) giữ API và phát âm thua; prefab nối BtnHome, BtnRetry, CanvasGroup, Root và UICoin theo dõi ResourceType.Coin. Root gom hình/chữ/nút để dùng hiệu ứng mở/đóng unscaled chung; ví giữ nguyên vị trí. Hai nút có UIButtonScaleEffect và phản hồi rung/click. Không có phần thưởng hoặc CoinAttraction trên thiết kế LosePopup.

Home/Retry khóa nhấn lặp qua closing, UseHeart một lần (unlimited không trừ), ResetStatePlaying lưu thay đổi rồi đóng popup. Retry gọi callback RetryLevel hiện có sau tween, Home nạp scene Home sau tween và không ghi nhận LoseGame thêm lần nữa. Mở popup không trừ mạng; xác nhận này áp dụng cho thua thật, không dùng điều kiện order của luồng thoát từ Pause.

Kiểm chứng LosePopup: Unity biên dịch; đủ liên kết card/CanvasGroup/Canvas/nút, hai UIButtonScaleEffect và một UICoin. Play Mode dùng ví/mạng riêng: Retry hữu hạn 3→2 đúng một lần, callback sau tween unscaled; unlimited giữ số mạng lưu=3 và callback một lần. GetRemainHeart trả -1 khi unlimited nên kiểm tra số mạng lưu trong dữ liệu test. Kết quả output/LoseUI/Check.txt; đã về Edit Mode. Chưa chạy chuyển scene Home trong vòng kiểm tra này.


## Hướng dẫn màn 1 (10/11/2026)
Scene HotPotDemo có object LevelOneTutorial với LevelOneTutorial; tham chiếu LevelController, FoodBoardManager, CustomerManager và prefab Assets/HotPot/Prefabs/UI/Tutorial/SelectDishOverlay.prefab. Prefab riêng tái sử dụng cơ chế định vị BoosterTrialOverlay và bàn tay Spine; hiện bàn tay cùng bốn tấm nền tối, Hole và Prompt. Bàn tay có localScale1.2, lớn hơn hình gốc20%; scaleGuideWithFocus=false giữ nhóm Guide ở scale1 để kích thước bàn tay giống nhau giữa hai bước. Giữ Focus làm anchor định vị không có hình. sortingOrder29900 dưới popup30000, toàn bộ graphic không chặn raycast. Không thay prefab hướng dẫn booster. Không dừng thời gian hoặc khóa thao tác gameplay.

Khi LevelLoaded nhận levelNumber1, chờ0.35 giây cho đĩa hiện rồi trỏ cố định đĩa đầu cột thứ2 (index1) với Select a dish!. DishPicked thành công mới đóng bước này, chạm trượt không đóng. CustomerManager.CustomerArrived(index, order) phát khi khách hợp lệ sẵn sàng gọi món, index là vị trí queue bắt đầu từ0. Khách đầu tiên(index0) của level2 kích hoạt Tap the order to see ingredients! trên món đầu của thẻ; level1 không hiện bước xem order. OrderView.IngredientsOpened chỉ phát khi mở nguyên liệu thành công bằng SetExpanded; sự kiện này kết thúc hướng dẫn order tương ứng.

Tải/chơi lại reset hướng dẫn tương ứng: level1 chọn đĩa, level2 xem order khách đầu; level khác không hiện; rời Playing hoặc tắt object sẽ hủy coroutine/overlay và gỡ sự kiện. Không lưu cờ tutorial vào profile. Kiểm chứng Play Mode output/Tutorial/Check.txt PASS chọn đĩa thật, khách thứ3 và mở chi tiết, reset màn1, không hiện màn2. Chưa kiểm tra chạm trên thiết bị vật lý.

Cập nhật hiển thị: hai bước tutorial level1 hiện bàn tay lớn hơn20% với kích thước cố định, nền tối, hole và chữ nhắc. Bước order lấy tâm ảnh món đầu từ OrderItemView.TargetPosition và kích thước DishSize theo scale world, không lấy collider thẻ. Bước chọn đĩa trỏ đĩa đầu cột2. Luồng sự kiện hoàn tất giữ nguyên.

Kiểm chứng bố trí mới trong Play Mode: nền tối/hole đều bật, localScale bàn tay=1.2; tâm Focus khớp đĩa đầu cột2 và tâm hình món order (sai số dưới0.5% kích thước màn hình). PASS cả hai bước, reset màn1 và không hiện màn2; kết quả output/Tutorial/Check.txt. Đã về Edit Mode.

Kích thước bàn tay: BoosterTrialOverlay có scaleGuideWithFocus mặc định true để giữ hành vi booster cũ. SelectDishOverlay đặt false: Guide.localScale=1, Hand.localScale=1.2 cố định, không phụ thuộc đường kính hole. Prompt bật cho cả Select a dish! và Tap the order to see ingredients!. Kiểm tra trực tiếp Show với bounds1x và3x: hand.lossyScale không đổi, chữ hướng dẫn đang bật và đổi đúng nội dung.

Prompt tutorial dùng TextMeshProUGUI, font Assets/Fonts/FredokaOne-Regular Color.asset, fontSize45, tắt auto sizing. BoosterTrialOverlay.promptLabel nhận TMP và Show cập nhật nội dung cho cả hai bước; tip kiểu Text cũ giữ riêng cho prefab booster hiện có. Kiểm tra prefab đã gán đúng font/cỡ45 và Show đổi đúng lời nhắc order.

Vị trí hole chọn đĩa: lấy tâm Food.IconRenderer.bounds của đĩa đầu cột2; truyền bao vuông có cạnh bằng chiều lớn nhất của hình món. Vì vậy hole nằm ở tâm món, không bị kéo xuống theo nền đĩa/bóng hoặc công thức canh mép trên của BoosterTrialOverlay.Show. Bàn tay giữ cỡ cố định và Prompt TMP45 như trước.

LevelOneTutorial đặt sẵn trên object cùng tên trong scene là controller nhẹ theo sự kiện; chỉ tạo overlay ở màn1 (chọn đĩa) và màn2 (order khách đầu), màn khác không tạo UI/coroutine hướng dẫn. Giữ object hoạt động để nhận LevelLoaded khi chuyển/chơi lại màn, không cần tạo/hủy controller riêng cho từng level. Đổi tên script/class giữ nguyên GUID e681708764fd49a4abf7b3817ca18c24 và các tham chiếu Inspector.

Tutorial xem order hiện chuyển sang khách đầu tiên level2 (CustomerArrived index0); bỏ điều kiện khách thứ3 level1 và việc xếp sau bước chọn đĩa. Kiểm chứng Play Mode output/Tutorial/LevelTwoOrderCheck.txt PASS level1 chỉ chọn đĩa, level2 khách đầu hiện hướng dẫn và đóng khi mở nguyên liệu, chơi lại level2 hiện lại, level3 không hiện. Không đổi bàn tay/hole/font hoặc cơ chế mở nguyên liệu.

WinPopup count-up: CoinAttraction.Play trước, sau0.4giây unscaled mới bắt đầu tăng số trên UICoin qua DOTween, chạy được khi Pause. Thời gian tăng số bằng duration+lifetime.constantMax−0.4 để giữ cùng mốc kết thúc dự kiến của hạt. Chỉ hiển thị xem trước phần thưởng; onLastParticleFinish vẫn cộng Coin đúng một lần và sự kiện Coin chốt số dư, hủy tween. OnDisable hủy tween, OnEnable đọc lại số dư thực.

Kiểm chứng thay đổi count-up0.2s: Unity đã biên dịch mã và prefab đã gán rewardCoin. Chưa hoàn tất kiểm tra Play Mode của thay đổi này: scene đang mở không có Profile/registry CanvasWin đã khởi tạo, nên bộ kiểm tra bị dừng; object kiểm tra tạm đã dọn và trở về Edit Mode.

Điều kiện pre-win hiện hành: chỉ so tổng phần board≤slot trống, không giới hạn số khách. Cách đếm đĩa đôi không trùng, hệ số tăng tốc và cơ chế reset giữ nguyên.

CanvasWin có field Inspector coinCountDelay (Coin Count Delay), mặc định0.4giây, Min0. Chỉnh trực tiếp trên WinPopup.prefab để đổi thời gian chờ trước khi UICoin tăng dần. Thời gian tween tối thiểu0.01giây để tránh giá trị âm khi tăng delay; sự kiện hoàn tất hạt vẫn chốt Coin thực như trước.

WinPopup title Spine cập nhật: field CanvasWin.titleAnimation trỏ SkeletonGraphic (Win) đã được thêm trong prefab, dùng Assets/Spines/win/Win_SkeletonData.asset. Mỗi Show phát Logo Start_End trước, bắt đầu Win Start khi logo còn0.2s, sau Win Start(1.3333s) lặp Idle(3.3333s); clear track/setup pose trước khi chạy lại popup cache. unscaledTime=true để title chạy khi timeScale0; không chỉnh nội dung animation Spine hoặc hiệu ứng ánh sáng. Title không chặn raycast.

Kiểm chứng title: Unity biên dịch; prefab gán đúng title và UnscaledTime=true; kiểm tra chuỗi AnimationState liên tiếp chuyển Start sang Idle lặp. Không chạy lại toàn bộ luồng claim trong vòng kiểm tra này.

WinPopup khi mở runtime ẩn Reward và cả hai nút Next/Watch, khóa nhận thưởng (kể cả callback Ads). Lịch hiện UI dùng titleClaimDelay tính từ lúc Win Start bắt đầu, mặc định2giây unscaled; khi tới mốc mới hiện Glow và scale bật nhẹ Reward/nút, tween xong mới mở tương tác. Đóng hoặc mở lại hủy timer lần trước. Edit Mode preview vẫn hiện đủ bố cục.

Glow của WinPopup cũng ẩn khi mở và hiện cùng Reward/Next/Watch sau titleClaimDelay tính từ Win Start. CanvasWin giữ tham chiếu Inspector trực tiếp tới object Glow đã dựng.

Hiệu ứng hiện thưởng WinPopup (11/11/2026): sau Start, Reward scale0.8→scale thiết kế bằng Ease.OutBack trong claimRevealDuration mặc định0.3s; RootButtons chạy tương tự trễ0.08s. Tween unscaled, không thay scale từng nút nên không xung đột UIButtonScaleEffect khi nhấn. Trong tween vẫn khóa rewardReady và interactable; xong cả hai mới mở nhận thưởng. OnDisable/Show hủy tween cũ và trả scale thiết kế để không co nhỏ dần khi mở lại. Field claimButtons gán RootButtons trên prefab.

Kiểm chứng hiệu ứng scale hiện thưởng: Unity biên dịch thành công, prefab claimButtons=RootButtons và claimRevealDuration=0.3. Chưa kiểm tra trực quan Play Mode sau thay đổi này.


## LosePopup title và hiệu ứng xuất hiện (11/11/2026)
Prefab mới giữ hình SkeletonGraphic (Lose), dùng Assets/Spines/lose/Lose_SkeletonData.asset với animation tên animation dài0.5667s. CanvasLose.titleAnimation chạy lại clip một lần, không loop, UnscaledTime=true. Khi mở runtime, Text_Lose và RootButtons ẩn và khóa thao tác. TrackEntry.Complete mới hiện chúng: thông báo scale0.8→scale thiết kế bằng Ease.OutBack trong choiceRevealDuration mặc định0.3s; nhóm nút theo sau0.08s. Hiệu ứng unscaled và chỉ mở tương tác sau khi cả hai tween xong. Không có Idle riêng trong asset Lose, giữ pose cuối của clip.

Inspector nối titleAnimation, lossMessage=Root/Text_Lose, choiceButtons=Root/RootButtons; nút Home/Retry và UICoin giữ tham chiếu cũ. Từng nút giữ UIButtonScaleEffect; hiệu ứng xuất hiện chạy trên nhóm cha nên không xung đột scale khi click. Show/OnDisable reset scale thiết kế, hủy tween và listener title để mở lại không bị co nhỏ hoặc lộ lựa chọn do callback cũ. Edit Mode preview vẫn hiện đủ thông báo/nút. Quy tắc life/Home/Retry không đổi, chỉ được thực hiện sau khi reveal xong.

Kiểm chứng Play Mode output/LoseUI/TitleRevealCheck.txt PASS ẩn/khóa trước title, không trừ mạng khi gọi Retry sớm, title và scale chạy khi timeScale0, về đúng scale thiết kế/mở tương tác, hữu hạn3→2 đúng một lần sau xác nhận, callback sau exit, unlimited không mất mạng, mở lại chạy title từ đầu và đóng giữa title không hiện lựa chọn cũ. Dùng ví/mạng riêng; đã về Edit Mode. Chưa thử chuyển scene Home trong vòng kiểm tra này.


## Chuỗi Logo → Win title và hiện ví (11/11/2026)
CanvasWin.logoAnimation nối SkeletonGraphic (Logo) đã thêm trong prefab, dùng Start_End dài3s. Mỗi Show ẩn Win title, UICoin, Glow, Reward và Next/Watch rồi phát Logo một lần, UnscaledTime=true. OnLogoUpdate theo dõi TrackTime qua SkeletonGraphic.UpdateComplete; khi TrackTime≥duration−titleOverlap (Inspector mặc định0.2s, tức2.8s) mới bật Win title và phát Start. Logo vẫn chạy nốt0.2s rồi tắt khi Complete; Win tiếp tục Start→Idle như trước. Không sửa nội dung skeleton/animation.

Sau2giây kể từ lúc Win Start bắt đầu (khoảng4.8s tính từ lúc mở popup), bật UICoin cùng Glow, Reward và nút; không dùng sự kiện Win Start hoàn tất để hiện UI. UICoin lúc đó OnEnable đọc số dư thật/đăng ký sự kiện; Reward/nút giữ scale reveal0.3s và trễ nhóm nút0.08s; xong tween mới nhận thao tác. Coin count-up khi claim vẫn giữ delay chỉnh Inspector0.4s. Đóng/mở lại gỡ callback Logo và hủy timer hiện UI để không có callback cũ; Edit Mode preview hiện title Win/UI cuối, không che bằng Logo.

Kiểm chứng Play Mode output/WinUI/LogoSequenceCheck.txt PASS Logo hiện trước, Win Start bắt đầu tại TrackTime2.804453 khi timeScale0 và Logo vẫn còn hiện, UICoin/Glow/Reward/nút chỉ hiện sau Win Start, tween hoàn tất mới mở tương tác, mở lại reset về Logo, đóng giữa intro hủy callback. Không claim tiền trong bộ kiểm tra; đã về Edit Mode. Các helper được sắp lại theo flow bằng13pure moves, so nguyên khối xác nhận không thay thân hàm/comment.

Cập nhật mốc hiện UI: Title Claim Delay (CanvasWin.titleClaimDelay) mặc định2s, tính từ Win Start bắt đầu ở giây2.8, không tính từ Logo hay lúc Win Start kết thúc. UICoin/Glow/Reward bắt đầu hiện khoảng4.8s, nút bắt đầu scale4.88s và mở tương tác khoảng5.18s với duration0.3s. DOVirtual.DelayedCall dùng unscaled time và bị hủy trong StopTitleSequence khi đóng/mở lại. Play Mode output/WinUI/LogoSequenceCheck.txt PASS UI hiện sau2.007607s từ Win Start khi timeScale0, scale reveal/mở tương tác và hủy timer còn chờ; không claim tiền. Đã về Edit Mode.

Cập nhật11/11/2026: bỏ ngưỡng RemainingCustomers khỏi IsPreWin. Chỉ tổng phần nguyên liệu còn trên toàn bộ board≤ChefManager.CountEmptySlots; không cộng chỗ đi thẳng qua đầu bếp. Giữ các điều kiện vòng lặp Playing/không Pause/chưa pre-win và ưu tiên thắng/bế tắc trước.
