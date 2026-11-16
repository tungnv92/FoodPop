# Băng chuyền và đầu bếp

Animation Spine của đầu bếp: [ChefAnimationFrames.md](../Features/HotPot/ChefAnimationFrames.md). Tham chiếu ChefManager: [Configuration.md](../Features/HotPot/Configuration.md#đầu-bếp-chefmanager).

## File
- `Scripts/gamebase/ConveyorChefMovement.cs`: spline, tạo/bố trí slot, di chuyển, sorting.
- `Scripts/gamebase/ChefManager.cs`, `ChefManager.DirectServe.cs`: chọn món, sơ chế, đi thẳng tới order, thắng màn.
- `Editor/ConveyorChefMovementEditor.cs`: nút Setup tổng.
- `Prefabs/Ingame/Conveyor_Slot.prefab`; scene `HotPotDemo.unity` (object `Conveyor_Chef`).

## Băng và slot
- 10 slot (`ConveyorChefMovement.DefaultSlotCount`, chỉnh qua `Slot Count`).
- Sprite `Art/Sprites/Ingame/conveyor.png` (994×653, 100 PPU) với khay giữa `table_slot.png`. `BeltCenterline` theo local sprite: trên y 2.545, dưới y −2.625, hai bên x ±4.272, bo góc 1.0, bắt đầu giữa cạnh trên, chạy theo chiều kim đồng hồ. Đổi vị trí/kích thước `table_slot` thì phải cập nhật các điểm này.
- `Fit Spline To Conveyor` bật thì `Awake` luôn dựng lại spline theo art; tắt để giữ spline chỉnh tay.
- Nút Inspector **Setup: Build Spline + Create Slots + Snap**: dựng spline, gom/tạo slot từ `Slot Prefab`, xóa slot dư, áp sorting, chia đều — một bước Undo. Không có menu Tools.

## Chọn slot trống
`ChefManager.TryGetNearestEmptySlots()` lấy slot đang hoạt động và trống, ưu tiên quãng đường còn lại tới cửa vào trạm ngắn nhất (`GetDistanceToStation()`; cửa vào là `Station Start`, đảo chiều thì `Station End`). Slot được điền và đánh dấu không trống trước khi hiệu ứng bay bắt đầu.

## Sơ chế
- Khi rảnh, bật `autoPrepareInStation` và không bị booster pause, `FindSlotToPrepare` chọn món thô theo nhu cầu khách ở **mọi vị trí** trên băng. Nguyên liệu `skipPreparation` (Ice, Salt) được `Prepare()` ngay khi lên đĩa.
- **Giữ đĩa nguồn:** đĩa nguồn giữ food và `isEmpty = false` trong lúc món bay vào đầu bếp, sơ chế và bay về; hình món trên đĩa ẩn (`BeginFoodArrival`). Vòng tiến độ (`preparationTrack`/`preparationArc` — hai LineRenderer có sẵn trong prefab) bám theo đĩa, đầy dần theo thời gian sơ chế, dừng khi pause. Xong thì món bay về đúng đĩa nguồn. Hủy sơ chế khôi phục món; `ArrivalId` bảo vệ đĩa đã bị xóa/dùng lại.
- Món thô bay lên thớt (sprite `ChefManager.GetRawSprite(slot)`), tới thớt thì ẩn sprite và chỉ còn anim chặt + vụn (`FoodIconConfig.GetChopDebris`, không có vụn/khói dự phòng); poof (`PlayPreparedPoof`) hiện sprite sơ chế rồi bay về.
- Thời gian sơ chế = `ChefAnims.GetPreparationDuration(type)` (độ dài animation), không gồm bay vào/ra.
- Món đã sơ chế, đáp lại đĩa nguồn phải nằm `Serve Delay After Landing` giây (field CustomerManager; scene 0,3 s) rồi mới bay lên order.

## Nhận món trực tiếp từ board
Sau khi `DishPickBatch.TryFill` giữ đủ slot và chốt chi phí, `ChefManager.TryPreparePicked` cho đầu bếp rảnh nhận một phần thô ưu tiên cao nhất của lượt lấy (gồm hai nửa đĩa đôi và nhóm nối), nếu ưu tiên cao hơn các phần thô đang trên băng (ngang thì băng trước). Phần đó bay thẳng từ board tới thớt; slot của nó vẫn được giữ và nhận món khi bay về.

## <a id="sức-chứa-khi-lấy-đĩa-dishpickplan"></a>Sức chứa khi lấy đĩa (DishPickPlan)
`DishPickPlan` là nơi duy nhất quyết định sức chứa, dùng chung cho `DishPickBatch.TryFill`, `FoodBoardManager.HasAvailablePick` và `GetPickupGroupRejection`. N = tổng `TotalAmount` của mọi Food duy nhất trong nhóm (`GetPickMembers`, gồm hai nửa đĩa đôi); S = số slot trống. Nhóm phải lộ, không đóng băng, không nắp (`CanPickGroup`).

| Trường hợp | Kết quả |
| --- | --- |
| N ≤ S | Mọi phần vào băng (luồng thường, có thể kèm `TryPreparePicked`) |
| N = S + 1 | Chỉ khi đầu bếp rảnh và có order nhận ngay một phần → phần đó đi thẳng tới order |
| N > S + 1, hoặc dư 1 nhưng sai nguyên liệu / đầu bếp bận | Không lấy (Result: `InsufficientCapacity`, `ChefUnavailable`, `NoCustomerNeed`, `InvalidGroup`) |

**Đi thẳng tới order:** `DirectServeReservation` giữ đúng một nhu cầu bằng `DishProgress.Reserve` (theo luật đế và incoming); batch giữ N−1 slot cho phần còn lại. Lỗi điền/thanh toán → nhả slot và hủy giữ chỗ. Chốt xong `ChefManager.BeginDirectPreparation` bay sprite nguồn tới thớt, sơ chế, rồi `ServingFood.LaunchBoosterServe` giao tới đúng order — không tạo slot ảo, không trả về băng. `IsBusy` và `HasPendingService` tính cả lượt giữ này; cancel/replay/disable nhả nhu cầu chưa gửi.

## Thắng màn
`ChefManager.PlayWin()` hủy sơ chế, chặn nhận việc, thu nhỏ mọi slot rồi tắt; `ResetForLevel()` khôi phục khi `LoadLevel`. Chuỗi đầy đủ: [UI.md](UI.md#thắng).

Bản mô phỏng web của băng/đầu bếp: [LevelStudioWeb.md](../Features/HotPot/LevelStudioWeb.md#đồng-bộ-với-unity).

**Kiểm chứng:** Setup slot, giữ đĩa nguồn và vòng tiến độ đã kiểm tra trên object tạm; DishPickPlan/đi thẳng PASS 197 kiểm tra tích hợp + 192 biên sức chứa (probe ở `output/FlexiblePick`); PlayWin/reset đã Play Mode (bản ẩn đĩa tức thì). Chưa Play Mode: hoạt cảnh nhận món trực tiếp, bản thu nhỏ đĩa khi thắng.
