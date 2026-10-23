# Booster

## Khay giữ món: trạng thái ngăn kéo

- Component `Assets/HotPot/Scripts/Booster/FoodHoldingTray.cs` trên prefab `Assets/HotPot/Prefabs/Ingame/FoodHoldingTray.prefab`, đã gắn tại `FoodHoding` trong scene.
- Các trạng thái: `Retracted`, `Extending`, `Extended`, `Retracting`.
- Gọi `Extend()` để kéo ra, `Retract()` để thu vào. Mặc định bắt đầu thu gọn; thời gian trượt đầy đủ 0,3 giây, dùng thời gian game nên dừng khi timeScale bằng 0.
- Có thể đảo chiều giữa chừng; gọi lặp cùng chiều không khởi động lại hiệu ứng. Khi tắt component, hủy chuyển động và trả về trạng thái bắt đầu.
- Menu ngữ cảnh của component có `Kéo khay ra` và `Thu gọn khay`: xem trước tức thì trong Edit Mode, chạy animation trong Play Mode.
- Cấu trúc: `WallHousing` cố định; `Drawer/Back`, `Drawer/FoodRoot`, `Drawer/Front` trượt cùng nhau. Vật liệu `FoodHoldingTrayClip` cắt phần khay nằm phía sau mép vách, không dùng scale để giả lập kéo ra.
- Đặt food dưới `Drawer/FoodRoot`; thứ tự vẽ sau/food/trước vẫn là 0/10/20.
- CleanBrush đã nối với FoodHoldingStorage trong FoodHoding; thu vào khay và trả về băng theo nhu cầu order. Không trả nguyên liệu về board.

Tài liệu tham chiếu hệ thống booster trong FoodPop, dựa trên `Assets/HotPot/Scripts/Booster/BoosterController.cs`.

## Các file chính

- `Assets/HotPot/Scripts/Booster/BoosterController.cs`: xử lý thao tác, điều kiện sử dụng, logic và hiệu ứng của booster.
- `Assets/HotPot/Scripts/Booster/Booster.cs`: lớp cơ sở của nút booster.
- `Assets/HotPot/Scripts/Booster/BoosterType.cs`: định danh loại booster.
- `Assets/HotPot/Scripts/Booster/PickupHandBooster.cs`
- `Assets/HotPot/Scripts/Booster/AutoCompleteBooster.cs`
- `Assets/HotPot/Scripts/Booster/CleanBrushBooster.cs`
- `Assets/HotPot/Scripts/Booster/ShuffleBooster.cs`

## Luồng chung

- `Update` đọc thao tác chuột/chạm bằng Input System; khi PickupHand đang chọn đĩa thì chuyển sang xử lý lựa chọn.
- `TryActivate` kiểm tra booster hợp lệ, trạng thái đang chạy và điều kiện riêng qua `CanUse`.
- Không kích hoạt khi chưa gán board hoặc board đang chạy animation.
- Khi không đủ điều kiện, nút chạy hiệu ứng từ chối và ghi log lý do.
- Khi hợp lệ, booster chạy ngay; không dùng ButtonPressFx hoặc chờ hiệu ứng bấm. Icon vẫn làm mờ khi bận và lắc khi bị từ chối.
- `RunLocked` khóa thao tác board và đánh dấu controller đang bận trong khi chạy hiệu ứng; sau đó mở khóa và hiện lại các nút.
- `SetBusy` cập nhật trạng thái khả dụng của toàn bộ nút booster.

## PickupHand

### Điều kiện

- Có đĩa không đóng băng trong phạm vi `maxVisibleRows`.
- Băng chuyền có đủ slot trống cho toàn bộ `TotalAmount` của đĩa, bao gồm cả hai nửa nếu là đĩa đôi.

### Xử lý

1. Khóa board, ẩn các nút booster và hiện thông báo kèm nút hủy.
2. Giới hạn số hàng hiển thị; kéo camera xuống nếu cần để các hàng chọn được nằm phía trên thông báo.
3. Mỗi frame cập nhật các đĩa có thể chọn theo số slot trống hiện tại.
4. Chạm đĩa không hợp lệ sẽ chạy hiệu ứng từ chối nếu đĩa có `Selection`.
5. Chọn hợp lệ: chỉ làm sáng đĩa đó, tăng thứ tự vẽ, nhấc và phóng nhẹ; mở nắp nếu đang ẩn.
6. Gọi `board.TryPickDishAt` để dùng luồng lấy đĩa của board.
7. Nếu lấy thất bại, trả vị trí, scale và thứ tự vẽ của đĩa về trước lúc nhấc.
8. Chờ board hết animation, đưa camera về và cập nhật lại lựa chọn thông thường.

Hủy lựa chọn sẽ ẩn thông báo, khôi phục camera và giao diện board, mở khóa và hiện lại nút.

## AutoComplete

- Điều kiện: `CustomerManager.HasAutoCompleteTarget()` trả về true.
- Xin giữ chỗ công việc qua `CustomerManager.TryReserveAutoComplete`.
- Hiện ngôi sao particle gần đầu bếp.
- Với từng nguyên liệu còn thiếu, ưu tiên lấy từ băng chuyền qua `ChefManager.TryTakeFood`, rồi khay giữ món qua `FoodHoldingStorage.TryTake`, sau đó từ board qua `board.TryTakePortion`.
- Nếu không tìm thấy nguyên liệu, ghi cảnh báo và vẫn tạo hình nguyên liệu từ `IconConfig` để tiếp tục hiệu ứng.
- Nguyên liệu bay vào sao; sao bay tới phía trên ô món rồi biến mất kèm hiệu ứng nổ.
- Tạo các hình nguyên liệu ở trạng thái `Prepared`, bung ra và lần lượt gửi vào khay qua `LaunchBoosterServe` theo thứ tự danh sách công việc.
- Chờ khay hết nhận nguyên liệu trước khi kết thúc booster.

## CleanBrush / Thu hồi món

- Điều kiện: board không đang animation, controller rảnh, băng có food, kho giữ món đã gán và hoạt động; không bắt đầu khi khay đang trả món.
- Khóa thao tác board trong toàn bộ lượt; tạm dừng băng chuyền, sơ chế và các chuyến phục vụ mới. Công việc chặt dở được hủy, giữ nguyên trạng thái food.
- Kéo khay ra, chờ tới vị trí Extended, sau đó lấy từng phần food khỏi slot. Food nhấc lên 0,15 giây với nhịp phóng nhẹ, bay vòng cung vào khay; các lượt khởi hành cách nhau 0,07 giây.
- Không dùng chổi, bọt xà phòng hoặc bong bóng. Các field hình chổi cũ còn giữ để tương thích dữ liệu Inspector.
- Trước khi bay, food nháy sáng nhẹ với viền vàng ấm và phóng tối đa 12%; hình đĩa và food cùng nhấc lên một đoạn theo Lift Height. Khi food bắt đầu bay vào khay, đĩa trống rơi nhanh dần về đúng slot rồi nảy nhẹ; slot root và bóng dưới đĩa giữ nguyên. Clean chờ cả food lẫn đĩa đáp xong trước khi bỏ tạm dừng băng. Chỉnh Plate Drop Duration để đổi thời gian rơi (mặc định 0,2 giây, thêm nhịp nảy 0,12 giây). Sau nhịp chuẩn bị, food về scale gốc và bay vào khay; không thu nhỏ khi cất. Chỉnh `Lift Duration`, `Collect Pulse`, `Collection Highlight` trên CleanBrushBooster.
- Mỗi phần được ghi vào FoodHoldingStorage ngay sau khi rời băng, giữ nguyên FoodType, FoodState, sprite và scale world gốc trên băng. Food không thu nhỏ khi bay vào hoặc khi nằm trong khay; scale cha được bù khi xếp hình. Chờ tất cả phần đáp rồi khôi phục các cờ tạm dừng và khóa board cũ; khay giữ kéo ra khi còn food, chỉ thu vào sau khi hết food.
- Clean vẫn thu mọi food trên băng, không lọc theo order khi thu.
- FoodHoldingStorage dùng CustomerManager.GetNeededIngredients để trả đúng số phần còn thiếu sau khi trừ food đã có hoặc đang bay trên băng. Khay kéo ra và food bay tới một slot trống được giữ chỗ trước; băng tiếp tục chạy trong lượt trả.
- Phần đã sơ chế trở lại băng ở trạng thái Prepared, phần thô vẫn Raw. Food chỉ bị xóa khỏi kho sau khi đáp thành công; lượt trả thất bại giữ lại phần trong kho.
- AutoComplete tìm nguyên liệu theo thứ tự băng chuyền, khay giữ món, rồi board để không tạo trùng food đã cất.
- CancelCleanCollection hủy lượt đang thu, giải phóng khóa và cờ tạm dừng. LevelController.LoadLevel gọi hủy thu và ResetForLevel để xóa kho cũ khi chơi lại.
- Scene đã gán controller.holdingStorage tới component FoodHoldingStorage ở FoodHoding/FoodHoldingTray; food hiển thị trong Drawer/FoodRoot.
## Shuffle

### Điều kiện và phạm vi

- Có ít nhất hai nhóm nguyên liệu còn phần và khác nhau về loại hoặc số lượng; có thể nằm trong cùng một đĩa đôi.
- Đĩa đôi tham gia xáo food nhưng giữ nguyên vị trí, hướng, hình dạng và vùng chiếm.
- Khi có đĩa đôi, gom các nhóm food còn phần của đĩa đơn và cả hai nửa đĩa đôi để xáo chung. Loại và số lượng luôn đi cùng nhau, bảo toàn tổng nguyên liệu; các nửa đã hết phần vẫn trống.
- Đĩa đơn tiếp tục đổi vị trí như trước. Board chỉ có đĩa đơn dùng luồng xáo vị trí hiện có.
- Trạng thái nắp ẩn, đóng băng và giá trị block được lưu theo vị trí, rồi áp lại sau khi đổi đĩa.

### Xử lý

1. Tạm ngưng cơ chế ẩn ngoài màn hình, làm sáng và phồng nhẹ các đĩa tham gia.
2. Đóng các nắp đang mở, gán vật liệu cầu vồng và rung nắp.
3. Xáo food bên trong nếu có đĩa đôi; sau đó tạm gỡ trạng thái đóng băng và ẩn nhãn số lượng khi di chuyển.
4. Tạo thứ tự mới qua `MakeShuffledOrder`.
5. Bay các đĩa đơn theo vòng cung tới vị trí mới, chuyển cha sang cột đích; không đổi transform của đĩa đôi.
6. Khôi phục vật liệu, nắp và trạng thái đóng băng theo vị trí đích.
7. Ghi danh sách cột mới về board qua `ApplyColumnDishes`, rồi bật lại cơ chế ẩn ngoài màn hình.

### Ưu tiên nguyên liệu

- Lấy danh sách nhu cầu từ `CustomerManager.GetNeededIngredients`.
- Đặt nguyên liệu cần đầu tiên có đĩa đáp ứng vào một vị trí hàng 1 không đóng băng.
- Nếu đặt được, tìm nhu cầu tiếp theo để đặt vào hàng 2 không đóng băng.
- Với cả hai hàng, ưu tiên ô không ẩn; dùng ô ẩn khi không còn ô phù hợp khác.
- Xáo ngẫu nhiên các đĩa còn lại vào vị trí còn trống.

## Hiệu ứng và phụ thuộc

- `FxRoot` tạo object `BoosterFx` ở gốc scene khi cần.
- Các sprite hiệu ứng được tái sử dụng qua `RentFx` và `ReleaseFx`.
- Particle ngôi sao và vệt bọt được giữ instance để tái sử dụng.
- Các phụ thuộc chính: `FoodBoardManager`, `ChefManager`, `CustomerManager`, `Food`, `ConveyorSlot` và `OneShotEffects`.

## Quy tắc khi chỉnh sửa

- Mọi hàm mới hoặc sửa phải có comment EN/VI trực tiếp phía trên; tooltip chỉ dùng tiếng Việt.
- Tách các trách nhiệm độc lập thành helper có tên rõ ràng, giữ callback và lifecycle gọn.
- Khi thay đổi logic lấy/trả/xáo đĩa, tuân thủ các quy tắc board, gravity và ánh xạ cột hàng chờ trong `AGENTS.md`.
- Cập nhật tài liệu này khi hành vi booster thay đổi.

- Khay không nhận lệnh thu vào khi FoodHoldingStorage còn phần food, kể cả menu thử thủ công. AutoComplete lấy hết phần cuối hoặc chuyến trả cuối đáp xuống mới cho thu khay.


## Particle thu món

- CleanCollectionTrail: vệt chấm sáng vàng kem và sao thưa trong không gian world, sống 0,5–0,6 giây, kích thước hạt gấp đôi bản đầu, bắt đầu khi food bay và dừng phát khi chuyến bay kết thúc hoặc bị hủy. Controller tái sử dụng vệt đã kết thúc.
- Prefab trong Assets/HotPot/Prefabs/Effects/Collection; vật liệu trong Art/Materials/Particles; dùng texture Effects có sẵn. Dựng lại bằng Tools/HotPot/Build Clean Collection Effects và gán Collection Trail trên CleanBrushBooster.


## Bảo toàn nguyên liệu giữa Clean và AutoComplete

- ConveyorSlot tạo ArrivalId mới khi bắt đầu chuyến đáp và khi xóa food. FoodFlyAnimator, chuyến sơ chế và chuyến trả từ khay chỉ hoàn tất/hủy đúng ArrivalId đã giữ; callback cũ không tác động slot dùng lại, kể cả cùng FoodType.
- AutoComplete gọi FoodHoldingStorage.CancelPendingReturn trước khi giữ chỗ order và lấy nguyên liệu. Phần đang trả được giữ lại đúng một lần trong kho, phần giữ chỗ trên băng và sprite bay tạm được hủy an toàn.
- Kiểm tra Play Mode: slot dùng lại cùng loại không bị callback cũ hoàn tất/xóa; nguyên liệu gốc trong kho lấy được đúng một lần; Clean rồi AutoComplete giữa chuyến trả bàn giao an toàn; chạy tới hết level qua 8 lượt AutoComplete, tất cả khách được phục vụ, kho trống và không có cảnh báo thiếu nguyên liệu. Lượt thử dùng nguyên liệu thật từ board và thông số tăng tốc chỉ trong Play Mode.

