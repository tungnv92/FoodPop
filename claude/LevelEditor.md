# Level Editor — handoff

Cập nhật: 2026-10-29. Editor và runtime đã hỗ trợ đĩa đôi dọc/ngang; phần kiểm chứng và giới hạn còn lại ghi ở cuối tài liệu.

Files:
- `Assets/HotPot/Editor/LevelConfigEditorWindow.cs` (`EditorWindow`, namespace `FoodPop`)
- `Assets/HotPot/Editor/LevelConfigInspector.cs` — Inspector của `LevelConfig`, nút "Mở Level Editor" + các field chỉ đọc để tránh sửa trực tiếp asset.
- Data: `Assets/HotPot/Scripts/gamebase/LevelConfig.cs` (+ `BoardColumn`), `CustomerOrder.cs`, `OrderFood.cs`, `DishShape.cs`
- Runtime nạp level: `Assets/HotPot/Scripts/gamebase/LevelController.cs`
- Runtime board/hiển thị: `Assets/HotPot/Scripts/gamebase/FoodBoardManager.cs`, `Food.cs`, `IngredientDishPool.cs`, `FoodFlyAnimator.cs`.
- Tích hợp booster: `Assets/HotPot/Scripts/Booster/BoosterController.cs`.
- Điền bảng theo order: `Assets/HotPot/Editor/LevelBoardOrderFiller.cs`.
- Asset mẫu: `Assets/HotPot/Config/Resources/Levels/LevelConfig_01.asset` (06/11/2026: mọi level nằm trong `Config/Resources/Levels`, game nạp theo số qua `LevelConfig.Load`; level mới tạo bằng editor được lưu vào thư mục này với `levelNumber` khớp số trong tên) (3 cột x 7 hàng, trái 2 khách, phải 2 khách)

Mở: menu `Tools/FoodPop/Level Editor` (lấy LevelConfig đang chọn) hoặc nút trong Inspector. `minSize` 980x560.

## Data model

### Độ khó và bản chơi lại Easy (11/11/2026)

- `LevelConfig.difficultyMode`: `DifficultyMode.Normal = 0`, `Hard = 1`, `Easy = 2`. Chỉnh **Difficulty Mode** ở đầu panel khách trong Level Editor; vẫn sửa trên bản nháp và phải bấm **Lưu**.
- Runtime đọc field cấu hình, không suy ra Hard từ số level. Các asset hiện có 15, 20, 25 được đánh dấu **Hard** theo mốc đã chốt (`>=15`, chia hết cho 5); có thể đánh dấu level khác là Hard trong editor.
- Với cấu hình **Hard**, `ForRetry(retryCount)` trả bản gốc khi retry < 2; retry >= 2 nạp `Resources/Levels/LevelConfig_NN_Easy`. Bản Easy có cùng `levelNumber`, được đánh dấu **Easy**, không làm tăng số level.
- Đã nhập `LevelConfig_15_Easy` (10 khách, 5×10), `LevelConfig_20_Easy` (13 khách, 5×13), `LevelConfig_25_Easy` (14 khách, 5×14) từ Web Studio; tổng nguyên liệu khớp order. Khi đánh dấu màn mới là Hard, có thể cung cấp asset `_Easy` tương ứng; nếu thiếu asset thì vẫn nạp bản Hard gốc, không chặn lượt chơi hoặc đổi dữ liệu.
- `LevelController` áp dụng lựa chọn khi bắt đầu màn và nạp lại. Replay ghi nhận lượt chơi lại trước khi chọn cấu hình; Retry sau thua dùng bộ đếm đã tăng lúc ghi nhận thua. Hồi sinh tiếp tục màn hiện tại, không nạp lại bản khác.
- Kiểm chứng: 55 kiểm tra Edit Mode qua, gồm ngưỡng retry -1/0/1/2/3/10, giữ bản Easy khi nạp lại, Normal không đổi dù số là 15/20/25, số level và tổng nguyên liệu ba bản Easy; build runtime/editor hoàn tất với 0 lỗi; chưa thử luồng thua/chơi lại trong Play Mode. Bổ sung 23 kiểm tra cho trường hợp thiếu Easy: giữ đúng tham chiếu Hard gốc, không đổi dữ liệu, vẫn giữ ngưỡng của bản Easy có sẵn; build runtime/editor 0 lỗi.

- `LevelConfig` (ScriptableObject, menu `Hot Pot/Level Config`):
  - `customerQueue`: `List<CustomerOrder>` chung, lấy lần lượt cho hai chỗ phục vụ. `useUnifiedCustomerQueue` bật thì danh sách rỗng vẫn là rỗng; nếu chưa bật và chưa có khách chung, đọc xen kẽ `leftQueue`/`rightQueue` cũ.
  - `boardRows` (số ô/cột), `boardColumns`; `ingredientBoard`: `List<BoardColumn>` (trái→phải), mỗi cột `foods` (trên→dưới). Đĩa đôi chiếm hai ô nên số ô không đồng nghĩa số đĩa.
  - `OnValidate` → `ResizeBoard()` giữ đúng `boardColumns` x `boardRows`, kích thước tối thiểu 1; khôi phục danh sách, cột và ô null (bảng luôn hình chữ nhật).
  - `GetBoardCount(FoodType)` cộng số lượng nguyên liệu ở cả hai phần của đĩa đôi, bỏ qua dữ liệu riêng tại ô bị chiếm. `TotalCustomers` đếm theo `GetCustomerQueue()`.
  - `GetCell`, `TryGetCoveredCell`, `TryGetCoveringDish`, `IsCoveredCell` tra ô gốc/ô bị chiếm; `CollectBoardProblems` kiểm tra cấu hình đĩa đôi.
- `CustomerOrder.ingredientOrders`: tối đa `MaxDishes = 4` nhóm/khách, mỗi nhóm `ingredients` chứa 1–6 FoodType khi chơi. Mỗi nhóm có thêm `dish` (prefab `FinalFoodView`, tùy chọn). Từ 2026-11-03 `CustomerOrder` chỉ còn `customerType`, `timeServe`, `ingredientOrders`; `useIngredientOrders`, `finalFoods`, `GetIngredientOrders` và `FinalFoodType`/`FinalFoodConfig` đã bị gỡ; giới hạn số nguyên liệu dùng `IngredientOrder.MaxIngredients` (6).
- `CustomerOrder.customerType`: `Normal = 0`, `Boss = 1`, `Shipper = 2`; mặc định Normal.
- `CustomerOrder.timeServe`: `float`, mặc định `30f` giây. Chỉ hiện chỉnh cho Boss/Shipper, phải hữu hạn và lớn hơn 0; giá trị không hợp lệ trong ô chỉnh được trả về 30. Normal ẩn ô chỉnh. Chưa có cơ chế đếm ngược/hết giờ trong runtime.
- `OrderFood` (1 ô board): `foodType`, `requiredCount` (≥1, số nguyên liệu trong đĩa), `isHidden` (nắp, mở khi lên đầu cột), `isFrozen` + `blockValue` (mỗi lần pick bất kỳ đĩa → mọi đĩa băng −1, về 0 thì tan).
- Đĩa đôi thêm `dishShape` (`Single`, `Vertical`, `Horizontal`), `secondFoodType`, `secondCount` (≥1). Hai phần có thể cùng loại. Nắp/băng dùng chung; `CountOf` cộng cả hai phần, `Clear` đặt lại thành đĩa đơn trống.
- `LevelController.LoadLevel` → `FoodBoardManager.LoadIngredientBoard(config.ingredientBoard)` + `CustomerManager.LoadCustomers(config)`.

## Bố cục cửa sổ
`OnGUI`: `DrawToolbar` → (nếu có level) 2 cột: `DrawCustomerPanel` (trái, 40% vùng hiển thị, scroll riêng) | divider | `DrawBoardPanel` (phải, 60% vùng hiển thị: board + validation, scroll riêng) → `RunPendingAction`.

Thay đổi cấu trúc (thêm/xoá khách, món, resize, fill, clear) được hoãn qua `pendingAction` và chạy sau khi vẽ xong để layout group cân bằng. Chỉnh field đi qua `SerializedObject` của bản nháp → có Undo/Redo; thao tác trực tiếp dùng `Undo.RecordObject` + `SetDirty` trên bản nháp, không phải asset nguồn.

### Bản nháp và lưu thủ công
- `sourceLevel` giữ asset gốc; `level` là bản sao tạm (`HideInHierarchy | DontSave`) dùng cho toàn bộ chỉnh sửa. Không dùng `HideAndDontSave` vì giá trị này chứa `NotEditable`, khiến các `PropertyField` như Hidden/Frozen bị Unity khóa. Đổi field, thêm/xóa dữ liệu, điền bảng và Undo/Redo không làm đổi asset nguồn; lưu project cũng không ghi các thay đổi của bản nháp vào asset.
- Chỉ khi bấm **Lưu** (hoặc chọn Lưu trong hộp thoại đóng cửa sổ), `SaveDraft` mới chép bản nháp vào nguồn. Giữ nguyên tên/cờ hiển thị của nguồn, gọi `SaveAssetIfDirty(sourceLevel)` để chỉ lưu đúng asset đó.
- **Tải lại** nạp lại dữ liệu từ asset nguồn và bỏ bản nháp hiện tại sau khi xác nhận. Nút **Load** cùng số level cũng nạp lại. Không tự lưu khi tải lại, chuyển level, chọn asset khác hay tạo level mới.
- `savedDraftJson` là mốc so sánh; `hasUnsavedChanges` đánh dấu cửa sổ có thay đổi và cho Unity hiện lựa chọn lưu/bỏ/hủy khi đóng. Chuyển level hoặc tải lại khi đang sửa sẽ hỏi **Bỏ thay đổi / Tiếp tục chỉnh**.
- Bản nháp và mốc so sánh được giữ qua tải lại script khi Unity khôi phục được đối tượng tạm. `OnDisable` gỡ theo dõi Undo; `OnDestroy` dọn bản nháp và lịch sử Undo của nó.
- `EnsureDraftEditable` bỏ cờ khóa ở bản nháp cũ sau tải lại script, đồng thời chuẩn hóa cờ trong mốc JSON để không báo thay đổi giả; không xóa dữ liệu đang chỉnh. Đã kiểm chứng `SerializedProperty.editable` của Hidden/Frozen và bản nháp trong cửa sổ đang mở; bật trạng thái không làm đổi nguồn.
- Các field ở Inspector chỉ để xem; dùng Level Editor để chỉnh và lưu. Tạo level mới vẫn tạo một asset trống ngay, các chỉnh sửa tiếp theo chỉ nằm trong bản nháp đến khi bấm Lưu.

### Toolbar
- ObjectField chọn LevelConfig; tự theo `Selection` khi chọn LevelConfig khác trong Project (`OnSelectionChange`).
- Điều hướng: `◀ Back` / ô số level / `Load` / `Next ▶`. Số level đọc từ đuôi tên asset (`LevelConfig_07` → 7, `ParseLevelNumber`). Không có level → hỏi tạo `LevelConfig_NN` trong `Assets/HotPot/Config` (`CreateLevelNumber`). Next ở level cuối → hỏi tạo level kế.
- `Tạo level mới` (SaveFilePanel, tên gợi ý theo số lượng level), `Tìm trong Project` (Ping asset nguồn), `Tải lại` (bỏ bản nháp và nạp nguồn), `Lưu` (`SaveDraft` → `AssetDatabase.SaveAssetIfDirty` cho đúng nguồn).

### Panel khách (trái)

- Một **Hàng khách chung · hai chỗ phục vụ**; khách đầu vào trước, chỗ nào trống nhận khách kế tiếp. Dữ liệu hai hàng cũ có nút chuyển sang danh sách chung trên bản nháp.
- Mỗi khách có bộ chọn **Loại khách** Normal/Boss/Shipper. Boss và Shipper hiện thêm **Thời gian phục vụ (s)**, mặc định 30; Normal ẩn trường này.
- Mỗi nhóm (`DrawIngredientGroup`) hiện icon nguyên liệu và foldout chọn nguyên liệu trực tiếp, cùng `DrawGroupDish` (từ 2026-11-03): preview `AssetPreview` 48px của prefab món của nhóm và dropdown liệt kê mọi prefab dưới `Assets/HotPot` có `FinalFoodView` ở gốc (`GetDishPrefabs()` quét, có cache; cache xoá ở `OnEnable` và `OnProjectChange`). Chọn prefab thì chép nguyên liệu của nó (theo thứ tự công thức) vào nhóm và gán `dish` (`ApplyDishPrefab`). Bật/tắt nguyên liệu (`SetIngredientSelection`) gọi `LinkMatchingDish`: gán `dish` bằng prefab đầu tiên có nguyên liệu khớp (thứ tự bất kỳ) hoặc xoá. Nhãn dưới dropdown: "Prefab món khớp nguyên liệu" hoặc "Chưa có prefab món khớp nguyên liệu (thẻ order không có hình)". `AddCustomer` không còn đụng `useIngredientOrders`/`finalFoods`; `AddDish` xoá `dish`. Không cần FinalFoodType cho tổ hợp mới.
- `+ Thêm khách` tạo Normal, `timeServe = 30`, một nhóm trống, không chọn sẵn cơm/cá hồi. `+ Nhóm nguyên liệu` thêm nhóm trống, khóa khi đủ 4 nhóm.
- [Đã gỡ 2026-11-03: `EnsureIngredientGroups` chuyển đổi `finalFoods` cũ không còn; level đã được chuyển sang `ingredientOrders`.] Giữ thứ tự chọn; phần tử đầu là đế. Dữ liệu lặp giữ số lượng khi đọc và tính tổng; bộ chọn checkbox thao tác theo loại nguyên liệu.
- Các popup chọn món cũ (`FinalFoodListPopup`, `FinalFoodPickerPopup`, `DrawDishRow`, `DrawRecipeIngredients`, `GetFinalFoodConfig`, `GetFinalTexture`) đã bị gỡ ngày 2026-11-03; chưa compile/chạy thử trong Unity. Chỉ bấm **Lưu** mới ghi bản nháp về asset.

### Panel board (phải)
- Ô `Số hàng` / `Số cột` → resize (Undo "Đổi kích thước bảng").
- Lưới cố định `DrawBoardGrid`: mỗi ô 180×180px. Đĩa ngang gộp hai ô trái/phải, đĩa dọc gộp hai ô trên/dưới; ô bị chiếm không vẽ riêng. `DrawCellArea` tô nền/viền cam cho đĩa dọc, xanh cho đĩa ngang.
- `DrawMainPanel`: icon kèm dấu ẩn/băng, dropdown `Kiểu đĩa`, loại nguyên liệu và số lượng phần 1, `Ẩn nguyên liệu`/`Ẩn cả đĩa`, `Đóng băng`, `Giá trị băng`. `DrawSecondPanel` hiện icon và loại/số lượng riêng của phần 2, dùng chung dấu ẩn/băng.
- Riêng đĩa dọc, `DrawVerticalDish` đặt dropdown kiểu đĩa ở đầu, tiếp theo là hai thẻ nguyên liệu cùng kích thước (`DrawVerticalPortion`): icon bên trái, nhãn PHẦN 1/PHẦN 2 và số lượng bên phải, dropdown nguyên liệu bên dưới. Footer `DrawVerticalSharedSettings` có đường phân cách và nhãn ÁP DỤNG CẢ ĐĨA, gom nắp/băng/giá trị băng dùng chung; dành sẵn chỗ để bật băng không làm bố cục nhảy.
- `ChangeDishShape` có Undo; đĩa đôi cần ô bên dưới/bên phải trong bảng và chưa thuộc đĩa khác. Nếu ô sẽ bị chiếm đang có dữ liệu, hỏi xác nhận trước khi xóa dữ liệu ô đó.
- `DrawBoardProblems` hiện cảnh báo tràn bảng, chồng vùng chiếm, ô bị chiếm còn dữ liệu riêng, thiếu nguyên liệu, số lượng không dương hoặc kiểu đĩa không hợp lệ; kèm hướng dẫn chọn/dồn đĩa ngang và Shuffle.
- `Điền bảng theo tổng order` (`FillBoardFromOrders`):
  1. Đếm nguyên liệu cần (`CountOrderedFoods`: đọc thẳng `ingredientOrders` của từng khách trong hàng chung (`AddOrderTotals`), cộng từng phần tử của nhóm; nguyên liệu lặp được tính đủ số lượng).
  2. `LevelBoardOrderFiller.TryFill` tính mỗi đĩa đôi là hai vị trí nguyên liệu, ô bị chiếm không tính thành đĩa riêng. Gom các nửa đĩa đôi trước, sau đó ô đơn theo thứ tự cột→hàng. Giữ kiểu đĩa và nắp/băng chung của đĩa đôi; thay lại cả nguyên liệu lẫn số lượng ở hai phần.
  3. Kiểm tra trước khi sửa: nếu số loại vượt số vị trí, tổng số phần nhỏ hơn số nửa đĩa đôi, hoặc vùng chiếm sai/chồng nhau → báo lỗi và giữ nguyên bảng. Mỗi nửa đĩa đôi bắt buộc có ít nhất một phần.
  4. `AllocatePortions`: mỗi loại một vị trí, vị trí dư chia cho loại có lượng/vị trí cao nhất (hòa → tổng lớn hơn → thứ tự enum); không loại nào nhiều vị trí hơn tổng lượng của nó. Chia đều lượng mỗi loại, phần dư vào vị trí đầu. Hai nửa có thể cùng loại.
  5. Ưu tiên điền đủ hai nửa mọi đĩa đôi, rồi điền ô đơn. Ô đơn thừa và dữ liệu riêng của ô bị chiếm được xóa. Đĩa đơn được điền sẽ reset nắp/băng như trước. Có Undo cho toàn bộ thao tác.
  - Toàn bộ thao tác chỉ sửa bản nháp; vẫn phải bấm **Lưu** để ghi asset. Không giữ lại nguyên liệu cũ của đĩa đôi để trừ nhu cầu nữa. Không tự xáo trộn hoặc sinh trạng thái ẩn/băng.
- `Xoá bảng`: mọi ô về đĩa đơn None, số lượng mặc định 1, không ẩn, không băng, xóa dữ liệu phần 2.

### Kiểm tra nguyên liệu
Bảng theo từng FoodType: Order cần | Bảng có | Trạng thái (Đủ xanh / Thiếu N đỏ / Dư N vàng). Số bảng có tính cả hai phần của đĩa đôi, kể cả khi cùng loại, và bỏ qua ô bị chiếm. Dòng tóm tắt tổng khách + kích thước bảng. Đếm theo chỉ số enum (an toàn với khoảng trống ID số).

## Dish Link — chuỗi A–B–C (2026-10-23)

- Trong phần board, mở **Dish Link · dây nối thẳng**, bấm **+ Nhóm 2 đĩa** hoặc **+ Nhóm 3 đĩa**. Nhập cột/hàng của A, B, C bằng số bắt đầu từ **1**. Nhóm ba đĩa chỉ có hai đoạn **A–B** và **B–C**, không nối thành tam giác.
- Dữ liệu `LevelConfig.dishLinks` chứa `DishLinkConfig.dishes`: danh sách tọa độ `(cột, hàng)` bắt đầu từ **0**, theo thứ tự nối. Tọa độ là vị trí ban đầu trước khi runtime dồn board. Chọn ô gốc của đĩa đôi, không chọn ô bị chiếm. Mỗi đĩa chỉ thuộc một nhóm.
- Dây vàng bện tròn có đổ khối, viền tối, phản sáng kem và móc vàng vát mép theo ảnh demo; được vẽ bằng `DishLinkView`; mỗi đoạn có đúng hai đầu, luôn thẳng dù khác cột/hàng hoặc có đĩa khác ở giữa. Dây đi qua không làm đĩa trung gian thành thành viên. Dây bám vị trí khi dồn; đĩa được dồn theo quy tắc vùng chiếm hiện có.
- Chỉ chạm thành viên nằm ở hàng đầu để lấy cả nhóm. Đĩa ở hàng sau không nhận chọn thường và không kích hoạt nháy từ chối. Chọn thường yêu cầu không có đĩa ngoài nhóm phía trước bất kỳ thành viên nào trong các cột nó chiếm. Các thành viên liên tiếp cùng cột được lấy cùng nhau; một đĩa ngoài nhóm nằm giữa chúng sẽ chặn nhóm.
- Chạm một thành viên ở hàng đầu khi nhóm bị chặn (độ sâu, nắp/băng hoặc thiếu slot) sẽ nháy viền đỏ trên mọi đĩa trong nhóm, kể cả đĩa sâu. Cập nhật selection mỗi khung hình giữ nguyên màu/hiển thị của lượt nháy; hết nháy trả về viền nghỉ hiện tại.
- Cần đủ slot cho tổng lượng của mọi đĩa, gồm cả hai phần của đĩa đôi. Giữ chỗ/điền nguyên nhóm trước khi gỡ; lỗi điền được hoàn tác. Một thao tác lấy nhóm chỉ giảm băng trên board **một lần**.
- Một thành viên còn băng sẽ chặn cả nhóm. Nắp tự mở khi lộ đầu cột; thành viên thấp cùng cột cũng mở khi cả nhóm đã lộ để tránh kẹt nắp. Đĩa thành viên ở hàng đầu vẫn sáng/hiện viền dù thành viên khác chưa sẵn sàng; các đĩa sâu bị chặn vẫn tối. Chỉ cần một thành viên ở hàng đầu thì toàn bộ dây và móc của nhóm sáng; không có thành viên đầu hàng thì dây tối. Độ sáng không thay đổi điều kiện lấy cả nhóm.
- PickupHand kiểm tra tổng chỗ và băng của cả nhóm, làm sáng/mở nắp cả nhóm, cho phép bỏ qua độ sâu. AutoComplete vẫn lấy từng phần; thành viên hết nguyên liệu được gỡ, hai thành viên còn lại nối trực tiếp, còn một thì bỏ dây. Shuffle bỏ qua mọi đĩa thuộc nhóm.
- **Điền bảng theo tổng order** giữ nhóm và bảo đảm có nguyên liệu ở mọi ô gốc được nối, ưu tiên cùng các nửa đĩa đôi trước ô đơn thường. Giữ nắp/băng của đĩa nối; nếu tổng order không đủ hoặc tọa độ sai, báo lỗi trước khi sửa dữ liệu. **Xóa bảng** cũng xóa các nhóm.
- Editor cảnh báo nhóm không đủ 2–3 đĩa, tọa độ ngoài bảng, ô bị chiếm, thành viên trống hoặc lặp/thuộc nhiều nhóm. Runtime bỏ qua nguyên nhóm sai và ghi cảnh báo.
- Các chỉnh sửa nhóm nằm trên **bản nháp**; chỉ ghi asset khi bấm **Lưu**. **Tải lại** phục hồi dữ liệu đã lưu. Không tự thêm nhóm vào level hiện có.
- Mã chính: `DishLinkConfig`, `DishLinkGroup`, `DishPickBatch`, `DishLinkView`, `FoodBoardManager.DishLinks`, `LevelConfig.DishLinks`, `LevelConfigEditorWindow.DishLinks`.
- Kiểm chứng: 36 kiểm tra trong Unity preview scene qua, gồm lấy nhóm ba đĩa, từ chối chạm thành viên hàng sau và lấy nhóm từ hàng đầu, blocker, nắp/băng, thiếu slot và rollback khi slot lỗi, PickupHand bỏ qua độ sâu, AutoComplete nối lại, replay, điền bảng và validation. Chưa kiểm tra hoạt cảnh trong Play Mode.

## Icon / tài nguyên
- Nguyên liệu: `FoodIconConfig.asset` trước, fallback `Art/Sprites/Foods/Sushi/Raw/<kebab-case>.png`. Cache trong `icons`.
- Món hoàn chỉnh: [Đã thay thế 2026-11-03] không còn sprite đơn/`FinalFoodConfig.asset`/cache `finalIcons`/`FinalSpriteFolder`; hình món là preview prefab `FinalFoodView` (`GetDishPrefabs()`).
- `OnEnable` xoá cache (sau domain reload).
- `VietnameseNames` hard-code theo thứ tự khai báo enum `FoodType` — thêm FoodType mới phải cập nhật mảng này.

## Giới hạn hiện tại (để tham khảo khi làm ý tưởng mới)
- Board bắt buộc chữ nhật: mọi cột cùng `boardRows` (không có cột dài ngắn khác nhau).
- Không có preview giống in-game (vị trí đĩa thật, băng chuyền, số slot), không mô phỏng/kiểm tra level có giải được không (thứ tự đĩa vs thứ tự khách, slot băng chuyền).
- Không có kéo-thả/hoán đổi ô, copy/paste cột, xáo trộn board, hay random ẩn/băng.
- Validation kiểm tra tổng số lượng và cấu hình vùng chiếm đĩa đôi; chưa xét thứ tự phục vụ, khả năng giải hay độ khó.
- Runtime dùng số cột được scene hỗ trợ trong khoảng `FoodBoardManager.MinColumns..MaxColumns` (hiện 3–5); editor vẫn cho nhập kích thước ngoài khoảng này. Đĩa đôi vượt vùng cột thực tế hoặc không hợp lệ bị báo lỗi và bỏ qua khi nạp.
- Không có nút Play/test level trực tiếp từ editor.

## Đĩa đôi (dọc / ngang) — ĐÃ TRIỂN KHAI (2026-10-22)

Đĩa đôi chiếm 2 ô của board.
- **Dọc**: chiếm 2 hàng liên tiếp trong 1 cột. Chọn được khi nửa trên ở hàng 0. Chọn 1 lần → bay toàn bộ số lượng còn lại của cả hai phần, giải phóng hai hàng.
- **Ngang**: chiếm 2 cột kề nhau cùng hàng. Chọn được khi ở hàng 0 của CẢ 2 cột. Luật "cổng": chỉ trượt lên khi 2 ô phía trên đều trống → các đĩa bên dưới ở cả 2 cột bị kẹt tới khi gỡ đĩa ngang. Tạo liên kết giữa 2 cột (người chơi phải dọn đều).

Đã chốt (2026-10-22):
1. Nội dung: 2 nguyên liệu (có thể khác loại), mỗi phần có số lượng riêng.
2. Chạm 1 lần lấy hết cả 2 phần.
3. Băng chuyền không đủ slot cho cả 2 phần → chặn, viền nháy đỏ (như đĩa đơn).
4. Shuffle đổi nội dung nguyên liệu giữa các vị trí, hỗ trợ cả đĩa đôi và giữ vùng chiếm cố định; đĩa thuộc Dish Link được giữ nguyên.
Băng/nắp hiện dùng chung cho cả đĩa: một giá trị băng, một nắp; đĩa ngang chỉ tự mở nắp khi đứng đầu cả hai cột.

Data và editor hiện tại (giữ board chữ nhật):
- `OrderFood`: `DishShape dishShape` {Single, Vertical, Horizontal} + `secondFoodType`, `secondCount`. Ô gốc = ô trên (dọc) / ô trái (ngang); ô còn lại là ô "bị chiếm" (bỏ qua khi nạp).
- Editor: dropdown "Kiểu đĩa"; vẽ gộp hai ô thành một khung đĩa với hai panel, không vẽ riêng ô bị chiếm. Validate: ngang không ở cột cuối, dọc không ở hàng cuối, hai đĩa đôi không chồng nhau, ô bị chiếm không có dữ liệu riêng. Bảng kiểm tra cộng cả hai phần.
- "Điền bảng theo tổng order" giữ hình dạng và trạng thái chung của đĩa đôi, điền lại cả hai phần theo tổng order; ưu tiên đủ các nửa đĩa đôi rồi mới tới ô đơn. Không sửa dữ liệu nếu order không đủ để điền cả hai nửa. Đã qua 20 kiểm tra: tổng nguyên liệu chính xác, cùng loại ở hai nửa, giữ nắp/băng/bố cục, bỏ qua ô bị chiếm, Undo, từ chối không đổi dữ liệu và không tác động asset nguồn.

Runtime (`FoodBoardManager`, `Food`, `BoosterController`):
- `columnDishes` giữ entry giữ chỗ trỏ tới cùng `Food` cho ô bị chiếm.
- `FillBoard` nạp theo hàng rồi cột để giữ chỗ trước khi duyệt ô bị chiếm; `CreateBoardDish` cấu hình dữ liệu/hình ảnh. `Food.Shape`, `SecondFoodType`, `SecondAmount`, `TotalAmount` giữ dữ liệu runtime; `TakePortion` lấy đúng nửa còn nguyên liệu.
- `SettleBoard`: xét mọi ô trống; đĩa ngang chỉ di chuyển khi cả hai ô phía trên trống, dồn hai cột đồng bộ. Đĩa dọc giữ hai hàng liền nhau. Sau khi gỡ đĩa, chạy lại đến khi không còn đĩa di chuyển được.
- `TryPickAt`/selection: đĩa ngang selectable khi là đầu cột ở cả 2 cột.
- Slot băng chuyền cần bằng tổng số phần của cả hai nửa (không cố định hai slot). Kiểm tra đủ chỗ trước khi điền; lỗi điền slot được hoàn tác, đĩa vẫn nguyên trên board. Mỗi nửa bay từ icon riêng tới các slot đúng loại.
- Shuffle đổi nội dung đĩa đơn/đôi, giữ vùng chiếm và bỏ qua đĩa thuộc Dish Link. PickupHand lấy cả đĩa từ một trong hai ô. AutoComplete lấy từng phần từ cả hai nửa; giữ vùng chiếm đến khi toàn bộ đĩa hết nguyên liệu.
- Băng của một đĩa đôi chỉ giảm một lần mỗi lượt lấy đĩa. Replay trả mỗi đĩa về pool một lần, xóa các tham chiếu ô chiếm và trạng thái đĩa đôi trước khi dùng lại.
- Hình: đĩa đôi dùng sprite chữ nhật bo nhẹ riêng cho ngang/dọc trong `Assets/HotPot/Art/Sprites/UI/Dishes`, đồng bộ chất liệu đĩa đơn. Căn sprite đúng kích thước vùng chiếm hai ô; hai icon không bị kéo giãn và có số lượng riêng. Nửa hết nguyên liệu ẩn icon. Viền chọn dùng sprite vòng riêng; `PlateSilhouette` chỉ dự phòng.
- Hidden dùng nắp chữ nhật riêng `PlateLidHorizontal.png` / `PlateLidVertical.png`, tham chiếu màu bạc, cách đổ bóng và núm tròn của `PlateLid.png` gốc. `horizontalDoubleLid` / `verticalDoubleLid` đã gán trong scene và có đường dẫn mặc định ở `OnValidate`. `Food.ConfigureShapeLid` căn nắp chung phủ cả hai phần; hoạt cảnh nhấc/mờ giữ nguyên, pool khôi phục sprite/scale nắp tròn. Khi còn Frozen, lớp băng và giá trị băng vẫn hiển thị phía trên nắp.
- `FoodBoardManager` giữ `horizontalDoublePlate`, `verticalDoublePlate`, `doublePlateOutlineMaterial`; đã gán trong `HotPotDemo.unity`, `OnValidate` hỗ trợ gán mặc định. Pool khôi phục sprite/viền tròn gốc khi đĩa được dùng lại làm đĩa đơn. Xem `Assets/HotPot/Documentation/DoubleDishArt.md` để biết đường dẫn asset, prompt tạo ảnh và kiểm chứng render.
- Viền chọn đĩa đôi có sprite riêng `PlateSelectionOutlineHorizontal.png` / `PlateSelectionOutlineVertical.png` trong `Assets/HotPot/Art/Sprites/UI/Dishes`, nền và tâm trong suốt. `horizontalDoubleOutline` / `verticalDoubleOutline` đã gán trong scene và được truyền tới `DishSelectionOutline`; sprite dùng được cả viền trắng lẫn nháy đỏ. Ưu tiên vòng viền riêng, shader `PlateSilhouette` chỉ dự phòng. Có menu `Tools/FoodPop/Rebuild Double Dish Outlines` để tạo lại hình học viền.
- `Food.ConfigureShape` điều phối các helper khôi phục kích thước gốc, áp vùng chiếm và tạo hình phần 2 có thể tái sử dụng. `LateUpdate` đồng bộ độ sáng, độ mờ và thứ tự vẽ của phần 2 sau thay đổi từ board/booster.
- Kiểm chứng: build runtime/editor không lỗi; 30 kiểm tra hành vi trong preview scene độc lập, gồm cổng ngang, vùng chiếm dọc, tổng nguyên liệu, sức chứa, băng dùng chung, lấy từng phần và tái sử dụng pool. Chưa kiểm tra hoạt cảnh trong Play Mode.

- 2026-11-05: Toolbar thêm nút `UpdateFoodPrefab` (`UpdateFoodPrefabs`): xoá cache prefab, quét lại và gọi `LinkMatchingDish` cho mọi nhóm của `customerQueue`/`leftQueue`/`rightQueue` trong bản nháp; hộp thoại báo số nhóm khớp và liệt kê nhóm chưa có prefab (tối đa 15). Cần bấm Lưu để ghi asset.
