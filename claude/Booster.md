# Booster

## Túi giữ món

- Component `Assets/HotPot/Scripts/Booster/FoodHoldingTray.cs` (giữ tên class để không hỏng tham chiếu) trên prefab `Assets/HotPot/Prefabs/Ingame/FoodHoldingTray.prefab`, instance tên `FoodHolding` trong scene HotPotDemo. Không xoá object con `Bag` (Back/FoodRoot/Front) trong instance: thiếu FoodRoot thì Clean báo "chưa có túi giữ món sẵn sàng". FoodHoldingStorage tự tìm FoodHoldingTray trên cùng object và object con tên `FoodRoot` nếu chưa gán; FoodHoldingTray tự tìm object con `Bag` làm thân túi.
- Cấu trúc: `Bag/Back` (sprite `Art/Sprites/Table_Assets/paper_bag.png`, scale 1,25, sorting 0), `Bag/FoodRoot` (SortingGroup 10, chứa food đã cất), `Bag/Front` (sprite `paper_bag_front.png`, sorting 20, là mép trước cắt từ paper_bag, cùng kích thước và pivot, phải đặt đúng vị trí và scale với `Back`). Food nằm giữa hai lớp nên trông như ở trong túi. Không còn ngăn kéo trượt, `WallHousing` hay vật liệu `FoodHoldingTrayClip`.
- Túi ẩn khi chưa dùng (`startHidden`) và khi đã trống: Clean gọi `ShowBag()` để túi mọc lên từ đáy kèm nhịp vượt nhẹ (`showDuration` 0,28 giây); khi phần food cuối cùng rời túi (trả về băng hoặc AutoComplete lấy), hoặc Clean kết thúc/bị hủy mà không thu được gì, `HideIfEmpty()` thu túi xuống về đáy rồi ẩn (`hideDuration` 0,2 giây). `ResetForLevel` ẩn ngay. Túi chỉ thu nhỏ scale của `Bag` về 0, object vẫn bật nên Clean vẫn nhận túi là sẵn sàng. Menu ngữ cảnh: `Test/Hiện túi`, `Test/Ẩn túi`. `MouthPosition` là điểm thế giới của miệng túi (field `mouth`, hoặc `body` + `mouthOffset`, mặc định (0, 0.62, 0)). `Bounce()` tạo hiệu ứng nảy co giãn nhẹ quanh đáy túi (`bottomY`, `bounceDuration` 0,28 giây, `bounceStrength` 0,1); gọi liên tiếp không khởi động lại cú nảy vừa bắt đầu. Menu ngữ cảnh: `Test/Nảy túi`.
- Bố cục food trong túi (field trên FoodHoldingStorage): `storedScale` 0,6 (so với băng chuyền), `perRow` 3, `columnSpacing` 0,36, `firstRowY` 0,2, `rowSpacing` 0,12, `maxRows` 3, `tiltAngle` 10°, `settleSpeed` 14, `riseDuration` 0,15 giây. Hàng được điền từ dưới lên: món cũ nằm thấp (bị mép trước che), món mới ló ra ở miệng túi. Hàng lẻ lệch nửa cột; món thừa xếp chồng lên hàng trên cùng. Khi bố cục đổi, food trượt mượt về vị trí mới.
- CleanBrush thu món vào túi; túi trả món về băng theo nhu cầu order. Không trả nguyên liệu về board.

Tài liệu tham chiếu hệ thống booster trong FoodPop, dựa trên `Assets/HotPot/Scripts/Booster/BoosterController.cs`.

Đối chiếu code gần nhất: 05/11/2026 (mục khóa theo level, BoosterConfig và `shortDes` chưa chạy thử trong Unity). Phần BuyBooster đã được kiểm tra Play Mode bằng ví tạm trong bộ nhớ vào ngày này. Các kết quả Play Mode về hiệu ứng/gameplay bên dưới là kiểm tra trước khi bổ sung số lượt và collider; chưa chạy lại toàn bộ logic kích hoạt, tiêu hao và collider trong lượt làm popup mua.

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
- Khi game đang tạm dừng (`Time.timeScale <= 0`), `TryActivate` trả về `false` ngay, không chạy booster hay yêu cầu mua.
- Sau kiểm tra tạm dừng, `TryActivate` kiểm tra khóa level (`IsLocked`) trước mọi kiểm tra khác: booster đang khóa chỉ lắc icon khóa, không mở popup mua, không hiện short message và không trừ lượt (05/11/2026).
- Khi không đủ điều kiện, nút chạy hiệu ứng từ chối và ghi log lý do.
- Khi hợp lệ, booster chạy ngay, không chờ hiệu ứng bấm. `UIButtonClick` chỉ tạo phản hồi hình ảnh độc lập; không dùng `ButtonPressFx` cho booster. Icon vẫn làm mờ khi bận và lắc khi bị từ chối.
- `RunLocked` khóa thao tác board và đánh dấu controller đang bận trong khi chạy hiệu ứng; khối `finally` khôi phục khóa input đã lưu và hiện lại các nút. Pickup truyền trạng thái trước chế độ chọn để không giữ nhầm khóa tạm.
- `SetBusy` cập nhật trạng thái khả dụng của toàn bộ nút booster.

## Số lượt booster (02/11/2026)

- Mỗi `Booster` có `boosterType` (`GamePackages.ResourceType`); số lượt đọc từ `ProfileManager.instance.PlayerDataManager.GetResourceValue` (0 khi hồ sơ chưa nạp).
- Hiển thị: còn lượt → bật `objNumber`, ghi số vào `txtNumber`; hết lượt → bật `objPlus`. `OnEnable` đăng ký `RegisterResourceChange` và cập nhật ngay; `OnDisable` gỡ đăng ký. Gọi `RefreshAmount()` nếu cần cập nhật tay.
- Chạm booster đã hết lượt (khi không có booster khác đang chạy): gọi `Booster.RequestBuy()` → `UIManager.ShowBuyBooster(BoosterType)` → popup `CanvasBuyBooster` được tạo từ prefab trong `Assets/HotPot/Config/UI/UIRef.asset` khi gọi lần đầu và dùng lại sau đó. Không lắc icon, không chạy booster. Prefab: `Assets/HotPot/Prefabs/UI/Booster/BuyBoosterPopup.prefab`.
- Popup bán **+3 lượt** với giá lấy từ `BoosterConfig` (mặc định 900 gold; `ResourceType.Coin`, `CanvasBuyBooster.AmountPerPurchase = 3`). Dùng API nghiệp vụ `PlayerDataManager.TryBuyBooster(resource, 3, price, "buy_booster")` với `price` là giá đọc khi mở popup: kiểm tra đủ tiền và sức chứa, cập nhật Coin/booster cùng thống kê trong một lần lưu; chỉ đóng popup khi trả true. Không thay bằng gọi trực tiếp API tài nguyên chung trong popup. Ánh xạ PickupHand → BoosterPickup, CleanBrush → BoosterClean, Shuffle → BoosterShuffle, AutoComplete → BoosterAutoComplete. Mua xong đóng popup, không tự dùng booster.
- Số vàng hiển thị trên popup (`goldBalanceLabel`) cũng đọc cùng `ResourceType.Coin`, đăng ký sự kiện đổi Coin trong lúc mở và gỡ khỏi ví đã đăng ký khi đóng. Không tồn tại số dư Gold riêng. Giá mua (`price`, từ BoosterConfig) độc lập với ô số dư; OnEnable đăng ký Coin và đọc số dư ban đầu, OnDisable gỡ đăng ký; không kiểm tra hoặc nối lại mỗi frame.
- Thiếu vàng hoặc ví/hồ sơ/dữ liệu RecordManager chưa sẵn sàng: giữ popup mở, không thay đổi tài nguyên và hiện thông báo ngắn lấy từ `shortDes`: thiếu vàng dùng `NotEnoughCoin` (có {0} là giá), lỗi giao dịch dùng `PurchaseFailed` (05/11/2026). Kiểm tra tham chiếu thống kê trước khi trừ Coin. Nút Để sau đóng mà không mua; popup dừng gameplay đang chạy và trả lại tốc độ cũ khi đóng, giữ nguyên trạng thái nếu trước đó đã tạm dừng.
- Kiểm tra cũ 02/11/2026 trước khi đổi gói sang ba lượt: Play Mode bằng ví tạm xác nhận gói một lượt/900 gold, chặn bấm lặp, từ chối 899, chấp nhận đúng 900, đóng/hủy và thiếu hồ sơ an toàn. Kết quả này thuộc phiên bản cũ, không thay cho kiểm tra gói ba lượt và API có thống kê. Xem cấu trúc UI và hướng dẫn chỉnh prefab tại `claude/UI.md`.
- Đã kiểm tra lại gói ba lượt trong Play Mode (02/11/2026) với ví và RecordTrackingManager tạm: cả bốn loại trừ 900 Coin (giá lúc đó), cộng đúng 3; thống kê chi Coin tổng/trong màn tăng 900 và booster nhận tăng 3 cho mỗi giao dịch. Có kiểm tra bấm lặp, 899/900 Coin, hủy và thiếu RecordManager; không sửa ví hoặc bản ghi thật.
- Kiểm tra hết lượt diễn ra sau kiểm tra khóa level và trước `CanUse`: nếu hết lượt thì yêu cầu mua, kể cả board hiện không có mục tiêu hợp lệ. Khi controller đang bận, dùng nhánh từ chối đang có booster chạy thay vì yêu cầu mua. Gán đúng `boosterType` trong Inspector để đọc/trừ đúng tài nguyên; đây là `ResourceType`, khác với `Booster.Type` dùng định danh hành động và yêu cầu mua.
- Trừ lượt (`Booster.Consume()` → `OnRemoveBooster(type, 1, "ingame")`): Shuffle, AutoComplete, Clean trừ ngay khi bắt đầu chạy; PickupHand truyền callback Consume vào TryPickDishAt và chốt lượt sau khi batch giữ/điền đủ slot, trước khi bay/gỡ đĩa; nếu thanh toán false thì hoàn tác slot, không lấy đĩa. Hủy hoặc lấy thất bại trước đó không mất lượt. Revive dùng Clean qua đường riêng nên không trừ lượt.

## Khóa booster theo level và BoosterConfig (05/11/2026)

- `BoosterConfig` (`Assets/HotPot/Scripts/Config/BoosterConfig.cs`, ScriptableObject, menu `Hot Pot/Booster Config`) lưu từng booster: `type` (BoosterType), `price` (Coin mỗi lần mua, mặc định 900) và `unlockLevel` (mặc định 1). Asset `Assets/HotPot/Config/BoosterConfig.asset` được gán vào field `boosterConfig` của `IngameConfig`, đọc qua `IngameConfig.Instance.Boosters`.
- `unlockGift` (mặc định 2, 05/11/2026): số lượt tặng một lần khi booster được giới thiệu ở level mở khóa; đọc qua `GetUnlockGift(type)`. `Booster.Grant(amount, placement)` cộng lượt qua `OnAddBooster`.
- API: `GetName(type)`, `GetInfo(type)`, `GetPrice(type)`, `GetUnlockLevel(type)`, `IsLocked(type, level)`. Thiếu entry cho loại nào thì ghi cảnh báo và dùng giá 900, unlock level 1; `GetName` trả tên loại booster khi `boosterName` để trống.
- Giá trị hiện tại: cả bốn loại giá 900; PickupHand, CleanBrush, AutoComplete mở ở level 1; Shuffle mở ở level 5.
- Giới thiệu khi mở khóa (05/11/2026): mỗi entry có thêm `boosterName` (tên hiển thị) và `boosterInfo` (mô tả tác dụng, `TextArea`) để màn intro booster dùng khi booster được mở khóa. Nội dung viết bằng tiếng Anh: PickupHand "Helping Hand" – "Pick any plate you want from the table."; CleanBrush "Belt Sweep" – "Collect the plates on the conveyor into the holding bag."; Shuffle "Shuffle" – "Swap the positions of the plates on the table."; AutoComplete "Auto Complete" – "Instantly finish one plate a customer is waiting for." Màn giới thiệu booster mở khóa (mục "Giới thiệu booster mở khóa") đọc hai trường này; popup mua vẫn dùng `presentations` riêng của nó.
- `Booster` có nhóm "Khóa": `objLock` (icon khóa và chữ, bật khi đang khóa), `objUnlock` (icon booster, dấu cộng và số lượt, bật khi đã mở), `txtUnlockLevel` (ghi "Lv N"). `RefreshLock(level)` đặt `IsLocked = level < unlockLevel`, bật/tắt hai object trên và ghi "Lv " + unlockLevel.
- `BoosterController` có field `levelController` (tự tìm bằng `FindFirstObjectByType` nếu trống). Controller đăng ký `LevelController.LevelLoaded` trong `OnEnable`, gỡ trong `OnDisable`, gọi `RefreshLock(config.levelNumber)` cho mọi booster và refresh một lần ngay trong `OnEnable` theo `levelController.CurrentLevel`. Mở khóa so với `LevelConfig.levelNumber` của màn đang chơi.
- Khi chạm booster bị khóa, `PlayDenied()` lắc `objLock` thay vì icon (xem mục Luồng chung).
- Scene HotPotDemo: Pickup, Clean và AutoComplete có thêm object `Lock` (bản sao của Lock ở Shuffle, tắt sẵn, runtime bật/tắt) và object `Unlock` mới chứa icon, `btn_plus` và `DotNumber`. Cả bốn component đã gán `objLock`, `objUnlock`, `txtUnlockLevel`.
- Thông báo ngắn (`shortDes`, 05/11/2026): `BoosterConfig` có list `shortDes`, mỗi item gồm `type` (enum `BoosterConfig.ShortDesType`) và `info` (`TextArea`). API `GetShortDes(type)` trả về `info`; thiếu item hoặc `info` trống thì ghi cảnh báo và trả về tên loại. Tất cả thông báo ngắn hiện viết bằng tiếng Anh. `BoosterController` dùng helper `ShortDes(type)`. Muốn đổi nội dung chỉ cần sửa list trong `Assets/HotPot/Config/BoosterConfig.asset`, không cần sửa code.
- Quy ước từ ngữ tiếng Anh cho nội dung hiển thị (05/11/2026): mọi nội dung booster hiển thị cho người chơi đều gọi là **plate** (đĩa trên bàn, phần trên băng chuyền và món khách gọi), không dùng "dish" hay "ingredient". Tên loại thông báo cũng theo plate: `PlateEmpty`, `PlateFrozen`, `LinkedPlateFrozen`, `LinkedPlatesNoSpace`, `NoPlateToPick` (giữ nguyên số thứ tự nên dữ liệu asset không đổi).
- Nội dung hiện tại và nơi dùng:

| ShortDesType | Nội dung | Nơi dùng |
| --- | --- | --- |
| BoosterRunning | `Another booster is in use.` | Booster khác đang chạy |
| BoardBusy | `Please wait for the board to settle.` | Board đang animation (`CanUse`) |
| BeltEmpty | `The conveyor is empty.` | Clean khi băng trống |
| PickupPrompt | `Choose a plate to pick up` | `PickupHandBooster.ShowPrompt`, chế độ chọn đĩa |
| NoPlateToPick | `No plates to pick up.` | Pickup không còn nguyên liệu |
| PlateEmpty | `This plate is empty.` | `GetPickupGroupRejection`, ô không còn đĩa |
| PlateFrozen | `This plate is frozen.` | `GetPickupGroupRejection`, đĩa đơn đóng băng |
| LinkedPlateFrozen | `A linked plate is still frozen.` | `GetPickupGroupRejection`, thành viên nhóm link đóng băng |
| BeltFull | `Not enough free space on the conveyor.` | `GetPickupGroupRejection`, đĩa đơn không đủ chỗ |
| LinkedPlatesNoSpace | `Not enough space for all linked plates.` | `GetPickupGroupRejection`, nhóm link không đủ chỗ |
| NotEnoughCoin | `Not enough gold. You need {0} gold to buy this booster.` | Popup mua, `{0}` là giá (format bằng `string.Format`) |
| PurchaseFailed | `Purchase failed. Please try again.` | Popup mua, giao dịch thất bại |
| TryPrompt | `Tap to try!` | Màn giới thiệu booster mở khóa, dòng nhắc phía trên nút trong lượt dùng thử (05/11/2026) |

- Log lý do vẫn bằng tiếng Việt và không đi qua `shortDes` (Shuffle, AutoComplete, lý do kho Clean, hai hàng Pickup). Pickup khi chỉ còn một hàng không hiện thông báo, chỉ lắc icon.
- `PickupHandBooster.ShowPrompt` đọc `PickupPrompt`; field serialize `promptMessage` đã bỏ, giá trị cũ còn trong scene bị bỏ qua.
- Chưa chạy thử trong Unity: khóa/mở theo level, lắc khi bị khóa, log, giá và thông báo ngắn lấy từ BoosterConfig.

## Giới thiệu booster mở khóa (05/11/2026)

- Component `BoosterUnlockIntro` (`Assets/HotPot/Scripts/Booster/BoosterUnlockIntro.cs`) gắn trên object `BoosterController` trong scene HotPotDemo. Nếu chưa gán, tự tìm `BoosterController` trên cùng object và `LevelController` trong scene.
- Kích hoạt khi `LevelController.LevelLoaded` và `levelNumber` của màn từ 2 trở lên. Booster mở khóa ở level 1 có sẵn từ đầu nên không có màn giới thiệu. Chọn các booster có `BoosterConfig.GetUnlockLevel(type)` bằng đúng level của màn và chưa được xem.
- Đánh dấu đã xem: PlayerPrefs `FoodPop.BoosterIntro.<BoosterType>` = 1. Menu ngữ cảnh `Test/Xóa cờ đã xem giới thiệu booster` xóa mọi cờ để chạy lại khi thử nghiệm.
- Quà mở khóa (05/11/2026): ngay sau khi ghi cờ đã xem (sau "Got it" và icon bay xuống nút, trước lượt dùng thử), `BoosterUnlockIntro` gọi `Booster.Grant(BoosterConfig.GetUnlockGift(type), "unlock_intro")`, tức `PlayerDataManager.OnAddBooster`. Mặc định `unlockGift` = 2 cho mọi booster trong `BoosterConfig.asset`. Chỉ cộng một lần vì đi cùng cờ đã xem; booster mở từ level 1 không có màn giới thiệu nên không được tặng. Lượt dùng thử sau đó trừ một lượt như chạm thường.
- Nhịp hiện popup (05/11/2026): sau khi tạm dừng màn chơi và hiện dáng khóa, `BoosterUnlockIntro` chờ `popupDelay` (0,8 giây, thời gian thực) rồi mới mở popup để người chơi kịp nhìn bàn chơi. Field nằm dưới header "Nhịp hiện popup và icon bay".
- Luồng cho từng booster, lần lượt nếu có nhiều booster cùng level:
  1. Chờ đến khi không booster nào đang chạy (`BoosterController.IsRunning`), rồi tạm dừng màn chơi và nhớ tốc độ cũ.
  2. Booster hiện dáng khóa (`ShowLockVisual(true)`).
  3. Mở popup `CanvasBoosterIntro` qua `UIManager.ShowBoosterIntro`: icon, tên và mô tả lấy từ BoosterConfig (`GetName`, `GetInfo`), nút "Got it". Popup không có dòng level mở khóa.
  4. Khi bấm "Got it": popup ẩn icon rồi đóng; một bản sao icon bay theo vòng cung từ vị trí trên popup tới icon của nút, ổ khóa thu nhỏ dần trên đường bay. Khi icon đáp, nút chuyển sang dáng đã mở và nảy lên (`popDuration`). Sau đó booster được đánh dấu đã xem.
  5. Lượt dùng thử: `BoosterUnlockIntro` tạo prefab `trialOverlayPrefab` (`Assets/HotPot/Prefabs/UI/Booster/BoosterTrialOverlay.prefab`) rồi gọi `Show(<bao SpriteRenderer nền của booster>, Camera.main, <dòng nhắc>)`, trong đó dòng nhắc là `shortDes` mục `TryPrompt` ("Tap to try!"). Lớp phủ là canvas riêng trên mọi UI, tối toàn màn hình trừ một lỗ tròn ở nút, có bàn tay Spine và dòng nhắc phía trên lỗ; chi tiết xem `claude/UI.md`, mục "Lớp phủ dùng thử booster (05/11/2026)". Nút phập phồng nhẹ (`pulseScale`) trong lúc chờ. Lớp phủ bị hủy khi người chơi chạm đúng nút hoặc khi `StopIntro()` chạy.
  6. Người chơi chạm đúng nút đó: lớp phủ bị xóa, tạm dừng được nhả và `BoosterController.TryActivate(booster)` chạy booster như chạm thường, **trừ một lượt** (thường lấy từ 2 lượt vừa tặng). Các chạm khác bị bỏ qua, kể cả khi game đang tạm dừng. (05/11/2026: đã bỏ `TryActivateFree` và cờ `pickupFree`; Pickup trừ lượt qua `settings.Consume` như bình thường.)
- Hủy giữa chừng (`OnDisable`, nạp lại màn): `StopIntro()` kết thúc lượt dùng thử, xóa lớp phủ và icon bay, đóng popup, trả nút về dáng theo trạng thái khóa hiện tại và nhả tạm dừng nếu chính component này đã dừng màn chơi. Nếu hủy trước khi icon bay xong thì booster chưa được đánh dấu đã xem và sẽ chạy lại ở lần nạp sau.
- API mới trong `BoosterController`:
  - `BeginTrial(Booster, Action tapped)` và `EndTrial()`: khi lượt dùng thử đang bật, `Update` chỉ chờ chạm vào booster đó (kể cả khi game tạm dừng); chạm xong gọi `tapped` một lần và thoát lượt. `EndTrial()` thoát mà không gọi callback.
- Thành phần mới trong `Booster`:
  - `IconRenderer`: SpriteRenderer của icon.
  - `LockRoot`, `UnlockRoot`: transform của `objLock` và `objUnlock` (null nếu chưa gán).
  - `ShowLockVisual(bool locked)`: bật dáng khóa hoặc dáng đã mở, không đổi `IsLocked`.
- Tham số Inspector của `BoosterUnlockIntro` (giá trị mặc định trong code):
  - Bay và nảy: `flyDuration` (0,65 giây), `flyArc` (1,5), `popDuration` (0,42 giây).
  - Hướng dẫn dùng thử: `trialOverlayPrefab` (prefab `BoosterTrialOverlay`), `pulseScale` (0,06).
  - Vẽ: `sortingOrder` (940), chỉ áp cho icon bay từ popup xuống nút; lớp phủ dùng thử có canvas riêng nên không phụ thuộc số này.
  - Tham chiếu: `controller`, `levelController` (để trống thì tự tìm).
- Chưa chạy thử trong Unity: luồng bay, lớp phủ prefab (lỗ sáng, bàn tay Spine), lượt dùng thử, quà mở khóa và nhịp hiện popup.

## Gán tham chiếu và nhận thao tác (02/11/2026)

- Không còn công cụ editor dựng booster: `BoosterSetup` (menu Setup Boosters), `AutoCompleteEffectBuilder` (Build AutoComplete Effects) và `CleanBrushEffectBuilder` (Build CleanBrush Effects) cùng `.meta` đã được xóa khỏi cây làm việc. `CleanCollectionEffectBuilder` vẫn được giữ lại.
- `BoosterController.ResolveReferences()` chạy trong `Awake`, chỉ gán các field còn trống: `board` = `FoodBoardManager.Instance`; `boosters` = mọi `Booster` con của controller, kể cả object đang tắt (gom lại nếu danh sách rỗng hoặc có phần tử null); `boosterRoot` = object con tên `Root`. Đã xoá `Configure` chỉ dành cho editor.
- Click booster dùng `BoxCollider2D`: `Booster.Contains` gọi `OverlapPoint` trên field `boxCollider`; chỉ nhận khi collider bật và object đang hiện. Field trống thì tìm `BoxCollider2D` trên icon rồi trên chính booster; không có thì tự thêm collider trigger vào icon lúc chạy (Unity khớp cỡ theo sprite, mất khi thoát Play Mode). Nên thêm sẵn collider trong scene để chỉnh vùng chạm.
- Nút hủy Pickup dùng field `cancelCollider` theo cùng quy tắc (`PickupHandBooster.CancelContains`). Không còn kiểm tra bằng bounds sprite; đã xoá `SetHitArea`, `SetPrompt`, `HasPrompt`.
- `TryGetPointerDown` vẫn cần: đổi điểm chạm sang world cho icon booster và cho chế độ chọn đĩa Pickup (nút hủy, `TryGetDishAt`). Phương án chưa làm: chuyển sang EventSystem + `Physics2DRaycaster` + `IPointerClickHandler`.
- Prefab particle (`Prefabs/Effects/AutoCompleteStar`, `AutoCompleteBurst`, `AutoCompleteSparkle`, `CleanFoamTrail`, `CleanFoamPuff`, `CleanBubblePop`) và sprite hiệu ứng gán tay trong Inspector; chỉnh trực tiếp trên prefab vì không còn builder ghi đè.

## Hiệu ứng click độc lập — UIButtonClick (03/11/2026)

- Script `Assets/HotPot/Scripts/UI/UIButtonClick.cs`: component riêng cho phản hồi nhấn, không gọi `TryActivate`, `Consume`, kiểm tra số lượt, popup hoặc khóa board.
- Gán `Hit Area` là `BoxCollider2D` của nút, `Visual` là transform hình ảnh cần nhún, `Input Camera` là camera nhận thao tác. Nên để collider ngoài transform hình ảnh co giãn để vùng chạm ổn định.
- Component dùng action riêng `<Pointer>/press`, đăng ký trong `OnEnable`, gỡ trong `OnDisable`; không polling bằng `Update`. Nếu chưa gán camera, lấy `Camera.main` một lần trong `Awake`.
- Nhấn trong collider đang bật và hiển thị sẽ co hình ảnh xuống 90%, bật lên 106%, rồi trở về scale ban đầu trong 0,22 giây. Chỉnh `Duration`, `Pressed Scale`, `Rebound Scale` trong Inspector.
- Dùng `Time.unscaledDeltaTime`, độc lập tốc độ game và điều kiện sử dụng booster. Có thể gọi `Play()` trực tiếp để chạy riêng hiệu ứng; bấm lặp bắt đầu lại nhịp, không cộng dồn scale.
- Tắt nút/component sẽ dừng hiệu ứng và trả scale gốc. Nếu chức năng hiện tại ẩn nút ngay (ví dụ vào chọn Pickup), hiệu ứng cũng ngừng theo nút; không trì hoãn gameplay để chờ nhún xong.
- Đã gắn và gán tham chiếu `UIButtonClick` cho 4 nút trong scene `Assets/HotPot/Scenes/HotPotDemo.unity`.
- Kiểm tra Play Mode ngày 03/11/2026 trên object thử độc lập, không có `Booster`: co đúng 90%, gọi lặp không cộng dồn scale, tắt component trả scale gốc và hiệu ứng hoàn tất khi `timeScale = 0`. Unity biên dịch script thành công. Lệnh build .NET riêng chưa thành công vì thiếu các DLL tham chiếu tại `Temp/bin/Debug`; không coi đó là một lần build đạt.

## PickupHand

### Điều kiện

- Có ít nhất một cột còn từ 2 hàng trở lên. Khi mọi cột chỉ còn 1 hàng thì chặn: chỉ lắc icon, không hiện thông báo ngắn; log ghi lý do "cần ít nhất một cột còn từ 2 hàng trở lên" (hằng `PickupNeedsTwoRows`, kiểm tra đầu tiên trong `CanPickup`, dùng `GetBoardMaxRows()`).
- Có đĩa trong phạm vi `maxVisibleRows` mà toàn bộ nhóm link không đóng băng.
- Băng chuyền có đủ slot trống cho tổng `TotalAmount` cả nhóm, bao gồm hai nửa của đĩa đôi.

### Xử lý

1. Khóa board, ẩn các nút booster và hiện thông báo `PickupPrompt` (`Choose a plate to pick up`) kèm nút hủy.
2. Giới hạn số hàng hiển thị; kéo camera xuống nếu cần để các hàng chọn được nằm phía trên thông báo.
3. Mỗi frame cập nhật các đĩa có thể chọn theo số slot trống hiện tại.
4. Chạm đĩa không hợp lệ sẽ nháy cả nhóm và hiện short message đúng lý do: thành viên đóng băng, thiếu chỗ cho cả nhóm hoặc ngoài vùng chọn. Khi không thể bật Pickup cũng hiện lý do từ chối.
5. Chọn hợp lệ: làm sáng cả nhóm, tăng thứ tự vẽ, nhấc và phóng nhẹ đồng thời mọi thành viên; mở mọi nắp đang ẩn.
6. Gọi board.TryPickDishAt(column, row, settings.Consume): board kiểm tra nhóm và giữ đủ slot trước khi chốt lượt; thanh toán lỗi thì trả slot và giữ đĩa.
   Khi lấy thành công mới gọi `settings.Consume()` một lần cho cả nhóm, không trừ theo số đĩa hoặc số phần.
7. Nếu lấy thất bại, trả vị trí, scale và thứ tự vẽ của cả nhóm về trước lúc nhấc, hiện short message theo trạng thái lúc thất bại.
8. Chờ board hết animation, đưa camera về và cập nhật lại lựa chọn thông thường. Khôi phục trạng thái khóa input trước khi vào chế độ chọn, tránh lưu nhầm khóa tạm của Pickup.

Camera có `SafeAreaCamera`: trong Play Mode component này tính `orthographicSize` theo Y ban đầu của camera (gốc khung hình), không theo Y hiện tại, nên lúc Pickup trượt camera xuống thì khung nhìn dịch xuống thật chứ không bị phóng to/thu nhỏ. Nếu sau này có code chủ động đặt lại vị trí camera cho bố cục mới, gọi `SafeAreaCamera.RefreshFramingOrigin()`.

Hủy lựa chọn sẽ ẩn thông báo, khôi phục camera, giao diện board và khóa input đã lưu, hiện lại nút. Chơi lại hoặc disable controller sẽ hủy Pickup và trả các đĩa đang nhấc cùng camera về trạng thái ban đầu.

### Hàm và quyền sở hữu khóa board

- `BeginPickupSelection` lưu `pickupInputWasLocked` trước khi khóa board và đánh dấu `pickupOwnsInput`.
- `TryPickupDishAt(column, row)` kiểm tra vị trí, vùng chọn và điều kiện cả nhóm trước khi bắt đầu hiệu ứng. Tọa độ cột/hàng bắt đầu từ 0.
- `FoodBoardManager.GetPickMembers` lấy danh sách thành viên hiện tại; `GetPickupGroupRejection` là nguồn kiểm tra chung cho khả năng chọn và lý do từ chối. Kiểm tra băng mọi thành viên trước khi kiểm tra tổng sức chứa.
- `LiftPickupGroup` dùng chung tiến độ cho cả nhóm; mỗi thành viên được lưu vị trí, scale và sorting riêng. `RestorePickupPoses` trả các giá trị này khi chưa lấy thành công.
- `CancelPickupForReplay` hủy coroutine Pickup, dọn tư thế và thông báo chọn, trả camera cùng giới hạn hàng, khôi phục khóa input. Được gọi từ `OnDisable` và `LevelController.LoadLevel`.
- Với board ban đầu không khóa, hoàn tất, thất bại và hủy Pickup đều trả `InputLocked = false`. Nếu board đã bị khóa từ trước, giữ nguyên khóa đó.

### Short message khi bị từ chối

Nội dung lấy từ `BoosterConfig.shortDes` (tiếng Anh, xem mục Khóa booster theo level và BoosterConfig):

| Trường hợp | ShortDesType |
| --- | --- |
| Không còn đĩa tại ô vừa chạm | `PlateEmpty` |
| Một thành viên nhóm link đóng băng | `LinkedPlateFrozen` |
| Đĩa đơn đóng băng | `PlateFrozen` |
| Không đủ slot cho cả nhóm link | `LinkedPlatesNoSpace` |
| Không đủ slot cho đĩa đơn | `BeltFull` |
| Không còn nguyên liệu để bật Pickup | `NoPlateToPick` |

- `ShowPickupDenied` nháy mọi thành viên của nhóm và gọi `UIManager.ShowShortMessage` (tham số `showMessage = false` thì chỉ nháy và ghi log). Đĩa nằm ngoài số hàng cho phép chọn chỉ nháy, không hiện thông báo. Chưa gán board (`BoardNotAssigned`) chỉ ghi log, không hiện cho người chơi; đã bỏ ba loại `BoardMissing`, `PickupOutsideRows` và `PickupFailed` khỏi `ShortDesType` (05/11/2026). Nếu bị từ chối khi đang chọn, giữ chế độ chọn để người chơi chọn nhóm khác hoặc hủy.
- Khi sức chứa thay đổi trong lúc nhấc, kiểm tra lại trạng thái để hiện đúng lý do sau khi đặt cả nhóm xuống. Trường hợp thất bại chưa có lý do cụ thể chỉ nháy đĩa và ghi log, không hiện thông báo.
- Khi không thể bật Pickup, hiện lý do do `TryActivate`/`CanUse` trả về. Thành viên link nằm ngoài vùng hàng chọn vẫn thuộc nhóm và được lấy cùng thành viên hợp lệ được chạm.
- Riêng hết lượt sẽ chuyển sang yêu cầu mua trước khi kiểm tra điều kiện gameplay; nhánh này không hiện short message từ chối. Khi tạm dừng, hàm kích hoạt trả về ngay.

### Chẩn đoán khi không lấy được đĩa

- `FoodBoardManager.logPickFailures` mặc định bật, ghi log tiền tố `[FoodBoardManager] Không pick được:` khi khóa input, thiếu camera, chạm trượt, chọn đĩa sâu hoặc giao dịch lấy nhóm thất bại.
- `PickGroup` chặn khi tạm dừng; dùng `DescribePickBlock` mô tả nhóm chưa lộ, thành viên đóng băng/ẩn/hết phần, hoặc nguyên nhân ở đĩa đơn.
- `DishPickBatch.LastFailure` được xóa khi bắt đầu mỗi `TryFill`, ghi nguyên nhân nếu dữ liệu đĩa không hợp lệ, thiếu `ChefManager`, thiếu slot (kèm số trống/cần), hoặc slot không nhận được food. Khi điền thất bại giữa lượt hoặc callback commitCost trả false/ném exception, rollback các slot đã điền trước đó, giữ nguyên đĩa trên board.
- `ConveyorSlot.TryFillFood` tự tìm `Food` trên chính slot hoặc object con, kể cả đang tắt, nếu chưa gán `slotFood`. Không tìm được sẽ ghi lỗi `[ConveyorSlot]` và trả về `false`; vẫn nên gán đúng tham chiếu trong prefab.
- Các log kỹ thuật này không thay thế short message: Pickup vẫn dùng `GetPickupGroupRejection`, còn giao dịch thất bại mà không có lý do gameplay tương ứng thì không hiện thông báo.

### Kết quả kiểm tra Play Mode trước thay đổi số lượt/collider

- Nhóm hai đĩa nhấc cùng độ cao; hủy giữa hiệu ứng trả đúng vị trí và scale cả hai.
- Lấy thành công cả nhóm dùng đúng hai slot cho hai phần; board hết khóa sau khi hoàn tất.
- Hủy chế độ chọn, hủy giữa lúc nhấc và lấy thất bại do hết chỗ đều trả khóa board đúng trạng thái trước Pickup.
- Chọn đĩa không đóng băng nhưng thành viên link đóng băng hiện đúng thông báo băng; thiếu chỗ cho nhóm hiện đúng thông báo nhóm.
- Tắt controller dọn khóa và ẩn thông báo chọn; khóa có sẵn trước Pickup được giữ nguyên.
- Build runtime và editor thành công. Dữ liệu thử chỉ thay đổi trong Play Mode, không lưu vào scene.
- Cần kiểm tra lại với code hiện tại: có/hết lượt, popup mua tạm, trừ lượt thành công/thất bại/hủy, collider icon và nút hủy, cùng các log lỗi mới. Các mục này chưa được xác nhận lại trong lượt cập nhật tài liệu ngày 02/11/2026.

## AutoComplete

- Điều kiện: `CustomerManager.HasAutoCompleteTarget()` trả về true.
- Xin giữ chỗ công việc qua `CustomerManager.TryReserveAutoComplete`.
- Hiện ngôi sao particle gần đầu bếp.
- Với từng nguyên liệu còn thiếu, ưu tiên lấy từ băng chuyền qua `ChefManager.TryTakeFood`, rồi túi giữ món qua `FoodHoldingStorage.TryTake`, sau đó từ board qua `board.TryTakePortion`. Khi lấy từ túi, món bay từ miệng túi và túi nảy nhẹ.
- Nếu không tìm thấy nguyên liệu, ghi cảnh báo và vẫn tạo hình nguyên liệu từ `IconConfig` để tiếp tục hiệu ứng.
- Nguyên liệu bay vào sao; sao bay tới phía trên ô món rồi biến mất kèm hiệu ứng nổ.
- Tạo các hình nguyên liệu ở trạng thái `Prepared`, bung ra và lần lượt gửi vào khay qua `LaunchBoosterServe` theo thứ tự danh sách công việc.
- Chờ khay hết nhận nguyên liệu trước khi kết thúc booster.

## CleanBrush / Thu hồi món

- Điều kiện: board không đang animation, controller rảnh, băng có food, kho giữ món đã gán và hoạt động; không bắt đầu khi túi đang trả món.
- Khóa thao tác board trong toàn bộ lượt; tạm dừng băng chuyền, sơ chế và các chuyến phục vụ mới. Công việc chặt dở được hủy, giữ nguyên trạng thái food.
- Lấy từng phần food khỏi slot; túi luôn hiển thị, không kéo ra hay thu vào. Food nhấc lên 0,15 giây với nhịp phóng nhẹ, rồi bay vòng cung ở kích thước gốc tới miệng túi (`FoodHoldingStorage.MouthPosition`); các lượt khởi hành cách nhau 0,07 giây.
- Không dùng chổi, bọt xà phòng hoặc bong bóng. Các field hình chổi cũ còn giữ để tương thích dữ liệu Inspector.
- Trước khi bay, food nháy sáng nhẹ với viền vàng ấm và phóng tối đa 12%; hình đĩa và food cùng nhấc lên một đoạn theo Lift Height. Khi food bắt đầu bay vào túi, đĩa trống rơi nhanh dần về đúng slot rồi nảy nhẹ; slot root và bóng dưới đĩa giữ nguyên. Clean chờ cả food lẫn đĩa đáp xong trước khi bỏ tạm dừng băng. Chỉnh Plate Drop Duration để đổi thời gian rơi (mặc định 0,2 giây, thêm nhịp nảy 0,12 giây). Sau nhịp chuẩn bị, food giữ scale gốc và bay tới miệng túi. Tới nơi, `DropIn` hiện món tại miệng túi rồi chìm xuống đúng vị trí phía sau lớp trước, thu về `storedScale`, và túi nảy. Nếu bị hủy giữa chuyến bay, `Reveal` đặt món thẳng vào vị trí. Chỉnh `Lift Duration`, `Collect Pulse`, `Collection Highlight` trên CleanBrushBooster.
- Mỗi phần được ghi vào FoodHoldingStorage ngay sau khi rời băng, giữ nguyên FoodType, FoodState, sprite và scale world gốc trên băng. Food giữ scale gốc khi bay vào, sau đó thu về `storedScale` trong túi; scale cha được bù khi xếp hình. Chờ tất cả phần đáp rồi khôi phục các cờ tạm dừng và khóa board cũ.
- Clean vẫn thu mọi food trên băng, không lọc theo order khi thu.
- FoodHoldingStorage dùng CustomerManager.GetNeededIngredients để trả đúng số phần còn thiếu sau khi trừ food đã có hoặc đang bay trên băng. (04/11/2026) Chỉ trả nguyên liệu mà khách hiện tại nhận được ngay (`CustomerManager.TryFindCustomer`): nguyên liệu phụ ở lại trong túi tới khi đế của món đã được giữ chỗ, món có nguyên liệu đang bay tới hoặc khay đang bận cũng chờ. Món trả về giữ FoodState; món thô được đầu bếp tự sơ chế lại khi vào trạm (`ChefManager.FindSlotToPrepare`). Nhờ vậy túi không tự lấp băng bằng món chưa nhận được và gây bế tắc giả. Khi trả món, food nhô lên miệng túi (`riseDuration`), túi nảy, rồi bay từ miệng túi tới một slot trống đã giữ chỗ trước; băng tiếp tục chạy trong lượt trả.
- Phần đã sơ chế trở lại băng ở trạng thái Prepared, phần thô vẫn Raw. Food chỉ bị xóa khỏi kho sau khi đáp thành công; lượt trả thất bại giữ lại phần trong kho.
- AutoComplete tìm nguyên liệu theo thứ tự băng chuyền, túi giữ món, rồi board để không tạo trùng food đã cất.
- CancelCleanCollection hủy lượt đang thu, giải phóng khóa và cờ tạm dừng. LevelController.LoadLevel gọi hủy thu và ResetForLevel để xóa kho cũ khi chơi lại.
- Scene đã gán controller.holdingStorage tới component FoodHoldingStorage ở FoodHoding/FoodHoldingTray; food hiển thị trong Bag/FoodRoot.
## Shuffle

### Điều kiện và phạm vi

- Có ít nhất hai nhóm nguyên liệu còn phần và khác nhau về loại hoặc số lượng; có thể nằm trong cùng một đĩa đôi.
- `CollectShuffleSpots` bỏ qua toàn bộ đĩa thuộc nhóm link; các đĩa này không tham gia xáo vị trí hoặc nguyên liệu.
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
- Particle ngôi sao và vệt sáng thu món được giữ instance để tái sử dụng.
- Các phụ thuộc chính: `FoodBoardManager`, `ChefManager`, `CustomerManager`, `Food`, `ConveyorSlot` và `OneShotEffects`.

## Quy tắc khi chỉnh sửa

- Mọi hàm mới hoặc sửa phải có comment EN/VI trực tiếp phía trên; tooltip chỉ dùng tiếng Việt.
- Tách các trách nhiệm độc lập thành helper có tên rõ ràng, giữ callback và lifecycle gọn.
- Khi thay đổi logic lấy/trả/xáo đĩa, tuân thủ các quy tắc board, gravity và ánh xạ cột hàng chờ trong `AGENTS.md`.
- Cập nhật tài liệu này khi hành vi booster thay đổi.


## Particle thu món

- CleanCollectionTrail: vệt chấm sáng vàng kem và sao thưa trong không gian world, sống 0,5–0,6 giây, kích thước hạt gấp đôi bản đầu, bắt đầu khi food bay và dừng phát khi chuyến bay kết thúc hoặc bị hủy. Controller tái sử dụng vệt đã kết thúc.
- Prefab trong Assets/HotPot/Prefabs/Effects/Collection; vật liệu trong Art/Materials/Particles; dùng texture Effects có sẵn. Collection Trail được gán sẵn trên CleanBrushBooster; chỉnh trực tiếp prefab/vật liệu.
- (04/11/2026) Đã xóa `CleanCollectionEffectBuilder` (menu Build Clean Collection Effects) cùng các công cụ Setup/Build khác trong `Assets/HotPot/Editor`; không còn cách dựng lại tự động.


## Bảo toàn nguyên liệu giữa Clean và AutoComplete

- ConveyorSlot tạo ArrivalId mới khi bắt đầu chuyến đáp và khi xóa food. FoodFlyAnimator, chuyến sơ chế và chuyến trả từ túi chỉ hoàn tất/hủy đúng ArrivalId đã giữ; callback cũ không tác động slot dùng lại, kể cả cùng FoodType.
- AutoComplete gọi FoodHoldingStorage.CancelPendingReturn trước khi giữ chỗ order và lấy nguyên liệu. Phần đang trả được giữ lại đúng một lần trong kho, phần giữ chỗ trên băng và sprite bay tạm được hủy an toàn.
- Kiểm tra Play Mode: slot dùng lại cùng loại không bị callback cũ hoàn tất/xóa; nguyên liệu gốc trong kho lấy được đúng một lần; Clean rồi AutoComplete giữa chuyến trả bàn giao an toàn; chạy tới hết level qua 8 lượt AutoComplete, tất cả khách được phục vụ, kho trống và không có cảnh báo thiếu nguyên liệu. Lượt thử dùng nguyên liệu thật từ board và thông số tăng tốc chỉ trong Play Mode.


## Thông báo ngắn khi Clean trên băng trống

- Clean bị từ chối khi băng trống sẽ gọi UIManager.ShowShortMessage với nội dung lấy từ BoosterConfig `shortDes`, mục `BeltEmpty`: `"The conveyor is empty."` (trước đây là chuỗi tiếng Việt "Băng chuyền đang trống"). Icon vẫn lắc, không chạy booster.
- ShortMessageView dùng prefab Assets/HotPot/Prefabs/UI/ShortMessage.prefab đặt dưới UIManager trong scene. Thanh nâu bo góc viền vàng, chữ sáng, không chặn raycast.
- Hiện bằng trượt lên 28 đơn vị UI và tăng alpha trong 0,18 giây; giữ 1,5 giây; trượt nhẹ lên và mờ trong 0,2 giây. Dùng thời gian độc lập với timeScale. Hiện lặp thay nội dung và bắt đầu lại thời gian, không xếp chồng.
- UIManager.HideAll và OnDisable xóa trạng thái thông báo. Có thể chỉnh vị trí, kích thước và thời gian trong prefab/Inspector.

## Builder đã xóa và asset cần kiểm tra trước khi dọn

- Đã xóa khỏi cây làm việc: `Assets/HotPot/Editor/BoosterSetup.cs`, `AutoCompleteEffectBuilder.cs`, `CleanBrushEffectBuilder.cs` cùng `.meta`; không còn bước xóa các file này.
- Asset không còn dùng: `Art/Sprites/Effects/FxStarRays.png`, `Art/Sprites/Effects/FxCometArc.png`, `Art/Sprites/UI/Boosters/AutoCompleteStar.png`, `Art/Materials/Effects/SpriteAdditive.mat` (nếu có). Kiểm tra reference trước khi xoá.
- Giữ lại: shader `Art/Shaders/Effects/SpriteAdditive.shader`, material trong `Art/Materials/Particles`, các prefab particle ở trên và texture `FxSoftStar`, `FxMagicRing`, `FxBokeh`, `FxTrail`, `FxBubble`, `FxFoam`, `CleanBroom`.

## API tài nguyên chuyên biệt (02/11/2026)

OnAddResource và OnRemoveResource là private. Consume dùng OnRemoveBooster để ghi lượng tiêu thực tế vào thống kê tổng/trong màn, lưu cùng số dư trước callback. API booster từ chối Coin/UnlimitedLives và số lượng âm; không ghi/lưu khi không đổi số dư. Đã kiểm tra bằng harness .NET với dependency giả; chưa chạy lại toàn bộ gameplay trong Play Mode. Xem Assets/HotPot/Documentation/DataSystem.md.

## Giao dịch và chuẩn hóa tài nguyên (02/11/2026)

OnRemoveCoin/OnRemoveBooster/Consume trả bool và chỉ trừ đủ, thiếu thì giữ nguyên số dư/thống kê và không lưu. Shuffle/AutoComplete/Clean chỉ chạy sau Consume thành công; Pickup chốt tại ranh giới batch nói trên. TryBuyBooster gom trừ Coin/cộng đủ ba lượt/thống kê vào một lần lưu, giữ trạng thái gameplay nếu lưu báo lỗi và chỉ thông báo sau chốt. Popup cũ MoreBooster cũng dùng giao dịch với giá đọc từ BoosterConfig và ba lượt. Đã pass 69 assertions của harness mã nguồn thật với Unity/PlayerPrefs giả lập, gồm lỗi lưu và rollback slot Pickup; chưa chạy lại toàn bộ Play Mode. Kết quả Play Mode ở các mục trước thuộc phiên bản trước giao dịch. Quy tắc chuẩn hóa tài nguyên xem Assets/HotPot/Documentation/DataSystem.md.

Hợp đồng 02/11/2026: ProfileManager và PlayerDataManager luôn tồn tại tại nơi gọi runtime, không kiểm tra null cho hai manager. Vẫn kiểm tra dữ liệu đã khởi tạo và RecordManager/SaveService cần cho giao dịch. Đăng ký Coin dùng cờ isObservingCoin để gỡ đúng listener; Edit Mode không truy cập runtime. Kiểm tra thiếu profile cũ là lịch sử, không phải nhánh hiện tại.

Cập nhật 03/11/2026: Lỗi ghi được SaveService bắt và log cảnh báo; không hoàn tác xu, booster hoặc thống kê, giao dịch vẫn trả true và UI/gameplay tiếp tục. Mỗi lần mua vẫn thử lưu một lần, kiểm tra đủ tiền/sức chứa và không cấp thiếu gói. Bỏ snapshot/rollback dữ liệu lưu; rollback slot Pickup khi thao tác không hợp lệ vẫn giữ. Harness cập nhật pass 67 assertions với PlayerPrefs giả lập, chưa chạy lại toàn bộ Play Mode; kết quả 69 assertions trước đó thuộc bản có rollback lưu.

Cập nhật 03/11/2026 — vòng đời CoinBinding: CanvasBuyBooster đăng ký và đọc số dư trong OnEnable, gỡ đúng listener trong OnDisable. Đã bỏ Update và lời gọi binding trong Show; lúc đang mở chỉ nhận sự kiện đổi Coin, không polling hoặc tự đổi nguồn. Nếu dữ liệu chưa khởi tạo thì bật lại popup mới thử nối lại. Đã rà mã luồng Open bật GameObject và cleanup OnDisable; chưa chạy lại Play Mode cho thay đổi này.

Cập nhật 03/11/2026: CanvasBuyBooster cache PlayerDataManager vào field playerData tại OnEnable. Đăng ký/đọc Coin, mua hàng và gỡ listener đều dùng cùng field này; không lấy lại manager trong Purchase hoặc helper binding. Edit Mode không truy cập ProfileManager. Đã rà tham chiếu và diff; chưa chạy lại Play Mode.

Cập nhật UI 03/11/2026: CanvasBuyBooster kế thừa LevelPopupView → UICanvasPrime. UIManager lấy prefab từ UIRef, không đặt popup shop sẵn trong scene. Đóng qua Close/CloseUI/HideAll đều dọn đăng ký Coin và nhả đúng pause của shop; nội dung gói 900 Coin/3 lượt và giao dịch không đổi.
Kiểm chứng đổi tên UIButtonClick: Unity biên dịch thành công; giữ GUID 559b3f6cfb27c1f47aee4f766c7afd6e; cả 5 component scene nhận đúng class mới. Không thay đổi hành vi hiệu ứng.

UIButtonClick bổ sung UnityEvent onClick tùy chọn để dùng chung cho setting (03/11/2026). Khi nhấn trúng collider: chạy Play rồi gọi event. Bốn nút booster giữ event trống, không gọi thêm logic booster hoặc thay đổi điều kiện sử dụng.

## BuyBoosterPopup mới (10/11/2026)

CanvasBuyBooster trực tiếp kế thừa UICanvasPrime, dùng TMP và ba nút Coin/Watch/đóng, hiệu ứng và feedback như CanvasPause. Tên/mô tả/giá theo BoosterConfig; Coin nhận 3 lượt qua TryBuyBooster. Watch nhận riêng 1 lượt không trừ Coin. Field activeAdsBooster trên prefab mặc định tắt; tắt chỉ hiện Coin ở giữa, bật hiện thêm Watch +1. Theo yêu cầu tạm, Watch gọi thẳng OnWatchAdsSuccess, người dùng tự gán SDK callback sau. Thưởng qua OnAddBooster, kiểm tra đủ sức chứa và chặn nhận lặp. Test ví/stat riêng đã xác nhận flag ẩn/hiện, +1/no Coin/callback lặp/thiếu Coin; mua Coin thành công chưa chạy lại với UI mới. Xem claude/UI.md.

Kiểm chứng timing cuối: output/BuyBoosterUI/AnimationCheck.txt PASS bố cục Coin-only ở giữa và hai nút khi bật ads, mở/đóng thực sự hoàn tất khi timeScale=0, nhả pause sau exit và Watch chỉ +1 một lần/no Coin. Đã trở về Edit Mode, cập nhật ảnh xem trước trạng thái cuối.

Refactor10/11/2026: CanvasBuyBooster và CanvasRevive dùng animation/feedback/cache manager chung ở UICanvasPrime; không còn bản sao ResetAnimation/AnimateOpen/PlayHaptic/PlaySoundClick/CloseAnimated trong từng lớp. PlayerData được cache từ OnEnable qua CacheFeedbackManagers và dùng lại cho giao dịch/listener. Quy tắc Coin+3, Watch+1 và giá hồi sinh/reset giữ nguyên. Harness giá25 assertions và Play Mode regression output/PopupRefactor/Check.txt pass; test dùng tài nguyên riêng, không tiêu ví thật.


## Cập nhật cảnh báo thoát và âm thanh (10/11/2026)
Quy tắc hiện hành thay thế mô tả luôn mở cảnh báo trước đây: Home/Replay từ Pause chỉ hiện WarningQuit nếu CustomerManager.HasCompletedOrder=true (đã giao đủ một order trong lượt chơi). Chưa có order hoàn tất: đi thẳng, không trừ life. Sau order đầu: Confirm mới gọi UseHeart; unlimited không trừ. LoadCustomers reset cờ khi tải/chơi lại màn. Giao một phần hoặc bỏ qua order rỗng không tính.

IngameSoundManager dùng 10 clip trong Assets/HotPot/Audio/Sounds: click=SFX_INGAME_CLICK; mở/đóng popup=sfx_ui_popup_show/sfx_ui_popup_disappear; mua thành công=SFX_INGAME_BUY_ITEM; thưởng Watch booster=SFX_INGAME_REWARD_COLLECT; đề nghị revive=SFX_INGAME_OUT_OF_SPACE; giới thiệu booster=SFX_INGAME_NEW_ITEM_UNLOCK; thắng/thua=SFX_INGAME_GAME_WIN/SFX_INGAME_GAME_FAILED; hồi sinh Ads hoặc Coin thành công=sfx_game_revive. UICanvasPrime phát âm vòng đời popup, UIManager phát kết quả màn. Mọi clip dùng nguồn SFX chung nên tuân theo Sound, hoạt động khi pause. Giữ âm chế biến/phục vụ hiện có; thư mục này không cung cấp nhạc nền mới.

Kiểm chứng: Unity biên dịch thay đổi và đã gán/lưu đủ 10 clip trong scene, mỗi clip có thời lượng >0. Chưa kiểm tra nghe trực tiếp hoặc toàn bộ luồng chơi sau thay đổi này.
