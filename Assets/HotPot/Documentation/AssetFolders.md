# Asset folders / Thư mục asset

EN: All game content belongs under `Assets/HotPot`. Keep existing feature folders and preserve `.meta` files/GUIDs when moving assets. Update path-based loaders and verify prefab/scene references after moving.

VI: Mọi nội dung game đặt trong `Assets/HotPot`. Giữ cấu trúc theo tính năng và giữ nguyên file `.meta`/GUID khi chuyển asset. Cập nhật đường dẫn nạp và kiểm tra tham chiếu prefab/scene sau khi chuyển.

| Type / Loại | Folder / Thư mục |
| --- | --- |
| Sprites, textures / Ảnh | `Art/Sprites/<feature>` |
| Fonts / Phông chữ | `Art/Fonts` |
| Font materials / Vật liệu phông chữ | `Art/Fonts/Materials` |
| Shaders | `Art/Shaders/<feature>` |
| Materials / Vật liệu | `Art/Materials/<feature>` |
| Particle materials / Vật liệu hạt | `Art/Materials/Particles` |
| Meshes / Lưới | `Art/Meshes/<feature>` |
| Animation clips, controllers / Hoạt ảnh | `Animations/<feature>` |
| Prefabs | `Prefabs/<feature>`; gameplay objects / đối tượng gameplay: `Prefabs/Ingame` |
| Scenes | `Scenes` |
| Configuration assets / Asset cấu hình | `Config`; default shared config / cấu hình chung mặc định: `Config/Resources/IngameConfig.asset` |
| Runtime scripts / Mã lúc chạy | `Scripts/<feature>` |
| Editor tools / Công cụ editor | `Editor` |
| Documentation / Tài liệu | `Documentation` |

EN: Temporary files and source-art backups stay outside `Assets`, using workspace `tmp` and `output` folders. External libraries keep their existing locations.

VI: File tạm và ảnh nguồn dự phòng nằm ngoài `Assets`, dùng thư mục `tmp` và `output` của workspace. Giữ nguyên vị trí thư viện ngoài.
