# IngameConfig

Asset `Assets/HotPot/Config/Resources/IngameConfig.asset`, truy cập qua `IngameConfig.Instance` (mẫu: `private static IngameConfig Config => IngameConfig.Instance;`). Component không có field serialize IngameConfig riêng. Runtime chỉ đọc, không ghi trạng thái vào asset.

| Nhóm | Chức năng |
| --- | --- |
| `ingredientFlight` | Bay từ băng tới khay và nảy khi đáp |
| `boardMotion` | Thời gian dồn cột board |
| `boardFlight` | Vòng cung, đường cong, scale, xoay khi bay từ board lên băng |
| `dishAssembly` | Ghép lớp và nảy món hoàn chỉnh |
| `dishOrderFlight` | Món hoàn chỉnh bay vào order |
| `customers` | Chuyển động khách, nhịp hàng chờ |
| `preparation` | Nhấc/thả và hiệu ứng chặt (thời gian sơ chế lấy từ animation Spine, xem [ChefAnimationFrames.md](ChefAnimationFrames.md)) |
| `orderPop` | Nảy ảnh món và dấu tích trên order |
| `iceHit` | Nảy khi đĩa đóng băng bị trừ |
| `trayStack` | Kích thước/bước xếp chồng trên khay |
| `preWin` | `speedMultiplier` (1.3, khoảng 1–2), `hidePlatesDelay` (1 s), `plateShrinkDuration` (0,35 s) |
| `winReward` | `coin` (100, nút nhận thường ở WinPopup), `watchAdsCoin` (200, nút xem quảng cáo) |
| **Food Icons** | Gán `FoodIconConfig.asset` một lần; đọc qua `IngameConfig.Instance.FoodIcons` |

## Bảng nguyên liệu (FoodIconConfig)
Một bảng chung cho icon thô/sơ chế, nhóm sơ chế, âm chặt và vụn. Food, board, OrderView, FoodHoldingStorage không giữ bảng riêng; `ConveyorSlot.TryFillFood(type)`, `DishPickBatch.TryFill(...)`, `FoodHoldingStorage.Configure(...)` không nhận tham số bảng. `FoodIcon.skipPreparation` (Ice, Salt) cho nguyên liệu không cần sơ chế.

Kích thước/ảnh ô khay nằm trên `DishSlot.prefab`; bố cục scene, đường băng, công thức nằm ở component/asset tương ứng.

**Kiểm chứng:** đã kiểm tra trong Unity liên kết asset và icon; `preWin` mới dùng giá trị mặc định (asset chưa lưu).
