# IngameConfig

EN: Edit shared gameplay timing and animation settings in `Assets/HotPot/Config/Resources/IngameConfig.asset`. Food instances read IngameConfig.Instance directly and have no serialized configuration fields. Other components retain existing Resolve(assigned) timing overrides.

VI: Chỉnh thời gian và hoạt cảnh màn chơi dùng chung tại `Assets/HotPot/Config/Resources/IngameConfig.asset`. Food đọc trực tiếp IngameConfig.Instance, không còn field serialize cấu hình riêng. Các component khác vẫn giữ cơ chế ghi đè thời gian hiện có qua Resolve(assigned).

| Group | Purpose / Chức năng |
| --- | --- |
| ingredientFlight | Belt-to-tray flight and landing pop / Bay từ băng chuyền tới khay và nảy khi đáp |
| boardMotion | Board column shift timing / Thời gian dồn cột board |
| boardFlight | Board-to-belt arc, curve, scale and spin / Vòng cung, đường cong, phóng thu và xoay khi bay từ board lên băng |
| dishAssembly | Merge timing and completed-dish pop / Thời gian ghép và nảy món hoàn chỉnh |
| dishOrderFlight | Completed dish flight to order / Món hoàn chỉnh bay vào order |
| customers | Customer movement and queue pacing / Chuyển động khách và nhịp hàng chờ |
| preparation | Chef lifting, dropping and chop effects; action duration comes from ChefAnims animation references / Nhấc, thả và hiệu ứng chặt; thời gian sơ chế lấy từ animation reference trên ChefAnims |
| orderPop | Independent order image and checkmark pops / Nảy độc lập ảnh món và dấu tích trên order |
| iceHit | Frozen dish damage pop / Nảy khi đĩa đóng băng chịu sát thương |

EN: Slot size, image and colour stay on `DishSlot.prefab`. Scene layout, renderer/object references, conveyor path, recipe data and tray stack sizing stay on their respective components/assets. Runtime code reads the config without writing gameplay state into it. Migrated values preserve the current scene tuning, including board flight duration, portion delay, scale peaks and queue spacing.

VI: Kích thước, ảnh và màu ô vẫn nằm trên `DishSlot.prefab`. Bố cục scene, tham chiếu đối tượng/bộ hiển thị, đường băng chuyền, công thức món và kích thước chồng trên khay giữ tại component/asset tương ứng. Mã lúc chạy chỉ đọc cấu hình, không ghi trạng thái chơi vào asset. Giá trị chuyển đổi giữ thông số scene hiện tại, gồm thời gian bay từ board, độ trễ từng phần, mức phóng lớn và khoảng cách hàng chờ.

## Bảng nguyên liệu chung — 03/11/2026

- Gán FoodIconConfig.asset một lần vào **Food Icons** trên Config/Resources/IngameConfig.asset. Runtime và Editor đọc qua IngameConfig.Instance.FoodIcons; Resources.Load chỉ chạy khi cache chưa có asset.
- Food không còn field IngameConfig/FoodIconConfig; board, OrderView và FoodHoldingStorage cũng không giữ bảng riêng. Icon thô/sơ chế, nhóm sơ chế, âm thanh và vụn cùng đọc một bảng. Edit Mode chỉ đọc asset, không phụ thuộc manager runtime.
- Bỏ Food.SetIconConfig() và property IconConfig của Food/board. ConveyorSlot.TryFillFood(type), DishPickBatch.TryFill(...) và FoodHoldingStorage.Configure(...) không nhận bảng icon nữa.
- Đã kiểm tra trong Unity: cache trả cùng asset, liên kết đúng bảng hiện có, Food không còn hai field serialize, icon Raw/Prepared và fill slot không cần tham số config. Chưa chạy trọn level trong Play Mode sau refactor.

09/11/2026: Thêm nhóm **Pre Win** (`preWin.speedMultiplier`, mặc định 1.3, khoảng 1–2): hệ số nhân `Time.timeScale` khi số phần món còn trên board ≤ số slot trống trên băng chuyền. Asset hiện có chưa lưu giá trị nên dùng mặc định 1.3 cho tới khi chỉnh trong Inspector. Logic trong `LevelController`; chi tiết ở claude/UI.md mục Pre-win.

04/11/2026: Thời gian sơ chế không còn đặt tại preparation.preparationDuration. Gán AnimationReferenceAsset cho từng FoodType trên ChefAnims; runtime đọc Animation.Duration. Animation và thời gian không thuộc FoodIconConfig. Xem claude/Conveyor.md để biết mapping và kiểm tra hiện tại.
