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
        private const string NewLevelFolder = "Assets/HotPot/Config";
        private const string FoodIconConfigPath = "Assets/HotPot/Config/FoodIconConfig.asset";
        private const string FinalFoodConfigPath = "Assets/HotPot/Config/FinalFoodConfig.asset";
        private const string FinalSpriteFolder = "Assets/HotPot/Art/Sprites/Foods/Sushi/Final/";
        private const float CellWidth = 180f;
        private const float CellHeight = 180f;
        private const float CellGap = 4f;
        private const float BoardIconSize = 44f;
        private const float RowIconSize = 26f;
        private const float FinalIconSize = 48f;
        private const HideFlags DraftHideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        private static readonly string[] ShapeNames = { "Đĩa đơn", "Đĩa đôi dọc", "Đĩa đôi ngang" };
        private static readonly Color VerticalDoubleColor = new Color(0.85f, 0.35f, 0.19f, 0.95f);
        private static readonly Color HorizontalDoubleColor = new Color(0.11f, 0.62f, 0.46f, 0.95f);

        private static readonly FoodType[] FoodValues = (FoodType[])Enum.GetValues(typeof(FoodType));
        private static readonly FinalFoodType[] FinalFoodValues = (FinalFoodType[])Enum.GetValues(typeof(FinalFoodType));

        // Vietnamese names follow enum declaration order, including gaps in the numeric IDs.
        private static readonly string[] VietnameseNames =
        {
            "Không có", "Cơm sushi", "Cá hồi", "Cá ngừ", "Tôm", "Thanh cua", "Trứng cuộn",
            "Dưa leo", "Trứng cá hồi", "Lươn", "Bạch tuộc", "Rong biển", "Sò điệp", "Nước tương", "Gừng ngâm", "Wasabi"
        };

        // Display names such as "Salmon (Cá hồi)": original enum name first, Vietnamese in brackets.
        private static readonly string[] FoodNames = BuildFoodNames();

        [SerializeField] private LevelConfig level;
        [SerializeField] private LevelConfig sourceLevel;
        [SerializeField] private string savedDraftJson;
        private SerializedObject serialized;
        private Vector2 customerScroll;
        private Vector2 boardScroll;
        private Action pendingAction;
        private int levelNumberInput = 1;
        private readonly Dictionary<FoodType, Texture2D> icons = new Dictionary<FoodType, Texture2D>();
        private readonly Dictionary<FinalFoodType, Texture2D> finalIcons = new Dictionary<FinalFoodType, Texture2D>();
        private FinalFoodConfig finalFoodConfig;
        private string[] finalFoodNames;
        private GUIStyle okStyle, missingStyle, extraStyle;
        private readonly List<string> boardProblems = new List<string>();

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

        // EN: Restores an editable isolated draft after reload, migrates older locked drafts and subscribes to Undo/Redo.
        // VI: Khôi phục bản nháp độc lập có thể chỉnh sau khi tải lại, sửa bản nháp cũ bị khóa và theo dõi Undo/Redo.
        private void OnEnable()
        {
            icons.Clear();
            finalIcons.Clear();
            finalFoodConfig = null;
            finalFoodNames = null;
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

        // EN: Refreshes serialized draft fields and repaint after Undo/Redo, including when the draft remains dirty.
        // VI: Làm mới field tuần tự hóa và vẽ lại sau Undo/Redo, kể cả khi bản nháp vẫn có thay đổi chưa lưu.
        private void OnDraftUndoRedo()
        {
            serialized?.Update();
            RefreshDraftStatus();
            Repaint();
        }

        // EN: Requires an explicit discard choice before reloading or switching away from an edited draft.
        // VI: Yêu cầu chọn bỏ thay đổi rõ ràng trước khi tải lại hoặc chuyển khỏi bản nháp đang chỉnh.
        private bool ConfirmDiscardDraft()
        {
            RefreshDraftStatus();
            return !hasUnsavedChanges || EditorUtility.DisplayDialog("Thay đổi chưa lưu",
                "Bỏ các thay đổi chưa lưu và nạp dữ liệu level? Asset chỉ được cập nhật khi bấm Lưu.", "Bỏ thay đổi", "Tiếp tục chỉnh");
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

        // EN: Handles the window's explicit Save choice when closing with unsaved edits.
        // VI: Xử lý lựa chọn Lưu rõ ràng khi đóng cửa sổ có chỉnh sửa chưa lưu.
        public override void SaveChanges()
        {
            SaveDraft();
            base.SaveChanges();
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

        // EN: Left column: the customer orders of both queues, in its own scroll view.
        // VI: Cột bên trái: order của khách ở cả hai hàng, có thanh cuộn riêng.
        private void DrawCustomerPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f));
            customerScroll = EditorGUILayout.BeginScrollView(customerScroll);
            DrawQueues();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // EN: Right column: the raw ingredient board and the ingredient check, in its own scroll view.
        // VI: Cột bên phải: bảng nguyên liệu thô và phần kiểm tra nguyên liệu, có thanh cuộn riêng.
        private void DrawBoardPanel()
        {
            EditorGUILayout.BeginVertical();
            boardScroll = EditorGUILayout.BeginScrollView(boardScroll);
            DrawBoard();
            EditorGUILayout.Space(14f);
            DrawValidation();
            EditorGUILayout.Space(10f);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // EN: Thin vertical line separating the two columns.
        // VI: Đường kẻ dọc mỏng ngăn cách hai cột.
        private static void DrawPanelDivider()
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.35f));
            GUILayout.Space(6f);
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

        // EN: Reads the trailing number of an asset name (LevelConfig_07 -> 7); returns -1 when there is none.
        // VI: Đọc số ở cuối tên asset (LevelConfig_07 -> 7); trả về -1 nếu không có.
        private static int ParseLevelNumber(string assetName)
        {
            Match match = Regex.Match(assetName ?? string.Empty, @"(\d+)$");
            return match.Success && int.TryParse(match.Groups[1].Value, out int number) ? number : -1;
        }

        // EN: Standard asset name for a level number (two digits minimum), e.g. LevelConfig_03.
        // VI: Tên asset chuẩn theo số level (tối thiểu hai chữ số), ví dụ LevelConfig_03.
        private static string LevelAssetName(int number)
        {
            return $"LevelConfig_{number:00}";
        }

        // EN: Confirms discarding edits, creates only the new numbered asset and opens its draft.
        // VI: Xác nhận bỏ chỉnh sửa, chỉ tạo asset mới theo số và mở bản nháp của nó.
        private void CreateLevelNumber(int number)
        {
            if (!ConfirmDiscardDraft())
                return;
            string path = AssetDatabase.GenerateUniqueAssetPath($"{NewLevelFolder}/{LevelAssetName(number)}.asset");
            LevelConfig asset = CreateInstance<LevelConfig>();
            asset.ResizeBoard();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            SetLevel(asset, true);
            EditorGUIUtility.PingObject(asset);
        }

        // EN: Confirms discarding edits before creating an empty asset and editing its isolated draft.
        // VI: Xác nhận bỏ chỉnh sửa trước khi tạo asset trống và chỉnh bản nháp độc lập của nó.
        private void CreateLevel()
        {
            if (!ConfirmDiscardDraft())
                return;
            string path = EditorUtility.SaveFilePanelInProject("Tạo LevelConfig", LevelAssetName(GetNumberedLevels().Count + 1), "asset", "Chọn nơi lưu level mới", NewLevelFolder);
            if (string.IsNullOrEmpty(path))
                return;
            LevelConfig asset = CreateInstance<LevelConfig>();
            asset.ResizeBoard();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssetIfDirty(asset);
            SetLevel(asset, true);
            Selection.activeObject = asset;
        }

        // ---------------------------------------------------------------- Customers

        // EN: Draws the left and right customer queues side by side, like the order counter.
        // VI: Vẽ hai hàng khách trái và phải cạnh nhau, giống quầy order.
        private void DrawQueues()
        {
            DrawSectionTitle("Khách hàng ở quầy order");
            EditorGUILayout.BeginHorizontal();
            DrawQueue("leftQueue", "Hàng trái");
            GUILayout.Space(8f);
            DrawQueue("rightQueue", "Hàng phải");
            EditorGUILayout.EndHorizontal();
        }

        // EN: Draws one queue: its customers (each ordering one final dish) in serving order and a button to add a customer.
        // VI: Vẽ một hàng khách: các khách (mỗi khách order một món hoàn chỉnh) theo thứ tự phục vụ và nút thêm khách.
        private void DrawQueue(string queuePath, string title)
        {
            SerializedProperty queue = serialized.FindProperty(queuePath);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(260f));
            EditorGUILayout.LabelField($"{title} · {queue.arraySize} khách", EditorStyles.boldLabel);
            for (int i = 0; i < queue.arraySize; i++)
                DrawCustomer(queue, queuePath, i);
            if (GUILayout.Button("+ Thêm khách"))
                pendingAction = () => AddCustomer(queuePath);
            EditorGUILayout.EndVertical();
        }

        // EN: Draws one customer card: remove button, every ordered final dish (all shown on the in-game card at once), a warning above MaxDishes and a button to add a dish.
        // VI: Vẽ thẻ của một khách: nút xoá, mọi món hoàn chỉnh khách order (hiện cùng lúc trên thẻ trong game), cảnh báo khi vượt MaxDishes và nút thêm món.
        private void DrawCustomer(SerializedProperty queue, string queuePath, int index)
        {
            SerializedProperty dishes = queue.GetArrayElementAtIndex(index).FindPropertyRelative("finalFoods");
            string dishesPath = dishes.propertyPath;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string label = index == 0 ? $"Khách {index + 1} (phục vụ trước)" : $"Khách {index + 1}";
            EditorGUILayout.LabelField($"{label} · {dishes.arraySize} món", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(24f)))
                pendingAction = () => DeleteElement(queuePath, index);
            EditorGUILayout.EndHorizontal();

            for (int d = 0; d < dishes.arraySize; d++)
                DrawDishRow(dishes.GetArrayElementAtIndex(d), dishesPath, d);
            if (dishes.arraySize == 0)
                EditorGUILayout.LabelField("Khách chưa order món nào.", EditorStyles.miniLabel);
            if (dishes.arraySize > CustomerOrder.MaxDishes)
                EditorGUILayout.HelpBox($"Thẻ order chỉ hiện tối đa {CustomerOrder.MaxDishes} món; các món sau sẽ bị bỏ.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(dishes.arraySize >= CustomerOrder.MaxDishes))
            {
                if (GUILayout.Button("+ Món", EditorStyles.miniButton))
                    pendingAction = () => AddDish(dishesPath);
            }
            EditorGUILayout.EndVertical();
        }

        // EN: Draws one ordered dish: its picture (click to pick from a picture grid), the name picker, its ingredient icons and a remove button.
        // VI: Vẽ một món được order: ảnh món (bấm để chọn món từ bảng hình), ô chọn theo tên, icon nguyên liệu và nút xoá.
        private void DrawDishRow(SerializedProperty finalFood, string dishesPath, int index)
        {
            EditorGUILayout.BeginHorizontal();
            Rect iconRect = DrawFinalIcon((FinalFoodType)finalFood.intValue, FinalIconSize);
            if (GUI.Button(iconRect, new GUIContent(string.Empty, "Bấm để chọn món theo hình"), GUIStyle.none))
                OpenDishPicker(iconRect, finalFood.propertyPath, (FinalFoodType)finalFood.intValue);
            EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Link);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            DrawDishDropdown(finalFood.propertyPath, (FinalFoodType)finalFood.intValue);
            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(22f)))
                pendingAction = () => DeleteElement(dishesPath, index);
            EditorGUILayout.EndHorizontal();
            DrawRecipeIngredients((FinalFoodType)finalFood.intValue);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        // EN: Draws the ingredient icons of a final dish in one row, or a hint when the recipe is missing.
        // VI: Vẽ icon các nguyên liệu của món hoàn chỉnh trên một hàng, hoặc ghi chú khi thiếu công thức.
        private void DrawRecipeIngredients(FinalFoodType finalFood)
        {
            FinalFoodConfig config = GetFinalFoodConfig();
            IReadOnlyList<FoodType> ingredients = config != null ? config.GetIngredients(finalFood) : null;
            if (ingredients == null || ingredients.Count == 0)
            {
                EditorGUILayout.LabelField(config == null ? "Chưa có FinalFoodConfig." : "Chưa chọn món hoặc món chưa có công thức.", EditorStyles.miniLabel);
                return;
            }
            EditorGUILayout.BeginHorizontal();
            foreach (FoodType ingredient in ingredients)
                DrawIcon(ingredient, RowIconSize);
            GUILayout.Label($"{ingredients.Count} món", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // EN: Appends a customer ordering one dish (the first final dish of the table).
        // VI: Thêm một khách mới order một món (món hoàn chỉnh đầu tiên trong bảng).
        private void AddCustomer(string queuePath)
        {
            SerializedProperty queue = serialized.FindProperty(queuePath);
            queue.InsertArrayElementAtIndex(queue.arraySize);
            SerializedProperty dishes = queue.GetArrayElementAtIndex(queue.arraySize - 1).FindPropertyRelative("finalFoods");
            dishes.ClearArray();
            AddDish(dishes.propertyPath);
        }

        // EN: Appends a dish to a customer's order, defaulting to the first final dish of the table.
        // VI: Thêm một món vào order của khách, mặc định là món hoàn chỉnh đầu tiên trong bảng.
        private void AddDish(string dishesPath)
        {
            SerializedProperty dishes = serialized.FindProperty(dishesPath);
            dishes.InsertArrayElementAtIndex(dishes.arraySize);
            dishes.GetArrayElementAtIndex(dishes.arraySize - 1).intValue = (int)FinalFoodValues[Mathf.Min(1, FinalFoodValues.Length - 1)];
        }

        // EN: Removes an array element (a customer or one of their dishes).
        // VI: Xoá một phần tử mảng (một khách hoặc một món của khách).
        private void DeleteElement(string arrayPath, int index)
        {
            serialized.FindProperty(arrayPath).DeleteArrayElementAtIndex(index);
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

        private const float LineHeight = 18f;
        private const float LineStep = 20f;

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

        // EN: Second panel of a double dish, drawn with explicit rects: part 2 icon (with the shared hidden/frozen marks), a caption in the shape popup's line, part 2 ingredient and quantity.
        // VI: Khung sau của đĩa đôi, vẽ bằng vùng cố định: icon phần 2 (kèm dấu ẩn/đóng băng dùng chung), nhãn ở dòng ô chọn kiểu đĩa, nguyên liệu và số lượng phần 2.
        private void DrawSecondPanel(Rect area, SerializedProperty cell)
        {
            bool frozen = cell.FindPropertyRelative("isFrozen").boolValue;
            float y = DrawPartIcon(area, (FoodType)cell.FindPropertyRelative("secondFoodType").intValue, frozen ? cell.FindPropertyRelative("blockValue").intValue : 0, cell.FindPropertyRelative("isHidden").boolValue);
            GUI.Label(NextLine(area, ref y), "Phần 2", EditorStyles.miniBoldLabel);
            DrawPortionFields(area, ref y, cell.FindPropertyRelative("secondFoodType"), cell.FindPropertyRelative("secondCount"));
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

        // EN: Draws the dish shape popup in the given line; a change is deferred to ChangeDishShape, which checks the placement and clears the newly covered cell.
        // VI: Vẽ ô chọn kiểu đĩa trên dòng đã cho; khi đổi sẽ hoãn sang ChangeDishShape để kiểm tra vị trí và xoá ô bị chiếm mới.
        private void DrawShapePopup(Rect line, int column, int row, SerializedProperty shape)
        {
            int picked = EditorGUI.Popup(line, shape.enumValueIndex, ShapeNames);
            if (picked != shape.enumValueIndex)
                pendingAction = () => ChangeDishShape(column, row, (DishShape)picked);
        }

        // EN: Draws one portion's ingredient popup and quantity field on the next two lines of the panel.
        // VI: Vẽ ô chọn nguyên liệu và ô số lượng của một phần trên hai dòng kế tiếp của khung.
        private static void DrawPortionFields(Rect area, ref float y, SerializedProperty type, SerializedProperty count)
        {
            type.enumValueIndex = EditorGUI.Popup(NextLine(area, ref y), type.enumValueIndex, FoodNames);
            count.intValue = Mathf.Max(1, EditorGUI.IntField(NextLine(area, ref y), count.intValue));
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

        // EN: Applies the new board size to the asset (with Undo).
        // VI: Áp dụng kích thước bảng mới vào asset (có Undo).
        private void ResizeBoard()
        {
            serialized.ApplyModifiedProperties();
            Undo.RecordObject(level, "Đổi kích thước bảng");
            level.ResizeBoard();
            EditorUtility.SetDirty(level);
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

        // EN: Refills singles and both halves of doubles from order totals on the isolated draft, preserving double layouts and shared states.
        // VI: Điền lại đĩa đơn và cả hai nửa đĩa đôi theo tổng order trên bản nháp độc lập, giữ bố cục và trạng thái chung của đĩa đôi.
        private void FillBoardFromOrders()
        {
            serialized.ApplyModifiedProperties();
            bool filled = LevelBoardOrderFiller.TryFill(level, CountOrderedFoods(), out string message);
            if (!string.IsNullOrEmpty(message))
                EditorUtility.DisplayDialog(filled ? "Đã điền bảng" : "Không điền được bảng", message, "OK");
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

        // EN: Sums the ingredients needed by every ordered final dish of both queues, by enum index (independent of numeric ID gaps).
        // VI: Cộng nguyên liệu mà mọi món hoàn chỉnh được order ở hai hàng khách cần, theo chỉ số enum (độc lập khoảng trống ID số).
        private int[] CountOrderedFoods()
        {
            int[] totals = new int[FoodNames.Length];
            FinalFoodConfig config = GetFinalFoodConfig();
            AddOrderTotals(level.leftQueue, config, totals);
            AddOrderTotals(level.rightQueue, config, totals);
            return totals;
        }

        // EN: Adds one of each recipe ingredient for every dish of every customer, ignoring unknown dishes and retired or invalid ingredient IDs.
        // VI: Cộng mỗi nguyên liệu trong công thức một phần cho từng món của từng khách, bỏ qua món không có công thức và ID nguyên liệu đã loại hoặc không hợp lệ.
        private static void AddOrderTotals(List<CustomerOrder> queue, FinalFoodConfig config, int[] totals)
        {
            if (queue == null || config == null)
                return;
            foreach (CustomerOrder order in queue)
            {
                if (order?.finalFoods == null)
                    continue;
                foreach (FinalFoodType dish in order.finalFoods)
                {
                    foreach (FoodType ingredient in config.GetIngredients(dish))
                    {
                        int type = Array.IndexOf(FoodValues, ingredient);
                        if (type > 0 && type < totals.Length)
                            totals[type]++;
                    }
                }
            }
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

        // ---------------------------------------------------------------- Helpers

        // EN: Draws a bold section title with spacing.
        // VI: Vẽ tiêu đề mục in đậm kèm khoảng cách.
        private static void DrawSectionTitle(string title)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(title, EditorStyles.largeLabel);
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

        // EN: Returns (and caches) the icon texture of a food: from FoodIconConfig first, otherwise from Art/Sprites/Foods by file name.
        // VI: Trả về (và cache) texture icon của nguyên liệu: ưu tiên lấy từ FoodIconConfig, nếu không có thì lấy theo tên file trong Art/Sprites/Foods.
        private Texture2D GetIcon(FoodType type)
        {
            if (type == FoodType.None)
                return null;
            if (!icons.TryGetValue(type, out Texture2D icon))
            {
                FoodIconConfig config = AssetDatabase.LoadAssetAtPath<FoodIconConfig>(FoodIconConfigPath);
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

        // EN: Returns (and caches) the FinalFoodConfig asset, or null when it does not exist yet.
        // VI: Trả về (và cache) asset FinalFoodConfig, hoặc null nếu chưa có.
        private FinalFoodConfig GetFinalFoodConfig()
        {
            if (finalFoodConfig == null)
                finalFoodConfig = AssetDatabase.LoadAssetAtPath<FinalFoodConfig>(FinalFoodConfigPath);
            return finalFoodConfig;
        }

        // EN: Builds popup names "EnumName (display name)" for every final dish, using the config's display names when available.
        // VI: Tạo tên cho ô chọn dạng "TênEnum (tên hiển thị)" cho mọi món hoàn chỉnh, dùng tên hiển thị trong config nếu có.
        private string[] GetFinalFoodNames()
        {
            if (finalFoodNames != null)
                return finalFoodNames;
            FinalFoodConfig config = GetFinalFoodConfig();
            finalFoodNames = new string[FinalFoodValues.Length];
            for (int i = 0; i < FinalFoodValues.Length; i++)
            {
                FinalFoodConfig.FinalFoodRecipe recipe = config != null ? config.GetRecipe(FinalFoodValues[i]) : null;
                string name = FinalFoodValues[i].ToString();
                finalFoodNames[i] = recipe != null && !string.IsNullOrEmpty(recipe.displayName) ? $"{name} ({recipe.displayName})" : name;
            }
            return finalFoodNames;
        }

        // EN: Reserves a square, draws the final dish picture (or a placeholder) with a frame, and returns its area.
        // VI: Dành ô vuông, vẽ ảnh món hoàn chỉnh (hoặc ô giữ chỗ) kèm khung, và trả về vùng hiển thị.
        private Rect DrawFinalIcon(FinalFoodType type, float size)
        {
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.18f));
            Texture2D icon = GetFinalTexture(type);
            if (icon != null)
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            return rect;
        }

        // EN: Returns (and caches) the picture of a final dish: the config sprite first, otherwise the file Final/<count>/<slug>-nigiri.png derived from the enum name.
        // VI: Trả về (và cache) ảnh món hoàn chỉnh: ưu tiên sprite trong config, nếu không có thì lấy file Final/<số món>/<slug>-nigiri.png suy ra từ tên enum.
        private Texture2D GetFinalTexture(FinalFoodType type)
        {
            if (type == FinalFoodType.None)
                return null;
            if (finalIcons.TryGetValue(type, out Texture2D icon))
                return icon;
            FinalFoodConfig config = GetFinalFoodConfig();
            Sprite sprite = config != null ? config.GetSprite(type) : null;
            icon = sprite != null ? sprite.texture : null;
            if (icon == null)
            {
                Match match = Regex.Match(type.ToString(), @"^([A-Za-z]+)Nigiri(\d)$");
                if (match.Success)
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{FinalSpriteFolder}{match.Groups[2].Value}/{match.Groups[1].Value.ToLowerInvariant()}-nigiri.png");
            }
            finalIcons[type] = icon;
            return icon;
        }

        // EN: Returns the display name of a final dish from the config, or its enum name when none is set.
        // VI: Trả về tên hiển thị của món hoàn chỉnh trong config, hoặc tên enum nếu chưa đặt.
        private string GetDishDisplayName(FinalFoodType type)
        {
            FinalFoodConfig config = GetFinalFoodConfig();
            FinalFoodConfig.FinalFoodRecipe recipe = config != null ? config.GetRecipe(type) : null;
            return recipe != null && !string.IsNullOrEmpty(recipe.displayName) ? recipe.displayName : type.ToString();
        }

        // EN: Text the dish list searches in: enum name, display name and ingredient names (tooltip content).
        // VI: Chuỗi dùng để tìm món trong danh sách: tên enum, tên hiển thị và tên nguyên liệu (nội dung tooltip).
        private string GetDishSearchText(FinalFoodType type)
        {
            return $"{type} {GetDishTooltip(type)}";
        }

        // EN: Builds a short tooltip for a final dish: its display name and ingredient list.
        // VI: Tạo tooltip ngắn cho món hoàn chỉnh: tên hiển thị và danh sách nguyên liệu.
        private string GetDishTooltip(FinalFoodType type)
        {
            FinalFoodConfig config = GetFinalFoodConfig();
            FinalFoodConfig.FinalFoodRecipe recipe = config != null ? config.GetRecipe(type) : null;
            if (recipe == null)
                return type.ToString();
            List<string> parts = new List<string>();
            foreach (FoodType ingredient in recipe.ingredients)
            {
                int index = Array.IndexOf(FoodValues, ingredient);
                parts.Add(index >= 0 && index < VietnameseNames.Length ? VietnameseNames[index] : ingredient.ToString());
            }
            string name = string.IsNullOrEmpty(recipe.displayName) ? type.ToString() : recipe.displayName;
            return $"{name}\n{string.Join(" + ", parts)}";
        }

        // EN: Opens the picture grid under the clicked dish; picking a cell writes the dish into the given property with Undo.
        // VI: Mở bảng hình ngay dưới món được bấm; chọn một ô sẽ ghi món vào property đã cho (có Undo).
        private void OpenDishPicker(Rect anchor, string propertyPath, FinalFoodType current)
        {
            PopupWindow.Show(anchor, new FinalFoodPickerPopup(this, current, WriteDishTo(propertyPath)));
        }

        // EN: Draws a dropdown button showing the current dish picture and name; it opens a searchable list where every dish has its picture and ingredient icons.
        // VI: Vẽ nút dropdown hiện ảnh và tên món đang chọn; bấm sẽ mở danh sách có ô tìm kiếm, mỗi món kèm ảnh và icon nguyên liệu.
        private void DrawDishDropdown(string propertyPath, FinalFoodType current)
        {
            int index = Mathf.Max(0, Array.IndexOf(FinalFoodValues, current));
            GUIContent label = new GUIContent(GetFinalFoodNames()[index], GetFinalTexture(current), GetDishTooltip(current));
            Rect rect = GUILayoutUtility.GetRect(label, EditorStyles.popup, GUILayout.MinWidth(150f), GUILayout.Height(22f));
            Vector2 oldIconSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(18f, 18f));
            bool open = EditorGUI.DropdownButton(rect, label, FocusType.Keyboard, EditorStyles.popup);
            EditorGUIUtility.SetIconSize(oldIconSize);
            if (open)
                PopupWindow.Show(rect, new FinalFoodListPopup(this, current, rect.width, WriteDishTo(propertyPath)));
        }

        // EN: Returns a callback that writes a picked dish into the property at the given path (with Undo) and repaints the window.
        // VI: Trả về hàm gọi lại ghi món được chọn vào property tại đường dẫn đã cho (có Undo) và vẽ lại cửa sổ.
        private Action<FinalFoodType> WriteDishTo(string propertyPath)
        {
            return picked =>
            {
                if (serialized == null)
                    return;
                serialized.Update();
                SerializedProperty property = serialized.FindProperty(propertyPath);
                if (property == null)
                    return;
                property.intValue = (int)picked;
                serialized.ApplyModifiedProperties();
                Repaint();
            };
        }

        // EN: Searchable dropdown list of every final dish: grouped by dish line, each row shows the picture, name, enum ID and ingredient icons.
        // VI: Danh sách dropdown có tìm kiếm của mọi món hoàn chỉnh: nhóm theo dòng món, mỗi dòng hiện ảnh, tên, ID enum và icon nguyên liệu.
        private sealed class FinalFoodListPopup : PopupWindowContent
        {
            private const float RowHeight = 46f;
            private const float HeaderHeight = 20f;
            private const float Thumb = 40f;
            private const float IngredientIcon = 18f;

            private readonly LevelConfigEditorWindow owner;
            private readonly FinalFoodType current;
            private readonly float width;
            private readonly Action<FinalFoodType> onPick;
            private string search = string.Empty;
            private Vector2 scroll;
            private bool scrolledToCurrent;
            private bool focusSearch = true;

            // EN: Stores the owner window, the current dish, the popup width and the pick callback.
            // VI: Lưu cửa sổ gốc, món đang chọn, độ rộng bảng và hàm gọi lại khi chọn.
            public FinalFoodListPopup(LevelConfigEditorWindow owner, FinalFoodType current, float width, Action<FinalFoodType> onPick)
            {
                this.owner = owner;
                this.current = current;
                this.width = Mathf.Max(width, 380f);
                this.onPick = onPick;
            }

            // EN: Popup size: at least as wide as the dropdown button, tall enough for about nine rows.
            // VI: Kích thước bảng: rộng ít nhất bằng nút dropdown, cao khoảng chín dòng.
            public override Vector2 GetWindowSize()
            {
                return new Vector2(width, 460f);
            }

            // EN: Draws the search field and the filtered list; Enter picks the first match, Escape closes.
            // VI: Vẽ ô tìm kiếm và danh sách đã lọc; Enter chọn món đầu tiên khớp, Escape để đóng.
            public override void OnGUI(Rect rect)
            {
                List<FinalFoodType> matches = CollectMatches();
                if (HandleKeys(matches))
                    return;
                DrawSearchField();
                scroll = GUILayout.BeginScrollView(scroll);
                int lastLine = -1;
                float y = 0f;
                float currentY = -1f;
                foreach (FinalFoodType type in matches)
                {
                    int line = (int)type / 10;
                    if (line != lastLine && string.IsNullOrEmpty(search))
                    {
                        GUILayout.Label(Regex.Replace(type.ToString(), @"Nigiri\d$", string.Empty), EditorStyles.boldLabel, GUILayout.Height(HeaderHeight));
                        y += HeaderHeight + 2f;
                    }
                    lastLine = line;
                    if (type == current)
                        currentY = y;
                    DrawRow(type);
                    y += RowHeight + 2f;
                }
                if (matches.Count == 0)
                    GUILayout.Label("Không có món nào khớp.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.EndScrollView();
                ScrollToCurrentOnce(currentY);
            }

            // EN: Draws the search box and focuses it when the popup opens.
            // VI: Vẽ ô tìm kiếm và đặt con trỏ vào đó khi bảng vừa mở.
            private void DrawSearchField()
            {
                GUI.SetNextControlName("FinalFoodSearch");
                search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                if (!focusSearch)
                    return;
                focusSearch = false;
                EditorGUI.FocusTextInControl("FinalFoodSearch");
            }

            // EN: Enter picks the first match and Escape closes the popup; returns true when the popup was closed.
            // VI: Enter chọn món khớp đầu tiên, Escape đóng bảng; trả về true khi bảng đã đóng.
            private bool HandleKeys(List<FinalFoodType> matches)
            {
                Event e = Event.current;
                if (e.type != EventType.KeyDown)
                    return false;
                if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && matches.Count > 0)
                {
                    Pick(matches[0]);
                    return true;
                }
                if (e.keyCode == KeyCode.Escape)
                {
                    editorWindow.Close();
                    return true;
                }
                return false;
            }

            // EN: Scrolls once so the current dish is visible when the list opens unfiltered.
            // VI: Cuộn một lần để món đang chọn hiện ra khi danh sách mở mà chưa lọc.
            private void ScrollToCurrentOnce(float currentY)
            {
                if (scrolledToCurrent || Event.current.type != EventType.Repaint)
                    return;
                scrolledToCurrent = true;
                if (currentY > 0f)
                {
                    scroll.y = Mathf.Max(0f, currentY - RowHeight * 2f);
                    editorWindow.Repaint();
                }
            }

            // EN: Lists the dishes whose enum name, display name or ingredient names contain the search text (case-insensitive).
            // VI: Liệt kê các món có tên enum, tên hiển thị hoặc tên nguyên liệu chứa chuỗi tìm kiếm (không phân biệt hoa thường).
            private List<FinalFoodType> CollectMatches()
            {
                List<FinalFoodType> result = new List<FinalFoodType>();
                string query = search.Trim();
                foreach (FinalFoodType type in FinalFoodValues)
                {
                    if (type == FinalFoodType.None)
                        continue;
                    if (query.Length == 0 || owner.GetDishSearchText(type).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        result.Add(type);
                }
                return result;
            }

            // EN: Draws one dish row (highlight, picture, name, enum ID, ingredient icons) and picks it on click.
            // VI: Vẽ một dòng món (tô sáng, ảnh, tên, ID enum, icon nguyên liệu) và chọn món khi bấm.
            private void DrawRow(FinalFoodType type)
            {
                Rect row = GUILayoutUtility.GetRect(width - 24f, RowHeight, GUILayout.ExpandWidth(true));
                GUILayout.Space(2f);
                bool hover = row.Contains(Event.current.mousePosition);
                if (type == current)
                    EditorGUI.DrawRect(row, new Color(0.25f, 0.6f, 1f, 0.35f));
                else if (hover)
                    EditorGUI.DrawRect(row, new Color(1f, 1f, 1f, 0.1f));

                Rect thumb = new Rect(row.x + 4f, row.y + (RowHeight - Thumb) * 0.5f, Thumb, Thumb);
                Texture2D texture = owner.GetFinalTexture(type);
                if (texture != null)
                    GUI.DrawTexture(thumb, texture, ScaleMode.ScaleToFit);
                else
                    EditorGUI.DrawRect(thumb, new Color(0.5f, 0.5f, 0.5f, 0.25f));

                float textX = thumb.xMax + 8f;
                GUI.Label(new Rect(textX, row.y + 3f, row.xMax - textX, 18f), owner.GetDishDisplayName(type), EditorStyles.boldLabel);
                GUI.Label(new Rect(textX, row.y + 21f, 120f, 16f), type.ToString(), EditorStyles.miniLabel);
                DrawIngredientIcons(new Rect(textX + 120f, row.y + 22f, row.xMax - textX - 120f, IngredientIcon), type);

                if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                    Pick(type);
                if (hover)
                    editorWindow.Repaint();
            }

            // EN: Draws the dish's ingredient icons in a row inside the given area.
            // VI: Vẽ icon nguyên liệu của món thành một hàng trong vùng đã cho.
            private void DrawIngredientIcons(Rect area, FinalFoodType type)
            {
                FinalFoodConfig config = owner.GetFinalFoodConfig();
                IReadOnlyList<FoodType> ingredients = config != null ? config.GetIngredients(type) : null;
                if (ingredients == null)
                    return;
                float x = area.x;
                foreach (FoodType ingredient in ingredients)
                {
                    if (x + IngredientIcon > area.xMax)
                        break;
                    Texture2D icon = owner.GetIcon(ingredient);
                    if (icon != null)
                        GUI.DrawTexture(new Rect(x, area.y, IngredientIcon, IngredientIcon), icon, ScaleMode.ScaleToFit);
                    x += IngredientIcon + 2f;
                }
            }

            // EN: Sends the picked dish to the owner, closes the popup and stops the current GUI pass.
            // VI: Gửi món được chọn cho cửa sổ gốc, đóng bảng và dừng lượt vẽ GUI hiện tại.
            private void Pick(FinalFoodType type)
            {
                onPick?.Invoke(type);
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        // EN: Popup grid of every final dish picture: one row per dish line, one column per ingredient count (2-6); hover shows name and ingredients.
        // VI: Bảng hình mọi món hoàn chỉnh: mỗi hàng một dòng món, mỗi cột một số nguyên liệu (2-6); rê chuột để xem tên và nguyên liệu.
        private sealed class FinalFoodPickerPopup : PopupWindowContent
        {
            private const float Cell = 64f;
            private const float LabelWidth = 76f;
            private const float Header = 22f;
            private const int MinTier = FinalFoodConfig.MinIngredients;
            private const int MaxTier = FinalFoodConfig.MaxIngredients;

            private readonly LevelConfigEditorWindow owner;
            private readonly FinalFoodType current;
            private readonly Action<FinalFoodType> onPick;
            private readonly List<int> lines = new List<int>();
            private Vector2 scroll;

            // EN: Stores the owner window (for pictures and tooltips), the current dish and the pick callback, and lists the dish lines found in the enum.
            // VI: Lưu cửa sổ gốc (để lấy ảnh và tooltip), món hiện tại và hàm gọi lại khi chọn, và liệt kê các dòng món có trong enum.
            public FinalFoodPickerPopup(LevelConfigEditorWindow owner, FinalFoodType current, Action<FinalFoodType> onPick)
            {
                this.owner = owner;
                this.current = current;
                this.onPick = onPick;
                foreach (FinalFoodType type in FinalFoodValues)
                {
                    int line = (int)type / 10;
                    if (type != FinalFoodType.None && !lines.Contains(line))
                        lines.Add(line);
                }
            }

            // EN: Size of the grid: label column plus one column per tier, header plus one row per dish line (capped to stay on screen).
            // VI: Kích thước bảng: cột tên cộng mỗi cấp một cột, dòng tiêu đề cộng mỗi dòng món một hàng (giới hạn để không tràn màn hình).
            public override Vector2 GetWindowSize()
            {
                float width = LabelWidth + (MaxTier - MinTier + 1) * (Cell + 4f) + 24f;
                float height = Header + lines.Count * (Cell + 4f) + 12f;
                return new Vector2(width, Mathf.Min(height, 620f));
            }

            // EN: Draws the tier header and one row of picture buttons per dish line; clicking a picture picks it and closes the popup.
            // VI: Vẽ tiêu đề số nguyên liệu và mỗi dòng món một hàng nút ảnh; bấm vào ảnh để chọn và đóng bảng.
            public override void OnGUI(Rect rect)
            {
                scroll = GUILayout.BeginScrollView(scroll);
                GUILayout.BeginHorizontal();
                GUILayout.Space(LabelWidth);
                for (int tier = MinTier; tier <= MaxTier; tier++)
                    GUILayout.Label($"{tier} món", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(Cell + 4f));
                GUILayout.EndHorizontal();
                foreach (int line in lines)
                    DrawLine(line);
                GUILayout.EndScrollView();
            }

            // EN: Draws one dish line: its name, then a picture button for each tier that exists in the enum.
            // VI: Vẽ một dòng món: tên dòng, rồi mỗi cấp có trong enum một nút ảnh.
            private void DrawLine(int line)
            {
                GUILayout.BeginHorizontal();
                FinalFoodType first = (FinalFoodType)(line * 10 + MinTier);
                string label = Enum.IsDefined(typeof(FinalFoodType), first) ? Regex.Replace(first.ToString(), @"Nigiri\d$", string.Empty) : line.ToString();
                GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(LabelWidth), GUILayout.Height(Cell));
                for (int tier = MinTier; tier <= MaxTier; tier++)
                {
                    FinalFoodType type = (FinalFoodType)(line * 10 + tier);
                    Rect cell = GUILayoutUtility.GetRect(Cell, Cell, GUILayout.Width(Cell), GUILayout.Height(Cell));
                    GUILayout.Space(4f);
                    if (Enum.IsDefined(typeof(FinalFoodType), type))
                        DrawCell(cell, type);
                }
                GUILayout.EndHorizontal();
            }

            // EN: Draws one picture cell, highlighting the current dish, and picks it on click.
            // VI: Vẽ một ô ảnh, tô sáng món đang chọn, và chọn món khi bấm.
            private void DrawCell(Rect cell, FinalFoodType type)
            {
                bool selected = type == current;
                bool hover = cell.Contains(Event.current.mousePosition);
                EditorGUI.DrawRect(cell, selected ? new Color(0.25f, 0.6f, 1f, 0.45f) : hover ? new Color(1f, 1f, 1f, 0.12f) : new Color(0.5f, 0.5f, 0.5f, 0.15f));
                Texture2D texture = owner.GetFinalTexture(type);
                if (texture != null)
                    GUI.DrawTexture(new Rect(cell.x + 2f, cell.y + 2f, cell.width - 4f, cell.height - 4f), texture, ScaleMode.ScaleToFit);
                if (GUI.Button(cell, new GUIContent(string.Empty, owner.GetDishTooltip(type)), GUIStyle.none))
                {
                    onPick?.Invoke(type);
                    editorWindow.Close();
                }
                if (hover)
                    editorWindow.Repaint();
            }
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
    }
}
