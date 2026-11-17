# CustomerManager

## Class
- `Scripts/Customer/CustomerManager.cs`: hàng khách, ưu tiên phục vụ, giữ chỗ, hoàn thành, đồng hồ Boss/Shipper.
- `Scripts/Customer/Lane.cs`: tham chiếu Inspector và trạng thái runtime một làn (`[Serializable]`, giữ tên field).
- `Scripts/Customer/DishProgress.cs`: tiến độ một món — `dish` (FinalFoodView, có thể null), công thức, `missing`, `incoming`, `baseReserved`, `assembled`.
- `Scripts/Customer/CustomerTimerBar.cs`: thanh thời gian phục vụ.

Hoạt cảnh bay/ghép món thuộc `ServingFood` — xem [ServingFood.md](ServingFood.md).

## Dữ liệu
- `LevelConfig.GetCustomerQueue()` trả bản sao hàng khách chung (`customerQueue`). Cấu trúc order: [Configuration.md](Configuration.md#khách-customerorder).
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
- `TryReserveDirectServe` giữ một phần cho luồng đi thẳng từ đầu bếp (cùng `TryFindCustomer`/`CanAccept`); `IsValid` kiểm tra khách vẫn order và progress còn thuộc làn. Xem [ConveyorChef.md](ConveyorChef.md#sức-chứa-khi-lấy-đĩa-dishpickplan).

## Hoàn thành khách
- `OnIngredientDelivered` cập nhật tiến độ; món đủ và hết chuyến bay → `ServingFood.CompleteDish`. `OnDishDelivered` đánh dấu món đã giao.
- Đủ order → `HasCompletedOrder = true` (dùng cho cảnh báo thoát, [UI.md](UI.md#cảnh-báo-thoát)) → `FinishCustomer`: dừng đồng hồ, chữ khen tại `HeartPosition`, âm khách vui, chờ `leaveDelay`, ẩn thẻ, khách trượt ra → reset khay, `completedCustomers++`, phát `RemainingCustomersChanged`, lấy khách kế tiếp.
- `CheckAllServed` đặt `AllServed` một lần khi hết hàng chung và mọi làn đã xong (LevelController xử lý thắng).

## Hàng chờ
- `waitingFigures` của `waitingLane` hiển thị `customerQueue.Count − nextCustomer` người (`waitingTint`, `waitingScale`, sorting giảm dần); pool tự bổ sung.
- `StepQueueForward` dùng `lane.walker` riêng đưa chân dung từ đầu hàng vào quầy; hai làn chạy song song được.

## Spine khách Normal (`IngameConfig.customers`)
Mọi khách Normal (ở quầy, khi bước lên và trong hàng chờ) hiển thị bằng spine (hiện tạm dùng `Assets/Spines/Chars/char_01/Char01_SkeletonData`); Boss/Shipper vẫn dùng ảnh tĩnh. Spine được **đặt sẵn trong scene** HotPotDemo, không tạo bằng code:
- `Customer spine` (SkeletonAnimation + `CustomerSpine`, mặc định ẩn) là con của `Portrait` ở mỗi khách quầy và của `CustomerManager/Waiting customer template`. Local position (0, -2.58), scale 0.99 để chân khớp ảnh 526px tâm giữa; đổi nhân vật/kích thước thì sửa trực tiếp trên object này.
- `CustomerManager.waitingFigureTemplate` trỏ tới `Waiting customer template` (ẩn); khách hàng chờ và khách đang bước lên quầy là bản nhân bản của mẫu này.
- Khi hiện, `CustomerSpine` ẩn sprite cha và chép sorting của nó. Khách không phải Normal hoặc ảnh không có spine con thì dùng sprite như cũ.

| Field | Ý nghĩa |
| --- | --- |
| `maxVisibleWaiting` | Số chỗ hàng chờ nhìn thấy (mặc định 4, đúng cho HotPotDemo tỉ lệ ~9:19.5: chỗ thứ 5 ở x = -7.25 nằm hẳn ngoài màn hình). Pool hình khách chờ = giá trị này + 1, tạo sẵn và khởi tạo spine lúc `LoadCustomers`; khách sau đó vẫn tính trong hàng logic nhưng không có hình. Màn hình rộng hơn (tablet) cần tăng |
| `walkSpeed` | Tốc độ đi bộ (world/giây, mặc định 5) dùng chung khi khách bước lên quầy, rời quầy (`slideDistance`) và dồn lên trong hàng chờ; quãng đường dài thì đi lâu hơn, chuyển động đều |
| `angryDelay` / `angryDelayMax` | Khoảng chờ ngẫu nhiên trước khi giận, mặc định 10–15 giây. Mỗi khách chọn ngưỡng riêng khi tới quầy và chọn lại khi nguyên liệu đúng đáp xuống khay; reset thời gian chờ về 0 và trở lại `idle`. Ngưỡng giữ nguyên giữa các frame; đủ ngưỡng thì phát `angry`. |
| `idle/walk/happy/angryAnimation` | Tên animation: `idle` bắt đầu ngẫu nhiên trong clip khi vào hàng chờ hoặc tới quầy, gồm khách phục vụ đầu tiên; hàng chờ phủ `waitingTint`, bố trí lại giữ pha và tốc độ; trở lại idle sau nhận nguyên liệu giữ cách phát cũ, `walk` khi bước lên quầy và khi trượt ra, `happy` khi nhận đủ món; chờ clip happy hoàn tất trước khi chuyển walk và đi ra |

Đã kiểm tra trong Play Mode (HotPotDemo, level 2) với spine đặt sẵn: spine đứng đúng vị trí ở quầy và hàng chờ (hàng chờ là bản nhân bản của mẫu), chuyển `angry` sau ~10s, console không lỗi. Chưa kiểm tra bằng mắt: `happy`/`walk` khi rời quầy, chiều quay mặt khi đi bộ, vị trí chữ khen.

Kiểm chứng thay đổi 16/11/2026: Unity refresh/biên dịch thành công (isCompiling=false), diff --check không báo lỗi; kiểm tra code xác nhận reset tại callback nguyên liệu đáp xuống hợp lệ và trước kiểm tra ghép món. Chưa chạy Play Mode cho angry → idle sau giao nguyên liệu. Sắp xếp helper được kiểm tra bảo toàn nội dung từng dòng.

Kiểm chứng happy 16/11/2026: Unity biên dịch thành công; kiểm tra animation-state trong Edit Mode trên Char01 thật (happy dài1.6667s) PASS chờ ở giữa clip, thoát chờ sau Complete rồi phát walk, và Complete xảy ra trước khi yield không gây chờ thêm. Chưa kiểm tra toàn bộ rời quầy trong Play Mode. diff --check của hai script không có lỗi.

### Chi tiết triển khai (gộp từ ghi chú bàn giao 16–17/11/2026)
- Mọi khách Normal dùng spine `Assets/Spines/Chars/char_01/Char01_SkeletonData` (tạm thời, chưa chuyển vào `Assets/HotPot` theo yêu cầu người dùng).
- Spine **đặt sẵn trong scene** `Assets/HotPot/Scenes/HotPotDemo.unity` (người dùng không muốn tạo lúc chạy):
  - `Customer spine` (SkeletonAnimation + `CustomerSpine`, ẩn) dưới `Portrait` của 2 khách quầy và dưới `CustomerManager/Waiting customer template` (ẩn).
  - `CustomerManager.waitingFigureTemplate` → mẫu đó; khách hàng chờ và walker (khách bước lên quầy) là bản `Instantiate` của mẫu.
- Animation: hàng chờ `idle` (phủ `waitingTint`), bước lên `walk`, tại quầy `idle`, chờ đủ ngưỡng ngẫu nhiên `IngameConfig.customers.angryDelay`–`angryDelayMax` (10–15s) → `angry`; nhận một nguyên liệu đúng đáp xuống khay → reset `waitTime = 0`, về `idle` và đếm chờ lại từ đầu; đủ món → phát hết `happy` một lần rồi mới `walk` và trượt ra. Boss/Shipper vẫn ảnh tĩnh.
- Code: `Scripts/Customer/CustomerSpine.cs` (mới), `CustomerManager.cs` (`ShowSpine`, `TickPatience`, `CloneFigure`), `Lane.cs` (`waitTime`), `Config/IngameConfig.cs` (angryDelay + tên animation).

#### Nhận nguyên liệu (16/11/2026)
OnIngredientDelivered chỉ xử lý khi làn đang order và chỉ số món hợp lệ. Sau Arrive, reset thời gian chờ và đưa spine đang hiện về idle; không reset khi mới giữ chỗ hoặc nguyên liệu đang bay. Mọi đường phục vụ dùng callback này áp dụng cùng hành vi. Happy/rời quầy sau khi đủ món giữ nguyên.

#### Chờ happy trước khi đi ra (16/11/2026)
CustomerSpine.Play trả Spine.TrackEntry. FinishCustomer đăng ký WaitForSpineAnimationComplete ngay khi bắt đầu happy, giữ leaveDelay và hiệu ứng ẩn thẻ, sau đó chờ Complete trước khi phát walk/SlideOut. Nếu happy đã hoàn tất trong lúc chờ/ẩn thẻ thì không chờ lại nguyên clip. Khách ảnh tĩnh giữ luồng cũ; reload vẫn dừng coroutine qua LoadCustomers.

Tốc độ di chuyển (17/11/2026, speed-based): `IngameConfig.customers.walkSpeed` (đơn vị world/giây, mặc định 5) dùng chung cho bước lên quầy, rời quầy và dồn lên trong hàng chờ; thời gian = quãng đường / tốc độ, chuyển động đều (bỏ easing). Đã bỏ `stepForwardDuration` và `slideDuration`. `slideDistance` (hiện 9) vẫn là quãng đường ra. Hàng chờ giờ có hoạt cảnh: khi người đầu hàng rời đi, mọi khách chờ bắt đầu lùi một chỗ rồi đi lên (spine `walk`, tới nơi chuyển `idle` pha ngẫu nhiên) qua coroutine `WalkQueue`. leaveDelay (hiện 0.6s) chỉ là độ trễ. Kiểm chứng: Unity biên dịch thành công; chưa xem hoạt cảnh trong Play Mode.

Hiệu năng (17/11/2026, thống nhất với Codex sau 2 vòng review):
1. Hàng chờ chỉ có `maxVisibleWaiting + 1` hình (mặc định 5), tạo sẵn ở `LoadCustomers` qua `EnsureFigures` + `CustomerSpine.PrepareForQueue()` (gọi `SkeletonAnimation.Initialize(false)` ngay để không khởi tạo giữa lúc chơi).
2. `PrepareForQueue` đặt `Mix = 0` cho mọi PhysicsConstraint của hình hàng chờ (Char01 không có timeline physics nên không bị ghi đè). Khách quầy và walker giữ physics.
3. SkeletonAnimation của `Waiting customer template` đặt `updateWhenInvisible = Nothing` (walker nhân bản từ mẫu nên cũng vậy).
Kiểm chứng Play Mode level 2 (5 khách): pool 5 hình đã khởi tạo (`valid`), physics hàng chờ Mix 0, quầy Mix 1, hiển thị đúng, console sạch. Kiểm tra replay/đổi level (Play Mode 17/11/2026): để khách quầy chuyển `angry` rồi `ReplayLevel()` → khách quầy về `idle`, hàng chờ đúng vị trí, pool vẫn 5 hình, physics hàng chờ vẫn Mix 0, quầy Mix 1. Sau đó `LoadLevel(LevelConfig_23)` (18 khách, cùng luồng với NextLevel) → hiển thị 18 khách, chỉ 5 hình chờ active (x = -4.05 … -7.25; hình thứ 5 nằm ngoài khung game), console sạch. Chưa gọi đúng `NextLevel()` (cần trạng thái Won) và chưa profile trên thiết bị. Việc còn lại: profile on-device (Profiler + Frame Debugger) trước khi throttle/chỉnh mesh/texture.

Thứ tự vẽ (17/11/2026): hai khách quầy cùng sortingOrder 0 nên khách đang bước lên làn 2 từng bị chèn lẫn với khách đứng ở làn 1. Nay khách ở quầy = order gốc, walker = gốc − 1 (đi phía sau khách ở quầy), hàng chờ = gốc − 2 − k (sau walker). Biên dịch thành công, chưa xem trong Play Mode.

#### Ngưỡng giận ngẫu nhiên (16/11/2026)
Lane.angryAfter giữ ngưỡng riêng được Random.Range chọn trong khoảng cấu hình10–15s khi vào quầy và mỗi lần nguyên liệu đúng đáp xuống khay. TickPatience chỉ so với ngưỡng đã chọn, không random mỗi frame. angryDelay giữ vai trò mốc tối thiểu10s, angryDelayMax là mốc tối đa15s. Cấu hình cần đặt tối đa không nhỏ hơn tối thiểu.

#### Lệch nhịp idle hàng chờ và tại quầy (16/11/2026)
LayoutWaitingQueue gọi CustomerSpine.Play(idle, true, randomStart: true). Khi bắt đầu vòng idle mới, TrackTime được chọn ngẫu nhiên từ0 tới độ dài clip; các lần layout tiếp theo giữ entry và thời điểm đang phát, không random lại liên tục. BringNextCustomer cũng yêu cầu randomStart cho idle khi khách vào quầy, gồm hai khách được phục vụ đầu tiên. Idle đang chạy được giữ nguyên pha. Trở lại idle sau nhận nguyên liệu vẫn dùng cách phát cũ; walk/happy không đổi và không thay đổi tốc độ animation.

Kiểm chứng: Unity refresh/biên dịch thành công, diff --check hai script không có lỗi; xác nhận LayoutWaitingQueue và BringNextCustomer bật randomStart. Chưa kiểm tra hình ảnh hàng chờ trong Play Mode. Thay đổi ngưỡng angry10–15s trước đó cũng đã refresh/biên dịch thành công, chưa kiểm tra Play Mode.

#### Kiểm chứng ban đầu
Play Mode HotPotDemo level 2: spine đúng vị trí ở quầy/hàng chờ, `angry` sau ~10s, console sạch.

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
