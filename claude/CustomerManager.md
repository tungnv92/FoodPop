# CustomerManager

Tài liệu tham chiếu hệ thống khách hàng trong FoodPop, dựa trên `Assets/HotPot/Scripts/Customer/CustomerManager.cs`.

## Các class chính

- `Assets/HotPot/Scripts/Customer/CustomerManager.cs`: điều phối hàng khách, ưu tiên phục vụ và callback hoàn thành.
- `Assets/HotPot/Scripts/Customer/Lane.cs`: class độc lập chứa tham chiếu Inspector và trạng thái runtime của một làn; giữ `[Serializable]` và tên field để bảo toàn dữ liệu đã lưu.
- `Assets/HotPot/Scripts/Customer/DishProgress.cs`: class độc lập quản lý tiến độ một món, điều kiện nhận nguyên liệu, giữ chỗ, hủy giữ chỗ và ghi nhận đáp xuống.

## Trách nhiệm

- Quản lý một hàng khách chung, phân khách lần lượt cho hai chỗ phục vụ, theo dõi tiến độ order và thứ tự ưu tiên nhận nguyên liệu.
- Giao hoạt cảnh phục vụ, khay và việc ghép món cho `ServingFood` riêng của mỗi làn.
- Điều phối khách vào quầy, khách rời đi, hàng chờ tiến lên và trạng thái phục vụ hết khách.
- Cung cấp nhu cầu nguyên liệu và giữ chỗ món cho booster AutoComplete.

## Tham chiếu và dữ liệu

- `IngameConfig`: thông số dùng chung, lấy qua `IngameConfig.Resolve(ingameConfig)`.
- `ConveyorChefMovement`: danh sách slot và vùng ra món của băng chuyền.
- `FinalFoodConfig`: công thức để đọc level cũ và tên/ảnh tùy chọn khi nhóm nguyên liệu khớp một món có sẵn.
- `LevelConfig.GetCustomerQueue()`: trả về bản sao danh sách khách chung; runtime không dùng `GetQueue(side)` để phân khách nữa.
- `LevelConfig.customerQueue`: danh sách khách theo thứ tự. Khi `useUnifiedCustomerQueue` bật hoặc danh sách này có phần tử, nó được ưu tiên hơn dữ liệu cũ.
- `LevelConfig.useUnifiedCustomerQueue`: cho phép danh sách chung rỗng có nghĩa là không có khách, tránh lấy lại khách từ dữ liệu cũ.
- `leftQueue` / `rightQueue`: giữ để tương thích level cũ. Khi chưa dùng hàng chung, đọc xen kẽ `trái[0], phải[0], trái[1], phải[1], ...`; hàng dài hơn vẫn giữ đủ phần tử. `TotalCustomers` đếm theo cùng quy tắc.
- `lanes`: mặc định hai phần tử, làn trái trước và làn phải sau.

Manager giữ `customerQueue` là bản sao hàng chung, `nextCustomer` là chỉ số khách chưa phân chỗ tiếp theo và `waitingLane` là làn hợp lệ đầu tiên được dùng làm hàng chờ hiển thị chung.

Mỗi `Lane` giữ `CustomerView`, `OrderView`, `ServingFood`, các hình khách chờ, danh sách chân dung và hướng trượt. `queue` runtime chỉ chứa lịch sử khách đã được phân vào chỗ này; `queueIndex` chỉ khách hiện tại trong lịch sử đó, không phải vị trí trong hàng chung. `customerNumber` là chỉ số khách toàn cục, bắt đầu từ 0. Các trạng thái khác gồm `ordering`, `arrivalStamp`, tiến độ món, pool hình chờ và `walker` riêng cho khách đang đi vào quầy.

Mỗi `DishProgress` giữ loại món, công thức, danh sách nguyên liệu còn thiếu (`missing`), số nguyên liệu đang bay (`incoming`), cờ đã giữ chỗ đế (`baseReserved`) và cờ đã giao món (`assembled`). Nguyên liệu `None` bị bỏ qua khi dựng công thức.

## Nạp khách và dựng order

1. `LoadCustomers` bỏ qua config null; nếu hợp lệ thì dừng coroutine cũ, ẩn chữ khen, lấy bản sao hàng chung và đặt lại `nextCustomer`, `arrivalCounter`, `waitingLane`, trạng thái hoàn thành.
2. Mỗi làn nhận lịch sử phân khách rỗng, chỉ số về 0, tắt nhận order, reset khay và xóa tiến độ món. Ghi nhớ vị trí hàng chờ một lần, ẩn hình chờ, hình đang di chuyển và khách cũ.
3. Chọn làn hợp lệ đầu tiên làm `waitingLane`. Duyệt các chỗ theo thứ tự `lanes`, gọi `AssignNextCustomer` rồi chạy `BringNextCustomer`: mặc định khách 1 vào trái, khách 2 vào phải; nếu chỉ có một khách thì chỉ dùng một chỗ.
4. `AssignNextCustomer` chỉ nhận khách nếu còn khách chung và chỗ đó chưa có khách chưa hoàn tất. Nó lưu `customerNumber`, thêm order vào lịch sử của làn rồi tăng `nextCustomer` ngay, trước khi coroutine hoạt cảnh nhường lượt, để tránh lấy trùng khách.
5. `SetupOrder` gọi `CustomerOrder.GetIngredientOrders`, chỉ nhận nhóm có 1–6 FoodType hợp lệ và giới hạn tối đa 4 nhóm theo số ô của `OrderView`. Không yêu cầu nhóm khớp món có tên; thiếu thẻ hoặc nhóm không hợp lệ sẽ ghi cảnh báo.
6. `BeginServing` truyền loại món, công thức, thẻ order và callback đáp nguyên liệu/giao món sang `ServingFood`.
7. Thẻ nhận danh sách nguyên liệu, ẩn các ô dư, cập nhật bố cục rồi gọi `ShowIngredientOrders()` để luôn hiện ảnh prepared của từng nguyên liệu. Tên/ảnh món khớp chỉ là dữ liệu tùy chọn.
8. `CustomerView.SetCustomerType` nhận Normal/Boss/Shipper từ order; khách hiện chân dung theo số thứ tự toàn cục, thẻ chạy hiệu ứng hiện và hàng chờ chung được bố trí lại. Sau một frame khách nhận `arrivalStamp` mới và bắt đầu order.

Ví dụ: ban đầu phục vụ khách 1 và 2. Nếu khách 2 rời quầy trước, chỗ bên phải lấy khách 3; khi khách 1 rời quầy, chỗ bên trái lấy khách 4. Không chia trước khách số lẻ/chẵn cho từng làn.
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
- Sau khi khách trượt ra xong, reset khay và tăng `lane.queueIndex`. Gọi `AssignNextCustomer` để lấy khách tiếp theo từ hàng chung vào chỗ vừa trống, rồi chạy `StepQueueForward` và `BringNextCustomer`. Nếu hết khách chung thì ẩn khách ở chỗ đó và kiểm tra hoàn thành.
- `CheckAllServed` chỉ đánh dấu `AllServed` và ghi log một lần khi `nextCustomer` đã hết hàng chung và mọi làn đã hoàn tất khách được phân vào. Khách đang đi vào hoặc đang rời quầy vẫn được tính là chưa hoàn tất; hàm này không tự mở màn hình thắng.

## Hàng chờ và chân dung

- `waitingFigures` trong scene được đọc theo thứ tự gần quầy nhất trước. Chỉ pool của `waitingLane` hiển thị hàng chờ chung; hình chờ ở các làn khác bị tắt. Số người chờ bằng `customerQueue.Count - nextCustomer`, không gồm khách đã giữ chỗ dù chưa tới quầy.
- Nếu có ít nhất hai vị trí, dùng khoảng cách hai vị trí cuối để nối dài hàng; nếu chỉ có một, dùng khoảng cách từ quầy tới vị trí đó; nếu không có, dùng `queueSpacing` và hướng làn.
- `EnsureFigures` bổ sung sprite khi pool chưa đủ; các hình dư được tắt để tái sử dụng.
- Khách chờ dùng `waitingTint`, `waitingScale`, cùng sorting layer với khách ở quầy và thứ tự vẽ giảm dần về phía sau.
- `StepQueueForward` dùng `lane.walker` riêng, đặt chân dung khách đã giữ chỗ tại vị trí đầu hàng chung rồi di chuyển tới quầy, tăng kích thước và độ sáng. Hai làn có thể chạy hoạt cảnh vào quầy đồng thời mà không tranh cùng sprite.
- Hàng chờ còn lại được bố trí lại ngay theo `nextCustomer`; hiện tại không nội suy chuyển chỗ của các hình phía sau trong lúc `walker` di chuyển.
- Chân dung khách chờ, khách đang đi và khách tại cả hai quầy đều lặp theo chỉ số toàn cục trong `waitingLane.portraits`.
- Khi nạp lại level, coroutine cũ bị dừng và mọi `walker` bị tắt trước khi phân khách lại.

## API cho các hệ thống khác

| API | Hành vi |
| --- | --- |
| `Instance` | Trả singleton; tìm trong scene khi chưa có instance. |
| `LoadCustomers(LevelConfig)` | Hủy luồng cũ, nạp hàng chung và phân khách đầu tiên cho các chỗ phục vụ. |
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

## Chỉnh khách trong Unity Editor

- `LevelConfigEditorWindow` hiển thị một danh sách **Hàng khách chung · hai chỗ phục vụ**.
- Với asset chưa bật chế độ hàng chung và chưa có phần tử trong `customerQueue`, tool hiện nút **Chuyển sang danh sách khách chung**. Thao tác có Undo, sao chép thứ tự tương thích từ `GetCustomerQueue()`, bật `useUnifiedCustomerQueue` và đánh dấu asset cần lưu.
- Các field trái/phải cũ vẫn được giữ trong asset; sau khi chuyển, hàng chung được ưu tiên, kể cả khi người dùng xóa hết khách.
- Kiểm tra tổng nguyên liệu order trong tool dùng `GetCustomerQueue()` để thống nhất với runtime.

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
- Giữ việc lấy khách chung đồng bộ trước khi nhường coroutine; không đặt trước khách tiếp theo vào từng làn hoặc tăng `nextCustomer` thêm trong hoạt cảnh.
- Phân biệt `customerNumber` toàn cục với `queueIndex` trong lịch sử riêng của làn. Điều kiện hoàn tất phải xét cả hàng chung và khách đã phân chỗ.
- Tách quản lý tiến độ ở `CustomerManager` và hoạt cảnh ở `ServingFood`.
- Mọi hàm thêm hoặc sửa phải có comment EN/VI trực tiếp phía trên; tooltip chỉ dùng tiếng Việt theo `AGENTS.md`.


## Order nguyên liệu và loại khách (29/10/2026)

- `CustomerOrder.customerType`: `Normal = 0`, `Boss = 1`, `Shipper = 2`; khách cũ mặc định Normal. Hiện ba loại dùng chung logic phục vụ, chưa có luật riêng hay ảnh nhân vật riêng.
- `useIngredientOrders` và `ingredientOrders[].ingredients` là dữ liệu order chính ở Unity và web. Mỗi khách tối đa 4 nhóm, mỗi nhóm 1–6 nguyên liệu; phần tử đầu là đế, phần tử lặp giữ số lượng.
- `GetIngredientOrders(FinalFoodConfig)` sao chép dữ liệu. Khi cờ bật, danh sách rỗng là rỗng thật. Level cũ chưa có nhóm được tra từ `finalFoods`, không sửa asset nguồn lúc chơi.
- `FinalFoodConfig.FindMatchingRecipe` chỉ tìm tên/ảnh tùy chọn bằng đúng số lượng từng nguyên liệu. Nhóm chưa có công thức vẫn nhận đủ nguyên liệu và hoàn tất; không bắt buộc thêm FinalFoodType.
- `OrderView.ShowIngredientOrders` hiển thị nguyên liệu prepared trực tiếp. Với nhóm chưa có ảnh món, ServingFood bỏ qua ghép/bay ảnh món và vẫn báo hoàn thành sau khi nguyên liệu đáp xuống.
- Level Editor: chọn Loại khách trong từng khách; chỉnh nguyên liệu trực tiếp cho mỗi nhóm. Web xuất `.asset` giữ hàng chung và mảng nguyên liệu đóng gói little-endian; lưu server giữ cả loại khách.
- CustomerView nhận loại khách khi vào quầy, cung cấp `Type` và nhãn TMP tùy chọn. Không cần gán nhãn để phục vụ.

### Thời gian phục vụ `timeServe`

| Thuộc tính | Quy tắc hiện tại |
| --- | --- |
| Kiểu dữ liệu | `float`, đơn vị giây, mặc định `30f` trên `CustomerOrder`. |
| Boss / Shipper | Level Editor và web hiện **Thời gian phục vụ (s)**; giá trị phải hữu hạn và lớn hơn 0. |
| Normal | Ẩn ô chỉnh và bỏ qua giá trị; chuyển về Boss/Shipper giữ thời gian đã nhập. |
| Tương thích | Khách Boss/Shipper chưa có trường này được dùng mặc định 30 giây. |
| Lưu trữ | Web giữ giá trị khi lưu server và xuất/nhập `.asset`; mô phỏng giữ trong snapshot khách. |
| Runtime | Chưa có đếm ngược, thanh thời gian, khách bỏ đi hoặc điều kiện thua khi hết giờ. `CustomerView` hiện chỉ nhận loại khách. |

Không dùng `timeServe` để thay đổi tốc độ băng chuyền, thời gian sơ chế hoặc thời gian bay nguyên liệu. Chi tiết UI Unity tại [LevelEditor.md](LevelEditor.md), dữ liệu web tại [LevelStudioWeb.md](../Assets/HotPot/Documentation/LevelStudioWeb.md).
