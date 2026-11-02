# Conveyor Chef

Cập nhật: 29/10/2026.

## Số slot

- Số slot mặc định giảm từ **12 xuống 10** (`ConveyorChefMovement.DefaultSlotCount`).
- `Conveyor_Chef` trong scene `Assets/HotPot/Scenes/HotPotDemo.unity` đã được đặt thành 10 slot và chia đều lại trên spline.
- `Slot Count` trong Inspector vẫn cho phép cấu hình số lượng slot khác.

## Đường chạy theo sprite băng chuyền mới (02/11/2026)

- Art băng chuyền đổi sang sprite nhìn thẳng `Assets/HotPot/Art/Sprites/Ingame/conveyor.png` (994x653 px, 100 PPU, pivot giữa), khay giữa là `table_slot.png`.
- `BeltCenterline` trong `ConveyorChefMovement` được dựng lại theo đường giữa làn băng: giữa mép ngoài `conveyor` và mép `table_slot` (khay đặt thấp hơn tâm băng 0.08). Theo local của sprite: cạnh trên y = 2.545, cạnh dưới y = -2.625, hai cạnh bên x = ±4.272, bo góc bán kính 1.0; điểm đầu vẫn là giữa cạnh trên, chạy theo chiều kim đồng hồ (giữ nguyên ý nghĩa `Station Start/End`, `Station Exit`).
- Nếu đổi vị trí/kích thước `table_slot` so với `conveyor`, phải cập nhật lại các điểm này.
- `Awake` giờ luôn dựng lại spline theo art khi `Fit Spline To Conveyor` bật, nên spline cũ lưu trong scene không còn làm lệch đĩa lúc chạy. Tắt tùy chọn này để giữ spline chỉnh tay (khi đó chỉ dựng khi spline rỗng).
- Tìm renderer tự động (khi chưa gán `Conveyor Renderer`) giờ tìm sprite tên `conveyor` thay cho `ConveyorStandalone`.
- Offset hàng trước/sau trong scene (0.03 / 0.06) được giữ nguyên.

## Setup trong Inspector

Inspector của `ConveyorChefMovement` chỉ giữ một nút:

**Setup: Build Spline + Create Slots + Snap**

Khi bấm, nút thực hiện lần lượt:

1. Dựng lại spline theo sprite băng chuyền và các offset đã cấu hình.
2. Gom slot hiện có và tạo thêm slot thiếu từ `Slot Prefab`.
3. Xóa slot dư từ cuối danh sách để khớp `Slot Count`, giữ các slot đầu danh sách.
4. Áp dụng sorting order và layer cho slot.
5. Tính lại chiều dài đường chạy và chia đều các slot trên spline.

Toàn bộ thao tác Setup được gom thành một bước Undo. Cần gán `Slot Prefab` để tạo thêm slot khi tăng số lượng. Sau khi chỉnh và Setup, lưu scene hoặc prefab để giữ thay đổi.

Đã bỏ các nút riêng: `Build Spline From Conveyor`, `Create / Collect Slots`, `Sort Slots Above Belt`, `Snap Slots To Path`. Các hàm xử lý tương ứng vẫn được gọi bên trong Setup tổng.

## Bỏ setup trong Tools

- Đã bỏ menu `Tools/HotPot/Setup Conveyor_Chef Movement`.
- Đã xóa `ConveyorChefAutoSetup.cs` cùng file `.meta`, đồng thời bỏ cơ chế tự setup một lần khi reload script.
- Thao tác setup hiện thực hiện trực tiếp bằng nút trong Inspector.

## Chọn slot trống khi lấy món

- `ChefManager.TryGetNearestEmptySlots()` chỉ xét slot đang hoạt động và có `isEmpty == true`.
- Ưu tiên slot có quãng đường còn lại tới cửa vào trạm sơ chế ngắn nhất theo chiều băng chuyền, qua `ConveyorChefMovement.GetDistanceToStation()`.
- Với chiều kim đồng hồ, cửa vào là `Station Start`; khi đảo chiều, cửa vào là `Station End`. Slot vừa đi qua cửa vào phải chờ gần một vòng nên xếp sau các slot sắp tới cửa vào.
- Thay thế ưu tiên cũ dựa trên quãng đường đã đi từ cuối trạm. Không dùng khoảng cách từ món trên board để ưu tiên khi có component di chuyển.
- Vẫn yêu cầu đủ slot cho toàn bộ lượt lấy; slot được điền và đánh dấu hết trống trước khi hiệu ứng bay bắt đầu.

## Giữ đĩa nguồn khi sơ chế (thay thế luồng trả trống trước đó)

- Đĩa nguồn giữ food và isEmpty = false suốt lúc món nhảy vào đầu bếp, sơ chế và bay về. Board không thể chiếm slot này.
- ChefManager ghi nhận đĩa gốc và ArrivalId, ẩn hình món trên đĩa bằng BeginFoodArrival(), không sao chép món sang dữ liệu riêng và không gọi ClearFood() khi bắt đầu sơ chế.
- ConveyorSlot.SetPreparationProgress() hiện vòng tròn bám theo đĩa gốc: nền tối, cung xanh đầy dần theo chiều kim đồng hồ từ phía trên. Tiến độ theo thời gian sơ chế, dừng khi pause; giữ đầy trong hiệu ứng hoàn tất và bay về.
- Sơ chế xong, món bay về đúng đĩa nguồn đang di chuyển. Khi đáp, hoàn tất lượt nhận và ẩn vòng tiến độ.
- Bỏ luồng tìm đĩa khác, chờ đĩa trống tại đầu bếp và ưu tiên tranh slot với board.
- Xóa món, tắt slot hoặc hủy sơ chế dọn vòng tiến độ; mã lượt đáp bảo vệ đĩa đã bị xóa/dùng lại. Hủy sơ chế khôi phục món trên đĩa gốc.

## File liên quan

Bản web: play-engine.js giữ chefItem tham chiếu cùng món trên slot nguồn, không trả trống. play-demo.js vẽ vòng tiến độ quanh slot sơ chế và cung bay về đúng đĩa. verify-play.cjs kiểm tra giữ chỗ khi đầy, trả đúng nguồn dù có đĩa khác trống, hoàn tất và reset.

- `Assets/HotPot/Scripts/gamebase/ConveyorChefMovement.cs`: cấu hình slot, dựng spline và bố trí slot trên đường chạy.
- `Assets/HotPot/Editor/ConveyorChefMovementEditor.cs`: nút Setup tổng, đồng bộ số slot và hỗ trợ Undo.
- `Assets/HotPot/Prefabs/Ingame/Conveyor_Slot.prefab`: prefab dùng khi tạo slot mới.
- `Assets/HotPot/Scenes/HotPotDemo.unity`: scene chứa Conveyor Chef với 10 slot.

## Kiểm tra đã thực hiện

- Xác nhận Conveyor Chef trong scene có đúng 10 slot sau khi giảm từ 12.
- Kiểm tra Setup trên object tạm trong Unity theo chuỗi `12 → 10 → 14 → 10 → 10`: số phần tử trong danh sách và số component slot con đều khớp Slot Count; slot giữ liên kết prefab.
- Kiểm tra mã Inspector chỉ còn một nút Setup tổng.
- Kiểm tra Unity trên object tạm: giữ chỗ, từ chối fill đè, hình học vòng 25%/100%, món Prepared về đúng phần gốc và dọn vòng khi xóa. Web kiểm tra hết level, bảo toàn nguyên liệu và giữ slot nguồn. Chưa chạy trọn hiệu ứng Unity trong Play Mode.
- 02/11/2026: dựng ảnh xem trước bằng cách ghép `conveyor.png`, `table_slot.png` và 10 `plate_conveyor.png` (scale 0.84) chia đều trên đường mới: các đĩa nằm giữa làn xanh, không đè lên khay hay mép ngoài. Chưa biên dịch và chưa chạy Play Mode trong Unity.
