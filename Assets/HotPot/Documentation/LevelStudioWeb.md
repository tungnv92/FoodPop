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

**Giới hạn:** `timeServe` hiện chỉ là cấu hình và metadata trong snapshot demo. Chưa có thanh đếm ngược, khách bỏ đi hoặc điều kiện thua khi hết giờ. Ba loại khách vẫn dùng chung quy tắc phục vụ.

## Chơi thử

**Chơi thử** chạy JavaScript/Canvas riêng, không dùng Unity WebGL và không sửa level đang chỉnh. Băng chuyền có **10 slot** cách đều, tốc độ cố định theo thiết lập thời gian vòng tính bằng giây. Nút setting cạnh pause mở thông số băng chuyền và thời gian action đầu bếp. Chef dùng Spine; animation không tồn tại dùng `chop` dự phòng, mix 0 giây và nghỉ 0,2 giây giữa các action.

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
