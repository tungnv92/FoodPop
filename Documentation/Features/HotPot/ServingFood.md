# ServingFood — phục vụ, khay và hoạt cảnh món

Mỗi gốc prefab `ServingHotPot` (`Prefabs/Ingame/ServingHotPot.prefab`) có `ServingFood`, `ServingTrayView`, `TrayDishStack`; mỗi instance gán vào `CustomerManager.Lane.servingFood`.

| Component | Trách nhiệm |
| --- | --- |
| CustomerManager | Hàng khách, ưu tiên gửi, giữ chỗ công thức, tiến độ order, khách rời |
| ServingFood | Bay/đáp nguyên liệu, ghép món, bay món vào order, âm thanh/hiệu ứng, pool sprite và pool món (`RentDish`/`ReturnDish`) |
| ServingTrayView | Số ô (`SetSlotCount`) và vị trí; hình ô từ `DishSlot.prefab` |
| TrayDishStack | Thứ tự lớp, vị trí chồng, món hoàn chỉnh |
| IngameConfig | Thông số bay/ghép dùng chung (`ingredientFlight`, `dishAssembly`, `trayStack`) |

## Luồng phục vụ
1. `BeginOrder(view, dishes, recipes, callbacks)` nhận thẻ order, danh sách prefab `FinalFoodView` (mỗi nhóm một, có thể null) và công thức.
2. `CustomerManager` giữ chỗ nguyên liệu (`DishProgress.Reserve`) rồi gọi `LaunchServe`; phóng lỗi thì nhả giữ chỗ, không lấy món khỏi băng. Mỗi ô khay chỉ một nguyên liệu đang bay tới; các ô/khay khác chạy song song.
3. **Luật đế**: nguyên liệu đầu của công thức phải tới (hoặc đang bay tới) trước; các nguyên liệu khác thứ tự tự do. Món có `anyIngredientOrder = true` bỏ luật đế.
4. Ô khay dựng ảnh từng lớp (`DishSlotView.BeginDish` / `RevealIngredient` / `ClearDish`): mọi lớp và đĩa ẩn lúc đầu; nguyên liệu bay tới `GetLandingPosition(type)`; khi đáp, lớp tương ứng nảy hiện và nguyên liệu đầu tiên làm đĩa nảy ra (`ShowPlate`, `plateShowDuration` 0,2 s, `layerRevealDuration` 0,18 s).
   - Đĩa chọn theo món: `FinalFoodView.plate` riêng (ví dụ `dish_double` của FinalFood_8), nếu không thì `Plate_Tea` khi prefab có lớp `TeaLeaves`, còn lại `Plate`.
   - Nhóm không có prefab dùng cách chồng cũ (`TrayDishStack.AddPiece`) và hoàn tất không có ảnh.
5. Đủ công thức → `CompleteDish` → ghép (`MergeStack`, poof `dishCompleteEffect`) → món hoàn chỉnh (pool) bay lên thẻ (`FlyDishToOrder`), thẻ đánh dấu tích; callback phát khi món tới.
6. `ResetOrder` dừng coroutine, bỏ callback, dọn ô và trả mọi sprite về pool (kể cả đang bay). Đổi khách, replay, hủy manager đều reset.

Thẻ order (`OrderItemView`) chỉ hiện instance prefab món và dấu tích; icon nguyên liệu đã tới mờ về `doneAlpha` 0.35 (8 prefab `Prefabs/OrderView/OrderPreview_*`).

Phần đi thẳng từ đầu bếp (`DirectServeReservation.TryDeliver` → `LaunchBoosterServe`, dùng chung với AutoComplete): xem [Conveyor.md](../../Handoff/Conveyor.md).

## Hoạt cảnh riêng cho món
- `FinalFoodView.isAnimate` + `foodAnimation` (base abstract `Scripts/Food/FinalFoodAnimation.cs`: `Play()` coroutine, `ResetAnimation()`, `SetSorting()`; `OnDisable` reset đạo cụ).
- Khi nguyên liệu cuối đáp với `isAnimate = true`, `DishSlotView.RevealIngredient` chạy hoạt cảnh; `ServingFood.AssembleDish` chờ `IsPreparing = false` rồi mới ghép. Chỉ instance trên khay chạy; thẻ order không. Dùng `Time.deltaTime` nên theo Pause.
- `AnimatorFinalFoodAnimation`: Play bật state Prepare, chờ Animation Event. Clip Prepare 1,15 s, event `PlayParticles` 0.08, `StopParticles` 0.6, `Show...Dish` 1.05, `CompleteAnimation` 1.14 — **phải giữ event hoàn tất** khi chỉnh clip, không thì kẹt luồng phục vụ. Animator tắt mặc định.
- Velocity Over Lifetime của particle: X/Y/Z phải cùng mode (TwoConstants).

| Món | Script | Clip/controller | Ghi chú |
| --- | --- | --- | --- |
| FinalFood_10 (Edamame + Salt) | `SaltedEdamameAnimation` | `Animations/Food/SaltedEdamame` | Lọ muối `Anims/salt_1` (scale cố định 1), hạt `SaltGrains.mat`; `edamame_3` fade alpha 0→1 từ 0.08 đến 1.05 s |
| FinalFood_96/98/100/102 (gà ức/xiên/đùi/cánh + TeriyakiSauce) | `TeriyakiChickenAnimation` | `Animations/Food/TeriyakiChicken{Breast,Skewer,Thigh,Wing}` | Lọ sốt quét và bóp; hạt `Debris_FxDrop.mat` là con của vòi lọ, Simulation Space = Custom (Anims) |

Đạo cụ (muối, lọ sốt) nằm ngoài `Layers` nên không đổi công thức.

**Kiểm chứng:** luồng khay và hoạt cảnh Edamame đã Play Mode PASS; gà teriyaki đã kiểm tra trong Editor (clip, event, alpha), chưa Play Mode; FinalFood_8 (khay riêng, `anyIngredientOrder`) và opacity icon order chưa kiểm chứng trong Unity.
