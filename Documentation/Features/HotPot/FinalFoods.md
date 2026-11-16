# Món hoàn chỉnh (FinalFoodView)

Món không phải mục dữ liệu riêng (không có enum/ScriptableObject món). Một món = một nhóm `IngredientOrder` trong order (`ingredients` 1–6 `FoodType`, mục đầu là đế) cộng trường tùy chọn `dish` — prefab có `FinalFoodView` ở gốc. Ảnh trên thẻ order và khay lấy từ prefab; nhóm không có prefab vẫn chạy nhưng không có hình.

## Prefab món ghép lớp
- Đặt ở `Assets/HotPot/Prefabs/Food/FinalFood_N.prefab`; sprite lớp `Art/Sprites/Foods/Sushi/Final/<nguyên liệu>_3.png` (và các thư mục món khác như `Foods/Drinks`, `Foods/Edamame`).
- `FinalFoodView.layers`: danh sách {FoodType, SpriteRenderer} theo thứ tự công thức, đế trước.
- Field tùy chọn: `plate` (khay/đĩa riêng của món, không phải lớp nguyên liệu), `anyIngredientOrder` (bỏ luật đế), `isAnimate` + `foodAnimation` (hoạt cảnh khi ghép, xem [ServingFood.md](ServingFood.md#hoạt-cảnh-riêng-cho-món)).
- API: `GetIngredients()`, `Matches(ingredients)` (cùng nguyên liệu và số lượng, thứ tự bất kỳ), `GetLocalSize()`, `SetSorting(layer, baseOrder)` (giữ thứ tự tương đối, cả đạo cụ).
- **Thêm món:** tạo prefab, gắn `FinalFoodView`, điền `layers`. Level Editor tự quét mọi prefab dưới `Assets/HotPot` có `FinalFoodView` — không cần cập nhật danh sách. Không có menu dựng prefab (tránh ghi đè chỉnh tay).
- Level Editor: chọn prefab thì chép nguyên liệu vào nhóm và gán `dish`; bật/tắt nguyên liệu thì tự nối prefab đầu tiên khớp (hoặc xóa `dish`).

## Nguyên liệu không phải hải sản
- Trà xanh: TeaLeaves (lá trà khô) và Ice; ảnh ở `Art/Sprites/Foods/Drinks`. Đĩa khay dùng `Plate_Tea` khi prefab có lớp TeaLeaves.
- Edamame và Salt: ảnh ở `Art/Sprites/Foods/Edamame`.
- Ice, Salt có `skipPreparation` — không qua đầu bếp ([IngameConfig.md](IngameConfig.md)).
- Menu `Setup Green Tea` / `Setup Edamame` chỉ đăng ký icon nguyên liệu trong FoodIconConfig.

Prompt tạo ảnh món: `Documentation/Design/Prompts/sushi-progression/`.
