# PopupDailyLogin — mẫu chuẩn cho popup quà đăng nhập

Trạng thái ngày 2026-11-23. `Assets/Packages/DailyGift/Prefabs/PopupDailyLogin.prefab` là **hình mẫu** cho mọi PopupDailyLogin về sau: popup mới copy cấu trúc, cách gán sprite, cách nạp dữ liệu và luồng trạng thái ở đây. Logic feature (ngày, đã nhận, sang ngày mới) xem [FeatureModules.md](FeatureModules.md#dailygift-đã-chuyển).

Kiểm chứng: đã biên dịch không lỗi, render prefab trong Editor, chạy thử 3 trạng thái của Day 7 (chưa tới / hôm nay / đã nhận). **Chưa** chạy Play Mode ở scene Home: chưa kiểm tra mở popup thật, `Init`/`ShowReward` nạp config lúc chạy và nhận quà thật.

## 1. Kiến trúc

| Thành phần | Vai trò |
|---|---|
| `PopupDailyLogin : FoodPop.UICanvasPrime` | Popup. Mở bằng `FoodPop.UIManager.Instance.OpenUI<PopupDailyLogin>()` (gọi từ `ButtonDailyLogin`). Prefab được đăng ký trong `Assets/HotPot/Config/UI/UIRef_Home.asset`; scene Home có object `UIManager (FoodPop)` trỏ tới asset này. Không dùng `Resources`/`PopupId`. |
| `Day` | Một ô ngày 1–6, là instance của `Prefabs/UI/Day.prefab`. |
| `Day7 : Day` | Ô ngày 7 dạng hộp quà có nắp, dựng trực tiếp trong popup (không phải instance của `Day.prefab`). |
| `DailyGiftRewardConfig` | `days[0..6]` (`DailyGiftRewardStage`: `dayID`, `gifts`) và `resourceIcons` (bảng loại tài nguyên → icon, nguồn icon duy nhất cho cả 7 ngày). |
| `DailyGiftFeature` | Dữ liệu ngày hiện tại, đã nhận, còn nhận được hôm nay. |

Luồng mở: `Open()` → `InitData()` (lần đầu: `days[i].Init(i+1, config.GetRewardStage(i))`) → `UpdateButtonStatus()` → `LoadData()` (đặt trạng thái từng ô) → `AnimateOpen()`.
Luồng nhận: `ClaimButton()` → làm tối ô hôm nay → `OnClaimReward()` (đánh dấu đã nhận, **cộng và lưu thưởng ngay** qua `GrantReward`, rồi mới chạy `RewardAnimator` chỉ để hiển thị) → `CloseWithAnimation()`.

## 2. Cấu trúc prefab

```
PopupDailyLogin   Canvas Overlay order 30000 · CanvasScaler 1080×1920 match 0 · GraphicRaycaster
                  CanvasGroup · Image nền đen alpha 0.98 · PopupDailyLogin (card = Box)
└─ Box                    popup daily.png 958×1264 (card chạy hiệu ứng mở/đóng)
   ├─ Header              header.png 791×310, neo đỉnh Box
   │  ├─ Title            TMP "Daily Login" (FredokaOne-Regular Color 70)
   │  └─ Close            exit.png 120×120 · Button → OnClickClose · UIButtonScaleEffect
   ├─ DaysLayout          900×900, chứa 6 instance Day.prefab đặt tay (lưới 3×2, bước 280×305)
   │  └─ Day … Day (5)    254×282
   ├─ Day 7               day 7 open.png 705×275 · Day7
   │  ├─ Rewards          3 ô Reward n (Icon 120×120 + Amount), x = −180 / 0 / 180
   │  ├─ Lid              day 7 gift.png 755×286 (nắp đậy Rewards)
   │  ├─ Disable (off)    day 7 open.png phủ màu tối + tick_L.png
   │  └─ Tag              tag 7.png 160×154 (nhãn "Day 7" bằng ảnh)
   ├─ Comeback TMP (off)  chữ hẹn ngày mai (next_day_text)
   └─ Claim Button        bt green claim.png 380×184 · Button → ClaimButton · UIButtonScaleEffect
```

`Day.prefab`:

```
Day               day 1.png · Day
├─ Highlight (off)   Day 2.png, stretch (highlight_go)
├─ Day text          TMP "Day N" (dayText)
├─ Reward UI         Image icon, preserveAspect (rewardIcon)
├─ Amount            TMP số lượng (rewardAmount)
└─ Disable (off)     day 1.png màu (0.15, 0.35, 0.38, 0.55) + tick_L.png (disableGo)
```

Thứ tự con quyết định lớp vẽ: nền → nổi bật → nội dung (icon, số) → nắp → lớp đã nhận → nhãn. Giữ thứ tự này khi dựng popup mới.

## 3. Sprite đang dùng

| Phần | Đường dẫn | Kích thước |
|---|---|---|
| Khung popup | `Assets/Packages/DailyGift/Sprites/popup daily.png` | 958×1264 |
| Bảng tên | `Assets/Packages/DailyGift/Sprites/header.png` | 791×310 |
| Ô ngày thường / ô đã nhận (tô màu) | `Assets/Packages/DailyGift/Sprites/day 1.png` | 254×282 |
| Ô ngày hôm nay | `Assets/Packages/DailyGift/Sprites/Day 2.png` | 254×281 |
| Dấu tick đã nhận | `Assets/Packages/DailyGift/Sprites/tick_L.png` | 148×109 |
| Khay Day 7 (mở) / lớp đã nhận Day 7 | `Assets/Packages/DailyGift/Sprites/day 7 open.png` | 705×275 |
| Nắp hộp Day 7 | `Assets/Packages/DailyGift/Sprites/day 7 gift.png` | 755×286 |
| Nhãn Day 7 | `Assets/Packages/DailyGift/Sprites/tag 7.png` | 160×154 |
| Nút đóng | `Assets/HotPot/Art/Sprites/UI/button/exit.png` | 120×120 |
| Nút Claim | `Assets/HotPot/Art/Sprites/UI/button/bt green claim.png` | 380×184 |
| Icon Coin | `Assets/HotPot/Art/Sprites/UI/win & lose/coins reward.png` | 292×280 |
| Icon UnlimitedLives | `Assets/HotPot/Art/Sprites/UI/Home/live ic.png` | 92×77 |
| Icon BoosterPickup | `Assets/HotPot/Art/Sprites/UI/Boosters/pickup.png` | 356×348 |
| Icon BoosterClean | `Assets/HotPot/Art/Sprites/UI/Boosters/sweep.png` | 308×304 |
| Icon BoosterShuffle | `Assets/HotPot/Art/Sprites/UI/Boosters/shuffle.png` | 304×280 |
| Icon BoosterAutoComplete | `Assets/HotPot/Art/Sprites/UI/Boosters/order.png` | 332×272 |

Kích thước trong bảng là vùng sprite dùng trong prefab; texture của các sprite trong `Packages/DailyGift/Sprites/` đã được thêm viền tới bội số 4 (xem mục 7). `daily login.png` và `daily login 2.png` (1080×2340) chỉ là mockup tham khảo, không gán vào prefab nào.

## 4. Nạp dữ liệu từ config

- **Ngày 1–6:** `Day.ShowReward` lấy phần thưởng đầu tiên `stage.gifts[0]`: `rewardIcon.sprite = config.GetResourceIcon(gift.type)` và `rewardAmount.text = gift.GetNormalValue()` (booster `x1`, mạng vô hạn dạng giờ, xu là số). Icon tự đúng theo loại thưởng, không cần sửa thêm khi đổi config.
- **Ngày 7:** `Day7.ShowReward` điền tối đa 3 phần thưởng (`rewardIcons[]`, `rewardAmounts[]`), icon tra theo loại bằng `config.GetResourceIcon(type)`, ô thừa bị ẩn. Muốn hơn 3 phần thưởng phải thêm ô trong prefab và mảng.
- Mọi icon thưởng lấy từ bảng `resourceIcons` theo loại tài nguyên; không chọn icon theo số thứ tự enum `ResourceType`. Thêm loại tài nguyên mới thì thêm một dòng vào bảng (thiếu dòng → icon trống). Đã bỏ field `DailyGiftRewardStage.giftIcon` và hàm `GetIconDailyGiftByDayNum`.

## 5. Trạng thái ô

| Trạng thái | Gọi | Ô 1–6 | Day 7 |
|---|---|---|---|
| Chưa tới | `Enable(true)` | nền thường | nắp đóng |
| Hôm nay | `Enable(true)` + `Highlight()` | nền `Day 2.png` + nhịp scale 1↔1.05 | nắp mở + nhịp scale |
| Đã nhận | `Enable(false)` | lớp tối + tick | nắp mở, lớp tối + tick |

Ô không có nhãn chữ hoặc nền nổi bật riêng (như Day 7) ghi đè `ShowLabel` / `SetHighlightVisible` thay vì để trống ref; `Day` không kiểm tra null.

## 6. Quy ước cho popup mới theo mẫu này

1. Popup kế thừa `FoodPop.UICanvasPrime`; gốc có Canvas (Overlay, order 30000), CanvasScaler 1080×1920, GraphicRaycaster, CanvasGroup; `card` là khung nội dung. Đăng ký prefab trong `UIRef` của scene mở nó; prefab không đặt trong `Resources`.
2. Mở/đóng bằng `AnimateOpen` / `CloseWithAnimation`, không dùng `UIAnimator`. Mọi nút có `UIButtonScaleEffect`; rung bằng `PlayHaptic()`. Scene Home không có `IngameSoundManager` nên không gọi `PlayClickFeedback`/`PlaySoundClick` ở popup Home.
3. Ô lặp lại là instance của một prefab con (`Day.prefab`); ô đặc biệt kế thừa script và ghi đè hàm virtual.
4. Icon/số lượng luôn nạp từ config qua ref trực tiếp (Image, TMP) trên script ô; không bật/tắt sẵn nhiều object icon.
5. Cộng và lưu thưởng trước, hiệu ứng thưởng chỉ để hiển thị.
6. Sprite riêng của popup đặt trong thư mục sprite của package; icon tài nguyên dùng chung lấy từ `Assets/HotPot/Art/Sprites/UI`.

## 7. Đánh giá

Đã sửa ngày 2026-11-23 (kiểm tra bằng Editor: biên dịch, 0 sprite bị mất trong popup, render lại popup đúng bố cục):
- Sprite riêng trong `Packages/DailyGift/Sprites/` đã thêm viền trong suốt **bên phải và bên trên** cho chia hết cho 4 (vd. `day 1.png` 254×282 → 256×284, `popup daily.png` → 960×1264). Vì sprite ở chế độ Multiple (rect gốc dưới-trái), vùng cắt sprite vẫn đúng kích thước cũ nên hình và kích thước Image không đổi; texture giờ nén ETC2_RGBA8. Ảnh gốc sao lưu ở `tmp/daily_sprites_pad/originals/`, script `tmp/daily_sprites_pad/pad_to_4.ps1` — khi thay sprite mới chạy lại script hoặc giao ảnh đúng bội số 4.
- Đổi thư mục `Sprite/` → `Sprites/` (giữ `.meta`/GUID).
- Đã gỡ phụ thuộc `_Hoang`: Image của `DaysLayout`, `Day.prefab/Highlight/Image`, object `Countdown`; không còn sprite `_Hoang` trong popup.
- Mọi TMP dùng `FredokaOne-Regular Color`; `Title` không còn `WarpTextExample`.
- `Day.prefab` gốc 254×282, 6 instance không còn ghi đè kích thước.

Còn lại:
- Tên file lẫn hoa thường và dấu cách (`Day 2.png` cạnh `day 1.png`); không bắt buộc, đổi tên cần giữ `.meta`.
- `live ic.png` (92×77) là icon dùng chung trong `HotPot/Art/Sprites/UI/Home`, chưa thêm viền vì ảnh hưởng UI Home khác.
- `Day 7/Rewards/Reward n/Amount` đặt ở (41, −125) so với icon 120×120 — kiểm tra lại vị trí số khi chạy thật.
- `RewardAnimator` vẫn lấy từ hệ UI Home cũ (`UIManager.Ins.GetPanel()`); khi bỏ hệ cũ cần nguồn hiệu ứng thưởng khác.
