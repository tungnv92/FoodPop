# Popup cửa hàng: PopupShop

Trạng thái đối chiếu 2026-11-25. Làm theo mẫu [DailyLoginPopup.md](DailyLoginPopup.md): popup kế thừa `FoodPop.UICanvasPrime`, mở qua `FoodPop.UIManager`, không dùng `Resources`/`PopupId`.

Kiểm chứng: đã đối chiếu script, prefab, config và UIRef ngày 23/11; ngày 25/11 rà lại phần đồng bộ thanh tab với Home. Lượt cập nhật tài liệu 25/11 không chạy biên dịch, Play Mode hoặc mua IAP; các luồng runtime dưới đây được mô tả từ code.

## Vị trí file

| Loại | Đường dẫn |
|---|---|
| Script | `Assets/HotPot/Scripts/UI/Shop/PopupShop.cs`, `UI_IAP_Pack.cs` (namespace `FoodPop`) |
| Prefab popup | `Assets/HotPot/Prefabs/UI/Shop/PopupShop.prefab` |
| Prefab gói | `Assets/HotPot/Prefabs/UI/Shop/UIShop_Remove Ads.prefab`, `UIShop_Remove Ads Pack.prefab`, `UIShop_SmallPack.prefab`, `UIShop_MediumPack.prefab`, `UIShop_LargePack.prefab`, `CoinPack.prefab` |
| Sprite | `Assets/HotPot/Art/Sprites/UI/Shop/` |
| Logic IAP dùng chung | Package `Assets/Packages/IAP` (`IAPManager`, `IAPProductConfig`), xem [IAP.md](IAP.md). Đã xóa toàn bộ `Assets/_Toolkit` (2026-11-25); nút mua trong 2 prefab PopupRemoveAds đã unpack khỏi `Button Purchase.prefab` cũ |

Đã dời từ `_Hoang/Prefabs/UI/...`, `_Hoang/Prefabs/UI/Popup/Resources/PopupShop.prefab`, `HotPot/Prefabs/UI/NoAds|StarterPack/` và `_Hoang/Scripts/Runtime/UI/Popup/` (giữ `.meta`/GUID). Đã xóa `Shop Content.prefab` (không còn nơi dùng).

`PopupShop` được đăng ký trong `UIRef_Home.asset` và `UIRef.asset`.

Đã xóa (2026-11-23): prefab `PopupRemoveAdsPack` / `PopupStarterPack`, script `PopupRemoveAdsPack.cs`, `PopupStarterPack.cs`, `RemoveAdsButton.cs`, `StarterPackButton.cs`, `Shop.cs` (toolkit), luồng gợi ý tự mở `IAPManager.CheckSuggestRemoveAds` / `CheckSuggestStarterPack` và `HomePanel.CheckSuggestIapPacks`; field `Shop _shop` không dùng trong `ShopTab`. Gói Remove Ads / Remove Ads Pack vẫn mua được trong PopupShop; Starter Pack hiện không có UI.

## Mở popup

| Popup | Gọi từ |
|---|---|
| `PopupShop.Show()` (danh sách ở đầu) | `UIHome_Taskbar.OnClickShop()`; bỏ qua nếu Shop đã mở |
| `PopupShop.ShowCoins()` (mở thẳng phần gói Coin) | `UIHome_Coin.OnClickCoin()` và `LavaQuest/UI/Gold.ButtonAdd` |

## Luồng

- `OnEnable` cache manager, ghi sorting order gốc một lần, nối `closeButton`, đăng ký sự kiện mua Remove Ads và phát `VisibleChanged(true)`. `OnDisable` gỡ listener và phát `VisibleChanged(false)`. Home taskbar theo sự kiện này để chọn Shop/Home: một `Img_Selected` trượt ngang, chỉ đổi width theo tab và giữ height/Y thiết kế (prefab hiện tại 280/0); icon chuyển 90%↔100% scale thiết kế cùng nhịp unscaled 0.22 s. Hướng dẫn chỉnh Inspector và kiểm chứng xem [PopupHome.md](PopupHome.md#hiệu-ứng-chuyển-tab). Nút đóng có `UIButtonScaleEffect`; không dùng `UIAnimator` hay lời gọi `CloseButton` gắn trong Inspector.
- `PopupShop` là popup toàn màn hình: mở bằng `AnimateOpen(false)` (hiện ngay, không phóng to/mờ dần), nút đóng gọi thẳng `Close()` (không thu nhỏ). Khi mở: cuộn về đầu, gọi `Init()` cho từng `UI_IAP_Pack` trong `packs`, ẩn `Group_RemoveAds` + `Header_RemoveAds` nếu `playerData.HasRemoveAds`; `ShowCoins()` mở popup rồi `JumpToCoins()`: dựng lại layout, cuộn ngay để `Header_CoinPacks` (`coinSection`, con trực tiếp của `Content`) nằm ở đầu khung nhìn, giới hạn trong khoảng cuộn (phần Coin ở cuối nên thực tế cuộn tới đáy). Thử trong Editor với khung 1080×1920: nội dung cao 4082, vị trí cần cuộn 2841, giới hạn 2162. Lắng nghe `Observer.OnBoughtRemoveAdsAction` (phát trong `PlayerDataManager.OnBoughtRemoveAds` sau khi lưu) để ẩn mục Xóa quảng cáo ngay khi mua xong. Không còn dùng component `Shop` của toolkit.

Prefab: gốc có Canvas Overlay order 30000, CanvasScaler 1080×1920 match 0, GraphicRaycaster, CanvasGroup. `card` = `Root`.

### Hai chế độ hiển thị

| Ngữ cảnh | Sorting order | Nút đóng | Quay về Home |
|---|---|---|---|
| `Owner.IsUICanvasOpened<PopupHome>()` là true | `tabSortingOrder` mặc định 10; dưới Canvas taskbar order 20 | Ẩn | Bấm tab Home; `UIHome_Taskbar` gọi `CloseUI<PopupShop>()` |
| Không có Home đang mở, ví dụ mở từ ingame | Sorting order prefab đã cache (30000) | Hiện | Nút đóng đóng Shop ngay |

`ShowCoins()` cũng chọn chế độ theo trạng thái Home, không theo nơi gọi. Mở Coin trên Home sẽ chọn tab Shop; không giữ Home là tab đang chọn. Mở popup khác qua UIManager sẽ đóng Shop nhưng giữ màn nền Home. Scene Home có sẵn instance PopupShop tắt dưới manager; `InitCanvasData()` nhận instance đó và dùng lại trước khi cần tạo từ UIRef. Xem [PopupHome.md](PopupHome.md#shop-như-tab-của-home).

## Config gói (`IAPProductConfig`)

Mỗi gói có một asset riêng (không gộp chung như `Product Config Container` của toolkit). Script `Assets/Packages/IAP/Runtime/IAPProductConfig.cs` (trước là `UIShopPackConfig`, giữ GUID), asset trong `Assets/HotPot/Config/Shop/`. Tạo mới qua menu `Create > GamePackages > IAP > IAPProductConfig`.

| Field | Ý nghĩa |
|---|---|
| `packName` | Tên gói, ghi vào ô `packNameText` (`Pack Name` ở Remove Ads Pack, Small, Medium, Large); điền sẵn từ `ProductId.NameById` cũ, LargePack = "Large Pack" |
| `packType` (`ProductPackType`, trước là `ShopPackType`) | RemoveAds, RemoveAdsPack, SmallPack, MediumPack, LargePack, CoinPack, StarterPack, RevivePack, BoosterPack; RemoveAds/RemoveAdsPack xóa quảng cáo, StarterPack đánh dấu đã mua Starter Pack |
| `productId` | Id IAP, chuỗi nhập tay đúng như trên store (không còn dropdown `ProductId.Ids`) |
| `productType` | `Consumable` / `NonConsumable` (Unity IAP) |
| `price` | Giá gói (USD), nhập tay; hiện ở ô giá dạng `$0.00` khi store chưa sẵn sàng hoặc không có sản phẩm (`IAPManager.Instance.GetPrice(config)`), có store thì hiện giá theo store. Giá đề xuất hiện tại ở bảng dưới |
| `rewards` (`List<ResourceGame>`) | Phần thưởng riêng của gói, thứ tự tùy ý (UI ghép theo loại; mỗi loại chỉ nên có một dòng); `UnlimitedLives` tính bằng **phút** |


Asset (giá trị chép từ `Product Config Container` cũ, `UnlimitedLife` đổi giây → phút):

| Asset | productId | Thưởng | Giá đề xuất |
|---|---|---|---|
| RemoveAds | removeads (NonConsumable) | — | $2.99 |
| RemoveAdsPack | removeadspack (NonConsumable) | 1000 coin, 1h, mỗi booster x1 | $4.99 |
| SmallPack | smallpack | 5000 coin, mỗi booster x1, 2h | $4.99 |
| MediumPack | mediumpack | 10000 coin, mỗi booster x2, 2h | $8.99 |
| LargePack | bigpack | 25000 coin, mỗi booster x5, 6h | $19.99 |
| StarterPack | beginnerpack | 1000 coin, 30m, mỗi booster x2 | $1.99 |
| RevivePack | revivepack | 2000 coin, mỗi booster x1 (chưa có UI) | $1.99 |
| BoosterPack | boosterpack | mỗi booster x2 (chưa có UI) | $2.99 |
| CoinPack_1K … CoinPack_100K | 1kgold … 100kgold | 1000 / 5000 / 10000 / 25000 / 50000 / 100000 coin | $0.99 / 3.99 / 6.99 / 14.99 / 27.99 / 49.99 |

Booster cũ ánh xạ đúng theo `IAPManager.CollectReward` cũ: Booster1 → Clean, Booster2 → Shuffle, Booster3 → Pickup, Booster4 → AutoComplete. `productId` vẫn mang tiền tố `com.pixelblast.` của game cũ.

## Luồng mua theo config

- Luồng mua, restore, blocker và các trường hợp lỗi: xem [IAP.md](IAP.md). `IAPManager.packConfigs` chứa đủ 14 config; **thêm gói mới phải thêm asset vào danh sách này.**
- `UI_IAP_Pack` (đổi tên từ `MonetToolkit.Pack`, giữ GUID, chuyển vào `HotPot/Scripts/UI/Shop/`) giữ `config`, `purchaseButton`, `priceText`, `packNameText`, `locationType`, `rewardSlots`. Mỗi `RewardSlot` = (`type` loại tài nguyên, `amountText`). `Init()` hiện tên, giá rồi `ShowRewardAmounts()`: mỗi ô tìm phần thưởng **cùng loại** trong `config.rewards` (không phụ thuộc thứ tự) và ghi `GetNormalValue()`; config không có loại đó thì ẩn chữ số của ô. Phần thưởng không có ô vẫn được cộng khi mua, chỉ không hiện số. Bấm nút → `OnClickPurchase()` gọi thẳng `IAPManager.Instance.Buy(config, locationType)` (listener nối trong `OnEnable`, gỡ trong `OnDisable`).
- Đã xóa `ButtonPurchase` và `ButtonPurchaseSale` (cùng `Button Purchase Sale.prefab` không còn nơi dùng); `locationType` cũ được chép sang `UI_IAP_Pack` (các gói đang hiện trong Shop: `popup_shop`; `popup_beginner_pack` thuộc UI Starter Pack cũ đã xóa). Nút mua chỉ có `UIButtonScaleEffect`, không còn lời gọi gắn trong Inspector.
- Icon booster trong các gói đổi sang sprite FoodPop `HotPot/Art/Sprites/UI/Boosters/` (`sweep`, `shuffle`, `pickup`, `order`) theo thứ tự thưởng trong config.
- `PopupShop.packs` giữ 11 `UI_IAP_Pack`; 6 gói Coin trong `Group_CoinPacks` là instance `CoinPack.prefab`, mỗi instance gán config 1K…100K và tên `UIShop_CoinPack_<n>`. Component `Shop` (toolkit) đã bỏ khỏi prefab và `Shop.cs` đã xóa.
- Đã sửa lỗi cũ: mạng vô hạn trước đây cộng nguyên số giây vào hàm tính phút (3600 → 3600 phút).

Đối chiếu hiện tại: `PopupShop.packs` có 11 ref (2 gói Remove Ads, Small/Medium/Large và 6 gói Coin); thư mục Shop có 7 prefab nguồn còn tồn tại. Có 14 asset IAPProductConfig; Starter/Revive/Booster vẫn có config và đăng ký IAP nhưng chưa có UI riêng. Không dùng lại kết quả quét 13 pack/3 popup của bản đã xóa. Chưa thử Play Mode/mua hàng.

## Quy ước đặt tên object trong prefab gói

| Tiền tố | Dùng cho | Ví dụ |
|---|---|---|
| `Text_` | TextMeshPro | `Text_PackName`, `Text_Price`, `Text_Amount`, `Text_Desc`, `Text_Title`, `Text_Header`, `Text_Label` |
| `Icon_` | Ảnh biểu tượng | `Icon_Coin`, `Icon_Life`, `Icon_RemoveAds`, `Icon_Clock`, `Icon_BoosterClean` / `Shuffle` / `Pickup` / `AutoComplete` |
| `Img_` | Ảnh nền, trang trí | `Img_Light`, `Img_LifeBg`, `Img_TimeBg`, `Img_HeaderBg`, `Img_ShopBg`, `Img_Title`, `Img_Bg` |
| `Btn` | Nút | `BtnPurchase`, `BtnClose` |
| `Reward_` | Nhóm một phần thưởng (icon + số) | `Reward_Coin`, `Reward_Life`, `Reward_RemoveAds` |
| `Group_` | Nhóm bố cục | `Group_Rewards`, `Group_Boosters`, `Group_RemoveAds`, `Group_CoinPacks` |
| `Header_` | Tiêu đề mục trong PopupShop | `Header_RemoveAds`, `Header_SpecialPacks`, `Header_CoinPacks` |
| `FX_` | Hiệu ứng | `FX_Blink`, `FX_Particles` |

Icon booster đặt theo thứ tự thưởng trong config: Clean, Shuffle, Pickup, AutoComplete. Instance gói trong PopupShop tên `UIShop_<config>` (vd. `UIShop_CoinPack_5K`). Code không tìm object theo tên, mọi ref gán trực tiếp trong Inspector.

## Còn lại

- Luồng IAP chưa chuyển hoàn toàn sang FoodPop.UIManager: `HandlePurchaseComplete` gọi `global::UIManager.Ins.OpenPopup(PopupId.PurchaseSuccess)` trước khi tra config và cấp thưởng; đường lỗi gọi PurchaseFail, BoosterPack còn đóng MoreBooster qua manager cũ. Scene Home mới không có manager này. Đây là điểm tích hợp chưa hoàn tất, không thể coi bảng config/cấp thưởng trong tài liệu là giao dịch đã chạy thành công.
- `Product Config Container.asset`, `ProductConfig`, `RewardConfig`, `RewardType` không còn dùng cho mua hàng.
- Prefab còn phụ thuộc `_Hoang`: font `fredoka-one ... SDF` + material outline ở chữ giá, `TMP.prefab` của `_Hoang/1_SharedModules`, `Blink.prefab`, `Coin Display`.
- `HotPot/Prefabs/UI/NoAds/PopupRemoveAds.prefab` không còn nơi dùng (`UI_IAP_Pack` chưa gán config); `PopupId.Shop/RemoveAds/StarterPack` không còn được gọi (trừ `PopupMoreLives.cs` cũ).
