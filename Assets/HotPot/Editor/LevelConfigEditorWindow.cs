using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Visual level designer for LevelConfig: edit both customer queues, the raw ingredient board and check that ingredients cover the orders.
    // VI: Công cụ thiết kế level cho LevelConfig: chỉnh hai hàng khách, bảng nguyên liệu thô và kiểm tra nguyên liệu có đủ cho các order.
    public class LevelConfigEditorWindow : EditorWindow
    {
        private const string FoodSpriteFolder = "Assets/HotPot/Art/Sprites/Foods/Sushi/Raw/";
        private const string NewLevelFolder = "Assets/HotPot/Config";
        private const string FoodIconConfigPath = "Assets/HotPot/Config/FoodIconConfig.asset";
        private const string FinalFoodConfigPath = "Assets/HotPot/Config/FinalFoodConfig.asset";
        private const string FinalSpriteFolder = "Assets/HotPot/Art/Sprites/Foods/Sushi/Final/";
        private const float CellWidth = 132f;
        private const float BoardIconSize = 44f;
        private const float RowIconSize = 26f;
        private const float FinalIconSize = 48f;

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
            if (target != null)
                window.SetLevel(target);
        }

        // EN: Clears cached icons and the final-dish table so they reload after a domain reload.
        // VI: Xoá cache icon và bảng món hoàn chỉnh để nạp lại sau khi reload domain.
        private void OnEnable()
        {
            icons.Clear();
            finalIcons.Clear();
            finalFoodConfig = null;
            finalFoodNames = null;
        }

        // EN: Follows the Project selection when the user selects another LevelConfig asset.
        // VI: Tự chuyển sang LevelConfig khác khi người dùng chọn asset đó trong Project.
        private void OnSelectionChange()
        {
            if (Selection.activeObject is LevelConfig selected && selected != level)
            {
                SetLevel(selected);
                Repaint();
            }
        }

        // EN: Sets the edited level and rebuilds its SerializedObject (gives Undo/Redo for free).
        // VI: Gán level đang chỉnh và tạo lại SerializedObject (có sẵn Undo/Redo).
        private void SetLevel(LevelConfig target)
        {
            level = target;
            serialized = target != null ? new SerializedObject(target) : null;
            int number = target != null ? ParseLevelNumber(target.name) : -1;
            if (number > 0)
                levelNumberInput = number;
        }

        // EN: Draws the toolbar and two columns (customers left, ingredient board right), then runs any deferred structural change.
        // VI: Vẽ thanh công cụ và hai cột (khách hàng bên trái, bảng nguyên liệu bên phải), sau đó thực hiện thay đổi cấu trúc đã hoãn.
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

        // EN: Top bar: level picker, create, ping and save.
        // VI: Thanh trên cùng: chọn level, tạo mới, định vị trong Project và lưu.
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            LevelConfig picked = (LevelConfig)EditorGUILayout.ObjectField(level, typeof(LevelConfig), false, GUILayout.Width(280f));
            if (picked != level)
                SetLevel(picked);
            DrawLevelNavigation();
            if (GUILayout.Button("Tạo level mới", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                CreateLevel();
            using (new EditorGUI.DisabledScope(level == null))
            {
                if (GUILayout.Button("Tìm trong Project", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    EditorGUIUtility.PingObject(level);
                if (GUILayout.Button("Lưu", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                    AssetDatabase.SaveAssets();
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

        // EN: Creates LevelConfig_NN in the Config folder with an empty board and opens it.
        // VI: Tạo LevelConfig_NN trong thư mục Config với bảng trống và mở nó.
        private void CreateLevelNumber(int number)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{NewLevelFolder}/{LevelAssetName(number)}.asset");
            LevelConfig asset = CreateInstance<LevelConfig>();
            asset.ResizeBoard();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            SetLevel(asset);
            EditorGUIUtility.PingObject(asset);
        }

        // EN: Asks for a file name and creates a new LevelConfig asset with an empty board.
        // VI: Hỏi tên file và tạo asset LevelConfig mới với bảng nguyên liệu trống.
        private void CreateLevel()
        {
            string path = EditorUtility.SaveFilePanelInProject("Tạo LevelConfig", LevelAssetName(GetNumberedLevels().Count + 1), "asset", "Chọn nơi lưu level mới", NewLevelFolder);
            if (string.IsNullOrEmpty(path))
                return;
            LevelConfig asset = CreateInstance<LevelConfig>();
            asset.ResizeBoard();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            SetLevel(asset);
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

        // EN: Draws the board size fields, the column-by-column ingredient grid and helper buttons.
        // VI: Vẽ ô chỉnh kích thước bảng, lưới nguyên liệu theo từng cột và các nút hỗ trợ.
        private void DrawBoard()
        {
            DrawSectionTitle("Bảng nguyên liệu thô");
            DrawBoardSize();
            DrawBoardGrid();
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

        // EN: Draws the board as columns (left to right), each listing its foods from top to bottom.
        // VI: Vẽ bảng theo các cột (trái sang phải), mỗi cột liệt kê nguyên liệu từ trên xuống dưới.
        private void DrawBoardGrid()
        {
            SerializedProperty board = serialized.FindProperty("ingredientBoard");
            if (board.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Bảng đang trống, đổi số hàng hoặc số cột để tạo bảng.", MessageType.None);
                return;
            }
            EditorGUILayout.BeginHorizontal();
            for (int c = 0; c < board.arraySize; c++)
            {
                SerializedProperty foods = board.GetArrayElementAtIndex(c).FindPropertyRelative("foods");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(CellWidth));
                GUILayout.Label($"Cột {c + 1}", EditorStyles.centeredGreyMiniLabel);
                for (int r = 0; r < foods.arraySize; r++)
                    DrawBoardCell(foods.GetArrayElementAtIndex(r));
                EditorGUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // EN: Draws a board cell with ingredient ID, quantity, concealment and frozen-block strength controls.
        // VI: Vẽ ô board với ID nguyên liệu, số lượng, tùy chọn che và giá trị khối băng.
        private void DrawBoardCell(SerializedProperty cell)
        {
            SerializedProperty type = cell.FindPropertyRelative("foodType");
            SerializedProperty count = cell.FindPropertyRelative("requiredCount");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect iconRect = DrawIcon((FoodType)type.intValue, BoardIconSize);
            SerializedProperty frozen = cell.FindPropertyRelative("isFrozen");
            SerializedProperty block = cell.FindPropertyRelative("blockValue");
            if (frozen.boolValue)
                DrawFrozenMark(iconRect, block.intValue);
            if (cell.FindPropertyRelative("isHidden").boolValue)
                DrawHiddenMark(iconRect);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            type.enumValueIndex = EditorGUILayout.Popup(type.enumValueIndex, FoodNames);
            count.intValue = Mathf.Max(1, EditorGUILayout.IntField(count.intValue));
            EditorGUILayout.PropertyField(cell.FindPropertyRelative("isHidden"), new GUIContent("Ẩn nguyên liệu"));
            EditorGUILayout.PropertyField(frozen, new GUIContent("Đóng băng"));
            if (frozen.boolValue)
                block.intValue = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Giá trị băng"), block.intValue));
            EditorGUILayout.EndVertical();
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

        // EN: Resets every board cell to empty, visible and unfrozen food with a default count and block value of one.
        // VI: Đặt lại mọi ô board thành trống, không ẩn, không đóng băng và có số lượng cùng giá trị băng mặc định bằng một.
        private void ClearBoard()
        {
            serialized.ApplyModifiedProperties();
            Undo.RecordObject(level, "Xoá bảng nguyên liệu");
            foreach (BoardColumn column in level.ingredientBoard)
            {
                foreach (OrderFood cell in column.foods)
                {
                    cell.foodType = FoodType.None;
                    cell.requiredCount = 1;
                    cell.isHidden = false;
                    cell.isFrozen = false;
                    cell.blockValue = 1;
                }
            }
            EditorUtility.SetDirty(level);
        }

        // EN: Fills the board from order totals, splits quantities across cells and resets generated or unused cells to visible and unfrozen.
        // VI: Điền board theo tổng order, chia số lượng giữa các ô và đặt ô được tạo hoặc còn dư thành không ẩn, không đóng băng.
        private void FillBoardFromOrders()
        {
            serialized.ApplyModifiedProperties();
            Undo.RecordObject(level, "Điền bảng theo order");
            level.ResizeBoard();
            List<OrderFood> cells = new List<OrderFood>();
            foreach (BoardColumn column in level.ingredientBoard)
                cells.AddRange(column.foods);

            int[] needed = CountOrderedFoods();
            List<int> types = new List<int>();
            for (int type = 1; type < needed.Length; type++)
            {
                if (needed[type] > 0)
                    types.Add(type);
            }

            if (types.Count > cells.Count)
            {
                EditorUtility.DisplayDialog("Không đủ ô", $"Có {types.Count} loại nguyên liệu nhưng bảng chỉ có {cells.Count} ô. Hãy tăng số hàng hoặc số cột.", "OK");
                return;
            }

            int[] cellsPerType = AllocateCells(types, needed, cells.Count);
            int cellIndex = 0;
            foreach (int type in types)
                cellIndex = PlaceFoodSplit(cells, cellIndex, FoodValues[type], needed[type], cellsPerType[type]);
            for (int i = cellIndex; i < cells.Count; i++)
            {
                cells[i].foodType = FoodType.None;
                cells[i].requiredCount = 1;
                cells[i].isHidden = false;
                cells[i].isFrozen = false;
                cells[i].blockValue = 1;
            }
            EditorUtility.SetDirty(level);

            int emptyCells = cells.Count - cellIndex;
            if (emptyCells > 0)
                EditorUtility.DisplayDialog("Còn ô trống", $"Tổng order chỉ có {cellIndex} nguyên liệu nên còn {emptyCells} ô trống. Hãy thêm order hoặc giảm kích thước bảng.", "OK");
        }

        // EN: Decides how many cells each food gets: one each first, then each spare cell goes to the food with the highest amount per cell
        //     (ties: larger total, then enum order). A food never gets more cells than its total, so no cell would hold zero.
        // VI: Quyết định số ô cho mỗi loại: trước hết mỗi loại một ô, sau đó mỗi ô dư được giao cho loại có số lượng mỗi ô cao nhất
        //     (bằng nhau thì ưu tiên tổng lớn hơn, rồi theo thứ tự enum). Một loại không bao giờ có nhiều ô hơn tổng của nó, nên không ô nào bằng 0.
        private static int[] AllocateCells(List<int> types, int[] needed, int cellCount)
        {
            int[] cellsPerType = new int[needed.Length];
            foreach (int type in types)
                cellsPerType[type] = 1;
            int spare = cellCount - types.Count;
            while (spare > 0)
            {
                int best = -1;
                foreach (int type in types)
                {
                    if (cellsPerType[type] >= needed[type])
                        continue;
                    if (best < 0 || IsBetterCandidate(type, best, needed, cellsPerType))
                        best = type;
                }
                if (best < 0)
                    break;
                cellsPerType[best]++;
                spare--;
            }
            return cellsPerType;
        }

        // EN: True when food a should receive the next spare cell before food b (higher amount per cell, then larger total).
        // VI: Trả về true nếu loại a được nhận ô dư tiếp theo trước loại b (số lượng mỗi ô cao hơn, rồi tổng lớn hơn).
        private static bool IsBetterCandidate(int a, int b, int[] needed, int[] cellsPerType)
        {
            long left = (long)needed[a] * cellsPerType[b];
            long right = (long)needed[b] * cellsPerType[a];
            if (left != right)
                return left > right;
            return needed[a] > needed[b];
        }

        // EN: Writes visible, unfrozen food into consecutive cells, splitting totals evenly and returning the next free index.
        // VI: Ghi nguyên liệu không ẩn, không đóng băng vào các ô liên tiếp, chia đều tổng và trả về chỉ số trống kế tiếp.
        private static int PlaceFoodSplit(List<OrderFood> cells, int startIndex, FoodType type, int total, int cellCount)
        {
            int baseAmount = total / cellCount;
            int remainder = total % cellCount;
            for (int i = 0; i < cellCount; i++)
            {
                OrderFood cell = cells[startIndex + i];
                cell.foodType = type;
                cell.requiredCount = baseAmount + (i < remainder ? 1 : 0);
                cell.isHidden = false;
                cell.isFrozen = false;
                cell.blockValue = 1;
            }
            return startIndex + cellCount;
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
