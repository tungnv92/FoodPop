# FoodPop — ghi chú bàn giao (cập nhật 06/11/2026, 08:45)

## Cập nhật 04–06/11
- **IngameConfig chỉ dùng `IngameConfig.Instance`** (asset `Resources/IngameConfig`): đã bỏ field `[SerializeField] IngameConfig ingameConfig` + `IngameConfig.Resolve(ingameConfig)` ở `FoodBoardManager`, `FoodFlyAnimator`, `ChefManager`, `CustomerManager`, `ServingFood`, `TrayDishStack` (`Food`, `OrderItemView` đã dùng `Instance` sẵn). Mẫu: `private static IngameConfig Config => IngameConfig.Instance;`. Trong `Scripts/` không còn `ingameConfig`. Lưu ý: `ChefManager` từng bị IDE lưu đè bản cũ (04/11 14:56) sau khi đã sửa — đã làm lại 06/11.
- **Rung (haptic)** — Lofelt Nice Vibrations + `GamePackages.ProfileManager`: `PlayHaptic()` (kiểm tra null `ProfileManager.instance` / `PlayerDataManager`, rồi `isVibrateOn` → `HapticPatterns.PlayEmphasis(0.8f, 0.5f)`).
  - `FoodBoardManager.TryPickAt`: rung mỗi khi chạm trúng một đĩa (cả đĩa sâu / đĩa không lấy được); chạm chỗ trống không rung.
  - `OrderItemView.PlayHaptic()`, gọi từ `OrderView.Update` khi chạm trúng món trên thẻ (trước khi bật/tắt chi tiết nguyên liệu).
- **Nguyên liệu không cần sơ chế (Salt, Ice)**: `FoodIconConfig.FoodIcon.skipPreparation` + `FoodIconConfig.NeedsPreparation(type)`; `ConveyorSlot.TryFillFood` gọi `slotFood.Prepare()` ngay khi đặt lên đĩa nếu không cần sơ chế → đầu bếp bỏ qua (`CanPrepare` chỉ lấy món thô), khách nhận ngay (`IsPrepared`). Asset `Config/FoodIconConfig.asset`: đã tick Ice (19), Salt (21). Chưa có hiệu ứng riêng khi đáp (đáp như món thường).
- **Quy tắc thứ tự thành viên** (`AGENTS.md` → "Mandatory member order by MonoBehaviour lifecycle"): mọi class được sửa phải sắp lại cả class (hằng số/property/event public → field serialize → state private → callback Unity theo thứ tự chạy → API public theo luồng gameplay → private theo thứ tự gọi lần đầu). Đã sắp (chỉ di chuyển, kiểm tra tập dòng không đổi): `FoodBoardManager`, `OrderView`, `OrderItemView`, `ConveyorSlot`, `ChefManager`, `CustomerManager`, `ServingFood`, `TrayDishStack`. Với `ChefManager`, `CustomerManager`, `ServingFood`, `TrayDishStack` thứ tự API public giữ như cũ.
- **Ghi file khi IDE đang mở**: đã 2 lần IDE lưu đè bản cũ lên file Claude vừa ghi (`ChefManager.cs` 04/11, `FoodBoardManager.cs` 05/11) → sau khi Claude sửa, Reload trong IDE, không lưu bản cũ. Repo `.gitattributes` = `* text=auto` nên khác biệt CRLF/LF không tạo diff git.
- Các tool Setup trong `Editor/` (CustomerSetup, BoosterSetup, …) đã bị người dùng xoá — không tạo lại tool Setup dựng prefab.

## Cập nhật 03/11
- **Đĩa đôi — nhãn số lượng thứ hai** (`Food.RefreshSecondVisual`): "Second quantity" chỉ hiện khi `SecondAmount > 1` (giống nhãn đầu `amount > 1`); vẫn ẩn khi có nắp / đóng băng / booster tạm ẩn nhãn. Chạy mỗi frame trong `Food.LateUpdate` nên nửa thứ hai còn 1 phần là tự tắt.
- **Món thô bay vào đầu bếp** (`ChefManager.PrepareFood`), đã chốt với người dùng:
  - Bay lên thớt: sprite thô (`Cutting food`) hiện.
  - Tới thớt: **ẩn sprite thô** (`flyer.enabled = false`), không bật bóng đổ dưới món — chỉ còn anim chặt của đầu bếp; khói + vụn chặt vẫn phát tại thớt.
  - Poof (`PlayPreparedPoof`): bật lại với sprite đã sơ chế, rồi bay về đĩa như cũ. `SetupCuttingFood` bật lại renderer mỗi lượt.
  - `ChefManager.GetRawSprite(slot)`: sprite bay = hình đang hiện trên đĩa (`ConveyorSlot.FoodSprite`), nếu trống thì icon thô trong `FoodIconConfig` (tránh sprite bay bị trống khi `Ingredient` renderer và icon của slot Food là hai renderer khác nhau).
  - `ConveyorSlot.SetIngredientVisible` ẩn/hiện cả icon của `slotFood` (nếu khác `ingredientRenderer`) → lúc món bay đi, đĩa không còn bản món chồng.

## Cập nhật 02/11 (chi tiết trong `claude/foodpop-FoodBoardManager.md`)
- `FoodBoardManager`: nút **Update View** trên Inspector (`Editor/FoodBoardManagerEditor.cs`) dàn lại cột/đĩa sau khi đổi `rowSpacing`/`columnSpacing` (Play và Edit Mode); tách `ApplyDishShape` dùng chung.
- Lỗi vừa Play không pick được dish dù băng trống: nghi slot băng chuyền chưa gán `slotFood`. `ConveyorSlot.TryFillFood` giờ tự tìm `Food` con, không có thì LogError. `DishPickBatch.LastFailure` + log chẩn đoán `logPickFailures` trên FoodBoardManager (tắt khi xong). **Chưa xác nhận đã hết lỗi.**
- Người dùng đổi ảnh bàn thành `ingredientTable3/4/5LaneSprite` và bỏ tự gán sprite đĩa đôi + ảnh bàn trong `OnValidate` (phải gán tay).
- Người dùng/agent khác cũng sửa `Booster.cs` (dùng `GamePackages` / `ProfileManager` cho số lượt), `BoosterController.cs`, `PickupHandBooster.cs`, `UIManager.cs`, `ConveyorChefMovement.cs`; `ConveyorSlot.TryFillFood` giờ lấy icon từ `IngameConfig.Instance.FoodIcons` (bỏ tham số iconConfig).

## Vị trí project
- Máy công ty: `D:\Unity\Fork\FoodPop`; máy desktop-d4lrcql: `E:\FoodPop` (Unity 6000.3.17f1, URP), repo GitHub (`main`) qua GitHub Desktop (bản 3.6.7).
- Commit do Claude làm có tiền tố `[Claude]` và dòng `Agent: Claude` trong mô tả.
- Commit 17/10: `[Claude] Chef prep station…` (212 file) và `[Claude] CustomerManager orders, direct references, dead code cleanup` (62 file), đã push.
- 19/10: order theo final food, tối đa 4 món/khách, khay phục vụ, xếp chồng nguyên liệu trên khay — **chưa commit, phần xếp chồng chưa play test**.
- 21/10: đã thử commit toàn bộ 327 file trên máy desktop-d4lrcql (`[Claude] Sync FoodPop project…`) nhưng GitHub Desktop báo **lock file** (`.git/index.lock`) — cần xoá file lock (đóng mọi tiến trình git/Codex trước) rồi commit + push lại.

## Order theo final food (19/10)
- `FinalFoodType` (Scripts/gamebase): 40 ID = dòng món × 10 + số nguyên liệu (Sake 1x, Maguro 2x, Ebi 3x, Tamago 4x, Unagi 5x, Tako 6x, Hotate 7x, Kanikama 8x; x = 2..6).
- `FinalFoodConfig` + `Config/FinalFoodConfig.asset` (đã gán ảnh): mỗi món 2–6 nguyên liệu khác nhau, ảnh `Art/Sprites/Foods/Sushi/Final/<số món>/<slug>-nigiri.png`. Nguyên liệu đầu tiên mọi công thức là SushiRice. Bảng đầy đủ: `Documentation/FinalFoods.md`.
- `CustomerOrder.finalFoods`: tối đa `CustomerOrder.MaxDishes` = 4. Thẻ order chỉ hiện ảnh món (màu gốc, có bóng `DishShadow`), không hiện nguyên liệu.
- `OrderView` (người dùng sửa): 1–2 món xếp dọc 1 cột, 3–4 món 2 cột; ẩn ô thừa; nền Artwork chỉ đổi chiều rộng.
- `ServingTrayView` (người dùng viết, `Assets/CardDrop/Scripts/Customer`, trên object "Serving food"): `SetSlotCount(n)` tạo n ô viền nét đứt (`CardDrop/Prefabs/DishSlot.prefab`), 2 ô/hàng. Claude thêm `SortingLayerId`, `StackSortingOrder` (tray + 2).

## Nguyên liệu → khay (19/10, đã chốt với người dùng)
- Nguyên liệu sơ chế bay từ băng tới **ô khay** của món (không tới thẻ). Luật: **nguyên liệu đầu tiên của công thức (đế) phải tới trước**; các nguyên liệu khác nhận khi đế đã tới hoặc đang bay tới, thứ tự tự do. Chưa có món nhận → nằm chờ trên băng, đế được gửi thì bay theo ngay. Ưu tiên khách đến trước, món đứng trước trên thẻ.
- `TrayDishStack` = **component trên prefab `HotPot/Prefabs/Ingame/ServingHotPot.prefab`** (object "Serving food" trong scene), cạnh ServingTrayView; hai component trỏ nhau (`ServingTrayView.dishStack` ↔ `TrayDishStack.tray`). CustomerManager giữ `Lane.servingTray` (liên kết cũ của người dùng) và lấy stack qua `servingTray.DishStack`. Khay thiếu TrayDishStack → vẫn tạo ô, nguyên liệu bay tới thẻ + cảnh báo. Inspector có mục "Trạng thái (chỉ xem khi chạy)".
- Xếp chồng theo thứ tự công thức dù tới lộn xộn (lớp sau bị đẩy lên); đường bay tính lại điểm đến mỗi frame. Đủ nguyên liệu → các lớp chụm vào tâm ô, poof (`dishCompleteEffect` = FoodPreparedEffect), ảnh món hoàn chỉnh bật ra trên ô, thẻ đánh dấu tích. Món nằm trên khay tới khi xong cả order; khách trượt ra xong thì dọn (về pool). Không có khay → bay tới thẻ.
- Thông số xếp chồng nằm trong `IngameConfig.trayStack` (`foodScale`, `stackStepRatio`, …); ảnh món hoàn chỉnh tự co vừa ô. Pop đáp/món, `assembleDelay`, `mergeDuration`, `dishCompleteEffect` trên CustomerManager.
- Bài học: không đổi tên/kiểu trường đã gán trong scene (đổi `servingTray`→`trayStack` từng làm mất liên kết khi người dùng lưu scene); khi ghi file scene từ ngoài, Unity hỏi Reload.
- AutoPlayer tự ưu tiên cơm vì `TryFindCustomer` chỉ trả true cho topping khi đế đã giữ chỗ.

## Âm thanh — IngameSoundManager (18/10)
- `Scripts/Audio/IngameSoundManager.cs` (singleton, object `IngameSoundManager` trong scene, AudioSource gán sẵn) giữ mọi SFX và thông số; script gameplay chỉ gọi `PlayDishLaunch`, `NextPortionPitch` + `PlayFoodLand`, `PlayChop(food)`, `PlayPrepared`, `PlayServe`, `PlayCustomerHappy`.
- Tiếng chặt chỉ lấy từ `FoodIconConfig` (tiếng riêng từng món + biến thể `_2/_3`); đã bỏ tiếng chặt chung thịt/rau/mặc định. Tiếng pop rơi xuống slot: một clip, cao độ ngẫu nhiên 0.85–1.2, các phần cùng đĩa chênh ≥ 6%. Tiếng phục vụ khách giới hạn 0.12 s, chuỗi cao dần.
- Đã xoá: OrderItemDone.wav, ChefChop*.wav, FoodLandPop_2/_3. `OneShotEffects` giờ chỉ lo particle.

## Nguyên liệu ẩn / đóng băng (18/10)
- `OrderFood.isHidden`, `isFrozen`, `blockValue` (≥1). Level Editor: dấu ? vàng cho ô ẩn, phủ xanh + số cho ô đóng băng.
- Đĩa đóng băng không chọn được; mỗi lần chọn thành công một đĩa, mọi đĩa băng trên board giảm `blockValue` 1; về 0 thì tan. Ẩn hoặc đóng băng → ẩn nhãn số lượng.
- Ảnh băng: `Art/Sprites/Blocks/Ice/IceBlock_{Frost,Cracked,Faceted,Clear}.png`, mảng `FoodBoardManager.iceBlockSprites` (do agent khác thêm), index = blockValue − 1; `Food.SetIceBlockSprites`.
- Shader `Art/Shaders/IceSurface.shader` + `Art/Materials/IceSurface.mat`: sương viền, dải sáng, lấp lánh (vết nứt thủ tục không dùng nữa). Bị trừ băng: khối băng nảy + vụn `Prefabs/Effects/IceHit.prefab`; tan: `IceBreak.prefab` (sprite `FxIceShard.png`).
- `Art/Sprites/Effects/IceBlock.png` (bản cũ) không còn dùng.

## Khác (18/10)
- `FoodBoardManager.columnSpacing`: x cột = (c − (n−1)/2) × spacing, 0 = chồng nhau.
- Đĩa vừa chọn lên slot trống sắp tới trạm sơ chế sớm nhất (`ConveyorChefMovement.GetDistanceToStation`).
- Hàng khách chờ hiện sẵn (nhỏ, tối, dồn lên); tự tạo hình nếu scene không có khách chờ (`queueSpacing`).
- Debug: `AutoPlayer` + `GameplayRecorder`, menu Tools → HotPot → Auto Play And Record, video ở `Recordings/` (đã gitignore cùng `tmp/`, `Assets/_Recovery`).
- Lưu ý: nhiều agent sửa song song — 18/10 Claude ghi đè `Food.cs` của agent khác (đã viết lại `SetIceBlockSprites`). Nên chia file theo agent.

## Công cụ
- Tool Setup dựng prefab (CustomerSetup, BoosterSetup, …) đã bị xoá theo yêu cầu người dùng; prefab/config chỉnh trực tiếp.
- Level Editor (`Tools/FoodPop/Level Editor`): danh sách món mỗi khách ("+ Món", khoá ở 4); dropdown có ảnh + tìm kiếm; bấm ảnh lớn mở bảng hình 8×5.
- `FoodBoardManager`: căn giữa cột, ảnh bàn 3/4/5 làn; nút Update View trên Inspector (02/11). LevelConfig_01 (người dùng sửa): bảng 3 cột; trái [Sake2], [Maguro2 + Sake3], [Maguro3]; phải [Ebi2], [Tamago3].

## Việc còn treo
- Play test 04–06/11: rung khi chạm đĩa / món trên thẻ; Salt/Ice lên băng là khách nhận ngay; các class đã sắp lại thứ tự vẫn compile; component không còn ô Ingame Config.
- Có thể thêm hiệu ứng riêng (ting/poof) khi món không cần sơ chế đáp xuống đĩa.
- Play test 03/11: nhãn số lượng đĩa đôi; món thô ẩn khi tới thớt, hiện lại lúc poof.
- Xác nhận lỗi không pick được dish (02/11) đã hết; gán lại `Slot Food` cho các ConveyorSlot; tắt `logPickFailures` khi xong.
- Play test xếp chồng; chỉnh tỉ lệ lớp/món theo ảnh khay.
- Có thể: khay trượt đi cùng khách; Level Editor cảnh báo khi cơm nằm sâu trong bảng; dùng ảnh Final cấp trung gian khi chồng trùng một bước công thức.
- Nguy cơ kẹt băng khi topping chờ cơm — cần luật xử lý nguyên liệu thừa trên băng.
- `Prefabs/Effects/FoodLandEffect.prefab` không còn dùng.

## Cài đặt audio và rung (10/11/2026)
- PlayerSettingsController trên IngameSoundManager mute AudioSource SFX theo IsSoundOn, mute AudioSource Background Music theo IsMusicOn và đặt HapticController.hapticsEnabled theo isVibrateOn. Đăng ký StateChanged trong OnEnable, gỡ trong OnDisable, thứ tự 100 để hồ sơ đã khởi tạo.
- Cờ dùng dữ liệu PlayerData sẵn có, thay đổi qua API PlayerDataManager và lưu một lần. Sound/Music độc lập. Background Music đã loop và được nối nhưng chưa gán clip; không dùng nhạc demo thư viện. Play Mode đã kiểm tra mute/cờ rung và lưu; chưa xác nhận rung trên thiết bị hoặc nghe nhạc nền.

UI click (10/11/2026): IngameSoundManager có clickSound và PlayClick() phát UIClick.wav đã gán trên source SFX hiện có. CanvasPause kế thừa trực tiếp UICanvasPrime, sáu nút gọi PlayHaptic/PlaySoundClick theo cài đặt; không phụ thuộc LevelPopupView. Play Mode xác nhận click phát cả lúc timeScale=0 và bị chặn khi Sound tắt; chưa xác nhận cảm nhận rung trên thiết bị thật.


## Cập nhật cảnh báo thoát và âm thanh (10/11/2026)
Quy tắc hiện hành thay thế mô tả luôn mở cảnh báo trước đây: Home/Replay từ Pause chỉ hiện WarningQuit nếu CustomerManager.HasCompletedOrder=true (đã giao đủ một order trong lượt chơi). Chưa có order hoàn tất: đi thẳng, không trừ life. Sau order đầu: Confirm mới gọi UseHeart; unlimited không trừ. LoadCustomers reset cờ khi tải/chơi lại màn. Giao một phần hoặc bỏ qua order rỗng không tính.

IngameSoundManager dùng 10 clip trong Assets/HotPot/Audio/Sounds: click=SFX_INGAME_CLICK; mở/đóng popup=sfx_ui_popup_show/sfx_ui_popup_disappear; mua thành công=SFX_INGAME_BUY_ITEM; thưởng Watch booster=SFX_INGAME_REWARD_COLLECT; đề nghị revive=SFX_INGAME_OUT_OF_SPACE; giới thiệu booster=SFX_INGAME_NEW_ITEM_UNLOCK; thắng/thua=SFX_INGAME_GAME_WIN/SFX_INGAME_GAME_FAILED; hồi sinh Ads hoặc Coin thành công=sfx_game_revive. UICanvasPrime phát âm vòng đời popup, UIManager phát kết quả màn. Mọi clip dùng nguồn SFX chung nên tuân theo Sound, hoạt động khi pause. Giữ âm chế biến/phục vụ hiện có; thư mục này không cung cấp nhạc nền mới.

Kiểm chứng: Unity biên dịch thay đổi và đã gán/lưu đủ 10 clip trong scene, mỗi clip có thời lượng >0. Chưa kiểm tra nghe trực tiếp hoặc toàn bộ luồng chơi sau thay đổi này.

Kiểm tra bổ sung: Play Mode phát được cả 9 sự kiện và click khi timeScale=0; cờ order ban đầu=false. UIButtonClick phát tiếng click trước callback. Đã thoát Play Mode. Chưa kiểm thử đầy đủ hai nhánh Home/Replay và nghe chất lượng âm trực tiếp.

### Tiếng nhấn khi mở popup
Nút mở popup không phát SFX_INGAME_CLICK; giữ rung/hiệu ứng nút và âm popup. UIButtonClick so sánh ActivePopup trước/sau callback để chỉ bỏ tiếng nhấn khi thực sự mở popup (booster đủ số lượng vẫn có tiếng nhấn). Các chuyển popup sau hiệu ứng đóng như Pause→WarningQuit, WarningQuit→Pause và Revive→Lose chỉ phát rung ở nút. Nút đổi setting, đóng popup, mua/thưởng và điều hướng trực tiếp vẫn giữ tiếng nhấn.
