# HotPotDemo — world objects / Vật thể world

Open `Assets/HotPot/Scenes/HotPotDemo.unity` in a 9:16 Game view. The scene is visual only; no gameplay controller or button callbacks are attached.

Physical content uses ordinary Transform and SpriteRenderer, outside Canvas: customers and waiting queues, chef, dishes and food, order cards, counters, conveyor, rack and boosters. Environment surfaces are separate world objects. The serving pots are separate prefab instances rather than baked into the counter.

Only numeric badges and text use world-space UI. All text is TextMeshProUGUI; there is no legacy UI Text. Quantity canvases belong to the corresponding world object and use a 0.01 local scale for 100 pixels per unit.

Reusable assets are in `Assets/HotPot/Prefabs/Demo`:
- `IngredientDish.prefab`: 12 belt instances and 12 supply instances; optional ingredient and quantity children.
- `OrderIngredient.prefab`: 10 recipe chips (left 4, right 6).
- `Customer.prefab`: 2 active customers and 4 waiting customers, with portrait overrides.
- `ServingHotPot.prefab`: 2 serving pots.
- `Quantity.prefab`: nested count badge and TMP label.
- `Booster.prefab`: 4 booster artwork objects.
- `ConveyorStandalone.prefab`: grey belt with thick red raised rim (`ConveyorStandalone.png`, order 0) plus a `Front` child (`ConveyorStandaloneFront.png`, order 66) holding the front inner rim so it draws over the chef (order 65). Both sprites share the same canvas and centre pivot.

Conveyor movement: `ConveyorChefMovement` (Scripts/gamebase) on `Conveyor_Chef` needs a `SplineContainer` (com.unity.splines). The inspector button *Setup* fits a closed spline to the belt centre of ConveyorStandalone, creates 12 `Conveyor_Slot` prefab instances as children and snaps them evenly on the path; at runtime slots move at `speed` world units/s, and every slot sprite uses the conveyor's sorting layer with orders from `slotSortingOrder` (default 70) so slots always draw above the belt, its Front layer (66) and the chef (65).

Edit the scene or prefab instances directly. The retired migration and UI builders are removed after conversion. ChefAnimated.prefab now provides Idle/Chop visual animation via IsChopping; no gameplay logic is attached. Camera uses an orthographic size of 8; sprite content uses layer 30. The preview is exported to `output/HotPotDemo/preview.png`.

Mở scene ở Game view dọc 9:16. Scene chỉ dựng hình, chưa có controller gameplay hoặc callback nút bấm.

Nội dung vật thể dùng Transform thường và SpriteRenderer, nằm ngoài Canvas: khách và hàng chờ, đầu bếp, đĩa và nguyên liệu, thẻ order, quầy, băng chuyền, khay và booster. Các bề mặt môi trường được tách thành vật thể riêng. Hai nồi phục vụ là instance prefab riêng, không nằm trong ảnh quầy.

Chỉ nhãn số lượng và chữ dùng UI world-space. Toàn bộ chữ dùng TextMeshProUGUI, không còn UI Text cũ. Canvas số lượng nằm trong vật thể tương ứng, scale 0.01 theo tỷ lệ 100 pixel mỗi world unit.

Prefab dùng chung nằm trong `Assets/HotPot/Prefabs/Demo`: đĩa (24 instance), nguyên liệu order (10), khách (6), nồi phục vụ (2), số lượng và booster (4). Chỉnh trực tiếp scene hoặc prefab instance. Các công cụ chuyển đổi và dựng UI cũ đã được dọn. Băng chuyền gồm lớp nền xám viền đỏ dày (order 0) và object con `Front` (order 66) chứa viền trong phía trước để che lên đầu bếp (order 65); hai sprite cùng kích thước và tâm. ChefAnimated.prefab hiện có animation Idle/Chop qua bool IsChopping, chưa nối logic gameplay. Camera orthographic size 8, sprite dùng layer 30.

Chuyển động băng chuyền: `ConveyorChefMovement` (Scripts/gamebase) gắn trên `Conveyor_Chef`, cần `SplineContainer` (package com.unity.splines). Nút *Setup* trong Inspector dựng spline khép kín theo tâm mặt băng ConveyorStandalone, tạo 12 prefab `Conveyor_Slot` làm con và chia đều lên đường đi; khi chạy, slot di chuyển với `speed` đơn vị/giây, mọi sprite của slot dùng cùng sorting layer với băng chuyền và order từ `slotSortingOrder` (mặc định 70) để luôn nằm trên băng, lớp Front (66) và đầu bếp (65).
