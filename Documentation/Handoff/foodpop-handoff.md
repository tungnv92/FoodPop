# FoodPop — ghi chú bàn giao

Điểm bắt đầu cho agent mới. Trạng thái đến 13/11/2026; tài liệu viết lại dạng hiện trạng ngày 16/11/2026 (lịch sử chi tiết nằm trong git). Quy tắc bắt buộc: `AGENTS.md` (Claude Code nạp qua `CLAUDE.md`).

## Project
- Unity 6000.3.17f1, URP. Máy công ty `D:\Unity\Fork\FoodPop`, máy desktop `E:\FoodPop`. Repo GitHub nhánh `main`.
- Nội dung game trong `Assets/HotPot` (cấu trúc thư mục: mục "Mandatory file and asset organization" trong `AGENTS.md`). Scene chơi `Scenes/HotPotDemo.unity`, menu `Scenes/Home.unity`.
- Gameplay: board nguyên liệu (cột đĩa) → chạm đĩa đầu cột → nguyên liệu lên băng chuyền 10 slot → đầu bếp sơ chế → bay tới khay của khách (hai chỗ phục vụ) → ghép thành món → đủ order thì khách rời. Hết khách = thắng; băng kẹt hoặc Boss/Shipper hết giờ = mời hồi sinh.

## Bản đồ tài liệu
| Chủ đề | File |
| --- | --- |
| Nguyên liệu, level, khách, board, scene HotPotDemo và Home | [Configuration.md](../Features/Configuration.md) |
| IngameConfig, FoodIconConfig | [IngameConfig.md](../Features/IngameConfig.md) |
| Board, chạm, đóng băng, chẩn đoán pick | [FoodBoardManager.md](../Features/FoodBoardManager.md) |
| Băng chuyền, đầu bếp, sức chứa DishPickPlan | [ConveyorChef.md](../Features/ConveyorChef.md) |
| Đầu bếp Spine | [ConveyorChef.md](../Features/ConveyorChef.md#đầu-bếp-spine) |
| Hàng khách, đồng hồ Boss/Shipper | [CustomerManager.md](../Features/CustomerManager.md) |
| Khay, ghép món, hoạt cảnh món | [ServingFood.md](../Features/ServingFood.md) |
| UI, popup, luồng thắng/thua/hồi sinh, âm thanh, hướng dẫn | [UI.md](../Features/UI.md) |
| Booster, túi giữ món, mua booster | [Booster.md](../Features/Booster.md) |
| Dữ liệu người chơi, lưu offline | [DataSystem.md](../Features/DataSystem.md) |
| Level Editor Unity | [LevelEditor.md](../Features/LevelEditor.md) |
| Level Studio Web, trạng thái level | [LevelStudioWeb.md](../Features/LevelStudioWeb.md) |

## Lưu ý khi làm việc
- **IDE lưu đè:** đã nhiều lần IDE ghi bản cũ lên file agent vừa sửa (`ChefManager.cs`, `FoodBoardManager.cs`). Sau khi agent sửa code, Reload trong IDE, không lưu bản cũ.
- **Nhiều agent song song:** từng có agent ghi đè `Food.cs` của agent khác — chia file theo agent.
- **Không đổi tên/kiểu field đã gán trong scene** (đổi tên từng làm mất liên kết); nếu cần thì dùng `FormerlySerializedAs`. Ghi file scene từ ngoài thì Unity hỏi Reload.
- **Không tạo tool Setup/Builder dựng prefab** — người dùng đã xóa chúng; prefab/config chỉnh tay.
- `IngameConfig` chỉ truy cập qua `IngameConfig.Instance`.
- 17/11/2026: Đã thay padding nhân bản mép của 159 ảnh trong commit `a2b7ac3` bằng viền trong suốt từ pixel gốc trước commit. Giữ GUID, bù pivot cho 148 sprite Single và chỉnh rect của 11 sprite Multiple (bao gồm sửa rect `Ingame/dot_red` bị bỏ sót trước đó). Xem [Configuration.md](../Features/Configuration.md#padding-sprite). Đã kiểm tra byte pixel gốc/viền trong suốt, GUID và tọa độ pivot/rect; chưa kiểm tra trực quan toàn bộ popup hoặc trên thiết bị.
- Ảnh mới thêm vào `Assets/HotPot/Art/Sprites/` tự nhận Sprite, Single, Full Rect ở lần import đầu (`Editor/SpriteImportDefaults.cs`, chỉ khi chưa có `.meta`; mặc định Unity 6 ở project này là Multiple + Tight). Ảnh đã import và chỉnh tay về sau không bị đổi; ảnh ngoài thư mục này vẫn theo mặc định Unity.
- `.gitattributes` = `* text=auto` nên khác biệt CRLF/LF không tạo diff.
- Debug: `AutoPlayer` + `GameplayRecorder` (menu Tools → HotPot → Auto Play And Record, video ở `Recordings/`, đã gitignore). Probe/kết quả kiểm chứng để ngoài Assets trong `output/`.

## Việc còn treo
- **Spine khách Normal** (chi tiết: [CustomerManager.md](../Features/CustomerManager.md#spine-khách-normal-ingameconfigcustomers)):
  1. `happy` → `walk` khi khách rời quầy và walker spine khi khách mới bước lên (cần chơi thật cho đủ món).
  2. Chiều quay mặt khi `walk`: nếu đi lùi thì lật `skeleton.ScaleX` theo `lane.slideDirection` (và hướng từ hàng chờ vào quầy).
  3. Vị trí chữ khen: `CustomerView.PraisePosition`/`PortraitCenterOffsetX` dùng bounds của sprite đang bị tắt — kiểm tra có lệch không.
  4. Các bước sau theo kế hoạch: spine riêng cho Boss/Shipper, có thể chuyển spine vào `Assets/HotPot/Art/...` khi người dùng yêu cầu.
  5. Chưa commit (git còn thay đổi font không liên quan của người dùng — đừng commit chung).
- **Play test chưa chạy:** chuỗi thắng (chờ 1 s, thu nhỏ đĩa, Curtain Close → nền đen fade → Win title); hồi sinh hết giờ (popup, thanh toán, +30 s); pre-win; FinalFood_8 khay riêng; opacity icon order; gà teriyaki; nhận món trực tiếp từ board; giới thiệu booster mở khóa; khóa booster theo level; luồng thua → chơi lại bản Easy.
- Xác nhận lỗi "vừa Play không pick được đĩa" (02/11) đã hết; gán `Slot Food` cho mọi ConveyorSlot; tắt `logPickFailures`.
- Gán clip nhạc nền; kiểm tra rung trên thiết bị thật; nối SDK Ads (Watch hiện gọi thẳng callback thành công ở BuyBooster, Revive, Win).
- Console: lỗi serialize trùng `restScale` ở `CanvasBoosterIntro`; cảnh báo thiếu animation `Idle` của `Chef_SkeletonData`; cảnh báo PMA/Linear của Curtain.
- Level 29 v12 mới có trên web, chưa đồng bộ về Unity. 165 nhóm order (level 16–25) chưa có prefab món khớp.
- Level30 Hard chưa xác nhận mục tiêu winrate 40–50% (kết quả mô phỏng v20 phân cực: nhóm bình thường 0/50, cẩn thận 50/50; script ở `output/level30`).
- Cucumber (FoodType 7) thiếu tham chiếu Raw/Prepared trong `FoodIconConfig`.
- Đối chiếu `PlayHaptic()` (từng ghi là có kiểm tra null `ProfileManager.instance`/`PlayerDataManager`) với quy tắc cấm kiểm tra null trong `AGENTS.md`.
- Có thể: hiệu ứng riêng khi nguyên liệu không cần sơ chế (Ice, Salt) đáp xuống đĩa; khay trượt theo khách; Level Editor cảnh báo khi cơm nằm sâu trong bảng.
- Asset không còn dùng (kiểm tra reference trước khi xóa): `Prefabs/Effects/FoodLandEffect.prefab`, `Art/Sprites/Effects/IceBlock.png`, `Prefabs/Effects/ChefChopSmoke`, `ChefMeatDebris`, `ChefVegetableDebris`, `Art/Sprites/Effects/FxStarRays.png`, `FxCometArc.png`, `Art/Sprites/UI/Boosters/AutoCompleteStar.png`.
