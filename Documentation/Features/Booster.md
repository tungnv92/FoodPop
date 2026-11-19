# Booster

## Thành phần
| File (`Assets/HotPot/Scripts/...`) | Vai trò |
| --- | --- |
| `Booster/BoosterController.cs` | Thao tác, điều kiện, logic và hiệu ứng booster |
| `Booster/Booster.cs` | Nút booster (số lượt, khóa, collider) |
| `Booster/BoosterType.cs` | Định danh hành động |
| `Booster/PickupHandBooster.cs`, `AutoCompleteBooster.cs`, `CleanBrushBooster.cs`, `ShuffleBooster.cs` | Cài đặt riêng |
| `Booster/BoosterUnlockIntro.cs` | Giới thiệu booster mở khóa |
| `Booster/FoodHoldingStorage.cs` | Tủ giữ món (hiện/ẩn, ô chứa, nảy ô, trả món); đã gộp `FoodHoldingTray` vào đây |
| `Config/BoosterConfig.cs` → `Config/BoosterConfig.asset` | Giá, mở khóa, tên, mô tả, thông báo ngắn |

`BoosterController.ResolveReferences()` (Awake) tự gán field trống: `board` = `FoodBoardManager.Instance`, `boosters` = mọi `Booster` con, `boosterRoot` = con `Root`, `levelController` tự tìm. Không có tool Setup/Builder; prefab particle (`Prefabs/Effects/AutoComplete*`, `Prefabs/Effects/Collection`) và sprite hiệu ứng gán tay.

## Luồng chung
1. `Update` đọc chạm bằng Input System (`TryGetPointerDown` đổi sang world); vùng chạm là `BoxCollider2D` của nút (`Booster.Contains`; thiếu thì tự thêm trigger lúc chạy — nên đặt sẵn trong scene).
2. `TryActivate`: game đang pause (`Time.timeScale <= 0`) → trả false. Booster bị khóa level → lắc `objLock`, không làm gì thêm. Booster khác đang chạy → từ chối. Hết lượt → `Booster.RequestBuy()` mở popup mua. Sau đó mới `CanUse` (board không đang animation + điều kiện riêng).
3. Không hợp lệ: lắc icon, `UIManager.ShowShortMessage(ShortDes(type))`, log lý do (tiếng Việt).
4. Hợp lệ: chạy ngay; `RunLocked` khóa board và đánh dấu bận, `finally` khôi phục khóa input đã lưu.
5. Trừ lượt: `Booster.Consume()` → `OnRemoveBooster(type, 1, "ingame")`. Shuffle/AutoComplete/Clean trừ khi bắt đầu (không chạy nếu trả false). Pickup trả `settings.Consume` vào `TryPickDishAt`, chỉ chốt sau khi batch giữ đủ slot; lỗi thì nhả slot, giữ đĩa. Hồi sinh (dọn băng về board) không trừ lượt Clean.

## Số lượt, khóa level và BoosterConfig
- `Booster.boosterType` (`ResourceType`, khác `Booster.Type`) đọc số lượt qua `GetResourceValue`; còn lượt hiện `objNumber`/`txtNumber`, hết hiện `objPlus`. Đăng ký `RegisterResourceChange` trong `OnEnable`.
- `BoosterConfig` mỗi entry: `type`, `price` (hiện 900), `unlockLevel`, `unlockGift` (2), `boosterName`, `boosterInfo`. API `GetName`, `GetInfo`, `GetPrice`, `GetUnlockLevel`, `IsLocked`, `GetUnlockGift`, `GetShortDes`. Đọc qua `IngameConfig.Instance.Boosters`.
- Mở khóa: PickupHand, CleanBrush, AutoComplete level 1; Shuffle level 5. `BoosterController` nghe `LevelController.LevelLoaded` → `RefreshLock(levelNumber)` bật `objLock` (chữ "Lv N") hoặc `objUnlock`.
- Nội dung hiển thị tiếng Anh, gọi mọi thứ là **plate**. `shortDes`:

| ShortDesType | Nội dung |
| --- | --- |
| BoosterRunning | Another booster is in use. |
| BoardBusy | Please wait for the board to settle. |
| BeltEmpty | The conveyor is empty. |
| PickupPrompt | Choose a plate to pick up |
| NoPlateToPick | No plates to pick up. |
| PlateEmpty / PlateFrozen / LinkedPlateFrozen | This plate is empty. / This plate is frozen. / A linked plate is still frozen. |
| BeltFull / LinkedPlatesNoSpace | Not enough free space on the conveyor. / Not enough space for all linked plates. |
| NotEnoughCoin | Not enough gold. You need {0} gold to buy this booster. |
| PurchaseFailed | Purchase failed. Please try again. |
| TryPrompt | Tap to try! |

## <a id="mua-booster"></a>Mua booster (CanvasBuyBooster)
- `UIManager.ShowBuyBooster(type)` → popup từ UIRef (`Prefabs/UI/Booster/BuyBoosterPopup.prefab`), giữ pause tới khi đóng xong.
- Tên/mô tả/giá từ BoosterConfig; `presentations` chỉ giữ icon; chữ số lượng "x3".
- Coin: `TryBuyBooster(resource, 3, price, "buy_booster")`; thiếu tiền/thất bại giữ popup và hiện `NotEnoughCoin`/`PurchaseFailed`. Không tự dùng booster.
- `activeAdsBooster` (mặc định false): tắt thì chỉ nút Coin ở giữa; bật thì thêm Watch +1 (`OnAddBooster(resource, 1, "watch_ads_booster")`, kiểm tra sức chứa, chặn nhận lặp). Watch tạm gọi thẳng `OnWatchAdsSuccess`, chưa có SDK.
- Cache `PlayerDataManager` trong `OnEnable` cho hiển thị Coin, mua và gỡ listener.

## Giới thiệu booster mở khóa
- `BoosterUnlockIntro` trên object BoosterController. Khi `LevelLoaded` với level ≥ 2, chọn booster có `unlockLevel` đúng level và chưa xem (PlayerPrefs `FoodPop.BoosterIntro.<BoosterType>`; menu ngữ cảnh `Test/Xóa cờ đã xem giới thiệu booster`).
- Luồng: chờ không booster nào chạy → pause → nút hiện dáng khóa → chờ `popupDelay` 0,8 s → `CanvasBoosterIntro` (tia sáng xoay, icon nhấp nhô, "NEW BOOSTER!", tên, mô tả, "Got it"; xuất hiện theo bước nền tối `blackBackground` → khung `Root` → icon, unscaled) → icon bay vòng cung xuống nút (`flyDuration` 0,65 s), ổ khóa thu nhỏ, nút nảy → tặng `unlockGift` lượt (`Booster.Grant`, lưu ngay) → đánh dấu đã xem → lượt dùng thử. Thứ tự này đảm bảo thoát app giữa hai bước không mất quà (tệ nhất là lần sau xem lại intro và nhận quà thêm một lần).
- Lượt dùng thử: `BoosterTrialOverlay` (canvas riêng `sortingOrder` 32767, tối toàn màn trừ lỗ tròn quanh nút, bàn tay Spine, dòng `TryPrompt`); nút phập phồng `pulseScale`; `BoosterController.BeginTrial` chỉ nhận chạm đúng nút đó → nhả pause → `TryActivate` như thường (**trừ một lượt**).
- Hủy giữa chừng (`OnDisable`, nạp lại màn) → `StopIntro()` dọn mọi thứ; chưa đánh dấu xem thì lần nạp sau chạy lại.

## PickupHand
- Điều kiện: có ít nhất một cột ≥ 2 hàng; có nhóm đĩa lộ trong `maxVisibleRows` không đóng băng và thỏa sức chứa `DishPickPlan` ([ConveyorChef.md](ConveyorChef.md#sức-chứa-khi-lấy-đĩa-dishpickplan)).
- Xử lý: khóa board, ẩn nút booster, hiện `PickupPrompt` + nút hủy (`cancelCollider`), kéo camera xuống nếu cần (SafeAreaCamera giữ size theo Y gốc; đổi bố cục camera thì gọi `RefreshFramingOrigin()`). Chạm nhóm không hợp lệ → nháy cả nhóm + thông báo từ `GetPickupGroupRejection`. Chạm hợp lệ → nhấc cả nhóm, mở nắp, `board.TryPickDishAt(column, row, settings.Consume)`; thất bại thì trả tư thế/scale/sorting.
- Hủy/replay/disable (`CancelPickupForReplay`, chạy qua `CancelForReplay` từ `OnDisable` và `LevelController.LoadLevel`) trả đĩa, camera và khóa input về trạng thái trước Pickup; `pickupCameraMoved` giữ tới khi camera trượt về xong nên replay giữa lúc trượt vẫn đưa camera về Y gốc.
- Chẩn đoán: `FoodBoardManager.logPickFailures` (log `[FoodBoardManager] Không pick được:`), `DishPickBatch.LastFailure`; xem [FoodBoardManager.md](FoodBoardManager.md).

## AutoComplete
- Điều kiện `CustomerManager.HasAutoCompleteTarget()`; giữ chỗ qua `TryReserveAutoComplete`; trước đó `FoodHoldingStorage.CancelPendingReturn`.
- Lấy từng nguyên liệu thiếu theo thứ tự: băng (`ChefManager.TryTakeFood`, không lấy phần đầu bếp đang sở hữu) → túi (`FoodHoldingStorage.TryTake`, túi nảy) → board (`board.TryTakePortion`); không tìm thấy thì cảnh báo và vẫn tạo hình từ icon.
- Nguyên liệu bay vào sao → sao bay tới ô món, nổ → các phần Prepared gửi vào khay qua `LaunchBoosterServe`. Chờ khay nhận xong mới kết thúc.

## CleanBrush và tủ giữ món
- Điều kiện: băng có food, tủ sẵn sàng, tủ không đang trả món (tủ đầy vẫn dùng được, khi đó mọi food về board). Khóa board, tạm dừng băng/sơ chế/phục vụ mới; hủy việc chặt dở.
- **Clean dọn tối đa `maxClearSlots` (4) slot** (`ClearBelt(settings, maxClearSlots, FreeSpots)`, dùng chung với hồi sinh): `SelectClearTargets` chia food trên băng thành **món thừa** (không order hiện tại nào cần, so với `GetNeededIngredients(..., false)`, mỗi nhu cầu khớp một lần) và **món đang cần**. Thứ tự chọn trong giới hạn 4: (1) lấp mọi ô tủ còn trống, món thừa trước, thiếu thì món đang cần → vào tủ (`LaunchToHolding`); (2) món thừa; (3) món đang cần còn lại — (2) và (3) về board (`LaunchToBoard`). Ví dụ tủ trống 4 ô → 4 món vào tủ (món thừa trước); tủ còn 1 ô → 1 món vào tủ + 3 món thừa về board; tủ đầy → 4 món thừa về board. Lưu ý: tủ chỉ tự trả món khách cần, nên món thừa trong tủ có thể nằm lâu và chiếm ô (chỉ rời tủ khi khách cần hoặc AutoComplete lấy). Slot không được chọn ở lại băng. Tủ đầy vẫn dùng được (mọi phần được dọn về board). Các chuyến bay theo thứ tự slot, cách nhau `collectInterval`.
- Mỗi phần: nháy sáng + nhấc (`Lift Duration` 0,15 s), đĩa trống rơi về slot (`Plate Drop Duration` 0,2 s + nảy), food bay vòng cung (vệt `CleanCollectionTrail`). Vào tủ: tới đúng ô (`FoodHoldingStorage.GetPosition`), `DropIn` hiện ở kích thước gốc rồi thu về `storedScale`, giữ FoodType/FoodState. Về board: thành đĩa thô 1 phần (mất trạng thái đã sơ chế).
- `CleanBrushBooster` chỉ còn thông số thu món (`maxClearSlots` 4, `collectInterval`, `liftDuration`, `liftHeight`, `collectionHighlight`, `collectPulse`, `plateDropDuration`, `flyDuration`, `flyArc`) và hiệu ứng (`collectionTrail`, `sortingOrder`). Field chổi/bong bóng cũ cùng asset chỉ dùng cho chúng (`CleanFoamTrail`, `CleanFoamPuff`, `CleanBubblePop`, material `CleanSparkle`/`CleanFoam`/`CleanDrop`, sprite `FxFoam`/`FxBubble`) đã bị xóa ngày 17/11/2026.
- Tủ: prefab `Prefabs/Ingame/FoodHoldingTray.prefab` (instance `FoodHolding`), cấu trúc `Bag/Back` (sorting 0) – `Bag/FoodRoot` (SortingGroup 10) với 4 ô `Food`, `Food (1)`, `Food (2)`, `Food (3)` (mỗi ô có sprite `tray`, sorting 1; mặc định tắt) – `Bag/Front` (sorting 20). **Không xóa `Bag`/`FoodRoot`.** Chỉ còn một component `FoodHoldingStorage` trên root (script `FoodHoldingTray` đã gộp vào và bị xóa ngày 17/11/2026). **Tủ luôn hiện ở scale thiết kế**, không còn animation mọc lên/thu xuống (đã bỏ `ShowBag`, `HideIfEmpty`, `HideImmediate`, `startHidden`, `bottomY`, `showDuration`, `hideDuration`); chỉ các ô bật/tắt theo food. **Ô khay ẩn lúc đầu:** `Awake` tắt mọi ô (kể cả khi scene override bật ô để chỉnh bố cục); ô chỉ bật khi food đáp xuống (`DropIn`) hoặc được hiện lại (`Reveal`), và tắt khi food rời kho. **Tủ không nảy; cả ô (khay + food) nảy:**
  - Food đáp xuống khay (`DropIn`): cả ô bật lên 2 nhịp thấp dần (`spotHopHeight` 0,3 theo tọa độ cục bộ của `FoodRoot`; 0 để chỉ bẹp-giãn tại chỗ), mỗi lần chạm xuống bẹp nhẹ (`spotBounceStrength` 0,1 = bẹp 10%), trong `spotBounceDuration` 0,45 s; food thu từ kích thước gốc về `storedScale`.
  - Food rời ô để trả về băng: ô chỉ bẹp-giãn tại chỗ trong cùng thời gian. Ô luôn trở về vị trí/scale thiết kế.
  - `Reveal` (hủy lượt thu/trả) đặt food lên khay ngay, không nảy. AutoComplete lấy food thì ô tắt luôn, không nảy.
- Ô giữ món: field `spots` của `FoodHoldingStorage`; để trống thì lấy các con trực tiếp của `FoodRoot` theo thứ tự hierarchy (hiện tại: `Food` → `Food (2)` → `Food (3)` → `Food (1)`); số ô = số food tối đa. Mỗi ô giữ một food: `Store` giữ ô trống đầu tiên và đặt food (sorting 5, đang ẩn) lên khay, ô bật khi food đáp, bỏ food khỏi kho thì tắt ô. Thông số: `storedScale` (prefab 0,8), `foodOffset` (vị trí food so với tâm ô), `settleSpeed`, `riseDuration`. Đã bỏ cách xếp hàng cũ (`perRow`, `maxRows`, `tiltAngle`…) và miệng túi (`mouth`/`mouthOffset`).
- Trả món: `CustomerManager.GetNeededIngredients` trừ phần đã có/đang bay trên băng; chỉ trả nguyên liệu khách hiện tại nhận ngay (`TryFindCustomer`) để tránh bế tắc giả. Food phóng to tại ô rồi bay tới slot trống đã giữ; chỉ xóa khỏi kho (và tắt ô) sau khi đáp thành công. AutoComplete lấy food từ tủ cũng bay ra từ ô của nó. Phần thô được đầu bếp sơ chế lại.
- **Hồi sinh kẹt băng đẩy thẳng mọi món về board, không dùng tủ** (`ClearBelt(settings, int.MaxValue, 0)` — dọn **mọi** slot, không giới hạn 4, 17/11/2026): `CanCleanForRevive` chỉ cần board, cấu hình Clean và băng có food (tủ đầy hay trống không ảnh hưởng). Hủy chuyến trả đang chờ của tủ, tạm dừng băng/sơ chế/phục vụ, rồi **mọi** slot có food lần lượt (cách nhau `collectInterval`) nháy sáng + nhấc, đĩa rơi về slot, food bay vòng cung về board: `board.TryPickReturnColumn` **chia đều** món về các cột đang dùng bằng cách xoay vòng lần lượt từng cột (con trỏ `nextReturnColumn`, reset khi nạp level, nối tiếp giữa các lượt dọn); food chỉ bay tới **hàng đầu tiên** của cột đó (không bay tới đúng vị trí cuối cột), đĩa được thêm vào cuối cột, đáp xong (kể cả khi bị dừng giữa chừng) `AddReturnedDish` tạo đĩa thô 1 phần (mở, không băng) và nảy lên — cùng cách Clean đưa phần dư về board. Món đã sơ chế trên băng về board thành món thô. Hiệu ứng bay dùng chung `FlyFoodFromSlot`. Thành công khi băng còn ≥ 1 slot trống; không trừ lượt booster.
- `ConveyorSlot.ArrivalId` đổi mỗi lượt đáp/xóa food để callback cũ không chạm slot dùng lại.
- `CancelCleanCollection` + `ResetForLevel` khi replay.
- Replay/disable gọi `BoosterController.CancelForReplay()`: chạy hủy Clean và Pickup, rồi nếu Shuffle/AutoComplete còn chạy thì dừng coroutine, trả `OffscreenHidingSuspended = false`, khôi phục khóa input, ẩn sprite bay, dừng sao AutoComplete và bỏ trạng thái bận. Giữ chỗ nguyên liệu của AutoComplete được reset khi nạp lại khách; chỉ tắt controller (không replay) thì chưa trả giữ chỗ.

## Shuffle
- Điều kiện: ≥ 2 nhóm nguyên liệu khác loại/số lượng. Bỏ qua đĩa thuộc nhóm link; đĩa đôi giữ vị trí nhưng xáo food bên trong (loại + số lượng đi cùng nhau).
- Ưu tiên: nhu cầu đầu tiên (`GetNeededIngredients`) vào hàng 1 không đóng băng, nhu cầu kế vào hàng 2; ưu tiên ô không ẩn; còn lại ngẫu nhiên.
- Hiệu ứng: đóng nắp + vật liệu cầu vồng, đĩa đơn bay vòng cung tới vị trí mới, áp lại nắp/băng theo vị trí đích, `ApplyColumnDishes`.

## Hiệu ứng nút và thông báo
- `UIButtonClick` (`Scripts/UI/UIButtonClick.cs`): `Hit Area` (BoxCollider2D), `Visual`, `Input Camera`; co 90% → 106% → gốc trong 0,22 s, unscaled; `onClick` UnityEvent tùy chọn. Không gọi logic booster.
- `ShortMessage.prefab` dưới UIManager: trượt lên + fade 0,18 s, giữ 1,5 s, mờ 0,2 s; hiện lặp thay nội dung.
- Effect dùng `RentFx`/`ReleaseFx` dưới object `BoosterFx`.

**Kiểm chứng:** BuyBooster (Coin-only/Ads, timing), Clean ↔ AutoComplete bảo toàn nguyên liệu, PickupHand nhóm link đã Play Mode PASS ở các phiên trước. Tủ 4 ô (17/11/2026): đã kiểm trong Editor rằng code biên dịch, prefab nhận 4 ô, `Store` lần thứ 5 trả null; sau khi gộp `FoodHoldingTray` thì prefab/scene không còn script thiếu và giá trị hiện/ẩn được giữ nguyên; **chưa chạy Play Mode** cho luồng thu/trả, nảy food/ô, chọn món cần vào tủ, phần dư và hồi sinh trả food về board (mới chỉ biên dịch). Chưa chạy thử trong Unity: khóa level, giới thiệu booster mở khóa (bay icon, lớp phủ, quà), mua Coin thành công qua UI mới.
