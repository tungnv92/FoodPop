# Băng chuyền và đầu bếp

`ChefManager.Instance` tham chiếu `ChefAnims` và `conveyorMovement`; `IsBusy` tính cả lượt giữ đi thẳng tới order.

## File
- `Scripts/gamebase/ConveyorChefMovement.cs`: spline, tạo/bố trí slot, di chuyển, sorting.
- `Scripts/gamebase/ChefManager.cs`, `ChefManager.DirectServe.cs`: chọn món, sơ chế, đi thẳng tới order, thắng màn.
- `Scripts/gamebase/ChefAnims.cs`: animation Spine của đầu bếp (`SkeletonAnimation`).
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

## Đầu bếp Spine
- Skeleton `Assets/Spines/Chef/Chef_SkeletonData.asset` (JSON/atlas/texture cùng thư mục), 22 animation. Reference sơ chế: `Assets/HotPot/Animations/Chef/Chef_*.asset`; reference nghỉ và Win: `Assets/Spines/Chef/ReferenceAssets/`.
- `HotPotDemo.unity` gán 25 dòng ánh xạ FoodType → AnimationReferenceAsset trên `ChefAnims`. Tham chiếu thiếu hoặc khác bộ Spine → lỗi cấu hình.
- **Thời gian sơ chế = `Animation.Duration` của clip** (không có field thời gian riêng), nên đổi clip là đổi nhịp gameplay. Gọi lặp giữ động tác hiện tại; mix = 0.
- Ánh xạ: Salmon → `Slice_Salmon`; Tuna và loại chưa có action riêng → `Slice_Tuna`; PickledGinger → `Cut_Ginger`; Seaweed → `Cut_Seaweed`; Avocado → `Cut_Avocado`; Scallop → `Slice_Scallop`; Octopus → `Slice_Octopus`; CrabStick → `Slice_CrabStick` (đều 1,7667 s); Wasabi → `Grating_Wasabi`; SushiRice → `RiceBall` (1,8333 s); TeaLeaves → `PourTea`; Edamame → `Boil_Bean` (2 s); ChickenThigh → `Grill_chicken_thigh`; ChickenWing → `Grill_chicken_wing`; ChickenBreast → `Grill_chicken_breast`; ChickenBites → `Grill_chicken_skewer`; Tamagoyaki → `Friend_Egg`; Shrimp → `Boil_Shrimp`; Unagi → `Grill_Eel` (2 s; cập nhật 20/11/2026). Có animation mới: tạo reference trong `Animations/Chef/` rồi thay dòng nguyên liệu trên `ChefAnims`.
- Nghỉ: Idle1 lặp ngay, sau 1,5 giây (cấu hình) chuyển sang lặp Idle2 (mỗi clip 1,6667 s).

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
- `ChefManager.HideDishes()` gọi ngay khi khách cuối nhận đủ món (`AllServed` → `FinishLevel(Won)`): hủy sơ chế, chặn tự nhận việc và `ShrinkAllDishes` (mọi slot thu nhỏ về 0 trong `preWin.plateShrinkDuration`, unscaled, rồi tắt object; không xóa food). Khi mọi khách đã ra khỏi màn hình (cộng `preWin.winPanelDelay`, mặc định 0), `ChefManager.PlayWin()` chỉ phát animation Win.
- `ChefAnims.winAnimation` = `Win.asset` (1 s): phát một lần, giữ frame cuối, `UnscaledTime` nên chạy khi `Time.timeScale = 0`; `SetPreparing` không ghi đè trạng thái thắng.
- `LoadLevel` → `ResetForLevel()` dừng thu nhỏ, trả scale, hiện lại slot, đầu bếp về Idle1.
- Chuỗi thắng đầy đủ (chờ, pause, WinPopup): [UI.md](UI.md#thắng).

Bản mô phỏng web của băng/đầu bếp: [LevelStudioWeb.md](LevelStudioWeb.md#đồng-bộ-với-unity).

**Kiểm chứng:** Setup slot, giữ đĩa nguồn và vòng tiến độ đã kiểm tra trên object tạm; DishPickPlan/đi thẳng PASS 197 kiểm tra tích hợp + 192 biên sức chứa (probe ở `output/FlexiblePick`); PlayWin/reset đã Play Mode (bản ẩn đĩa tức thì). Chưa Play Mode: hoạt cảnh nhận món trực tiếp, bản thu nhỏ đĩa khi thắng.
## Cập nhật animation thanh cua — 20/11/2026

- Bộ Spine hiện có 22 animation. So với HEAD có thêm Boil_Shrimp, Grill_Eel và Slice_CrabStick; Shrimp/Unagi đã được gán sẵn trong working tree trước lần cập nhật này.
- Thêm `Assets/HotPot/Animations/Chef/Chef_Slice_CrabStick.asset` cùng `.meta`, dùng chung Chef_SkeletonData; đổi riêng CrabStick (FoodType 5) từ Slice_Tuna sang Slice_CrabStick trong HotPotDemo và scene đang mở. Giữ nguyên 24 mapping khác, kể cả Shrimp/Unagi.
- Unity Edit Mode đã xác nhận GetPreparationAnimationName(CrabStick) trả Slice_CrabStick và GetPreparationDuration trả 1,7667 giây. Chưa kiểm tra trọn lượt sơ chế trong Play Mode; không thay đổi script runtime hoặc lưu các chỉnh sửa scene ngoài mapping này.
