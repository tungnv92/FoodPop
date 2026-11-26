# UI màn chơi

Gồm HUD, popup (Pause, Win, Lose, Revive, WarningQuit, BuyBooster, BoosterIntro), luồng trạng thái màn, âm thanh UI và hướng dẫn màn 1–2. Booster và túi giữ món: [Booster.md](Booster.md).

## Kết nối Internet bắt buộc
- Lớp phủ core độc lập với UIManager: `Assets/Packages/Common/Network/InternetConnection.cs` + `Assets/Packages/Common/Prefabs/Resources/InternetConnection.prefab`. Canvas sorting order 32760, chặn raycast toàn màn hình, không có nút đóng; không đăng ký vào UIRef và không đóng popup đang mở.
- Hiện Checking Internet connection… khi xác thực; thất bại hiện Internet connection required / Please reconnect to continue playing và Try again. Nút bị khóa trong lúc request chạy. Nội dung theo tiếng Anh hiện có của game.
- Giữ tốc độ gameplay trước khi chặn; trước đó đã pause thì reconnect vẫn pause. Đồng hồ, request và kiểm tra kết nối dùng thời gian thực, không phụ thuộc timeScale.
- Load, chuỗi Home và Start của LevelController đợi `ProfileManager.IsReady`; nút Play cũng kiểm tra điều kiện này. Bật ProfileManager.TimeManager.cheatTime bỏ chặn mạng để test. Chi tiết đồng hồ local và chu kỳ xác thực xem [FeatureModules.md](FeatureModules.md).
- Kiểm chứng 20/11: Play Mode xác thực HTTPS và pause/reconnect qua sự kiện về từ nền đã qua; đã xem ảnh popup Checking và render trạng thái lỗi. Chưa ngắt mạng thật trên Android hoặc bấm tay luồng Load → Home.

## Kiến trúc
- `UIManager` (`Scripts/UI/UIManager.cs`) nằm trên object `UiManager` có sẵn trong scene; `Instance` không tự tạo manager. Không đặt popup sẵn trong scene.
- `Config/UI/UIRef.asset` (`uiCanvasRefList`) là danh sách prefab duy nhất; manager tạo instance khi gọi lần đầu dưới `uiCanvasParentTrans` rồi cache theo Type. Mở một popup thì đóng popup khác.
- API generic (`T : UICanvasPrime`): `GetUICanvas<T>`, `OpenUI<T>`, `CloseUI<T>`, `IsUICanvasOpened<T>`, `CloseAllUI()`. API gameplay: `ShowWin(next, hasNextLevel, animate)`, `ShowLose(replay, animate)`, `ShowRevive(revive, decline, available, case, animate)`, `ShowPause`, `ShowWarningQuit(confirm, cancel)`, `ShowBuyBooster(type)`, `ShowBoosterIntro(icon, title, info, gotIt)`, `ShowShortMessage(message)`, `HideAll()`. `ActivePopup`/`ActiveCanvas` để kiểm tra.
- Kế thừa: `CanvasBoosterIntro → LevelPopupView → UICanvasPrime`; `CanvasWin`, `CanvasLose`, `CanvasPause`, `CanvasBuyBooster`, `CanvasRevive`, `CanvasWarningQuit` → `UICanvasPrime` trực tiếp.
- Cache scale của `LevelPopupView` là `entranceRestScale`, tách tên khỏi `UICanvasPrime.restScale` để Unity không báo field trùng khi serialize `CanvasBoosterIntro`. Đây là cache runtime riêng, không đổi field Inspector hoặc thêm alias serialize `restScale` ở lớp con. Kiểm chứng 18/11: rà khai báo field trên 9 chuỗi kế thừa popup không còn tên trùng; Unity biên dịch sạch, nạp/nhân bản/serialize–deserialize prefab BoosterIntro không phát sinh lỗi trùng mới (`output/BoosterIntro/SerializationCheck.txt`).
- `UICanvasPrime` dùng chung: `Open/Close/IsOpening`, `OnOpenCanvas/OnCloseCanvas`, `onCloseAction`, `isDestroyOnClose`; cache manager feedback; `PlayClickFeedback` (rung + click theo cài đặt); pause sở hữu theo instance (`PauseGameplay`/`ReleaseGameplayPause`); mở phóng 88% → gốc + fade trong `openDuration` 0.24 s, đóng thu 90% + fade trong `closeDuration` 0.16 s, unscaled; `AnimateClose` (chỉ chạy hoàn tất, cho Revive kiểm tra thanh toán) và `CloseWithAnimation` (đóng rồi điều hướng). `Close()` hủy tween/callback chờ và báo manager. `HideAll`/Close trực tiếp đóng ngay.
- `ShortMessage` là thông báo ngắn độc lập trong scene; nội dung booster lấy từ `BoosterConfig.shortDes`.
- Nút: `UIButtonScaleEffect` (co 94% khi nhấn, bật OutBack khi click, unscaled) cho nút điều hướng; `UIButtonClick` (`Scripts/UI/UIButtonClick.cs`) phát click trước callback, **bỏ tiếng click nếu callback mở popup** (so `ActivePopup` trước/sau).
- Chỉnh thiết kế trực tiếp trên prefab; không có tool Setup/Preview dựng popup.

| Popup | Script | Prefab |
| --- | --- | --- |
| Thắng | `CanvasWin.cs` | `Prefabs/UI/LevelResult/WinPopup.prefab` |
| Thua | `CanvasLose.cs` | `Prefabs/UI/LevelResult/LosePopup.prefab` |
| Hồi sinh | `CanvasRevive.cs` | `Prefabs/UI/LevelResult/RevivePopup.prefab` |
| Tạm dừng/cài đặt | `CanvasPause.cs` | `Prefabs/UI/Pause/PausePopup.prefab` |
| Cảnh báo thoát | `CanvasWarningQuit.cs` | `Prefabs/UI/WarningQuit/WarningQuitPopup.prefab` |
| Mua booster | `CanvasBuyBooster.cs` | `Prefabs/UI/Booster/BuyBoosterPopup.prefab` |
| Booster mở khóa | `CanvasBoosterIntro.cs` | `Prefabs/UI/Booster/BoosterIntroPopup.prefab` |
| Lớp phủ dùng thử / hướng dẫn | `BoosterTrialOverlay.cs` | `Prefabs/UI/Booster/BoosterTrialOverlay.prefab`, `Prefabs/UI/Tutorial/SelectDishOverlay.prefab` |

Sprite UI: `Art/Sprites/UI/LevelResult/`. Script trong `Assets/HotPot/Scripts/UI/`.

## Camera và HUD
- **SafeAreaCamera** (`Scripts/UI/SafeAreaCamera.cs`): từ `defaultOrthographicSize` 11.7, nếu `cameraBounds` (tâm 0,0, 10.797363 × 23.4) vừa thì phóng to cho vùng 1 lấp kín chiều ngang nhưng không nhỏ hơn size chuẩn quá `maxZoomInPercent` (5% → 11.115); nếu không, `secondaryBounds` (x −5.2, y −11.7, 10.4 × 23.4) vừa thì giữ 11.7; cả hai không vừa thì tăng size để chứa vùng 2. Không dịch camera. Chạy cả Edit Mode.
- `ScreenTopAnchor` / `ScreenBottomAnchor` bám mép trên/dưới vùng an toàn (`targetCamera`, `topOffset`), cập nhật trong `LateUpdate`; `ScreenBottomAnchor` còn đặt vị trí ngay trong `OnEnable`, để object vừa bật (như `BottomBannerBar` của Pickup) đo được đúng vị trí trong cùng frame.
- TopPanel: `UICoin` (cache `PlayerDataManager` trong `OnEnable`, `DefaultExecutionOrder(100)`, nghe Coin), `UICustomer` (`CustomerManager.RemainingCustomersChanged`), `UILevel` (`LevelController.LevelLoaded`, hiện `levelNumber`). Không polling, không truy cập runtime ở Edit Mode.

## Luồng trạng thái màn (`LevelController`)
```text
Playing → Won → (PresentWin) → CanvasWin
Playing → ReviveOffered → CanvasRevive → Reviving → Playing
                                    └→ (từ chối) Lost → CanvasLose
```
- **Nạp màn:** `Start` → `LoadSavedLevel()` theo `PlayerDataManager.GetCurrentLevel()` (vượt level cuối thì lùi về level cao nhất có). `LoadLevel` chọn Hard/Easy theo `difficultyMode` ([Configuration.md](Configuration.md#level-levelconfig)). Thắng tăng `currentLevel` trong `TrackingWin`. `TrackingLose` reset chuỗi thắng với mọi kiểu thua (thua, Replay, BackHome) nhưng chỉ tăng retry khi thua thật.
- **Pre-win:** mỗi frame khi `Playing` và không pause, sau kiểm tra thắng/bế tắc, nếu tổng phần món trên board (`Food.TotalAmount`, đĩa đôi tính một lần, gồm đĩa băng/ẩn) ≤ `ChefManager.CountEmptySlots()` thì `EnterPreWin()`: `Time.timeScale` × `preWin.speedMultiplier` (1.3) tới hết màn. `LoadLevel`/`OnDisable` gọi `ExitPreWin()`. Log prefix `[LevelController]`.
- <a id="thắng"></a>**Thắng:** `CustomerManager.AllServed` (khách cuối nhận đủ món) → `FinishLevel(Won)` (bật `FoodBoardManager.ResultLocked`; `BoosterController.TryActivate` từ chối khi State ≠ `Playing`, nên trong lúc chờ không chạm board/booster được; thua cũng bật `ResultLocked`, `LoadLevel` tắt lại) → `ExitPreWin()` → `ChefManager.HideDishes()` (thu nhỏ đĩa ngay — [ConveyorChef.md](ConveyorChef.md#thắng-màn)) → chờ `CustomerManager.AllCustomersGone` (mọi khách đã ra khỏi khung camera: làn trái khoảng 0,6 s, làn phải khoảng 1,1 s sau khi bắt đầu đi) + `preWin.winPanelDelay` (mặc định 0 s, giờ game) → pause → `ChefManager.PlayWin()` (chef Win) → `ShowResult` (nạp sẵn `LevelConfig.Load(n+1)`) → `CanvasWin`. Next → `NextLevel()` → `LoadLevel` → `Resources.UnloadUnusedAssets()`.
- **Bế tắc:** hoãn khi đầu bếp bận, món đang bay, khách có pending service, board đang dồn/mở nắp hoặc booster chạy. Nếu không món nào trên băng phục vụ được và không nhóm lộ nào thỏa `DishPickPlan` ([ConveyorChef.md](ConveyorChef.md#sức-chứa-khi-lấy-đĩa-dishpickplan)) → `ReviveOffered` (case OutOfSlots).
- **Hết giờ:** Boss/Shipper hết `timeServe` (kiểm tra sau thắng, trước bế tắc) → `ReviveOffered` (case OutOfTime), pause.
- **Hồi sinh:** OutOfSlots dọn sạch băng chuyền, mọi food bay thẳng về board thành đĩa thô (`BoosterController.TryCleanForRevive()`, không dùng tủ giữ món, không trừ lượt booster, giữ khách/nguyên liệu; chi tiết [Booster.md](Booster.md)); OutOfTime gọi `ResetExpiredServeTimers` (+30 s cho khách đã hết giờ, không Clean). Thu bị ngắt thì mời lại. Không giới hạn số lần.
- **Thua:** chỉ khi từ chối hồi sinh → `Lost`, `onLost`, `CanvasLose`.

## Popup
### CanvasWin
- Không còn bước thu xu chờ trong chuỗi Home; thưởng màn được cộng trực tiếp ở CanvasWin theo luồng dưới đây.
- Mở: ẩn title (`Spine_Win`), UICoin, glow (`fx_glow`), Reward, nút; Curtain (`Assets/Spines/Curtain`, `curtainAnimation`) chạy Close (unscaled); khi Close **Complete** thì giữ rèm thêm `curtainHoldDelay` (0.5 s, unscaled) rồi nền đen `blackBackground` fade từ 0 lên alpha thiết kế trong `blackFadeDuration` (0.3 s, unscaled); fade xong mới chạy Win title Spine (`Assets/Spines/win`, Start 1.33 s → Idle lặp, unscaled); Glow hiện khi Idle bắt đầu (`TrackEntry.Start`): fade alpha 0 → alpha thiết kế và scale 0.8 → 1 (OutBack) trong `glowRevealDuration` (0.3 s, unscaled). Sau `titleClaimDelay` (2 s từ Win Start) hiện UICoin, Reward và `buttonsRoot` scale 0.8 → 1 (OutBack, `claimRevealDuration` 0.3 s, nút trễ 0.08 s); xong tween mới cho bấm. Curtain là con đầu của nhóm nội dung (nằm sau).
- Next = `IngameConfig.winReward.coin` (100), Watch = `winReward.watchAdsCoin` (200) (Watch tạm gọi thẳng `OnWatchAdsSuccess`, chưa có SDK). Chọn một lần; khóa nút, ẩn Reward, `CoinAttraction` bay về UICoin; `OnAddCoin(..., "win_reward")` cộng và lưu ngay lúc bấm (thoát game khi xu đang bay không mất thưởng); sự kiện Coin chạy trước, rồi `UICoin.AnimateReward(số dư cũ, ...)` đếm từ số dư cũ lên số dư đã lưu sau `coinCountDelay` (0.4 s). Hẹn giờ unscaled `coinAttraction.duration + lifetime.constantMax` (không dựa vào `onLastParticleFinish`) rồi, nếu chơi tiếp (có màn kế tiếp và level hiện tại < `winReward.levelBackHome`, tạm đặt 100), ẩn hết nền đen, title, glow, ví, thưởng và nút, chỉ để lại Curtain đang đóng, gọi `NextLevel()` nạp màn mới phía sau rèm (`LoadLevel(next, behindCurtain: true)` không gọi `HideAll` và giữ timeScale = 0), rồi phát Curtain "Open" (unscaled) để lộ màn mới; Open xong WinPopup tự `Close()` (không phát âm đóng) rồi gọi `LevelController.StartNextLevel()` trả lại timeScale, game mới bắt đầu chạy. Không chơi tiếp (màn cuối hoặc từ level `levelBackHome`): nhận thưởng nhưng popup đứng yên, chưa có luồng về Home.
- Hiệu ứng xu dùng nested prefab `Packages/CoinAttraction/Prefabs/CoinAttraction.prefab`: bung → rơi nhẹ → tăng tốc hút về ví, lật sheet đã duyệt và nảy/lóe khi xu đến, tất cả unscaled. Chỉnh RectTransform của instance `CoinAttraction` để đặt điểm phát (offset lúc tách 0,-327); emitter constraint tắt. Thông số, sprite, API, demo, generator và export nằm trong [CoinAttraction.md](CoinAttraction.md).
- Kiểm chứng 18/11/2026: Unity biên dịch thành công; probe Play Mode tách riêng hiệu ứng ở timeScale=0 ghi nhận đủ 10 xu kết thúc, nảy ví/lóe sáng, hủy giữa chừng reset sạch và phát lại đủ 10 xu. Kết quả ở `output/CoinFlight/RuntimeCheck.txt`. Probe không gọi claim hoặc ghi profile; chưa thử giao dịch Next/Watch thực tế hoặc build Android.
- Đóng/mở lại hủy timer (`StopTitleSequence`) và tween. Chi tiết prefab tái sử dụng, API và menu export xem [CoinAttraction.md](CoinAttraction.md).

### CanvasLose
- Title Spine (`Assets/Spines/lose`, 0.57 s, không lặp, unscaled); khi xong mới hiện `messageRoot` và `buttonsRoot` (scale 0.8 → 1, `choiceRevealDuration` 0.3 s).
- Home/Retry: chống nhấn lặp, `UseHeart` một lần (vô hạn không trừ), `ResetStatePlaying`, rồi Retry (`RetryLevel`) hoặc nạp Home sau tween. Mở popup không trừ mạng.

### Quy ước đặt tên popup
- Khung chung: `BlackBG`, `Root` (= `card`), `PopupBG` (thân), `TitleRibbon/Text_Title`, `BtnClose`, `PopupBG/RootButtons` (hàng nút).
- Nút `Btn<Tên>`, chữ `Text_<Tên>`, hình nội dung `icon_<tên>`, hình trang trí/hiệu ứng `fx_<tên>` (`fx_glow`, `fx_rays`, `fx_twinkle_1`), Spine `Spine_<Tên>` (`Spine_Win`, `Spine_Curtain`), khung/nhóm PascalCase (`SettingList`, `BoosterPanel`, `InfoPanel`, `OutOfSlot`). Biến thể trạng thái ghi rõ chủ thể (`On_Music`, `Text_InfoOutOfTime`), không để trùng tên giữa các nhóm. Con `Renderer0..n` do Spine sinh thì giữ nguyên.
- Áp dụng cho cả 7 popup (16/11/2026): Pause, Revive, BuyBooster, WarningQuit, Win, Lose, BoosterIntro. BoosterIntro dùng `BlackBG` (trước là Scrim) và `Root` (trước là Card), không có `PopupBG`. Code không tìm các object này theo tên.
- Tên field serialize theo hậu tố kiểu: `…Button`, `…Label` (TMP_Text/Text), `…Icon` (Image), `…Root` (GameObject/Transform là nhóm), `…Animation` (SkeletonGraphic), `…Duration`/`…Delay` (thời gian), `…Title`/`…Message` (string). Cùng vai trò dùng cùng tên giữa các popup: `closeButton` (nút X), `coinButton` (mua/hồi sinh bằng Coin), `buttonsRoot` (`RootButtons`), `blackBackground` + `blackFadeDuration` (nền đen). Field đổi tên giữ `[FormerlySerializedAs("tênCũ")]` để không mất giá trị Inspector (16/11/2026: BuyBooster/BoosterIntro `boosterNameLabel`, `descriptionLabel`; BuyBooster `coinButton`; WarningQuit `closeButton`; Revive `outOfSlotRoot`, `outOfTimeRoot`, `outOfSlotTitle`; Win `buttonsRoot`, `glowRoot`; Lose `buttonsRoot`, `messageRoot`; BoosterIntro `blackBackground`, `blackFadeDuration`).

### CanvasRevive
- Hai nhóm nội dung `OutOfSlot`/`OutOfTime` trong prefab, bật một nhóm theo `ReviveCase`; tiêu đề đổi theo case.
- Lần đầu mỗi màn: Watch Ads hoặc Coin 900. Sau lần hồi sinh thành công đầu, ẩn và chặn Ads. Giá Coin = 900 + 1000 × số lần hồi sinh thành công (gồm Ads). Bộ đếm reset khi `LevelLoaded` hoặc Won/Lost (đăng ký ở Awake, gỡ OnDestroy); `isDestroyOnClose = false`.
- Coin kiểm tra đủ tiền, trừ sau hiệu ứng đóng; lỗi thì hiện lại popup. Watch tạm gọi thẳng thành công (không SDK). X → `DeclineRevive`. `available = false` khóa Coin/Watch nhưng vẫn cho X. `LevelController` giữ pause.
- Chữ dài (ưu tiên hiện đủ, "…" chỉ là lưới an toàn, đều Overflow Ellipsis): `Text_Watch` 300×100, Auto Size 36–60, tối đa 2 dòng; `Text_Price` 160×100 tâm x=36 (bắt đầu sau `icon_coin`, không chồng icon), Auto Size 36–60, 1 dòng; `Text_InfoOutOfSlot`/`Text_InfoOutOfTime` 800×200, Auto Size 36–50, tối đa 3 dòng (icon tương ứng `icon_out_of_slot`/`icon_out_of_time`); `Text_Title` 600×100, Auto Size 40–70, 1 dòng. Đã thử trong Editor: giá "123,900", "Смотреть рекламу", câu mô tả tiếng Đức 3 dòng, "Keine freien Plätze mehr!" đều hiện đủ. Chưa kiểm tra bằng ảnh chụp/Play Mode.

### CanvasPause
- Nút Home (`Root/PopupBG/RootButtons/BtnHome`), Restart (`RootButtons/BtnReplay`) xếp bằng `HorizontalLayoutGroup` (spacing 26, giữ tâm ±188), đóng (`Root/BtnClose`). Ba hàng `SettingList/RowSound`, `RowMusic`, `RowVibration` (con `icon_<x>`, `Text_<X>`, `On_<X>`/`Off_<X>` loại trừ nhau, núm `Knob_<X>_On`/`Knob_<X>_Off`, ví dụ `On_Music`, `Knob_Music_On`). Ba hàng rộng 775 khớp `SettingList`. Quy tắc chữ dài (ưu tiên hiện đủ, "…" chỉ là lưới an toàn), đều Overflow Ellipsis:
  - Nhãn `Text_<X>`: khung x −184..95 (279×100), Auto Size 30–50, tối đa 2 dòng.
  - `Text_Home`/`Text_Replay`: khung 300×130, Auto Size 36–60, tối đa 2 dòng.
  - `Text_Title`: khung 600×100, Auto Size 40–70, 1 dòng (ribbon chỉ chứa một dòng).
  - Đã thử trong Editor: "Chơi lại màn này", "Начать уровень заново", "Hiệu ứng rung khi chạm", "Vibrationsrückmeldung", "Settings & Options Menu" đều hiện đủ, không bị cắt. Chưa kiểm tra bằng ảnh chụp/Play Mode. Ba hàng setting không có `UIButtonScaleEffect`. Tiêu đề: `TitleRibbon/Text_Title`.
- `OnEnable` cache `PlayerDataManager`/`LevelController`, đọc cài đặt, nghe `StateChanged`; `OnDisable` gỡ.
- Đóng: tiếp tục màn. Home/Restart: xem cảnh báo thoát bên dưới.
- `UIManager.ShowPause` bỏ qua thao tác bấm khi `PauseLocked`: `LevelController.FinishLevel` bật khóa ngay khi thắng (lúc chờ hiện bảng thắng) và `LoadLevel` gỡ khóa, nên không thể Replay/Home chen vào lượt thắng.

### <a id="cảnh-báo-thoát"></a>CanvasWarningQuit
- Home/Restart từ Pause **chỉ hiện cảnh báo khi `CustomerManager.HasCompletedOrder = true`** (đã giao đủ một order trong lượt; reset ở `LoadCustomers`). Chưa có order hoàn tất thì đi thẳng, không trừ mạng.
- Một nút Confirm chung: Home → `UseHeart`, `BackHome` tracking, `ResetStatePlaying`, nạp `Home.unity`; Replay → `UseHeart`, lưu, `ReplayLevel`. X quay lại Pause, game vẫn dừng. Chữ "You will lose 1 life!" và icon luôn hiện, kể cả khi mạng vô hạn (vô hạn không bị trừ thật).

### CanvasBuyBooster, CanvasBoosterIntro, BoosterTrialOverlay
Xem [Booster.md](Booster.md#mua-booster).

## <a id="âm-thanh-và-rung"></a>Âm thanh và rung
- `IngameSoundManager` (`Scripts/Audio/IngameSoundManager.cs`, singleton trên object cùng tên) dùng clip trong `Assets/HotPot/Audio/Sounds`: click `SFX_INGAME_CLICK`; mở/đóng popup `sfx_ui_popup_show`/`sfx_ui_popup_disappear`; mua `SFX_INGAME_BUY_ITEM`; thưởng Watch booster `SFX_INGAME_REWARD_COLLECT`; mở popup hồi sinh chỉ phát `sfx_game_revive` (`CanvasRevive` ghi đè `UICanvasPrime.OpenSound`, không phát âm mở chung); booster mới `SFX_INGAME_NEW_ITEM_UNLOCK`; thắng/thua `SFX_INGAME_GAME_WIN`/`SFX_INGAME_GAME_FAILED`. `SFX_INGAME_OUT_OF_SPACE` (`Event.OutOfSpace`) hiện không còn được phát. Tất cả qua nguồn SFX chung, chạy khi pause. `UICanvasPrime` phát âm mở/đóng, `UIManager` phát âm kết quả.
- Gameplay chỉ gọi API của manager: `PlayDishLaunch`, `NextPortionPitch` + `PlayFoodLand` (cao độ 0.85–1.2, các phần cùng đĩa chênh ≥ 6%), `PlayChop(food)` (clip từ `FoodIconConfig`, biến thể `_2/_3`), `PlayPrepared`, `PlayServe` (≤ 0.12 s, chuỗi cao dần), `PlayCustomerHappy`.
- `PlayerSettingsController` (trên IngameSoundManager, order 100) mute nguồn SFX theo `IsSoundOn`, nguồn Background Music theo `IsMusicOn`, `HapticController.hapticsEnabled` theo `isVibrateOn`. Nhạc nền đã nối AudioSource loop nhưng **chưa gán clip**.
- Rung: `HapticPatterns.PlayEmphasis(0.8f, 0.5f)` (Nice Vibrations) khi chạm đĩa trên board, chạm món trên thẻ order và nhấn nút UI.

## <a id="hướng-dẫn-màn-1-2"></a>Hướng dẫn màn 1–2
- Object `LevelOneTutorial` trong HotPotDemo (script cùng tên), tham chiếu `LevelController`, `FoodBoardManager`, `CustomerManager`, prefab `SelectDishOverlay.prefab` (dùng `BoosterTrialOverlay`, `scaleGuideWithFocus = false`, bàn tay Spine scale 1.2 cố định, Prompt TMP Fredoka 45, `sortingOrder` 29900, không chặn raycast, không pause).
- Level 1: sau 0.35 s trỏ đĩa đầu cột 2 (tâm `Food.IconRenderer.bounds`) với "Select a dish!"; đóng khi `DishPicked` thành công.
- Level 2: khách đầu (`CustomerManager.CustomerArrived` index 0) → "Tap the order to see ingredients!" trên món đầu của thẻ (`OrderItemView.TargetPosition`); đóng khi `OrderView.IngredientsOpened`.
- Nạp/chơi lại reset; level khác không tạo overlay. Không lưu cờ vào profile.

**Kiểm chứng:** Play Mode đã PASS cho Pause/cài đặt, WarningQuit, BuyBooster, Revive (giá/Ads/reset), Win claim, Lose, hướng dẫn, âm thanh (9 sự kiện). **Chưa Play Mode:** chuỗi thắng mới (ẩn đĩa khi khách cuối nhận đủ món, chờ khách ra khỏi màn, Curtain → nền đen), hồi sinh OutOfTime (popup, thanh toán, +30 s), pre-win. Chưa kiểm tra rung trên thiết bị, chưa có nhạc nền, chưa có SDK Ads.

## Dọn hệ UI legacy (26/11/2026)

- `Assets/_Hoang` đã xóa sau khi chuyển các tài nguyên còn dùng; không còn manager UI toàn cục, PopupId hoặc UIAnimator cũ.
- Sáu popup LavaQuest kế thừa `UICanvasPrime`, đăng ký trong cả UIRef Home và gameplay; tạo qua UIManager. Claim/Fail/Info mở chồng trên bản đồ. Cấu trúc và kiểm chứng: [FeatureModules.md](FeatureModules.md#hoàn-tất-chuyển-ui-lavaquest-26112026).
- Script chuyển scene hiện ở `Assets/HotPot/Scripts/Scene/`: `Load.cs`, `LoadSceneManager.cs`, `SceneId.cs`, `SceneTransition.cs`, giữ GUID khi di chuyển.
- Theo yêu cầu người dùng, không triển khai hoặc kiểm thử scene Load trong đợt này; file scene vẫn được giữ. Home và HotPotDemo đã kiểm tra không có Missing Script. Play Mode chỉ kiểm tra tạo sáu popup LavaQuest, chưa thử toàn bộ luồng UI/nhận thưởng.