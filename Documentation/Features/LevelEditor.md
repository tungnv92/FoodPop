# Level Editor (Unity)

## File
- `Editor/LevelConfigEditorWindow.cs` (namespace `FoodPop`), `Editor/LevelConfigInspector.cs` (Inspector chỉ xem + nút **Mở Level Editor**), `Editor/LevelBoardOrderFiller.cs`.
- Data: `Scripts/gamebase/LevelConfig.cs` (+ `BoardColumn`), `CustomerOrder.cs`, `OrderFood.cs`, `DishShape.cs`, `DishLinkConfig`.
- Asset: `Config/Resources/Levels/LevelConfig_NN.asset` (và `_Easy`). Level mới tạo bằng editor được lưu vào thư mục này với `levelNumber` khớp tên.
- Mở: menu `Tools/FoodPop/Level Editor` hoặc nút trên Inspector (`minSize` 980×560).

## Data model
- `LevelConfig`: `difficultyMode` (Normal/Hard/Easy), `customerQueue`, `boardRows` × `boardColumns`, `ingredientBoard` (cột trái → phải, ô trên → dưới; luôn chữ nhật, `ResizeBoard` trong `OnValidate`), `dishLinks`. Chi tiết độ khó và khách: [Configuration.md](Configuration.md).
- `OrderFood` (một ô): `foodType`, `requiredCount` (≥ 1), `isHidden` (nắp, mở khi lên đầu cột), `isFrozen` + `blockValue` (mỗi lần lấy đĩa bất kỳ, mọi đĩa băng −1; về 0 thì tan), `dishShape` (Single/Vertical/Horizontal), `secondFoodType`, `secondCount`.
- Tra cứu: `GetCell`, `TryGetCoveredCell`, `TryGetCoveringDish`, `IsCoveredCell`, `GetBoardCount(FoodType)` (cộng cả hai nửa đĩa đôi), `CollectBoardProblems`.

## Cửa sổ
- **Bản nháp:** mọi chỉnh sửa (có Undo) diễn ra trên bản sao tạm (`HideInHierarchy | DontSave`, không dùng `HideAndDontSave` vì sẽ khóa field). Chỉ **Lưu** mới chép về asset nguồn (`SaveAssetIfDirty`). **Tải lại** bỏ bản nháp. Chuyển level/đóng cửa sổ khi có thay đổi sẽ hỏi.
- **Toolbar:** chọn LevelConfig (theo Selection), `◀ Back` / số level / `Load` / `Next ▶` (không có level thì hỏi tạo), `Tạo level mới`, `Tìm trong Project`, `Tải lại`, `Lưu`, `UpdateFoodPrefab` (quét lại prefab món và nối `dish` cho mọi nhóm khớp nguyên liệu).
- **Panel khách (trái, 40%):** Difficulty Mode; một hàng khách chung; mỗi khách có Loại khách (Boss/Shipper hiện **Thời gian phục vụ (s)**, mặc định 30, phải > 0). Mỗi nhóm (tối đa 4): chọn nguyên liệu (thứ tự chọn, mục đầu là đế) và prefab món (preview 48 px, dropdown mọi prefab có `FinalFoodView` dưới `Assets/HotPot`); chọn prefab thì chép nguyên liệu, đổi nguyên liệu thì tự nối prefab khớp.
- **Panel board (phải, 60%):** số hàng/cột; lưới 180×180 px/ô (đĩa đôi gộp hai ô, cam = dọc, xanh = ngang); mỗi ô chỉnh kiểu đĩa, nguyên liệu/số lượng từng phần, ẩn, đóng băng, giá trị băng. Cảnh báo cấu hình (tràn bảng, chồng vùng chiếm, ô bị chiếm còn dữ liệu, số lượng không dương…).
- **Điền bảng theo tổng order:** đếm nguyên liệu từ `ingredientOrders`; điền đủ hai nửa mọi đĩa đôi và ô gốc của nhóm nối trước, rồi ô đơn theo cột → hàng; mỗi loại ít nhất một vị trí, vị trí dư cho loại có lượng/vị trí cao nhất; giữ kiểu đĩa và nắp/băng của đĩa đôi/đĩa nối. Không đủ thì báo lỗi, không sửa gì. **Xóa bảng** đưa mọi ô về đĩa đơn trống và xóa nhóm nối.
- **Kiểm tra nguyên liệu:** bảng Order cần | Bảng có | Đủ/Thiếu/Dư theo từng FoodType.
- Icon: `FoodIconConfig.asset`, dự phòng `Art/Sprites/Foods/Sushi/Raw/<kebab-case>.png`. `VietnameseNames` theo thứ tự enum `FoodType` — **thêm FoodType mới phải cập nhật mảng này**.

## Đĩa đôi
- **Dọc:** chiếm 2 hàng một cột, chọn được khi nửa trên ở hàng 0. **Ngang:** chiếm 2 cột cùng hàng, chọn được khi ở hàng 0 của cả hai cột; luật cổng — chỉ trượt lên khi cả hai ô phía trên trống.
- Hai phần có loại/số lượng riêng; chạm một lần lấy hết cả hai; nắp và băng dùng chung (băng giảm một lần mỗi lượt lấy).
- Runtime: `Food.Shape`, `SecondFoodType`, `SecondAmount`, `TotalAmount`, `TakePortion`; `SettleBoard` dồn đĩa ngang đồng bộ hai cột. Nhãn số lượng phần 2 chỉ hiện khi `SecondAmount > 1`.
- Hình: sprite đĩa/nắp/viền chọn riêng cho ngang/dọc trong `Art/Sprites/Table_Assets`, gán trên `FoodBoardManager` trong scene: khay `tray_2a` (ngang) / `tray_3a` (dọc), viền chọn `tray_2` / `tray_3`, nắp `mystery_tray_2` / `mystery_tray_3`. Bộ cũ `Art/Sprites/UI/Dishes` (`CeramicPlate*`, `PlateLid*`, `PlateSelectionOutline*`) đã xóa ngày 17/11/2026 vì không còn scene/prefab nào dùng.

## Dish Link (nhóm nối 2–3 đĩa)
- Panel **Dish Link · dây nối thẳng**: **+ Nhóm 2 đĩa** / **+ Nhóm 3 đĩa**, nhập cột/hàng từ 1 (dữ liệu lưu từ 0, vị trí trước khi dồn; chọn ô gốc của đĩa đôi). Chuỗi A–B–C, không thành tam giác; mỗi đĩa thuộc tối đa một nhóm.
- Runtime (`DishLinkView`, dây vàng thẳng): chỉ chạm thành viên ở hàng đầu để lấy cả nhóm; phía trước mọi thành viên không được có đĩa ngoài nhóm. Thành viên đóng băng chặn cả nhóm. Nhóm bị chặn → nháy đỏ cả nhóm. Sức chứa: [ConveyorChef.md](ConveyorChef.md#sức-chứa-khi-lấy-đĩa-dishpickplan).
- PickupHand lấy cả nhóm bỏ qua độ sâu; AutoComplete lấy từng phần (thành viên hết thì gỡ, nối lại phần còn lại); Shuffle bỏ qua đĩa trong nhóm. Runtime bỏ qua nhóm sai và cảnh báo.

## Giới hạn
Board chỉ hình chữ nhật; runtime hỗ trợ 3–5 cột (`FoodBoardManager.MinColumns..MaxColumns`). Không có preview in-game, kiểm tra giải được, kéo-thả ô hay chơi thử trực tiếp trong Unity (chơi thử dùng [Level Studio Web](LevelStudioWeb.md)).

**Kiểm chứng:** độ khó/ForRetry 78 kiểm tra Edit Mode PASS; đĩa đôi 30 kiểm tra và Dish Link 36 kiểm tra trong preview scene PASS; điền bảng 20 kiểm tra PASS. Chưa Play Mode: luồng thua → chơi lại bản Easy, hoạt cảnh đĩa đôi/nhóm nối.
