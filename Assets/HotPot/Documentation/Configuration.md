# HotPot configuration / Cấu hình HotPot

## Sushi ingredient replacement — 2026-10-16

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
| 11 | Tofu | Sesame |
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
