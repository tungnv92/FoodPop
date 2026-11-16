# Level Studio Web

Cập nhật: 29/10/2026. Source: `Web/LevelStudio`; frontend trong `public/studio/`, API dùng Sites authentication và D1 (`DB`). `dist/` là đầu ra build, không sửa trực tiếp. Hosting identity nằm ở `.openai/hosting.json`.

Web: https://foodpop-level-studio.basicberry10.chatgpt.site/studio/

## Chỉnh level và đồng bộ

1. Nhập `.asset` Unity text hoặc chỉnh level đã tải từ server. Khi mở trang, tự lấy các level server và đồng bộ theo tên; bản nháp bị thay thế được sao lưu trước.
2. Tab **Board & nguyên liệu** chỉnh từng ô, cọ tô, đĩa đôi, ẩn, băng, link và đối chiếu nguyên liệu. Tab **Order khách** chỉnh một danh sách khách chung.
3. Mỗi tên level (trim và chuẩn hóa NFC) là một chuỗi phiên bản. **Lưu** ghi server và lịch sử theo tài khoản được máy chủ xác thực; không dùng Ctrl+S. Bản nháp vẫn tự giữ trên máy.
4. **Level dùng chung** chỉ hiện bản mới nhất mỗi level; **Đồng bộ** thay bản local bằng bản server cùng tên. **Lịch sử** đọc các bản lưu server và cho khôi phục phiên bản.
5. Xuất `.asset` rồi thay dữ liệu trong Unity, giữ `.meta` để bảo toàn GUID. Web không ghi trực tiếp vào project. Có thể tải/khôi phục workspace JSON để sao lưu thủ công.

Site dùng danh sách tài khoản được cấp quyền truy cập, không mở công khai. Người được cấp quyền và đăng nhập có thể đọc/lưu các level dùng chung. Quyền xem site và quyền source là hai phạm vi riêng.

## Order nguyên liệu và loại khách

- `customerQueue` và `useUnifiedCustomerQueue` được giữ nguyên khi xuất `.asset`; Unity và web cùng lấy lần lượt cho hai chỗ phục vụ. Không chia trước thành hàng lẻ/chẵn. Level cũ được đọc xen kẽ trái/phải nếu chưa dùng hàng chung.
- `ingredientOrders[].ingredients` cùng `useIngredientOrders` là dữ liệu chính. Mỗi khách tối đa 4 nhóm, mỗi nhóm chơi được có 1–6 FoodType. Mục đầu là đế, mục lặp giữ số lượng.
- `finalFoods` chỉ là gợi ý tên/ảnh hoặc dữ liệu tương thích cũ. Tổ hợp chưa có món có tên vẫn chơi và xuất được. Thêm khách/nhóm không chọn sẵn nguyên liệu.
- `customerType`: Normal = 0, Boss = 1, Shipper = 2. Khách cũ mặc định Normal.
- Với Boss/Shipper, hiện **Thời gian phục vụ (s)** tương ứng `timeServe`, mặc định 30. Giá trị phải hữu hạn và lớn hơn 0; ô nhập trả giá trị không hợp lệ về 30, API từ chối thời gian không hợp lệ.
- Normal ẩn thời gian; đổi lại Boss/Shipper giữ giá trị đã nhập. Khách đặc biệt thiếu trường này dùng 30 giây. Lưu server và xuất/nhập `.asset` giữ thời gian tùy chỉnh.
- Unity `.asset` đóng gói cả `finalFoods` và `ingredients` theo little-endian. Cờ ưu tiên cho phép danh sách chung/nhóm nguyên liệu rỗng không rơi về dữ liệu cũ.

Boss/Shipper hiện đếm ngược `timeServe` khi đang phục vụ; hết giờ dừng demo và cho hồi sinh thời gian hoặc chơi lại. Normal không tính giờ. Pause giữ nguyên thời gian. Hồi sinh demo không mua Ads/Coin.

## Chơi thử

**Chơi thử** chạy JavaScript/Canvas riêng, không dùng Unity WebGL và không sửa level đang chỉnh. Băng chuyền có **10 slot** cách đều, tốc độ cố định theo thiết lập thời gian vòng tính bằng giây. Nút setting cạnh pause mở thông số băng chuyền và thời gian action đầu bếp. Chef dùng Spine hiện tại trong Unity, action một lượt theo mapping 25 nguyên liệu; thời lượng mặc định lấy từ animation, Idle1 chuyển Idle2 sau 1,5 giây, mix 0. Không còn cooldown 0,2 giây do engine web cũ tự thêm.

Chỉ lấy đĩa đầu cột; đĩa đôi và cả nhóm link cần đủ slot trước khi lấy. Đĩa còn lại dồn lên, băng giảm theo lượt, nắp mở khi lộ. Nguyên liệu được sơ chế, đổi ảnh prepared và có dấu tích trên băng. Tự phục vụ hai khách từ hàng chung, ưu tiên khách đến trước và đế trước. Dây link trong demo nối thẳng và bám vị trí đĩa khi dồn.

Đây là bản mô phỏng để thử level, không phải engine Unity; chưa mô phỏng đầy đủ booster, vật lý, âm thanh và particle. Danh mục/ảnh là snapshot từ Unity; thêm nguyên liệu hoặc đổi runtime cần cập nhật source và build lại web.

Luồng sơ chế đồng bộ Conveyor Chef: đĩa nguồn vẫn có món và giữ chỗ khi nguyên liệu ở đầu bếp. Vòng tròn tiến độ quanh đĩa gốc đầy dần trong lúc sơ chế, giữ đầy khi món bay về và tắt khi đáp. Món luôn quay về đúng đĩa nguồn; không tìm đĩa khác hay chờ slot trống. Pause dừng mô phỏng và tiến độ; chơi lại/đóng demo xóa trạng thái lượt sơ chế.

## Chạy và kiểm tra

- Local: chạy `npm run dev -- --hostname 127.0.0.1 --port 4173` trong `Web/LevelStudio`; áp dụng migration D1 local trước khi thử API. Đăng nhập phát triển chỉ có ở loopback, không có trên production.
- `node verify.cjs`: xuất/nhập các asset mẫu, enum và cấu trúc board.
- `node verify-play.cjs`: lấy đĩa, dồn, link, băng, phục vụ và giới hạn 10 slot.
- `node verify-ingredient-orders.cjs`: nhóm nguyên liệu tùy chọn, ba loại khách, thời gian mặc định/tùy chỉnh, round-trip và validation.
- `node verify-shared.mjs`: API lưu/lịch sử, cần server local và DB đã migration.
- `node node_modules/typescript/bin/tsc --noEmit`: kiểm tra kiểu API/frontend framework.

Các kiểm tra dữ liệu Unity đã xác nhận mặc định 30 giây và giữ `timeServe` sau serialization. Kiểm tra giao diện web đã xác nhận Boss/Shipper hiện thời gian, Normal ẩn và đổi loại không làm mất giá trị. Bản triển khai gần nhất sau thay đổi này là **20**. Chi tiết runtime xem `claude/CustomerManager.md`, UI Unity xem `claude/LevelEditor.md`.


## Chẩn đoán preview local (02/11/2026)

- `ERR_CONNECTION_REFUSED` tại `127.0.0.1:4199` nghĩa là chưa có server lắng nghe cổng đó, chưa phải lỗi validation của level.
- Server tĩnh chỉ phục vụ `public/`, không cung cấp `/api/shared`; lỗi JSON khi phản hồi là HTML/404 làm đồng bộ server thất bại. Workspace trình duyệt tại một origin local có thể chỉ có các level mẫu; cần kiểm tra tên/số level trong thư viện trước khi kết luận lỗi dữ liệu.
- `PlayDemo.open` chặn lỗi cấu trúc từ `Core.analyze` hoặc danh sách khách rỗng. Mất cân bằng số phần riêng lẻ không trực tiếp chặn mở demo trong implementation hiện tại.
- Kiểm tra trực tiếp bản deployed `/studio/` ngày 02/11/2026 đã tái hiện lỗi Chơi thử của LevelConfig_22: toast `Cần sửa level trước khi chơi: Nhóm chưa chọn nguyên liệu.` Khách 8 có nhóm món thứ 4 rỗng (`Chọn nguyên liệu (0)`); ba nhóm trước đã có nguyên liệu. Bảng đối chiếu cho thấy mọi loại nguyên liệu hiện tại đều khớp số phần giữa order và board. Cần bỏ nhóm trống nếu thêm nhầm, hoặc chọn nguyên liệu cho nhóm đó rồi cập nhật board tương ứng. Phiên chẩn đoán chưa sửa dữ liệu level hay lưu server.

## Nhập level 1–5 từ server (03/11/2026)

- Đã đồng bộ từng bản mới nhất qua Level dùng chung và xuất `.asset`: LevelConfig_01 v4, LevelConfig_02 v1, LevelConfig_03 v3, LevelConfig_04 v2, LevelConfig_05 v5.
- Dữ liệu được đưa vào `Assets/HotPot/Config/LevelConfig_01.asset` đến `LevelConfig_05.asset`; giữ nguyên các file `.meta`, `nextLevel` và `levelNumber` của project. (06/11/2026: các level đã chuyển sang `Assets/HotPot/Config/Resources/Levels`, field `nextLevel` đã bỏ; khi xuất lại, ghi vào thư mục mới và đặt `levelNumber` khớp số trong tên file.)
- Order cũ được chuyển bằng công thức của Web Studio thành `customerQueue`/`ingredientOrders`, xen kẽ trái/phải theo quy tắc tương thích. Không giữ các trường khách đã bỏ khỏi Unity (`finalFoods`, `useIngredientOrders`); `dish` chưa có tham chiếu dùng fileID 0.
- Kiểm tra dữ liệu: số khách lần lượt 4, 5, 7, 9, 12; board lần lượt 3×5, 3×7, 3×7, 4×6, 3×14. `Core.analyze` không báo lỗi cấu trúc ở cả năm level; đã kiểm tra đọc lại YAML và giữ nguyên board sau chuyển đổi. Chưa chạy chơi thử trong Unity.

## Nhập level 16–25 và cân bằng Level 17 (11/11/2026)

### Bổ sung biến thể Easy và độ khó Unity

- Đã xuất riêng ba bản server `LevelConfig_15_Easy`, `LevelConfig_20_Easy`, `LevelConfig_25_Easy` về `Config/Resources/Levels`, giữ số màn 15/20/25, thứ tự khách, board và các trạng thái. Số khách: 10/13/14; kích thước: 5×10/5×13/5×14. Cả ba cân bằng nguyên liệu, các nhóm tìm được prefab hình món hiện có.
- Unity có trường `difficultyMode` (`Normal=0`, `Hard=1`, `Easy=2`); ba bản gốc được đánh dấu Hard, các biến thể là Easy. Đây là cấu hình Unity bổ sung sau nhập, không phải công thức số level được hardcode khi chơi. Mặc định level khác là Normal; đặt lại độ khó trong Level Editor khi nhập lại dữ liệu web cũ chưa có field này.
- LevelController đọc `RecordTrackingManager.GetRetryCount` khi nạp: bản Hard chuyển sang `_Easy` từ retry thứ hai nếu có asset; nếu thiếu thì tiếp tục dùng bản Hard gốc. Bản Easy giữ nguyên số level, dùng chung thống kê của màn gốc. 55 kiểm tra cấu hình/asset Edit Mode qua; chưa xác nhận luồng retry trong Play Mode.

- Đã tải dữ liệu đồng bộ server bằng trình duyệt tích hợp, lấy đúng tên `LevelConfig_16` đến `LevelConfig_25`, không lấy các bản `_Easy` và không dùng số thứ tự của thư viện làm số level.
- Nhập vào `Assets/HotPot/Config/Resources/Levels`, đặt `levelNumber` bằng số trong tên. Giữ `.meta` có sẵn, tạo GUID cho asset mới. Chuyển order sang `customerQueue/ingredientOrders`, giữ thứ tự khách, nguyên liệu đế, lượng nguyên liệu, loại khách và `timeServe`; bỏ field khách cũ không còn dùng trong Unity.
- Chỉ Level 17 ban đầu lệch nguyên liệu: Đùi gà cần 4/có 11, Gà viên cần 7/có 0. Theo yêu cầu cân bằng và dễ chơi, đã đổi đúng 7 phần từ Đùi gà sang Gà viên tại (cột,hàng) bắt đầu từ 1: `(2,1) ×1`, `(4,9) ×1`, `(2,10) ×1`, `(3,11) ×1`, `(2,13) ×2`, `(1,14) ×1`. Giữ order và số phần từng ô; Gà viên ở đầu cột phục vụ khách 2 sớm, 4 phần Đùi gà còn lại dành cho khách 1, 2, 3 và 5. Đã lưu Web Studio **LevelConfig_17 v10** rồi xuất lại bản cuối về Unity.
- Kiểm tra cả 10 asset sau khi Unity nhập: cấu trúc hợp lệ, mọi FoodType có tổng board bằng tổng order, đúng `levelNumber`; số khách lần lượt **11, 14, 14, 12, 15, 12, 15, 18, 12, 16**. Board/order giữ nguyên sau chuyển đổi, ngoại trừ điều chỉnh Level 17 nêu trên.
- Chạy lại đường đi thắng trên dữ liệu Level 17 đã nhập bằng `LevelSimulation` của Web Studio: **14/14 khách, 56 lượt lấy, tối đa 6/10 slot**, kết thúc không còn đĩa trên board hoặc nguyên liệu trên băng. Tôn trọng đầu cột, nguyên liệu đế trước và hai chỗ phục vụ; không dùng booster. Đây là đường đi có chờ hoàn tất sơ chế/phục vụ giữa lượt, không bảo đảm mọi lựa chọn ngẫu nhiên hoặc nhấn liên tục đều thắng. Chưa kiểm tra gameplay/timer trực tiếp trong Unity.
- Mô phỏng cũng tìm được đường thắng cho Level 18, 19, 21, 22 và 24. Chưa tìm được đường thắng cho 16, 20, 23 và 25 trong lượt dò giới hạn đã chạy; điều này không chứng minh các level đó vô nghiệm. Không tự đổi bố cục các level còn lại.
- Đã đối chiếu prefab `FinalFoodView` thực tế: gán được **152 nhóm**, **165 nhóm** chưa có hình món khớp trong project và giữ `dish: {fileID: 0}`. Các nhóm vẫn giữ công thức và chạy logic phục vụ, nhưng không hiện hình món trên thẻ/khay theo contract hiện có; không tự tạo prefab hoặc gán hình sai nguyên liệu.

## Level 29 đầy board, mức dễ (12/11/2026)

- Đã lấy LevelConfig_29 v11 trên server và lưu bản chỉnh thành **v12**. Chỉ thay `ingredientBoard` và `dishLinks`; giữ order 14 khách, 33 nhóm và 101 phần nguyên liệu. Không cập nhật asset Unity trong lần chỉnh này.
- Board 4×20 có đủ 80/80 ô. Nguyên liệu được xếp theo tiến trình order, gom một số phần cùng loại trong phạm vi khách hiện tại/kế tiếp để giảm dồn băng; 8 ô hidden ở phần giữa/sau, không có băng. Có đúng 4 cặp nối giữa cột 1–2 ở hàng 3, 7, 11, 15 (đếm từ 1).
- Kiểm tra: Core.analyze không báo lỗi cấu trúc và tổng từng nguyên liệu khớp order. 10 lượt mô phỏng theo nhu cầu hai khách hiện tại, lựa chọn có nhiễu và mỗi 16 giây xét lấy một đĩa, đều thắng 14/14 khách, 76 lượt chọn. Đây là mô phỏng có chờ phục vụ, không phải số liệu win rate người thật; không khẳng định nhấn liên tục hay chọn ngẫu nhiên sẽ thắng.
- Đã xác nhận toast lưu server v12 và mở được Chơi thử trên web; chưa chơi hết màn bằng thao tác trực tiếp trong trình duyệt. Kết quả và ảnh board nằm tại `output/level29`.
## Level 30 khó theo cách Level 25 (12/11/2026)

- Lấy bản server LevelConfig_30 v12, giữ 14 khách, 35 nhóm món và đúng thứ tự nguyên liệu order. Bản ban đầu thiếu 1 cơm, dư 1 nước tương và 1 wasabi; board mới khớp hoàn toàn 115 phần order.
- Đã lưu cấu hình cuối trên Web Studio **v14**: board 5×16, đầy 80/80 ô, 25 hidden, 10 cặp nối ngang/chéo giữa nhiều cột và độ sâu khác nhau, không đóng băng. Hai khách đầu có nguyên liệu dễ tiếp cận; phần sau trộn nguyên liệu và gom phần để tăng áp lực 10 slot. Không cập nhật asset Unity trong tác vụ này.
- Đánh giá bằng LevelSimulation hiện tại: 100 lượt chọn theo nhu cầu hai khách đang phục vụ, có nhiễu và nhịp bấm 1–3 giây; thắng 23/100, cả 100 lượt hoàn tất hai khách đầu. 77 lượt thua dừng khi đã phục vụ 6 khách (40 lượt), 8 (1), 10 (2), 11 (34). Đây là kết quả chiến thuật mô phỏng, không phải win rate người thật; heuristic không tương đương mọi lựa chọn của người chơi.
- Đã chạy lại thành công một đường thắng đủ 14 khách: 70 lượt chọn, đỉnh băng 10/10 slot, không booster. Core.analyze không có lỗi cấu trúc hay lệch tổng từng nguyên liệu. Đã xác nhận demo mở được và server báo lưu v14; chưa chơi hết màn trực tiếp trong UI hoặc trong Unity.
- Script thiết kế, báo cáo có đường thắng và ảnh xác nhận nằm ở `output/level30`.

## Đồng bộ gameplay Unity local lên web (12/11/2026)

- Đối chiếu ChefManager, DishPickPlan, DishProgress, CustomerManager, ChefAnims và scene HotPotDemo hiện tại. Không sửa dữ liệu level server hoặc runtime Unity.
- Sơ chế: nguyên liệu thô đã đáp ở mọi vị trí trên băng đều được xét; ưu tiên khách tới trước, thứ tự món, công thức/đế rồi vị trí gần trạm. Một phần từ lượt lấy có thể bay trực tiếp tới đầu bếp khi ưu tiên cao hơn món đã đáp trên băng; ngang ưu tiên thì món trên băng đi trước. Slot nguồn vẫn giữ chỗ tới khi món về.
- Sức chứa lấy cả nhóm: N<=S đi băng thường; N=S+1 chỉ được nếu đầu bếp rảnh và order nhận được đúng một phần dư; N>S+1 từ chối. Phần dư giữ nhu cầu ngay từ đầu, đi thẳng đầu bếp→order, không tạo slot ảo. Đĩa đôi và mọi thành viên nối đều tính đủ phần. Luật đế trước và mỗi món chỉ có một lượt incoming vẫn áp dụng.
- Món prepared đáp về đĩa chờ 0,3 giây theo giá trị CustomerManager trong scene rồi phục vụ từ mọi vị trí, bỏ vùng exit cũ. Không nhầm với mặc định code 0,2 giây.
- Snapshot animation tại public/studio/spine/chef-v3 đồng bộ Chef.json, atlas, texture local; catalog.gameplay giữ mapping và thời lượng 25 nguyên liệu. Setting action=0 dùng animation, giá trị >0 dùng để thử thời lượng riêng; thiết lập v2 tránh mang mặc định 1,5 giây cũ thành override ngầm. Bay vào/ra và poof có pha riêng, giữ pause và replay.
- Boss/Shipper có thời gian còn lại, hết giờ đóng băng mô phỏng; Hồi sinh thời gian reset riêng khách hết giờ, giữ board và order. Đây là hồi sinh kiểm thử miễn phí, không mô phỏng giao dịch Ads/Coin.
- Kiểm tra đã chạy: verify.cjs, verify-ingredient-orders.cjs, verify-play.cjs, verify-runtime-sync.cjs (264 tổ hợp biên sức chứa và kiểm tra hành vi giữ order, đế lặp, ưu tiên, thời gian đáp, timer/hồi sinh), tsc --noEmit; preview trình duyệt nạp Spine mới và sơ chế/phục vụ không lỗi JS. Preview tĩnh không có API shared, không dùng nó để kiểm chứng lưu server.
- Giới hạn còn lại: hình học đường băng và thời gian bay chỉ mô phỏng, chưa xuất toàn bộ tọa độ world Unity; không tái tạo mua booster, hiệu ứng/âm thanh, popup economy hoặc hoạt cảnh ghép từng prefab món. Kết quả đánh giá độ khó Level 29/30 từ engine cũ ở các mục trước là lịch sử, phải chạy lại trước khi dùng với engine mới.

- Triển khai thành công Sites phiên bản **24**, commit d8734a6859bd08e4408406548944e649b19599f3. URL production do Sites trả về: https://foodpop-level-studio.nguyentung92bg.chatgpt.site/studio/ . Đã đăng nhập, đồng bộ 33 level, mở Level 30, xác nhận setting action theo animation và lấy đĩa trên bản production; không có lỗi JavaScript trong lần kiểm tra. Không ghi lại level server.

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
`nTriển khai Sites v25 thành công, commit b7643f7821a731808966d7b5a61b5cce726dfa63. Production đã tải 33 level và mở demo Level30 với resource mới, các nút booster và túi; không thấy lỗi JavaScript trong lần kiểm tra. Không lưu hoặc thay đổi level server.

LevelConfig_30_Easy server v3 (12/11/2026): tách 8 phần từ ô nhiều lượng sang 8 ô trống, đầy 80/80 ô board 5x16. Giữ 105 phần khớp từng loại với 14 khách/30 order, 24 hidden và 10 nhóm nối. Kiểm tra bằng UI cân bằng và cấu trúc; đã xác nhận lưu web v3, chưa chơi thử đo độ khó.

LevelConfig_30_Easy server v4: đổi 4 cặp nối xa ở hàng 6–14 thành cặp kề ngang/chéo. Cả 10 nhóm đều lệch tối đa 1 cột và 1 hàng. Giữ nguyên 80 ô, 105 phần, 14 khách, 24 hidden; UI xác nhận lưu web v4. Không đo lại độ khó.

### LevelConfig_30_Easy v5: giữ nhận diện Hard v20 (12/11/2026)

- Đã lưu qua UI production **v5**. Lấy board Hard v20 làm nền, không dùng bản thử xếp toàn board theo order. Giữ cùng loại nguyên liệu tại **76/80 vị trí (95%)** so với Hard; không tráo vị trí hàng loạt. Bốn ô đổi loại và một số ô giảm/chia số phần để khớp tổng order Easy. Board vẫn 5×16, đầy 80 ô, không có băng.
- Giảm hidden từ 32 xuống 24 bằng cách mở tám nắp ở đoạn đầu; giảm nhóm nối từ 14 xuống 9 bằng cách gỡ bốn cặp xa và cặp cơm/bơ tại điểm kẹt sau 6 khách. Chín cặp còn lại giữ đúng vị trí của Hard, đều kề ngang/chéo; không tạo liên kết xa mới.
- Giữ nguyên hoàn toàn `customerQueue` của Easy v4: **14 khách/30 món/105 phần**, so với Hard 14 khách/35 món/115 phần. Không sửa thêm order trong lần chỉnh này; người dùng xác nhận không cần thay đổi thêm phần khách hàng. Hard v20 và asset Unity không bị sửa.
- Core.analyze không có lỗi cấu trúc, tổng từng nguyên liệu khớp order. File xuất từ UI `LevelConfig_30_Easy (1).asset` khớp chính xác board, hidden, links và order của `output/level30-easy/candidate.json`; `verify-applied.cjs` PASS và xác nhận 76/80 vị trí nguyên liệu trùng Hard.
- Kiểm chứng bằng hai chính sách có chủ đích trong `bot.md`, chỉ quan sát 3 hàng, không biết hidden chưa mở, không đọc khách tương lai, không booster/revive: bình thường **100/100 thắng**, cẩn thận **100/100 thắng** trên seed 1337200 + user×31, tách bộ dò 50 lượt/nhóm seed 1211500 + user×31. Cả 200 lượt qua hai khách đầu, đều 71 lượt lấy; thời gian thực trung bình thắng lần lượt **321,5 giây / 352,7 giây**, chung 337,1 giây. Không có lượt thua nên thời gian thua và phân bố kẹt không áp dụng. Đỉnh băng vẫn có thể đạt 10/10 slot.
- Đây là kiểm tra hai chiến thuật với nhịp bấm khác nhau, không phải 200 người chơi độc lập hay cam kết winrate người thật 100%. Bản Easy v4 trước chỉnh: chính sách bình thường thua sau 3 khách, cẩn thận thắng; bản mới loại được điểm kẹt này trong bộ thử. Chưa chơi hết bằng thao tác UI hoặc Unity Play Mode. Script, trace và ảnh lưu v5 nằm tại `output/level30-easy`; các báo cáo xếp lại board theo order ban đầu đã bị thay thế, không dùng làm kết quả v5.
- Quy tắc cho mọi cặp Hard/Easy về sau đã ghi trong [bot.md](bot.md): Easy giữ nhận diện bố cục Hard và kinh nghiệm chơi lại, chỉ giảm các yếu tố gây khó.

Đánh giá lại Level30 Hard 12/11/2026 từ file xuất trực tiếp web: 14 khách, 35 order, 115 phần, 80 ô. Dùng engine web hiện tại, không sửa level; 3 nhóm x100 seed, chỉ xét order hai khách hiện tại và nguyên liệu lộ, không đọc hàng ẩn/khách tương lai, không booster/hồi sinh. Vội 27/100, bình thường 64/100, cẩn thận 83/100; cả300 qua hai khách đầu. Nhóm bình thường thua5 lượt sau6 khách,31 lượt sau11 khách. Đây là tỉ lệ của chính sách mô phỏng, chưa phải telemetry người thật. Chi tiết, thông số hành vi và trace: output/level30/evaluate-current.cjs và current-evaluation.json. Đã chạy verify-unity-parity.cjs (528 trường hợp Unity) và verify-play.cjs PASS. Không dùng số23% từ engine/bố cục cũ để đại diện hiện tại.

## Level 30 Hard: đánh giá có chủ đích và học lại (12/11/2026, server v20)

Quy tắc bắt buộc cho các lần đánh giá tiếp theo được tập hợp tại [bot.md](bot.md), gồm giới hạn 3 hàng, hidden, cách học từ thất bại và cách báo cáo hai nhóm bình thường/cẩn thận.

- Đã lưu `LevelConfig_30` **v20** qua UI production, thay bản v19 trung gian. Chỉ đổi vị trí/loại và số phần của các ô; giữ order 14 khách/35 món/115 phần, board 5×16 đủ 80 ô, tổng từng FoodType cân bằng. Giữ hidden và nhóm nối theo dữ liệu gốc xuất server; không thay `LevelConfig_30_Easy` hay asset Unity. File xuất UI `LevelConfig_30 (5).asset` được so sánh chính xác `ingredientBoard`, `dishLinks`, `customerQueue` với `output/level30/two-profile-result.json` bằng `verify-two-applied.cjs` (PASS); Core.analyze không có lỗi và mọi delta bằng 0.
- Tiêu chí người dùng cập nhật: bỏ nhóm vội, tính đều nhóm bình thường/cẩn thận; điểm bắt đầu thua mong muốn quanh khách 6 thay vì khách 8. Chỉ quan sát **3 hàng đầu**, không biết nguyên liệu hidden chưa từng mở, không dùng thông tin nguyên liệu hàng sâu hoặc khách chưa đến. Lượt chơi lại chỉ giữ nhận biết nguyên liệu đã thực sự lộ trong 3 hàng đầu ở lượt trước. Giữ Hard để đo học lại; không kích hoạt cơ chế chuyển Easy khi retry của gameplay thực tế.
- Các tỉ lệ trước đây dùng hành động lỗi ngẫu nhiên không còn là kết luận chính theo yêu cầu mới. Trên ứng viên v20, bộ seed độc lập 9635200–9635299 từng cho bình thường 52/100, cẩn thận 43/100, chung 47,5%; **không dùng con số này để xác nhận mục tiêu sau khi người dùng yêu cầu không chơi bừa**. Phép thử ghi nhớ chỉ trừ điểm các nước lấy dư (`learning-evaluation.json`) cũng bị thay thế bởi phân tích đường mở bên dưới.

### Phép thử có chủ đích, giới hạn tầm nhìn

`output/level30/evaluate-deliberate.cjs` và `three-row-deliberate-evaluation.json`: 50 chuỗi mỗi chiến thuật, thay nhịp phản ứng theo seed, không tạo lỗi chọn ngẫu nhiên, không bỏ sót số phần đang giữ, không booster/revive. Đây là hai chính sách xác định với nhịp bấm khác nhau, **không phải 100 người chơi độc lập và chưa phải mô hình đã hiệu chuẩn với telemetry**.

- Chính sách bình thường ban đầu ưu tiên nhu cầu hiện tại, nguyên liệu đế và tránh phần dư; sau khi thực sự thua áp dụng tính đường mở nhóm chắn. Chính sách cẩn thận tính đường mở từ lượt đầu. Chỉ lập kế hoạch tới mục tiêu trong 3 hàng đầu; mọi thành viên của đường xét phải nằm trong 3 hàng đó. Với hidden, chỉ dùng loại nếu đã quan sát trực tiếp trong lượt trước. Không đọc order tương lai.
- Cách tính đường: tìm nguyên liệu đang thiếu, cộng toàn bộ phần của nhóm nối và các đĩa chắn phía trên, trừ phần có thể đáp ứng nhu cầu hiện tại; ưu tiên đường ít phần dư hơn. Khi thiếu nguyên liệu mà không còn việc sơ chế/phục vụ hữu ích, chỉ mở nhóm thuộc đường đã chọn thay vì mở cột không liên quan.
- Chẩn đoán cụ thể v20: sau 6 khách, hai khách hiện tại còn thiếu **2 phần bơ**. Nước mở các cột khác mang nguyên liệu chưa cần lên băng, sau đó lấy 3 cơm làm chỉ còn 2 slot; nhóm nối chứa 2 bơ + 2 cơm cần 4 slot nên bị kẹt. Lượt chơi lại mở đúng đường tới nhóm bơ và dành chỗ cho toàn nhóm, thay vì tiếp tục mở cột khác. Đây là lỗi kế hoạch có đường sửa, không phải bằng chứng level vô nghiệm.

| Chính sách | Thắng lượt đầu | Thời gian thắng TB | Thời gian thua TB | Lượt bấm kẹt TB / phổ biến | Khách đã phục vụ khi kẹt |
| --- | --- | --- | --- | --- | --- |
| Bình thường theo nhu cầu trước mắt | 0/50 | — | 164,8 giây | 36 / 36 | 6 (50 lượt) |
| Cẩn thận tính đường mở | 50/50 | 345,0 giây | — | — | — |

- Cả 100 lượt đầu đều qua hai khách đầu. Thời gian trung bình chung lượt đầu 254,9 giây; tách thắng 345,0 giây và thua 164,8 giây. Toàn bộ lượt thua dừng ở `ReviveOffered`, không phải timeout runner. Không có lượt thua sau 7–10 khách trong bộ thử này; sớm nhất là **đã phục vụ 6 khách**, không phải đang phục vụ khách thứ 6.
- Nhóm bình thường chơi lại sau phân tích: **50/50 thắng ở lượt hai**, trung bình 321,1 giây, 66 lượt lấy; chuỗi đầu tiêu biểu từ thua lượt 36 sau 6 khách sang thắng 14/14 khách. Đây là kết quả khi giả định người chơi áp dụng được bài học tính đường mở, **không phải tỉ lệ người thật tự rút ra bài học 100%**. Chính sách được đổi quy tắc sau thất bại; chưa đo xác suất người thật nhận ra/ap dụng quy tắc hay so sánh nhóm người thật không học.
- Nếu tính đều hai chính sách, lượt đầu là 50/100; kết quả quá phân cực (0%/100%) và gần như trùng đường đi trong từng nhóm. **Chưa xác nhận mục tiêu winrate người chơi bình thường/cẩn thận 40–50%**. Không dùng số lượt đổi nhịp bấm để tạo cảm giác có mẫu hành vi đa dạng. Cần quan sát người chơi hoặc một mô hình hành vi được hiệu chuẩn để suy ra tỉ lệ quần thể.
- Đã xác nhận UI lưu v20, cân bằng và xuất khớp ứng viên. Chỉ kiểm chứng bằng engine Web Studio/runner; chưa chơi hết trực tiếp bằng thao tác UI, chưa kiểm thử Play Mode Unity và chưa đo telemetry người thật. Các kết quả không giới hạn tầm nhìn (`deliberate-evaluation.json`) đã bị loại; không sử dụng làm kết luận.

### Đồng bộ Level30 Hard/Easy về Unity local (12/11/2026)

- Theo yêu cầu đồng bộ sau chỉnh web, đã cập nhật `Config/Resources/Levels/LevelConfig_30.asset` theo Hard **v20** và `LevelConfig_30_Easy.asset` theo Easy **v5**, từ đúng hai file xuất UI đã đối chiếu với ứng viên được lưu.
- Thay `ingredientBoard` và `dishLinks`; order local đã khớp nội dung gameplay với web nên giữ nguyên serialization và tham chiếu prefab món. Giữ `levelNumber: 30`, Hard `difficultyMode: 1`, Easy `difficultyMode: 2`; không sửa `.meta`/GUID hoặc runtime.
- `output/level30-easy/sync-local.cjs` đã đọc lại và xác nhận board/hidden/số phần, nhóm nối và nội dung order khớp web ở cả hai bản; tham chiếu prefab món giữ nguyên, SHA-256 của `.meta` không đổi. Core.analyze không có lỗi cấu trúc và mọi nguyên liệu cân bằng: Hard 14 khách/35 món/115 phần/14 nhóm nối; Easy 14 khách/30 món/105 phần/9 nhóm nối.
- Đây là kiểm tra file dữ liệu local; chưa chạy Unity Play Mode sau đồng bộ.
