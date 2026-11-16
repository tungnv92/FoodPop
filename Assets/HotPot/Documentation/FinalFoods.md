# Final food / Món hoàn chỉnh

EN: A final dish is no longer a data entry. There is no `FinalFoodType` enum, no `FinalFoodConfig` ScriptableObject (`Config/FinalFoodConfig.asset`) and no `Tools > HotPot > Build Final Food Config` menu any more (removed 2026-11-03); dishes have no ID and no single sprite. A dish is defined by an ingredient group in a level order (`CustomerOrder.ingredientOrders`, max `CustomerOrder.MaxDishes` = 4): `IngredientOrder.ingredients` (1–6 `FoodType`, first = base, repeats keep quantities, `IngredientOrder.MaxIngredients` = 6) plus an optional `dish` field, a dish prefab whose root carries `FinalFoodView`. The picture on the order card and on the serving tray comes from that prefab. A group without a `dish` prefab still works (the card ticks) but shows no dish picture.
VI: Món hoàn chỉnh không còn là một mục dữ liệu. Không còn enum `FinalFoodType`, ScriptableObject `FinalFoodConfig` (`Config/FinalFoodConfig.asset`) và menu `Tools > HotPot > Build Final Food Config` (đã gỡ ngày 2026-11-03); món không có ID và không có một sprite duy nhất. Một món được xác định bởi một nhóm nguyên liệu trong order của level (`CustomerOrder.ingredientOrders`, tối đa `CustomerOrder.MaxDishes` = 4): `IngredientOrder.ingredients` (1–6 `FoodType`, phần tử đầu = đế, lặp lại giữ số lượng, `IngredientOrder.MaxIngredients` = 6) cùng trường tùy chọn `dish`, là prefab món có `FinalFoodView` ở gốc. Hình trên thẻ order và khay phục vụ lấy từ prefab này. Nhóm không có prefab `dish` vẫn chạy (thẻ vẫn tick) nhưng không có hình món.

EN: Status 2026-11-03: code and docs updated after the refactor; `FinalFoodType.cs`, `FinalFoodConfig.cs`, `FinalFoodConfigMenu.cs` and `Config/FinalFoodConfig.asset` are now deleted from the project, and Unity compiled without errors on 2026-11-03 (Console clear, 0 errors); not yet play-tested (đã compile không lỗi trong Unity 03/11, chưa chơi thử). The ~40 old single-image dishes (nigiri tiers, green tea, edamame, teriyaki skewer) lost their picture by the user's choice; their sprite files remain in Art but nothing references them. See the layered-prefab section below for the current dish prefabs.
VI: Trạng thái 2026-11-03: code và tài liệu đã cập nhật sau refactor; `FinalFoodType.cs`, `FinalFoodConfig.cs`, `FinalFoodConfigMenu.cs` và `Config/FinalFoodConfig.asset` đã bị xóa khỏi dự án, Unity đã compile không lỗi ngày 03/11 (Console sạch, 0 lỗi); chưa chơi thử (đã compile không lỗi trong Unity 03/11, chưa chơi thử). Khoảng 40 món ảnh đơn cũ (các cấp nigiri, trà xanh, edamame, xiên teriyaki) đã mất hình theo lựa chọn của người dùng; file sprite vẫn nằm trong Art nhưng không còn gì tham chiếu. Xem mục prefab món ghép lớp bên dưới cho các prefab món hiện tại.

## Tham khảo công thức cũ / Legacy recipe reference

EN: Tham khảo công thức cũ — không còn là dữ liệu chạy game; ID chỉ là lịch sử. The tables below (and the Ingredient usage, Green tea and Edamame sections) record the former `FinalFoodConfig` recipes (source art `Art/Sprites/Foods/Sushi/Final/<số món>/<tên>-nigiri.png`, recipes in `output/imagegen/sushi-progression/tiers-3-6-prompts.md`). Keep them only as a design reference; the game no longer reads them. The `Setup Green Tea` / `Setup Edamame` menus now only register the ingredient icons in FoodIconConfig (tea leaves/ice, edamame/salt) and no longer create the drink/side-dish recipes or import the Final sprites.
VI: Tham khảo công thức cũ — không còn là dữ liệu chạy game; ID chỉ là lịch sử. Các bảng bên dưới (cùng các mục Số công thức dùng mỗi nguyên liệu, Trà xanh và Edamame) ghi lại công thức `FinalFoodConfig` trước đây (ảnh nguồn `Art/Sprites/Foods/Sushi/Final/<số món>/<tên>-nigiri.png`, công thức trong `output/imagegen/sushi-progression/tiers-3-6-prompts.md`). Chỉ giữ làm tham khảo thiết kế; game không còn đọc. Menu `Setup Green Tea` / `Setup Edamame` giờ chỉ đăng ký icon nguyên liệu trong FoodIconConfig (lá trà/đá, edamame/muối), không còn tạo công thức đồ uống/món phụ hay nhập sprite Final.


| ID | FinalFoodType | Món | Số món | Nguyên liệu |
|---|---|---|---|---|
| 12 | SakeNigiri2 | Nigiri cá hồi (2 món) | 2 | Cơm + Cá hồi |
| 13 | SakeNigiri3 | Nigiri cá hồi (3 món) | 3 | Cơm + Cá hồi + Wasabi |
| 14 | SakeNigiri4 | Nigiri cá hồi (4 món) | 4 | Cơm + Cá hồi + Trứng cá hồi + Rong biển |
| 15 | SakeNigiri5 | Nigiri cá hồi (5 món) | 5 | Cơm + Cá hồi + Trứng cá hồi + Rong biển + Dưa leo |
| 16 | SakeNigiri6 | Nigiri cá hồi (6 món) | 6 | Cơm + Cá hồi + Trứng cá hồi + Rong biển + Dưa leo + Wasabi |
| 22 | MaguroNigiri2 | Nigiri cá ngừ (2 món) | 2 | Cơm + Cá ngừ |
| 23 | MaguroNigiri3 | Nigiri cá ngừ (3 món) | 3 | Cơm + Cá ngừ + Wasabi |
| 24 | MaguroNigiri4 | Nigiri cá ngừ (4 món) | 4 | Cơm + Cá ngừ + Wasabi + Rong biển |
| 25 | MaguroNigiri5 | Nigiri cá ngừ (5 món) | 5 | Cơm + Cá ngừ + Wasabi + Rong biển + Dưa leo |
| 26 | MaguroNigiri6 | Nigiri cá ngừ (6 món) | 6 | Cơm + Cá ngừ + Wasabi + Rong biển + Dưa leo + Nước tương |
| 32 | EbiNigiri2 | Nigiri tôm (2 món) | 2 | Cơm + Tôm |
| 33 | EbiNigiri3 | Nigiri tôm (3 món) | 3 | Cơm + Tôm + Wasabi |
| 34 | EbiNigiri4 | Nigiri tôm (4 món) | 4 | Cơm + Tôm + Dưa leo + Rong biển |
| 35 | EbiNigiri5 | Nigiri tôm (5 món) | 5 | Cơm + Tôm + Dưa leo + Rong biển + Trứng cá hồi |
| 36 | EbiNigiri6 | Nigiri tôm (6 món) | 6 | Cơm + Tôm + Dưa leo + Rong biển + Trứng cá hồi + Nước tương |
| 44 | TamagoNigiri4 | Nigiri trứng cuộn (4 món) | 4 | Cơm + Trứng cuộn + Rong biển + Dưa leo |
| 45 | TamagoNigiri5 | Nigiri trứng cuộn (5 món) | 5 | Cơm + Trứng cuộn + Rong biển + Dưa leo + Thanh cua |
| 46 | TamagoNigiri6 | Nigiri trứng cuộn (6 món) | 6 | Cơm + Trứng cuộn + Rong biển + Dưa leo + Thanh cua + Gừng ngâm |
| 52 | UnagiNigiri2 | Nigiri lươn (2 món) | 2 | Cơm + Lươn |
| 53 | UnagiNigiri3 | Nigiri lươn (3 món) | 3 | Cơm + Lươn + Wasabi |
| 54 | UnagiNigiri4 | Nigiri lươn (4 món) | 4 | Cơm + Lươn + Dưa leo + Rong biển |
| 55 | UnagiNigiri5 | Nigiri lươn (5 món) | 5 | Cơm + Lươn + Dưa leo + Rong biển + Trứng cuộn |
| 56 | UnagiNigiri6 | Nigiri lươn (6 món) | 6 | Cơm + Lươn + Dưa leo + Rong biển + Trứng cuộn + Nước tương |
| 62 | TakoNigiri2 | Nigiri bạch tuộc (2 món) | 2 | Cơm + Bạch tuộc |
| 63 | TakoNigiri3 | Nigiri bạch tuộc (3 món) | 3 | Cơm + Bạch tuộc + Wasabi |
| 64 | TakoNigiri4 | Nigiri bạch tuộc (4 món) | 4 | Cơm + Bạch tuộc + Rong biển + Dưa leo |
| 65 | TakoNigiri5 | Nigiri bạch tuộc (5 món) | 5 | Cơm + Bạch tuộc + Rong biển + Dưa leo + Trứng cá hồi |
| 66 | TakoNigiri6 | Nigiri bạch tuộc (6 món) | 6 | Cơm + Bạch tuộc + Rong biển + Dưa leo + Trứng cá hồi + Wasabi |
| 72 | HotateNigiri2 | Nigiri sò điệp (2 món) | 2 | Cơm + Sò điệp |
| 73 | HotateNigiri3 | Nigiri sò điệp (3 món) | 3 | Cơm + Sò điệp + Wasabi |
| 74 | HotateNigiri4 | Nigiri sò điệp (4 món) | 4 | Cơm + Sò điệp + Trứng cá hồi + Rong biển |
| 75 | HotateNigiri5 | Nigiri sò điệp (5 món) | 5 | Cơm + Sò điệp + Trứng cá hồi + Rong biển + Dưa leo |
| 76 | HotateNigiri6 | Nigiri sò điệp (6 món) | 6 | Cơm + Sò điệp + Trứng cá hồi + Rong biển + Dưa leo + Wasabi |
| 84 | KanikamaNigiri4 | Nigiri thanh cua (4 món) | 4 | Cơm + Thanh cua + Rong biển + Dưa leo |
| 85 | KanikamaNigiri5 | Nigiri thanh cua (5 món) | 5 | Cơm + Thanh cua + Rong biển + Dưa leo + Trứng cuộn |
| 86 | KanikamaNigiri6 | Nigiri thanh cua (6 món) | 6 | Cơm + Thanh cua + Rong biển + Dưa leo + Trứng cuộn + Nước tương |

## Số công thức dùng mỗi nguyên liệu / Ingredient usage

| Nguyên liệu | FoodType | Số công thức |
|---|---|---|
| Cơm | SushiRice | 36 |
| Rong biển | Seaweed | 24 |
| Dưa leo | Cucumber | 21 |
| Trứng cá hồi | SalmonRoe | 10 |
| Trứng cuộn | Tamagoyaki | 7 |
| Wasabi | Wasabi | 12 |
| Thanh cua | CrabStick | 5 |
| Cá hồi | Salmon | 5 |
| Cá ngừ | Tuna | 5 |
| Tôm | Shrimp | 5 |
| Lươn | Unagi | 5 |
| Bạch tuộc | Octopus | 5 |
| Sò điệp | Scallop | 5 |
| Nước tương | SoySauce | 4 |
| Gừng ngâm | PickledGinger | 1 |

## Trà xanh / Green tea

| ID | FinalFoodType | Đồ uống | Nguyên liệu trên board |
|---|---|---|---|
| 91 | GreenTea | Trà xanh nóng | TeaLeaves (18) |
| 92 | IcedGreenTea | Trà xanh đá | TeaLeaves (18) + Ice (19) |

EN: TeaLeaves represents dried loose green tea, not fresh leaves. Water and cups are supplied resources and do not consume board portions. The existing raw-to-prepared interaction turns tea leaves into brewed tea; this addition does not introduce a separate brewing station. Each order consumes one portion of each listed ingredient. Existing level orders are unchanged; select the drinks in the level editor when designing a new order. Drink artwork lives in Art/Sprites/Foods/Drinks (Raw, Prepared, Shared and Final).

VI: TeaLeaves là lá trà xanh khô, không phải lá tươi. Nước và cốc có sẵn, không tiêu hao phần trên board. Tương tác chế biến hiện có đổi lá trà thành trà đã pha; phần bổ sung này chưa tạo trạm pha trà riêng. Mỗi order tiêu hao một phần mỗi nguyên liệu đã liệt kê. Order các level hiện tại được giữ nguyên; chọn đồ uống trong trình sửa level khi thiết kế order mới. Ảnh đồ uống nằm tại Art/Sprites/Foods/Drinks (Raw, Prepared, Shared và Final).
## Edamame / Món phụ đậu nành non

| ID | FinalFoodType | Món | Nguyên liệu trên board |
|---|---|---|---|
| 101 | BoiledEdamame | Edamame luộc | Edamame (20) |
| 102 | SaltedEdamame | Edamame luộc rắc muối | Edamame (20) + Salt (21) |

EN: Both dishes use the same bowl and pod arrangement; visible salt crystals distinguish the salted version. Raw edamame uses a pod icon and its prepared state uses the plain boiled bowl. Raw salt is shown as a salt bag; prepared salt uses loose crystals. Water and bowls do not consume board portions. Uses the existing preparation interaction, without adding a separate boiling station. Existing level orders remain unchanged. Re-register artwork with Tools > HotPot > Setup Edamame. Art lives in Art/Sprites/Foods/Edamame.

VI: Hai món dùng cùng bát và cách xếp đậu; hạt muối phân biệt bản rắc muối. Edamame thô dùng ảnh quả đậu, trạng thái chế biến dùng bát đậu luộc thường. Muối thô có hình túi muối; muối đã chuẩn bị dùng ảnh hạt muối. Nước và bát không tiêu hao phần trên board. Dùng tương tác chế biến hiện có, chưa thêm trạm luộc riêng. Giữ nguyên order level hiện tại. Đăng ký lại ảnh bằng Tools > HotPot > Setup Edamame. Ảnh nằm trong Art/Sprites/Foods/Edamame.

EN: All active three-ingredient nigiri recipes use sushi rice, their main seafood and wasabi. Their icons show a thin pale-green wasabi drizzle in a mayonnaise-style zigzag, without changing the recipe. Other tiers are unchanged.
VI: Tất cả nigiri ba nguyên liệu đang dùng gồm cơm sushi, hải sản chính và wasabi. Icon thể hiện wasabi xanh nhạt rưới thành đường zíc zắc mảnh kiểu mayonnaise, không đổi công thức. Các cấp khác giữ nguyên.

## Món ghép lớp / Layered final food prefabs

EN: Dishes are drawn as several stacked SpriteRenderers (sprites `Art/Sprites/Foods/Sushi/Final/<ingredient>_3.png`) in a prefab. They need no FinalFoodType: the prefab root carries `FinalFoodView` (Scripts/gamebase) whose `layers` list {ingredient (FoodType), renderer (SpriteRenderer)} in recipe order, base first. API: `GetIngredients()`, `Matches(ingredients)` (same ingredients and quantities, any order), `GetLocalSize()` (largest side of the union of layer sprites, root-local) and `SetSorting(sortingLayer, baseOrder)` (keeps the authored relative order of layers). The former `FinalFoodConfig.dishPrefabs` list and `FindDishPrefab` no longer exist. Matching now happens in the Level Editor: picking a prefab copies its ingredients into the group and sets `dish`, and toggling ingredients links the first prefab under `Assets/HotPot` whose ingredients match (or clears `dish`). The order stores the `dish` prefab. At runtime `OrderItemView.ShowDish(FinalFoodView)` instantiates the prefab once per prefab under the card icon and fits it to the slot; `ServingFood` merges the layers on the tray and then shows a pooled instance of the same prefab, which flies to the card. The order-card drop shadow was removed entirely on 2026-11-03 (superseded: earlier text said it was not drawn for prefab dishes), and the slot's count label was removed too; the card slot only shows the dish prefab instance and the checkmark. On the serving tray the slot now builds the dish picture layer by layer: `TrayDishStack.Begin` calls `DishSlotView.BeginDish` to place an instance of the group's prefab on the slot with every layer hidden (plate hidden, picture drawn above the plate); when an ingredient starts flying, the plate pops in; the flyer aims at `GetLandingPosition(type)` (centre of the first still-hidden layer of that ingredient) and on landing `RevealIngredient(type)` pops that layer in. At assembly the built picture is hidden (`HideDish`) and the finished pooled dish takes its place; the plate is hidden when the finished dish flies to the card. See ServingFood.md.
VI: Các món được vẽ bằng nhiều SpriteRenderer chồng lớp (sprite `Art/Sprites/Foods/Sushi/Final/<nguyên liệu>_3.png`) trong một prefab. Không cần FinalFoodType: gốc prefab có `FinalFoodView` (Scripts/gamebase), `layers` liệt kê {ingredient (FoodType), renderer (SpriteRenderer)} theo thứ tự công thức, đế trước. API: `GetIngredients()`, `Matches(ingredients)` (cùng nguyên liệu và số lượng, thứ tự bất kỳ), `GetLocalSize()` (cạnh lớn nhất của hợp các sprite lớp, theo tọa độ local của gốc) và `SetSorting(sortingLayer, baseOrder)` (giữ thứ tự tương đối của các lớp). Danh sách `FinalFoodConfig.dishPrefabs` và `FindDishPrefab` cũ không còn. Việc khớp giờ nằm ở Level Editor: chọn prefab thì chép nguyên liệu của nó vào nhóm và gán `dish`; bật/tắt nguyên liệu thì tự nối với prefab đầu tiên trong `Assets/HotPot` có nguyên liệu khớp (hoặc xóa `dish`). Order lưu prefab `dish`. Khi chạy, `OrderItemView.ShowDish(FinalFoodView)` tạo mỗi prefab một lần dưới icon của thẻ và co vừa ô; `ServingFood` gộp các lớp trên khay rồi hiện một instance (pool) của cùng prefab và bay lên thẻ. Bóng đổ trên thẻ order đã bị gỡ hoàn toàn ngày 2026-11-03 (thay thế: trước đây ghi là không vẽ cho món prefab), nhãn số lượng của ô cũng đã bỏ; ô trên thẻ chỉ hiện instance prefab món và dấu tick. Trên khay phục vụ, ô giờ dựng ảnh món từng lớp: `TrayDishStack.Begin` gọi `DishSlotView.BeginDish` đặt một instance prefab của nhóm lên ô với mọi lớp đang ẩn (đĩa ẩn, ảnh vẽ phía trên đĩa); khi một nguyên liệu bắt đầu bay thì đĩa nảy ra; nguyên liệu bay tới `GetLandingPosition(type)` (tâm lớp còn ẩn đầu tiên của nguyên liệu đó) và khi đáp xuống `RevealIngredient(type)` làm lớp đó nảy hiện. Lúc ghép, ảnh đang dựng bị ẩn (`HideDish`) và món hoàn chỉnh (pool) thế chỗ; đĩa ẩn khi món hoàn chỉnh bay lên thẻ. Xem ServingFood.md.

| Prefab | Lớp | Nguyên liệu |
|---|---|---|
| `Prefabs/Food/FinalFood_1.prefab` | rice_3, salmon_3, seaweed_3 | Cơm + Cá hồi + Rong biển |
| `Prefabs/Food/FinalFood_2.prefab` | rice_3, tuna_3, seaweed_3 | Cơm + Cá ngừ + Rong biển |
| `Prefabs/Food/FinalFood_3.prefab` | rice_3, tuna_3 | Cơm + Cá ngừ |
| `Prefabs/Food/FinalFood_4.prefab` | rice_3, salmon_3 | Cơm + Cá hồi |
| `Prefabs/Food/FinalFood_5.prefab` | wasabi (sprite wasabi) | Wasabi |

EN: Set up by hand (no setup menu, so manual prefab edits are never overwritten; the project rule is no Unity menus that rebuild prefabs). To add a dish: add `FinalFoodView` to the prefab root and fill `layers` in recipe order; the Level Editor scans every prefab under `Assets/HotPot` with `FinalFoodView` on its root, so no list needs updating. The HotPotDemo scene objects `FinalFood_1..4` are not linked to these prefabs. `FinalFood_5` (single `wasabi` layer, Wasabi only) added 2026-11-03. Compiled without errors in Unity on 2026-11-03; not yet play-tested.
VI: Thiết lập tay (không có menu dựng, để sửa riêng trên prefab không bị ghi đè; quy tắc dự án là không có menu Unity dựng lại prefab). Thêm món: gắn `FinalFoodView` vào gốc prefab và điền `layers` theo thứ tự công thức; Level Editor tự quét mọi prefab dưới `Assets/HotPot` có `FinalFoodView` ở gốc nên không cần cập nhật danh sách nào. Các object `FinalFood_1..4` trong scene HotPotDemo chưa liên kết với các prefab này. Đã thêm `FinalFood_5` (một lớp `wasabi`, chỉ Wasabi) ngày 2026-11-03. Đã compile không lỗi trong Unity 03/11, chưa chơi thử.
