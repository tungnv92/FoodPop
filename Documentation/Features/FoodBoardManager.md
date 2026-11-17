# FoodBoardManager

File: `Scripts/gamebase/FoodBoardManager.cs` (+ partial `FoodBoardManager.DishLinks.cs`, namespace `FoodPop`, singleton trong scene trên `Conveyor_Food`). Inspector: `Editor/FoodBoardManagerEditor.cs`. Dữ liệu board của level (`ingredientBoard`, ô ẩn/đóng băng, đĩa đôi, nhóm nối): [Configuration.md](Configuration.md#level-levelconfig) và [LevelEditor.md](LevelEditor.md).

## Bố cục và pool
- Singleton theo scene trên `Conveyor_Food`; `LevelController` gọi `LoadIngredientBoard`.
- Pool đĩa `IngredientDishPool`: tái sử dụng đĩa scene, trả đĩa khi đổi level, chỉ tạo mới khi hết; `ReleaseDish(food)` từ chối trả trùng/lạ. Bên giữ slot phải bỏ tham chiếu trước khi trả.
- Bố cục: hàng đầu tại local (0,0,0), các hàng sau theo `rowSpacing`; `columnSpacing` (scene 1.6) neo ở cột đầu; cột active được căn giữa và ảnh bàn đổi theo số làn (`ingredientTable3/4/5LaneSprite`, gán tay).
- Đĩa chọn được (đầu cột) có viền trắng; đĩa phía sau tối `inactiveBrightness` 0.75, nhãn số giữ sáng.

## Vai trò
- Board nguyên liệu: `ingredientColumns` (3–5 cột), `columnDishes` (phần tử 0 = đầu cột; đĩa đôi chiếm 2 ô cùng tham chiếu, `IsDishAnchor` = ô trên-trái), pool `IngredientDishPool`, bố cục (`rowSpacing`, `columnSpacing`), băng, nắp, nhóm nối.
- **Nạp level:** `LoadIngredientBoard(columns, links)` → `ApplyColumnSpacing` → `UpdateIngredientTableVisual` (ảnh bàn 3/4/5 làn) → `FillBoard` → `CreateBoardDish` (+ `ApplyDishShape`) → `BuildDishLinks` → `SettleBoard`. Sprite bàn, đĩa đôi, viền, nắp **gán tay** trên Inspector.
- **Chạm:** `Update` → `TryGetPointerDown` → `TryPickAt` (chỉ đĩa đầu cột; rung `PlayEmphasis` khi chạm trúng đĩa) → `PickGroup` → `DishPickBatch.TryFill` (theo `DishPickPlan`, giữ slot + `ConveyorSlot.TryFillFood`) → đầu bếp nhận trực tiếp phần ưu tiên nếu rảnh, phần còn lại bay lên băng → `SettleBoard` → `ReduceFrozenBlocks`.
- `HasAvailablePick` duyệt các nhóm ở hàng đầu (không trùng thành viên) bằng `CanPickGroup` + `DishPickPlan`; nhóm chưa lộ không tính. Sức chứa: [ConveyorChef.md](ConveyorChef.md#sức-chứa-khi-lấy-đĩa-dishpickplan).
- API cho booster: `GetColumnDishes`, `GetColumnTransform`, `GetRowLocalPosition`, `ApplyColumnDishes`, `TryGetDishAt`, `TryPickDishAt`, `TryTakePortion`, `GetPickMembers`, `GetPickupGroupRejection`, `RefreshSelection`, `InputLocked`, `IsAnimating`, `OffscreenHidingSuspended`, `MaxVisibleRows`.

## Đĩa ngoài màn hình
`offscreenHideMargin` (1): đĩa thấp hơn mép dưới camera − margin bị `SetActive(false)`, bật lại khi dồn lên (`RefreshOffscreenDishes` trong `LateUpdate`, theo vị trí hàng đích). Đĩa đầu cột luôn bật. Đĩa ngoài màn hình vẫn bị trừ băng nhưng không phát hiệu ứng. Shuffle đặt `OffscreenHidingSuspended = true` khi xáo.

## Đóng băng
Ảnh băng `Art/Sprites/Blocks/Ice/IceBlock_{Frost,Cracked,Faceted,Clear}.png` (mảng `iceBlockSprites`, index = `blockValue − 1`), shader `Art/Shaders/Blocks/IceSurface.shader`. Bị trừ: nảy + vụn `Prefabs/Effects/IceHit.prefab`; tan: `IceBreak.prefab`. Ẩn hoặc đóng băng thì ẩn nhãn số lượng.

## Nút Update View
Inspector có nút **Update View** (`UpdateView()`): áp lại `ApplyColumnSpacing` sau khi đổi `rowSpacing`/`columnSpacing`; Play Mode dàn lại đĩa và dây nối, Edit Mode sắp các `Food` con theo y và ghi Undo.

## Chẩn đoán lấy đĩa
- `logPickFailures` (đang bật — **tắt khi xong**) ghi `[FoodBoardManager] Không pick được:` khi input bị khóa, thiếu camera, chạm trượt, đĩa không ở hàng đầu, game pause, nhóm bị chặn (`DescribePickBlock`) hoặc `DishPickBatch.LastFailure` (dữ liệu đĩa sai, thiếu ChefManager, thiếu slot, slot không nhận food).
- `ConveyorSlot.TryFillFood` tự tìm `Food` con nếu `slotFood` trống, không có thì `LogError [ConveyorSlot]`. Nên gán sẵn `Slot Food` trong prefab.

## Ý tưởng chưa làm
- SpriteMask/gradient ở đáy board (gắn `ScreenBottomAnchor`) để đĩa "chui ra" từ mép băng.
- Chỉ báo "+N" số đĩa còn dưới màn hình mỗi cột.
- Lazy spawn đĩa cho cột rất dài (> 15–20 đĩa).

**Kiểm chứng:** lấy trọn nhóm nối/đĩa đôi trên board thật PASS (nằm trong bộ `output/FlexiblePick`). Lỗi "vừa Play không pick được đĩa" (02/11) chưa xác nhận đã hết.
