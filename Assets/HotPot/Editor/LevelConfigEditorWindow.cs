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
        private const float CellWidth = 132f;
        private const float BoardIconSize = 44f;
        private const float RowIconSize = 26f;

        private static readonly FoodType[] FoodValues = (FoodType[])Enum.GetValues(typeof(FoodType));

        // Vietnamese names follow enum declaration order, including gaps in the numeric IDs.
        private static readonly string[] VietnameseNames =
        {
            "Không có", "Cơm sushi", "Cá hồi", "Cá ngừ", "Tôm", "Thanh cua", "Trứng cuộn",
            "Dưa leo", "Trứng cá hồi", "Lươn", "Mè", "Bạch tuộc", "Đậu phụ chiên", "Sò điệp"
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

        // EN: Clears cached icons so sprites reload after a domain reload.
        // VI: Xoá cache icon để sprite được nạp lại sau khi reload domain.
        private void OnEnable()
        {
            icons.Clear();
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

        // EN: Draws one queue: its customers in serving order and a button to add a customer.
        // VI: Vẽ một hàng khách: các khách theo thứ tự phục vụ và nút thêm khách.
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

        // EN: Draws one customer card with a remove button and its ordered foods.
        // VI: Vẽ thẻ của một khách với nút xoá và các món khách order.
        private void DrawCustomer(SerializedProperty queue, string queuePath, int index)
        {
            SerializedProperty foods = queue.GetArrayElementAtIndex(index).FindPropertyRelative("foods");
            string foodsPath = foods.propertyPath;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string label = index == 0 ? $"Khách {index + 1} (phục vụ trước)" : $"Khách {index + 1}";
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(24f)))
                pendingAction = () => DeleteElement(queuePath, index);
            EditorGUILayout.EndHorizontal();

            for (int f = 0; f < foods.arraySize; f++)
                DrawOrderFoodRow(foods.GetArrayElementAtIndex(f), foodsPath, f);
            if (GUILayout.Button("+ Món", EditorStyles.miniButton))
                pendingAction = () => AddFood(foodsPath);
            EditorGUILayout.EndVertical();
        }

        // EN: Draws an order row using the actual enum value for its icon, including gaps in IDs.
        // VI: Vẽ dòng order dùng giá trị enum thực cho icon, kể cả khi ID có khoảng trống.
        private void DrawOrderFoodRow(SerializedProperty food, string foodsPath, int index)
        {
            SerializedProperty type = food.FindPropertyRelative("foodType");
            SerializedProperty count = food.FindPropertyRelative("requiredCount");
            EditorGUILayout.BeginHorizontal();
            DrawIcon((FoodType)type.intValue, RowIconSize);
            type.enumValueIndex = EditorGUILayout.Popup(type.enumValueIndex, FoodNames, GUILayout.MinWidth(150f));
            GUILayout.Label("x", GUILayout.Width(10f));
            count.intValue = Mathf.Max(1, EditorGUILayout.IntField(count.intValue, GUILayout.Width(40f)));
            if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(22f)))
                pendingAction = () => DeleteElement(foodsPath, index);
            EditorGUILayout.EndHorizontal();
        }

        // EN: Appends a customer whose order starts with one empty food row.
        // VI: Thêm một khách mới, order bắt đầu với một dòng món trống.
        private void AddCustomer(string queuePath)
        {
            SerializedProperty queue = serialized.FindProperty(queuePath);
            queue.InsertArrayElementAtIndex(queue.arraySize);
            SerializedProperty foods = queue.GetArrayElementAtIndex(queue.arraySize - 1).FindPropertyRelative("foods");
            foods.ClearArray();
            AddFood(foods.propertyPath);
        }

        // EN: Appends a food row (count 1) using the first food type not yet in that list.
        // VI: Thêm một dòng món (số lượng 1), dùng loại đồ ăn đầu tiên chưa có trong danh sách.
        private void AddFood(string foodsPath)
        {
            SerializedProperty foods = serialized.FindProperty(foodsPath);
            int nextType = FindUnusedFoodType(foods);
            foods.InsertArrayElementAtIndex(foods.arraySize);
            SerializedProperty added = foods.GetArrayElementAtIndex(foods.arraySize - 1);
            added.FindPropertyRelative("foodType").enumValueIndex = nextType;
            added.FindPropertyRelative("requiredCount").intValue = 1;
        }

        // EN: Returns the first food type (not None) missing from a food list, or SushiRice when all are used.
        // VI: Trả về loại nguyên liệu đầu tiên (khác None) chưa có trong danh sách, hoặc SushiRice nếu đã dùng hết.
        private static int FindUnusedFoodType(SerializedProperty foods)
        {
            for (int type = 1; type < FoodNames.Length; type++)
            {
                bool used = false;
                for (int i = 0; i < foods.arraySize && !used; i++)
                    used = foods.GetArrayElementAtIndex(i).FindPropertyRelative("foodType").enumValueIndex == type;
                if (!used)
                    return type;
            }
            return 1;
        }

        // EN: Removes an array element (a customer or a food row).
        // VI: Xoá một phần tử mảng (một khách hoặc một dòng món).
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

        // EN: Draws a board cell with stable ingredient IDs, quantity and a hidden-until-front toggle.
        // VI: Vẽ ô board với ID nguyên liệu cố định, số lượng và tùy chọn ẩn đến khi lên đầu cột.
        private void DrawBoardCell(SerializedProperty cell)
        {
            SerializedProperty type = cell.FindPropertyRelative("foodType");
            SerializedProperty count = cell.FindPropertyRelative("requiredCount");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawIcon((FoodType)type.intValue, BoardIconSize);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            type.enumValueIndex = EditorGUILayout.Popup(type.enumValueIndex, FoodNames);
            count.intValue = Mathf.Max(1, EditorGUILayout.IntField(count.intValue));
            EditorGUILayout.PropertyField(cell.FindPropertyRelative("isHidden"), new GUIContent("Ẩn nguyên liệu"));
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

        // EN: Resets every board cell to empty, visible food with a default count of one.
        // VI: Đặt lại mọi ô board thành trống, không ẩn và có số lượng mặc định bằng một.
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
                }
            }
            EditorUtility.SetDirty(level);
        }

        // EN: Fills the board from the order totals: every food gets at least one cell, spare cells go to the foods with the largest
        //     amount per cell; stable IDs are resolved, totals split evenly and unused cells reset to visible.
        // VI: Điền bảng theo tổng order: mỗi loại có ít nhất một ô, các ô dư được chia cho loại có số lượng mỗi ô lớn nhất để không ô nào
        //     trống; dùng ID cố định, chia đều tổng và đặt lại ô dư thành không ẩn.
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

        // EN: Writes visible food into consecutive cells, splitting totals evenly and returning the next free index.
        // VI: Ghi nguyên liệu không ẩn vào các ô liên tiếp, chia đều tổng và trả về chỉ số trống kế tiếp.
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

        // EN: Sums required counts over both queues by enum index, independently of numeric ID gaps.
        // VI: Cộng số lượng hai hàng khách theo chỉ số enum, độc lập khoảng trống ID số.
        private int[] CountOrderedFoods()
        {
            int[] totals = new int[FoodNames.Length];
            AddOrderTotals(level.leftQueue, totals);
            AddOrderTotals(level.rightQueue, totals);
            return totals;
        }

        // EN: Adds queue counts using enum indices and ignores retired or invalid ingredient IDs.
        // VI: Cộng số lượng hàng khách theo chỉ số enum và bỏ qua ID nguyên liệu đã loại hoặc không hợp lệ.
        private static void AddOrderTotals(List<CustomerOrder> queue, int[] totals)
        {
            if (queue == null)
                return;
            foreach (CustomerOrder order in queue)
            {
                if (order?.foods == null)
                    continue;
                foreach (OrderFood food in order.foods)
                {
                    int type = food != null ? Array.IndexOf(FoodValues, food.foodType) : 0;
                    if (type > 0 && type < totals.Length)
                        totals[type] += food.requiredCount;
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

        // EN: Reserves a square and draws the food icon, or a grey placeholder for None/missing art.
        // VI: Dành một ô vuông và vẽ icon nguyên liệu, hoặc ô xám khi là None/thiếu ảnh.
        private void DrawIcon(FoodType type, float size)
        {
            Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            Texture2D icon = GetIcon(type);
            if (icon != null)
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.25f));
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
