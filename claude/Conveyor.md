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
- Hai LineRenderer `Preparation Track` (nền tối, order 81) và `Preparation Progress` (cung xanh, order 82) được tạo sẵn trong `Conveyor_Slot.prefab` (inactive), gán vào field `preparationTrack`/`preparationArc` của ConveyorSlot. Code chỉ bật/tắt và vẽ lại điểm của cung; màu, độ dày (0.045), material Sprites-Default và sorting chỉnh trực tiếp trên prefab. Không còn tạo object/material lúc chạy; nếu chưa gán hai field thì không hiện vòng.
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

## Cấu hình nguyên liệu chung — 03/11/2026

Food không lưu IngameConfig/FoodIconConfig. Chef, board và slot đọc IngameConfig.Instance.FoodIcons từ asset Resources đã cache; TryFillFood(type) không cần truyền config. Gán Food Icons một lần trong Config/Resources/IngameConfig.asset. Đã kiểm tra icon Raw/Prepared và fill slot trên object tạm trong Unity; xem Documentation/IngameConfig.md để biết phạm vi refactor.

## Chef animation reference — 04/11/2026

- Cấu hình trực tiếp trên component ChefAnims: Idle Animation (Idle1), Idle Loop Animation (Idle2), Idle Loop Delay và Ingredient Animations (FoodType + AnimationReferenceAsset). Không đặt animation/thời gian trong FoodIconConfig.
- ChefManager dùng ChefAnims.GetPreparationDuration(type), lấy Animation.Duration của reference; vòng tiến độ dùng cùng thời lượng. Thời gian này chỉ gồm sơ chế, chưa gồm bay vào, hiệu ứng hoàn tất và bay về.
- Animation sơ chế phát một lượt. Khi về nghỉ (lúc vào game và sau mỗi lượt sơ chế): lặp Idle1, sau Idle Loop Delay (mặc định 1,5s) chuyển sang lặp Idle2 (AnimationState.AddAnimation có delay); gọi về nghỉ khi đang Idle1/Idle2 thì giữ nguyên, không phát lại từ đầu; chuyển animation không mix. Tham chiếu phải thuộc cùng SkeletonDataAsset với SkeletonAnimation của đầu bếp.
- Reference assets nằm trong Assets/HotPot/Animations/Chef. Scene dùng Assets/Spines/export/Chef_SkeletonData.asset, có Idle1, Idle2 (đều 1,6667s; reference Idle1/Idle2 nằm trong Assets/Spines/export/ReferenceAssets, do nút Create Animation Reference Assets tạo; animation Idle cũ không còn nên Chef_Idle.asset không dùng nữa), Slice_Salmon, Slice_Tuna, Cut_Ginger, Cut_Seaweed (đều 1,7667s), Grating_Wasabi và RiceBall (đều 1,8333s), PourTea và Boil_Bean (đều 2s). Mapping: Salmon → Slice_Salmon; Wasabi → Grating_Wasabi; SushiRice → RiceBall; PickledGinger → Cut_Ginger (Chef_Cut_Ginger.asset); Seaweed → Cut_Seaweed (Chef_Cut_Seaweed.asset); TeaLeaves → PourTea (Chef_PourTea.asset); Edamame → Boil_Bean (Chef_Boil_Bean.asset); Tuna và các loại chưa có action riêng tạm gán Slice_Tuna rõ ràng trong danh sách. Khi có animation mới, tạo reference và thay ở dòng nguyên liệu tương ứng.
- 04/11/2026: thêm reference Grating_Wasabi và RiceBall, gán vào HotPotDemo bằng cách sửa trực tiếp file scene. Chưa mở Unity để kiểm tra reference và chưa chạy Play Mode.
- 05/11/2026: thêm reference Cut_Ginger và Cut_Seaweed, gán vào HotPotDemo bằng cách sửa trực tiếp file scene. Chưa mở Unity để kiểm tra và chưa chạy Play Mode.
- 06/11/2026: thêm reference PourTea, gán cho TeaLeaves trong HotPotDemo bằng cách sửa trực tiếp file scene. Chưa mở Unity để kiểm tra và chưa chạy Play Mode.
- 09/11/2026: thêm chuỗi nghỉ Idle1 → Idle2 trong ChefAnims; gán Idle Loop Animation = ReferenceAssets/Idle2 và Idle Loop Delay = 1,5 trong HotPotDemo bằng cách sửa trực tiếp file scene. Chưa mở Unity để biên dịch và chưa chạy Play Mode.
- 09/11/2026: thêm reference Boil_Bean, gán cho Edamame trong HotPotDemo bằng cách sửa trực tiếp file scene. Chưa mở Unity để kiểm tra và chưa chạy Play Mode.
- Bỏ preparation.preparationDuration dùng chung khỏi IngameConfig. Không thay đổi bản mô phỏng web trong lần chỉnh Unity này.
- Đã biên dịch và kiểm tra reference cùng skeleton/thời lượng cho 25 nguyên liệu trong Unity Edit Mode; đã lưu mapping vào HotPotDemo. Chưa kiểm tra trọn lượt sơ chế trong Play Mode.

## Bỏ khói và vụn dự phòng của ChefManager — 04/11/2026

- Xóa khỏi ChefManager các field `Chop Smoke Prefab`, `Meat Debris Prefab`, `Vegetable Debris Prefab` (header "Vụn khi cắt") cùng toàn bộ logic khói lúc cắt (`SetSmoke`, instance `chopSmoke`).
- Vụn khi chặt giờ chỉ lấy prefab riêng của từng nguyên liệu qua `FoodIconConfig.GetChopDebris`; nguyên liệu chưa gán prefab vụn thì không bắn vụn (không còn dự phòng thịt/rau theo nhóm).
- Các prefab `Prefabs/Effects/ChefChopSmoke`, `ChefMeatDebris`, `ChefVegetableDebris` vẫn còn trong project, không bị xóa.
- Giá trị cũ của các field này trong scene là dữ liệu thừa, Unity tự bỏ khi lưu lại scene. Chưa biên dịch trong Unity sau lần chỉnh này.

## Bỏ đoạn ra món (Station Exit) — 09/11/2026

- Xóa field `stationExitStart`/`stationExitEnd`, hàm `IsInExitZone` và gizmo xanh lá khỏi `ConveyorChefMovement`. `CustomerManager.Update` giờ thử phục vụ từ mọi slot: món đã sơ chế, đã đáp lại đĩa nguồn thì bay thẳng tới order ngay, ở bất kỳ vị trí nào trên băng.
- Giá trị cũ của hai field trong scene là dữ liệu thừa, Unity tự bỏ khi lưu lại scene. Chưa biên dịch và chưa chạy Play Mode trong Unity.
- Món sơ chế xong vẫn bay về đĩa nguồn; sau khi đáp phải nằm trên đĩa `Serve Delay After Landing` giây (field trên CustomerManager, mặc định 0,2s) rồi mới được bay lên order. Xem `claude/CustomerManager.md`.

## Nhận món trực tiếp từ board — 11/11/2026

- Sau khi giữ đủ slot và chốt chi phí của lượt lấy, `DishPickBatch.TryFill` gọi `ChefManager.TryPreparePicked` trước khi chạy các chuyến bay thông thường.
- Khi đầu bếp rảnh, đang bật tự sơ chế và game không pause, chọn một phần thô ưu tiên cao nhất trong toàn bộ lượt lấy (gồm cả hai nửa đĩa đôi và nhóm liên kết). So sánh theo nhu cầu khách với các phần thô đã đáp trên băng: chỉ nhận trực tiếp nếu ưu tiên cao hơn; ngang ưu tiên thì món trên băng được xử lý trước. Không yêu cầu băng chuyền trống.
- Phần nhận trực tiếp bay từ đúng nửa nguyên liệu trên board tới thớt; đầu bếp bận ngay từ khi nhận việc. Các phần còn lại bay về slot như cũ; nguyên liệu không cần sơ chế vẫn bay về slot.
- Slot của phần nhận trực tiếp vẫn giữ food và mã lượt đáp suốt lượt bay vào, sơ chế và bay về; `FoodFlyAnimator` bỏ qua phần này. Món hoàn tất trả đúng slot đã giữ, dùng cơ chế pause/hủy hiện có. Thời gian bay về tính theo khoảng cách từ slot tới đầu bếp.
- Kiểm chứng 11/11/2026: Unity đã refresh và nạp API `TryPreparePicked`; `EditorApplication.isCompiling = false`, `EditorUtility.scriptCompilationFailed = false`. `git diff --check` không có lỗi khoảng trắng; các helper khi sắp xếp lại được so sánh nguyên khối để xác nhận giữ nguyên nội dung. Chưa chạy kiểm thử hành vi/hoạt cảnh trong Play Mode.


## Lấy trọn đĩa khi thiếu một slot (11/11/2026)
DishPickPlan là nơi duy nhất quyết định sức chứa, được DishPickBatch.TryFill và FoodBoardManager.HasAvailablePick dùng chung. Result tách rõ FitsConveyor, DirectToOrder, InsufficientCapacity, ChefUnavailable, NoCustomerNeed, InvalidGroup; FailureReason giải thích riêng từng nhánh. Tổng N lấy từ mọi Food duy nhất của GetPickMembers, bao gồm TotalAmount của cả hai nửa đĩa đôi; số ô footprint không nhân thêm lượng. Việc nhóm có lộ/không băng/không nắp vẫn do CanPickGroup quyết định.

| Trường hợp | Kết quả |
|---|---|
| N≤S slot trống | Mọi phần vào băng; giữ luồng TryPreparePicked cũ, vẫn có đĩa trả về |
| N=S+1 | Chỉ được nếu đầu bếp rảnh và có order nhận ngay một phần của nhóm |
| N>S+1 | Không lấy; tuyệt đối không sơ chế nhiều phần cùng lượt |
| Dư1 nhưng sai nguyên liệu/đầu bếp bận | Không lấy; phân biệt lý do rõ ràng |

DirectServeReservation giữ đúng một nhu cầu bằng DishProgress.Reserve (gồm luật đế và incoming). DishPickBatch giữ recipe và N−1 slot trước khi chốt chi phí/di chuyển; FillConveyor chỉ bỏ một phần của đúng nguồn/nửa đã chọn, mọi phần còn lại vẫn có slot riêng. Nếu điền/thanh toán lỗi, nhả các slot của lượt và Cancel reservation; chưa chạy đầu bếp/đổi board. Chốt xong ChefManager.BeginDirectPreparation dùng sprite nguồn độc lập, bay tới điểm cắt, sơ chế một phần và dùng ServingFood.LaunchBoosterServe (API phục vụ nguyên liệu đã giữ có sẵn, dùng chung với AutoComplete) tới đúng order. Không tạo slot ảo, không trả phần đi thẳng về băng, không giữ tham chiếu Food board đã trả pool. Phần trên băng vẫn chờ sơ chế như thường.

ChefManager.DirectServe.cs tách riêng nhánh mới; dùng chung Hop, PlayChops, poof, Spine và âm chặt theo FoodType với nhánh cũ. IsBusy tính cả giữ lượt đi thẳng; cancel/replay/disable nhả nhu cầu order chưa gửi, ẩn sprite/hiệu ứng và không hoàn tất arrival của slot khác. Sau khi gửi, ServingFood sở hữu phần incoming tới lúc đáp. Bộ đếm HasPendingService bao gồm nhu cầu giữ từ đầu lúc sơ chế, tránh phục vụ trùng. Booster pause và Time.timeScale dừng lượt sơ chế; AutoComplete không giữ lại phần đã book.

Kiểm chứng bước đầu: Unity compile không lỗi; ma trận192 biên sức chứa; output/FlexiblePick/Integration.txt PASS87assertions thực: 9/10+2,8/10+3, sai loại, dư2, nguồn trùng, nửa2 của đĩa đôi, nhóm4phần, giữ order đúng một lần, lỗi thanh toán không mất tài nguyên, cancel nhả recipe, đủ slot vẫn dùng nhánh cũ. Probe C# lưu ngoài Assets tại output/FlexiblePick để review/chạy lại bằng Unity RunCommand; không có test asset hoặc thay đổi config level thật.

Kiểm chứng cuối: output/FlexiblePick/BoardAndRevive.txt PASS83assertions trên board thật (9/10+toàn3,9/10+2sai loại đều vào ReviveOffered; có lựa chọn cần thì vẫn Playing; nhóm nối/đĩa đôi lấy trọn; nhóm chưa lộ bị loại; còn món phục vụ được không hồi sinh nhầm). EdgeCases.txt PASS27assertions nhóm nối chứa đĩa đôi, global/booster pause, cancel đế lặp, order cũ sau reset, S=0+N=1, callback thanh toán ném lỗi không để lại giữ chỗ. Cùng Integration.txt87 và ma trận192, tổng197kiểm tra tích hợp và192biên sức chứa. Test không xác nhận Lose, không mua booster và không lưu config/scene thật; trở về Edit Mode. Đã kiểm chứng các pure move thành viên giữ nguyên khối; chưa thử chạm trên thiết bị hoặc hiệu chỉnh hình bay trực quan.


## Đồng bộ bản chơi thử web — 12/11/2026
Bản web Sites v24 đã cập nhật chọn sơ chế theo nhu cầu ở mọi vị trí băng, nhận từ board, N=S+1 giữ một phần đi thẳng tới order, phục vụ sau thời gian đáp 0,3s theo scene, Spine hiện tại và thời lượng 25 action. Đã kiểm tra 264 tổ hợp sức chứa và các hồi quy giữ slot/order, ưu tiên và pause. Tọa độ đường băng/bay vẫn mô phỏng, không thay thế kiểm chứng Play Mode Unity. Chi tiết và giới hạn tại Assets/HotPot/Documentation/LevelStudioWeb.md.

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
