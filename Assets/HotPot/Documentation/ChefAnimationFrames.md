# Chef Animator / Animator đầu bếp

- Controller: `Assets/HotPot/Animations/Chef/Chef.controller`.
- Reusable prefab: `Assets/HotPot/Prefabs/Demo/ChefAnimated.prefab`, installed in HotPotDemo.
- Default state **Idle**: 48 frames, 16 fps, 3-second loop.
- State **Chop**: 36 frames, 24 fps, 1.5-second loop, subtle knife trails.
- Set Animator bool **IsChopping** to true for Chop; false returns to Idle. Transitions are immediate with no crossfade. Root motion is disabled; clips contain only SpriteRenderer sprite curves.
- Frames retained: `Art/Sprites/Chef/IdleV4Slow` and `Art/Sprites/Chef/ChopV5Smooth`, all 512 × 512 RGBA, Single/Full Rect, PPU 100, pivot (0.5, 0.0625).

The scene remains a visual demo with no order/conveyor gameplay logic. Chop retains minor local interpolation artifacts accepted for this prototype.

Unused generated images, old frame sequences, source contact sheets and the retired migration editor tool were removed from Assets after checking dependencies of all serialized scene/prefab/config/animation/controller/material roots. Recovery archive: `output/HotPotDemo/Archive/asset-cleanup-20261016-110706`.

Controller có hai trạng thái: **Idle** mặc định (48 frame, vòng 3 giây) và **Chop** (36 frame, vòng 1.5 giây, có vệt sáng dao). Đổi bool **IsChopping** trong Animator để chuyển trạng thái. Không dùng root motion hoặc curve scale. Prefab đầu bếp đã được đặt trong HotPotDemo. Các frame đều 512 × 512, Single/Full Rect, PPU 100, pivot chung (0.5, 0.0625).

Đây vẫn là demo hình ảnh, chưa có logic order/băng chuyền. Chop còn méo nội suy nhẹ cục bộ đã được chấp nhận cho bản tạm. Asset sinh thừa đã được dọn sau kiểm tra dependency, bản phục hồi nằm ngoài Assets tại đường dẫn archive ở trên.

## Spine chef integration / Tích hợp đầu bếp Spine

EN: The active chef uses ChefAnims with Spine SkeletonAnimation and explicit per-ingredient AnimationReferenceAsset entries. Preparation plays one cycle and uses the selected animation's native duration. Repeated calls keep the current action; transitions use zero mix duration. Idle1 loops immediately on entering idle, then Idle2 loops after the configured delay (default 1.5 seconds). Missing or incompatible ingredient references throw a configuration error. Older sprite-frame descriptions above are historical.

VI: Đầu bếp dùng ChefAnims với Spine SkeletonAnimation và danh sách AnimationReferenceAsset riêng từng nguyên liệu. Sơ chế phát một lượt, thời gian lấy từ thời lượng gốc của animation đã chọn. Gọi lặp giữ động tác hiện tại; chuyển động tác không trộn. Khi về nghỉ, Idle1 lặp ngay rồi chuyển sang lặp Idle2 sau thời gian cấu hình (mặc định 1,5 giây). Tham chiếu nguyên liệu thiếu hoặc không cùng bộ Spine gây lỗi cấu hình. Mô tả frame sprite phía trên là lịch sử.

### Spine action update / Cập nhật động tác Spine — 2026-11-11

| Ingredient / Nguyên liệu | Animation | Duration / Thời lượng |
| --- | --- | --- |
| Avocado / Bơ | Cut_Avocado | 1.7667 s |
| Scallop / Sò điệp | Slice_Scallop | 1.7667 s |
| Octopus / Bạch tuộc | Slice_Octopus | 1.7667 s |
| Edamame / Đậu edamame | Boil_Bean | 2 s |

EN: Added Chef_Cut_Avocado.asset and Chef_Slice_Scallop.asset under Assets/HotPot/Animations/Chef and assigned them in HotPotDemo, including the open scene. Edamame already referenced Chef_Boil_Bean.asset and was verified unchanged. Other ingredient mappings are unchanged. Unity Editor validation resolved all three actions through ChefAnims.GetPreparationAnimation and checked their native durations. No Play Mode gameplay test was run. Existing unsaved scene edits were preserved; only the two new mapping GUIDs were written to the scene file.

VI: Đã thêm Chef_Cut_Avocado.asset và Chef_Slice_Scallop.asset trong Assets/HotPot/Animations/Chef, gán vào HotPotDemo và scene đang mở. Đậu edamame đã tham chiếu Chef_Boil_Bean.asset, được kiểm tra và giữ nguyên. Ánh xạ nguyên liệu khác không đổi. Kiểm tra trong Unity Editor đã lấy thành công cả ba động tác qua ChefAnims.GetPreparationAnimation và xác nhận thời lượng gốc. Chưa thử gameplay trong Play Mode. Giữ các chỉnh sửa scene chưa lưu; chỉ ghi hai GUID ánh xạ mới vào file scene.

EN: Subsequent update: added `Assets/HotPot/Animations/Chef/Chef_Slice_Octopus.asset` for the newly available `Slice_Octopus` Spine action and assigned Octopus in HotPotDemo and the open scene. Other mappings were preserved. Unity Editor command compiled and executed successfully; `GetPreparationAnimationName(Octopus)` resolved to `Slice_Octopus` and `GetPreparationDuration(Octopus)` returned 1.7667 seconds. Only the Octopus mapping GUID was written to the scene file; unrelated unsaved scene edits were preserved. Not Play Mode tested.

VI: Cập nhật tiếp: thêm `Assets/HotPot/Animations/Chef/Chef_Slice_Octopus.asset` cho động tác Spine mới `Slice_Octopus`, gán cho Octopus trong HotPotDemo và scene đang mở. Giữ các ánh xạ khác. Lệnh Unity Editor biên dịch và chạy thành công; `GetPreparationAnimationName(Octopus)` trả `Slice_Octopus`, `GetPreparationDuration(Octopus)` trả 1,7667 giây. Chỉ ghi GUID ánh xạ Octopus vào file scene; giữ chỉnh sửa scene chưa lưu ngoài phạm vi. Chưa thử Play Mode.
