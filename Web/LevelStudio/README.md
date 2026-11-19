# FoodPop Level Studio

Source editor and demo: public/studio/. Generated deployment: dist/.

Shared storage uses Sites authentication and D1 (DB). Every admitted signed-in site visitor can read and save shared levels. Author IDs are supplied by the hosting server; email/name are display fields. Source access and site visitor access remain separate.

Local: npm run dev -- --hostname 127.0.0.1 --port 4173. The starter's loopback-only sign-in uses a development account; it is absent from production. Apply generated migrations to local DB before preview.

Tests: node verify.cjs; node verify-play.cjs; node verify-ingredient-orders.cjs; node verify-shared.mjs (requires local dev server and migrated local DB).

Manual save creates immutable revisions. Optimistic concurrency rejects stale writes. Retried operation IDs do not create duplicate versions. Loading a shared version backs up an existing local draft to IndexedDB. Local storage and local history never upload automatically.

Keep .openai/hosting.json project_id and DB binding stable. Publish with the Sites source workflow; production migrations run during publishing. Never commit .wrangler, .sites-runtime, credentials or local environment files.

Level identity is its trimmed NFC-normalized name. named_levels enforces a unique name key; named_revisions stores its ordered history. Original legacy tables remain archived. The authenticated migrate-names action atomically copies duplicate histories into one series without changing legacy rows. Sync replaces all local copies of that name after backing them up, rather than appending another local level.

Web orders use customerQueue as the authoritative sequence. Legacy two-lane data is interleaved left/right on first edit. The web demo fills two seats from the same sequence whenever either seat becomes available. Unity .asset export preserves customerQueue and useUnifiedCustomerQueue; Unity runtime uses the same shared sequence, with legacy left/right interleaving only as an import fallback.


Orders use `useIngredientOrders` and `ingredientOrders[].ingredients` as authoritative data (up to four groups, one to six ingredients per playable group). The first entry is the base; repeated entries preserve quantities. Legacy finalFoods resolves through the catalog only when no authoritative groups exist. Matching recipe names/artwork are optional, so unnamed combinations can be served.

Customer classifications are `Normal = 0`, `Boss = 1`, `Shipper = 2`. Boss/Shipper expose `timeServe` in seconds, default 30; custom values must be finite and positive. Normal hides the control and preserves its stored value when switching types. Server saves and Unity .asset round trips retain the value. The demo counts down Boss/Shipper serving time, pauses on timeout, and supports a free timer-only playtest revive. Normal has no countdown.

See `../../Assets/HotPot/Documentation/LevelStudioWeb.md` and `../../claude/CustomerManager.md` for the current UI, data contract and runtime behavior.

Chef preparation (2026-10-29): the original plate remains occupied during preparation and return. A clockwise progress ring follows that plate; food returns to the same moving plate, and the ring disappears on landing. Tests cover full-belt reservation, original-plate return, conservation and restart.

Unity gameplay sync (2026-11-12): chef prioritizes landed raw food anywhere by current customer need; board picks may bypass belt travel, or route exactly one overflow portion directly to a reserved order when N=S+1. Prepared food serves anywhere after the scene landing delay (0.3s). Current Unity Spine export and 25 action mappings are in chef-v3/catalog.gameplay; action override 0 uses animation duration. Run node verify-runtime-sync.cjs for boundary regressions. Flight geometry and economy/booster flows remain simplified. Prior difficulty simulation results require rerunning with this engine.

## Đồng bộ web từ Unity local — 12/11/2026, cập nhật sau v24

Mục này thay thế mô tả v24 về timer và giới hạn resource/hình học. Web mô phỏng bằng JavaScript, không chạy C# trực tiếp.

- Board: dồn theo footprint; cổng hai cột đĩa ngang; hidden đầu màn mở ngay, sau khi dồn chờ shift/fade. Lấy trọn nhóm nối, kiểm tra băng/nắp và sức chứa chung.
- Conveyor/Chef: 10 slot theo scene, spline 512 mẫu và vị trí đầu bếp local; mặc định 14,918 giây/vòng; thông số bay và 25 action Spine local, mix 0. Giữ slot nguồn và nhu cầu một phần đi thẳng.
- Customer: một queue/hai chỗ, thứ tự đến, đế trước, một incoming/món, chờ đáp 0,3 giây; ghép/rời quầy theo cấu hình. Boss/Shipper vẫn đếm về 0 nhưng không xử thua: LevelController local hiện không kiểm tra ServeTimeExpired.
- Kẹt băng: đợi hết hoạt động, kiểm tra pick/service rồi ReviveOffered; chấp nhận dùng Clean, từ chối Lost. Pre-win tăng tốc 1,4. Chơi lại Hard lần hai dùng _Easy nếu có; thắng có nút màn tiếp.
- Có Pickup/Clean/Shuffle/AutoComplete và túi theo luật local, giữ raw/prepared; Clean không lấy phần đầu bếp đang sở hữu. Nút playtest miễn phí, không mô phỏng Coin/Ads hay dữ liệu tài khoản.
- Resource: 169 PNG, 13 âm thanh, 102 mô tả prefab món nhiều lớp, cấu hình 99 component và Spine đầu bếp. Không tải toàn bộ project/Library. Shader, ParticleSystem, animation prefab không chạy nguyên bản; web dùng hình/lớp và thời lượng đã xuất. Cucumber FoodType7 thiếu tham chiếu Raw/Prepared trong config Unity, demo giữ trạng thái thiếu hình.
- scripts/sync-unity-runtime.cjs tạo catalog.unity từ export local, kèm dấu vân tay 95 script. Đây là snapshot khi build, không tự cập nhật khi Unity thay đổi.
- Đã chạy verify.cjs, verify-ingredient-orders.cjs, verify-play.cjs, verify-runtime-sync.cjs, verify-unity-parity.cjs và TypeScript --noEmit. Bao gồm 528 trường hợp sức chứa lấy kết quả trực tiếp từ Unity; deadlock/revive/decline, bag, Pickup, Shuffle, AutoComplete, pre-win/thắng. Preview trình duyệt kiểm tra resource/giao diện; preview tĩnh không kiểm chứng API server. Chưa kiểm chứng tương đương từng frame hoặc toàn bộ level bằng người chơi.
