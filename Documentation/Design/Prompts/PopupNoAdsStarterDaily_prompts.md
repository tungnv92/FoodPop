# Prompt tạo sprite cho popup No ADS, Starter Pack, Daily Rewards

Ngày 2026-11-25. Mockup tham khảo: `tmp/popup_art/mockup_3_popups.webp` (3 popup: No ADS, Starter Pack, Daily Rewards). Ảnh do Codex (`codex exec`, tính năng `image_generation`) tạo, lưu thô ở `tmp/popup_art/raw/`, sau đó thêm viền cho kích thước chia hết cho 4 rồi mới import vào Unity.

## Quy tắc chung (áp dụng cho mọi ảnh)

- Phong cách: casual mobile game, 2.5D cartoon, viền nâu đậm mềm, đổ bóng nhẹ, màu tươi ấm, giống mockup và giống UI hiện có của game sushi (FoodPop).
- Nền **trong suốt** (PNG RGBA), chỉ một vật thể, căn giữa, không cắt sát mép (chừa khoảng 4–8 px).
- **Không vẽ chữ** trong ảnh (chữ làm bằng TextMeshPro trong Unity).
- Không có khung popup, không nền tối phía sau.

## Danh sách ảnh

| # | File | Mô tả prompt | Kích thước gợi ý |
|---|---|---|---|
| 1 | `noads_roof.png` | Mái ngói kiểu Nhật màu xanh dương, hai đầu cong lên, viền nâu; góc trái có cụm hoa anh đào hồng và lá xanh; nhìn chính diện, rộng ngang, dùng làm đầu popup | 1024×384 |
| 2 | `noads_sign.png` | Bảng tên gỗ bo tròn màu hồng đỏ, viền trong kem, để trống phần chữ | 640×224 |
| 3 | `noads_icon.png` | Chữ "ADS" khối 3D trắng bị gạch chéo bằng vòng tròn cấm màu đỏ, đặt trên chiếc đệm vuông xanh dương có hoa văn sóng Nhật và tua vàng ở góc, vài tia lấp lánh | 640×512 |
| 4 | `badge_flower_pink.png` | Huy hiệu hình bông hoa 8 cánh màu hồng đậm có viền trắng, kèm dải ruy băng vàng nhỏ phía dưới, để trống chữ | 256×256 |
| 5 | `noads_panel.png` | Ô nền bo góc màu be/kem nhạt có viền trong nhạt, phẳng, dùng làm nền minh họa trong popup | 640×448 |
| 6 | `starter_roof.png` | Mái ngói kiểu Nhật màu xanh lá, hai đầu cong lên, viền nâu, góc trái có cụm hoa anh đào hồng; giống mái No ADS nhưng màu xanh lá | 1024×384 |
| 7 | `starter_chef.png` | Ông đầu bếp sushi mập mạp, ria mép xám, khăn trắng buộc đầu, áo trắng cổ xanh, mắt nhắm cười hiền, tay đặt trên bụng; nửa thân trên, nhìn chính diện | 512×448 |
| 8 | `starter_lantern.png` | Đèn lồng giấy Nhật màu kem, nắp và đáy đen, tua đỏ phía dưới, để trống mặt chữ | 224×320 |
| 9 | `starter_counter_top.png` | Mặt quầy gỗ phủ khăn xanh lá có hoa văn nhạt, viền gỗ vàng cam, nhìn chính diện hơi từ trên, dài ngang | 1024×256 |
| 10 | `starter_counter_body.png` | Thân quầy gỗ ván dọc màu xanh lá đậm với hai cột gỗ vàng cam hai bên, nhìn chính diện | 1024×448 |
| 11 | `btn_claim_locked.png` | Nút bo tròn màu xám xanh có icon máy quay video bên trái, ổ khóa vàng nhỏ ở góc trên phải, để trống chữ | 384×160 |
| 12 | `progress_vertical.png` | Thanh tiến trình dọc bo tròn: rãnh nền đen viền nâu, phần đầy màu xanh lá bóng; kèm 1 núm tròn xanh lá (đã đạt) và 1 núm tròn kem (chưa đạt) tách riêng thành 3 phần trên cùng một ảnh: rãnh, núm xanh, núm kem | 512×512 |
| 13 | `heart_infinite.png` | Trái tim đỏ bóng 3D có ký hiệu vô cực (∞) màu trắng ở giữa | 256×256 |

## Thư mục đích sau khi xử lý

- No ADS (1–5): `Assets/HotPot/Art/Sprites/UI/NoAds/`
- Starter Pack (6–10): `Assets/HotPot/Art/Sprites/UI/StarterPack/`
- Daily Rewards (11–13): `Assets/Packages/DailyReward/Sprites/`

Ghi chú 2026-11-25: các ảnh Codex trên đã được thay bằng art chính thức của artist ở `UI/no ads/`, `UI/starter_pack/` và `Packages/DailyReward/Sprites/daily_reward/`; Starter Pack đã có art đầu bếp thật (tim vô cực dùng `UI/Shop/live.png`).
