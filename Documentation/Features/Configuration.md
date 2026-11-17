# Cấu hình HotPot

## Nguyên liệu (`FoodType`)
- `Scripts/gamebase/FoodType.cs`; giữ cố định giá trị số, không tái sử dụng ID đã bỏ (8, 11). `None` không phải đồ ăn.
- Bộ sushi: 1 SushiRice, 2 Salmon, 3 Tuna, 4 Shrimp, 5 CrabStick, 6 Tamagoyaki, 7 Cucumber, 9 SalmonRoe, 10 Unagi, 12 Octopus, 13 Seaweed, 14 Scallop, 15 SoySauce, 16 PickledGinger, 17 Wasabi, cùng các loại thêm sau (Avocado, Edamame, Ice, Salt, gà…) — danh sách chuẩn là enum và `FoodIconConfig.asset`.
- Sprite: `Art/Sprites/Foods/Sushi/Raw` và `/Prepared` (196×196, pivot giữa, 100 PPU); Seaweed/SoySauce/PickledGinger/Wasabi dùng chung một sprite tại `Sushi/Shared`. `FoodIconConfig.FoodIcon.sprite` = Raw, `preparedSprite` = Prepared; `GetIcon(type, state)` rơi về Raw nếu thiếu Prepared.
- Bảng icon dùng chung qua `IngameConfig.Instance.FoodIcons`, xem [IngameConfig.md](IngameConfig.md).
- Nguyên liệu không phải hải sản: trà xanh gồm TeaLeaves (lá trà khô) và Ice, ảnh ở `Art/Sprites/Foods/Drinks` (khay dùng `Plate_Tea` khi prefab món có lớp TeaLeaves); Edamame và Salt, ảnh ở `Art/Sprites/Foods/Edamame`. Ice, Salt có `skipPreparation`, không qua đầu bếp. Menu `Setup Green Tea` / `Setup Edamame` chỉ đăng ký icon nguyên liệu trong FoodIconConfig.

## Level (`LevelConfig`)
- `Scripts/gamebase/LevelConfig.cs`, asset `Config/Resources/Levels/LevelConfig_NN.asset`, nạp theo tên bằng `LevelConfig.Load(n)`; `levelNumber` (≥ 1) phải khớp tên file và hiển thị qua `UILevel`.
- `customerQueue`: danh sách khách cấp lần lượt cho hai chỗ trên quầy.
- Board: `boardRows` × `boardColumns`, `ingredientBoard` là danh sách `BoardColumn` (trái → phải), mỗi cột `foods` từ trên xuống; `GetBoardCell`, `GetBoardCount(FoodType)`. Ô có thể ẩn/đóng băng (`OrderFood.isHidden`, `isFrozen`, `blockValue`), đĩa đôi và `dishLinks` — xem [LevelEditor.md](LevelEditor.md).
- **Độ khó**: `difficultyMode` (`Normal=0`, `Hard=1`, `Easy=2`) do tác giả đặt, runtime không suy từ số màn. Hiện Hard: 15, 20, 25, 30. `ForRetry` dùng bản Hard khi retry < 2, từ lần 2 nạp `LevelConfig_NN_Easy` (cùng `levelNumber`); thiếu bản Easy thì giữ Hard.
- `LevelController` cung cấp `CurrentLevel`, phát `LevelLoaded` sau khi nạp; chơi lại/màn tiếp không đổi tiến trình hồ sơ.

## Khách (`CustomerOrder`)
| Field | Ý nghĩa |
| --- | --- |
| `customerType` | Normal = 0, Boss = 1, Shipper = 2 |
| `timeServe` | Giây (mặc định 30), chỉ dùng cho Boss/Shipper; hết giờ → popup hồi sinh OutOfTime ([CustomerManager.md](CustomerManager.md)) |
| `ingredientOrders` | Tối đa `CustomerOrder.MaxDishes` = 4 nhóm, mỗi nhóm 1–`IngredientOrder.MaxIngredients` (6) nguyên liệu; mục đầu là đế, mục lặp giữ số lượng |
| `IngredientOrder.dish` | Prefab `FinalFoodView` tùy chọn làm ảnh món trên thẻ và khay; không có thì nhóm không có hình |

Luật phục vụ, xếp món trên khay và định nghĩa món (`FinalFoodView`): [ServingFood.md](ServingFood.md).

## Hệ thống liên quan
- Board nguyên liệu (pool đĩa, bố cục, chạm, đóng băng): [FoodBoardManager.md](FoodBoardManager.md).
- Băng chuyền và đầu bếp (sơ chế, Spine đầu bếp, sức chứa): [ConveyorChef.md](ConveyorChef.md).
- Hàng khách, Spine khách Normal, đồng hồ Boss/Shipper: [CustomerManager.md](CustomerManager.md).

## Scene HotPotDemo
- Scene chơi chính `Assets/HotPot/Scenes/HotPotDemo.unity` (tên "Demo" còn từ giai đoạn prototype), Game view dọc 945:2048.
- Vật thể gameplay (khách, đầu bếp, đĩa, món, thẻ order, quầy, băng chuyền, khay, booster) dùng Transform + SpriteRenderer ngoài Canvas. Chỉ nhãn số lượng/chữ dùng UI world-space (TextMeshProUGUI, canvas scale 0.01 = 100 px/unit).
- Băng chuyền `ConveyorStandalone`: lớp nền (order 0) và con `Front` (order 66) che lên đầu bếp (order 65). Slot băng vẽ từ `slotSortingOrder` (70). Chuyển động và số slot: xem [ConveyorChef.md](ConveyorChef.md).
- Prefab dùng chung ở `Assets/HotPot/Prefabs/Ingame` và `Prefabs/Demo`. Chỉnh trực tiếp scene/prefab; không có tool Setup dựng prefab.
- Camera dùng `Scripts/UI/SafeAreaCamera.cs`; luật fit hai vùng: xem [UI.md](UI.md#camera-và-hud).

## Scene Home
- `Home.unity`: Canvas Overlay (CanvasScaler 1080×1920), EventSystem Input System.
- `GreetingText` "Chào ngày mới" (Fredoka Color, fallback `LiberationSans SDF - Fallback` cho glyph tiếng Việt).
- `DateText`, `TomorrowText`, `DaysInMonthText` dùng `Scripts/UI/CurrentDateText.cs`: `displayMode` FullDate (thứ tiếng Việt + dd/MM/yyyy, `dayOffset`, `prefix`) hoặc DaysInMonth; tính trong `OnEnable`, không polling (không tự đổi qua nửa đêm).
- `RedCircle` (`Art/Sprites/UI/RedCircle.png`, 300×300) với `Scripts/UI/PulseScale.cs`: scale 1 → 1.2 theo cosin, chu kỳ 1 s, unscaled; trả scale gốc khi tắt.

**Kiểm chứng:** Home đã kiểm tra Play Mode qua Unity MCP (13/11); chưa xem trên thiết bị.

## Padding sprite

159 ảnh từng được nới kích thước trong commit `a2b7ac3` đã được tạo lại từ ảnh trước commit, thêm viền RGBA=0 để chiều rộng/cao chia hết cho 4; không nhân bản pixel mép và không resize nội dung. Giữ nguyên GUID, PPU và chế độ Single/Multiple hiện có.

- 148 sprite Single dùng pivot Custom đã bù phần thêm: `(offset + pivotGốc × kíchThướcGốc) / kíchThướcMới`, với offset Y tính từ đáy. Nội dung giữ vị trí so với Transform; bounds và kích thước native tăng 1–3 px. Các chỗ fit theo bounds, SetNativeSize hoặc preserveAspect vẫn cần kiểm tra trực quan. Border 9-slice có sẵn được cộng phần padding tương ứng; ảnh không dùng slice giữ border bằng 0.
- 11 ảnh Multiple giữ nguyên kích thước rect và dịch rect theo padding trái/đáy, giữ ID của từng sprite. Rect `Ingame/dot_red` bị bỏ sót ở commit cũ đã được dịch đúng.
- Khi thêm ảnh mới, không chạy lại script padding nhân bản mép cũ trong `output/sprite-pad-backup/`.

Kiểm chứng 17/11/2026: so sánh lại ảnh đã lưu của cả 159 file, pixel gốc giữ nguyên từng byte và mọi pixel thêm đều RGBA=0; xác nhận GUID không đổi, pivot Single bảo toàn gốc tọa độ nội dung và 11 rect Multiple khớp ảnh gốc. Unity đã import mẫu `edamame_3` thành công ở chế độ Single, kích thước 172×112 và pivot pixel (85.5, 56.5). `git diff --check` không có lỗi whitespace. Chưa kiểm tra hình ảnh toàn bộ scene/popup và chưa build Android.
