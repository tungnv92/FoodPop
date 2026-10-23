# CustomerManager

Tài liệu tham chiếu hệ thống khách hàng trong FoodPop, dựa trên `Assets/HotPot/Scripts/Customer/CustomerManager.cs`.

## Các class chính

- `Assets/HotPot/Scripts/Customer/CustomerManager.cs`: điều phối hàng khách, ưu tiên phục vụ và callback hoàn thành.
- `Assets/HotPot/Scripts/Customer/Lane.cs`: class độc lập chứa tham chiếu Inspector và trạng thái runtime của một làn; giữ `[Serializable]` và tên field để bảo toàn dữ liệu đã lưu.
- `Assets/HotPot/Scripts/Customer/DishProgress.cs`: class độc lập quản lý tiến độ một món, điều kiện nhận nguyên liệu, giữ chỗ, hủy giữ chỗ và ghi nhận đáp xuống.

## Trách nhiệm

- Quản lý hàng khách của từng làn, tiến độ order và thứ tự ưu tiên nhận nguyên liệu.
- Giao hoạt cảnh phục vụ, khay và việc ghép món cho `ServingFood` riêng của mỗi làn.
- Điều phối khách vào quầy, khách rời đi, hàng chờ tiến lên và trạng thái phục vụ hết khách.
- Cung cấp nhu cầu nguyên liệu và giữ chỗ món cho booster AutoComplete.

## Tham chiếu và dữ liệu

- `IngameConfig`: thông số dùng chung, lấy qua `IngameConfig.Resolve(ingameConfig)`.
- `ConveyorChefMovement`: danh sách slot và vùng ra món của băng chuyền.
- `FinalFoodConfig`: ảnh món hoàn chỉnh và công thức nguyên liệu.
- `LevelConfig.GetQueue(side)`: hàng order của từng làn.
- `lanes`: mặc định hai phần tử, làn trái trước và làn phải sau.

Mỗi `Lane` giữ `CustomerView`, `OrderView`, `ServingFood`, các hình khách chờ, danh sách chân dung và hướng trượt. Trạng thái runtime gồm hàng order, chỉ số khách hiện tại, cờ `ordering`, dấu thứ tự đến `arrivalStamp`, tiến độ các món và pool hình khách chờ.

Mỗi `DishProgress` giữ loại món, công thức, danh sách nguyên liệu còn thiếu (`missing`), số nguyên liệu đang bay (`incoming`), cờ đã giữ chỗ đế (`baseReserved`) và cờ đã giao món (`assembled`). Nguyên liệu `None` bị bỏ qua khi dựng công thức.

## Nạp khách và dựng order

1. `LoadCustomers` bỏ qua config null; nếu hợp lệ thì dừng coroutine của manager, ẩn chữ khen và đặt lại trạng thái hoàn thành.
2. Mỗi làn lấy hàng khách theo `side`, đặt chỉ số về 0, hủy order phục vụ cũ và xóa tiến độ món.
3. Ghi nhớ các vị trí khách chờ trong scene một lần rồi bố trí lại hàng chờ.
4. Làn có khách chạy `BringNextCustomer`; làn không có khách ẩn `CustomerView`.
5. `SetupOrder` chỉ nhận món có công thức hợp lệ, giới hạn số món theo số ô của `OrderView`. Thiếu thẻ order hoặc công thức sẽ ghi cảnh báo.
6. `BeginServing` truyền loại món, công thức, thẻ order và callback đáp nguyên liệu/giao món sang `ServingFood`.
7. Thẻ hiện ảnh món, ẩn các ô dư, nhận công thức và cập nhật bố cục.
8. Khách hiện chân dung, thẻ chạy hiệu ứng hiện; sau một frame khách nhận `arrivalStamp` mới và bắt đầu order.

Khách không có món hợp lệ sẽ ghi cảnh báo và chạy luồng kết thúc ngay.

## Phục vụ từ băng chuyền

- `Update` duyệt các slot nằm trong vùng ra món qua `conveyor.IsInExitZone`.
- Chỉ lấy nguyên liệu ở slot không trống, không đang đáp xuống, có `food` và đã sơ chế (`IsPrepared`).
- `TryFindCustomer` chọn khách đang order đến sớm nhất có thể nhận nguyên liệu; trong cùng khách, ưu tiên món đứng trước trên thẻ.
- Món chỉ nhận nguyên liệu còn thiếu khi không có nguyên liệu đang bay tới và `ServingFood.CanServe` cho phép.
- Nguyên liệu đầu tiên của công thức là đế. Phải gửi đế trước; các nguyên liệu khác chỉ được gửi sau khi đế đã đáp xuống do điều kiện `incoming > 0` chặn gửi tiếp.
- Trước khi gọi `LaunchServe`, manager xóa một nguyên liệu khỏi `missing`, tăng `incoming` và đánh dấu đế nếu phù hợp.
- Nếu phóng thất bại, `CancelReservation` trả nguyên liệu vào danh sách thiếu, giảm số đang bay và bỏ cờ giữ chỗ đế khi cần.

## Đáp nguyên liệu và hoàn thành khách

- `OnIngredientDelivered` cập nhật tiến độ khi làn còn order và chỉ số món hợp lệ. Khi món hết nguyên liệu thiếu và không còn chuyến bay, yêu cầu `ServingFood.CompleteDish`.
- `OnDishDelivered` đánh dấu món đã giao; callback trùng hoặc không hợp lệ bị bỏ qua.
- Nếu khách còn món chưa giao, phát âm thanh `PlayPrepared`; nếu mọi món đã giao, chạy `FinishCustomer`.
- `FinishCustomer` tắt nhận order, hiện chữ khen tại `HeartPosition`, phát âm thanh khách vui, chờ `leaveDelay`, ẩn thẻ rồi cho khách trượt ra.
- Sau đó reset khay và tăng chỉ số hàng khách. Nếu còn khách thì cho hàng chờ tiến lên rồi đưa khách tiếp theo vào; nếu hết thì ẩn khách và kiểm tra hoàn thành.
- `CheckAllServed` chỉ đánh dấu `AllServed` và ghi log một lần khi không còn khách trong mọi làn; hàm này không tự mở màn hình thắng.

## Hàng chờ và chân dung

- `waitingFigures` trong scene được đọc theo thứ tự gần quầy nhất trước.
- Nếu có ít nhất hai vị trí, dùng khoảng cách hai vị trí cuối để nối dài hàng; nếu chỉ có một, dùng khoảng cách từ quầy tới vị trí đó; nếu không có, dùng `queueSpacing` và hướng làn.
- `EnsureFigures` bổ sung sprite khi pool chưa đủ; các hình dư được tắt để tái sử dụng.
- Khách chờ dùng `waitingTint`, `waitingScale`, cùng sorting layer với khách ở quầy và thứ tự vẽ giảm dần về phía sau.
- `StepQueueForward` chuyển hình đầu hàng tới quầy, tăng kích thước và độ sáng, đồng thời dồn các khách phía sau lên một chỗ.
- Chân dung lặp theo chỉ số khách trong danh sách `portraits` của làn.

## API cho các hệ thống khác

| API | Hành vi |
| --- | --- |
| `Instance` | Trả singleton; tìm trong scene khi chưa có instance. |
| `LoadCustomers(LevelConfig)` | Hủy luồng cũ và nạp lại các hàng khách. |
| `TryFindCustomer(FoodType)` | Kiểm tra có khách nhận được nguyên liệu ngay lúc này. |
| `AllServed` | Trạng thái mọi khách đã được phục vụ. |
| `GetNeededIngredients(List<FoodType>)` | Xóa và điền lại danh sách nhu cầu nguyên liệu của khách đang order. |
| `HasAutoCompleteTarget()` | Kiểm tra có món đủ điều kiện AutoComplete, không giữ chỗ. |
| `TryReserveAutoComplete(out AutoCompleteJob)` | Chọn món và giữ chỗ toàn bộ nguyên liệu còn thiếu. |

`GetNeededIngredients` xếp khách theo thời điểm đến, món theo thứ tự trên thẻ và nguyên liệu theo công thức. Sau đó trừ từng phần nguyên liệu đã có hoặc đang đáp xuống băng chuyền khỏi nhu cầu sớm nhất tương ứng; không yêu cầu nguyên liệu trên băng đã sơ chế. Nguyên liệu đã giữ chỗ phục vụ không còn nằm trong `missing`.

## AutoComplete

- Chỉ xét làn đang order có `ServingFood` và món còn thiếu nguyên liệu.
- Bỏ qua món đã giao, đang có nguyên liệu bay hoặc không được `CanServe` cho phép.
- Chọn món thiếu nhiều nguyên liệu nhất; nếu bằng nhau, ưu tiên khách đến sớm hơn, rồi món đứng trước trên thẻ.
- `AutoCompleteJob` chứa component `serving`, chỉ số `dish` và danh sách `ingredients` theo thứ tự công thức, đế trước.
- Giữ chỗ tất cả nguyên liệu thiếu trước khi trả job để băng chuyền không gửi trùng. Booster chịu trách nhiệm thực hiện hoạt cảnh và gửi nguyên liệu; manager theo dõi hoàn thành qua callback phục vụ.

## Vòng đời và thiết lập scene

- `ResetInstance` xóa singleton khi vào Play Mode, kể cả khi tắt domain reload.
- `Awake` đăng ký instance; component trùng bị hủy.
- `OnDestroy` của instance chính reset order từng làn rồi giải phóng singleton; component trùng không được reset khay đang hoạt động.
- Trong Editor, `OnValidate` chỉ tự gán các `ServingFood` còn thiếu khi scene có đúng hai component này, sắp theo tọa độ X từ trái sang phải.
- Khi thiếu tham chiếu, kiểm tra công cụ `Tools → HotPot → Setup Customers And References`, đồng thời bảo đảm mỗi làn được gán đúng khay và thẻ order.

## Lưu ý khi sửa

- Giữ đồng bộ `missing`, `incoming`, trạng thái đế và callback đáp nguyên liệu để tránh gửi trùng hoặc ghép món quá sớm.
- Giữ ưu tiên khách theo `arrivalStamp` và thứ tự món trên thẻ; AutoComplete có tiêu chí số nguyên liệu thiếu riêng.
- Reset order phục vụ khi nạp lại khách để hủy trạng thái cũ của khay.
- Tách quản lý tiến độ ở `CustomerManager` và hoạt cảnh ở `ServingFood`.
- Mọi hàm thêm hoặc sửa phải có comment EN/VI trực tiếp phía trên; tooltip chỉ dùng tiếng Việt theo `AGENTS.md`.
