# PopupMoreLife — popup hết mạng

Trạng thái 2026-11-23. Làm theo mẫu [DailyLoginPopup.md](DailyLoginPopup.md) (popup kế thừa `FoodPop.UICanvasPrime`, mở qua `FoodPop.UIManager`).

Kiểm chứng: biên dịch không lỗi, render prefab trong Editor khớp mockup (dữ liệu mẫu). **Chưa** chạy Play Mode: chưa mở popup thật từ Home/ingame, chưa kiểm tra đồng hồ đếm, mua bằng Coin và Watch.

## Kiến trúc

- Script `Assets/HotPot/Scripts/UI/PopupMoreLife.cs` (`FoodPop.PopupMoreLife : UICanvasPrime`).
- Prefab `Assets/HotPot/Prefabs/UI/MoreLife/PopupMoreLife.prefab`, đăng ký trong `Assets/HotPot/Config/UI/UIRef_Home.asset` (Home) và `UIRef.asset` (ingame).
- Mở bằng `FoodPop.UIManager.Instance.OpenUI<FoodPop.PopupMoreLife>()` từ `LifeDisplay.OpenBuy` (nút + mạng, bỏ qua khi vô hạn/đầy) và `PlayButton.OnClick` (bấm Play khi không đủ mạng).
- Dữ liệu: `HeartFeature` — `GetHeartDisplay()` trả (số mạng, thời gian hồi kế tiếp / "FULL"), `AddHeart()`; giá lấy từ `HeartConfig.refillGoldCost` (900). Không dùng `GameManager.Config.RefillLivesPrice`.

Luồng:
- `OnEnable`: cache profile + `HeartFeature`, nối 3 nút, chạy coroutine làm mới số mạng/đồng hồ mỗi giây (`WaitForSecondsRealtime`, vẫn chạy khi pause). `OnDisable` dừng coroutine và gỡ listener.
- `Open()`: hiện giá, số mạng, đồng hồ → `AnimateOpen()`.
- Nút xu (`BtnBuyCoin`): `playerData.TryBuyFullHearts(price, "more_live")` (trừ xu + hồi đầy + lưu một lần). Thành công → âm `ItemBought`, đóng; thất bại → `Owner.ShowShortMessage(...)` (ingame có thanh thông báo; Home chưa gán `shortMessage` nên không hiện).
- Nút Watch: tạm gọi thẳng `OnWatchAdsSuccess()` (chưa có SDK) → `AddHeart()`, `RecordAdsReward()`, đóng. Khi nối SDK chỉ cần gọi `OnWatchAdsSuccess` từ callback quảng cáo thành công.
- Nút X: `CloseWithAnimation()`. Mọi nút rung bằng `PlayHaptic()` và có `UIButtonScaleEffect`.

## Cấu trúc prefab

```
PopupMoreLife     Canvas Overlay order 30000 · CanvasScaler 1080×1920 · GraphicRaycaster · CanvasGroup · PopupMoreLife (card = Root)
├─ BlackBG
└─ Root
   ├─ PopupBG                 UI/popup/popup.png
   │  ├─ RootButtons         HorizontalLayoutGroup
   │  │  ├─ BtnWatchAds      UI/button/yellow bt.png · icon_ads (UI/popup/ads ic.png) bên trái · Text_Watch "Watch" · icon_live (tắt)
   │  │  └─ BtnBuyCoin       UI/button/bt green claim.png · icon_coin (Ingame/ic_coin.png) · Text_Price (priceText)
   │  └─ MoreLife            UI/popup/popup bst 2.png
   │     ├─ Text_NextLife    "Time to next life:"
   │     ├─ Icon_Heart       UI/popup/live 0.png
   │     ├─ Text_HeartCount  heartCountText
   │     └─ TimeBg           UI/popup/time bg.png · TimeIcon (time ic.png) · Text_CoolDown (cooldownText)
   ├─ TitleRibbon            UI/popup/header ribbon.png · Text_Title "Out Of Lives!"
   └─ BtnClose               UI/button/exit.png
```

Sprite đều trong `Assets/HotPot/Art/Sprites/UI/...` (và `Ingame/ic_coin.png`), kích thước bội số 4. Tên object cũ chép từ Revive đã đổi: `Text_InfoOutOfSlot` → `Text_NextLife`, `icon_out_of_slot` → `Icon_Heart`, `Text_InfoOutOfLive` → `Text_HeartCount`, `BtnReviveCoin` → `BtnBuyCoin`.

## Còn lại

- `_Hoang/Scripts/Runtime/UI/Popup/PopupMoreLives.cs`, `PopupId.MoreLife` và `GameManager.Config.RefillLivesPrice` không còn được popup này dùng.
- Hết xu ở Home chưa có phản hồi (Home không có `ShortMessageView`, chưa nối Shop).
- Chưa nối SDK quảng cáo cho Watch.
