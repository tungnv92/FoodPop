# Project rules

## Mandatory implementation simplicity

- Write focused, readable code that fits the requested scope and the existing project architecture. Do not introduce speculative abstractions, fallback paths, snapshots, backups, rollback, or retry systems without a concrete requirement.
- Trust the project's runtime guarantees: `ProfileManager.instance` and `PlayerDataManager` exist at their runtime call sites. Access them directly instead of adding null checks or null-conditional access. Edit Mode previews must avoid runtime dependencies.
- Cache shared manager references in class fields when several methods use them. For `CanvasBuyBooster`, cache `PlayerDataManager` in `OnEnable`; use that same reference for Coin display, purchases, and unsubscribe.
- Prefer lifecycle callbacks and existing events over per-frame polling. Register Coin listeners and read the initial balance in `OnEnable`, then unsubscribe in `OnDisable`; do not move binding back into `Update`.
- Keep offline saving simple: apply validated gameplay changes, attempt one save for a purchase, and handle write errors centrally in `SaveService` with a warning. Do not restore old resource/tracking state or cached JSON when saving fails. Preserve business validation such as sufficient funds and full reward capacity.
- Extract helpers only when they clarify a coherent responsibility. Avoid unnecessary wrappers, repeated lookups, duplicate checks, and unrelated changes. Verify changes in proportion to their impact and report only checks actually run.
- VI: Viết code gọn, rõ trách nhiệm, đúng phạm vi yêu cầu và kiến trúc hiện có. Không tự thêm abstraction, nhánh dự phòng, snapshot, backup, rollback hoặc retry khi chưa có nhu cầu cụ thể.
- VI: Tin vào bảo đảm runtime của project: `ProfileManager.instance` và `PlayerDataManager` luôn tồn tại tại nơi gọi. Truy cập trực tiếp, không kiểm tra null hoặc dùng `?.` cho hai manager. Xem trước Edit Mode phải tránh phụ thuộc runtime.
- VI: Cache manager dùng chung vào field của class. `CanvasBuyBooster` cache `PlayerDataManager` trong `OnEnable`, dùng cùng tham chiếu để hiển thị Coin, mua hàng và gỡ listener.
- VI: Ưu tiên callback vòng đời và sự kiện hiện có thay vì polling mỗi frame. Đăng ký Coin và đọc số dư ban đầu trong `OnEnable`, gỡ trong `OnDisable`; không đưa binding trở lại `Update`.
- VI: Lưu offline đơn giản: áp dụng thay đổi gameplay đã kiểm tra, thử lưu một lần khi mua và chỉ xử lý lỗi ghi tập trung bằng cảnh báo trong `SaveService`. Không khôi phục tài nguyên/thống kê hoặc JSON cũ khi lưu lỗi. Vẫn giữ kiểm tra đủ tiền và sức chứa toàn bộ phần thưởng.
- VI: Chỉ tách helper khi làm rõ trách nhiệm; tránh hàm bọc không cần thiết, tra cứu lặp, kiểm tra trùng và sửa ngoài phạm vi. Kiểm chứng phù hợp mức ảnh hưởng và chỉ báo những kiểm tra thực sự đã chạy.

## Mandatory waiting-column entrance mapping

- The waiting row has five fixed columns and the top board row has six tiles. Waiting column c enters only top tile c or c+1 (one-based numbering). Prefer the matching queue column, then the left neighbor at an empty top tile; never use a distant column.
- Queue data is stored in rows of five, starting with the visible row. Preserve card order and totals when updating layouts.
- FindPathConfig stores only adjacent upward sources per tile. Compute collapse routes at runtime, visiting columns closest to the original pick first and breaking equal-distance ties to the left; prefer a complete route, otherwise retain the longest movable prefix. Each step uses an adjacent tile above; diagonal paths may continue horizontally across successive rows without a total drift limit. Vertical anchors are no longer mandatory.
- Board gravity works independently of reserve availability or a complete path to the top. Cards below a blocking tile still fall along a valid configured prefix. After block damage/breaking, resettle every existing empty cell.
- Recheck after every individual landing. Queue columns enter independently once the card departing their own entrance has traveled one quarter of a tile, using the shared fall speed. Cards still entering are not gravity sources. Preserve pause and cancel pending refill on replay/destruction.
- VI: Hàng chờ có năm cột và hàng đầu board có sáu tile. Cột chờ c chỉ vào tile c hoặc c+1 (từ 1); tile trống ưu tiên cột queue cùng chỉ số rồi cột kề trái, không lấy cột xa. Dữ liệu queue chia thành hàng năm card, giữ thứ tự và tổng số card.
- VI: FindPathConfig chỉ lưu nguồn kề hàng trên cho mỗi tile. Tính đường lúc chạy, duyệt cột gần cột chọn gốc trước, bằng khoảng cách ưu tiên trái; ưu tiên đường hoàn chỉnh, nếu bị chặn giữ nhánh dồn dài nhất. Mỗi bước dùng tile kề hàng trên, đường chéo được tiếp tục qua nhiều hàng, không giới hạn lệch ngang tổng cộng và không bắt buộc mốc dọc. Card dưới block vẫn dồn độc lập với queue; sau sát thương/phá block phải dồn mọi ô trống.
- VI: Kiểm tra lại sau từng card tiếp đất. Các cột queue vào độc lập khi card rời đúng cửa đi được một phần tư tile, dùng tốc độ rơi chung. Card chưa vào xong không làm nguồn dồn; tuân theo pause và hủy bổ sung khi replay/hủy pool.

## Mandatory clear function structure

- Split functions into focused helpers when doing so makes the class clearer and easier to follow. Each function should have one coherent responsibility.
- Keep lifecycle methods such as `Awake`, `Start`, `Update`, `LateUpdate`, and callbacks concise: use them to coordinate well-named helpers for distinct behavior instead of embedding several unrelated responsibilities.
- Separate independently understandable behavior, such as `UpdateIceSurface` and `UpdateBushSurface`, into appropriately named functions. Pass required inputs explicitly when practical.
- Avoid unnecessary fragmentation: do not extract trivial statements or add wrappers that make the flow harder to trace. Use judgment rather than a fixed function length limit.
- Preserve existing behavior when restructuring code, and follow the mandatory EN/VI comment rule for every new or modified function.
- Quy tắc bắt buộc: chia hàm theo từng trách nhiệm khi giúp class rõ ràng và dễ theo dõi hơn. Giữ các hàm vòng đời và callback gọn, dùng chúng để điều phối các hàm có tên diễn đạt đúng chức năng. Tách các xử lý độc lập như mặt băng và mặt bụi cây; truyền đầu vào rõ ràng khi phù hợp. Không chia vụn các câu lệnh đơn giản hoặc tạo hàm bọc khiến luồng khó đọc hơn. Khi tách hàm phải giữ hành vi hiện tại và thêm/cập nhật comment EN/VI cho mọi hàm mới hoặc sửa đổi.

## Mandatory member order by MonoBehaviour lifecycle

- Order class members so the file reads in the same order Unity runs it. This is mandatory for every new class and for every existing class that is created or modified.
- Member order inside a class:
  1. Nested types and constants, then public properties and events.
  2. Serialized fields (`[SerializeField]`, public Inspector fields), grouped by purpose, then private runtime state.
  3. Unity callbacks in execution order: `Awake` → `OnEnable` → `Reset` / `OnValidate` (editor) → `Start` → `FixedUpdate` → `Update` → `LateUpdate` → render/gizmo callbacks (`OnGUI`, `OnDrawGizmos`, `OnDrawGizmosSelected`) → `OnApplicationFocus` / `OnApplicationPause` / `OnApplicationQuit` → `OnDisable` → `OnDestroy`. Collision, trigger and pointer callbacks follow `LateUpdate`.
  4. Public API in gameplay flow order (for example load → play → revive/decline → replay/next).
  5. Private helpers and coroutines in the order they are first called, placed after the public methods that use them.
- When modifying a class whose members are out of order, reorder that class in the same task. Reordering must be pure moves: no behavior changes, keep every EN/VI comment attached to its member, and verify that no lines were lost or altered. Unity serializes fields by name, so moving fields does not lose Inspector values.
- Do not reorder classes outside the scope of the task or third-party code.
- Cross-cutting concerns such as Tracking may be grouped in a named `#region` placed after the private helpers. Inside the region, members follow the same first-call order. Do not move members out of an existing region when reordering.
- VI: Quy tắc bắt buộc: sắp xếp thành viên trong class theo đúng thứ tự Unity chạy, áp dụng cho mọi class mới và mọi class được tạo hoặc sửa.
- VI: Thứ tự: (1) kiểu lồng, hằng số, property và event công khai; (2) field serialize theo nhóm chức năng, rồi tới biến trạng thái riêng; (3) callback Unity theo thứ tự chạy `Awake` → `OnEnable` → `Reset`/`OnValidate` → `Start` → `FixedUpdate` → `Update` → `LateUpdate` → callback vẽ/gizmo → `OnApplicationFocus`/`OnApplicationPause`/`OnApplicationQuit` → `OnDisable` → `OnDestroy`, callback va chạm/trigger/pointer đặt sau `LateUpdate`; (4) API công khai theo luồng gameplay; (5) hàm private và coroutine theo thứ tự được gọi lần đầu, đặt sau hàm công khai dùng chúng.
- VI: Khi sửa một class đang sai thứ tự, sắp xếp lại class đó ngay trong cùng nhiệm vụ. Chỉ di chuyển, không đổi hành vi, giữ comment EN/VI đi cùng hàm và kiểm tra không mất hoặc sửa dòng nào. Không sắp xếp class ngoài phạm vi nhiệm vụ hoặc code bên thứ ba.
- VI: Phần chức năng cắt ngang như Tracking được phép gom vào một `#region` có tên, đặt sau các hàm private. Bên trong region vẫn theo thứ tự gọi lần đầu. Khi sắp xếp lại class, không đưa thành viên ra khỏi region có sẵn.

## Mandatory bilingual function comments

- Whenever adding a new function or modifying an existing function, add or update explanatory comments in both English (EN) and Vietnamese (VI).
- This rule is mandatory for all project code, including runtime scripts, editor tools, validation/test helpers, constructors, callbacks, coroutine methods, and expression-bodied methods.
- Place the paired comments directly above the function. Describe its purpose and relevant behavior; keep both language versions consistent with the implementation.
- Use the existing project convention:

```csharp
// EN: Describe what the function does and why.
// VI: Mô tả chức năng của hàm và mục đích của nó.
public void ExampleFunction()
{
    // Implementation.
}
```

- Existing comments in only one language must be supplemented with the other language when the function is modified. Update both comments whenever its behavior changes.
- Quy tắc bắt buộc: mọi hàm được thêm mới hoặc sửa đổi phải có comment bằng cả EN và VI, theo mẫu trên; nội dung comment phải đúng với chức năng hiện tại của hàm.

## Mandatory Vietnamese-only tooltips

- Every `[Tooltip]` must be written in Vietnamese only. Do not add English text or "EN / VI" pairs inside tooltips.
- This applies to new fields and to any field whose tooltip is added or edited. Bilingual EN/VI comments above functions are still mandatory; this rule only covers Inspector tooltips.
- Quy tắc bắt buộc: mọi `[Tooltip]` chỉ viết bằng tiếng Việt, không thêm tiếng Anh hay cặp "EN / VI" trong tooltip. Áp dụng cho field mới và mọi tooltip được thêm hoặc sửa. Comment EN/VI phía trên hàm vẫn bắt buộc như cũ; quy tắc này chỉ áp dụng cho tooltip trong Inspector.

## Mandatory file and asset organization

- Every newly created, generated, imported, or copied project file must be placed in the correct folder for its type and gameplay feature. This is mandatory alongside bilingual function comments.
- Main game content belongs under `Assets/HotPot`, in these folders (follow the existing feature subfolders):
  - Sprites/textures: `Art/Sprites/<feature>`.
  - Source fonts and TMP font assets: `Art/Fonts`; font material presets: `Art/Fonts/Materials`.
  - Shaders: `Art/Shaders/<feature>` (never a separate `Shaders/` folder).
  - Materials: `Art/Materials/<feature>`; particle materials: `Art/Materials/Particles`.
  - Meshes: `Art/Meshes/<feature>`.
  - Audio clips and mixers: `Audio/<feature>`.
  - Animation clips and Animator controllers: `Animations/<feature>`.
  - Prefabs: `Prefabs/<feature>` (gameplay objects: `Prefabs/Ingame`); scenes: `Scenes`.
  - Configuration assets: `Config`; shared config `Config/Resources/IngameConfig.asset`; levels `Config/Resources/Levels/LevelConfig_NN.asset`.
  - Runtime scripts: `Scripts/<feature>`; editor-only tools: `Editor`.
  - Documentation never goes under `Assets`; it lives in the root `Documentation/` folder (see "Documentation layout").
- Sprite and texture images must have width and height that are multiples of 4 so Android can compress them to ETC2; otherwise Unity keeps them uncompressed (RGBA32). Also keep `Max Size` at or above the image size so Unity does not downscale it to an odd size.
- Exception — reusable meta-feature packages (DailyGift, DailyReward, LavaQuest, Heart, UserProfile, … and the shared feature-module framework) live under `Assets/Packages`, not `Assets/HotPot`: framework in `Packages/FeatureModules/` (`Runtime/`, `Editor/`); each feature in `Packages/<Feature>/` with `Runtime/` (feature, data, module, config classes), `UI/` (MonoBehaviour UI scripts), `Configs/` (config and `<Feature>Module` assets; sub-assets in subfolders such as `Configs/Rewards/`), `Prefabs/` (`Prefabs/Resources/` only for prefabs loaded by `Resources.Load`), `Sprites/` for the package's own images, `Editor/` when needed, and `Legacy/` for old code kept only until migration is verified. See `Documentation/Features/FeatureModules.md`.
- Create a clearly named feature subfolder when needed. Do not mix sprites, shaders, materials, fonts, animations, or code in one folder, or leave generated assets loose in the project root.
- Keep temporary outputs and source-art backups outside `Assets`, in the workspace `tmp` and `output` folders.
- Preserve `.meta` files and GUIDs when moving existing Unity assets. Update path-based loaders and documentation, and verify consuming references after moves.
- Do not reorganize external libraries or folders outside `Assets/HotPot` unless explicitly requested.
- VI: Ngoại lệ — package tính năng meta dùng lại (DailyGift, DailyReward, LavaQuest, Heart, UserProfile… và khung feature-module chung) nằm trong `Assets/Packages`, không đặt trong `Assets/HotPot`: khung ở `Packages/FeatureModules/` (`Runtime/`, `Editor/`); mỗi tính năng ở `Packages/<Tính năng>/` gồm `Runtime/` (feature, data, module, class config), `UI/` (script UI MonoBehaviour), `Configs/` (config và asset `<Tính năng>Module`; asset con để thư mục con như `Configs/Rewards/`), `Prefabs/` (`Prefabs/Resources/` chỉ cho prefab nạp bằng `Resources.Load`), `Sprites/` cho ảnh riêng của package, `Editor/` khi cần, và `Legacy/` cho code cũ chỉ giữ tới khi kiểm chứng xong việc chuyển đổi. Xem `Documentation/Features/FeatureModules.md`.
- Quy tắc bắt buộc: mọi file được tạo, sinh, import hoặc sao chép phải nằm đúng thư mục theo loại và chức năng, theo cấu trúc trên. Ảnh sprite/texture phải có chiều rộng và cao chia hết cho 4 để nén ETC2 trên Android (không thì Unity để RGBA32 không nén), và `Max Size` không nhỏ hơn kích thước ảnh để Unity không thu nhỏ thành kích thước lẻ. Không để lẫn sprite, font, shader, material, âm thanh, animation và code; font và shader luôn nằm trong `Art/` (`Art/Fonts`, `Art/Shaders/<tính năng>`), âm thanh trong `Audio/<tính năng>`. Khi di chuyển asset Unity phải giữ `.meta`/GUID, cập nhật đường dẫn và kiểm tra tham chiếu. File tạm và bản sao lưu ảnh nguồn phải ở ngoài `Assets`; không sắp xếp thư viện ngoài hoặc thư mục ngoài `Assets/HotPot` khi chưa được yêu cầu.

## Mandatory automatic Markdown documentation updates

- Whenever a change affects behavior, configuration, assets, workflows, or rules described in an existing `.md` file, automatically update the relevant Markdown documentation in the same task, without waiting for the user to request it.
- Find and read the relevant documentation before updating it; all project documentation lives under `Documentation/` (see "Documentation layout" below). Update existing documents in place instead of creating duplicate descriptions.
- Keep documentation consistent with the final implementation: revise outdated behavior, relevant usage instructions and references; record meaningful verification results and distinguish verified behavior from untested expectations. Do not claim tests that were not run.
- Documentation updates are part of completing the change. Briefly mention the updated document in the completion response. If a feature has no existing document, follow the project's file organization rules when creating documentation that is needed.
- VI: Mỗi khi thay đổi hành vi, cấu hình, asset, quy trình hoặc quy tắc đã được mô tả trong file `.md`, bắt buộc tự cập nhật tài liệu liên quan ngay trong cùng nhiệm vụ, không chờ người dùng yêu cầu.
- VI: Tìm và đọc tài liệu liên quan trước khi cập nhật; toàn bộ tài liệu của dự án nằm trong `Documentation/` (xem mục "Documentation layout" bên dưới); sửa trực tiếp tài liệu hiện có, tránh tạo bản mô tả trùng lặp.
- VI: Tài liệu phải khớp triển khai cuối cùng: sửa nội dung lỗi thời, hướng dẫn sử dụng và tham chiếu liên quan; ghi kết quả kiểm tra có ý nghĩa, phân biệt phần đã kiểm chứng với phần chưa kiểm tra. Không ghi đã chạy kiểm tra nếu thực tế chưa chạy.
- VI: Cập nhật tài liệu là một phần bắt buộc để hoàn tất thay đổi; nhắc ngắn gọn file đã cập nhật khi báo hoàn tất. Nếu tính năng chưa có tài liệu, tuân thủ quy tắc tổ chức file của dự án khi cần tạo tài liệu mới.

## Documentation layout (mandatory)

- Every Markdown document or rule file (new or existing) lives under `Documentation/` at the project root. Never create `.md` files anywhere else (not in `Assets/`, `claude/`, `output/`, `design/`, …). Exceptions: `AGENTS.md`, `CLAUDE.md` and `README.md` at the root, and a `README.md` next to a standalone tool such as `Web/LevelStudio/`.
- `Documentation/Rules/` — project rules; every file here is binding for all agents (Codex, Claude, Multi Agents workers).
- `Documentation/Features/` — feature and system documentation, one flat folder (no per-area subfolders): gameplay systems, UI, boosters, config, data, editor and web tools.
- `Documentation/Handoff/` — only handoff notes between agents and sessions (status, open work, what was verified), e.g. `foodpop-handoff.md` as the entry point. Describe how a system works in `Features/`, not here.
- `Documentation/Design/<feature>/` — UI/art specs and mockups from the artist; image-generation prompts in `Documentation/Design/Prompts/`.
- `Documentation/Chats/` — chat records written by Multi Agents (`/document`). `Documentation/INDEX.md` is an auto-generated index (statistics + tree); do not edit it by hand.
- Docs were moved here on 2026-11-13 (`claude/*` → `Documentation/Handoff/`, `Assets/HotPot/Documentation/*` → `Documentation/Features/`, `output/**/prompts*.md` → `Documentation/Design/Prompts/`). Use the new paths in all references.
- On 2026-11-17 `Features/HotPot/*` was flattened into `Features/`, and the system docs `Booster`, `UI`, `Conveyor`, `CustomerManager`, `LevelEditor` and `foodpop-FoodBoardManager` (renamed `FoodBoardManager.md`) moved from `Handoff/` to `Features/`.
- VI: Mọi tài liệu Markdown và file quy tắc (mới hay cũ) nằm trong `Documentation/` ở gốc dự án; không tạo `.md` ở nơi khác (không đặt trong `Assets/`, `claude/`, `output/`, `design/`…). Ngoại lệ: `AGENTS.md`, `CLAUDE.md`, `README.md` ở gốc và `README.md` đi kèm công cụ độc lập như `Web/LevelStudio/`.
- VI: `Rules/` = quy tắc (bắt buộc với mọi agent), `Features/` = tài liệu tính năng/hệ thống (một thư mục phẳng, không chia thư mục con theo khu vực), `Handoff/` = chỉ ghi chú bàn giao (trạng thái, việc còn treo, đã kiểm chứng gì), không mô tả cách hệ thống hoạt động, `Design/<tính năng>/` = spec + mockup của Artist, `Design/Prompts/` = prompt tạo ảnh, `Chats/` = tài liệu chat của Multi Agents. `INDEX.md` do app tự sinh, không sửa tay.
- VI: Tài liệu đã được chuyển vào đây ngày 13/11/2026; ngày 17/11/2026 gộp `Features/HotPot/*` lên `Features/` và chuyển tài liệu hệ thống (`Booster`, `UI`, `Conveyor`, `CustomerManager`, `LevelEditor`, `FoodBoardManager`) từ `Handoff/` sang `Features/`. Mọi tham chiếu phải dùng đường dẫn mới.





