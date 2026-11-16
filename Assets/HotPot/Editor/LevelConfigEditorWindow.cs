using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Visual level designer for LevelConfig: edit both customer queues, the raw ingredient board and check that ingredients cover the orders.
    // VI: Công cụ thiết kế level cho LevelConfig: chỉnh hai hàng khách, bảng nguyên liệu thô và kiểm tra nguyên liệu có đủ cho các order.
    public partial class LevelConfigEditorWindow : EditorWindow
    {
        private const string FoodSpriteFolder = "Assets/HotPot/Art/Sprites/Foods/Sushi/Raw/";
        private const string NewLevelFolder = "Assets/HotPot/Config/Resources/" + LevelConfig.ResourceFolder;
        private const string FoodIconConfigPath = "Assets/HotPot/Config/FoodIconConfig.asset";
        private const string DishPrefabFolder = "Assets/HotPot";
        private const float CellWidth = 180f;
        private const float CellHeight = 180f;
        private const float CellGap = 4f;
        private const float BoardIconSize = 44f;
        private const float RowIconSize = 26f;
        private const float DishPreviewSize = 48f;
        private const HideFlags DraftHideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

        private const float LineHeight = 18f;
        private const float LineStep = 20f;

        [SerializeField] private LevelConfig level;
        [SerializeField] private LevelConfig sourceLevel;
        [SerializeField] private string savedDraftJson;
        private static readonly string[] ShapeNames = { "Đĩa đơn", "Đĩa đôi dọc", "Đĩa đôi ngang" };
        private static readonly Color VerticalDoubleColor = new Color(0.85f, 0.35f, 0.19f, 0.95f);
        private static readonly Color HorizontalDoubleColor = new Color(0.11f, 0.62f, 0.46f, 0.95f);

        private static readonly FoodType[] FoodValues = (FoodType[])Enum.GetValues(typeof(FoodType));

        // Vietnamese names follow enum declaration order, including gaps in the numeric IDs.
        private static readonly string[] VietnameseNames =
        {
            "Không có", "Cơm sushi", "Cá hồi", "Cá ngừ", "Tôm", "Thanh cua", "Trứng cuộn",
            "Dưa leo", "Trứng cá hồi", "Lươn", "Bạch tuộc", "Rong biển", "Sò điệp", "Nước tương", "Gừng ngâm", "Wasabi", "Lá trà", "Đá", "Edamame", "Muối", "Đùi gà", "Sốt teriyaki", "Bơ", "Gà viên", "Cánh gà", "Ức gà"
        };

        // Display names such as "Salmon (Cá hồi)": original enum name first, Vietnamese in brackets.
        private static readonly string[] FoodNames = BuildFoodNames();
        private SerializedObject serialized;
        private Vector2 customerScroll;
        private Vector2 boardScroll;
        private Action pendingAction;
        private int levelNumberInput = 1;
        private readonly Dictionary<FoodType, Texture2D> icons = new Dictionary<FoodType, Texture2D>();
        private List<FinalFoodView> dishPrefabs;
        private GUIStyle okStyle, missingStyle, extraStyle;
        private readonly List<string> boardProblems = new List<string>();

        // EN: Clears icon and dish-prefab caches, restores an editable isolated draft after reload, migrates older locked drafts and subscribes to Undo/Redo.
        // VI: Xóa cache icon và prefab món, khôi phục bản nháp độc lập có thể chỉnh sau khi tải lại, sửa bản nháp cũ bị khóa và theo dõi Undo/Redo.
        private void OnEnable()
        {
            icons.Clear();
            dishPrefabs = null;
            saveChangesMessage = "Level có thay đổi chưa lưu. Bấm Lưu để ghi vào asset, hoặc bỏ thay đổi để giữ dữ liệu đã lưu.";
            if (level != null && EditorUtility.IsPersistent(level))
            {
                sourceLevel = level;
                level = null;
            }
            if (level == null && sourceLevel != null)
                CreateDraft(sourceLevel);
            EnsureDraftEditable();
            Undo.undoRedoPerformed += OnDraftUndoRedo;
            RefreshDraftStatus();
        }

        // EN: Edits only the draft, applies deferred actions and updates the unsaved indicator after drawing.
        // VI: Chỉ chỉnh bản nháp, áp dụng thao tác đã hoãn và cập nhật dấu chưa lưu sau khi vẽ.
        private void OnGUI()
        {
            EnsureStyles();
            DrawToolbar();
            if (level == null)
            {
                EditorGUILayout.HelpBox("Chọn một LevelConfig ở thanh trên (hoặc trong Project), hoặc bấm \"Tạo level mới\".", MessageType.Info);
                return;
            }
            if (serialized == null || serialized.targetObject != level)
                serialized = new SerializedObject(level);

            serialized.Update();
            EditorGUILayout.BeginHorizontal();
            DrawCustomerPanel();
            DrawPanelDivider();
            DrawBoardPanel();
            EditorGUILayout.EndHorizontal();
            serialized.ApplyModifiedProperties();
            RunPendingAction();
            RefreshDraftStatus();
        }

        // EN: Removes the Undo listener while keeping the serialized draft available across script reloads.
        // VI: Gỡ theo dõi Undo, giữ bản nháp đã tuần tự hóa để khôi phục sau khi tải lại script.
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnDraftUndoRedo;
        }

        // EN: Releases the temporary draft when the window is actually destroyed.
        // VI: Giải phóng bản nháp tạm khi cửa sổ thực sự bị hủy.
        private void OnDestroy()
        {
            ReleaseDraft();
        }

        // EN: Menu entry that opens the designer with the currently selected LevelConfig (if any).
        // VI: Mục menu mở công cụ thiết kế với LevelConfig đang chọn (nếu có).
        [MenuItem("Tools/FoodPop/Level Editor")]
        public static void OpenFromMenu()
        {
            Open(Selection.activeObject as LevelConfig);
        }

        // EN: Opens (or focuses) the designer window and loads the given level.
        // VI: Mở (hoặc focus) cửa sổ thiết kế và nạp level được truyền vào.
        public static void Open(LevelConfig target)
        {
            LevelConfigEditorWindow window = GetWindow<LevelConfigEditorWindow>("Level Editor");
            window.minSize = new Vector2(980f, 560f);
            if (target != null && target != window.sourceLevel)
                window.SetLevel(target);
        }

        // EN: Handles the window's explicit Save choice when closing with unsaved edits.
        // VI: Xử lý lựa chọn Lưu rõ ràng khi đóng cửa sổ có chỉnh sửa chưa lưu.
        public override void SaveChanges()
        {
            SaveDraft();
            base.SaveChanges();
        }

        // EN: Builds display names "EnumName (Vietnamese)" for every FoodType value.
        // VI: Tạo tên hiển thị dạng "TênEnum (tiếng Việt)" cho mọi giá trị FoodType.
        private static string[] BuildFoodNames()
        {
            string[] names = Enum.GetNames(typeof(FoodType));
            for (int i = 0; i < names.Length; i++)
            {
                if (i < VietnameseNames.Length)
                    names[i] = $"{names[i]} ({VietnameseNames[i]})";
            }
            return names;
        }

        // EN: Loads an asset into an isolated draft after confirming any unsaved edits may be discarded.
        // VI: Nạp asset thành bản nháp độc lập sau khi xác nhận được phép bỏ các chỉnh sửa chưa lưu.
        private void SetLevel(LevelConfig target, bool discardConfirmed = false)
        {
            if (!discardConfirmed && !ConfirmDiscardDraft())
                return;
            ReleaseDraft();
            sourceLevel = target;
            CreateDraft(target);
            int number = target != null ? ParseLevelNumber(target.name) : -1;
            if (number > 0)
                levelNumberInput = number;
        }

        // EN: Requires an explicit discard choice before reloading or switching away from an edited draft.
        // VI: Yêu cầu chọn bỏ thay đổi rõ ràng trước khi tải lại hoặc chuyển khỏi bản nháp đang chỉnh.
        private bool ConfirmDiscardDraft()
        {
            RefreshDraftStatus();
            return !hasUnsavedChanges || EditorUtility.DisplayDialog("Thay đổi chưa lưu",
                "Bỏ các thay đổi chưa lưu và nạp dữ liệu level? Asset chỉ được cập nhật khi bấm Lưu.", "Bỏ thay đổi", "Tiếp tục chỉnh");
        }

        // EN: Tracks draft changes against its saved snapshot for the window's unsaved indicator and close dialog.
        // VI: So sánh bản nháp với mốc đã lưu để hiển thị dấu chưa lưu và hộp thoại khi đóng cửa sổ.
        private void RefreshDraftStatus()
        {
            bool changed = level != null && EditorJsonUtility.ToJson(level) != savedDraftJson;
            if (hasUnsavedChanges != changed)
            {
                hasUnsavedChanges = changed;
                Repaint();
            }
        }

        // EN: Disposes draft serialization and its Undo history without touching the source asset.
        // VI: Giải phóng dữ liệu tuần tự hóa và lịch sử Undo của bản nháp mà không tác động asset nguồn.
        private void ReleaseDraft()
        {
            pendingAction = null;
            serialized?.Dispose();
            serialized = null;
            if (level != null && !EditorUtility.IsPersistent(level))
            {
                Undo.ClearUndo(level);
                DestroyImmediate(level);
            }
            level = null;
        }

        // EN: Clones the source into an editable unsaved object, isolating serialized fields and Undo from the asset.
        // VI: Sao chép nguồn thành đối tượng chưa lưu có thể chỉnh, tách field tuần tự hóa và Undo khỏi asset.
        private void CreateDraft(LevelConfig target)
        {
            if (target != null)
            {
                level = Instantiate(target);
                level.name = target.name;
                level.hideFlags = DraftHideFlags;
                serialized = new SerializedObject(level);
            }
            savedDraftJson = level != null ? EditorJsonUtility.ToJson(level) : null;
            RefreshDraftStatus();
        }

        // EN: Reads the trailing number of an asset name (LevelConfig_07 -> 7); returns -1 when there is none.
        // VI: Đọc số ở cuối tên asset (LevelConfig_07 -> 7); trả về -1 nếu không có.
        private static int ParseLevelNumber(string assetName)
        {
            Match match = Regex.Match(assetName ?? string.Empty, @"(\d+)$");
            return match.Success && int.TryParse(match.Groups[1].Value, out int number) ? number : -1;
        }

        // EN: Removes NotEditable from restored drafts and normalizes snapshot flags without discarding unsaved field changes.
        // VI: Bỏ cờ NotEditable của bản nháp khôi phục và chuẩn hóa cờ trong mốc so sánh mà không bỏ chỉnh sửa field chưa lưu.
        private void EnsureDraftEditable()
        {
            if (level == null || EditorUtility.IsPersistent(level))
                return;
            level.hideFlags = DraftHideFlags;
            if (!string.IsNullOrEmpty(savedDraftJson))
                savedDraftJson = Regex.Replace(savedDraftJson, "\"m_ObjectHideFlags\"\\s*:\\s*\\d+", "\"m_ObjectHideFlags\":" + (int)DraftHideFlags);
        }

        // EN: Refreshes serialized draft fields and repaint after Undo/Redo, including when the draft remains dirty.
        // VI: Làm mới field tuần tự hóa và vẽ lại sau Undo/Redo, kể cả khi bản nháp vẫn có thay đổi chưa lưu.
        private void OnDraftUndoRedo()
        {
            serialized?.Update();
            RefreshDraftStatus();
            Repaint();
        }

        // EN: Commits the draft to its source only on an explicit save and saves only that asset.
        // VI: Chỉ ghi bản nháp vào nguồn khi lưu rõ ràng và chỉ lưu asset đó.
        private void SaveDraft()
        {
            if (sourceLevel == null || level == null)
                return;
            serialized?.ApplyModifiedProperties();
            string sourceName = sourceLevel.name;
            HideFlags sourceFlags = sourceLevel.hideFlags;
            EditorUtility.CopySerialized(level, sourceLevel);
            sourceLevel.name = sourceName;
            sourceLevel.hideFlags = sourceFlags;
            EditorUtility.SetDirty(sourceLevel);
            AssetDatabase.SaveAssetIfDirty(sourceLevel);
            savedDraftJson = EditorJsonUtility.ToJson(level);
            RefreshDraftStatus();
            ShowNotification(new GUIContent("Đã lưu level"));
        }

        // EN: Creates the coloured label styles used by the validation table.
        // VI: Tạo các style chữ có màu dùng cho bảng kiểm tra.
        private void EnsureStyles()
        {
            if (okStyle != null)
                return;
            okStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.35f, 0.75f, 0.35f) } };
            missingStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.9f, 0.35f, 0.3f) } };
            extraStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.9f, 0.7f, 0.2f) } };
        }

        // EN: Selects source assets while editing drafts, with explicit reload and save actions.
        // VI: Chọn asset nguồn nhưng chỉnh bản nháp, có thao tác tải lại và lưu rõ ràng.
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            LevelConfig picked = (LevelConfig)EditorGUILayout.ObjectField(sourceLevel, typeof(LevelConfig), false, GUILayout.Width(240f));
            if (picked != sourceLevel)
                SetLevel(picked);
            DrawLevelNavigation();
            if (GUILayout.Button("Tạo level mới", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                CreateLevel();
            using (new EditorGUI.DisabledScope(level == null))
            {
                if (GUILayout.Button("Tìm trong Project", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    EditorGUIUtility.PingObject(sourceLevel);
                if (GUILayout.Button("Tải lại", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                    pendingAction = () => SetLevel(sourceLevel);
                if (GUILayout.Button("Lưu", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                    pendingAction = SaveDraft;
                if (GUILayout.Button(new GUIContent("UpdateFoodPrefab", "Quét lại prefab món và gán cho mọi nhóm nguyên liệu prefab có danh sách nguyên liệu khớp"), EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    pendingAction = UpdateFoodPrefabs;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // EN: Back / level number / Load / Next controls for jumping between LevelConfig_NN assets.
        // VI: Nút Back / ô số level / Load / Next để chuyển nhanh giữa các asset LevelConfig_NN.
        private void DrawLevelNavigation()
        {
            using (new EditorGUI.DisabledScope(level == null))
            {
                if (GUILayout.Button("◀ Back", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    GoToNeighbourLevel(-1);
            }
            GUILayout.Label("Level", GUILayout.Width(36f));
            levelNumberInput = Mathf.Max(1, EditorGUILayout.IntField(levelNumberInput, EditorStyles.toolbarTextField, GUILayout.Width(40f)));
            if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                LoadLevelNumber(levelNumberInput);
            using (new EditorGUI.DisabledScope(level == null))
            {
                if (GUILayout.Button("Next ▶", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    GoToNeighbourLevel(1);
            }
            GUILayout.Space(8f);
        }

        // EN: Moves to the previous (-1) or next (+1) existing level by number; at the end, offers to create the next one.
        // VI: Chuyển sang level trước (-1) hoặc sau (+1) theo số thứ tự; nếu đã là level cuối thì hỏi có tạo level tiếp theo không.
        private void GoToNeighbourLevel(int direction)
        {
            GUI.FocusControl(null);
            List<LevelConfig> levels = GetNumberedLevels();
            int current = level != null ? ParseLevelNumber(level.name) : levelNumberInput;
            LevelConfig target = null;
            foreach (LevelConfig candidate in levels)
            {
                int number = ParseLevelNumber(candidate.name);
                if (direction < 0 && number < current)
                    target = candidate;
                if (direction > 0 && number > current)
                {
                    target = candidate;
                    break;
                }
            }
            if (target != null)
            {
                SetLevel(target);
                EditorGUIUtility.PingObject(target);
                return;
            }
            if (direction > 0 && current > 0 &&
                EditorUtility.DisplayDialog("Hết level", $"Đây là level cuối. Tạo mới {LevelAssetName(current + 1)}?", "Tạo mới", "Huỷ"))
                CreateLevelNumber(current + 1);
        }

        // EN: Finds every LevelConfig asset with a number at the end of its name, sorted by that number.
        // VI: Tìm mọi asset LevelConfig có số ở cuối tên, sắp xếp theo số đó.
        private static List<LevelConfig> GetNumberedLevels()
        {
            List<LevelConfig> levels = new List<LevelConfig>();
            foreach (string guid in AssetDatabase.FindAssets("t:LevelConfig"))
            {
                LevelConfig asset = AssetDatabase.LoadAssetAtPath<LevelConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && ParseLevelNumber(asset.name) > 0)
                    levels.Add(asset);
            }
            levels.Sort((a, b) => ParseLevelNumber(a.name).CompareTo(ParseLevelNumber(b.name)));
            return levels;
        }

        // EN: Standard asset name for a level number, shared with runtime Resources loading (LevelConfig.AssetName).
        // VI: Tên asset chuẩn theo số level, dùng chung với việc nạp Resources lúc chạy (LevelConfig.AssetName).
        private static string LevelAssetName(int number)
        {
            return LevelConfig.AssetName(number);
        }

        // EN: Confirms discarding edits, creates only the new numbered asset in Resources/Levels with a matching levelNumber and opens its draft.
        // VI: Xác nhận bỏ chỉnh sửa, chỉ tạo asset mới theo số trong Resources/Levels với levelNumber khớp số và mở bản nháp của nó.
        private void CreateLevelNumber(int number)
        {
            if (!ConfirmDiscardDraft())
                return;
            string path = AssetDatabase.GenerateUniqueAssetPath($"{NewLevelFolder}/{LevelAssetName(number)}.asset");
            LevelConfig asset = CreateInstance<LevelConfig>();
            asset.levelNumber = number;
            asset.ResizeBoard();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            SetLevel(asset, true);
            EditorGUIUtility.PingObject(asset);
        }

        // EN: Applies the new board size to the asset (with Undo).
        // VI: Áp dụng kích thước bảng mới vào asset (có Undo).
        private void ResizeBoard()
        {
            serialized.ApplyModifiedProperties();
            Undo.RecordObject(level, "Đổi kích thước bảng");
            level.ResizeBoard();
            EditorUtility.SetDirty(level);
        }

        // EN: Loads the LevelConfig whose name ends with the given number; offers to create it when missing.
        // VI: Nạp LevelConfig có tên kết thúc bằng số level đã nhập; nếu chưa có thì hỏi có tạo mới không.
        private void LoadLevelNumber(int number)
        {
            GUI.FocusControl(null);
            LevelConfig found = FindLevel(number);
            if (found != null)
            {
                SetLevel(found);
                EditorGUIUtility.PingObject(found);
                return;
            }
            if (EditorUtility.DisplayDialog("Chưa có level", $"Không tìm thấy level {number}. Tạo mới {LevelAssetName(number)}?", "Tạo mới", "Huỷ"))
                CreateLevelNumber(number);
        }

        // EN: Returns the LevelConfig whose name ends with the given number, or null.
        // VI: Trả về LevelConfig có tên kết thúc bằng số đã cho, hoặc null.
        private static LevelConfig FindLevel(int number)
        {
            foreach (LevelConfig candidate in GetNumberedLevels())
            {
                if (ParseLevelNumber(candidate.name) == number)
                    return candidate;
            }
            return null;
        }

        // EN: Confirms discarding edits before creating an empty asset (default folder Resources/Levels), taking levelNumber from the file name, and editing its isolated draft.
        // VI: Xác nhận bỏ chỉnh sửa trước khi tạo asset trống (thư mục mặc định Resources/Levels), lấy levelNumber theo số trong tên file, rồi chỉnh bản nháp độc lập của nó.
        private void CreateLevel()
        {
            if (!ConfirmDiscardDraft())
                return;
            string path = EditorUtility.SaveFilePanelInProject("Tạo LevelConfig", LevelAssetName(GetNumberedLevels().Count + 1), "asset", "Chọn nơi lưu level mới", NewLevelFolder);
            if (string.IsNullOrEmpty(path))
                return;
            LevelConfig asset = CreateInstance<LevelConfig>();
            asset.levelNumber = Mathf.Max(1, ParseLevelNumber(System.IO.Path.GetFileNameWithoutExtension(path)));
            asset.ResizeBoard();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            SetLevel(asset, true);
            Selection.activeObject = asset;
        }

        // EN: Rescans dish prefabs and re-links every ingredient group of every customer queue in the draft to the prefab with the same ingredient list (or clears it), then reports how many groups matched; the asset changes only after Save.
        // VI: Quét lại prefab món và nối lại mọi nhóm nguyên liệu của mọi hàng khách trong bản nháp với prefab có cùng danh sách nguyên liệu (hoặc xoá nếu không có), rồi báo số nhóm khớp; asset chỉ đổi sau khi bấm Lưu.
        private void UpdateFoodPrefabs()
        {
            dishPrefabs = null;
            int matched = 0;
            List<string> missing = new List<string>();
            foreach (string queuePath in new[] { "customerQueue", "leftQueue", "rightQueue" })
            {
                SerializedProperty queue = serialized.FindProperty(queuePath);
                if (queue == null || !queue.isArray)
                    continue;
                for (int c = 0; c < queue.arraySize; c++)
                {
                    SerializedProperty groups = queue.GetArrayElementAtIndex(c).FindPropertyRelative("ingredientOrders");
                    for (int g = 0; groups != null && g < groups.arraySize; g++)
                    {
                        SerializedProperty group = groups.GetArrayElementAtIndex(g);
                        LinkMatchingDish(group.FindPropertyRelative("ingredients"));
                        if (group.FindPropertyRelative("dish").objectReferenceValue != null)
                            matched++;
                        else
                            missing.Add($"{queuePath} · khách {c + 1} · nhóm {g + 1}");
                    }
                }
            }
            string message = $"Đã gán prefab món cho {matched} nhóm.";
            if (missing.Count > 0)
                message += $"\n{missing.Count} nhóm chưa có prefab khớp:\n" + string.Join("\n", missing.GetRange(0, Mathf.Min(15, missing.Count))) + (missing.Count > 15 ? "\n…" : string.Empty);
            message += "\n\nBấm Lưu để ghi vào asset.";
            EditorUtility.DisplayDialog("UpdateFoodPrefab", message, "OK");
        }

        // EN: Sets the group's dish to the first scanned prefab whose ingredient list equals the group's (any order), or clears it when none matches.
        // VI: Gán món của nhóm bằng prefab quét được đầu tiên có danh sách nguyên liệu bằng của nhóm (thứ tự bất kỳ), hoặc xóa khi không có prefab khớp.
        private void LinkMatchingDish(SerializedProperty values)
        {
            List<FoodType> ingredients = new List<FoodType>(values.arraySize);
            for (int i = 0; i < values.arraySize; i++) ingredients.Add((FoodType)values.GetArrayElementAtIndex(i).intValue);
            FinalFoodView match = null;
            foreach (FinalFoodView prefab in GetDishPrefabs())
                if (prefab.Matches(ingredients)) { match = prefab; break; }
            string groupPath = values.propertyPath.Substring(0, values.propertyPath.LastIndexOf('.'));
            serialized.FindProperty(groupPath + ".dish").objectReferenceValue = match;
        }

        // EN: Returns (and caches) every prefab under Assets/HotPot whose root has FinalFoodView, sorted by name; the cache resets when the project changes.
        // VI: Trả về (và cache) mọi prefab trong Assets/HotPot có FinalFoodView ở gốc, sắp theo tên; cache được xóa khi project thay đổi.
        private List<FinalFoodView> GetDishPrefabs()
        {
            if (dishPrefabs != null)
                return dishPrefabs;
            dishPrefabs = new List<FinalFoodView>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { DishPrefabFolder }))
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                FinalFoodView view = asset != null ? asset.GetComponent<FinalFoodView>() : null;
                if (view != null)
                    dishPrefabs.Add(view);
            }
            dishPrefabs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return dishPrefabs;
        }

        // EN: Uses 40 percent of the available panel area for customers, with independent scrolling for wider cards.
        // VI: Dành 40 phần trăm vùng panel cho khách hàng, có cuộn riêng để xem thẻ rộng hơn.
        private void DrawCustomerPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(Mathf.Max(0f, position.width - 12f) * 0.4f));
            customerScroll = EditorGUILayout.BeginScrollView(customerScroll);
            DrawQueues();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ---------------------------------------------------------------- Customers

        // EN: Edits the draft's authored difficulty and shared queue; changes reach the source asset only through Save.
        // VI: Chỉnh độ khó và hàng khách chung trên bản nháp; thay đổi chỉ vào asset nguồn khi bấm Lưu.
        private void DrawQueues()
        {
            EditorGUILayout.PropertyField(serialized.FindProperty("difficultyMode"), new GUIContent("Difficulty Mode"));
            DrawSectionTitle("Khách hàng ở quầy order");
            if (!level.useUnifiedCustomerQueue && level.customerQueue.Count == 0)
            {
                EditorGUILayout.HelpBox("Game đọc khách cũ xen kẽ trái/phải thành hàng chung.", MessageType.Info);
                if (!GUILayout.Button("Chuyển sang danh sách khách chung")) return;
                Undo.RecordObject(level, "Unify customer queue");
                level.customerQueue = level.GetCustomerQueue();
                level.useUnifiedCustomerQueue = true;
                EditorUtility.SetDirty(level);
                serialized.Update();
            }
            DrawQueue("customerQueue", "Hàng khách chung · hai chỗ phục vụ");
        }

        // ---------------------------------------------------------------- Helpers

        // EN: Draws a bold section title with spacing.
        // VI: Vẽ tiêu đề mục in đậm kèm khoảng cách.
        private static void DrawSectionTitle(string title)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(title, EditorStyles.largeLabel);
        }

        // EN: Draws customer cards in serving order with a 390-pixel minimum table width, 50 percent wider than the previous 260 pixels.
        // VI: Vẽ thẻ khách theo thứ tự phục vụ với bảng rộng tối thiểu 390 pixel, tăng 50 phần trăm so với 260 pixel trước đây.
        private void DrawQueue(string queuePath, string title)
        {
            SerializedProperty queue = serialized.FindProperty(queuePath);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(390f));
            EditorGUILayout.LabelField($"{title} · {queue.arraySize} khách", EditorStyles.boldLabel);
            for (int i = 0; i < queue.arraySize; i++)
                DrawCustomer(queue, queuePath, i);
            if (GUILayout.Button("+ Thêm khách"))
                pendingAction = () => AddCustomer(queuePath);
            EditorGUILayout.EndVertical();
        }

        // EN: Edits ingredient groups and customer type, exposing a positive serving time only for Boss and Shipper.
        // VI: Chỉnh nhóm nguyên liệu và loại khách, chỉ hiện thời gian phục vụ dương cho Boss và Shipper.
        private void DrawCustomer(SerializedProperty queue, string queuePath, int index)
        {
            SerializedProperty customer = queue.GetArrayElementAtIndex(index);
            SerializedProperty groups = customer.FindPropertyRelative("ingredientOrders");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Khách {index + 1} · {groups.arraySize} nhóm", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(24f)))
                pendingAction = () => DeleteElement(queuePath, index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(customer.FindPropertyRelative("customerType"), new GUIContent("Loại khách"));
            CustomerType type = (CustomerType)customer.FindPropertyRelative("customerType").intValue;
            if (type == CustomerType.Boss || type == CustomerType.Shipper)
            {
                SerializedProperty timeServe = customer.FindPropertyRelative("timeServe");
                EditorGUILayout.PropertyField(timeServe, new GUIContent("Thời gian phục vụ (s)"));
                if (float.IsNaN(timeServe.floatValue) || float.IsInfinity(timeServe.floatValue) || timeServe.floatValue <= 0f) timeServe.floatValue = 30f;
            }
            for (int i = 0; i < groups.arraySize; i++) DrawIngredientGroup(groups, i);
            using (new EditorGUI.DisabledScope(groups.arraySize >= CustomerOrder.MaxDishes))
                if (GUILayout.Button("+ Nhóm nguyên liệu", EditorStyles.miniButton))
                {
                    string path = groups.propertyPath;
                    pendingAction = () => AddDish(path);
                }
            EditorGUILayout.EndVertical();
        }

        // EN: Removes an array element (a customer or one of their dishes).
        // VI: Xoá một phần tử mảng (một khách hoặc một món của khách).
        private void DeleteElement(string arrayPath, int index)
        {
            serialized.FindProperty(arrayPath).DeleteArrayElementAtIndex(index);
        }

        // EN: Shows the group's dish prefab (preview + picker that copies the prefab's ingredients) and its ingredient icons, with a foldout ingredient picker.
        // VI: Hiện prefab món của nhóm (ảnh xem trước + bộ chọn chép nguyên liệu của prefab) và icon nguyên liệu, kèm bộ chọn nguyên liệu thu gọn.
        private void DrawIngredientGroup(SerializedProperty groups, int index)
        {
            SerializedProperty group = groups.GetArrayElementAtIndex(index);
            SerializedProperty values = group.FindPropertyRelative("ingredients");
            string path = values.propertyPath;
            var selectedFoods = new List<FoodType>();
            for (int i = 0; i < values.arraySize; i++) selectedFoods.Add((FoodType)values.GetArrayElementAtIndex(i).intValue);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, $"Nhóm {index + 1} · {values.arraySize} nguyên liệu", true);
            if (GUILayout.Button("x", GUILayout.Width(22f)))
            {
                string groupsPath = groups.propertyPath;
                pendingAction = () => DeleteElement(groupsPath, index);
            }
            EditorGUILayout.EndHorizontal();
            DrawGroupDish(group);
            EditorGUILayout.BeginHorizontal();
            foreach (FoodType type in selectedFoods) DrawIcon(type, RowIconSize);
            EditorGUILayout.EndHorizontal();
            if (group.isExpanded)
                for (int i = 1; i < FoodValues.Length; i++)
                {
                    FoodType type = FoodValues[i];
                    bool selected = selectedFoods.Contains(type);
                    using (new EditorGUI.DisabledScope(!selected && selectedFoods.Count >= IngredientOrder.MaxIngredients))
                    {
                        bool next = EditorGUILayout.ToggleLeft(FoodNames[i], selected);
                        if (next != selected) pendingAction = () => SetIngredientSelection(path, type, next);
                    }
                }
            EditorGUILayout.EndVertical();
        }

        // EN: Draws the group's dish: a prefab preview and a dropdown of every scanned dish prefab; picking one copies its ingredients into the group.
        // VI: Vẽ món của nhóm: ảnh xem trước prefab và danh sách chọn mọi prefab món quét được; chọn một prefab sẽ chép nguyên liệu của nó vào nhóm.
        private void DrawGroupDish(SerializedProperty group)
        {
            FinalFoodView dish = group.FindPropertyRelative("dish").objectReferenceValue as FinalFoodView;
            string groupPath = group.propertyPath;
            EditorGUILayout.BeginHorizontal();
            Rect preview = GUILayoutUtility.GetRect(DishPreviewSize, DishPreviewSize, GUILayout.Width(DishPreviewSize), GUILayout.Height(DishPreviewSize));
            Texture2D texture = dish != null ? AssetPreview.GetAssetPreview(dish.gameObject) : null;
            if (texture != null)
                GUI.DrawTexture(preview, texture, ScaleMode.ScaleToFit);
            else
                GUI.Box(preview, GUIContent.none);
            if (dish != null && texture == null && AssetPreview.IsLoadingAssetPreview(dish.GetInstanceID()))
                Repaint();
            EditorGUILayout.BeginVertical();
            if (EditorGUILayout.DropdownButton(new GUIContent(dish != null ? dish.name : "Chọn món từ prefab…"), FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                foreach (FinalFoodView prefab in GetDishPrefabs())
                {
                    FinalFoodView option = prefab;
                    menu.AddItem(new GUIContent($"{prefab.name}  ({string.Join(" + ", prefab.GetIngredients())})"), prefab == dish, () => { pendingAction = () => ApplyDishPrefab(groupPath, option); Repaint(); });
                }
                if (menu.GetItemCount() == 0)
                    menu.AddDisabledItem(new GUIContent($"Không có prefab FinalFoodView trong {DishPrefabFolder}"));
                menu.ShowAsContext();
            }
            EditorGUILayout.LabelField(dish != null ? "Prefab món khớp nguyên liệu" : "Chưa có prefab món khớp nguyên liệu (thẻ order không có hình)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // EN: Copies the prefab's ingredients (recipe order) into the group and links the prefab as its dish.
        // VI: Chép nguyên liệu của prefab (theo thứ tự công thức) vào nhóm và nối prefab làm món của nhóm.
        private void ApplyDishPrefab(string groupPath, FinalFoodView prefab)
        {
            SerializedProperty values = serialized.FindProperty(groupPath + ".ingredients");
            List<FoodType> ingredients = prefab.GetIngredients();
            values.arraySize = ingredients.Count;
            for (int i = 0; i < ingredients.Count; i++) values.GetArrayElementAtIndex(i).intValue = (int)ingredients[i];
            serialized.FindProperty(groupPath + ".dish").objectReferenceValue = prefab;
        }

        // EN: Reserves a square, draws the food icon or placeholder, and returns its area for state markers.
        // VI: Dành ô vuông, vẽ icon hoặc ô giữ chỗ và trả vùng hiển thị để đặt dấu trạng thái.
        private Rect DrawIcon(FoodType type, float size)
        {
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            Texture2D icon = GetIcon(type);
            if (icon != null)
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.25f));
            return rect;
        }

        // EN: Returns (and caches) the icon texture of a food: from FoodIconConfig first, otherwise from Art/Sprites/Foods by file name.
        // VI: Trả về (và cache) texture icon của nguyên liệu: ưu tiên lấy từ FoodIconConfig, nếu không có thì lấy theo tên file trong Art/Sprites/Foods.
        // EN: Uses the shared ingredient configuration instead of per-food catalogue references.
        // VI: Dùng cấu hình nguyên liệu chung thay cho tham chiếu bảng riêng trên từng food.
        private Texture2D GetIcon(FoodType type)
        {
            if (type == FoodType.None)
                return null;
            if (!icons.TryGetValue(type, out Texture2D icon))
            {
                FoodIconConfig config = IngameConfig.Instance.FoodIcons;
                Sprite sprite = config != null ? config.GetIcon(type) : null;
                icon = sprite != null ? sprite.texture : null;
                if (icon == null)
                {
                    string fileName = Regex.Replace(type.ToString(), "(?<!^)([A-Z])", "-$1").ToLowerInvariant();
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>(FoodSpriteFolder + fileName + ".png");
                }
                icons[type] = icon;
            }
            return icon;
        }

        // EN: Follows another selected source asset, asking before discarding an edited draft.
        // VI: Chuyển theo asset nguồn được chọn, hỏi trước khi bỏ bản nháp đang chỉnh.
        private void OnSelectionChange()
        {
            if (Selection.activeObject is LevelConfig selected && selected != sourceLevel)
            {
                SetLevel(selected);
                Repaint();
            }
        }

        // EN: Adds or removes one ingredient while preserving the order of all other selected entries, then re-links the group to the dish prefab with the same ingredient list.
        // VI: Thêm hoặc bỏ một nguyên liệu, giữ thứ tự các mục đã chọn còn lại, rồi nối lại nhóm với prefab món có cùng danh sách nguyên liệu.
        private void SetIngredientSelection(string path, FoodType type, bool selected)
        {
            SerializedProperty values = serialized.FindProperty(path);
            int existing = -1;
            for (int i = values.arraySize - 1; i >= 0 && existing < 0; i--)
                if (values.GetArrayElementAtIndex(i).intValue == (int)type)
                    existing = i;
            if (existing >= 0 && !selected)
                values.DeleteArrayElementAtIndex(existing);
            else if (existing < 0 && selected)
            {
                values.InsertArrayElementAtIndex(values.arraySize);
                values.GetArrayElementAtIndex(values.arraySize - 1).intValue = (int)type;
            }
            LinkMatchingDish(values);
        }

        // EN: Appends an empty ingredient group (no dish prefab) to the current customer's order.
        // VI: Thêm nhóm nguyên liệu trống (chưa có prefab món) vào order của khách.
        private void AddDish(string dishesPath)
        {
            SerializedProperty groups = serialized.FindProperty(dishesPath);
            groups.InsertArrayElementAtIndex(groups.arraySize);
            SerializedProperty group = groups.GetArrayElementAtIndex(groups.arraySize - 1);
            group.FindPropertyRelative("ingredients").ClearArray();
            group.FindPropertyRelative("dish").objectReferenceValue = null;
            group.isExpanded = true;
        }

        // EN: Adds a Normal customer with a default 30-second special-customer time and an empty ingredient group.
        // VI: Thêm khách Normal với thời gian khách đặc biệt mặc định 30 giây và nhóm nguyên liệu trống.
        private void AddCustomer(string queuePath)
        {
            SerializedProperty queue = serialized.FindProperty(queuePath);
            queue.InsertArrayElementAtIndex(queue.arraySize);
            SerializedProperty customer = queue.GetArrayElementAtIndex(queue.arraySize - 1);
            customer.FindPropertyRelative("customerType").intValue = (int)CustomerType.Normal;
            customer.FindPropertyRelative("timeServe").floatValue = 30f;
            SerializedProperty groups = customer.FindPropertyRelative("ingredientOrders");
            groups.ClearArray();
            AddDish(groups.propertyPath);
        }

        // EN: Thin vertical line separating the two columns.
        // VI: Đường kẻ dọc mỏng ngăn cách hai cột.
        private static void DrawPanelDivider()
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.35f));
            GUILayout.Space(6f);
        }

        // EN: Uses the remaining 60 percent for the raw ingredient board and validation, with independent scrolling.
        // VI: Dành 60 phần trăm còn lại cho bảng nguyên liệu thô và kiểm tra nguyên liệu, có cuộn riêng.
        private void DrawBoardPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            boardScroll = EditorGUILayout.BeginScrollView(boardScroll);
            DrawBoard();
            EditorGUILayout.Space(14f);
            DrawValidation();
            EditorGUILayout.Space(10f);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ---------------------------------------------------------------- Board

        // EN: Draws board controls, the ingredient grid, ordered link-chain editing and placement warnings.
        // VI: Vẽ điều khiển bảng, lưới nguyên liệu, chỉnh chuỗi nối có thứ tự và cảnh báo vị trí.
        private void DrawBoard()
        {
            DrawSectionTitle("Bảng nguyên liệu thô");
            DrawBoardSize();
            DrawBoardGrid();
            DrawDishLinks();
            DrawBoardProblems();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Điền bảng theo tổng order", GUILayout.Width(190f)))
                pendingAction = FillBoardFromOrders;
            if (GUILayout.Button("Xoá bảng", GUILayout.Width(90f)))
                pendingAction = ClearBoard;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // EN: Row/column fields; a change resizes every column so it keeps exactly boardRows foods.
        // VI: Ô số hàng/số cột; khi đổi sẽ chỉnh lại mọi cột để luôn có đúng boardRows nguyên liệu.
        private void DrawBoardSize()
        {
            SerializedProperty rows = serialized.FindProperty("boardRows");
            SerializedProperty columns = serialized.FindProperty("boardColumns");
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60f;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            int newRows = Mathf.Max(1, EditorGUILayout.IntField("Số hàng", rows.intValue, GUILayout.Width(120f)));
            int newColumns = Mathf.Max(1, EditorGUILayout.IntField("Số cột", columns.intValue, GUILayout.Width(120f)));
            if (EditorGUI.EndChangeCheck())
            {
                rows.intValue = newRows;
                columns.intValue = newColumns;
                pendingAction = ResizeBoard;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        // EN: Draws merged double cells on the fixed grid and overlays straight link-chain previews.
        // VI: Vẽ ô đĩa đôi gộp trên lưới cố định và phủ bản xem trước chuỗi nối thẳng.
        private void DrawBoardGrid()
        {
            SerializedProperty board = serialized.FindProperty("ingredientBoard");
            if (board.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Bảng đang trống, đổi số hàng hoặc số cột để tạo bảng.", MessageType.None);
                return;
            }
            int columns = board.arraySize;
            int rows = 0;
            for (int c = 0; c < columns; c++)
                rows = Mathf.Max(rows, board.GetArrayElementAtIndex(c).FindPropertyRelative("foods").arraySize);
            DrawBoardHeader(columns);
            Rect grid = GUILayoutUtility.GetRect(columns * (CellWidth + CellGap), rows * (CellHeight + CellGap), GUILayout.ExpandWidth(false));
            for (int c = 0; c < columns; c++)
            {
                SerializedProperty foods = board.GetArrayElementAtIndex(c).FindPropertyRelative("foods");
                for (int r = 0; r < foods.arraySize; r++)
                {
                    if (level.IsCoveredCell(c, r))
                        continue;
                    SerializedProperty cell = foods.GetArrayElementAtIndex(r);
                    DishShape shape = (DishShape)cell.FindPropertyRelative("dishShape").enumValueIndex;
                    if (!level.TryGetCoveredCell(c, r, out _, out _))
                        shape = DishShape.Single;
                    float width = shape == DishShape.Horizontal ? CellWidth * 2f + CellGap : CellWidth;
                    float height = shape == DishShape.Vertical ? CellHeight * 2f + CellGap : CellHeight;
                    Rect rect = new Rect(grid.x + c * (CellWidth + CellGap), grid.y + r * (CellHeight + CellGap), width, height);
                    DrawCellArea(rect, c, r, cell, shape);
                }
            }
            DrawDishLinkPreview(grid);
        }

        // EN: Draws the "Column N" labels above the grid, one per column at cell width plus gap.
        // VI: Vẽ nhãn "Cột N" phía trên lưới, mỗi cột một nhãn rộng bằng ô cộng khoảng cách.
        private static void DrawBoardHeader(int columns)
        {
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < columns; c++)
                GUILayout.Label($"Cột {c + 1}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(CellWidth + CellGap - 4f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // EN: Draws a shape-coloured dish frame; vertical doubles use matching portion cards and a separate shared-settings footer.
        // VI: Vẽ khung đĩa theo màu kiểu đĩa; đĩa đôi dọc dùng hai thẻ nguyên liệu đồng nhất và vùng tùy chọn chung riêng bên dưới.
        private void DrawCellArea(Rect rect, int column, int row, SerializedProperty cell, DishShape shape)
        {
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            if (shape != DishShape.Single && Event.current.type == EventType.Repaint)
            {
                Color tint = shape == DishShape.Vertical ? VerticalDoubleColor : HorizontalDoubleColor;
                EditorGUI.DrawRect(rect, new Color(tint.r, tint.g, tint.b, 0.16f));
            }
            if (shape != DishShape.Single && Event.current.type == EventType.Repaint)
                DrawOutline(rect, shape == DishShape.Vertical ? VerticalDoubleColor : HorizontalDoubleColor, 2f);
            Rect inner = new Rect(rect.x + 5f, rect.y + 4f, rect.width - 10f, rect.height - 8f);
            if (shape == DishShape.Vertical)
            {
                DrawVerticalDish(inner, column, row, cell);
                return;
            }
            Rect first = inner;
            Rect second = inner;
            if (shape == DishShape.Horizontal)
            {
                first.width = second.width = (inner.width - CellGap - 10f) * 0.5f;
                second.x = first.xMax + CellGap + 10f;
            }
            DrawMainPanel(first, column, row, cell, shape != DishShape.Single);
            if (shape != DishShape.Single)
                DrawSecondPanel(second, cell);
        }

        // EN: Draws a rectangle border of the given thickness.
        // VI: Vẽ đường viền hình chữ nhật với độ dày đã cho.
        private static void DrawOutline(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        // EN: Arranges the vertical dish as a shape header, two equally sized ingredient cards and shared lid/ice controls.
        // VI: Sắp xếp đĩa dọc thành ô chọn kiểu, hai thẻ nguyên liệu cùng kích thước và tùy chọn nắp/băng dùng chung.
        private void DrawVerticalDish(Rect area, int column, int row, SerializedProperty cell)
        {
            DrawShapePopup(new Rect(area.x, area.y + 2f, area.width, LineHeight), column, row, cell.FindPropertyRelative("dishShape"));
            const float headerHeight = 26f;
            const float footerHeight = 88f;
            const float gap = 8f;
            float cardHeight = (area.height - headerHeight - footerHeight - gap * 2f) * 0.5f;
            Rect first = new Rect(area.x, area.y + headerHeight, area.width, cardHeight);
            Rect second = new Rect(area.x, first.yMax + gap, area.width, cardHeight);
            DrawVerticalPortion(first, cell, false);
            DrawVerticalPortion(second, cell, true);
            DrawVerticalSharedSettings(new Rect(area.x, area.yMax - footerHeight, area.width, footerHeight), cell);
        }

        // EN: Draws the dish shape popup in the given line; a change is deferred to ChangeDishShape, which checks the placement and clears the newly covered cell.
        // VI: Vẽ ô chọn kiểu đĩa trên dòng đã cho; khi đổi sẽ hoãn sang ChangeDishShape để kiểm tra vị trí và xoá ô bị chiếm mới.
        private void DrawShapePopup(Rect line, int column, int row, SerializedProperty shape)
        {
            int picked = EditorGUI.Popup(line, shape.enumValueIndex, ShapeNames);
            if (picked != shape.enumValueIndex)
                pendingAction = () => ChangeDishShape(column, row, (DishShape)picked);
        }

        // EN: Changes a cell's dish shape (with Undo). A double dish needs a cell below (vertical) or to the right (horizontal) that no other double covers; that cell is cleared, after confirmation when it holds data.
        // VI: Đổi kiểu đĩa của một ô (có Undo). Đĩa đôi cần một ô bên dưới (dọc) hoặc bên phải (ngang) chưa bị đĩa đôi khác chiếm; ô đó sẽ bị xoá, có hỏi xác nhận nếu đang có dữ liệu.
        private void ChangeDishShape(int column, int row, DishShape shape)
        {
            serialized.ApplyModifiedProperties();
            OrderFood cell = level.GetCell(column, row);
            if (cell == null)
                return;
            OrderFood covered = null;
            if (shape != DishShape.Single && !TryGetFreeCellFor(column, row, shape, out covered))
                return;
            Undo.RecordObject(level, "Đổi kiểu đĩa");
            cell.dishShape = shape;
            covered?.Clear();
            EditorUtility.SetDirty(level);
        }

        // EN: Finds the cell a new double dish at (column, row) would cover and checks it: inside the board, not covered by another double, and (after confirmation) free to be cleared. Shows a dialog and returns false otherwise.
        // VI: Tìm ô mà đĩa đôi mới tại (column, row) sẽ chiếm và kiểm tra: nằm trong bảng, chưa bị đĩa đôi khác chiếm, và (sau khi xác nhận) được phép xoá. Nếu không thì hiện hộp thoại và trả về false.
        private bool TryGetFreeCellFor(int column, int row, DishShape shape, out OrderFood covered)
        {
            int coveredColumn = shape == DishShape.Horizontal ? column + 1 : column;
            int coveredRow = shape == DishShape.Vertical ? row + 1 : row;
            covered = level.GetCell(coveredColumn, coveredRow);
            if (covered == null)
            {
                EditorUtility.DisplayDialog("Không đặt được đĩa đôi", shape == DishShape.Vertical ? "Đĩa đôi dọc cần một ô bên dưới; ô này đang ở hàng cuối." : "Đĩa đôi ngang cần một ô bên phải; ô này đang ở cột cuối.", "OK");
                return false;
            }
            if (level.TryGetCoveringDish(coveredColumn, coveredRow, out int anchorColumn, out int anchorRow) && (anchorColumn != column || anchorRow != row))
            {
                EditorUtility.DisplayDialog("Không đặt được đĩa đôi", $"Ô cột {coveredColumn + 1} hàng {coveredRow + 1} đã thuộc một đĩa đôi khác.", "OK");
                return false;
            }
            bool hasData = covered.foodType != FoodType.None || covered.IsDouble;
            return !hasData || EditorUtility.DisplayDialog("Ô bị chiếm có dữ liệu",
                $"Ô cột {coveredColumn + 1} hàng {coveredRow + 1} sẽ bị xoá để nhường chỗ cho đĩa đôi. Tiếp tục?", "Xoá và đặt", "Huỷ");
        }

        // EN: Draws an identical compact ingredient card for either half with an icon, explicit quantity label and full-width food selector.
        // VI: Vẽ thẻ nguyên liệu gọn giống nhau cho mỗi nửa, gồm icon, nhãn số lượng rõ ràng và ô chọn nguyên liệu rộng toàn thẻ.
        private void DrawVerticalPortion(Rect area, SerializedProperty cell, bool second)
        {
            GUI.Box(area, GUIContent.none, EditorStyles.helpBox);
            Rect content = new Rect(area.x + 6f, area.y + 7f, area.width - 12f, area.height - 14f);
            SerializedProperty type = cell.FindPropertyRelative(second ? "secondFoodType" : "foodType");
            SerializedProperty count = cell.FindPropertyRelative(second ? "secondCount" : "requiredCount");
            bool frozen = cell.FindPropertyRelative("isFrozen").boolValue;
            DrawPartIcon(new Rect(content.x, content.y, BoardIconSize, BoardIconSize), (FoodType)type.intValue,
                frozen ? cell.FindPropertyRelative("blockValue").intValue : 0, cell.FindPropertyRelative("isHidden").boolValue);
            float textX = content.x + BoardIconSize + 8f;
            float textWidth = content.xMax - textX;
            GUI.Label(new Rect(textX, content.y + 2f, textWidth, LineHeight), second ? "PHẦN 2" : "PHẦN 1", EditorStyles.miniBoldLabel);
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 54f;
            count.intValue = Mathf.Max(1, EditorGUI.IntField(new Rect(textX, content.y + 24f, textWidth, LineHeight), "Số lượng", count.intValue));
            EditorGUIUtility.labelWidth = oldLabelWidth;
            type.enumValueIndex = EditorGUI.Popup(new Rect(content.x, content.yMax - LineHeight, content.width, LineHeight), type.enumValueIndex, FoodNames);
        }

        // EN: Draws an ingredient icon centred at the top of the panel with the frozen (blockValue > 0) and hidden marks; returns the y just below it.
        // VI: Vẽ icon nguyên liệu căn giữa ở đầu khung, phủ dấu đóng băng (blockValue > 0) và dấu ẩn; trả về toạ độ y ngay bên dưới icon.
        private float DrawPartIcon(Rect area, FoodType type, int blockValue, bool hidden)
        {
            Rect iconRect = new Rect(area.center.x - BoardIconSize * 0.5f, area.y, BoardIconSize, BoardIconSize);
            Texture2D icon = GetIcon(type);
            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(iconRect, new Color(0.5f, 0.5f, 0.5f, 0.25f));
            if (blockValue > 0)
                DrawFrozenMark(iconRect, blockValue);
            if (hidden)
                DrawHiddenMark(iconRect);
            return iconRect.yMax + 4f;
        }

        // EN: Marks a frozen ingredient: an icy blue tint over the icon with the block value in the corner.
        // VI: Đánh dấu nguyên liệu đóng băng: phủ màu xanh băng lên icon và ghi giá trị khối băng ở góc.
        private static void DrawFrozenMark(Rect rect, int blockValue)
        {
            EditorGUI.DrawRect(rect, new Color(0.55f, 0.85f, 1f, 0.45f));
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.LowerRight,
                fontSize = Mathf.RoundToInt(rect.height * 0.35f)
            };
            style.normal.textColor = Color.white;
            GUI.Label(rect, blockValue.ToString(), style);
        }

        // EN: Marks a hidden ingredient: dims its icon and draws a large "?" on top, like the lid shown in game.
        // VI: Đánh dấu nguyên liệu bị ẩn: làm tối icon và vẽ dấu "?" lớn đè lên, giống nắp che trong game.
        private static void DrawHiddenMark(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.55f));
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(rect.height * 0.6f)
            };
            style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(rect, "?", style);
        }

        // EN: Groups the settings affecting both halves below the ingredient cards and reserves space for the ice value.
        // VI: Gom các tùy chọn tác động cả hai nửa xuống dưới các thẻ nguyên liệu và dành chỗ cho giá trị băng.
        private static void DrawVerticalSharedSettings(Rect area, SerializedProperty cell)
        {
            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(new Rect(area.x, area.y, area.width, 1f), new Color(1f, 1f, 1f, 0.15f));
            float y = area.y + 6f;
            GUI.Label(NextLine(area, ref y), "ÁP DỤNG CẢ ĐĨA", EditorStyles.miniBoldLabel);
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = area.width - 22f;
            EditorGUI.PropertyField(NextLine(area, ref y), cell.FindPropertyRelative("isHidden"), new GUIContent("Ẩn cả đĩa"));
            SerializedProperty frozen = cell.FindPropertyRelative("isFrozen");
            EditorGUI.PropertyField(NextLine(area, ref y), frozen, new GUIContent("Đóng băng"));
            if (frozen.boolValue)
            {
                EditorGUIUtility.labelWidth = 80f;
                SerializedProperty block = cell.FindPropertyRelative("blockValue");
                block.intValue = Mathf.Max(1, EditorGUI.IntField(NextLine(area, ref y), "Giá trị băng", block.intValue));
            }
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        // EN: Returns the next control line inside a panel and advances the vertical cursor.
        // VI: Trả về dòng điều khiển kế tiếp trong khung và đẩy con trỏ dọc xuống.
        private static Rect NextLine(Rect area, ref float y)
        {
            Rect line = new Rect(area.x, y, area.width, LineHeight);
            y += LineStep;
            return line;
        }

        // EN: First panel of a dish, drawn with explicit rects: icon with hidden/frozen marks, shape popup, part 1 ingredient and quantity, lid and ice settings.
        // VI: Khung đầu của đĩa, vẽ bằng vùng cố định: icon kèm dấu ẩn/đóng băng, ô chọn kiểu đĩa, nguyên liệu và số lượng phần 1, tùy chọn nắp và băng.
        private void DrawMainPanel(Rect area, int column, int row, SerializedProperty cell, bool isDouble)
        {
            SerializedProperty frozen = cell.FindPropertyRelative("isFrozen");
            SerializedProperty block = cell.FindPropertyRelative("blockValue");
            SerializedProperty hidden = cell.FindPropertyRelative("isHidden");
            float y = DrawPartIcon(area, (FoodType)cell.FindPropertyRelative("foodType").intValue, frozen.boolValue ? block.intValue : 0, hidden.boolValue);
            DrawShapePopup(NextLine(area, ref y), column, row, cell.FindPropertyRelative("dishShape"));
            DrawPortionFields(area, ref y, cell.FindPropertyRelative("foodType"), cell.FindPropertyRelative("requiredCount"));
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Max(60f, area.width - 30f);
            EditorGUI.PropertyField(NextLine(area, ref y), hidden, new GUIContent(isDouble ? "Ẩn cả đĩa" : "Ẩn nguyên liệu"));
            EditorGUI.PropertyField(NextLine(area, ref y), frozen, new GUIContent("Đóng băng"));
            EditorGUIUtility.labelWidth = Mathf.Max(60f, area.width * 0.55f);
            if (frozen.boolValue)
                block.intValue = Mathf.Max(1, EditorGUI.IntField(NextLine(area, ref y), new GUIContent("Giá trị băng"), block.intValue));
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        // EN: Draws one portion's ingredient popup and quantity field on the next two lines of the panel.
        // VI: Vẽ ô chọn nguyên liệu và ô số lượng của một phần trên hai dòng kế tiếp của khung.
        private static void DrawPortionFields(Rect area, ref float y, SerializedProperty type, SerializedProperty count)
        {
            type.enumValueIndex = EditorGUI.Popup(NextLine(area, ref y), type.enumValueIndex, FoodNames);
            count.intValue = Mathf.Max(1, EditorGUI.IntField(NextLine(area, ref y), count.intValue));
        }

        // EN: Second panel of a double dish, drawn with explicit rects: part 2 icon (with the shared hidden/frozen marks), a caption in the shape popup's line, part 2 ingredient and quantity.
        // VI: Khung sau của đĩa đôi, vẽ bằng vùng cố định: icon phần 2 (kèm dấu ẩn/đóng băng dùng chung), nhãn ở dòng ô chọn kiểu đĩa, nguyên liệu và số lượng phần 2.
        private void DrawSecondPanel(Rect area, SerializedProperty cell)
        {
            bool frozen = cell.FindPropertyRelative("isFrozen").boolValue;
            float y = DrawPartIcon(area, (FoodType)cell.FindPropertyRelative("secondFoodType").intValue, frozen ? cell.FindPropertyRelative("blockValue").intValue : 0, cell.FindPropertyRelative("isHidden").boolValue);
            GUI.Label(NextLine(area, ref y), "Phần 2", EditorStyles.miniBoldLabel);
            DrawPortionFields(area, ref y, cell.FindPropertyRelative("secondFoodType"), cell.FindPropertyRelative("secondCount"));
        }

        // EN: Lists double-footprint and link-chain errors before a draft is saved or played.
        // VI: Liệt kê lỗi vùng chiếm đĩa đôi và chuỗi nối trước khi lưu hoặc chơi bản nháp.
        private void DrawBoardProblems()
        {
            boardProblems.Clear();
            level.CollectBoardProblems(boardProblems);
            level.CollectDishLinkProblems(boardProblems);
            foreach (string problem in boardProblems)
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            if (HasDoubleDish())
                EditorGUILayout.HelpBox("Đĩa đôi lấy cả hai phần trong một lần chạm và cần đủ chỗ trên băng chuyền. Đĩa ngang chỉ dồn khi cả hai ô phía trên trống, chỉ chọn thường khi đứng đầu cả hai cột. Shuffle giữ đĩa đôi đứng yên.", MessageType.Info);
        }

        // EN: True when any board cell (not covered by another double) is a double dish.
        // VI: True khi có ô nào trên bảng (không bị đĩa đôi khác chiếm) là đĩa đôi.
        private bool HasDoubleDish()
        {
            for (int c = 0; c < level.ingredientBoard.Count; c++)
            {
                List<OrderFood> foods = level.ingredientBoard[c]?.foods;
                if (foods == null)
                    continue;
                for (int r = 0; r < foods.Count; r++)
                {
                    if (foods[r] != null && foods[r].IsDouble && !level.IsCoveredCell(c, r))
                        return true;
                }
            }
            return false;
        }

        // EN: Refills singles and both halves of doubles from order totals on the isolated draft, preserving double layouts and shared states.
        // VI: Điền lại đĩa đơn và cả hai nửa đĩa đôi theo tổng order trên bản nháp độc lập, giữ bố cục và trạng thái chung của đĩa đôi.
        private void FillBoardFromOrders()
        {
            serialized.ApplyModifiedProperties();
            bool filled = LevelBoardOrderFiller.TryFill(level, CountOrderedFoods(), out string message);
            if (!string.IsNullOrEmpty(message))
                EditorUtility.DisplayDialog(filled ? "Đã điền bảng" : "Không điền được bảng", message, "OK");
        }

        // EN: Sums the ingredient requirements of every customer in the shared queue.
        // VI: Cộng nguyên liệu order của mọi khách trong hàng chung.
        private int[] CountOrderedFoods()
        {
            int[] totals = new int[FoodNames.Length];
            AddOrderTotals(level.GetCustomerQueue(), totals);
            return totals;
        }

        // EN: Counts every ingredient of every order group, retaining quantities.
        // VI: Đếm mọi nguyên liệu của mọi nhóm order, giữ nguyên số lượng.
        private static void AddOrderTotals(List<CustomerOrder> queue, int[] totals)
        {
            if (queue == null) return;
            foreach (CustomerOrder order in queue)
            {
                if (order?.ingredientOrders == null) continue;
                foreach (IngredientOrder group in order.ingredientOrders)
                    foreach (FoodType ingredient in group.ingredients)
                    {
                        int type = Array.IndexOf(FoodValues, ingredient);
                        if (type > 0 && type < totals.Length) totals[type]++;
                    }
            }
        }

        // EN: Clears board dishes and link chains together, with Undo on the isolated draft.
        // VI: Xóa đĩa trên bảng và chuỗi nối cùng lúc, có Undo trên bản nháp độc lập.
        private void ClearBoard()
        {
            serialized.ApplyModifiedProperties();
            Undo.RecordObject(level, "Xoá bảng nguyên liệu");
            level.dishLinks?.Clear();
            foreach (BoardColumn column in level.ingredientBoard)
            {
                foreach (OrderFood cell in column.foods)
                    cell.Clear();
            }
            EditorUtility.SetDirty(level);
        }

        // ---------------------------------------------------------------- Validation

        // EN: Table comparing, per food, the total required by all orders with the total raw count on the board.
        // VI: Bảng so sánh, theo từng loại, tổng số lượng các order cần với tổng nguyên liệu thô trên bảng.
        private void DrawValidation()
        {
            DrawSectionTitle("Kiểm tra nguyên liệu");
            int[] needed = CountOrderedFoods();
            int[] onBoard = CountBoardFoods();
            EditorGUILayout.LabelField($"Tổng khách: {level.TotalCustomers}  ·  Bảng: {level.boardColumns} cột x {level.boardRows} hàng", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Nguyên liệu", GUILayout.Width(226f));
            GUILayout.Label("Order cần", GUILayout.Width(80f));
            GUILayout.Label("Bảng có", GUILayout.Width(80f));
            GUILayout.Label("Trạng thái");
            EditorGUILayout.EndHorizontal();

            bool any = false;
            for (int type = 1; type < FoodNames.Length; type++)
            {
                if (needed[type] == 0 && onBoard[type] == 0)
                    continue;
                any = true;
                DrawValidationRow(type, needed[type], onBoard[type]);
            }
            if (!any)
                EditorGUILayout.LabelField("Chưa có order hay nguyên liệu nào.", EditorStyles.miniLabel);
        }

        // EN: Sums board foods by enum index while resolving their stable numeric IDs.
        // VI: Cộng nguyên liệu bảng theo chỉ số enum và tra đúng ID số cố định.
        private int[] CountBoardFoods()
        {
            int[] totals = new int[FoodNames.Length];
            for (int type = 1; type < totals.Length; type++)
                totals[type] = level.GetBoardCount(FoodValues[type]);
            return totals;
        }

        // EN: Draws validation by enum index, resolving the actual ingredient ID for its icon.
        // VI: Vẽ kiểm tra theo chỉ số enum, dùng ID nguyên liệu thực để hiển thị icon.
        private void DrawValidationRow(int type, int needed, int onBoard)
        {
            EditorGUILayout.BeginHorizontal();
            DrawIcon(FoodValues[type], 20f);
            GUILayout.Label(FoodNames[type], GUILayout.Width(200f));
            GUILayout.Label(needed.ToString(), GUILayout.Width(80f));
            GUILayout.Label(onBoard.ToString(), GUILayout.Width(80f));
            if (onBoard == needed)
                GUILayout.Label("Đủ", okStyle);
            else if (onBoard < needed)
                GUILayout.Label($"Thiếu {needed - onBoard}", missingStyle);
            else
                GUILayout.Label($"Dư {onBoard - needed}", extraStyle);
            EditorGUILayout.EndHorizontal();
        }

        // EN: Executes the deferred structural edit after the layout pass so GUI groups stay balanced.
        // VI: Thực hiện thao tác cấu trúc đã hoãn sau khi vẽ xong để các nhóm layout luôn cân bằng.
        private void RunPendingAction()
        {
            if (pendingAction == null)
                return;
            Action action = pendingAction;
            pendingAction = null;
            serialized.Update();
            action();
            serialized.ApplyModifiedProperties();
            serialized.Update();
            Repaint();
        }

        // EN: Drops the dish-prefab cache so prefabs added, removed or edited in the project show up in the picker.
        // VI: Xóa cache prefab món để prefab vừa thêm, xóa hoặc sửa trong project hiện trong bộ chọn.
        private void OnProjectChange()
        {
            dishPrefabs = null;
        }

        // EN: Clears one cell back to an empty single dish (with Undo).
        // VI: Đặt lại một ô thành đĩa đơn trống (có Undo).
        private void ClearCell(int column, int row)
        {
            serialized.ApplyModifiedProperties();
            OrderFood cell = level.GetCell(column, row);
            if (cell == null)
                return;
            Undo.RecordObject(level, "Xoá dữ liệu ô");
            cell.Clear();
            EditorUtility.SetDirty(level);
        }
    }
}
