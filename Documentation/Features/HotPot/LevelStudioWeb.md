# Level Studio Web

Source `Web/LevelStudio` (frontend `public/studio/`; `dist/` là đầu ra build, không sửa tay; API dùng Sites authentication và D1 `DB`; hosting identity ở `.openai/hosting.json`). Production: https://foodpop-level-studio.nguyentung92bg.chatgpt.site/studio/ (Sites v25). Site chỉ cho tài khoản được cấp quyền.

## Chỉnh level và đồng bộ
1. Nhập `.asset` Unity (text) hoặc mở level từ server; khi mở trang tự đồng bộ các level server theo tên (bản nháp bị thay được sao lưu trước).
2. Tab **Board & nguyên liệu**: chỉnh ô, cọ tô, đĩa đôi, ẩn, băng, link, đối chiếu nguyên liệu. Tab **Order khách**: hàng khách chung, loại khách, `timeServe` (Boss/Shipper, mặc định 30, phải > 0), nhóm nguyên liệu (mục đầu là đế).
3. Mỗi tên level (trim + NFC) là một chuỗi phiên bản. **Lưu** ghi server + lịch sử theo tài khoản; **Level dùng chung** hiện bản mới nhất; **Đồng bộ** thay bản local bằng bản server; **Lịch sử** khôi phục phiên bản. Bản nháp tự giữ trên máy.
4. **Xuất `.asset`** rồi thay dữ liệu trong `Assets/HotPot/Config/Resources/Levels/` (giữ `.meta`/GUID, đặt `levelNumber` khớp tên; đặt `difficultyMode` trong Level Editor vì web chưa có field này). Web không ghi trực tiếp vào project.

## Chơi thử
JavaScript/Canvas riêng (không phải Unity WebGL), không sửa level đang chỉnh. Nút setting cạnh pause chỉnh tốc độ băng và thời gian action đầu bếp (0 = theo animation). Nút booster miễn phí; không mô phỏng Coin/Ads/tài khoản. `PlayDemo.open` chặn lỗi cấu trúc từ `Core.analyze` (ví dụ nhóm chưa chọn nguyên liệu) hoặc hàng khách rỗng.

## <a id="đồng-bộ-với-unity"></a>Đồng bộ với Unity
Web mô phỏng lại runtime Unity bằng JavaScript (không chạy C#). `scripts/sync-unity-runtime.cjs` tạo `catalog.unity` từ export local (dấu vân tay 95 script) — là **snapshot lúc build**, đổi runtime Unity thì phải sync và build lại.
- **Board:** dồn theo footprint, cổng hai cột cho đĩa ngang, hidden đầu màn mở ngay; lấy trọn nhóm nối, kiểm tra băng/nắp và sức chứa chung (`DishPickPlan`).
- **Băng/đầu bếp:** 10 slot, spline 512 mẫu, 14,918 s/vòng, 25 action Spine (`public/studio/spine/chef-v3`), mix 0, Idle1 → Idle2 sau 1,5 s; giữ slot nguồn; nhận trực tiếp từ board; N = S + 1 đi thẳng tới order.
- **Khách:** một hàng/hai chỗ, đế trước, một incoming/món, chờ đáp 0,3 s, phục vụ từ mọi vị trí băng. Boss/Shipper hết giờ → hồi sinh +30 s (miễn phí trong demo).
- **Kẹt băng** → hồi sinh bằng Clean, từ chối → thua. Pre-win, chơi lại Hard lần 2 dùng `_Easy`, nút màn tiếp.
- **Booster:** Pickup, Clean, Shuffle, AutoComplete và túi theo luật local.
- **Resource:** 169 PNG, 13 âm thanh, 102 prefab món nhiều lớp. Shader, ParticleSystem, animation prefab không chạy nguyên bản; hình học đường bay chỉ mô phỏng.

## Chạy và kiểm tra
- Local: `npm run dev -- --hostname 127.0.0.1 --port 4173` trong `Web/LevelStudio`, áp migration D1 local trước khi thử API (đăng nhập dev chỉ có ở loopback). Server tĩnh chỉ phục vụ `public/`, không có `/api/shared`; `ERR_CONNECTION_REFUSED` nghĩa là chưa có server ở cổng đó.
- `node verify.cjs` (xuất/nhập asset, enum, board), `verify-play.cjs` (lấy đĩa, dồn, link, băng, phục vụ), `verify-ingredient-orders.cjs` (nhóm nguyên liệu, loại khách, `timeServe`), `verify-runtime-sync.cjs`, `verify-unity-parity.cjs` (528 trường hợp sức chứa lấy từ Unity), `verify-shared.mjs` (API, cần server + DB local), `node node_modules/typescript/bin/tsc --noEmit`.

## Trạng thái level
| Level | Trạng thái |
| --- | --- |
| 01–25 | Đã nhập về Unity. Level 17 cân bằng lại (Web v10). `LevelConfig_15/20/25_Easy` đã nhập. 165 nhóm order chưa có prefab món khớp (`dish: {fileID: 0}`) |
| 29 | Web **v12** (board 4×20 đầy, mức dễ). **Chưa đồng bộ về Unity** |
| 30 Hard | Web **v20** = Unity local (5×16, 14 khách/35 món/115 phần, 14 nhóm nối) |
| 30 Easy | Web **v5** = Unity local (giữ 76/80 vị trí của Hard; 14 khách/30 món/105 phần, 24 hidden, 9 nhóm nối) |

Quy tắc bot đánh giá độ khó và dựng bản Easy: [bot.md](bot.md). Kết quả mô phỏng và script nằm ở `output/level29`, `output/level30`, `output/level30-easy`.

**Kiểm chứng:** các script verify ở trên PASS sau lần sync gần nhất; production v25 đã mở Level 30 không lỗi JavaScript. Mô phỏng web không thay thế Play Mode Unity; chưa kiểm chứng tương đương từng frame.
