# Level Editor — handoff

Cập nhật: 2026-10-22. Editor và runtime đã hỗ trợ đĩa đôi dọc/ngang; phần kiểm chứng và giới hạn còn lại ghi ở cuối tài liệu.

Files:
- `Assets/HotPot/Editor/LevelConfigEditorWindow.cs` (`EditorWindow`, namespace `FoodPop`)
- `Assets/HotPot/Editor/LevelConfigInspector.cs` — Inspector của `LevelConfig`, nút "Mở Level Editor" + các field chỉ đọc để tránh sửa trực tiếp asset.
- Data: `Assets/HotPot/Scripts/gamebase/LevelConfig.cs` (+ `BoardColumn`), `CustomerOrder.cs`, `OrderFood.cs`, `DishShape.cs`
- Runtime nạp level: `Assets/HotPot/Scripts/gamebase/LevelController.cs`
- Runtime board/hiển thị: `Assets/HotPot/Scripts/gamebase/FoodBoardManager.cs`, `Food.cs`, `IngredientDishPool.cs`, `FoodFlyAnimator.cs`.
- Tích hợp booster: `Assets/HotPot/Scripts/Booster/BoosterController.cs`.
- Điền bảng theo order: `Assets/HotPot/Editor/LevelBoardOrderFiller.cs`.
- Asset mẫu: `Assets/HotPot/Config/LevelConfig_01.asset` (3 cột x 7 hàng, trái 2 khách, phải 2 khách)

Mở: menu `Tools/FoodPop/Level Editor` (lấy LevelConfig đang chọn) hoặc nút trong Inspector. `minSize` 980x560.

## Data model
- `LevelConfig` (ScriptableObject, menu `Hot Pot/Level Config`):
  - `leftQueue`, `rightQueue`: `List<CustomerOrder>`; phần tử 0 phục vụ trước.
  - `boardRows` (số ô/cột), `boardColumns`; `ingredientBoard`: `List<BoardColumn>` (trái→phải), mỗi cột `foods` (trên→dưới). Đĩa đôi chiếm hai ô nên số ô không đồng nghĩa số đĩa.
  - `OnValidate` → `ResizeBoard()` giữ đúng `boardColumns` x `boardRows`, kích thước tối thiểu 1; khôi phục danh sách, cột và ô null (bảng luôn hình chữ nhật).
  - `GetBoardCount(FoodType)` cộng số lượng nguyên liệu ở cả hai phần của đĩa đôi, bỏ qua dữ liệu riêng tại ô bị chiếm. `TotalCustomers` đếm khách của cả hai hàng.
  - `GetCell`, `TryGetCoveredCell`, `TryGetCoveringDish`, `IsCoveredCell` tra ô gốc/ô bị chiếm; `CollectBoardProblems` kiểm tra cấu hình đĩa đôi.
- `CustomerOrder.finalFoods`: `List<FinalFoodType>`, tối đa `MaxDishes = 4` món/khách; nguyên liệu mỗi món lấy từ `FinalFoodConfig`.
- `OrderFood` (1 ô board): `foodType`, `requiredCount` (≥1, số nguyên liệu trong đĩa), `isHidden` (nắp, mở khi lên đầu cột), `isFrozen` + `blockValue` (mỗi lần pick bất kỳ đĩa → mọi đĩa băng −1, về 0 thì tan).
- Đĩa đôi thêm `dishShape` (`Single`, `Vertical`, `Horizontal`), `secondFoodType`, `secondCount` (≥1). Hai phần có thể cùng loại. Nắp/băng dùng chung; `CountOf` cộng cả hai phần, `Clear` đặt lại thành đĩa đơn trống.
- `LevelController.LoadLevel` → `FoodBoardManager.LoadIngredientBoard(config.ingredientBoard)` + `CustomerManager.LoadCustomers(config)`.

## Bố cục cửa sổ
`OnGUI`: `DrawToolbar` → (nếu có level) 2 cột: `DrawCustomerPanel` (trái, 50% width, scroll riêng) | divider | `DrawBoardPanel` (phải: board + validation, scroll riêng) → `RunPendingAction`.

Thay đổi cấu trúc (thêm/xoá khách, món, resize, fill, clear) được hoãn qua `pendingAction` và chạy sau khi vẽ xong để layout group cân bằng. Chỉnh field đi qua `SerializedObject` của bản nháp → có Undo/Redo; thao tác trực tiếp dùng `Undo.RecordObject` + `SetDirty` trên bản nháp, không phải asset nguồn.

### Bản nháp và lưu thủ công
- `sourceLevel` giữ asset gốc; `level` là bản sao tạm (`HideInHierarchy | DontSave`) dùng cho toàn bộ chỉnh sửa. Không dùng `HideAndDontSave` vì giá trị này chứa `NotEditable`, khiến các `PropertyField` như Hidden/Frozen bị Unity khóa. Đổi field, thêm/xóa dữ liệu, điền bảng và Undo/Redo không làm đổi asset nguồn; lưu project cũng không ghi các thay đổi của bản nháp vào asset.
- Chỉ khi bấm **Lưu** (hoặc chọn Lưu trong hộp thoại đóng cửa sổ), `SaveDraft` mới chép bản nháp vào nguồn. Giữ nguyên tên/cờ hiển thị của nguồn, gọi `SaveAssetIfDirty(sourceLevel)` để chỉ lưu đúng asset đó.
- **Tải lại** nạp lại dữ liệu từ asset nguồn và bỏ bản nháp hiện tại sau khi xác nhận. Nút **Load** cùng số level cũng nạp lại. Không tự lưu khi tải lại, chuyển level, chọn asset khác hay tạo level mới.
- `savedDraftJson` là mốc so sánh; `hasUnsavedChanges` đánh dấu cửa sổ có thay đổi và cho Unity hiện lựa chọn lưu/bỏ/hủy khi đóng. Chuyển level hoặc tải lại khi đang sửa sẽ hỏi **Bỏ thay đổi / Tiếp tục chỉnh**.
- Bản nháp và mốc so sánh được giữ qua tải lại script khi Unity khôi phục được đối tượng tạm. `OnDisable` gỡ theo dõi Undo; `OnDestroy` dọn bản nháp và lịch sử Undo của nó.
- `EnsureDraftEditable` bỏ cờ khóa ở bản nháp cũ sau tải lại script, đồng thời chuẩn hóa cờ trong mốc JSON để không báo thay đổi giả; không xóa dữ liệu đang chỉnh. Đã kiểm chứng `SerializedProperty.editable` của Hidden/Frozen và bản nháp trong cửa sổ đang mở; bật trạng thái không làm đổi nguồn.
- Các field ở Inspector chỉ để xem; dùng Level Editor để chỉnh và lưu. Tạo level mới vẫn tạo một asset trống ngay, các chỉnh sửa tiếp theo chỉ nằm trong bản nháp đến khi bấm Lưu.

### Toolbar
- ObjectField chọn LevelConfig; tự theo `Selection` khi chọn LevelConfig khác trong Project (`OnSelectionChange`).
- Điều hướng: `◀ Back` / ô số level / `Load` / `Next ▶`. Số level đọc từ đuôi tên asset (`LevelConfig_07` → 7, `ParseLevelNumber`). Không có level → hỏi tạo `LevelConfig_NN` trong `Assets/HotPot/Config` (`CreateLevelNumber`). Next ở level cuối → hỏi tạo level kế.
- `Tạo level mới` (SaveFilePanel, tên gợi ý theo số lượng level), `Tìm trong Project` (Ping asset nguồn), `Tải lại` (bỏ bản nháp và nạp nguồn), `Lưu` (`SaveDraft` → `AssetDatabase.SaveAssetIfDirty` cho đúng nguồn).

### Panel khách (trái)
- Hai hàng "Hàng trái" / "Hàng phải" cạnh nhau. Mỗi khách là thẻ: tiêu đề (Khách 1 = phục vụ trước), nút x, danh sách món, `+ Món` (disable khi đủ 4; cảnh báo nếu > 4).
- Mỗi món (`DrawDishRow`): ảnh món 48px (bấm → `FinalFoodPickerPopup` lưới ảnh), dropdown tên (`FinalFoodListPopup` có tìm kiếm), icon nguyên liệu công thức + số lượng.
- `+ Thêm khách` thêm khách với 1 món mặc định (`FinalFoodValues[1]`).

### Popup chọn món
- `FinalFoodListPopup`: ô search (tìm theo tên enum, displayName, tên nguyên liệu tiếng Việt), nhóm theo dòng món (`(int)type / 10`), mỗi dòng ảnh + tên + enum + icon nguyên liệu; Enter chọn kết quả đầu, Esc đóng; tự cuộn tới món hiện tại.
- `FinalFoodPickerPopup`: lưới hàng = dòng món, cột = số nguyên liệu (`FinalFoodConfig.MinIngredients..MaxIngredients`, 2–6); enum quy ước `line*10 + tier`; hover hiện tooltip tên + nguyên liệu.

### Panel board (phải)
- Ô `Số hàng` / `Số cột` → resize (Undo "Đổi kích thước bảng").
- Lưới cố định `DrawBoardGrid`: mỗi ô 180×180px. Đĩa ngang gộp hai ô trái/phải, đĩa dọc gộp hai ô trên/dưới; ô bị chiếm không vẽ riêng. `DrawCellArea` tô nền/viền cam cho đĩa dọc, xanh cho đĩa ngang.
- `DrawMainPanel`: icon kèm dấu ẩn/băng, dropdown `Kiểu đĩa`, loại nguyên liệu và số lượng phần 1, `Ẩn nguyên liệu`/`Ẩn cả đĩa`, `Đóng băng`, `Giá trị băng`. `DrawSecondPanel` hiện icon và loại/số lượng riêng của phần 2, dùng chung dấu ẩn/băng.
- Riêng đĩa dọc, `DrawVerticalDish` đặt dropdown kiểu đĩa ở đầu, tiếp theo là hai thẻ nguyên liệu cùng kích thước (`DrawVerticalPortion`): icon bên trái, nhãn PHẦN 1/PHẦN 2 và số lượng bên phải, dropdown nguyên liệu bên dưới. Footer `DrawVerticalSharedSettings` có đường phân cách và nhãn ÁP DỤNG CẢ ĐĨA, gom nắp/băng/giá trị băng dùng chung; dành sẵn chỗ để bật băng không làm bố cục nhảy.
- `ChangeDishShape` có Undo; đĩa đôi cần ô bên dưới/bên phải trong bảng và chưa thuộc đĩa khác. Nếu ô sẽ bị chiếm đang có dữ liệu, hỏi xác nhận trước khi xóa dữ liệu ô đó.
- `DrawBoardProblems` hiện cảnh báo tràn bảng, chồng vùng chiếm, ô bị chiếm còn dữ liệu riêng, thiếu nguyên liệu, số lượng không dương hoặc kiểu đĩa không hợp lệ; kèm hướng dẫn chọn/dồn đĩa ngang và Shuffle.
- `Điền bảng theo tổng order` (`FillBoardFromOrders`):
  1. Đếm nguyên liệu cần (`CountOrderedFoods`: mỗi món của mọi khách cộng 1 cho mỗi nguyên liệu trong công thức).
  2. `LevelBoardOrderFiller.TryFill` tính mỗi đĩa đôi là hai vị trí nguyên liệu, ô bị chiếm không tính thành đĩa riêng. Gom các nửa đĩa đôi trước, sau đó ô đơn theo thứ tự cột→hàng. Giữ kiểu đĩa và nắp/băng chung của đĩa đôi; thay lại cả nguyên liệu lẫn số lượng ở hai phần.
  3. Kiểm tra trước khi sửa: nếu số loại vượt số vị trí, tổng số phần nhỏ hơn số nửa đĩa đôi, hoặc vùng chiếm sai/chồng nhau → báo lỗi và giữ nguyên bảng. Mỗi nửa đĩa đôi bắt buộc có ít nhất một phần.
  4. `AllocatePortions`: mỗi loại một vị trí, vị trí dư chia cho loại có lượng/vị trí cao nhất (hòa → tổng lớn hơn → thứ tự enum); không loại nào nhiều vị trí hơn tổng lượng của nó. Chia đều lượng mỗi loại, phần dư vào vị trí đầu. Hai nửa có thể cùng loại.
  5. Ưu tiên điền đủ hai nửa mọi đĩa đôi, rồi điền ô đơn. Ô đơn thừa và dữ liệu riêng của ô bị chiếm được xóa. Đĩa đơn được điền sẽ reset nắp/băng như trước. Có Undo cho toàn bộ thao tác.
  - Toàn bộ thao tác chỉ sửa bản nháp; vẫn phải bấm **Lưu** để ghi asset. Không giữ lại nguyên liệu cũ của đĩa đôi để trừ nhu cầu nữa. Không tự xáo trộn hoặc sinh trạng thái ẩn/băng.
- `Xoá bảng`: mọi ô về đĩa đơn None, số lượng mặc định 1, không ẩn, không băng, xóa dữ liệu phần 2.

### Kiểm tra nguyên liệu
Bảng theo từng FoodType: Order cần | Bảng có | Trạng thái (Đủ xanh / Thiếu N đỏ / Dư N vàng). Số bảng có tính cả hai phần của đĩa đôi, kể cả khi cùng loại, và bỏ qua ô bị chiếm. Dòng tóm tắt tổng khách + kích thước bảng. Đếm theo chỉ số enum (an toàn với khoảng trống ID số).

## Dish Link — chuỗi A–B–C (2026-10-23)

- Trong phần board, mở **Dish Link · dây nối thẳng**, bấm **+ Nhóm 2 đĩa** hoặc **+ Nhóm 3 đĩa**. Nhập cột/hàng của A, B, C bằng số bắt đầu từ **1**. Nhóm ba đĩa chỉ có hai đoạn **A–B** và **B–C**, không nối thành tam giác.
- Dữ liệu `LevelConfig.dishLinks` chứa `DishLinkConfig.dishes`: danh sách tọa độ `(cột, hàng)` bắt đầu từ **0**, theo thứ tự nối. Tọa độ là vị trí ban đầu trước khi runtime dồn board. Chọn ô gốc của đĩa đôi, không chọn ô bị chiếm. Mỗi đĩa chỉ thuộc một nhóm.
- Dây vàng bện tròn có đổ khối, viền tối, phản sáng kem và móc vàng vát mép theo ảnh demo; được vẽ bằng `DishLinkView`; mỗi đoạn có đúng hai đầu, luôn thẳng dù khác cột/hàng hoặc có đĩa khác ở giữa. Dây đi qua không làm đĩa trung gian thành thành viên. Dây bám vị trí khi dồn; đĩa được dồn theo quy tắc vùng chiếm hiện có.
- Chỉ chạm thành viên nằm ở hàng đầu để lấy cả nhóm. Đĩa ở hàng sau không nhận chọn thường và không kích hoạt nháy từ chối. Chọn thường yêu cầu không có đĩa ngoài nhóm phía trước bất kỳ thành viên nào trong các cột nó chiếm. Các thành viên liên tiếp cùng cột được lấy cùng nhau; một đĩa ngoài nhóm nằm giữa chúng sẽ chặn nhóm.
- Chạm một thành viên ở hàng đầu khi nhóm bị chặn (độ sâu, nắp/băng hoặc thiếu slot) sẽ nháy viền đỏ trên mọi đĩa trong nhóm, kể cả đĩa sâu. Cập nhật selection mỗi khung hình giữ nguyên màu/hiển thị của lượt nháy; hết nháy trả về viền nghỉ hiện tại.
- Cần đủ slot cho tổng lượng của mọi đĩa, gồm cả hai phần của đĩa đôi. Giữ chỗ/điền nguyên nhóm trước khi gỡ; lỗi điền được hoàn tác. Một thao tác lấy nhóm chỉ giảm băng trên board **một lần**.
- Một thành viên còn băng sẽ chặn cả nhóm. Nắp tự mở khi lộ đầu cột; thành viên thấp cùng cột cũng mở khi cả nhóm đã lộ để tránh kẹt nắp. Đĩa thành viên ở hàng đầu vẫn sáng/hiện viền dù thành viên khác chưa sẵn sàng; các đĩa sâu bị chặn vẫn tối. Chỉ cần một thành viên ở hàng đầu thì toàn bộ dây và móc của nhóm sáng; không có thành viên đầu hàng thì dây tối. Độ sáng không thay đổi điều kiện lấy cả nhóm.
- PickupHand kiểm tra tổng chỗ và băng của cả nhóm, làm sáng/mở nắp cả nhóm, cho phép bỏ qua độ sâu. AutoComplete vẫn lấy từng phần; thành viên hết nguyên liệu được gỡ, hai thành viên còn lại nối trực tiếp, còn một thì bỏ dây. Shuffle bỏ qua mọi đĩa thuộc nhóm.
- **Điền bảng theo tổng order** giữ nhóm và bảo đảm có nguyên liệu ở mọi ô gốc được nối, ưu tiên cùng các nửa đĩa đôi trước ô đơn thường. Giữ nắp/băng của đĩa nối; nếu tổng order không đủ hoặc tọa độ sai, báo lỗi trước khi sửa dữ liệu. **Xóa bảng** cũng xóa các nhóm.
- Editor cảnh báo nhóm không đủ 2–3 đĩa, tọa độ ngoài bảng, ô bị chiếm, thành viên trống hoặc lặp/thuộc nhiều nhóm. Runtime bỏ qua nguyên nhóm sai và ghi cảnh báo.
- Các chỉnh sửa nhóm nằm trên **bản nháp**; chỉ ghi asset khi bấm **Lưu**. **Tải lại** phục hồi dữ liệu đã lưu. Không tự thêm nhóm vào level hiện có.
- Mã chính: `DishLinkConfig`, `DishLinkGroup`, `DishPickBatch`, `DishLinkView`, `FoodBoardManager.DishLinks`, `LevelConfig.DishLinks`, `LevelConfigEditorWindow.DishLinks`.
- Kiểm chứng: 36 kiểm tra trong Unity preview scene qua, gồm lấy nhóm ba đĩa, từ chối chạm thành viên hàng sau và lấy nhóm từ hàng đầu, blocker, nắp/băng, thiếu slot và rollback khi slot lỗi, PickupHand bỏ qua độ sâu, AutoComplete nối lại, replay, điền bảng và validation. Chưa kiểm tra hoạt cảnh trong Play Mode.

## Icon / tài nguyên
- Nguyên liệu: `FoodIconConfig.asset` trước, fallback `Art/Sprites/Foods/Sushi/Raw/<kebab-case>.png`. Cache trong `icons`.
- Món hoàn chỉnh: sprite trong `FinalFoodConfig.asset`, fallback `Art/Sprites/Foods/Sushi/Final/<tier>/<slug>-nigiri.png` (regex `^([A-Za-z]+)Nigiri(\d)$`). Cache `finalIcons`.
- `OnEnable` xoá cache (sau domain reload).
- `VietnameseNames` hard-code theo thứ tự khai báo enum `FoodType` — thêm FoodType mới phải cập nhật mảng này.

## Giới hạn hiện tại (để tham khảo khi làm ý tưởng mới)
- Board bắt buộc chữ nhật: mọi cột cùng `boardRows` (không có cột dài ngắn khác nhau).
- Không có preview giống in-game (vị trí đĩa thật, băng chuyền, số slot), không mô phỏng/kiểm tra level có giải được không (thứ tự đĩa vs thứ tự khách, slot băng chuyền).
- Không có kéo-thả/hoán đổi ô, copy/paste cột, xáo trộn board, hay random ẩn/băng.
- Validation kiểm tra tổng số lượng và cấu hình vùng chiếm đĩa đôi; chưa xét thứ tự phục vụ, khả năng giải hay độ khó.
- Runtime dùng số cột được scene hỗ trợ trong khoảng `FoodBoardManager.MinColumns..MaxColumns` (hiện 3–5); editor vẫn cho nhập kích thước ngoài khoảng này. Đĩa đôi vượt vùng cột thực tế hoặc không hợp lệ bị báo lỗi và bỏ qua khi nạp.
- Không có nút Play/test level trực tiếp từ editor.

## Đĩa đôi (dọc / ngang) — ĐÃ TRIỂN KHAI (2026-10-22)

Đĩa đôi chiếm 2 ô của board.
- **Dọc**: chiếm 2 hàng liên tiếp trong 1 cột. Chọn được khi nửa trên ở hàng 0. Chọn 1 lần → bay toàn bộ số lượng còn lại của cả hai phần, giải phóng hai hàng.
- **Ngang**: chiếm 2 cột kề nhau cùng hàng. Chọn được khi ở hàng 0 của CẢ 2 cột. Luật "cổng": chỉ trượt lên khi 2 ô phía trên đều trống → các đĩa bên dưới ở cả 2 cột bị kẹt tới khi gỡ đĩa ngang. Tạo liên kết giữa 2 cột (người chơi phải dọn đều).

Đã chốt (2026-10-22):
1. Nội dung: 2 nguyên liệu (có thể khác loại), mỗi phần có số lượng riêng.
2. Chạm 1 lần lấy hết cả 2 phần.
3. Băng chuyền không đủ slot cho cả 2 phần → chặn, viền nháy đỏ (như đĩa đơn).
4. Shuffle đổi nội dung nguyên liệu giữa các vị trí, hỗ trợ cả đĩa đôi và giữ vùng chiếm cố định; đĩa thuộc Dish Link được giữ nguyên.
Băng/nắp hiện dùng chung cho cả đĩa: một giá trị băng, một nắp; đĩa ngang chỉ tự mở nắp khi đứng đầu cả hai cột.

Data và editor hiện tại (giữ board chữ nhật):
- `OrderFood`: `DishShape dishShape` {Single, Vertical, Horizontal} + `secondFoodType`, `secondCount`. Ô gốc = ô trên (dọc) / ô trái (ngang); ô còn lại là ô "bị chiếm" (bỏ qua khi nạp).
- Editor: dropdown "Kiểu đĩa"; vẽ gộp hai ô thành một khung đĩa với hai panel, không vẽ riêng ô bị chiếm. Validate: ngang không ở cột cuối, dọc không ở hàng cuối, hai đĩa đôi không chồng nhau, ô bị chiếm không có dữ liệu riêng. Bảng kiểm tra cộng cả hai phần.
- "Điền bảng theo tổng order" giữ hình dạng và trạng thái chung của đĩa đôi, điền lại cả hai phần theo tổng order; ưu tiên đủ các nửa đĩa đôi rồi mới tới ô đơn. Không sửa dữ liệu nếu order không đủ để điền cả hai nửa. Đã qua 20 kiểm tra: tổng nguyên liệu chính xác, cùng loại ở hai nửa, giữ nắp/băng/bố cục, bỏ qua ô bị chiếm, Undo, từ chối không đổi dữ liệu và không tác động asset nguồn.

Runtime (`FoodBoardManager`, `Food`, `BoosterController`):
- `columnDishes` giữ entry giữ chỗ trỏ tới cùng `Food` cho ô bị chiếm.
- `FillBoard` nạp theo hàng rồi cột để giữ chỗ trước khi duyệt ô bị chiếm; `CreateBoardDish` cấu hình dữ liệu/hình ảnh. `Food.Shape`, `SecondFoodType`, `SecondAmount`, `TotalAmount` giữ dữ liệu runtime; `TakePortion` lấy đúng nửa còn nguyên liệu.
- `SettleBoard`: xét mọi ô trống; đĩa ngang chỉ di chuyển khi cả hai ô phía trên trống, dồn hai cột đồng bộ. Đĩa dọc giữ hai hàng liền nhau. Sau khi gỡ đĩa, chạy lại đến khi không còn đĩa di chuyển được.
- `TryPickAt`/selection: đĩa ngang selectable khi là đầu cột ở cả 2 cột.
- Slot băng chuyền cần bằng tổng số phần của cả hai nửa (không cố định hai slot). Kiểm tra đủ chỗ trước khi điền; lỗi điền slot được hoàn tác, đĩa vẫn nguyên trên board. Mỗi nửa bay từ icon riêng tới các slot đúng loại.
- Shuffle đổi nội dung đĩa đơn/đôi, giữ vùng chiếm và bỏ qua đĩa thuộc Dish Link. PickupHand lấy cả đĩa từ một trong hai ô. AutoComplete lấy từng phần từ cả hai nửa; giữ vùng chiếm đến khi toàn bộ đĩa hết nguyên liệu.
- Băng của một đĩa đôi chỉ giảm một lần mỗi lượt lấy đĩa. Replay trả mỗi đĩa về pool một lần, xóa các tham chiếu ô chiếm và trạng thái đĩa đôi trước khi dùng lại.
- Hình: đĩa đôi dùng sprite chữ nhật bo nhẹ riêng cho ngang/dọc trong `Assets/HotPot/Art/Sprites/UI/Dishes`, đồng bộ chất liệu đĩa đơn. Căn sprite đúng kích thước vùng chiếm hai ô; hai icon không bị kéo giãn và có số lượng riêng. Nửa hết nguyên liệu ẩn icon. Viền chọn dùng sprite vòng riêng; `PlateSilhouette` chỉ dự phòng.
- Hidden dùng nắp chữ nhật riêng `PlateLidHorizontal.png` / `PlateLidVertical.png`, tham chiếu màu bạc, cách đổ bóng và núm tròn của `PlateLid.png` gốc. `horizontalDoubleLid` / `verticalDoubleLid` đã gán trong scene và có đường dẫn mặc định ở `OnValidate`. `Food.ConfigureShapeLid` căn nắp chung phủ cả hai phần; hoạt cảnh nhấc/mờ giữ nguyên, pool khôi phục sprite/scale nắp tròn. Khi còn Frozen, lớp băng và giá trị băng vẫn hiển thị phía trên nắp.
- `FoodBoardManager` giữ `horizontalDoublePlate`, `verticalDoublePlate`, `doublePlateOutlineMaterial`; đã gán trong `HotPotDemo.unity`, `OnValidate` hỗ trợ gán mặc định. Pool khôi phục sprite/viền tròn gốc khi đĩa được dùng lại làm đĩa đơn. Xem `Assets/HotPot/Documentation/DoubleDishArt.md` để biết đường dẫn asset, prompt tạo ảnh và kiểm chứng render.
- Viền chọn đĩa đôi có sprite riêng `PlateSelectionOutlineHorizontal.png` / `PlateSelectionOutlineVertical.png` trong `Assets/HotPot/Art/Sprites/UI/Dishes`, nền và tâm trong suốt. `horizontalDoubleOutline` / `verticalDoubleOutline` đã gán trong scene và được truyền tới `DishSelectionOutline`; sprite dùng được cả viền trắng lẫn nháy đỏ. Ưu tiên vòng viền riêng, shader `PlateSilhouette` chỉ dự phòng. Có menu `Tools/FoodPop/Rebuild Double Dish Outlines` để tạo lại hình học viền.
- `Food.ConfigureShape` điều phối các helper khôi phục kích thước gốc, áp vùng chiếm và tạo hình phần 2 có thể tái sử dụng. `LateUpdate` đồng bộ độ sáng, độ mờ và thứ tự vẽ của phần 2 sau thay đổi từ board/booster.
- Kiểm chứng: build runtime/editor không lỗi; 30 kiểm tra hành vi trong preview scene độc lập, gồm cổng ngang, vùng chiếm dọc, tổng nguyên liệu, sức chứa, băng dùng chung, lấy từng phần và tái sử dụng pool. Chưa kiểm tra hoạt cảnh trong Play Mode.




