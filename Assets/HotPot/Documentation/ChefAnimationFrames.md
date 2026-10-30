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

EN: The active chef uses ChefAnims with Spine SkeletonAnimation. Defaul/Idle is the initial, cancelled and completed state. Preparation maps SushiRice to RiceBall/Action; TeaLeaves to Tea/Action; Edamame, Shrimp and Octopus to Boil/Action; Tamagoyaki, Unagi, ChickenThigh and ChickenBites to Fry/Action. Other ingredients retain Cut/Action. None maps to idle. Repeating the same animation does not restart its track; transitions use zero mix duration. Missing actions log a warning and fall back to idle when available. ChefManager still controls preparation timing and start/end/cancel; this mapping does not change recipes or ingredient IDs. CuttingBoardSetup resolves the chef renderer through ChefAnims. Older sprite-frame descriptions above are historical.

VI: Đầu bếp dùng ChefAnims với Spine SkeletonAnimation. Defaul/Idle là trạng thái ban đầu, khi hủy và khi hoàn thành. Sơ chế ánh xạ SushiRice sang RiceBall/Action; TeaLeaves sang Tea/Action; Edamame, Shrimp và Octopus sang Boil/Action; Tamagoyaki, Unagi, ChickenThigh và ChickenBites sang Fry/Action. Nguyên liệu khác giữ Cut/Action; None dùng nghỉ. Gọi lại cùng animation không phát lại track; chuyển trạng thái có mix duration bằng 0. Thiếu động tác thì ghi cảnh báo và dùng nghỉ nếu có. ChefManager vẫn điều khiển thời gian và bắt đầu/kết thúc/hủy sơ chế; ánh xạ này không đổi công thức hoặc ID nguyên liệu. CuttingBoardSetup lấy renderer qua ChefAnims. Mô tả frame sprite phía trên là lịch sử.
