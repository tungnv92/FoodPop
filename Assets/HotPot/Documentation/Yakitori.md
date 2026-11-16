# Xiên gà teriyaki / Teriyaki chicken skewers

VI: Các nguyên liệu: `ChickenThigh = 22` (Đùi gà), `TeriyakiSauce = 23` (Sốt teriyaki), `ChickenBites = 25` (Gà viên). Món `TeriyakiChickenSkewer = 112` dùng đúng `[ChickenBites, TeriyakiSauce]`, mỗi loại một phần; gà là nguyên liệu đế nên được phục vụ trước sốt. Giữ ID nguyên liệu hiện có; không tự sửa các level. Level đang dùng đùi gà cho món xiên cần thay nguyên liệu trên board bằng gà viên để cân bằng công thức mới.

EN: Ingredients are ChickenThigh (22), TeriyakiSauce (23) and ChickenBites (25). TeriyakiChickenSkewer (112) requires ChickenBites and TeriyakiSauce, one portion each, bites first. Existing ingredient IDs and saved level assets remain unchanged. Levels supplying drumsticks for this recipe need their board ingredient changed to chicken bites.

## Ảnh / Artwork

| Trạng thái / State | Đường dẫn tương đối từ Foods/Yakitori / Relative path |
| --- | --- |
| Gà thô: đùi gà nguyên chiếc có xương / Raw whole drumstick with bone | Raw/chicken-thigh.png |
| Đùi gà sau sơ chế: đùi nướng nguyên chiếc / Prepared roasted drumstick | Prepared/chicken-thigh.png |
| Gà viên thô: các miếng gà cắt khối / Raw chicken bites | Raw/chicken-bites.png |
| Gà viên sau sơ chế: ảnh xiên gà trước đây / Prepared bites reuse the existing skewer | Prepared/chicken-bites.png |
| Chai sốt, dùng chung hai trạng thái / Sauce bottle shared by both states | Shared/teriyaki-sauce.png |
| Hai xiên gà phủ sốt / Finished glazed skewers | Final/teriyaki-chicken-skewer.png |

VI: Ảnh RGBA nền trong suốt, sinh bằng ImageGen tích hợp. Thư mục đầy đủ: `Assets/HotPot/Art/Sprites/Foods/Yakitori`. Icon được đăng ký trong FoodIconConfig. (Cập nhật 2026-11-03: `FinalFoodConfig` đã bị gỡ nên công thức/ảnh món `TeriyakiChickenSkewer = 112` không còn là dữ liệu chạy game, `Final/teriyaki-chicken-skewer.png` không còn được tham chiếu; món xiên nay chỉ là nhóm nguyên liệu `[ChickenBites, TeriyakiSauce]` và chỉ có hình nếu có prefab `FinalFoodView` khớp.) Đầu bếp dùng Fry/Action khi sơ chế gà; đùi gà giữ hình nguyên chiếc, còn gà viên trở thành xiên. Sốt dùng động tác mặc định hiện có. Nhóm âm thanh/vụn của gà là Meat.

EN: Transparent RGBA art is generated with the built-in ImageGen tool. FoodIconConfig references these sprites (updated 2026-11-03: `FinalFoodConfig` was removed, so recipe 112 and the Final skewer picture are no longer game data; the skewer is just the ingredient group [ChickenBites, TeriyakiSauce] and has a picture only if a matching `FinalFoodView` prefab exists). Chicken uses Fry/Action; drumsticks stay whole while bites become skewers. Sauce retains the default action. Chicken uses the Meat sound/debris category.

## Tool web / Web studio

VI: Catalog có hai nguyên liệu, công thức 112 và ảnh tương ứng. Chọn Gà viên + Sốt teriyaki trong order sẽ khớp công thức Xiên gà teriyaki. Board, đĩa đôi, cân bằng nguyên liệu, xuất .asset và demo dùng cùng ID. Không cần thêm cơm hoặc cá hồi. Chai sốt giữ cùng ảnh trước/sau sơ chế.

EN: Selecting chicken bites and teriyaki sauce matches recipe 112 in the web studio. Board editing, double dishes, balancing, .asset export and gameplay use matching IDs. Rice and salmon are not part of this recipe. Sauce keeps the same artwork before and after preparation.


## Cánh gà và ức gà — 29/10/2026

- `ChickenWing = 26` (Cánh gà): `Raw/chicken-wing.png` → `Prepared/chicken-wing.png`, cánh sống → cánh nướng.
- `ChickenBreast = 27` (Ức gà): `Raw/chicken-breast.png` → `Prepared/chicken-breast.png`, ức sống không xương → ức nướng.
- Hai trạng thái dùng ảnh PNG nền trong suốt riêng. FoodIconConfig, bộ tự điền icon và catalog web cùng đăng ký ID mới. Editor Unity và web có tên tiếng Việt.
- Nhóm Meat; chef dùng Fry/Action và giữ fallback Cut/Action khi animation không tồn tại. Hai nguyên liệu dùng được ở board, đĩa đôi và nhóm order tùy chọn; không tự thay level hay thêm công thức món hoàn chỉnh.

EN: ChickenWing (26) and ChickenBreast (27) have separate raw/grilled sprites, Meat categorization and the existing frying action with cutting fallback. They are available to board cells and ingredient-based orders in Unity and web; existing IDs, levels and named recipes remain unchanged.
