# Cấu hình HotPot

## Nguyên liệu (`FoodType`)
- `Scripts/gamebase/FoodType.cs`; giữ cố định giá trị số, không tái sử dụng ID đã bỏ (8, 11). `None` không phải đồ ăn.
- Bộ sushi: 1 SushiRice, 2 Salmon, 3 Tuna, 4 Shrimp, 5 CrabStick, 6 Tamagoyaki, 7 Cucumber, 9 SalmonRoe, 10 Unagi, 12 Octopus, 13 Seaweed, 14 Scallop, 15 SoySauce, 16 PickledGinger, 17 Wasabi, cùng các loại thêm sau (Avocado, Edamame, Ice, Salt, gà…) — danh sách chuẩn là enum và `FoodIconConfig.asset`.
- Sprite: `Art/Sprites/Foods/Sushi/Raw` và `/Prepared` (196×196, pivot giữa, 100 PPU); Seaweed/SoySauce/PickledGinger/Wasabi dùng chung một sprite tại `Sushi/Shared`. `FoodIconConfig.FoodIcon.sprite` = Raw, `preparedSprite` = Prepared; `GetIcon(type, state)` rơi về Raw nếu thiếu Prepared.
- Bảng icon dùng chung qua `IngameConfig.Instance.FoodIcons`, xem [IngameConfig.md](IngameConfig.md).

## Level (`LevelConfig`)
- `Scripts/gamebase/LevelConfig.cs`, asset `Config/Resources/Levels/LevelConfig_NN.asset`, nạp theo tên bằng `LevelConfig.Load(n)`; `levelNumber` (≥ 1) phải khớp tên file và hiển thị qua `UILevel`.
- `customerQueue`: danh sách khách cấp lần lượt cho hai chỗ trên quầy.
- Board: `boardRows` × `boardColumns`, `ingredientBoard` là danh sách `BoardColumn` (trái → phải), mỗi cột `foods` từ trên xuống; `GetBoardCell`, `GetBoardCount(FoodType)`. Ô có thể ẩn/đóng băng (`OrderFood.isHidden`, `isFrozen`, `blockValue`), đĩa đôi và `dishLinks` — xem [LevelEditor.md](../../Handoff/LevelEditor.md).
- **Độ khó**: `difficultyMode` (`Normal=0`, `Hard=1`, `Easy=2`) do tác giả đặt, runtime không suy từ số màn. Hiện Hard: 15, 20, 25, 30. `ForRetry` dùng bản Hard khi retry < 2, từ lần 2 nạp `LevelConfig_NN_Easy` (cùng `levelNumber`); thiếu bản Easy thì giữ Hard.
- `LevelController` cung cấp `CurrentLevel`, phát `LevelLoaded` sau khi nạp; chơi lại/màn tiếp không đổi tiến trình hồ sơ.

## Khách (`CustomerOrder`)
| Field | Ý nghĩa |
| --- | --- |
| `customerType` | Normal = 0, Boss = 1, Shipper = 2 |
| `timeServe` | Giây (mặc định 30), chỉ dùng cho Boss/Shipper; hết giờ → popup hồi sinh OutOfTime ([CustomerManager.md](../../Handoff/CustomerManager.md)) |
| `ingredientOrders` | Tối đa `CustomerOrder.MaxDishes` = 4 nhóm, mỗi nhóm 1–`IngredientOrder.MaxIngredients` (6) nguyên liệu; mục đầu là đế, mục lặp giữ số lượng |
| `IngredientOrder.dish` | Prefab `FinalFoodView` tùy chọn làm ảnh món trên thẻ và khay; không có thì nhóm không có hình |

Luật phục vụ và xếp món trên khay: [ServingFood.md](ServingFood.md). Danh mục món: [FinalFoods.md](FinalFoods.md).

## Board (`FoodBoardManager`)
- Singleton theo scene trên `Conveyor_Food`; `LevelController` gọi `LoadIngredientBoard`.
- Pool đĩa `IngredientDishPool`: tái sử dụng đĩa scene, trả đĩa khi đổi level, chỉ tạo mới khi hết; `ReleaseDish(food)` từ chối trả trùng/lạ. Bên giữ slot phải bỏ tham chiếu trước khi trả.
- Bố cục: hàng đầu tại local (0,0,0), các hàng sau theo `rowSpacing`; `columnSpacing` (scene 1.6) neo ở cột đầu; cột active được căn giữa và ảnh bàn đổi theo số làn (`ingredientTable3/4/5LaneSprite`, gán tay).
- Đĩa chọn được (đầu cột) có viền trắng; đĩa phía sau tối `inactiveBrightness` 0.75, nhãn số giữ sáng.
- Chi tiết pick/sức chứa: [foodpop-FoodBoardManager.md](../../Handoff/foodpop-FoodBoardManager.md).

## Đầu bếp (`ChefManager`)
`ChefManager.Instance`, tham chiếu `ChefAnims` và `conveyorMovement`. Khi rảnh, bật `autoPrepareInStation` và không bị booster pause, `Update` chọn món qua `FindSlotToPrepare`. `IsBusy` tính cả lượt giữ đi thẳng. Chi tiết: [Conveyor.md](../../Handoff/Conveyor.md), [ChefAnimationFrames.md](ChefAnimationFrames.md).

## Scene Home
- `Home.unity`: Canvas Overlay (CanvasScaler 1080×1920), EventSystem Input System.
- `GreetingText` "Chào ngày mới" (Fredoka Color, fallback `LiberationSans SDF - Fallback` cho glyph tiếng Việt).
- `DateText`, `TomorrowText`, `DaysInMonthText` dùng `Scripts/UI/CurrentDateText.cs`: `displayMode` FullDate (thứ tiếng Việt + dd/MM/yyyy, `dayOffset`, `prefix`) hoặc DaysInMonth; tính trong `OnEnable`, không polling (không tự đổi qua nửa đêm).
- `RedCircle` (`Art/Sprites/UI/RedCircle.png`, 300×300) với `Scripts/UI/PulseScale.cs`: scale 1 → 1.2 theo cosin, chu kỳ 1 s, unscaled; trả scale gốc khi tắt.

**Kiểm chứng:** Home đã kiểm tra Play Mode qua Unity MCP (13/11); chưa xem trên thiết bị.
