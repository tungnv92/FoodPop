# IAP (package `Assets/Packages/IAP`)

Mua trong ứng dụng bằng Unity IAP v5 (`com.unity.purchasing` 5.4.4, `StoreController`). Viết lại từ `MyIAPManager` của car-game ngày 2026-11-25; đã xóa module IAP cũ `Assets/_Toolkit`.

## File

| Loại | Đường dẫn |
|---|---|
| Manager | `Runtime/IAPManager.cs` (`GamePackages.IAPManager`, kèm enum `LocationType`) |
| Config sản phẩm | `Runtime/IAPProductConfig.cs` (`GamePackages.IAPProductConfig`, kèm enum `ProductPackType`); trước đây là `FoodPop.UIShopPackConfig` / `ShopPackType` (đổi tên, giữ GUID nên asset cũ vẫn dùng được) |
| Prefab | `Prefabs/IAPManager.prefab` (manager + Canvas `IAP Blocker`), đặt trong scene đầu tiên (`HotPot/Scenes/Load.unity`) |
| Sprite | `Sprites/LoadingRing.png` (vòng loading tạm, 128×128) |
| Asset config của FoodPop | `Assets/HotPot/Config/Shop/*.asset` (14 gói; dữ liệu riêng của game nên không nằm trong package) |

Phụ thuộc: `Packages/Common` (`ProfileManager`, `PlayerDataManager`, `ResourceGame`, `ResourceType`), `FeatureModules` (`IRecordTracking`, tùy chọn), DOTween, và các class Tangle do Unity IAP sinh ra trong `Assets/Scripts/UnityPurchasing/generated/` (`AppleTangle`, `AppleStoreKitTestTangle`, `GooglePlayTangle`; vị trí mặc định của Unity, không di chuyển). Dùng cho game khác: copy thư mục `Packages/IAP`, chạy Obfuscator (xem bên dưới) để có các file Tangle, đặt prefab vào scene đầu tiên, tạo asset `IAPProductConfig` (menu `Create > GamePackages > IAP > IAPProductConfig`) và kéo vào `packConfigs`.

## Kiểm tra receipt

- Tạo file Tangle: bật dịch vụ ở **Project Settings > Services > In-App Purchasing** (công tắc góc trên phải; cần liên kết Unity Cloud Project). Khối **Apple Configuration** → **Obfuscate Apple License Key**. Mục **Google Play** → phần **Receipt Obfuscator (client-side)** → dán License key → **Obfuscate License Key**.
- Hiện trạng (2026-11-25): `AppleTangle` đã có dữ liệu; `GooglePlayTangle` được tạo với ô key trống nên `IsPopulated = false`. **Trước khi build Android lên store phải dán License key thật** (Google Play Console > app > Monetize setup > Licensing, chuỗi base64 bắt đầu bằng `MIIBIjAN...`) rồi bấm Obfuscate lại; không cần sửa code.
- `CreateValidator` (trong `Start`) tạo một `CrossPlatformValidator` từ dữ liệu Tangle. Trên Android khi `GooglePlayTangle.IsPopulated` là false thì ghi warning, và `IsValidReceipt` chấp nhận mọi receipt Google Play (không chặn giao dịch). Apple: `CrossPlatformValidator` chỉ kiểm tra bằng `AppleTangle` khi máy dùng StoreKit 1 (iOS dưới 15); StoreKit 2 đã được Apple xác thực nên trả về hợp lệ.
- Receipt không hợp lệ trong `OnPurchasePending`: không trao gói, không `ConfirmPurchase`; nếu là giao dịch đang mua thì tắt blocker và gọi `failed`. Trong `OnPurchasesFetched`, đơn có receipt không hợp lệ bị bỏ qua khi khôi phục Xóa quảng cáo.
- Đây là kiểm tra phía client; không thay thế kiểm tra phía server.

## IAPProductConfig

| Field | Ý nghĩa |
|---|---|
| `packType` (`ProductPackType`) | RemoveAds, RemoveAdsPack, SmallPack, MediumPack, LargePack, CoinPack, StarterPack, RevivePack, BoosterPack. RemoveAds/RemoveAdsPack → xóa quảng cáo (`RemovesAds`), StarterPack → đánh dấu đã mua Starter Pack |
| `packName`, `productId`, `productType`, `price`, `rewards` | Xem [ShopPopups.md](ShopPopups.md#config-gói-iapproductconfig) |

## API

| Hàm | Hành vi |
|---|---|
| `GetPrice(config)` | Giá theo store; store chưa sẵn sàng hoặc không có sản phẩm thì `$price` trong config |
| `Buy(config, location, success, failed)` | Mua một gói. Đang mua thì bỏ qua. Store chưa sẵn sàng → bắt đầu kết nối lại và gọi `failed` |
| `RestorePurchases(done)` | Chỉ dùng cho nút Restore trên iOS (PopupSetting). `StoreController.RestoreTransactions` sau blocker; thành công thì `FetchPurchases` để áp dụng lại Xóa quảng cáo. `done` luôn được gọi đúng một lần: đang có giao dịch khác → `done(false)` ngay; store chưa sẵn sàng → kết nối lại, `done(false)`; mất kết nối khi đang restore → `done(false)` |

## Luồng

- `Start`: tạo `StoreController`, đăng ký callback một lần, `Connect` rồi `FetchProducts` từ `packConfigs`. **Thêm gói mới phải thêm asset vào `packConfigs`.**
- `OnPurchasePending`: ghi `IRecordTracking.RecordIAP`, `GrantPack` áp dụng quyền theo `packType` (`OnBoughtRemoveAds`, `OnBoughtStarterPack`) và cộng `rewards` qua `PlayerDataManager` (Coin, `AddUnlimitedLiveTime` theo phút, `OnAddBooster`; mỗi lần cộng đều tự lưu), **rồi mới** `ConfirmPurchase`. Crash giữa lúc trao và xác nhận thì store giao lại đơn ở lần mở sau, không mất gói đã trả tiền.
- Chỉ đơn của sản phẩm đang mua (`purchasingProductId`) mới tắt blocker và gọi `success`, placement `iap_<location>`. Đơn cũ store giao lại (ví dụ lần trước app bị tắt giữa chừng) vẫn được trao với placement `iap_pending` nhưng không đụng tới giao dịch đang chạy. Đơn không có config thì ghi lỗi và vẫn xác nhận.
- `OnPurchasesFetched`: chỉ khôi phục Xóa quảng cáo cho gói `RemovesAds`, không cộng lại thưởng.
- `OnPurchaseFailed` → gọi `failed`. `OnPurchaseDeferred` (Ask to Buy, thanh toán chờ) → mở khóa mua; đơn được giao sau qua `OnPurchasePending`. `OnStoreDisconnected` → store chưa sẵn sàng, giao dịch hoặc restore đang chạy báo lỗi qua đúng callback của nó (`failed` của `Buy`, `done(false)` của restore); lần `Buy` sau tự kết nối lại.
- Blocker: `SetPurchasing(bool)` bật/tắt cờ đang mua cùng Canvas `IAP Blocker` (Overlay, sorting 32000, nền đen 50% chặn bấm, vòng loading xoay bằng DOTween theo thời gian thực, `spinDuration` mặc định 1 s). Bật khi gửi yêu cầu mua/restore, tắt ở Pending/Failed/Deferred/Disconnected và khi restore xong. Không có thời gian hiện tối thiểu.
- Editor (`Application.isEditor`): không kết nối store; `Buy` trao gói ngay, blocker không hiện, Restore luôn báo thất bại. Bản build luôn đi qua store.

## Chưa có

- License key Google Play thật trong `GooglePlayTangle` (xem phần Kiểm tra receipt).
- Popup mua thành công/thất bại, tracking SDK/Firebase/AppsFlyer, hiệu ứng bay thưởng.

## Kiểm chứng

- Đã kiểm tra: Unity compile không lỗi; 14 asset giữ đúng `packType` sau khi đổi tên; scene Load giữ 14 config và blocker sau khi chuyển thành prefab.
- Đã kiểm tra: compile không lỗi sau khi thêm kiểm tra receipt và có đủ 3 file Tangle.
- Chưa kiểm tra: kiểm tra receipt trên thiết bị thật (iOS StoreKit 1/2, Android sau khi có key), mua thật, hủy, Ask to Buy, mất mạng giữa chừng, Restore trên thiết bị; blocker chưa được xem chạy thật; trên iOS màn mất kết nối của `InternetConnection` có hiện sau hộp thoại mua của Apple không.
