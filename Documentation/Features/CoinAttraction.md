# Coin Attraction

## Thư mục và phụ thuộc

Toàn bộ phần hiệu ứng do game sở hữu nằm tại `Assets/HotPot/Packages/CoinAttraction/`, gom theo yêu cầu để tái sử dụng và export:

- `Runtime/`: `CoinAttractionFeedback.cs`, assembly `FoodPop.CoinAttraction`.
- `Prefabs/CoinAttraction.prefab`: bộ phát đã cấu hình, kèm `CoinImpactGlow` bên trong.
- `Sprites/`: `CoinFlip.png` (sheet đã duyệt), `CoinFace.png` (ảnh nguồn riêng), `CoinImpactGlow.png` (Sprite ánh sáng riêng).
- `Demo/`: `index.html` và `flight.js`, xem độc lập bằng browser.
- `Tools/coin_flip3d.ps1`: sinh lại sheet bằng PowerShell 5.1/System.Drawing. Chạy không tham số để dùng ảnh nguồn và đầu ra trong `Sprites/`.
- `Editor/`: menu xuất Unity package và assembly Editor riêng.

Giữ GUID của script và sheet khi di chuyển. Bản sao ảnh mặt xu/ánh sáng có GUID riêng, không kéo theo asset UI khác. Package không phụ thuộc `ProfileManager`, `PlayerDataManager`, `UICoin`, `CanvasWin` hoặc DOTween. Phụ thuộc ngoài là Unity UI (`Unity.ugui`) và thư viện ParticleImage tại `Assets/AssetKits/ParticleImage/`; không di chuyển hoặc sửa thư viện này.

## Dùng prefab

1. Kéo `Prefabs/CoinAttraction.prefab` vào Canvas. Tránh đặt dưới một ParticleImage khác vì thư viện sẽ nhận bộ phát cha làm main.
2. Chỉnh RectTransform của **CoinAttraction** để đặt điểm phát; emitter constraint đã tắt nên không bị Reward ghi đè. Tên CoinParticles trong các trao đổi trước là node phát cũ, đã được thay bằng root của prefab độc lập.
3. Gọi `GetComponent<FoodPop.CoinAttractionFeedback>().Play(walletIcon)` với `walletIcon` là RectTransform điểm đến. API gán cả attractor target và đối tượng nảy, giữ scale gốc của ví.
4. Gọi `Stop()` để hủy; disable object cũng xóa hạt và khôi phục scale/alpha. Nhận thưởng, lưu tài nguyên và đổi màn do bên gọi quản lý.

Prefab độc lập không gán sẵn ví ngoài prefab. Có thể gán `walletIcon` và `ParticleImage.attractorTarget` trong Inspector rồi gọi `ParticleImage.Play()`, như instance bên trong WinPopup.

`CoinImpactGlow` nằm trong prefab hiệu ứng; khi mỗi xu đến, script đặt nó tại vị trí ví, cho ví nảy thêm 15% rồi về scale gốc và cho ánh sáng mờ dần trong 0.15 s. Phản hồi dùng coroutine unscaled, không cần tween library.

## Chuyển động

- 10 xu, mỗi burst 1 xu, cách 0.085 s. Sheet 4×4 chạy theo Lifetime; pha đầu biến thiên qua hai curve, startFrame=0, cycles=1. Hai curve cùng đạt 32 ở 90% lifetime và giữ đến cuối: frame 32 modulo 16 = frame 0 chính diện. Không dùng FPS cố định cho các xu đang bay để bảo đảm mọi lifetime/pha đều kết thúc chính diện.
- Bung nhỏ, rơi nhẹ bằng velocity Y âm trước khi attractor bắt đầu kéo lên (tới 32% lifetime giữ attractor bằng 0), sau đó tăng tốc về ví.
- Lifetime 0.93–1.01 s; size 120 px pop đầu rồi về 76% gần ví. Không trail, vortex hoặc xoay phẳng.
- Duration 0.95 s, tổng ngân sách `duration + lifetime.constantMax` là 1.96 s. Toàn bộ hiệu ứng chạy khi timeScale bằng 0.
- Sheet 576×576, 16 ô 144×144, pitch 0°, thickness 0.2197, cạnh vàng mịn có shading vát. Không thay ảnh mặt xu gốc dùng ở UI khác.

WinPopup dùng nested instance của prefab này, target là icon UICoin và giữ offset phát `(0, -327)` tại lúc tách. Luồng claim hiện có vẫn cộng/lưu ngay khi bấm, rồi UICoin tăng số sau delay 0.4 s; xem [UI.md](UI.md#canvaswin).

## Sinh sheet và cập nhật demo

Chạy từ gốc project bằng process PowerShell mới để tránh trùng type C# `CoinRender` đã nạp:

```powershell
powershell.exe -NoProfile -File Assets/HotPot/Packages/CoinAttraction/Tools/coin_flip3d.ps1
```

Mặc định đọc `Sprites/CoinFace.png`, ghi `Sprites/CoinFlip.png`, pitch 0°, thickness 0.2197, không khía, SS 3, DepthX=-0.12 và DepthY=0.32. Có thể truyền `-Dst` ra `tmp/` để xem thử trước khi thay asset. Không cần sửa ảnh `Art/Sprites/UI/Ingame/ic_coin.png` dùng chung của game.

`Demo/flight.js` là nguồn mã chuyển động; `Demo/index.html` chép inline mã đó và nhúng PNG base64 để mở độc lập. Sau khi sửa JS hoặc sinh PNG mới, đồng bộ phần script/ảnh nhúng trong HTML; chạy generator không tự cập nhật HTML. Demo dùng đường cong quadratic với nhịp bung 0.17 s, rơi 13 px trong 0.14 s, rồi bay 0.76–0.84 s. Thông số demo là mô phỏng thị giác, không phải phép đo chính xác video hoặc thuật toán ParticleImage trong game.

Xu trong demo cũng hội tụ về frame 0 ở 10% cuối chuyến bay; thanh FPS chỉ điều khiển xu xem thử riêng. Số Coin trong demo cộng theo từng xu đến. Trong game, tiền được cộng/lưu ngay khi claim và số hiển thị vẫn tăng theo delay 0.4 s của CanvasWin; package hiệu ứng không quản lý tiền.

## Export

Chọn **Tools → Coin Attraction → Export Unity Package**, chọn nơi lưu. Menu thu thập thư mục package này cùng toàn bộ runtime/editor ParticleImage và các phụ thuộc trong Assets; loại mã nguồn dưới Packages của Unity để project nhận dùng Unity UI đã cài. Không kèm demo thư viện hoặc WinPopup/gameplay code. Assembly riêng tránh phụ thuộc Assembly-CSharp của game.

Nếu export thủ công, chọn cả folder package và hai folder `Assets/AssetKits/ParticleImage/Runtime`, `Assets/AssetKits/ParticleImage/Editor`, rồi kiểm tra danh sách Include Dependencies, bỏ mã nguồn dưới `Packages/com.unity.ugui` nếu Unity thêm vào. Ưu tiên menu ở trên vì đã lọc phần này. Project nhận cần Unity UI; menu không cài thêm dependency UPM.

## Kiểm chứng

Đã kiểm tra prefab độc lập không tham chiếu WinPopup hoặc manager game; WinPopup trỏ tới nested prefab mới, giữ offset và không ràng buộc điểm phát. Play Mode probe prefab độc lập đã qua: `Play(target)`, đủ 10 xu đến ví ở timeScale=0, glow có Sprite và nảy ví, vị trí phát không đổi, có velocity rơi trước attractor, disable xóa hạt/reset và phát lại đủ 10 xu. Kết quả `output/CoinFlight/RuntimeCheck.txt`. Demo đã kiểm tra chuyển pha rơi/bay liên tục bằng canvas giả lập; generator trong package tái tạo sheet đã duyệt khớp từng byte. Chưa build Android hoặc import thử vào project Unity khác.

Bản export kiểm chứng: `output/CoinFlight/CoinAttraction.unitypackage`, 42 asset. Đã đọc danh sách asset trong archive và xác nhận đủ assembly/runtime helper, không chứa WinPopup, code gameplay, DOTween hoặc mã nguồn dưới Packages của Unity. Danh sách ở `output/CoinFlight/PackageContents.txt`; chưa import thử vào project khác.

Kiểm chứng mặt chính diện 18/11: 404 phép tính frame theo đúng công thức ParticleImage với các pha ngẫu nhiên và 4 mốc 90–100% lifetime đều ra frame 0; WinPopup kế thừa cấu hình Lifetime mới. Demo kiểm tra 40 pose nhìn thấy được ở 90–99.99% chuyến bay đều chính diện. Kết quả `output/CoinFlight/FrontalArrivalCheck.txt`. Không thay đổi thời gian bay hoặc logic thưởng.

Độ dày cập nhật 18/11: tăng thêm 30% so với 0.169 thành 0.2197 (tổng cộng +69% so với mức ban đầu 0.13), giữ pitch 0°, cạnh mịn và shader shading hiện có. Đã sinh lại sheet và ảnh nhúng demo; cơ chế giữ chính diện ở cuối chuyến bay không đổi.

Mặt chính diện cập nhật 18/11: generator dùng phép chiếu chiều sâu xiên `screenX=worldX+DepthX×worldZ`, `screenY=worldY+DepthY×worldZ` để lộ cạnh vàng phía dưới/phải ngay ở frame 0. Mặt trước vẫn tròn, pitch vẫn 0°, không tăng độ dày vật lý thêm. DepthX=-0.12, DepthY=0.32 có thể chỉnh trong tham số generator. Đã xem sheet 16 khung, giữ cơ chế kết thúc frame chính diện.

Đường bao cập nhật 18/11: opacity của khối trụ quyết định silhouette; màu mặt ảnh được composite premultiplied lên nền vàng đặc. Alpha viền ảnh nguồn không còn tạo khe tại chỗ nắp/cạnh. Kiểm tra 16 khung ở ngưỡng alpha 128: số hàng có nhiều đoạn đặc giảm từ 14 xuống 0; mọi pixel trên biên từng ô đều alpha 0, không bị cắt. Đã xem sheet, đồng bộ PNG nhúng demo và export; không thay sprite nguồn hoặc thời gian bay.
