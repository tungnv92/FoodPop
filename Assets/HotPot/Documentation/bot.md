# Quy tắc bot đánh giá độ khó FoodPop

## Vai trò và tầm nhìn

- Chơi có chủ đích, gần cách một người chơi thật quan sát, suy nghĩ và học lại nhất có thể. Không chọn bừa hoặc cố tình tạo lỗi ngẫu nhiên để đạt tỉ lệ thắng mong muốn.
- Chỉ quan sát nguyên liệu trong **3 hàng đầu tiên tính từ đầu board hiện tại**. Hàng mới đi vào vùng này sau khi dồn mới được quan sát; không đọc nguyên liệu ở hàng dưới.
- Không biết nguyên liệu hidden chưa mở. Không dùng dữ liệu level, ID nội bộ hoặc trạng thái engine để suy ra thông tin người chơi chưa thấy.
- Chỉ biết order của khách đã xuất hiện; không đọc khách tương lai.
- Khi chơi lại, được nhớ nguyên liệu, order và tình huống đã thực sự quan sát trong lượt trước. Không biến dữ liệu chưa từng thấy thành trí nhớ.
- Engine có thể giữ toàn bộ dữ liệu để chạy gameplay, nhưng phần quyết định nước đi phải tuân thủ các giới hạn quan sát trên.

## Quyết định nước đi

- Xét nhu cầu khách hiện tại, nguyên liệu đế cần trước, phần đã có trên băng/đang xử lý và số slot còn trống.
- Tính đủ số phần của toàn bộ nhóm nối trước khi lấy; tránh lấy dư không có mục đích.
- Khi nguyên liệu cần bị chặn, xem đường mở trong vùng quan sát và lượng nguyên liệu phải mang lên băng. Giữ đủ chỗ cho cả nhóm cần lấy sau đó.
- Chờ sơ chế/phục vụ nếu việc chờ có thể giải phóng chỗ; không liên tục mở cột không liên quan khi đang thiếu một nguyên liệu cụ thể.
- Mỗi lựa chọn quan trọng cần có lý do có thể truy lại từ thông tin đã thấy. Nếu biến thiên hành vi giữa các lượt, mô tả giả định rõ ràng; không dùng hành động ngẫu nhiên làm đại diện cho người chơi.

## Học từ thất bại

- Sau mỗi lần thua, phân tích nguyên nhân cụ thể: nguyên liệu nào chiếm slot, khách còn thiếu gì, đường lấy bị chặn ở đâu, nhóm cần lấy có bao nhiêu phần và nước đi nào làm mất khả năng tiếp tục.
- Phân biệt lỗi chọn đường, lấy dư, thiếu chỗ, thứ tự đế, nhịp bấm/chờ và thông tin chưa được biết. Không tự kết luận level vô nghiệm chỉ vì bot thua.
- Lần chơi lại phải đổi kế hoạch dựa trên chẩn đoán đó, ví dụ mở đúng đường sớm hơn hoặc chừa slot cho nhóm nối. Không chỉ đổi seed hoặc trừ điểm một nước đi cũ mà không hiểu nguyên nhân.
- Ghi lại bài học và thay đổi thực tế giữa các lượt để kiểm tra việc học có giúp vượt điểm kẹt hay không.
- Khi đo học lại riêng một bản Hard, giữ cùng dữ liệu Hard qua các lượt và ghi rõ đã tách khỏi cơ chế tự chuyển Easy của gameplay thực tế.

## Nhóm đánh giá và báo cáo

- Chỉ dùng hai nhóm **bình thường** và **cẩn thận**, trọng số bằng nhau. **Không đưa nhóm chơi vội vào tỉ lệ chung**.
- Nêu rõ cách định nghĩa hai nhóm và các giả định về khả năng quan sát, suy nghĩ, ghi nhớ, nhịp thao tác. Không gán nhãn người thật cho hai heuristic chưa hiệu chuẩn.
- Tách tỉ lệ thắng lượt đầu khỏi kết quả sau khi học lại. Báo số lượt bắt đầu mỗi lần thử, số thắng trong lần đó và tỉ lệ đã thắng tích lũy sau lượt 2, 3…; tránh nhầm mẫu số.
- Báo thời gian chơi trung bình, tách thắng/thua; dùng thời gian thực ở tốc độ chơi chuẩn, không nhầm với đồng hồ simulation được tăng tốc pre-win.
- Báo lượt bấm lúc kẹt (trung bình và phổ biến), phân bố số khách **đã phục vụ** khi kẹt và kết quả vượt hai khách đầu.
- Phân biệt “đang phục vụ khách thứ 6” với “đã phục vụ 6 khách”. Luôn ghi con số đã phục vụ để tránh lệch mốc.
- Nêu số mẫu, seed dùng tìm cấu hình và seed kiểm chứng độc lập; không chọn lại cấu hình theo bộ kiểm chứng rồi vẫn gọi bộ đó là độc lập.
- Phân biệt kết quả mô phỏng với winrate người thật. Các lượt chỉ khác thời gian bấm nhưng trùng chiến thuật/đường đi không phải những người chơi độc lập.
- Không dùng kết quả phân cực như một chính sách thua hết và một chính sách thắng hết để khẳng định đã xác nhận winrate người thật 50%.
- Nếu thay quy tắc chiến thuật sau thất bại, ghi rõ giả định người chơi áp dụng được bài học; không gọi tỉ lệ bot thắng lại là xác suất người thật tự học thành công.

## Mục tiêu hiện tại của Level30 Hard

- Mục tiêu thắng chung 40–50%, tính đều hai nhóm bình thường/cẩn thận, với cách chơi có chủ đích và giới hạn tầm nhìn trên.
- Cho bắt đầu có nguy cơ thua quanh **khách 6**, thay cho mục tiêu cũ là khách 8–11; giữ đoạn đầu để người chơi ghi nhớ thao tác và học lại. Không thiết kế kẹt quá sớm sau khách 4.
- Giữ order 14 khách, 35 món, 115 phần; board 5×16 đầy 80 ô, tổng từng nguyên liệu cân bằng và hai khách đầu dễ tiếp cận.
- Không đổi `LevelConfig_30_Easy` khi chỉnh bản Hard. Chỉnh data Web Studio không cần deploy lại giao diện; không tự cập nhật asset Unity ngoài phạm vi yêu cầu.
- Chỉ xác nhận đạt mục tiêu khi bằng chứng phù hợp; chưa đạt hoặc mô hình chưa đáng tin thì ghi rõ.

## Quy tắc thiết lập bản Easy từ level Hard

Áp dụng mặc định cho mọi cặp Hard/Easy về sau, không cần người dùng nhắc lại.

- Lấy **bản Hard mới nhất** làm nền bố cục. Easy phải giống cùng một màn đã được nới độ khó, để người chơi nhận ra và áp dụng kinh nghiệm từ các lần thua Hard.
- Giữ kích thước board, hình dáng và vị trí nguyên liệu gần Hard nhất có thể. Không xếp lại toàn board theo order, tráo hàng loạt hoặc tạo một bố cục khác hoàn toàn chỉ để tăng tỉ lệ thắng.
- Giảm có chủ đích các yếu tố gây khó: mở bớt hidden, gỡ bớt nhóm nối gây áp lực/chặn đường, giảm số phần phải lấy cùng lúc hoặc đơn giản hóa order khi cần. Ưu tiên giảm trở ngại trước khi đổi vị trí nguyên liệu.
- Khi giảm nguyên liệu theo order Easy, chỉnh tối thiểu số phần và ô cần thiết; ưu tiên giảm ô nhiều phần. Nếu phải đổi loại hoặc chia phần để giữ board đầy, chọn ô phù hợp gần đó và ghi rõ khác biệt với Hard.
- Giữ các mốc nhận diện và phần mở đầu quen thuộc; nới đúng điểm kẹt của Hard để bài học về đường mở, giữ slot và thứ tự lấy vẫn có ích. Không chỉ chuyển điểm kẹt sang một bẫy khác.
- Giữ order của bản Easy hiện có nếu chưa cần thay. Nếu đơn giản hóa order, giữ tiến trình khách và nguyên liệu đặc trưng gần Hard; không thay cả trải nghiệm bằng một bộ order không liên quan.
- Bảo đảm tổng từng nguyên liệu khớp order và cấu trúc board hợp lệ. Với Level30 hiện tại, giữ board 5×16 đầy 80 ô; order Easy hiện có là 14 khách/30 món/105 phần trừ khi có yêu cầu thay đổi.
- Đối chiếu Easy với Hard trước khi lưu: số ô giữ nguyên loại/vị trí, số phần thay đổi, hidden và nhóm nối đã giảm, order có thay đổi hay không. Nêu khác biệt có ý nghĩa trong báo cáo.
- Kiểm chứng Easy bằng hai cách chơi bình thường/cẩn thận và giới hạn quan sát trong tài liệu này. Easy cần giảm áp lực rõ ràng so với Hard; không tự đặt một winrate người thật chưa có bằng chứng.
- Lưu đúng tên `_Easy`, giữ nguyên bản Hard. Chỉnh dữ liệu web không tự đồng nghĩa đã cập nhật asset Unity; chỉ đồng bộ Unity khi thuộc phạm vi yêu cầu.

## Tham chiếu

- Lịch sử cấu hình, kết quả và giới hạn kiểm chứng: [LevelStudioWeb.md](LevelStudioWeb.md), mục Level 30 Hard server v20.
- Các quy tắc trong tài liệu này thay thế những phép đánh giá cũ dùng lựa chọn bừa, nhìn sâu hơn 3 hàng hoặc đọc thông tin chưa lộ.
