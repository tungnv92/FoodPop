# Bàn giao CoinAttraction — 18/11/2026

## Trạng thái

- Người dùng đã duyệt sprite và demo. Đã tích hợp vào WinPopup bằng nested prefab độc lập.
- Toàn bộ phần do game sở hữu nằm tại `Assets/HotPot/Packages/CoinAttraction/`: runtime, prefab, sprite, demo, generator và menu export.
- Tài liệu triển khai, API, thông số, cách chỉnh và export: [CoinAttraction.md](../Features/CoinAttraction.md). Luồng cộng/lưu thưởng và chuyển màn: [UI.md](../Features/UI.md#canvaswin).
- Đã sửa glow bị trống bằng Sprite riêng trong package; bỏ ràng buộc Reward để vị trí phát chỉnh được trực tiếp. WinPopup giữ offset `(0, -327)` tại thời điểm tách.
- Đã thêm nhịp rơi nhẹ giữa bung và hút lên ví. Không thay đổi luồng cộng/lưu thưởng.
- Chưa commit; workspace còn các sửa đổi gameplay/tài liệu khác của người dùng, không commit chung ngoài phạm vi.

## Đã kiểm chứng

- Unity biên dịch thành công; binding WinPopup trỏ đúng prefab mới, glow Sprite hợp lệ, điểm phát không bị constraint ghi đè.
- Play Mode probe prefab độc lập: `Play(target)`, timeScale=0, đủ 10 xu đến ví, nảy/lóe, velocity rơi trước hút, disable reset/xóa hạt và replay đủ 10 xu. Không gọi claim hoặc ghi profile. Kết quả: `output/CoinFlight/RuntimeCheck.txt`; probe: `tmp/coin_demo/ProbeWinCoins.cs`.
- Generator trong package tái tạo sheet đã duyệt khớp byte. Demo được kiểm tra bằng canvas giả lập: sprite nhúng khớp PNG và chuyển pha rơi/bay liên tục; đây không phải kiểm tra chuyển động bằng mắt trong browser.
- Đã export `output/CoinFlight/CoinAttraction.unitypackage`: 42 asset, kèm đủ runtime/editor ParticleImage và assembly package; không chứa code game, WinPopup, DOTween hoặc mã nguồn Unity UI. Danh sách: `output/CoinFlight/PackageContents.txt`.

## Chưa kiểm chứng

- Chưa import vào project Unity khác hoặc build Android.
- Probe không kiểm tra giao dịch Next/Watch thực tế; không diễn giải kết quả probe thành đã kiểm tra lưu thưởng/đổi màn.
- Chưa quay/xem trực tiếp chuyển động bản package cuối trong browser; video tham chiếu đã xem chậm tại đoạn 36,9–38,3 giây: [Royal Match](https://www.youtube.com/watch?v=HictLcfbvDU&t=37s).

## Cập nhật kết thúc chính diện — 18/11/2026
- Xu bay đã chuyển từ FPS + random startFrame sang Lifetime + hai curve pha đầu biến thiên, cùng giữ frame 0 trong 10% lifetime cuối. Demo cũng giữ chính diện trước khi đến ví; slider FPS chỉ điều khiển xu xem thử riêng.
- Đã kiểm tra 404 phép tính end-frame của cấu hình WinPopup và 40 pose cuối trong demo. Không đổi sprite sheet, thời gian bay hoặc cộng thưởng. Xem [CoinAttraction.md](../Features/CoinAttraction.md#chuyển-động).

## Tăng độ dày lần hai — 18/11/2026
- Tăng `ThickRatio` 0.169 → 0.2197 (+30% so với bản trước, +69% so với 0.13 ban đầu). Đã sinh lại sheet và đồng bộ demo; giữ pitch 0°, GUID, chuyển động và kết thúc chính diện.

## Lộ cạnh ở chính diện — 18/11/2026
- Dùng phép chiếu chiều sâu xiên DepthX=-0.12, DepthY=0.32 để cạnh vàng lộ dưới/phải ở frame chính diện mà không nén mặt tròn hoặc thêm góc nghiêng. Giữ thickness 0.2197 và kết thúc frame 0. Đã sinh lại sheet và đồng bộ demo.

## Sửa đường bao mặt/cạnh — 18/11/2026
- Sửa alpha mặt ảnh làm hở đường nối với cạnh 3D: dùng silhouette khối trụ và composite màu mặt lên nền vàng đặc. Kiểm tra 16 khung giảm từ 14 hàng alpha đặc bị tách đoạn xuống 0, không chạm biên ô. Sinh lại sheet, demo và package; giữ chuyển động/kết thúc chính diện.
