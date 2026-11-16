# FoodBoardManager — handoff

File: `Assets/HotPot/Scripts/gamebase/FoodBoardManager.cs` (+ partial `FoodBoardManager.DishLinks.cs`; namespace `FoodPop`, singleton trong scene).
Inspector: `Assets/HotPot/Editor/FoodBoardManagerEditor.cs`.

## Vai trò
- Quản lý board nguyên liệu: các cột (`ingredientColumns`, 3–5 cột), danh sách đĩa mỗi cột (`columnDishes`, phần tử 0 = đầu cột; đĩa đôi chiếm 2 ô cùng tham chiếu, `IsDishAnchor` = ô trên trái), pool đĩa (`IngredientDishPool`), bố cục hàng/cột (`rowSpacing`, `columnSpacing`), icon, băng (ice), nắp (hidden), dây liên kết đĩa (DishLinks).
- Input: `Update` → `TryGetPointerDown` → `TryPickAt` (chỉ đĩa đầu cột) → `PickGroup` (DishLinks) → `DishPickBatch.TryFill` (giữ slot + `ConveyorSlot.TryFillFood`) → đầu bếp nhận trực tiếp phần ưu tiên nếu rảnh, các phần còn lại bay lên băng → `SettleBoard` → `ReduceFrozenBlocks`.
- Nạp level: `LoadIngredientBoard(List<BoardColumn>, links)` → `ApplyColumnSpacing` → `UpdateIngredientTableVisual` → `FillBoard` → `CreateBoardDish` → `BuildDishLinks` → `SettleBoard`.
- Ảnh bàn nguyên liệu (người dùng sửa 02/11): 3 field `ingredientTable3LaneSprite` / `4LaneSprite` / `5LaneSprite`, chọn theo số cột đang dùng. `OnValidate` không còn tự gán ảnh bàn và sprite đĩa đôi (plate/outline/lid ngang–dọc) → phải gán tay trong Inspector.
- API cho booster: `GetColumnDishes`, `GetColumnTransform`, `GetRowLocalPosition`, `ApplyColumnDishes`, `TryGetDishAt`, `TryPickDishAt`, `TryTakePortion`, `RefreshSelection`, `InputLocked`, `IsAnimating`, `OffscreenHidingSuspended`, `MaxVisibleRows`.

## 2026-10-21: đĩa nằm dưới mép dưới camera
- Field `offscreenHideMargin` (mặc định 1, world): đĩa có tâm hàng thấp hơn `mép dưới camera - offscreenHideMargin` thì `SetActive(false)`; tự bật lại khi dồn lên gần màn hình. Đĩa đầu cột không bao giờ bị tắt.
- `LateUpdate` → `RefreshOffscreenDishes()` mỗi frame (chỉ SetActive khi đổi trạng thái). Dùng vị trí hàng đích (`GetRowLocalPosition(r)`) để đĩa bật ngay khi bắt đầu trượt vào.
- `GetCameraBottomY()`, `IsDishOnScreen(Food)`.
- `DamageIce`: đĩa ngoài màn hình chỉ trừ giá trị băng, không play effect. `ShatterIce` cũng chỉ play effect khi on screen.
- `TryGetDishAt` bỏ qua đĩa inactive.
- `OffscreenHidingSuspended`: true → giữ mọi đĩa active (`BoosterController.Shuffle` bật trong lúc xáo, tắt sau `ApplyNewLayout`).

## 2026-11-02: nút Update View
- Inspector `FoodBoardManagerEditor`: `DrawDefaultInspector` + nút **Update View** (không dùng menu Tools).
- `FoodBoardManager.UpdateView()` (public): dùng sau khi đổi `rowSpacing`/`columnSpacing`.
  - Luôn: `ApplyColumnSpacing(số cột đang bật)`.
  - Play Mode (`RelayoutBoardDishes`): `FinishColumnShift` mọi cột, đặt mỗi đĩa gốc về `GetRowLocalPosition(r)`, đĩa đôi gọi lại `ApplyDishShape` (offset theo spacing mới), rồi `UpdateDishLinkViews()`.
  - Edit Mode (`RelayoutSceneDishes`): với các cột đang bật, sắp các `Food` con trực tiếp theo y giảm dần và đặt vào hàng liên tiếp. Editor ghi Undo cho transform các cột + đĩa và `MarkSceneDirty`.
- Tách `ApplyDishShape(dish, shape, secondType, secondCount, column)` khỏi `CreateBoardDish` để dùng chung (dọc: kéo xuống 1 `rowSpacing`; ngang: tới vị trí cột kế).

## 2026-11-02: lỗi không pick được dish khi Play + log chẩn đoán
- Triệu chứng: vừa Play, chạm đĩa đầu cột không lấy được; băng chuyền trống 10 slot.
- Nguyên nhân (nghi chính, chưa xác nhận): giữ slot thành công nhưng `ConveyorSlot.TryFillFood` trả false vì slot chưa gán `slotFood` (component `Food` riêng của slot).
- Sửa `ConveyorSlot.TryFillFood`: `slotFood` trống → tự tìm `GetComponentInChildren<Food>(true)`; vẫn không có → `Debug.LogError("[ConveyorSlot] Slot '…' chưa gán Slot Food…")`. Nên gán lại `Slot Food` trong prefab/scene cho chuẩn.
- `DishPickBatch.LastFailure`: lý do lần `TryFill` gần nhất thất bại (dữ liệu đĩa sai / không có ChefManager / thiếu slot trống thật / slot không nhận nguyên liệu).
- Log chẩn đoán trên `FoodBoardManager`: field `logPickFailures` (mặc định bật, tắt khi xong) + `LogPickFailure(reason, context)`. Ghi lý do ở:
  - `Update`: `InputLocked = true` (booster chưa nhả khóa).
  - `TryGetPointerDown`: không có camera.
  - `TryPickAt`: chạm trượt (in toạ độ) hoặc đĩa không ở hàng đầu.
  - `PickGroup` (DishLinks): `Time.timeScale = 0`; `DescribePickBlock` (băng / nắp / hết phần / nhóm nối chưa lộ); `pickBatch.LastFailure`.
- Lưu ý: log cũ in "băng chuyền không đủ slot trống" cho mọi lỗi TryFill — đã sai, giờ dùng `LastFailure`.

## 2026-11-03: nhãn số lượng đĩa đôi
- `Food.RefreshSecondVisual`: nhãn "Second quantity" (bản sao `canvasCount`) chỉ hiện khi `SecondAmount > 1`, giống nhãn đầu (`amount > 1`); vẫn ẩn khi có nắp / đóng băng / booster tạm ẩn nhãn.
- Liên quan băng chuyền/đầu bếp hôm nay (món thô ẩn khi tới thớt, hiện lại lúc poof): xem `claude/foodpop-handoff.md` mục 03/11.

## Ý tưởng chưa làm
- SpriteMask/gradient ở đáy board (gắn `ScreenBottomAnchor`) để đĩa "chui ra" từ mép băng chuyền.
- Chỉ báo "+N" số đĩa còn dưới màn hình mỗi cột.
- Lazy spawn đĩa nếu level có cột rất dài (>15–20 đĩa/cột).


## Lấy trọn nhóm theo sức chứa (11/11/2026)
FoodBoardManager.HasAvailablePick duyệt nhóm hàng đầu không trùng thành viên và giữ nguyên CanPickGroup (lộ nhóm, không băng/nắp). Cả nó, DishPickBatch.TryFill và GetPickupGroupRejection đều dùng DishPickPlan: N≤S lấy thường; N=S+1 cần một phần có order nhận và đầu bếp rảnh; N>S+1 không lấy. N tính TotalAmount của mỗi Food duy nhất, gồm hai nửa đĩa đôi và mọi thành viên nối. Nhóm chưa lộ không được coi là lượt hợp lệ.
Chi tiết ba nhánh, giữ chỗ, hủy và kiểm chứng tại [Conveyor.md](Conveyor.md), mục Lấy trọn đĩa khi thiếu một slot. Kiểm tra board thật: nhóm nối4phần, đĩa đôi ngang và nhóm nối có đĩa đôi đều lấy trọn, không đếm trùng footprint; nhóm bị che không được lấy.

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
