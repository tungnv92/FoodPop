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

- `IngameConfig`: thông số dùng chung, lấy qua `IngameConfig.Resolve(ingameConfig)`. Bảng icon nguyên liệu dùng `IngameConfig.Instance.FoodIcons`, gán một lần trên asset; OrderView không còn field FoodIconConfig riêng.
- `ConveyorChefMovement`: danh sách slot của băng chuyền.
- (Đã gỡ 2026-11-03) `FinalFoodConfig` và field `finalFoodConfig` không còn trên `CustomerManager`; công thức lấy thẳng từ `order.ingredientOrders`, ảnh món lấy từ prefab `IngredientOrder.dish` (`FinalFoodView`).
- `LevelConfig.GetCustomerQueue()`: trả về bản sao danh sách khách chung; runtime không dùng `GetQueue(side)` để phân khách nữa.
- `LevelConfig.customerQueue`: danh sách khách theo thứ tự. Khi `useUnifiedCustomerQueue` bật hoặc danh sách này có phần tử, nó được ưu tiên hơn dữ liệu cũ.
- `LevelConfig.useUnifiedCustomerQueue`: cho phép danh sách chung rỗng có nghĩa là không có khách, tránh lấy lại khách từ dữ liệu cũ.
- `leftQueue` / `rightQueue`: giữ để tương thích level cũ. Khi chưa dùng hàng chung, đọc xen kẽ `trái[0], phải[0], trái[1], phải[1], ...`; hàng dài hơn vẫn giữ đủ phần tử. `TotalCustomers` đếm theo cùng quy tắc.
- `lanes`: mặc định hai phần tử, làn trái trước và làn phải sau.

Manager giữ `customerQueue` là bản sao hàng chung, `nextCustomer` là chỉ số khách chưa phân chỗ tiếp theo và `waitingLane` là làn hợp lệ đầu tiên được dùng làm hàng chờ hiển thị chung.

Mỗi `Lane` giữ `CustomerView`, `OrderView`, `ServingFood`, các hình khách chờ, danh sách chân dung và hướng trượt. `queue` runtime chỉ chứa lịch sử khách đã được phân vào chỗ này; `queueIndex` chỉ khách hiện tại trong lịch sử đó, không phải vị trí trong hàng chung. `customerNumber` là chỉ số khách toàn cục, bắt đầu từ 0. Các trạng thái khác gồm `ordering`, `arrivalStamp`, tiến độ món, pool hình chờ và `walker` riêng cho khách đang đi vào quầy.

Mỗi `DishProgress` giữ prefab món (`dish`, kiểu `FinalFoodView`, có thể null; thay cho `type` kiểu `FinalFoodType` cũ), công thức, danh sách nguyên liệu còn thiếu (`missing`), số nguyên liệu đang bay (`incoming`), cờ đã giữ chỗ đế (`baseReserved`) và cờ đã giao món (`assembled`). Nguyên liệu `None` bị bỏ qua khi dựng công thức.

## Nạp khách và dựng order

1. `LoadCustomers` bỏ qua config null; nếu hợp lệ thì dừng coroutine cũ, ẩn chữ khen, lấy bản sao hàng chung và đặt lại `nextCustomer`, `arrivalCounter`, `waitingLane`, trạng thái hoàn thành.
2. Mỗi làn nhận lịch sử phân khách rỗng, chỉ số về 0, tắt nhận order, reset khay và xóa tiến độ món. Ghi nhớ vị trí hàng chờ một lần, ẩn hình chờ, hình đang di chuyển và khách cũ.
3. Chọn làn hợp lệ đầu tiên làm `waitingLane`. Duyệt các chỗ theo thứ tự `lanes`, gọi `AssignNextCustomer` rồi chạy `BringNextCustomer`: mặc định khách 1 vào trái, khách 2 vào phải; nếu chỉ có một khách thì chỉ dùng một chỗ.
4. `AssignNextCustomer` chỉ nhận khách nếu còn khách chung và chỗ đó chưa có khách chưa hoàn tất. Nó lưu `customerNumber`, thêm order vào lịch sử của làn rồi tăng `nextCustomer` ngay, trước khi coroutine hoạt cảnh nhường lượt, để tránh lấy trùng khách.
5. `SetupOrder` đọc thẳng `order.ingredientOrders` (không còn `GetIngredientOrders`), tạo `DishProgress(group.dish, group.ingredients)`, chỉ nhận nhóm có 1–6 FoodType hợp lệ và giới hạn tối đa 4 nhóm theo số ô của `OrderView`. Không yêu cầu nhóm khớp món có tên; thiếu thẻ hoặc nhóm không hợp lệ sẽ ghi cảnh báo.
6. `BeginServing` truyền loại món, công thức, thẻ order và callback đáp nguyên liệu/giao món sang `ServingFood`.
7. Thẻ nhận danh sách nguyên liệu, ẩn các ô dư, rồi chỉ gọi `Layout(count)` để cập nhật bố cục (từ 2026-11-03 `SetupOrder` không còn gọi `lane.orderView.ShowIngredientOrders()`). Tên/ảnh món khớp chỉ là dữ liệu tùy chọn.
8. `CustomerView.SetCustomerType` nhận Normal/Boss/Shipper từ order; khách hiện chân dung theo số thứ tự toàn cục, thẻ chạy hiệu ứng hiện và hàng chờ chung được bố trí lại. Sau một frame khách nhận `arrivalStamp` mới và bắt đầu order.

Ví dụ: ban đầu phục vụ khách 1 và 2. Nếu khách 2 rời quầy trước, chỗ bên phải lấy khách 3; khi khách 1 rời quầy, chỗ bên trái lấy khách 4. Không chia trước khách số lẻ/chẵn cho từng làn.
Khách không có món hợp lệ sẽ ghi cảnh báo và chạy luồng kết thúc ngay.

## Phục vụ từ băng chuyền

- `Update` duyệt mọi slot trên băng (đã bỏ vùng ra món / Station Exit từ 09/11/2026): món vừa sơ chế xong, đáp lại đĩa là có thể bay ngay tới order phù hợp, không phải chạy tới đoạn ra món.
- Chỉ lấy nguyên liệu ở slot không trống, không đang đáp xuống, có `food` và đã sơ chế (`IsPrepared`).
- Món phải nằm trên đĩa ít nhất `Serve Delay After Landing` giây (mặc định 0,2s, field trên CustomerManager) kể từ lúc đáp (`ConveyorSlot.LandedTime`, ghi trong `CompleteFoodArrival`) rồi mới được bay vào order. Chưa biên dịch và chưa chạy Play Mode sau thay đổi này (09/11/2026).
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
- Khi thiếu tham chiếu, gán lại trực tiếp trong Inspector và bảo đảm mỗi làn được gán đúng khay và thẻ order. (Công cụ Setup Customers And References đã bị xóa 04/11/2026.)
- `GetNeededIngredients` liệt kê mọi nguyên liệu còn thiếu, kể cả nguyên liệu khách chưa nhận được ngay. Túi giữ món (`FoodHoldingStorage`) lọc thêm bằng `TryFindCustomer` nên chỉ trả món khách nhận được ngay (04/11/2026).

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
- [Cập nhật 2026-11-03: `useIngredientOrders` đã bị gỡ khỏi Unity] `ingredientOrders[].ingredients` là dữ liệu order chính. Mỗi khách tối đa 4 nhóm, mỗi nhóm 1–6 nguyên liệu; phần tử đầu là đế, phần tử lặp giữ số lượng.
- [Đã thay thế 2026-11-03] `GetIngredientOrders(FinalFoodConfig)` và `finalFoods` đã bị gỡ; 10 level `LevelConfig_01..10` đã được chuyển sang `ingredientOrders` (cùng nguyên liệu). Danh sách rỗng là rỗng thật.
- [Đã thay thế 2026-11-03] `FinalFoodConfig.FindMatchingRecipe` đã bị gỡ. Ảnh món là prefab `dish` (`FinalFoodView`) lưu trong nhóm, `OrderItemView.ShowDish(FinalFoodView)` hiện trên thẻ (một instance pool cho mỗi prefab, con của renderer icon, co vừa ô bằng `GetLocalSize()` và `dishSizeRatio`, vẽ ở sorting order của icon qua `SetSorting`; renderer icon không còn sprite; `DishShadow` sau đó đã bị gỡ hoàn toàn cùng nhãn số lượng của ô; ô thẻ chỉ còn prefab món và dấu tick). `ServingFood.BeginOrder(view, dishes, recipes, callbacks)`: nhóm có prefab và có chồng khay thì ghép lớp rồi hiện instance prefab trên ô khay và bay lên thẻ; nhóm không có prefab chỉ hoàn tất (thẻ tick). Inspector debug của `TrayDishStack` hiện tên prefab. Đã compile không lỗi trong Unity 03/11, chưa chơi thử. Trên khay, ô giờ dựng ảnh món từng lớp (`DishSlotView.BeginDish`/`RevealIngredient`), xem ServingFood.md.
- `OrderView.ShowIngredientOrders` (vẫn còn trong `OrderView` nhưng `CustomerManager` không còn gọi từ 2026-11-03) hiển thị nguyên liệu prepared trực tiếp. Với nhóm chưa có ảnh món, ServingFood bỏ qua ghép/bay ảnh món và vẫn báo hoàn thành sau khi nguyên liệu đáp xuống.
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
| Runtime | Có đếm ngược + thanh dọc + thua/hồi sinh khi hết giờ (05/11/2026, xem mục dưới). Không có khách bỏ đi. |

Không dùng `timeServe` để thay đổi tốc độ băng chuyền, thời gian sơ chế hoặc thời gian bay nguyên liệu. Chi tiết UI Unity tại [LevelEditor.md](LevelEditor.md), dữ liệu web tại [LevelStudioWeb.md](../Assets/HotPot/Documentation/LevelStudioWeb.md).

## Tiến độ khách cho TopPanel (03/11/2026)

RemainingCustomers đếm tổng hàng chung đã nạp trừ số khách hoàn tất rời quầy. LoadCustomers reset completedCustomers và phát RemainingCustomersChanged sau khi thiết lập hàng mới; FinishCustomer tăng completedCustomers và phát số còn lại ngay sau lane.queueIndex++. Khách đang bay món, đang phục vụ hoặc đang rời quầy vẫn được tính cho tới khi rời xong. UICustomer trên TopPanel theo dõi sự kiện, đọc giá trị hiện tại khi bật, gỡ đăng ký khi tắt; không polling và không thay đổi thứ tự/phân khách.

Kiểm chứng HUD: Play Mode xác nhận reset số còn lại khi nạp/chơi lại, giảm 1→0 sau luồng rời quầy của order rỗng chủ ý, hàng rỗng hiện 0 và bật lại HUD đọc đúng trạng thái hiện tại. Đã thoát Play Mode.

## Thanh thời gian phục vụ Boss/Shipper (05/11/2026)

Người dùng duyệt demo: thanh dọc **nằm giữa ảnh khách và thẻ order** (sau đó chốt **đặt trên OrderView**, không đặt trên khách), rút dần từ trên xuống; xanh > 50%, vàng 25–50%, đỏ + nháy < 25%. Hết giờ → **thua màn có hồi sinh**; hồi sinh **reset thời gian** của khách hết giờ. Thời gian dừng khi pause/popup (đếm bằng `Time.deltaTime`, dừng khi `Time.timeScale = 0`); vẫn chạy khi booster đang chạy.

| Phần | Chi tiết |
| --- | --- |
| `CustomerTimerBar` (`Scripts/Customer/CustomerTimerBar.cs`) | Field `background`, `fill1`, `fill2` (SpriteRenderer). `Show(remainingSeconds, totalSeconds)`, `SetProgress(remainingSeconds, totalSeconds)`, `Hide()`, property `FillAmount` trong 0–1. Trên 5 giây dùng Fill1; còn 0–5 giây dùng Fill2; tại 0 cả hai ẩn. Shader cắt dọc từ dưới lên như UI Image.fillAmount, không đổi scale/size/vị trí hay màu thiết kế. |
| `OrderView` | Field `timerBarPrefab` (prefab `Prefabs/OrderView/TimerBar`), instance riêng `timerBar` (không serialize) tạo bằng `GetOrCreateTimerBar()` lần đầu có khách tính giờ rồi dùng lại; `timerBarOnLeft` (mặc định bật: khách đứng bên trái thẻ ở cả hai làn), `timerBarGap` 0.08; property `TimerBar`. `PlaceTimerBar()` cuối `Layout` đặt thanh ngay ngoài mép nền phía khách (nền rộng theo số món). Thanh là con của root thẻ nên co/nảy cùng `PlayHide`/`PlayShow`. `CustomerView` không còn field thanh. |
| `Lane` | `serveTimeTotal`, `serveTimeLeft` (NonSerialized); `serveTimeTotal = 0` = khách không tính giờ. |
| `CustomerManager` | `StartServeTimer` trong `BringNextCustomer` (sau `SetVisible(true)`): Boss/Shipper có `timeServe > 0` → đầy + `Show(lane.serveTimeLeft, lane.serveTimeTotal)`, khách khác → `Hide()`. `TickServeTimers` đầu `Update` (trước kiểm tra `BoosterPaused`), chỉ khi `lane.ordering`. `StopServeTimer` đầu `FinishCustomer` (đủ món) và trong `LoadCustomers`. `GetTimerBar` lấy từ `lane.orderView.TimerBar`. API: `ServeTimeExpired`, `ResetExpiredServeTimers()`. |
| `LevelController` | `LateUpdate`: sau kiểm tra thắng, `ServeTimeExpired` → `OfferRevive(CanvasRevive.ReviveCase.OutOfTime)` (trước kiểm tra kẹt băng). `reviveCase == CanvasRevive.ReviveCase.OutOfTime`: popup hồi sinh luôn khả dụng; `ReviveLevel` khi hết giờ → ẩn popup, trả timeScale, `ResetExpiredServeTimers` cộng 30 giây cho khách hết giờ, về `Playing` (không chạy Clean). Từ chối → `Lost`. Hồi sinh do kẹt băng giữ nguyên luồng Clean cũ. |

Prefab riêng `Prefabs/OrderView/TimerBar.prefab` (root `CustomerTimerBar`, con `Background` sprite `Art/Sprites/Demo/RoundedPanel` Sliced 0.22×3.1 order 14 nâu mờ, `Fill` 0.14×3.02 order 15), đã gán vào `OrderView.timerBarPrefab` của prefab OrderView. `CustomerManager.StartServeTimer` gọi `GetOrCreateTimerBar()` chỉ khi khách Boss/Shipper; khách thường chỉ ẩn thanh nếu đã có. Chưa compile/play test phía Claude.


## Cập nhật cảnh báo thoát và âm thanh (10/11/2026)
Quy tắc hiện hành thay thế mô tả luôn mở cảnh báo trước đây: Home/Replay từ Pause chỉ hiện WarningQuit nếu CustomerManager.HasCompletedOrder=true (đã giao đủ một order trong lượt chơi). Chưa có order hoàn tất: đi thẳng, không trừ life. Sau order đầu: Confirm mới gọi UseHeart; unlimited không trừ. LoadCustomers reset cờ khi tải/chơi lại màn. Giao một phần hoặc bỏ qua order rỗng không tính.

IngameSoundManager dùng 10 clip trong Assets/HotPot/Audio/Sounds: click=SFX_INGAME_CLICK; mở/đóng popup=sfx_ui_popup_show/sfx_ui_popup_disappear; mua thành công=SFX_INGAME_BUY_ITEM; thưởng Watch booster=SFX_INGAME_REWARD_COLLECT; đề nghị revive=SFX_INGAME_OUT_OF_SPACE; giới thiệu booster=SFX_INGAME_NEW_ITEM_UNLOCK; thắng/thua=SFX_INGAME_GAME_WIN/SFX_INGAME_GAME_FAILED; hồi sinh Ads hoặc Coin thành công=sfx_game_revive. UICanvasPrime phát âm vòng đời popup, UIManager phát kết quả màn. Mọi clip dùng nguồn SFX chung nên tuân theo Sound, hoạt động khi pause. Giữ âm chế biến/phục vụ hiện có; thư mục này không cung cấp nhạc nền mới.

Kiểm chứng: Unity biên dịch thay đổi và đã gán/lưu đủ 10 clip trong scene, mỗi clip có thời lượng >0. Chưa kiểm tra nghe trực tiếp hoặc toàn bộ luồng chơi sau thay đổi này.


## Hướng dẫn màn 1 (10/11/2026)
Scene HotPotDemo có object LevelOneTutorial với LevelOneTutorial; tham chiếu LevelController, FoodBoardManager, CustomerManager và prefab Assets/HotPot/Prefabs/UI/Tutorial/SelectDishOverlay.prefab. Prefab riêng tái sử dụng cơ chế định vị BoosterTrialOverlay và bàn tay Spine; hiện bàn tay cùng bốn tấm nền tối, Hole và Prompt. Bàn tay có localScale1.2, lớn hơn hình gốc20%; scaleGuideWithFocus=false giữ nhóm Guide ở scale1 để kích thước bàn tay giống nhau giữa hai bước. Giữ Focus làm anchor định vị không có hình. sortingOrder29900 dưới popup30000, toàn bộ graphic không chặn raycast. Không thay prefab hướng dẫn booster. Không dừng thời gian hoặc khóa thao tác gameplay.

Khi LevelLoaded nhận levelNumber1, chờ0.35 giây cho đĩa hiện rồi trỏ cố định đĩa đầu cột thứ2 (index1) với Select a dish!. DishPicked thành công mới đóng bước này, chạm trượt không đóng. CustomerManager.CustomerArrived(index, order) phát khi khách hợp lệ sẵn sàng gọi món, index là vị trí queue bắt đầu từ0. Khách đầu tiên(index0) của level2 kích hoạt Tap the order to see ingredients! trên món đầu của thẻ; level1 không hiện bước xem order. OrderView.IngredientsOpened chỉ phát khi mở nguyên liệu thành công bằng SetExpanded; sự kiện này kết thúc hướng dẫn order tương ứng.

Tải/chơi lại reset hướng dẫn tương ứng: level1 chọn đĩa, level2 xem order khách đầu; level khác không hiện; rời Playing hoặc tắt object sẽ hủy coroutine/overlay và gỡ sự kiện. Không lưu cờ tutorial vào profile. Kiểm chứng Play Mode output/Tutorial/Check.txt PASS chọn đĩa thật, khách thứ3 và mở chi tiết, reset màn1, không hiện màn2. Chưa kiểm tra chạm trên thiết bị vật lý.

Cập nhật hiển thị: hai bước tutorial level1 hiện bàn tay lớn hơn20% với kích thước cố định, nền tối, hole và chữ nhắc. Bước order lấy tâm ảnh món đầu từ OrderItemView.TargetPosition và kích thước DishSize theo scale world, không lấy collider thẻ. Bước chọn đĩa trỏ đĩa đầu cột2. Luồng sự kiện hoàn tất giữ nguyên.

Kiểm chứng bố trí mới trong Play Mode: nền tối/hole đều bật, localScale bàn tay=1.2; tâm Focus khớp đĩa đầu cột2 và tâm hình món order (sai số dưới0.5% kích thước màn hình). PASS cả hai bước, reset màn1 và không hiện màn2; kết quả output/Tutorial/Check.txt. Đã về Edit Mode.

Kích thước bàn tay: BoosterTrialOverlay có scaleGuideWithFocus mặc định true để giữ hành vi booster cũ. SelectDishOverlay đặt false: Guide.localScale=1, Hand.localScale=1.2 cố định, không phụ thuộc đường kính hole. Prompt bật cho cả Select a dish! và Tap the order to see ingredients!. Kiểm tra trực tiếp Show với bounds1x và3x: hand.lossyScale không đổi, chữ hướng dẫn đang bật và đổi đúng nội dung.

Prompt tutorial dùng TextMeshProUGUI, font Assets/Fonts/FredokaOne-Regular Color.asset, fontSize45, tắt auto sizing. BoosterTrialOverlay.promptLabel nhận TMP và Show cập nhật nội dung cho cả hai bước; tip kiểu Text cũ giữ riêng cho prefab booster hiện có. Kiểm tra prefab đã gán đúng font/cỡ45 và Show đổi đúng lời nhắc order.

Vị trí hole chọn đĩa: lấy tâm Food.IconRenderer.bounds của đĩa đầu cột2; truyền bao vuông có cạnh bằng chiều lớn nhất của hình món. Vì vậy hole nằm ở tâm món, không bị kéo xuống theo nền đĩa/bóng hoặc công thức canh mép trên của BoosterTrialOverlay.Show. Bàn tay giữ cỡ cố định và Prompt TMP45 như trước.

LevelOneTutorial đặt sẵn trên object cùng tên trong scene là controller nhẹ theo sự kiện; chỉ tạo overlay ở màn1 (chọn đĩa) và màn2 (order khách đầu), màn khác không tạo UI/coroutine hướng dẫn. Giữ object hoạt động để nhận LevelLoaded khi chuyển/chơi lại màn, không cần tạo/hủy controller riêng cho từng level. Đổi tên script/class giữ nguyên GUID e681708764fd49a4abf7b3817ca18c24 và các tham chiếu Inspector.


## Hoạt cảnh riêng cho món (10/11/2026)
FinalFoodView có cờ Inspector isAnimate (mặc định false) và tham chiếu foodAnimation kiểu FinalFoodAnimation. Base abstract đặt tại Assets/HotPot/Scripts/Food/FinalFoodAnimation.cs, gồm Play() coroutine, ResetAnimation(), SetSorting(); OnDisable reset đạo cụ khi pool/replay. Món khác có thể kế thừa base, gán component lên prefab và bật isAnimate, không sửa ServingFood theo từng loại món.

DishSlotView.RevealIngredient chạy PlayPreparation ngay khi nguyên liệu cuối đáp xuống với isAnimate=true, không hiện lớp cuối trước hoạt cảnh. ServingFood.AssembleDish chờ IsPreparing=false trước khi ghép và bay món; MergeStack không chạy hoạt cảnh lần thứ hai. Cờ tắt hoặc không có component thì không chờ hoạt cảnh. Chỉ instance món hoàn chỉnh trên khay gọi PlayAnimation; instance thẻ order không tự chạy. Hoạt cảnh dùng Time.deltaTime nên tuân theo Pause. SetSorting của FinalFoodView cập nhật cả đạo cụ ngoài Layers.

FinalFood_10 bật isAnimate, gán SaltedEdamameAnimation → AnimatorFinalFoodAnimation → FinalFoodAnimation. Controller/clip trong Assets/HotPot/Animations/Food/SaltedEdamame: Idle (ảnh hoàn chỉnh, không đạo cụ), Prepare dài1.15giây (hiện/nghiêng/lắc salt_1 với kích thước thiết kế cố định, chuyển edamame_2 sang edamame_3). Anims tắt và Animator disabled mặc định; chỉ Play mới bật Animator. salt_1 là đạo cụ, không thêm vào Layers nên công thức vẫn Edamame+Salt.

Kiểm chứng Unity: biên dịch thành công; Play Mode dùng instance prefab thật kiểm tra isAnimate=false bỏ qua coroutine, true hoàn tất và chuyển hình, sorting muối trên lớp món, tắt giữa chừng reset pose/ẩn đạo cụ. Báo cáo output/FoodAnimation/Check.txt; đã về Edit Mode. Chưa kiểm tra toàn bộ lượt chơi level12/13/15 hoặc tinh chỉnh nhịp trên thiết bị.

### Animator và particle muối
AnimatorFinalFoodAnimation dùng chung cho món có clip: Play bật state Prepare và chờ completed; các Animation Event trên cùng root gọi PlayParticles ở0.08s, StopParticles ở0.6s, ShowSaltedDish ở1.05s và CompleteAnimation ở1.14s. Không chuyển state tự động hoặc đặt timer thay thế event: khi chỉnh clip cần giữ event hoàn tất để không chặn luồng phục vụ. Clip Idle/Prepare chỉnh trực tiếp trong Animation window trên FinalFood_10.

Anims/SaltParticles là ParticleSystem nằm trong prefab: hạt nhỏ0.035–0.065, lifetime0.22–0.3s, tốc độ xuống2–2.6 local units/s, emission80/s trong đoạn rắc, playOnAwake=false, loop=false, thời gian scaled. Material Assets/HotPot/Art/Materials/Particles/SaltGrains.mat tái dùng shader SpriteAdditive và texture FxGrain có sẵn; không tạo ảnh mới. Sorting props=base+2, hạt=base+3 cùng sorting layer món. ResetAnimation rebind Idle, disable Animator, dừng/xóa hạt và ẩn Anims. Món trên order không gọi Play, không tự rắc muối.

Kiểm chứng thay coroutine transform bằng Animator: Unity Play Mode với prefab thật PASS isAnimate=false bỏ qua, particle có hạt thật, Pause giữ tiến độ Animator, ngắt/pool xóa hạt và đạo cụ, tái dùng chạy hết qua Animation Event với hạt đã kết thúc trước khi phục vụ. output/FoodAnimation/AnimatorCheck.txt; đã về Edit Mode. Chưa kiểm tra toàn bộ level12/13/15 hoặc thiết bị.

Clip Prepare giữ m_LocalScale của Anims/salt_1 bằng scale thiết kế prefab (1,1,1) từ đầu đến cuối; không thu/phóng lọ muối. Vị trí, góc xoay, thời gian phát particle và event hoàn tất giữ nguyên. Đã kiểm tra các curve scale tại đầu/giữa/cuối đều=1.

Sửa trạng thái khởi tạo khay: Idle/Prepare không còn curve SpriteRenderer.m_Enabled trên edamame_2/edamame_3 vì Animator có thể ghi đè trạng thái ẩn của DishSlotView khi bind/kích hoạt. SaltedEdamameAnimation.Play mới chủ động hiện đậu thường/ẩn đậu muối; ShowSaltedDish event ở0.67s chuyển hình hoàn chỉnh. Khay tiếp tục sở hữu RevealIngredient trước khi chế biến; Reset Animator không ép hiện lớp món.

Kiểm chứng Play Mode output/FoodAnimation/InitialVisibilityCheck.txt PASS ô mới ẩn toàn bộ lớp, chỉ hiện Edamame khi đáp, ô tái dùng vẫn ẩn ban đầu và Animator/event hoàn tất đúng hình đậu muối. Đã về Edit Mode.

### Nhịp muối đáp và hạt rơi rõ hơn
Nguyên liệu cuối (Salt với đậu muối) đáp là bắt đầu Animator trên instance đang dựng ở khay; edamame_3 bắt đầu alpha0 và fade lên trong lúc rắc. Clip Prepare dài1.15s: PlayParticles0.08, StopParticles0.6, ShowSaltedDish1.05, CompleteAnimation1.14. edamame_3 fade alpha0→1 từ0.08s đến1.05s, ShowSaltedDish chỉ ẩn lớp đậu thường khi fade xong; sau đó IsPreparing=false cho phép ghép/poof và giao order. ClearDish/OnDisable hủy coroutine và ẩn/xóa particle theo vòng đời món.

SaltGrains dùng texture trắng đặc thay FxGrain (vệt sáng chiếm phần nhỏ texture khiến hạt thực tế quá bé); kích thước0.06–0.10, rate130/s, lifetime0.35–0.43s, vận tốc xuống1.4–1.7 và lệch trái0.15–0.65 local units/s. Vị trí SaltParticles local(.15,.45,0) dưới đạo cụ, hướng rơi về bát. Giữ nguyên scale lọ muối1.

Khắc phục hạt không rơi: velocityOverLifetime X/Y/Z đều phải cùng ParticleSystemCurveMode.TwoConstants (Z=0..0). Cấu hình trước dùng Z Constant khiến Unity báo Particle Velocity curves must all be in the same mode. ResetAnimation bỏ Animator.Update khi object inactive để không phát cảnh báo khi trả pool. Kiểm chứng Play Mode output/FoodAnimation/SaltLandingCheck.txt PASS chạy ngay khi Salt đáp, edamame_3 còn ẩn trong lúc16 hạt có vận tốc tổng hướng xuống, chỉ hiện hình hoàn chỉnh sau hạt kết thúc, ClearDish/replay hủy và xóa hạt. Đã về Edit Mode.

Cập nhật fade đậu muối: SaltedEdamameAnimation.Play bật edamame_3 với alpha0; clip Prepare điều khiển SpriteRenderer.m_Color.a (0 ở0/0.08s,0.25 ở0.3s,0.65 ở0.6s,1 ở1.05/1.15s). Giữ edamame_2 phía dưới trong lúc hòa hình; event1.05s chỉ ẩn lớp thường. ResetAnimation trả alpha1 mà không bật renderer, nên khay mới/tái dùng vẫn ẩn đúng. Kiểm chứng Play Mode output/FoodAnimation/SaltFadeCheck.txt PASS alpha trung gian trong lúc hạt rơi, kết thúc alpha1, tái dùng giữ lớp ẩn và reset alpha.

Tutorial xem order hiện chuyển sang khách đầu tiên level2 (CustomerArrived index0); bỏ điều kiện khách thứ3 level1 và việc xếp sau bước chọn đĩa. Kiểm chứng Play Mode output/Tutorial/LevelTwoOrderCheck.txt PASS level1 chỉ chọn đĩa, level2 khách đầu hiện hướng dẫn và đóng khi mở nguyên liệu, chơi lại level2 hiện lại, level3 không hiện. Không đổi bàn tay/hole/font hoặc cơ chế mở nguyên liệu.


## Giữ nhu cầu cho một phần đi thẳng (11/11/2026)
TryReserveDirectServe dùng cùng TryFindCustomer/CanAccept như phục vụ từ băng, chỉ giữ một phần qua DirectServeReservation. incoming tăng từ lúc giữ nên băng và AutoComplete không gửi trùng; khi ChefManager sơ chế xong mới TryDeliver qua ServingFood.LaunchBoosterServe. IsValid kiểm tra khách vẫn ordering, progress còn thuộc lane và đích CanServe; không giao cho order đã được thay khi replay. Cancel có tính lặp an toàn và giữ trạng thái đế đã nhận trước đó nếu công thức lặp nguyên liệu đế.
Xem [Conveyor.md](Conveyor.md) cho sức chứa nhóm và đường đi đầu bếp. Play Mode kiểm tra giữ/giao/cancel, đế lặp, order cũ, thanh toán lỗi và pause; báo cáo output/FlexiblePick.


## Bản chơi thử web — 12/11/2026
Sites v24 đã đồng bộ ưu tiên phục vụ từ mọi vị trí băng, chờ đáp 0,3s theo scene, giữ nhu cầu của một phần đi thẳng, đế trước và một incoming/món. Boss/Shipper có đếm ngược và dừng khi hết giờ; hồi sinh thời gian trong demo miễn phí, reset riêng khách hết giờ và không mua Ads/Coin. Runtime Unity không bị sửa. Chi tiết kiểm chứng và giới hạn xem Assets/HotPot/Documentation/LevelStudioWeb.md.

## Đồng bộ web từ Unity local — 12/11/2026, cập nhật sau v24

Mục này thay thế mô tả v24 về timer và giới hạn resource/hình học. Web mô phỏng bằng JavaScript, không chạy C# trực tiếp.

- Board: dồn theo footprint; cổng hai cột đĩa ngang; hidden đầu màn mở ngay, sau khi dồn chờ shift/fade. Lấy trọn nhóm nối, kiểm tra băng/nắp và sức chứa chung.
- Conveyor/Chef: 10 slot theo scene, spline 512 mẫu và vị trí đầu bếp local; mặc định 14,918 giây/vòng; thông số bay và 25 action Spine local, mix 0. Giữ slot nguồn và nhu cầu một phần đi thẳng.
- Customer: một queue/hai chỗ, thứ tự đến, đế trước, một incoming/món, chờ đáp 0,3 giây; ghép/rời quầy theo cấu hình. Boss/Shipper hết giờ mở revive với case OutOfTime và tạm dừng game; hồi sinh cộng 30 giây cho khách hết giờ, không chạy Clean; từ chối xác nhận Lost (cập nhật Unity 12/11/2026; bản web cần đối chiếu riêng).
- Kẹt băng: đợi hết hoạt động, kiểm tra pick/service rồi ReviveOffered; chấp nhận dùng Clean, từ chối Lost. Pre-win tăng tốc 1,4. Chơi lại Hard lần hai dùng _Easy nếu có; thắng có nút màn tiếp.
- Có Pickup/Clean/Shuffle/AutoComplete và túi theo luật local, giữ raw/prepared; Clean không lấy phần đầu bếp đang sở hữu. Nút playtest miễn phí, không mô phỏng Coin/Ads hay dữ liệu tài khoản.
- Resource: 169 PNG, 13 âm thanh, 102 mô tả prefab món nhiều lớp, cấu hình 99 component và Spine đầu bếp. Không tải toàn bộ project/Library. Shader, ParticleSystem, animation prefab không chạy nguyên bản; web dùng hình/lớp và thời lượng đã xuất. Cucumber FoodType7 thiếu tham chiếu Raw/Prepared trong config Unity, demo giữ trạng thái thiếu hình.
- scripts/sync-unity-runtime.cjs tạo catalog.unity từ export local, kèm dấu vân tay 95 script. Đây là snapshot khi build, không tự cập nhật khi Unity thay đổi.
- Đã chạy verify.cjs, verify-ingredient-orders.cjs, verify-play.cjs, verify-runtime-sync.cjs, verify-unity-parity.cjs và TypeScript --noEmit. Bao gồm 528 trường hợp sức chứa lấy kết quả trực tiếp từ Unity; deadlock/revive/decline, bag, Pickup, Shuffle, AutoComplete, pre-win/thắng. Preview trình duyệt kiểm tra resource/giao diện; preview tĩnh không kiểm chứng API server. Chưa kiểm chứng tương đương từng frame hoặc toàn bộ level bằng người chơi.

## Hồi sinh hết giờ — 12/11/2026

Boss/Shipper hết giờ được kiểm tra sau thắng và trước kẹt băng. LevelController chuyển ReviveOffered, dừng thời gian và truyền case OutOfTime qua UIManager tới CanvasRevive. Popup hiện Out Of Time! và phần mô tả cộng 30 giây. Ads/Coin dùng chung tiến độ và giá hiện có với hồi sinh kẹt băng. Đồng ý gọi ResetExpiredServeTimers: chỉ khách đang order đã hết giờ được cộng 30 giây, thanh đầy theo lượt 30 giây mới; khách còn giờ giữ nguyên. Không chạy Clean, không đặt lại món/board/băng chuyền. Từ chối chuyển Lost. Boss dùng cùng luật vì dùng chung bộ đếm phục vụ.

Kiểm chứng 12/11/2026: Unity biên dịch và thực thi lệnh tham chiếu ReviveCase.OutOfTime thành công (isCompiling=false); git diff --check cho bốn script sửa không báo lỗi khoảng trắng. Sắp xếp helper được kiểm tra bảo toàn toàn bộ dòng. Chưa chạy Play Mode cho popup, thanh toán và cộng thời gian. Bộ kiểm tra timer qua reflection không được công cụ Unity cho phép thực thi. Console có lỗi serialize trùng restScale ở CanvasBoosterIntro, ngoài phạm vi thay đổi này.

Hiển thị revive cập nhật 12/11/2026: CanvasRevive dùng hai object OutOfSlot/OutOfTime có sẵn trong RevivePopup, bật đúng một nhóm theo case, giữ ảnh và mô tả thiết kế của từng nhóm. Tiêu đề chung cập nhật theo case; cơ chế cộng 30 giây, Clean và thanh toán giữ nguyên. Unity Edit Mode đã kiểm tra tham chiếu, đổi case và giữ nội dung; chưa thử Play Mode cho thay đổi hiển thị này.

## Hai fill dạng fillAmount — 12/11/2026

`TimerBar.prefab` dùng hai sprite có sẵn Fill1/Fill2, chung material `Art/Materials/Customer/CustomerTimerFill.mat` và shader `Art/Shaders/Customer/CustomerTimerFill.shader`. Fill1 hiện khi còn trên 5 giây, Fill2 hiện khi còn tối đa 5 giây và lớn hơn 0; hết giờ ẩn cả hai. Ngưỡng theo giây thực tế, không theo phần trăm tổng thời gian. Hồi sinh 30 giây làm đầy lại và chọn Fill1.

Shader cắt pixel phía trên mức đầy, giữ mép dưới, hình dạng và kích thước sprite như Image.fillAmount dạng Vertical/Bottom. Dùng chiều cao cục bộ nên không phụ thuộc UV atlas; mỗi renderer nhận giá trị qua MaterialPropertyBlock, không tạo material riêng. Giữ màu của sprite trong prefab; bỏ đổi màu/nháy theo phần trăm cũ. CustomerManager truyền thời gian còn lại và tổng thời gian vào Show/SetProgress.

Kiểm chứng Unity Edit Mode PASS: liên kết prefab/material, shader không báo lỗi, các mốc 6s, 5s, 5.01s trên tổng90s, 0s, nạp lại30s, Hide/Show với tổng3s; thuộc tính cắt đúng tỉ lệ, scale/size/vị trí hai sprite không đổi. Chưa kiểm tra hình ảnh trên thiết bị hoặc toàn bộ luồng Play Mode.
