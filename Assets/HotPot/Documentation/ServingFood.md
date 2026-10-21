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

EN: `BeginOrder` receives the order view, dish types and recipes, plus ingredient/dish delivery callbacks. `CustomerManager` reserves a recipe ingredient before calling `LaunchServe`; a failed launch rolls the reservation back without consuming belt food. Each tray slot allows one incoming ingredient; different slots and different trays can receive in parallel. After the ingredient is physically placed, its callback updates customer progress. The manager requests `CompleteDish` when the recipe is complete. The finished-dish callback fires only after the dish reaches the order card.

VI: `BeginOrder` nhận thẻ order, loại món và công thức, cùng callback đáp nguyên liệu/món. `CustomerManager` giữ chỗ nguyên liệu trong công thức trước khi gọi `LaunchServe`; nếu không phóng được thì hoàn tác giữ chỗ mà không lấy thức ăn khỏi băng. Mỗi ô khay chỉ có một nguyên liệu đang bay tới; các ô và các khay khác nhau có thể nhận song song. Sau khi nguyên liệu đã được đặt lên khay, callback mới cập nhật tiến độ khách. Manager yêu cầu `CompleteDish` khi đủ công thức. Callback món hoàn chỉnh chỉ phát khi món đã tới thẻ order.

EN: `ResetOrder` stops this instance's coroutines, drops old callbacks, clears stacks/slots and returns all owned sprites to its pool, including sprites currently merging or flying into the order. Customer changes, replay and manager teardown reset the appropriate serving instances. Destroying a serving instance also removes its own world-space flyer container. `Tools > HotPot > Setup Customers And References` ensures the prefab has the serving component and binds each lane to its matching tray.

VI: `ResetOrder` dừng coroutine của instance này, bỏ callback cũ, dọn chồng/ô và trả mọi sprite đang sở hữu về pool, kể cả sprite đang ghép hoặc bay vào order. Khi đổi khách, replay hoặc hủy manager, các instance phục vụ tương ứng được đặt lại. Hủy một instance cũng xóa object chứa sprite bay theo tọa độ thế giới của riêng nó. `Tools > HotPot > Setup Customers And References` bảo đảm prefab có component phục vụ và nối từng làn với đúng khay.
