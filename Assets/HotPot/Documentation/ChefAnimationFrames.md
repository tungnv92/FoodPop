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
