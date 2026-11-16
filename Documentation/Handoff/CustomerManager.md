# CustomerManager

## Class
- `Scripts/Customer/CustomerManager.cs`: hàng khách, ưu tiên phục vụ, giữ chỗ, hoàn thành, đồng hồ Boss/Shipper.
- `Scripts/Customer/Lane.cs`: tham chiếu Inspector và trạng thái runtime một làn (`[Serializable]`, giữ tên field).
- `Scripts/Customer/DishProgress.cs`: tiến độ một món — `dish` (FinalFoodView, có thể null), công thức, `missing`, `incoming`, `baseReserved`, `assembled`.
- `Scripts/Customer/CustomerTimerBar.cs`: thanh thời gian phục vụ.

Hoạt cảnh bay/ghép món thuộc `ServingFood` — xem [ServingFood.md](../Features/HotPot/ServingFood.md).

## Dữ liệu
- `LevelConfig.GetCustomerQueue()` trả bản sao hàng khách chung (`customerQueue`). Cấu trúc order: [Configuration.md](../Features/HotPot/Configuration.md#khách-customerorder).
- `lanes`: hai làn (trái, phải). Mỗi `Lane` giữ `CustomerView`, `OrderView`, `ServingFood`, hình khách chờ, chân dung, hướng trượt; runtime: `queue` (lịch sử khách của làn), `queueIndex`, `customerNumber` (chỉ số toàn cục), `ordering`, `arrivalStamp`, `walker`, `serveTimeTotal`/`serveTimeLeft`.
- Manager giữ `customerQueue`, `nextCustomer`, `waitingLane` (làn hiển thị hàng chờ chung).

## Nạp khách
1. `LoadCustomers` dừng coroutine cũ, reset trạng thái làn/khay/đồng hồ, `HasCompletedOrder = false`, `completedCustomers = 0`, phát `RemainingCustomersChanged`.
2. Phân khách lần lượt cho từng chỗ trống (`AssignNextCustomer` tăng `nextCustomer` **trước** khi coroutine nhường lượt để không lấy trùng). Khách 1 vào trái, 2 vào phải; ai rời trước thì chỗ đó lấy khách kế tiếp.
3. `SetupOrder` tạo `DishProgress(group.dish, group.ingredients)` cho tối đa 4 nhóm hợp lệ, gọi `OrderView.Layout(count)`; `BeginServing` giao cho `ServingFood.BeginOrder`. Khách không có món hợp lệ kết thúc ngay.
4. `CustomerView.SetCustomerType` (Normal/Boss/Shipper), chân dung theo chỉ số toàn cục; sau một frame nhận `arrivalStamp` và phát `CustomerArrived(index, order)`.

## Phục vụ từ băng
- `Update` duyệt mọi slot: lấy món đã sơ chế, không đang đáp, đã nằm trên đĩa ≥ `Serve Delay After Landing` (`ConveyorSlot.LandedTime`).
- `TryFindCustomer(type)`: khách đang order đến sớm nhất có món nhận được nguyên liệu (ưu tiên món đứng trước trên thẻ). Món nhận khi còn thiếu nguyên liệu đó, không có `incoming` và `ServingFood.CanServe`. **Luật đế:** đế phải gửi trước (trừ món `anyIngredientOrder`).
- Trước `LaunchServe`: xóa khỏi `missing`, tăng `incoming`, đánh dấu đế. Phóng lỗi → `CancelReservation`.
- `TryReserveDirectServe` giữ một phần cho luồng đi thẳng từ đầu bếp (cùng `TryFindCustomer`/`CanAccept`); `IsValid` kiểm tra khách vẫn order và progress còn thuộc làn. Xem [Conveyor.md](Conveyor.md#sức-chứa-khi-lấy-đĩa-dishpickplan).

## Hoàn thành khách
- `OnIngredientDelivered` cập nhật tiến độ; món đủ và hết chuyến bay → `ServingFood.CompleteDish`. `OnDishDelivered` đánh dấu món đã giao.
- Đủ order → `HasCompletedOrder = true` (dùng cho cảnh báo thoát, [UI.md](UI.md#cảnh-báo-thoát)) → `FinishCustomer`: dừng đồng hồ, chữ khen tại `HeartPosition`, âm khách vui, chờ `leaveDelay`, ẩn thẻ, khách trượt ra → reset khay, `completedCustomers++`, phát `RemainingCustomersChanged`, lấy khách kế tiếp.
- `CheckAllServed` đặt `AllServed` một lần khi hết hàng chung và mọi làn đã xong (LevelController xử lý thắng).

## Hàng chờ
- `waitingFigures` của `waitingLane` hiển thị `customerQueue.Count − nextCustomer` người (`waitingTint`, `waitingScale`, sorting giảm dần); pool tự bổ sung.
- `StepQueueForward` dùng `lane.walker` riêng đưa chân dung từ đầu hàng vào quầy; hai làn chạy song song được.

## Đồng hồ Boss/Shipper
- Khách Boss/Shipper có `timeServe > 0`: `StartServeTimer` (trong `BringNextCustomer`) làm đầy thanh; `TickServeTimers` đầu `Update` khi `lane.ordering` (dùng `Time.deltaTime` — dừng khi pause, vẫn chạy khi booster chạy).
- Thanh `Prefabs/OrderView/TimerBar.prefab` do `OrderView.GetOrCreateTimerBar()` tạo (con của thẻ, đặt ngoài mép nền phía khách: `timerBarOnLeft`, `timerBarGap` 0.08). Fill1 khi còn > 5 s, Fill2 khi 0–5 s, hết giờ ẩn cả hai; cắt dọc từ dưới lên bằng shader `Art/Shaders/Customer/CustomerTimerFill.shader` (MaterialPropertyBlock).
- `ServeTimeExpired` → `LevelController` mời hồi sinh OutOfTime; đồng ý gọi `ResetExpiredServeTimers()` (+30 s cho khách đã hết giờ). Luồng popup: [UI.md](UI.md#canvasrevive).

## API
| API | Hành vi |
| --- | --- |
| `Instance` | Singleton theo scene |
| `LoadCustomers(LevelConfig)` | Nạp hàng chung, phân khách đầu |
| `TryFindCustomer(FoodType)` | Có khách nhận ngay nguyên liệu này không |
| `AllServed`, `RemainingCustomers`, `HasCompletedOrder` | Trạng thái |
| `RemainingCustomersChanged`, `CustomerArrived` | Sự kiện |
| `GetNeededIngredients(List<FoodType>)` | Nhu cầu theo thứ tự khách → món → công thức, trừ phần đã có/đang đáp trên băng |
| `HasAutoCompleteTarget()`, `TryReserveAutoComplete(out AutoCompleteJob)` | AutoComplete: chọn món thiếu nhiều nguyên liệu nhất (bằng nhau: khách sớm hơn, món trước), giữ chỗ toàn bộ nguyên liệu thiếu |
| `ServeTimeExpired`, `ResetExpiredServeTimers()` | Đồng hồ |

## Vòng đời và lưu ý
- `ResetInstance` khi vào Play Mode; component trùng bị hủy. `OnValidate` tự gán `ServingFood` khi scene có đúng hai component. Thiếu tham chiếu thì gán tay (không có tool Setup).
- Giữ đồng bộ `missing`, `incoming`, cờ đế và callback để tránh gửi trùng/ghép sớm. Phân biệt `customerNumber` toàn cục với `queueIndex` của làn.

**Kiểm chứng:** HUD số khách, giữ/giao/hủy phần đi thẳng đã Play Mode PASS; thanh thời gian Fill1/Fill2 PASS trong Edit Mode. Chưa Play Mode: luồng hết giờ → hồi sinh +30 s.
