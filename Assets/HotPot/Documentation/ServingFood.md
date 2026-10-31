# ServingFood

EN: Each `ServingHotPot` prefab root has its own `ServingFood`, alongside `ServingTrayView` and `TrayDishStack`. The scene's two serving objects are independent instances, each assigned to `CustomerManager.Lane.servingFood`.

VI: Mỗi gốc prefab `ServingHotPot` có `ServingFood` riêng, cùng `ServingTrayView` và `TrayDishStack`. Hai đối tượng phục vụ trong scene là hai instance độc lập, mỗi instance được gán vào `CustomerManager.Lane.servingFood` tương ứng.

| Component | Responsibility / Trách nhiệm |
| --- | --- |
| CustomerManager | Customer queues, dispatch priority, recipe reservations, order progress and customer departure / Hàng khách, ưu tiên gửi món, giữ chỗ công thức, tiến độ order và khách rời đi |
| ServingFood | Ingredient flights and landing, assembly, finished dish flight to order, sounds/effects and its own sprite pool / Bay và đáp nguyên liệu, ghép món, bay món hoàn chỉnh vào order, âm thanh/hiệu ứng và pool sprite riêng |
| ServingTrayView | Slot count and positions; slot appearance comes from DishSlot prefab / Số ô và vị trí; hình thức ô lấy từ prefab DishSlot |
| TrayDishStack | Ingredient layer order, stack placement and finished dish placement / Thứ tự lớp nguyên liệu, vị trí chồng và món hoàn chỉnh |
| IngameConfig | Shared flight, assembly and movement settings / Thông số bay, ghép món và chuyển động dùng chung |

EN: `BeginOrder` receives the order view, dish types and recipes, plus ingredient/dish delivery callbacks. `CustomerManager` reserves a recipe ingredient before calling `LaunchServe`; a failed launch rolls the reservation back without consuming belt food. Each tray slot allows one incoming ingredient; different slots and different trays can receive in parallel. After the ingredient is physically placed, its callback updates customer progress. The manager requests `CompleteDish` when the recipe is complete. For a named group with final artwork and a tray slot, the finished-dish callback fires after that dish reaches the order card. An unnamed group still completes after its ingredients settle and the assembly delay, without merging or flying a missing final image.

VI: `BeginOrder` nhận thẻ order, loại món và công thức, cùng callback đáp nguyên liệu/món. `CustomerManager` giữ chỗ nguyên liệu trong công thức trước khi gọi `LaunchServe`; nếu không phóng được thì hoàn tác giữ chỗ mà không lấy thức ăn khỏi băng. Mỗi ô khay chỉ có một nguyên liệu đang bay tới; các ô và các khay khác nhau có thể nhận song song. Sau khi nguyên liệu đã được đặt lên khay, callback mới cập nhật tiến độ khách. Manager yêu cầu `CompleteDish` khi đủ công thức. Với nhóm khớp món có ảnh hoàn chỉnh và có ô khay, callback phát sau khi món tới thẻ order. Nhóm không có ảnh món vẫn hoàn tất sau khi nguyên liệu đáp xuống và chờ thời gian ghép; bỏ qua ghép/bay ảnh món chưa tồn tại.

EN: `ResetOrder` stops this instance's coroutines, drops old callbacks, clears stacks/slots and returns all owned sprites to its pool, including sprites currently merging or flying into the order. Customer changes, replay and manager teardown reset the appropriate serving instances. Destroying a serving instance also removes its own world-space flyer container. `Tools > HotPot > Setup Customers And References` ensures the prefab has the serving component and binds each lane to its matching tray.

VI: `ResetOrder` dừng coroutine của instance này, bỏ callback cũ, dọn chồng/ô và trả mọi sprite đang sở hữu về pool, kể cả sprite đang ghép hoặc bay vào order. Khi đổi khách, replay hoặc hủy manager, các instance phục vụ tương ứng được đặt lại. Hủy một instance cũng xóa object chứa sprite bay theo tọa độ thế giới của riêng nó. `Tools > HotPot > Setup Customers And References` bảo đảm prefab có component phục vụ và nối từng làn với đúng khay.

## Ingredient orders and customer types / Order nguyên liệu và loại khách

EN: `CustomerOrder.ingredientOrders` supplies the authoritative ingredient groups, including quantities through repeated entries and the base as the first entry. `FinalFoodConfig.FindMatchingRecipe` supplies optional artwork by exact ingredient quantities. `OrderView.ShowIngredientOrders` keeps prepared ingredient icons visible. Normal, Boss and Shipper share serving rules; `timeServe` defaults to 30 seconds for the special-customer configuration and does not yet trigger a countdown or timeout here.

VI: `CustomerOrder.ingredientOrders` cấp các nhóm nguyên liệu chính, giữ số lượng bằng mục lặp và dùng mục đầu làm đế. `FinalFoodConfig.FindMatchingRecipe` chỉ cấp ảnh tùy chọn theo đúng lượng nguyên liệu. `OrderView.ShowIngredientOrders` luôn hiện icon nguyên liệu prepared. Normal, Boss và Shipper dùng chung quy tắc phục vụ; `timeServe` mặc định 30 giây cho cấu hình khách đặc biệt, chưa kích hoạt đếm ngược hoặc hết giờ tại đây.
