# FoodBoardManager — handoff

File: `Assets/HotPot/Scripts/gamebase/FoodBoardManager.cs` (namespace `FoodPop`, singleton trong scene).

## Vai trò
- Quản lý board nguyên liệu: các cột (`ingredientColumns`, 3–5 cột), danh sách đĩa mỗi cột (`columnDishes`, phần tử 0 = đầu cột), pool đĩa (`IngredientDishPool`), bố cục hàng/cột (`rowSpacing`, `columnSpacing`), icon, băng (ice), nắp (hidden).
- Input: `Update` → `TryGetPointerDown` → `TryPickAt` (chỉ chọn đĩa đầu cột không đóng băng, không ẩn).
- Luồng chọn chung `PickDish`: giữ slot băng chuyền (`ChefManager.TryGetNearestEmptySlots`, hết chỗ thì viền nháy đỏ) → bay nguyên liệu (`FoodFlyAnimator`) → gỡ đĩa về pool → `ShiftColumnUp` (dồn các đĩa bên dưới lên 1 hàng, có anim) → `RefreshColumnSelection` → `ReduceFrozenBlocks` (trừ 1 băng cho MỌI đĩa đóng băng trên board) → event `DishPicked`.
- Đĩa đầu cột đang ẩn: chờ cột trượt xong rồi `FadeOutLid` (`RevealHeadDish`).
- API cho booster: `GetColumnDishes`, `GetColumnTransform`, `GetRowLocalPosition`, `ApplyColumnDishes`, `TryGetDishAt`, `TryPickDishAt`, `RefreshSelection`, `InputLocked`, `IsAnimating`.
- Nạp level: `LoadIngredientBoard(List<BoardColumn>)`.

## Thay đổi 2026-10-21: xử lý đĩa nằm dưới mép dưới camera
Vấn đề: cột dài → các đĩa cuối nằm dưới màn hình nhưng vẫn active (Canvas số lượng, outline, hiệu ứng băng...).

Cách xử lý (giữ nguyên dữ liệu/logic, chỉ tắt GameObject):
- Field mới `offscreenHideMargin` (mặc định 1, đơn vị world): đĩa có **tâm hàng hiện tại** thấp hơn `mép dưới camera - offscreenHideMargin` thì `SetActive(false)`; tự bật lại khi dồn lên gần màn hình.
- `LateUpdate` → `RefreshOffscreenDishes()` chạy mỗi frame (rẻ, chỉ SetActive khi trạng thái đổi; tự đúng khi đổi tỉ lệ màn hình/camera/board di chuyển).
  - Dùng vị trí hàng đích (`GetRowLocalPosition(r)`) chứ không phải vị trí hiện tại → khi `ShiftColumnUp` bắt đầu, đĩa được bật ngay và trượt từ dưới mép vào.
  - Đĩa đầu cột (row 0) không bao giờ bị tắt.
- `GetCameraBottomY()`: mép dưới khung `inputCamera` (hoặc `Camera.main`); không có camera → không tắt đĩa nào.
- `IsDishOnScreen(Food)`: đĩa active và mép trên của plate ≥ mép dưới camera.
- `DamageIce`: đĩa không nằm trong màn hình → **chỉ trừ giá trị băng** (`ReduceBlock`), không play `iceHitEffect`/`iceBreakEffect`. (`Food.ReduceBlock` tự bỏ anim nảy băng khi đĩa inactive; `Food.OnEnable` refresh lại hình băng/nắp khi bật lại.)
- `TryGetDishAt`: bỏ qua đĩa đang inactive.
- Property mới `OffscreenHidingSuspended`: true → giữ mọi đĩa active.

`BoosterController.Shuffle` (file `Scripts/Booster/BoosterController.cs`): bật `board.OffscreenHidingSuspended = true` sau `CollectShuffleSpots`, tắt lại sau `ApplyNewLayout` → đĩa từ dưới màn hình bay lên vẫn nhìn thấy trong lúc xáo.

Ghi chú tương tác:
- `DishSelectionOutline.OnDisable` set selectable=false; khi bật lại đĩa là đĩa chờ (dimmed) — đúng vì đĩa đầu cột không bao giờ bị tắt.
- PickupHand: đĩa inactive vẫn có thể nằm trong `pickupTargets` nhưng không chạm được (ngoài màn hình + `TryGetDishAt` bỏ qua).

## Ý tưởng chưa làm
- SpriteMask/gradient ở đáy board (gắn `ScreenBottomAnchor`) để đĩa "chui ra" từ mép băng chuyền thay vì bị mép màn hình cắt.
- Chỉ báo "+N" số đĩa còn dưới màn hình mỗi cột.
- Lazy spawn đĩa nếu level có cột rất dài (>15–20 đĩa/cột).
