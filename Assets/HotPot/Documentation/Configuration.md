# HotPot configuration / Cấu hình HotPot

## Current ingredient set / Bộ nguyên liệu hiện tại

The current FoodType enum and FoodIconConfig contain 15 ingredients. IDs 8 and 11 are retired and must not be reused. ID 13 is Seaweed; IDs 15–17 are SoySauce, PickledGinger and Wasabi. These four types share one sprite between processing states, stored under Art/Sprites/Foods/Sushi/Shared. / FoodType và FoodIconConfig hiện có 15 nguyên liệu. ID 8 và 11 đã ngừng dùng, không tái sử dụng. ID 13 là rong biển; ID 15–17 là nước tương, gừng ngâm và wasabi. Bốn loại này dùng chung sprite cho hai trạng thái tại Art/Sprites/Foods/Sushi/Shared.

## Sushi ingredient replacement — 2026-10-16 (historical / lịch sử)

The active ingredient set now contains 14 sushi ingredients, with a Raw and Prepared sprite per type. / Bộ nguyên liệu hiện tại gồm 14 loại sushi, mỗi loại có ảnh thô và sơ chế.

- Sprites: `Art/Sprites/Foods/Sushi/Raw` and `Art/Sprites/Foods/Sushi/Prepared`; 196×196, transparent, centered pivot, 100 PPU, Sprite Mode Single.
- `FoodIconConfig.FoodIcon.sprite` remains the Raw field for serialization compatibility; `preparedSprite` is the Prepared field. `GetIcon(type, state)` resolves either state and falls back to Raw if Prepared is missing.
- `Food.SetIconConfig` binds the shared table; `Prepare` and `SetState` update the displayed sprite. `LevelController` binds this table when initializing dishes.
- Các ảnh Raw giữ nguyên miếng; Prepared cắt nhỏ và xếp gọn. Rice Prepared có dải rong biển. Không có Nori riêng.
- Existing level quantities, customer queue order and board dimensions were retained. / Giữ số lượng, thứ tự khách và kích thước bảng level hiện tại.

This replacement intentionally reuses old serialized numeric slots as follows; it is a themed replacement, not a conversion retaining the old ingredient meanings. Keep these new IDs stable from now on. / Lần thay chủ đề này dùng lại các ID số dưới đây; giữ cố định các ID sushi này khi phát triển tiếp.

| ID | Previous ingredient | Sushi ingredient |
|---|---|---|
| 1 | Tomato | SushiRice |
| 2 | Chili | Salmon |
| 3 | BeefRolls | Tuna |
| 4 | Shrimp | Shrimp |
| 5 | Squid | CrabStick |
| 6 | FishBalls | Tamagoyaki |
| 7 | NapaCabbage | Cucumber |
| 8 | Corn | Avocado |
| 9 | EnokiMushrooms | SalmonRoe |
| 10 | ShiitakeMushroom | Unagi |
| 11 | Tofu | Retired / Ngừng dùng |
| 12 | Noodles | Octopus |
| 13 | — | FriedTofu |
| 14 | — | Scallop |

Source atlas and scene preview: `output/HotPotDemo/Sushi` outside Assets. Pre-replacement scene (including unsaved edits), scripts, config, prefabs and legacy sprite/meta backups: `output/HotPotDemo/Archive/sushi-migration-20261016`.

## Previous configuration notes / Ghi chú cấu hình trước khi đổi sang sushi

- `FoodType` (Scripts/gamebase/FoodType.cs) identifies the 12 foods. Keep existing numeric values stable when adding types. `None` is not a collectible food.
- `OrderFood` (Scripts/gamebase/OrderFood.cs) is one ordered food: `foodType` and `requiredCount` (at least one).
- `CustomerOrder` (Scripts/gamebase/CustomerOrder.cs) is one customer's order: `foods` is a list of `OrderFood`, each type once. `GetRequiredCount(FoodType)` returns 0 for foods that are not requested.
- `LevelConfig` (Scripts/gamebase/LevelConfig.cs, **Assets > Create > Hot Pot > Level Config**) holds the customers to serve at the two queues of the order counter: `leftQueue` and `rightQueue`, each a list of `CustomerOrder`; element 0 is served first. `TotalCustomers` and `GetQueue(0 = left, 1 = right)` help gameplay code.
- `LevelConfig` also has an ingredient board of raw foods: `boardRows` x `boardColumns` (editable) and `ingredientBoard`, a list of `BoardColumn` (left to right), each holding `foods`, a list of `OrderFood` from top to bottom (a list of lists). Every column automatically keeps exactly `boardRows` foods. `GetBoardCell(column, row)` and `GetBoardCount(FoodType)` help gameplay code.
- `Config/LevelConfig_01.asset` is a sample level with 3 customers per queue; the first order of each queue matches the demo counter (left 4 foods, right 6 foods); its 4 x 3 board matches the demo ingredient supply.

Level Editor: open **Tools > FoodPop > Level Editor** or press **Mở Level Editor** on a LevelConfig asset. It edits both customer queues (add/move/remove customers and foods with icons), the raw ingredient board by columns (rows/columns editable, *Điền bảng theo tổng order*, *Xoá bảng*) and shows a check table of ordered vs board totals per food (Đủ / Thiếu / Dư). All edits support Undo.

- `FoodType` (Scripts/gamebase/FoodType.cs) định danh 12 loại đồ ăn; giữ nguyên giá trị số cũ khi thêm loại mới. `None` không phải đồ ăn thu thập.
- `OrderFood` (Scripts/gamebase/OrderFood.cs) là một món trong order: `foodType` và `requiredCount` (tối thiểu một).
- `CustomerOrder` (Scripts/gamebase/CustomerOrder.cs) là order của một khách: `foods` là danh sách `OrderFood`, mỗi loại một dòng. `GetRequiredCount(FoodType)` trả về 0 với món không yêu cầu.
- `LevelConfig` (Scripts/gamebase/LevelConfig.cs, **Assets > Create > Hot Pot > Level Config**) chứa các khách cần phục vụ ở hai hàng trên quầy order: `leftQueue` và `rightQueue`, mỗi hàng là danh sách `CustomerOrder`; phần tử 0 được phục vụ trước. `TotalCustomers` và `GetQueue(0 = trái, 1 = phải)` hỗ trợ code gameplay.
- `LevelConfig` còn có bảng nguyên liệu thô: `boardRows` x `boardColumns` (chỉnh được) và `ingredientBoard`, danh sách `BoardColumn` (trái sang phải), mỗi cột có `foods` là danh sách `OrderFood` từ trên xuống dưới (list lồng list). Mỗi cột tự giữ đúng `boardRows` nguyên liệu. `GetBoardCell(column, row)` và `GetBoardCount(FoodType)` hỗ trợ code gameplay.
- `Config/LevelConfig_01.asset` là level mẫu với 3 khách mỗi hàng; order đầu tiên của mỗi hàng khớp với quầy trong demo (trái 4 món, phải 6 món); bảng 4 x 3 khớp với khay nguyên liệu trong demo.

Level Editor: mở **Tools > FoodPop > Level Editor** hoặc bấm **Mở Level Editor** trên asset LevelConfig. Công cụ cho phép chỉnh hai hàng khách (thêm/di chuyển/xoá khách và món, có icon), bảng nguyên liệu thô theo cột (chỉnh số hàng/cột, *Điền bảng theo tổng order*, *Xoá bảng*) và bảng kiểm tra tổng order cần so với bảng có theo từng loại (Đủ / Thiếu / Dư). Mọi thao tác đều Undo được.

2026-10-16: Avocado removed. ID 8 retired; remaining IDs unchanged. Existing avocado order/board/scene entries now use FriedTofu with original quantities. Active set: 13 ingredients / 26 state sprites. Avocado PNG/meta backups are outside Assets in output/HotPotDemo/Archive/remove-avocado-20261016. Level Editor resolves enum indices separately from numeric IDs.

FoodBoardManager now owns the explicitly assigned column references, icon table, dish creation/reuse and layout. It is a scene-scoped singleton accessed through FoodBoardManager.Instance, attached to Conveyor_Food. LevelController retains level selection and delegates config.ingredientBoard to LoadIngredientBoard. / FoodBoardManager quản lý reference cột, bảng icon, tạo/tái dùng đĩa và bố cục; singleton theo scene nằm trên Conveyor_Food. LevelController chỉ nạp level và gọi manager xử lý board.

ChefManager owns chef preparation jobs and conveyor slot availability. Access it through ChefManager.Instance. / ChefManager quản lý công việc sơ chế của đầu bếp và tình trạng slot băng chuyền.
- References: chefAnimator and conveyorMovement, assigned in HotPotDemo. Slots reads the movement component's current list; no duplicate slot array.
- APIs: GetEmptySlotCount(), TryGetNearestEmptySlot(position, out slot), ContainsSlot(slot), TryPrepareSlot(slot), CancelPreparation(), SetConveyorMoving(bool), IsBusy and CurrentFood.
- TryPrepareSlot is called explicitly by gameplay code. It accepts only a raw Food on an active owned slot, runs IsChopping for 1.5 seconds, then calls Food.Prepare() if that slot still holds the same food. It does not automatically pick food, fill slots, or select a job.
- CancelPreparation and disabling the manager cancel unfinished work without preparing the food. The manager uses a scene-scoped singleton and releases its registration on destruction.
- ConveyorChefMovement retains spline construction, slot creation/placement, movement and rendering order. Slot occupancy queries now live on ChefManager.
- Play Mode validation: singleton startup, 12 slots, occupied/foreign/full slot handling, concurrent/prepared-food rejection, preparation completion and icon switch, cancellation and disable cleanup. Returned to Edit Mode without persisting test objects.

Ingredient dishes use IngredientDishPool owned by FoodBoardManager. The manager caches its grid and visual sorting once, adopts the existing scene dishes, returns board dishes before loading another level, then rents only occupied cells. New instances come from the assigned IngredientDish prefab only when there are no reusable dishes. ReleaseDish(food) returns a consumed owned dish, rejecting duplicate/foreign returns. Dishes already moved outside board columns stay with their current owner until explicitly returned. Returns reset Raw state, food type, amount, icon and visibility; rents restore position, scale, rotation, icon, quantity and sorting. Pool storage is inactive; owned instances are destroyed only on manager disposal. / Board dùng pool đĩa dùng chung, tái sử dụng cả đĩa scene, trả đĩa khi đổi level và chỉ tạo mới khi hết đĩa. Dùng ReleaseDish để trả đĩa đã dùng; bên quản lý slot phải bỏ tham chiếu trước khi trả đĩa.
Play Mode checks: initial 20-cell level reuses 12 scene dishes and creates 8; repeat load creates 0; shrinking to 6 returns 14; expanding to 25 creates only 5 more; repeated expansion creates 0; empty cells rent nothing; returned state/quantity/icons/sorting and foreign/duplicate return guards verified. Editor scene restored after testing.

Board layout: first row dishes spawn at column-local (0, 0, 0); later rows use negative local Y with the initial row spacing. FoodBoardManager.columnSpacing controls horizontal spacing in the first column parent's local units, anchored at the first column. Scene value: 1.6. Column origins were moved to their first dish while preserving the existing editor visuals. / Đĩa hàng đầu có tọa độ cục bộ bằng không; columnSpacing điều chỉnh khoảng cách ngang giữa các cột, giữ cột đầu làm mốc.

Selectable dishes: FoodBoardManager.RefreshSelectableDishes highlights the highest active nonempty dish per assigned column. DishSelectionOutline displays a white outline derived from the shared plate alpha and keeps the selected dish bright; waiting plates and ingredients use inactiveBrightness=0.75 while quantity UI remains unchanged. Refresh runs after load/release and in LateUpdate to handle depleted/disabled/reparented dishes. Pool returns clear selection; moving food onto ConveyorSlot clears the board outline and restores full brightness. Runtime checks passed for promotion, pool reuse and transfer. / Đĩa đầu cột có viền trắng; đĩa và thức ăn phía sau tối nhẹ, số lượng giữ nguyên độ sáng.
Supply board: four equal-width lanes and five identically thin rails, normalized from a generated cartoon board. Artwork repeats the same lane and rail segments and aligns centers to the current columnSpacing (1.5 at update time). Current texture widths: lane 312px, rail 13px, pitch 325px. Source/notes: output/HotPotDemo/SupplyBoard/UniformLanes. / Bốn làn có cùng chiều rộng, thanh ngăn trong và viền ngoài có cùng độ dày mảnh.

## Final food orders — 2026-10-19 / Order theo món hoàn chỉnh

Customers now order one final dish instead of loose ingredients. / Khách giờ order một món hoàn chỉnh thay vì từng nguyên liệu lẻ.

- `FinalFoodType` (Scripts/gamebase/FinalFoodType.cs): 40 stable IDs, value = dish line x 10 + ingredient count (e.g. `SakeNigiri3 = 13`). / 40 ID cố định, giá trị = dòng món x 10 + số nguyên liệu.
- `FinalFoodConfig` (Scripts/gamebase/FinalFoodConfig.cs, `Config/FinalFoodConfig.asset`): each final dish lists 2–6 distinct ingredients (one of each) and its sprite from `Art/Sprites/Foods/Sushi/Final/<count>/<slug>-nigiri.png`. Full table: `Documentation/FinalFoods.md`. Fill sprites with **Tools → HotPot → Build Final Food Config** (or ⋮ → *Tự điền công thức mặc định*). / Mỗi món có 2–6 nguyên liệu khác nhau và ảnh món; bảng đầy đủ ở FinalFoods.md.
- `CustomerOrder` now holds only `finalFood`. `CustomerManager` reads the recipe from `FinalFoodConfig`, shows the dish picture on the card (`CustomerView.finalFoodRenderer`), one line per ingredient (count badge hidden when 1), lays the lines out in a grid of 3 columns beside the picture, and pops the dish when every ingredient is delivered. / CustomerOrder chỉ còn `finalFood`; thẻ order hiện ảnh món bên trái, nguyên liệu xếp lưới 3 cột bên phải, đủ nguyên liệu thì ảnh món nảy lên.
- Card layout values (position/size of the dish picture, grid centre, spacing, columns, line scale) are on `CustomerView` under *Thẻ order món hoàn chỉnh*. / Thông số bố cục thẻ nằm trên CustomerView.
- **Tools → HotPot → Setup Customers And References** now also gives every order card 6 lines (duplicating the last line), creates the `FinalFood` renderer on each card and binds `FinalFoodConfig` to `CustomerManager`. / Công cụ setup giờ bổ sung đủ 6 dòng cho mỗi thẻ, tạo renderer FinalFood và gán FinalFoodConfig.
- Level Editor: each customer picks a final dish (picture + ingredient icons); the ingredient check and *Điền bảng theo tổng order* sum recipe ingredients. / Level Editor chọn món hoàn chỉnh cho từng khách; phần kiểm tra và tự điền bảng tính theo công thức.
- `LevelConfig_01`: left [SakeNigiri3], [MaguroNigiri4 + TamagoNigiri2], [EbiNigiri5]; right [UnagiNigiri4], [KanikamaNigiri6]; 4 x 5 board matches the 24 ingredients exactly (wasabi cell hidden). Previous version: `output/final-food-migration/LevelConfig_01.before.asset`. / Level mẫu đã đổi sang món hoàn chỉnh, bảng 4 x 5 khớp đúng 24 nguyên liệu; bản cũ lưu ngoài Assets.

### Order card shows every dish — 2026-10-19 / Thẻ order hiện mọi món

- `CustomerOrder.finalFoods` lists up to `CustomerOrder.MaxDishes` (4) dishes, all shown on the card at once as full-colour dish pictures (one `OrderItemView` slot per dish in a 2 x 2 grid centred on the card, a lone last dish centred in its row; grid centre, spacing and scale on the `OrderView` component of *Left order* / *Right order*, applied live in the editor, picture size `OrderItemView.dishSizeRatio`). Ingredient lines are no longer shown. / Thẻ order hiện ảnh tất cả món khách gọi (tối đa 4), màu gốc, không hiện nguyên liệu.
- A prepared ingredient on the belt flies to the first-arrived customer with a dish still missing it (earliest dish on the card first). The dish pops on every ingredient and gets a checkmark when complete; when all dishes are done the customer gets hearts and leaves. / Nguyên liệu bay tới món còn thiếu nó; món nảy nhẹ mỗi lần nhận, đủ thì có dấu tích; xong mọi món thì khách rời đi.
- Setup Customers adds `OrderView` to each order card, removes the old `FinalFood` / `DishCount` children, keeps exactly 4 slots per card (extra slots deleted) and binds the card to its CustomerManager lane (`Lane.orderView`). / Công cụ setup xoá object cũ và giữ đúng 4 ô mỗi thẻ.
- Level Editor: "+ Món" adds a dish (disabled at 4). The dish dropdown lists every dish with its picture, name, ID and ingredient icons and has a search box (dish or ingredient name; Enter picks the first match). Clicking the big picture opens the 8 x 5 picture grid. / Dropdown chọn món có ảnh, tên, nguyên liệu và ô tìm kiếm; bấm ảnh lớn để mở bảng hình.
- Dish pictures on the order card have a drop shadow (`DishShadow` child of each slot icon, created by Setup Customers): same sprite in translucent black behind the dish; tune `shadowOpacity` and `shadowOffset` (slot units, independent of picture size) on `OrderItemView`. / Ảnh món trên thẻ có bóng đổ; chỉnh `shadowOpacity`, `shadowOffset` trên OrderItemView.

### Order card, serving tray and 3-lane board — 2026-10-19 (afternoon) / Thẻ order, khay phục vụ và bàn 3 làn

- `OrderView` layout: 1–2 dishes stacked in one column, 3–4 dishes in two columns; unused slots are hidden. The `background` (the card's *Artwork*, sliced) only changes width to fit the columns (`dishCellSize`, `backgroundPadding`), keeping its height; it is hidden for an empty order. / 1–2 món xếp dọc một cột, 3–4 món hai cột; nền thẻ chỉ đổi chiều rộng theo số cột.
- `ServingTrayView` (`Assets/HotPot/Scripts/Customer`): each lane has an empty serving tray; `SetSlotCount` creates one dashed `DishSlot` (`Assets/HotPot/Prefabs/Ingame/DishSlot.prefab`, outline sprite `ServingTraySlotOutline.png`) per final food of the current order inside `usableArea`, 2 per row. `CustomerManager.Lane.servingFood` sets the count when an order is set up and clears it on level load / when the lane runs out of customers. Dishes are not placed on the tray yet. / Mỗi làn có khay trống, mỗi món trong order một ô viền nét đứt; chưa đặt món lên khay.
- Setup Customers binds `OrderView.background` to the card's *Artwork* and matches the two `ServingTrayView`s to the lanes left to right (keeps an existing link when it cannot match).
- `FoodBoardManager` centres the active columns and swaps the table image between 3-lane (`Assets/HotPot/Art/Sprites/Demo/IngredientTableStandalone3Lane.png`) and 4-lane. `LevelConfig_01` now uses a 3-column board: left [SakeNigiri2], [MaguroNigiri2 + SakeNigiri3], [MaguroNigiri3]; right [EbiNigiri2], [TamagoNigiri3].

### Ingredients stack on the serving tray — 2026-10-19 / Nguyên liệu xếp chồng trên khay

- Prepared ingredients now fly from the belt to the dish's **serving-tray slot** (not to the order card). Rule: the **first ingredient of the recipe (the base, SushiRice for every current dish) must come first**; any other recipe ingredient is accepted once the base is on the dish or flying to it, in any order. An ingredient no dish can take yet waits on the belt and flies as soon as a base is sent (first-arrived customer, earliest dish on the card first). / Nguyên liệu bay tới ô khay của món; nguyên liệu đầu tiên của công thức (đế) phải tới trước, các nguyên liệu sau thứ tự nào cũng được; chưa có món nhận thì nằm chờ trên băng.
- `TrayDishStack` (`Assets/HotPot/Scripts/Customer`) is a **component on each serving tray ("Serving food")**, next to `ServingTrayView`; it is reached through `CustomerManager.Lane.servingFood` → `ServingFood` → `ServingTrayView.DishStack` (serialized on the `ServingHotPot` prefab). When a tray has no TrayDishStack the slots are still created and ingredients fly to the order card. It prepares the slots through `ServingTrayView.SetSlotCount`, and its Inspector shows, while playing, each slot's dish, its layers bottom up and whether it is finished. Pieces stack in recipe order whatever their arrival order (later layers are lifted when an earlier layer lands); every flight re-aims each frame to its stack position. When every ingredient is in, the pieces squeeze to the slot centre, `dishCompleteEffect` (FoodPreparedEffect) plays and the finished dish picture pops in on the slot; the order card dish gets its checkmark. Finished dishes stay on the tray until the whole order is assembled; they are pooled when the customer has left. / Đủ nguyên liệu thì chồng gộp thành ảnh món hoàn chỉnh trên ô; món nằm trên khay tới khi khách xong.
- Tuning on each tray's `TrayDishStack`: `foodScale` = local scale of every piece inside the slot (× optional per-ingredient `foodScaleOverrides`; prepared sprites are 1.96 units and the slot outline is 1 unit, so ~0.45 fills the slot), `stackStepRatio` (rise per layer, ratio of the slot size); the finished dish picture is fitted to the slot. Ingredient flight and landing settings (`serveFlyDuration`, `serveArcHeight`, `servePeakScale`, `serveEndScale`, `serveSortingBoost`, `landPopDuration`, `landPopAmount`) are configured in `IngameConfig.ingredientFlight`; managers read the shared asset. / Các thông số bay nguyên liệu và nảy khi đáp được chỉnh trong nhóm `ingredientFlight` của `IngameConfig`; các manager chỉ đọc asset chung. Dish assembly effects (`dishPopDuration`, `dishPopAmount`, `assembleDelay`, `mergeDuration`, `dishCompleteEffect`) are configured in `IngameConfig.dishAssembly`; the dish-complete particle reference is on each `ServingFood`; the customer heart effect remains on `CustomerManager`. `ServingTrayView` exposes `SortingLayerId` and `StackSortingOrder` (tray order + 2). Without a tray, ingredients fall back to flying to the order card.


### Per-customer serving ownership / Phục vụ riêng từng khách

See ServingFood.md: each serving prefab owns its flights, assembly, effects and sprite pool through its own ServingFood. CustomerManager owns queues, recipe reservations and order progress. / Xem tài liệu ServingFood: mỗi prefab phục vụ tự quản lý chuyến bay, ghép món, hiệu ứng và pool; CustomerManager quản lý hàng khách, giữ chỗ công thức và tiến độ order.
