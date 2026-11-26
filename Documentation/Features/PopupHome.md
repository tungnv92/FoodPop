# PopupHome — màn Home

Trạng thái đối chiếu 2026-11-25. Dựng theo mockup màn Home (ảnh tham khảo người dùng gửi, không đưa vào project). Popup kế thừa `FoodPop.UICanvasPrime` như [DailyLoginPopup.md](DailyLoginPopup.md), nhưng là **màn nền** toàn màn hình.

Kiểm chứng 23/11: đối chiếu script, prefab, scene và UIRef; Unity biên dịch sạch. Đã chạy Play Mode probe riêng taskbar cho nền chọn dùng chung và scale icon; chưa thử luồng vào màn chơi hoặc mua IAP. Instance `PopupHome` được đặt sẵn (đang bật) trong `Assets/HotPot/Scenes/Home.unity` dưới `UIManager (FoodPop)/Root`, không cần bước tạo hay mở; `OnEnable` tự hiện avatar và level. Scene Home không còn `HomePanel` / `UIManager` cũ của `_Hoang` (prefab `_Hoang/Prefabs/UI/UI Manager .prefab` không nằm trong scene nào và đã xóa ngày 25/11, nên `global::UIManager.Ins` = null). Chưa kiểm tra toàn bộ màn Home trong Play Mode.

## File

| Loại | Đường dẫn |
|---|---|
| Script | `Assets/HotPot/Scripts/UI/Home/PopupHome.cs` (`FoodPop.PopupHome : UICanvasPrime`) |
| Khối mạng | `Assets/HotPot/Scripts/UI/Home/UIHome_Life.cs` (`FoodPop.UIHome_Life`, gắn trên `Group_Life`) |
| Khối Coin | `Assets/HotPot/Scripts/UI/Home/UIHome_Coin.cs` (`FoodPop.UIHome_Coin`, gắn trên `Group_Coin`) |
| Nút Daily reward | `Assets/HotPot/Scripts/UI/Home/UIHome_DailyReward.cs` (`FoodPop.UIHome_DailyReward`, gắn trên `BtnDailyReward`) |
| Nút Daily login | `Assets/HotPot/Scripts/UI/Home/UIHome_DailyLogin.cs` (`FoodPop.UIHome_DailyLogin`, gắn trên `BtnDailyLogin`) |
| Nút No ads | `Assets/HotPot/Scripts/UI/Home/UIHome_NoAds.cs` (`FoodPop.UIHome_NoAds`, gắn trên `BtnNoAds`) |
| Nút Starter pack | `Assets/HotPot/Scripts/UI/Home/UIHome_StarterPack.cs` (`FoodPop.UIHome_StarterPack`, gắn trên `BtnStarterPack`) |
| Nút Cài đặt | `Assets/HotPot/Scripts/UI/Home/UIHome_Setting.cs` (`FoodPop.UIHome_Setting`, gắn trên `BtnSetting`) |
| Popup Cài đặt | `Assets/HotPot/Scripts/UI/Home/PopupSetting.cs` (`FoodPop.PopupSetting : UICanvasPrime`), prefab `Assets/HotPot/Prefabs/UI/Setting/PopupSetting.prefab`, đăng ký trong `UIRef_Home.asset` |
| Prefab | `Assets/HotPot/Prefabs/UI/Home/PopupHome.prefab`; instance đặt sẵn trong scene Home, **không** đăng ký trong `UIRef_Home` |

## Màn nền (`IsScreen`)

`UICanvasPrime.IsScreen` (mặc định `false`); `PopupHome` trả `true`. `UIManager.HideAll()` (gọi mỗi khi mở popup qua `OpenUI`) bỏ qua canvas có `IsScreen`, nên Home vẫn hiện bên dưới popup. Canvas Home có sort order 0, popup 30000.

`UIManager.InitCanvasData()` nhận mọi `UICanvasPrime` đã đặt sẵn dưới object manager trong scene (gán `Owner`, thêm vào danh sách instance) nên `GetUICanvas<T>()` / `OpenUI<T>()` dùng lại instance đó thay vì tạo mới từ `UIRef`. PopupHome đang bật sẵn nên hiện ngay khi vào scene; đã bỏ `PopupHome.Show()`, `Open()` và `HomeSceneEntry.cs`.

## Cấu trúc prefab

```
PopupHome            Canvas Overlay order 0 · CanvasScaler 1080×1920 match 0 · GraphicRaycaster · CanvasGroup · PopupHome
├─ Img_Background    UI/Home/home bg.png · AspectRatioFitter EnvelopeParent
├─ Group_Top         neo đỉnh
│  ├─ BtnAvatar      UI/Home/icon bg.png · Icon_Avatar (avatar theo hồ sơ)
│  ├─ Group_Life     UIHome_Life · Img_Bg (coin bg) · Icon_Heart (Home/live ic) · Text_HeartCount · Text_Timer · Icon_AddLife (Ingame/btn_plus, chỉ hình). Group_Life có Image trong suốt + Button (không có hiệu ứng scale): bấm cả khối
│  ├─ Group_Coin     UIHome_Coin · Img_Bg (coin bg) · Icon_Coin (Home/coin ic) · Text_Coin · Icon_AddCoin (Ingame/btn_plus, chỉ hình). Group_Coin có Image trong suốt + Button (không có hiệu ứng scale): bấm cả khối
│  └─ BtnSetting     Ingame/btn_setting · Icon_Setting (Ingame/ic_setting)
├─ Group_Left        neo trên-trái: BtnDailyReward, BtnDailyLogin
├─ Group_Right       neo trên-phải: BtnNoAds, BtnStarterPack
│     mỗi nút: icon bg · Icon · Img_NameBg (icon name) + Text_Name · Img_Badge (UI/Home/!.png)
├─ BtnPlay           Home/bt Play · Text_Play "PLAY" · Text_Level
├─ Group_Taskbar     Canvas con override sorting order 20 · Home/taskbar · neo đáy, y = chiều cao BottomBanner (220), cao 233 · HorizontalLayoutGroup · UIHome_Taskbar (tab đang chọn rộng hơn 30%)
│  ├─ Img_Selected   một nền chọn dùng chung · ignoreLayout · trượt X và đổi rộng theo tab · cao 280
│  └─ Tab_Shop, Tab_Leaderboard, Tab_Home, Tab_Journey, Tab_Events
│        mỗi ô: Image trong suốt + Button (phủ cả ô) + UIButtonScaleEffect · LayoutElement · UIHome_Tab
│        ├─ Icon_Tab       ic shop / ic leaderboard / ic home / ic journey / ic lock (y 120, chọn thì 165)
│        └─ Text_Name      Shop / Rank / Home / Journey / Events (chỉ hiện khi được chọn)
└─ BottomBanner      neo đáy, cao 220 (chỗ cho banner quảng cáo)
```

Các nút riêng và ô tab có `UIButtonScaleEffect`; hai Button phủ khối mạng/Coin không có hiệu ứng scale. Đặt tên theo quy ước `Text_`/`Icon_`/`Img_`/`Btn`/`Group_` (xem [ShopPopups.md](ShopPopups.md)).

## Shop như tab của Home

Home giữ Canvas order 0; `PopupShop.Open()` nhận biết Home đang mở qua `Owner.IsUICanvasOpened<PopupHome>()`, chuyển Shop về order 10 và ẩn nút đóng. `Group_Taskbar` có Canvas con `overrideSorting=true`, order 20 nên vẫn hiện và bấm được trên Shop. Khi mở popup khác order 30000, thanh tab nằm dưới popup đó.

Cả nút Coin trên Home và tab Shop đều dùng cơ chế này: nút Coin gọi `ShowCoins()` để cuộn thẳng tới gói Coin, đồng thời Shop được chọn. Bấm Home để quay lại; mở Shop ngoài màn Home giữ order popup gốc (prefab 30000) và hiện nút đóng. Xem [ShopPopups.md](ShopPopups.md).

## Dữ liệu và nút

Chấm báo `Img_Badge` (`UI/Home/!.png`) của các nút bên có `UIBadgeEffect` (`Assets/HotPot/Scripts/UI/Home/UIBadgeEffect.cs`): khi được bật thì nảy từ 0 lên (OutBack 0.3s), sau đó lặp nhịp phóng 1.2 lần kèm lắc 15° rồi nghỉ 1.2s (chỉnh `pulseScale`, `wobbleAngle`, `interval` trong Inspector), chạy theo thời gian độc lập; tắt thì dừng và trả scale/góc. Chấm chỉ bật khi có quà nhận được: Daily reward / Daily login do `UIHome_DailyReward` / `UIHome_DailyLogin` bật theo `IsGoodToClaim()`; No ads / Starter pack tắt sẵn.

| Phần | Nguồn / hành động |
|---|---|
| Avatar | `UserProfileFeature.GetAvatarSprite(AvatarId)`, làm mới trong `OnEnable` khi màn được bật; nút chưa nối; UI Profile cũ đã xóa, module dữ liệu và sprite vẫn giữ |
| Mạng | `UIHome_Life` tự xử lý: `OnEnable` hiện ngay và làm mới mỗi giây (thời gian thực) qua `HeartFeature.GetHeartDisplay()`, đầy mạng hiện "Max"; bấm vào bất kỳ đâu trong khối (`lifeButton` trên `Group_Life`) thì rung rồi mở `PopupMoreLife` (bỏ qua khi vô hạn/đầy). `PopupHome` không còn giữ ref hay logic mạng |
| Coin | `UIHome_Coin` tự xử lý: `OnEnable` đăng ký `PlayerDataManager.RegisterResourceChange(Coin)` và hiện số dư, `OnDisable` gỡ; bấm vào bất kỳ đâu trong khối (`coinButton` trên `Group_Coin`) thì rung rồi `PopupShop.ShowCoins()`. `PopupHome` không còn giữ ref hay logic Coin |
| Cài đặt | `UIHome_Setting`: bấm có scale, rung rồi `OpenUI<PopupSetting>()`. `PopupSetting` dựng từ bản sao `PausePopup` (bỏ nút Home/Restart; `PopupBG` kiểu Sliced cao 640, viền sprite `popup setting.png` = 120): 3 dòng Music/Sound/Vibration bật/tắt qua `PlayerDataManager.SetMusicEnabled` / `SetSoundEnabled` / `SetVibrationEnabled`, theo dõi `PlayerDataManager.StateChanged`; nút `BtnRestorePurchase` (`restoreButton`) chỉ hiện khi build iOS (`#if UNITY_IOS`), bấm gọi `IAPManager.RestorePurchases` rồi báo kết quả bằng `ShowShortMessage`; mở/đóng bằng `AnimateOpen` / `CloseWithAnimation`. Hệ cũ không có prefab `PopupSettings` nên đây là popup mới; đã xóa `SettingButton.cs` cũ (không nơi dùng) |
| Daily reward | `UIHome_DailyReward` tự xử lý: chấm "!" hiện khi `DailyRewardFeature.IsGoodToClaim()`, kiểm tra lại mỗi giây (thời gian thực) vì quà miễn phí mở theo thời gian chờ. Bấm có hiệu ứng scale (`UIButtonScaleEffect`), rung rồi mở `PopupDailyReward` (đã chuyển sang `UICanvasPrime`, xem mục DailyReward trong [FeatureModules.md](FeatureModules.md)) |
| Daily login | `UIHome_DailyLogin` tự xử lý: chấm "!" hiện khi `DailyGiftFeature.IsGoodToClaim()`, kiểm tra lại mỗi giây (theo kịp lượt nhận và sang ngày). Bấm có hiệu ứng scale, rung rồi `OpenUI<PopupDailyLogin>()` |
| No ads | `UIHome_NoAds` tự xử lý: tự ẩn khi `HasRemoveAds` (cập nhật ngay qua `Observer.OnBoughtRemoveAdsAction`); bấm có scale, rung rồi `OpenUI<PopupNoAds>()` (xem ShopPopups.md); chấm "!" tắt sẵn (nút mua, không có quà để nhận) |
| Starter pack | `UIHome_StarterPack` tự xử lý: tự ẩn khi `PlayerDataManager.HasStarterPack` (cập nhật ngay qua `Observer.OnBoughtStarterPackAction`, phát trong `PlayerDataManager.OnBoughtStarterPack` sau khi lưu; `IAPManager` gọi hàm này khi mua `beginnerpack`). Bấm có scale, rung rồi `OpenUI<PopupStarterPack>()` (xem ShopPopups.md); nhãn "Starter pack"; chấm "!" tắt sẵn (nút mua, không có quà để nhận) |
| Play | hồ sơ sẵn sàng + đủ mạng → `LoadSceneManager.Ins.LoadScene(SceneId.Game)`; thiếu mạng → `PopupMoreLife`. `Text_Level` = "Level N" |
| Thanh tab | `UIHome_Taskbar` giữ 5 tab và theo dõi `PopupShop.VisibleChanged`: Shop đang mở → chọn Shop, đã đóng → chọn Home. Bấm Shop rung rồi mở `PopupShop.Show()` nếu chưa mở; bấm Home rung và `CloseUI<PopupShop>()`. Leaderboard/Journey/Events chỉ rung, chưa có màn. `UIHome_Taskbar` trượt nền chọn chung sang tab đích; `UIHome_Tab.SetSelected(bool, duration)` đồng thời chuyển độ rộng 1↔1.3, icon Y 120↔165, scale icon 90%↔100% và fade tên. Trạng thái đầu đặt ngay; các lần đổi chạy OutCubic unscaled trong `UIHome_Taskbar.transitionDuration` (0.22 s). Bấm đổi nhanh tiếp tục từ trạng thái đang hiển thị; giữ nguyên tab không chạy lại tween. Thứ tự con trong `Group_Taskbar` quyết định thứ tự tab. |

## Hiệu ứng chuyển tab

Taskbar chỉ có **một** `Img_Selected`, là con trực tiếp của `Group_Taskbar` và có `LayoutElement.ignoreLayout=true`, nằm trước các tab trong thứ tự con để hiển thị phía sau icon/chữ. Không còn nền riêng trong từng tab.

Khi đổi tab, nền trượt ngang từ vị trí đang hiển thị đến tâm tab mới, chiều rộng đổi theo ô đích. **Giữ nguyên chiều cao 280 và Y=0**, không giới hạn chiều cao bằng ô tab. Cập nhật qua `Canvas.willRenderCanvases` để bám layout sau khi các ô đổi độ rộng hoặc canvas đổi kích thước.

Icon tab không chọn ở 90% scale thiết kế; tab được chọn ở 100%. Icon nâng/hạ Y, đổi scale và tên fade cùng thời gian nền trượt. Thời gian chỉnh ở `UIHome_Taskbar.transitionDuration`; đặt 0 để đổi ngay. `UIHome_Tab` cache scale icon và alpha chữ một lần, không phụ thuộc thứ tự Awake cha/con.

Khi đổi nhanh, tween mới bắt đầu từ hình đang hiển thị; khi disable, hủy tween và đưa icon/chữ về trạng thái đích. Khi bật lại, taskbar đọc trạng thái Shop để đặt lại đúng tab ngay. Không thay luồng Shop/Home hoặc thêm màn cho các tab chưa triển khai.

Kiểm chứng Play Mode đã thực hiện ngày 23/11: ở timeScale=0, dùng bản sao taskbar với duration 2 s để lấy mẫu tin cậy khi editor chậm (prefab thật vẫn 0.22 s); xác nhận nền có vị trí trượt trung gian, đảo chiều không nhảy, chiều cao giữ 280, chiều rộng khớp tab ở 720/1080/1440, icon chọn 100% và icon thường 90%, disable/bật lại reset đúng. Không gọi handler điều hướng hoặc mua hàng. Kết quả: `output/HomeTabs/TransitionCheck.txt`; probe: `tmp/home_tabs/ProbeTabs.cs`.

### Tham số chỉnh trong Inspector

| Nơi chỉnh | Field / giá trị | Tác dụng |
|---|---|---|
| `Group_Taskbar` → `UIHome_Taskbar` | `selectionBackground` | Gán duy nhất RectTransform `Img_Selected` chung, không gán nền riêng từng tab |
| `Group_Taskbar` → `UIHome_Taskbar` | `transitionDuration = 0.22` | Cùng thời gian cho nền trượt và hiệu ứng icon/chữ; chạy unscaled với OutCubic |
| `Img_Selected` → RectTransform | Height 280, Y 0 trong prefab hiện tại | Code giữ nguyên chiều cao/Y đã thiết kế, chỉ cập nhật X và width. Có thể đổi chiều cao tại đây |
| `Img_Selected` → LayoutElement | `ignoreLayout = true` | Không tính nền chọn như một ô trong HorizontalLayoutGroup |
| Mỗi tab → `UIHome_Tab` | `selectedWidthRatio = 1.3` | Tab chọn rộng hơn 30% so với tab thường |
| Mỗi tab → `UIHome_Tab` | `iconNormalY = 120`, `iconSelectedY = 165` | Mức hạ/nâng icon theo trạng thái |
| `UIHome_Tab.ApplySelection` | Hệ số 0.9 → 1.0 | Icon thường 90%, icon chọn 100% **so với scale thiết kế đã cache**, không phải giá trị scale tuyệt đối |

Không chỉnh X/width nền để lưu vị trí cố định: taskbar tính lại theo layout. Scale nhấn nút của `UIButtonScaleEffect` là hiệu ứng riêng trên ô tab, không thay thế hệ số chọn của `Icon_Tab`. Ba tab Leaderboard/Journey/Events hiện vẫn chỉ rung và không đổi màn/chọn tab; chuyển chọn Shop/Home theo `PopupShop.VisibleChanged`.

Rà soát ngày 25/11: đã đối chiếu hai script Home tab, ref `selectionBackground`, một nền chọn chung và duration 0.22 trong prefab. Không chạy lại Play Mode hoặc biên dịch trong lượt cập nhật tài liệu này.

## Sprite đang gán trong prefab

| Phần | Đang dùng |
|---|---|
| Icon Daily reward | `Assets/HotPot/Art/Sprites/UI/Home/ic daily rew.png` |
| Icon Daily login | `Assets/HotPot/Art/Sprites/UI/Home/ic daily.png` |
| Icon No ads | `Assets/HotPot/Art/Sprites/UI/Home/ic ads.png` |
| Icon Starter pack | `Assets/HotPot/Art/Sprites/UI/Home/ic pack.png` |
| Tab thứ 5 | `UI/Home/ic lock.png` |

## Còn lại

- Nút Avatar chưa làm gì; PopupProfile cũ đã xóa, chưa có yêu cầu tạo UI thay thế.
- `IAPManager` mới (2026-11-25) không còn gọi `global::UIManager.Ins` hay popup PurchaseSuccess/PurchaseFail; luồng mua thật trên thiết bị chưa kiểm chứng. Chi tiết trong [ShopPopups.md](ShopPopups.md#còn-lại).
- Scene Home còn một instance `PopupShop` (tắt) đặt tay dưới `UIManager (FoodPop)/Root`; nay `UIManager` dùng chính instance này thay vì tạo từ `UIRef_Home`, nên sửa prefab PopupShop cần kiểm tra instance trong scene không ghi đè.
- Đầu bếp đang là ảnh tĩnh (thư mục `anim home sushi` đã bị xóa).
- `home bg.png` (1406×2340 → 1408×2340, Max Size 4096) và `!.png` (52×53 → 52×56) đã thêm viền trong suốt cho chia hết cho 4 (chia đều hai bên, lệch tối đa 1px); ảnh gốc sao lưu ở `tmp/home_sprites_pad/originals/`.
- Chưa có SafeArea cho tai thỏ/thanh điều hướng.
