using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FoodPop
{
    // EN: Editor tool that adds BoosterController to the BoosterController object (formerly BoosterSquare) and the matching booster component to each booster icon (recognised by its sprite name), keeps it anchored to the bottom of the screen (ScreenBottomAnchor), creates the PickupHand prompt when missing, then wires every reference.
    // VI: Công cụ editor thêm BoosterController vào object BoosterController (trước đây là BoosterSquare) và component booster tương ứng vào từng icon booster (nhận theo tên sprite), gắn ScreenBottomAnchor để bám mép dưới màn hình, tạo thông báo của PickupHand nếu chưa có, rồi gán mọi tham chiếu.
    public static class BoosterSetup
    {
        private const string RainbowMaterialPath = "Assets/HotPot/Art/Materials/LidRainbow.mat";
        private const string CancelSpritePath = "Assets/HotPot/Art/Sprites/UI/Boosters/BoosterCancel.png";
        private const string RootName = "Root";
        private const string PromptName = "PickupPrompt";
        // EN/VI: Layout of the prompt as designed by the user in the scene / Bố cục thông báo theo thiết kế người dùng trong scene.
        private const string SquareSpriteGuid = "311925a002f4447b3a28927169b83ea6";
        private static readonly Color PromptBackgroundColor = new Color(0.9056604f, 0.46969873f, 0.13243146f, 1f);
        private static readonly Vector3 BackgroundPosition = new Vector3(0f, -0.34f, 0f);
        private static readonly Vector3 BackgroundScale = new Vector3(10f, 4f, 1f);
        private static readonly Vector3 MessagePosition = Vector3.zero;
        private static readonly Vector3 CancelPosition = new Vector3(3.41f, 1.57f, 0f);
        private const float CancelScale = 0.3f;
        private const int BackgroundSortingOrder = 100;
        private const int CancelSortingOrder = 111;
        private const int MessageSortingOrder = 112;

        // EN: Runs the setup (including the bottom-of-screen anchor) on the active scene and saves it when it had no other unsaved changes.
        // VI: Chạy setup (kể cả gắn bám mép dưới màn hình) trên scene đang mở và lưu scene nếu scene không có thay đổi khác chưa lưu.
        [MenuItem("Tools/HotPot/Setup Boosters")]
        public static void Run()
        {
            Scene scene = EditorSceneManager.GetActiveScene();
            bool hadUnsavedChanges = scene.isDirty;
            GameObject square = GameObject.Find("BoosterController");
            if (square == null)
                square = GameObject.Find("BoosterSquare");
            if (square == null)
            {
                Debug.LogWarning("[BoosterSetup] Không tìm thấy object BoosterController (hoặc BoosterSquare) trong scene.");
                return;
            }
            BoosterController controller = square.GetComponent<BoosterController>();
            if (controller == null)
                controller = Undo.AddComponent<BoosterController>(square);
            if (square.GetComponent<ScreenBottomAnchor>() == null)
                Undo.AddComponent<ScreenBottomAnchor>(square);

            Transform root = square.transform.Find(RootName);
            if (root == null)
                Debug.LogWarning($"[BoosterSetup] Không tìm thấy object con '{RootName}' trong {square.name}; PickupHand sẽ ẩn từng nút booster thay vì ẩn root.", square);

            List<Booster> boosters = new List<Booster>();
            foreach (SpriteRenderer icon in square.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (IsInsidePrompt(icon.transform, square.transform))
                    continue;
                Booster booster = AddBoosterFor(icon, square.transform, root);
                if (booster != null)
                    boosters.Add(booster);
            }

            FoodBoardManager board = Object.FindFirstObjectByType<FoodBoardManager>(FindObjectsInactive.Include);
            controller.Configure(board, Camera.main, boosters, root != null ? root.gameObject : null);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!hadUnsavedChanges)
                EditorSceneManager.SaveScene(scene);
            Debug.Log($"[BoosterSetup] Đã gán {boosters.Count} booster cho BoosterController.");
        }

        // EN: True when the renderer belongs to the PickupHand prompt (its cancel button must not be treated as a booster icon).
        // VI: True khi renderer thuộc thông báo của PickupHand (nút hủy không được coi là icon booster).
        private static bool IsInsidePrompt(Transform renderer, Transform square)
        {
            Transform prompt = square.Find(PromptName);
            return prompt != null && renderer.IsChildOf(prompt);
        }

        // EN: Adds (or reuses) the booster component matching the icon's sprite name; returns null for icons without a booster class yet.
        // VI: Thêm (hoặc dùng lại) component booster ứng với tên sprite của icon; trả về null với icon chưa có lớp booster.
        private static Booster AddBoosterFor(SpriteRenderer icon, Transform square, Transform root)
        {
            string sprite = icon.sprite != null ? icon.sprite.name : string.Empty;
            if (sprite.Contains("Shuffle"))
            {
                ShuffleBooster shuffle = GetOrAdd<ShuffleBooster>(icon);
                if (shuffle.rainbowMaterial == null)
                    shuffle.rainbowMaterial = AssetDatabase.LoadAssetAtPath<Material>(RainbowMaterialPath);
                EditorUtility.SetDirty(shuffle);
                return shuffle;
            }
            if (sprite.Contains("PickupHand"))
            {
                PickupHandBooster pickup = GetOrAdd<PickupHandBooster>(icon);
                if (!pickup.HasPrompt)
                    CreatePickupPrompt(pickup, icon, square, root);
                EditorUtility.SetDirty(pickup);
                return pickup;
            }
            return null;
        }

        // EN: Returns the booster component of type T on the icon (adding it when missing), adds the ButtonPressFx click effect and assigns the icon renderer and the effect.
        // VI: Trả về component booster kiểu T trên icon (thêm nếu chưa có), thêm hiệu ứng bấm ButtonPressFx và gán renderer icon cùng hiệu ứng.
        private static T GetOrAdd<T>(SpriteRenderer icon) where T : Booster
        {
            T booster = icon.GetComponent<T>();
            if (booster == null)
                booster = Undo.AddComponent<T>(icon.gameObject);
            ButtonPressFx press = icon.GetComponent<ButtonPressFx>();
            if (press == null)
                press = Undo.AddComponent<ButtonPressFx>(icon.gameObject);
            press.Configure(icon.transform, icon);
            EditorUtility.SetDirty(press);
            booster.SetIcon(icon, press);
            return booster;
        }

        // EN: Builds the PickupPrompt object (outside the booster root, at the root's position) with a message text and a cancel button, hidden by default, and wires it to the booster. Positions and sizes are a starting point to adjust in the scene.
        // VI: Tạo object PickupPrompt (nằm ngoài root booster, tại vị trí của root) gồm text thông báo và nút hủy, mặc định ẩn, rồi gán cho booster. Vị trí và kích thước chỉ là điểm bắt đầu để chỉnh lại trong scene.
        private static void CreatePickupPrompt(PickupHandBooster pickup, SpriteRenderer icon, Transform square, Transform root)
        {
            Transform prompt = square.Find(PromptName);
            if (prompt == null)
            {
                prompt = new GameObject(PromptName).transform;
                Undo.RegisterCreatedObjectUndo(prompt.gameObject, "Create PickupPrompt");
                prompt.SetParent(square, false);
                prompt.position = root != null ? root.position : icon.transform.position;
            }
            prompt.gameObject.layer = icon.gameObject.layer;

            CreateBackground(prompt, icon);
            TextMeshPro text = CreatePromptText(prompt, icon);
            SpriteRenderer cancel = CreateCancelButton(prompt, icon);
            pickup.SetPrompt(prompt.gameObject, text, cancel);
            prompt.gameObject.SetActive(false);
        }

        // EN: Creates (or reuses) the orange background panel behind the prompt (Unity's built-in Square sprite, scaled to 10 x 4).
        // VI: Tạo (hoặc dùng lại) tấm nền cam phía sau thông báo (sprite Square có sẵn của Unity, phóng thành 10 x 4).
        private static void CreateBackground(Transform prompt, SpriteRenderer icon)
        {
            Transform existing = prompt.Find("BG");
            SpriteRenderer background = existing != null ? existing.GetComponent<SpriteRenderer>() : null;
            if (background == null)
            {
                GameObject go = new GameObject("BG");
                Undo.RegisterCreatedObjectUndo(go, "Create PickupPrompt background");
                go.transform.SetParent(prompt, false);
                go.transform.SetAsFirstSibling();
                background = go.AddComponent<SpriteRenderer>();
            }
            background.gameObject.layer = icon.gameObject.layer;
            background.transform.localPosition = BackgroundPosition;
            background.transform.localScale = BackgroundScale;
            if (background.sprite == null)
                background.sprite = LoadSpriteByGuid(SquareSpriteGuid);
            background.color = PromptBackgroundColor;
            background.sortingLayerID = icon.sortingLayerID;
            background.sortingOrder = BackgroundSortingOrder;
        }

        // EN: Loads the first sprite stored in the asset with the given GUID, or null.
        // VI: Nạp sprite đầu tiên trong asset có GUID đã cho, hoặc null.
        private static Sprite LoadSpriteByGuid(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return null;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite)
                    return sprite;
            return null;
        }

        // EN: Creates (or reuses) the message text, borrowing the font of an existing TMP text in the scene so Vietnamese glyphs match the game.
        // VI: Tạo (hoặc dùng lại) text thông báo, mượn font của một text TMP có sẵn trong scene để chữ tiếng Việt giống phần còn lại của game.
        private static TextMeshPro CreatePromptText(Transform prompt, SpriteRenderer icon)
        {
            Transform existing = prompt.Find("Message");
            TextMeshPro text = existing != null ? existing.GetComponent<TextMeshPro>() : null;
            if (text == null)
            {
                GameObject go = new GameObject("Message");
                Undo.RegisterCreatedObjectUndo(go, "Create PickupPrompt message");
                go.transform.SetParent(prompt, false);
                text = go.AddComponent<TextMeshPro>();
            }
            text.transform.localPosition = MessagePosition;
            text.gameObject.layer = icon.gameObject.layer;
            text.text = "Chọn đĩa để pickup";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 4f;
            text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(8f, 1.2f);
            text.sortingLayerID = icon.sortingLayerID;
            text.sortingOrder = MessageSortingOrder;
            TMP_Text sceneText = FindSceneFont(text);
            if (sceneText != null)
                text.font = sceneText.font;
            return text;
        }

        // EN: Returns another TMP text in the scene that has a font, or null.
        // VI: Trả về một text TMP khác trong scene có font, hoặc null.
        private static TMP_Text FindSceneFont(TMP_Text exclude)
        {
            foreach (TMP_Text candidate in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate != exclude && candidate.font != null)
                    return candidate;
            return null;
        }

        // EN: Creates (or reuses) the cancel button sprite at the top-right corner of the background panel; makes sure the cancel texture is imported as a sprite.
        // VI: Tạo (hoặc dùng lại) sprite nút hủy ở góc trên phải tấm nền; đảm bảo ảnh nút hủy được import dạng sprite.
        private static SpriteRenderer CreateCancelButton(Transform prompt, SpriteRenderer icon)
        {
            Transform existing = prompt.Find("CancelButton");
            SpriteRenderer cancel = existing != null ? existing.GetComponent<SpriteRenderer>() : null;
            if (cancel == null)
            {
                GameObject go = new GameObject("CancelButton");
                Undo.RegisterCreatedObjectUndo(go, "Create PickupPrompt cancel");
                go.transform.SetParent(prompt, false);
                cancel = go.AddComponent<SpriteRenderer>();
            }
            cancel.gameObject.layer = icon.gameObject.layer;
            cancel.transform.localPosition = CancelPosition;
            cancel.transform.localScale = Vector3.one * CancelScale;
            cancel.sprite = LoadCancelSprite();
            cancel.sortingLayerID = icon.sortingLayerID;
            cancel.sortingOrder = CancelSortingOrder;
            return cancel;
        }

        // EN: Loads the cancel button sprite, switching its importer to Sprite when needed.
        // VI: Nạp sprite nút hủy, chuyển importer sang Sprite nếu cần.
        private static Sprite LoadCancelSprite()
        {
            if (AssetImporter.GetAtPath(CancelSpritePath) is TextureImporter importer && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CancelSpritePath);
            if (sprite == null)
                Debug.LogWarning($"[BoosterSetup] Không tìm thấy sprite nút hủy tại {CancelSpritePath}.");
            return sprite;
        }
    }
}
