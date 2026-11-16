# Booster

## Thành phần
| File (`Assets/HotPot/Scripts/...`) | Vai trò |
| --- | --- |
| `Booster/BoosterController.cs` | Thao tác, điều kiện, logic và hiệu ứng booster |
| `Booster/Booster.cs` | Nút booster (số lượt, khóa, collider) |
| `Booster/BoosterType.cs` | Định danh hành động |
| `Booster/PickupHandBooster.cs`, `AutoCompleteBooster.cs`, `CleanBrushBooster.cs`, `ShuffleBooster.cs` | Cài đặt riêng |
| `Booster/BoosterUnlockIntro.cs` | Giới thiệu booster mở khóa |
| `Booster/FoodHoldingTray.cs`, `FoodHoldingStorage` | Túi giữ món |
| `Config/BoosterConfig.cs` → `Config/BoosterConfig.asset` | Giá, mở khóa, tên, mô tả, thông báo ngắn |

`BoosterController.ResolveReferences()` (Awake) tự gán field trống: `board` = `FoodBoardManager.Instance`, `boosters` = mọi `Booster` con, `boosterRoot` = con `Root`, `levelController` tự tìm. Không có tool Setup/Builder; prefab particle (`Prefabs/Effects/AutoComplete*`, `Prefabs/Effects/Collection`) và sprite hiệu ứng gán tay.

## Luồng chung
1. `Update` đọc chạm bằng Input System (`TryGetPointerDown` đổi sang world); vùng chạm là `BoxCollider2D` của nút (`Booster.Contains`; thiếu thì tự thêm trigger lúc chạy — nên đặt sẵn trong scene).
2. `TryActivate`: game đang pause (`Time.timeScale <= 0`) → trả false. Booster bị khóa level → lắc `objLock`, không làm gì thêm. Booster khác đang chạy → từ chối. Hết lượt → `Booster.RequestBuy()` mở popup mua. Sau đó mới `CanUse` (board không đang animation + điều kiện riêng).
3. Không hợp lệ: lắc icon, `UIManager.ShowShortMessage(ShortDes(type))`, log lý do (tiếng Việt).
4. Hợp lệ: chạy ngay; `RunLocked` khóa board và đánh dấu bận, `finally` khôi phục khóa input đã lưu.
5. Trừ lượt: `Booster.Consume()` → `OnRemoveBooster(type, 1, "ingame")`. Shuffle/AutoComplete/Clean trừ khi bắt đầu (không chạy nếu trả false). Pickup trả `settings.Consume` vào `TryPickDishAt`, chỉ chốt sau khi batch giữ đủ slot; lỗi thì nhả slot, giữ đĩa. Hồi sinh dùng Clean không trừ lượt.

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
- Luồng: chờ không booster nào chạy → pause → nút hiện dáng khóa → chờ `popupDelay` 0,8 s → `CanvasBoosterIntro` (tia sáng xoay, icon nhấp nhô, "NEW BOOSTER!", tên, mô tả, "Got it"; xuất hiện theo bước nền tối `blackBackground` → khung `Root` → icon, unscaled) → icon bay vòng cung xuống nút (`flyDuration` 0,65 s), ổ khóa thu nhỏ, nút nảy → đánh dấu đã xem → tặng `unlockGift` lượt (`Booster.Grant`) → lượt dùng thử.
- Lượt dùng thử: `BoosterTrialOverlay` (canvas riêng `sortingOrder` 32767, tối toàn màn trừ lỗ tròn quanh nút, bàn tay Spine, dòng `TryPrompt`); nút phập phồng `pulseScale`; `BoosterController.BeginTrial` chỉ nhận chạm đúng nút đó → nhả pause → `TryActivate` như thường (**trừ một lượt**).
- Hủy giữa chừng (`OnDisable`, nạp lại màn) → `StopIntro()` dọn mọi thứ; chưa đánh dấu xem thì lần nạp sau chạy lại.

## PickupHand
- Điều kiện: có ít nhất một cột ≥ 2 hàng; có nhóm đĩa lộ trong `maxVisibleRows` không đóng băng và thỏa sức chứa `DishPickPlan` ([Conveyor.md](Conveyor.md#sức-chứa-khi-lấy-đĩa-dishpickplan)).
- Xử lý: khóa board, ẩn nút booster, hiện `PickupPrompt` + nút hủy (`cancelCollider`), kéo camera xuống nếu cần (SafeAreaCamera giữ size theo Y gốc; đổi bố cục camera thì gọi `RefreshFramingOrigin()`). Chạm nhóm không hợp lệ → nháy cả nhóm + thông báo từ `GetPickupGroupRejection`. Chạm hợp lệ → nhấc cả nhóm, mở nắp, `board.TryPickDishAt(column, row, settings.Consume)`; thất bại thì trả tư thế/scale/sorting.
- Hủy/replay/disable (`CancelPickupForReplay`, gọi từ `OnDisable` và `LevelController.LoadLevel`) trả đĩa, camera và khóa input về trạng thái trước Pickup.
- Chẩn đoán: `FoodBoardManager.logPickFailures` (log `[FoodBoardManager] Không pick được:`), `DishPickBatch.LastFailure`; xem [foodpop-FoodBoardManager.md](foodpop-FoodBoardManager.md).

## AutoComplete
- Điều kiện `CustomerManager.HasAutoCompleteTarget()`; giữ chỗ qua `TryReserveAutoComplete`; trước đó `FoodHoldingStorage.CancelPendingReturn`.
- Lấy từng nguyên liệu thiếu theo thứ tự: băng (`ChefManager.TryTakeFood`, không lấy phần đầu bếp đang sở hữu) → túi (`FoodHoldingStorage.TryTake`, túi nảy) → board (`board.TryTakePortion`); không tìm thấy thì cảnh báo và vẫn tạo hình từ icon.
- Nguyên liệu bay vào sao → sao bay tới ô món, nổ → các phần Prepared gửi vào khay qua `LaunchBoosterServe`. Chờ khay nhận xong mới kết thúc.

## CleanBrush và túi giữ món
- Điều kiện: băng có food, túi sẵn sàng, túi không đang trả món. Khóa board, tạm dừng băng/sơ chế/phục vụ mới; hủy việc chặt dở.
- Mỗi phần: nháy sáng + nhấc (`Lift Duration` 0,15 s), bay vòng cung tới `FoodHoldingStorage.MouthPosition` (cách nhau 0,07 s, vệt `CleanCollectionTrail`), `DropIn` chìm vào túi và thu về `storedScale`; đĩa trống rơi về slot (`Plate Drop Duration` 0,2 s + nảy). Giữ FoodType/FoodState.
- Túi: prefab `Prefabs/Ingame/FoodHoldingTray.prefab` (instance `FoodHolding`), cấu trúc `Bag/Back` (`paper_bag.png`, sorting 0) – `Bag/FoodRoot` (SortingGroup 10) – `Bag/Front` (`paper_bag_front.png`, sorting 20). **Không xóa `Bag`/`FoodRoot`.** Ẩn khi trống: `ShowBag()` mọc lên (0,28 s), `HideIfEmpty()` thu xuống (0,2 s), `Bounce()` nảy. Bố cục trong túi: `storedScale` 0,6, `perRow` 3, `maxRows` 3, `tiltAngle` 10°.
- Trả món: `CustomerManager.GetNeededIngredients` trừ phần đã có/đang bay trên băng; chỉ trả nguyên liệu khách hiện tại nhận ngay (`TryFindCustomer`) để tránh bế tắc giả. Food nhô lên miệng túi rồi bay tới slot trống đã giữ; chỉ xóa khỏi kho sau khi đáp thành công. Phần thô được đầu bếp sơ chế lại.
- `ConveyorSlot.ArrivalId` đổi mỗi lượt đáp/xóa food để callback cũ không chạm slot dùng lại.
- `CancelCleanCollection` + `ResetForLevel` khi replay.

## Shuffle
- Điều kiện: ≥ 2 nhóm nguyên liệu khác loại/số lượng. Bỏ qua đĩa thuộc nhóm link; đĩa đôi giữ vị trí nhưng xáo food bên trong (loại + số lượng đi cùng nhau).
- Ưu tiên: nhu cầu đầu tiên (`GetNeededIngredients`) vào hàng 1 không đóng băng, nhu cầu kế vào hàng 2; ưu tiên ô không ẩn; còn lại ngẫu nhiên.
- Hiệu ứng: đóng nắp + vật liệu cầu vồng, đĩa đơn bay vòng cung tới vị trí mới, áp lại nắp/băng theo vị trí đích, `ApplyColumnDishes`.

## Hiệu ứng nút và thông báo
- `UIButtonClick` (`Scripts/UI/UIButtonClick.cs`): `Hit Area` (BoxCollider2D), `Visual`, `Input Camera`; co 90% → 106% → gốc trong 0,22 s, unscaled; `onClick` UnityEvent tùy chọn. Không gọi logic booster.
- `ShortMessage.prefab` dưới UIManager: trượt lên + fade 0,18 s, giữ 1,5 s, mờ 0,2 s; hiện lặp thay nội dung.
- Effect dùng `RentFx`/`ReleaseFx` dưới object `BoosterFx`.

**Kiểm chứng:** BuyBooster (Coin-only/Ads, timing), Clean ↔ AutoComplete bảo toàn nguyên liệu, PickupHand nhóm link đã Play Mode PASS ở các phiên trước. Chưa chạy thử trong Unity: khóa level, giới thiệu booster mở khóa (bay icon, lớp phủ, quà), mua Coin thành công qua UI mới.
