# Double-dish plate artwork / Hình đĩa đôi

Updated / Cập nhật: 2026-10-22.

EN: The approved design extends the existing single ceramic plate into a rectangle with subtly rounded corners. Separate horizontal and vertical sprites preserve upper-left lighting and the lower ceramic side wall. The single plate is unchanged.

VI: Thiết kế đã duyệt mở rộng đĩa sứ đơn hiện tại thành hình chữ nhật bo nhẹ bốn góc. Hai sprite ngang/dọc riêng giữ hướng sáng trên trái và thành sứ phía dưới. Đĩa đơn giữ nguyên.

## Assets / Asset

- `Assets/HotPot/Art/Sprites/UI/Dishes/CeramicPlateHorizontal.png`
- `Assets/HotPot/Art/Sprites/UI/Dishes/CeramicPlateVertical.png`
- `Assets/HotPot/Art/Sprites/UI/Dishes/PlateLidHorizontal.png`
- `Assets/HotPot/Art/Sprites/UI/Dishes/PlateLidVertical.png`
- `Assets/HotPot/Art/Shaders/Dishes/PlateSilhouette.shader`
- `Assets/HotPot/Art/Materials/Dishes/PlateSilhouette.mat`
- `Assets/HotPot/Art/Sprites/UI/Dishes/PlateSelectionOutlineHorizontal.png`
- `Assets/HotPot/Art/Sprites/UI/Dishes/PlateSelectionOutlineVertical.png`

EN: Double dishes now prefer dedicated white selection-ring sprites with transparent centres, matching the existing single-dish `PlateSelectionOutline` convention. The original sprite material handles both white selection and red rejection flashes. The silhouette shader remains a fallback when a ring is not assigned. `DoubleDishOutlineSetup` generates these simple geometric rings; use `Tools/FoodPop/Rebuild Double Dish Outlines` to rebuild them without changing their GUIDs.

VI: Đĩa đôi hiện ưu tiên sprite vòng viền chọn trắng riêng có tâm trong suốt, theo quy ước `PlateSelectionOutline` của đĩa đơn. Vật liệu sprite gốc xử lý cả viền chọn trắng và nháy báo từ chối đỏ. Shader bóng hình vẫn dự phòng khi chưa gán vòng viền. `DoubleDishOutlineSetup` tạo các vòng viền hình học đơn giản; dùng `Tools/FoodPop/Rebuild Double Dish Outlines` để tạo lại mà không đổi GUID.

EN: PNGs retain actual transparency. Unity imports one sprite per PNG, slicing the visible bounds using alpha ≥128 without modifying the source pixels. The short side is normalized to 2.5 sprite units. `FoodBoardManager` assigns each orientation and the shared outline material. `Food` fits the art to the previous footprint, compensates the lid's child scale, and restores the single sprite when pooled. The silhouette shader uses alpha only for white selection/red rejection.

VI: PNG giữ nền trong suốt thật. Unity nhập một sprite mỗi PNG, cắt vùng nhìn thấy theo alpha ≥128 mà không đổi pixel nguồn. Cạnh ngắn được chuẩn hóa thành 2,5 đơn vị sprite. `FoodBoardManager` gán hình theo hướng và vật liệu viền chung. `Food` căn hình vào vùng chiếm cũ, bù scale nắp con và khôi phục sprite đơn khi tái sử dụng từ pool. Shader viền chỉ lấy alpha để vẽ chọn trắng/báo từ chối đỏ.

## Generation record / Thông tin tạo ảnh

EN: Generated with the built-in `image_gen` tool, using the approved comparison preview and the original `CeramicPlate.png` as references (now under `Assets/HotPot/Art/Sprites/UI/Dishes`). Final prompts follow verbatim.

VI: Tạo bằng công cụ `image_gen` tích hợp, dùng ảnh so sánh đã duyệt và `CeramicPlate.png` gốc làm tham chiếu (hiện trong `Assets/HotPot/Art/Sprites/UI/Dishes`). Các prompt cuối được giữ nguyên bên dưới.

### Horizontal / Ngang

```text
Extract/recreate ONLY the middle HORIZONTAL rectangular plate from image 1 as one standalone production game sprite with a TRUE TRANSPARENT background. Image 1 is the user-approved design; image 2 is the actual existing single plate and controls exact material and simple 2D rendering style. Preserve the approved warm ivory thick rim, thin brown-gray outline, blue-gray shallow inner basin, top-left restrained diagonal highlight and flat darker lower side wall. Straight long sides, four subtly rounded corners, approximately 2:1 silhouette. Keep same hand-drawn simple sprite shading as image 2. One EMPTY plate only, horizontally oriented, fully visible, centered, tight even transparent padding around it. No round plate, no vertical plate, no board, no comparison sheet, no background pixels, no lettering, no food, no external drop shadow. Preserve all the approved rectangular plate's proportions and edge details.
```

### Vertical / Dọc

```text
Extract/recreate ONLY the right VERTICAL rectangular plate from image 1 as one standalone production game sprite with a TRUE TRANSPARENT background. Image 1 is the user-approved design; image 2 is the actual existing single plate and controls exact material and simple 2D rendering style. Preserve the approved warm ivory thick rim, thin brown-gray outline, blue-gray shallow inner basin, top-left restrained diagonal highlight and flat darker lower side wall. Straight long sides, four subtly rounded corners, approximately 1:2 silhouette (height about twice width). Keep same hand-drawn simple sprite shading as image 2. One EMPTY plate only, vertically oriented, fully visible, centered, tight even transparent padding around it. No round plate, no horizontal plate, no board, no comparison sheet, no background pixels, no lettering, no food, no external drop shadow. Preserve all the approved vertical plate's proportions and edge details. Lighting stays upper left and lower side wall stays on the bottom short edge, NOT a rotated horizontal sprite.
```

## Rectangular lids / Nắp chữ nhật

EN: Separate horizontal and vertical lids are based on the original silver `PlateLid.png` (now in `Art/Sprites/UI/Dishes`). They keep its silver-gray shading, warm pale lip and single round knob. `horizontalDoubleLid` / `verticalDoubleLid` are assigned in `HotPotDemo.unity`; `ConfigureShapeLid` fits one shared lid over the two-cell plate. Hidden conceals both ingredients and their quantities. Existing lift/fade and booster lid animations reuse the new resting scale. Pooling restores the original round lid. Alpha, orientation selection, two-cell coverage, zero-duration reveal and restoration have been checked in a temporary preview scene; camera render was visually inspected beside the single lid.

VI: Hai nắp ngang/dọc riêng tham chiếu `PlateLid.png` bạc gốc (hiện trong `Art/Sprites/UI/Dishes`), giữ sắc bạc xám, viền sáng ấm và một núm tròn. `horizontalDoubleLid` / `verticalDoubleLid` đã gán trong `HotPotDemo.unity`; `ConfigureShapeLid` căn một nắp chung phủ đĩa hai ô. Hidden che cả hai nguyên liệu và số lượng. Hoạt cảnh nhấc/mờ và nắp booster dùng lại scale nghỉ mới. Pool khôi phục nắp tròn gốc. Đã kiểm tra alpha, chọn đúng hướng, phủ hai ô, mở tức thì và khôi phục trong scene xem trước tạm; đã xem ảnh camera cạnh nắp đơn.

EN: Generated with the built-in `image_gen` tool. Exact final prompts:

VI: Tạo bằng công cụ `image_gen` tích hợp. Prompt cuối giữ nguyên:

### Horizontal lid / Nắp ngang

```text
Create ONE standalone production sprite of a HORIZONTAL DOUBLE-DISH LID, on a genuinely TRANSPARENT background. Image 1 is the actual existing round lid: copy its simple hand-painted 2D mobile-game style, silver/light blue-gray metal, warm off-white thin lower rim, dark thin contour, broad white upper-left highlight, simple stepped gray shading toward the lower right, and its small round metal knob with a short pedestal. Image 2 is the rectangular plate the lid must cover: its outline proportions and subtly rounded corners are the geometry reference ONLY. Redesign the round lid into one gently domed rectangular cloche with long STRAIGHT sides and four slightly rounded corners, horizontal approximately 2.4:1 silhouette, sized to cover both halves of the plate. Keep a SINGLE SMALL ROUND KNOB at the center; do not stretch the knob horizontally. Keep the same slight top-down angle and small visible front/bottom lip as image 1. Plain continuous silver top with no seams or divisions. This must look like the original single-lid artist made the matching rectangular version. One lid only, no plate underneath, no food, no background, no external shadow, no text, no comparison layout, no extra decorations, no realistic photography. Center the complete lid with small even transparent margins. Preserve the reference's restrained smooth painted shading, not shiny chrome or gritty texture.
```

### Vertical lid / Nắp dọc

```text
Create ONE standalone production sprite of a VERTICAL DOUBLE-DISH LID, on a genuinely TRANSPARENT background. Image 1 is the actual existing round lid: copy its simple hand-painted 2D mobile-game style, silver/light blue-gray metal, warm off-white thin lower rim, dark thin contour, broad white upper-left highlight, simple stepped gray shading toward the lower right, and its small round metal knob with a short pedestal. Image 2 is the rectangular plate the lid must cover: its outline proportions and subtly rounded corners are the geometry reference ONLY. Redesign the round lid into one gently domed rectangular cloche with long STRAIGHT sides and four slightly rounded corners, vertical approximately 1:2 silhouette, sized to cover both halves of the plate. Keep a SINGLE SMALL ROUND KNOB at the center; do not stretch the knob vertically. Keep the same slight top-down angle and small visible front/bottom lip as image 1. Plain continuous silver top with no seams or divisions. This must look like the original single-lid artist made the matching rectangular version. One lid only, no plate underneath, no food, no background, no external shadow, no text, no comparison layout, no extra decorations, no realistic photography. Center the complete lid with small even transparent margins. Preserve the reference's restrained smooth painted shading, not shiny chrome or gritty texture. Lighting remains upper left and the lower rim is along the bottom short side; do not rotate a horizontal lid.
```

## Verification / Kiểm chứng

EN: Runtime/editor build passed. Shader compilation, exact two-cell dimensions and restoration of the original single plate passed isolated checks. Camera rendering was visually inspected with real ingredient icons and outlines. The 30 double-dish behavior checks passed again after integration. Full Play Mode animation testing remains outstanding.

VI: Build runtime/editor thành công. Kiểm tra độc lập xác nhận shader biên dịch, kích thước đúng hai ô và khôi phục đĩa đơn gốc. Đã xem ảnh camera render với icon nguyên liệu thật và viền chọn. Bộ 30 kiểm tra hành vi đĩa đôi tiếp tục đạt sau tích hợp. Chưa kiểm tra đầy đủ hoạt cảnh trong Play Mode.
