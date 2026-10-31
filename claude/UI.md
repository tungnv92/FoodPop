# UI màn chơi

Cập nhật: 30/10/2026.

## Cấu trúc và nguyên tắc

- Mọi thao tác mở/đóng popup màn chơi được gọi qua `FoodPop.UIManager`.
- Dùng đúng object `UiManager` đã có trong scene `Assets/HotPot/Scenes/HotPotDemo.unity`; không tạo manager khác dưới `LevelController`.
- Mỗi popup có một prefab và class cụ thể riêng. Giữ bố cục, hình, chữ và nút trong prefab để chỉnh trực tiếp bằng Prefab Mode, không dựng lại giao diện bằng code lúc chạy.
- Các popup là bản prefab đã đặt sẵn dưới `UiManager`, được gán qua Inspector và ban đầu tắt. Runtime dùng lại các object này.
- Chỉ một popup được mở tại một thời điểm. `UIManager` đóng các popup khác trước khi mở popup mới.

```text
UiManager (UIManager)
├── CanvasWin    (CanvasWin, bản của WinPopup.prefab)
├── CanvasLose   (CanvasLose, bản của LosePopup.prefab)
└── CanvasRevive (CanvasRevive, bản của RevivePopup.prefab)
```

## File và prefab

| Thành phần | Vị trí |
| --- | --- |
| Manager scene | `Assets/HotPot/Scripts/UI/UIManager.cs` |
| Popup thắng | `Assets/HotPot/Scripts/UI/CanvasWin.cs` |
| Popup thua | `Assets/HotPot/Scripts/UI/CanvasLose.cs` |
| Popup hồi sinh | `Assets/HotPot/Scripts/UI/CanvasRevive.cs` |
| Lớp cơ sở dùng chung | `Assets/HotPot/Scripts/UI/LevelPopupView.cs` |
| Logic trạng thái màn | `Assets/HotPot/Scripts/gamebase/LevelController.cs` |
| Prefab thắng | `Assets/HotPot/Prefabs/UI/LevelResult/WinPopup.prefab` |
| Prefab thua | `Assets/HotPot/Prefabs/UI/LevelResult/LosePopup.prefab` |
| Prefab hồi sinh | `Assets/HotPot/Prefabs/UI/LevelResult/RevivePopup.prefab` |
| Sprite dùng chung | `Assets/HotPot/Art/Sprites/UI/LevelResult/` |
| Công cụ gán tham chiếu | `Assets/HotPot/Editor/LevelResultPrefabSetup.cs` |
| Công cụ xem thử | `Assets/HotPot/Editor/LevelResultPreview.cs` |
| Tài liệu chi tiết | `Assets/HotPot/Documentation/LevelResultUI.md` |

## Trách nhiệm và API

### UIManager

- Giữ ba tham chiếu scene: `canvasWin`, `canvasLose`, `canvasRevive`.
- `Instance` tìm manager có sẵn, không tự tạo object quản lý mới.
- `ShowWin(next, hasNextLevel, animate)` mở `CanvasWin`.
- `ShowLose(replay, animate)` mở `CanvasLose`.
- `ShowRevive(revive, decline, available, animate)` mở `CanvasRevive`.
- `HideAll()` đóng ba popup và xóa trạng thái popup đang mở.
- `ActivePopup` và `ActiveCanvas` phục vụ kiểm tra và dựng ảnh xem thử.
- Khi khởi tạo, chuẩn bị EventSystem nếu scene chưa có và ẩn mọi popup. Khi manager bị tắt, đóng mọi popup.

### Các class popup

- `CanvasWin`: nhận hành động Next và trạng thái còn màn kế tiếp.
- `CanvasLose`: nhận hành động chơi lại màn hiện tại.
- `CanvasRevive`: nhận riêng hành động hồi sinh và từ chối; từ chối vẫn khả dụng khi hồi sinh bị khóa.
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
3. Nút **NEXT** gọi `LevelController.NextLevel()` để nạp `LevelConfig.nextLevel` qua luồng đặt lại màn đầy đủ.
4. Các asset hiện có nối theo 01 → 02 → … → 10. Màn cuối không có `nextLevel`: khóa Next và báo đã hoàn thành tất cả màn hiện có.

### Bế tắc và hồi sinh

1. Băng chuyền đầy, không có món phục vụ được khách hiện tại và không còn xử lý cần chờ hoàn tất thì đề nghị hồi sinh trước.
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

## Setup và chỉnh sửa

1. Giữ object `UiManager` hoạt động trong scene, gắn component `UIManager`.
2. Đặt ba bản prefab dưới object này và gán đúng class/tham chiếu trong Inspector.
3. Gán cùng UIManager vào trường `uiManager` của `LevelController`.
4. Chỉnh thiết kế trong từng prefab; giữ hoặc cập nhật các tham chiếu hiển thị của class popup khi đổi cấu trúc con.
5. `Tools → HotPot → Setup Result Popup References` sửa tham chiếu hiển thị còn thiếu trên prefab, không dựng lại hình hoặc bố cục.
6. `Tools → HotPot → Setup Scene UIManager` dùng lại object manager đã có, gán hoặc bổ sung prefab con, nối controller và lưu scene đang mở.
7. `Tools → HotPot → Preview Win And Lose UI` dựng cả ba trạng thái qua một UIManager xem thử riêng. Ảnh lưu ngoài Assets tại `output/LevelResultUI/win.png`, `lose.png`, `revive.png`.

Cơ chế `LevelResultPanel` và bộ cấu hình Resources `LevelResultPrefabs` cũ đã được bỏ. Không đưa lại cơ chế tự tạo manager hoặc dựng popup bằng code khi mở.

## Kiểm tra đã thực hiện

- Biên dịch runtime và editor thành công.
- Scene dùng một `UiManager`, có đúng ba canvas con đúng class, giữ liên kết prefab và tham chiếu controller.
- Play Mode: Next chuyển đúng màn; màn cuối khóa Next; từ chối hồi sinh mở CanvasLose; chơi lại đóng popup và tiếp tục gameplay.
- Hồi sinh qua CanvasRevive và UIManager thu đủ 10/10 phần món vào khay, làm trống 10 slot tại thời điểm thu hoàn tất, trở lại Playing và nhả các khóa.
- Đã khôi phục Edit Mode sau kiểm tra.

Khi thêm hoặc sửa hàm, tiếp tục tuân thủ comment EN/VI; mọi Tooltip chỉ viết bằng tiếng Việt và mọi asset mới phải đặt đúng thư mục tính năng.
