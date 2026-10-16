# Project rules

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
- Main game content belongs under `Assets/CardDrop`. Follow `Assets/CardDrop/Documentation/AssetFolders.md` and the existing feature subfolders:
  - Sprites/textures: `Art/Sprites/<feature>`.
  - Source fonts and TMP font assets: `Art/Fonts`; font material presets: `Art/Fonts/Materials`.
  - Shaders: `Art/Shaders/<feature>`.
  - Materials: `Art/Materials/<feature>`; particle materials: `Art/Materials/Particles`.
  - Meshes: `Art/Meshes/<feature>`.
  - Animation clips and Animator controllers: `Animations/<feature>`.
  - Prefabs: `Prefabs`; scenes: `Scenes`; configuration assets: `Config`.
  - Runtime scripts: `Scripts/<feature>`; editor-only tools: `Editor`; documentation: `Documentation`.
- Create a clearly named feature subfolder when needed. Do not mix sprites, shaders, materials, fonts, animations, or code in one folder, or leave generated assets loose in the project root.
- Keep temporary outputs and source-art backups outside `Assets`, following the existing workspace conventions.
- Preserve `.meta` files and GUIDs when moving existing Unity assets. Update path-based loaders and documentation, and verify consuming references after moves.
- Do not reorganize external libraries or folders outside `Assets/CardDrop` unless explicitly requested.
- Quy tắc bắt buộc: mọi file được tạo, sinh, import hoặc sao chép phải nằm đúng thư mục theo loại và chức năng, theo cấu trúc trên. Không để lẫn sprite, font, shader, material, animation và code. Khi di chuyển asset Unity phải giữ `.meta`/GUID, cập nhật đường dẫn và kiểm tra tham chiếu. File tạm và bản sao lưu ảnh nguồn phải ở ngoài `Assets`; không sắp xếp thư viện ngoài hoặc thư mục ngoài `Assets/CardDrop` khi chưa được yêu cầu.





