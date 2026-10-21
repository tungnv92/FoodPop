# IngameConfig

EN: Edit shared gameplay timing and animation settings in `Assets/HotPot/Config/Resources/IngameConfig.asset`. Scene components and reusable food/order prefabs reference the same asset. Components without an explicit reference load this default Resources asset.

VI: Chỉnh thời gian và hoạt cảnh màn chơi dùng chung tại `Assets/HotPot/Config/Resources/IngameConfig.asset`. Các component trong scene và prefab nguyên liệu/order dùng cùng asset. Component chưa gán tham chiếu sẽ đọc asset Resources mặc định này.

| Group | Purpose / Chức năng |
| --- | --- |
| ingredientFlight | Belt-to-tray flight and landing pop / Bay từ băng chuyền tới khay và nảy khi đáp |
| boardMotion | Board column shift timing / Thời gian dồn cột board |
| boardFlight | Board-to-belt arc, curve, scale and spin / Vòng cung, đường cong, phóng thu và xoay khi bay từ board lên băng |
| dishAssembly | Merge timing and completed-dish pop / Thời gian ghép và nảy món hoàn chỉnh |
| dishOrderFlight | Completed dish flight to order / Món hoàn chỉnh bay vào order |
| customers | Customer movement and queue pacing / Chuyển động khách và nhịp hàng chờ |
| preparation | Chef preparation, lifting, dropping and chopping / Sơ chế, nhấc, thả và chặt nguyên liệu |
| orderPop | Independent order image and checkmark pops / Nảy độc lập ảnh món và dấu tích trên order |
| iceHit | Frozen dish damage pop / Nảy khi đĩa đóng băng chịu sát thương |

EN: Slot size, image and colour stay on `DishSlot.prefab`. Scene layout, renderer/object references, conveyor path, recipe data and tray stack sizing stay on their respective components/assets. Runtime code reads the config without writing gameplay state into it. Migrated values preserve the current scene tuning, including board flight duration, portion delay, scale peaks and queue spacing.

VI: Kích thước, ảnh và màu ô vẫn nằm trên `DishSlot.prefab`. Bố cục scene, tham chiếu đối tượng/bộ hiển thị, đường băng chuyền, công thức món và kích thước chồng trên khay giữ tại component/asset tương ứng. Mã lúc chạy chỉ đọc cấu hình, không ghi trạng thái chơi vào asset. Giá trị chuyển đổi giữ thông số scene hiện tại, gồm thời gian bay từ board, độ trễ từng phần, mức phóng lớn và khoảng cách hàng chờ.
