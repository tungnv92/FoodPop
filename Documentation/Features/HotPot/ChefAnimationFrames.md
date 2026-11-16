# Đầu bếp Spine

## Thành phần
- Runtime: `ChefAnims.cs` (Spine `SkeletonAnimation`), điều phối bởi `ChefManager.cs` và `ChefManager.DirectServe.cs` trong `Assets/HotPot/Scripts/gamebase/`.
- Skeleton: `Assets/Spines/Chef/Chef_SkeletonData.asset` (JSON/atlas/texture cùng thư mục), 14 animation.
- Reference sơ chế: `Assets/HotPot/Animations/Chef/Chef_*.asset`; reference nghỉ và Win: `Assets/Spines/Chef/ReferenceAssets/`.
- `HotPotDemo.unity` gán 25 dòng ánh xạ FoodType → AnimationReferenceAsset trên `ChefAnims`.

## Hành vi
- Sơ chế phát một lượt animation của nguyên liệu; thời gian sơ chế = `Animation.Duration` (không có field thời gian riêng). Gọi lặp giữ động tác hiện tại; mix = 0.
- Nghỉ: Idle1 lặp ngay, sau 1,5 giây (cấu hình) chuyển sang lặp Idle2.
- Tham chiếu thiếu hoặc khác bộ Spine → lỗi cấu hình.
- Ánh xạ: Salmon → `Slice_Salmon`; Tuna và loại chưa có action riêng → `Slice_Tuna`; PickledGinger → `Cut_Ginger`; Seaweed → `Cut_Seaweed`; Avocado → `Cut_Avocado`; Scallop → `Slice_Scallop`; Octopus → `Slice_Octopus` (đều 1,7667 s); Wasabi → `Grating_Wasabi`; SushiRice → `RiceBall` (1,8333 s); TeaLeaves → `PourTea`; Edamame → `Boil_Bean` (2 s). Idle1/Idle2 1,6667 s. Có animation mới: tạo reference trong `Animations/Chef/` rồi thay dòng nguyên liệu trên `ChefAnims`.

## Thắng màn
- `ChefAnims.winAnimation` = `Win.asset` (1 s). `PlayWin()` phát một lần, giữ frame cuối, `UnscaledTime` nên chạy khi `Time.timeScale = 0`; `SetPreparing` không ghi đè trạng thái thắng.
- `ChefManager.PlayWin()` hủy sơ chế, chặn tự nhận việc và `ShrinkAllDishes` (mọi slot thu nhỏ về 0 trong `preWin.plateShrinkDuration`, unscaled, rồi tắt object; không xóa food).
- `LoadLevel` → `ResetForLevel()` dừng thu nhỏ, trả scale, hiện lại slot, về Idle1.
- Chuỗi thắng đầy đủ (chờ, pause, WinPopup): xem [UI.md](../../Handoff/UI.md#thắng).

Luồng nhận việc, giữ đĩa nguồn và đi thẳng tới order: xem [Conveyor.md](../../Handoff/Conveyor.md).

**Kiểm chứng:** đã chạy Play Mode cho PlayWin/reset (bản ẩn đĩa tức thì); bản chờ 1 s + thu nhỏ đĩa mới biên dịch, chưa Play Mode.
