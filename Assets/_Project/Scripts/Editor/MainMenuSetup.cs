using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaGuard.EditorTools
{
    /// <summary>
    /// Ferramenta de editor: prepara o menu principal, em NebulaGuard → Montar cena MainMenu.
    /// Faz três coisas:
    ///  1. cria a cena MainMenu (câmera, fundo de estrelas e o objeto MainMenu, com as
    ///     dificuldades de Data/, a fonte e os botões da Kenney já ligados);
    ///  2. põe MainMenu (índice 0, a primeira a abrir) e Game no Build Settings;
    ///  3. acrescenta o botão "Menu principal" às telas de pausa e de fim da cena Game e o
    ///     liga no HUD.
    /// Só acrescenta: não recria a Game.unity nem duplica nada ao rodar de novo; a MainMenu já
    /// existente é mantida e só recebe o que estiver faltando.
    /// Fica numa pasta "Editor": só existe dentro do Unity e não entra no build do jogo.
    /// </summary>
    public static class MainMenuSetup
    {
        private const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string UiArt = "Assets/_Project/Art/UI/";
        private const string FontPath = "Assets/_Project/Art/Fonts/kenvector_future SDF.asset";

        // GUID do Sprite-Unlit-Default do URP (o mesmo material do fundo da cena Game).
        private const string UnlitMaterialGuid = "9dfc825aed78fcd4ba02077103263b40";

        private const float ButtonBorder = 12f; // mesmo valor do HudStyleSetup

        // As 3 camadas do fundo: os mesmos números da cena Game, para o menu ter o mesmo céu.
        private struct StarLayer
        {
            public string name;
            public int count;
            public Vector2 size;
            public Vector2 brightness;
            public Color tint;
            public float speed;
            public int order;
            public int seed;
        }

        private static readonly StarLayer[] StarLayers =
        {
            new StarLayer { name = "Stars_Far",  count = 150, size = new Vector2(0.05f, 0.1f),  brightness = new Vector2(0.2f, 0.45f), tint = new Color(0.686f, 0.784f, 1f), speed = 0.15f, order = 0, seed = 1 },
            new StarLayer { name = "Stars_Mid",  count = 80,  size = new Vector2(0.08f, 0.15f), brightness = new Vector2(0.4f, 0.7f),  tint = Color.white, speed = 0.4f, order = 1, seed = 2 },
            new StarLayer { name = "Stars_Near", count = 30,  size = new Vector2(0.12f, 0.22f), brightness = new Vector2(0.7f, 1f),    tint = Color.white, speed = 0.9f, order = 2, seed = 3 },
        };

        [MenuItem("NebulaGuard/Montar cena MainMenu")]
        public static void Setup()
        {
            if (!EditorUtility.DisplayDialog("Montar cena MainMenu",
                    "Vai criar a cena MainMenu (se ainda não existir), colocar MainMenu e Game no Build Settings " +
                    "e acrescentar o botão \"Menu principal\" às telas de pausa e de fim da cena Game. " +
                    "As cenas são salvas no final. Continuar?", "Continuar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            PrepareButtonSprites();
            AddMenuButtonsToGame();
            BuildMenuScene();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("Menu principal pronto. Com a cena MainMenu aberta, dê Play: Jogar → dificuldade → partida; Menu principal na pausa volta para cá.");
        }

        // ---------- Sprites ----------

        // Os botões da Kenney precisam da borda do 9-slice. O HudStyleSetup já faz isso;
        // repetimos aqui para esta ferramenta funcionar mesmo se ele nunca foi rodado.
        private static void PrepareButtonSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiArt.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;

                bool isButton = Path.GetFileName(path).StartsWith("Button");
                Vector4 border = isButton ? Vector4.one * ButtonBorder : Vector4.zero;
                if (importer.spriteImportMode == SpriteImportMode.Single && importer.spriteBorder == border)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.SaveAndReimport();
            }
        }

        private static Sprite LoadSprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UiArt}{name}.png");
            if (sprite == null)
                Debug.LogWarning($"Não achei o sprite {UiArt}{name}.png");
            return sprite;
        }

        // ---------- Botões "Menu principal" na cena Game ----------

        private static void AddMenuButtonsToGame()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath);
            var hud = Object.FindFirstObjectByType<HUD>();
            if (hud == null)
            {
                Debug.LogError("Não achei o HUD na cena Game; pulei os botões Menu principal.");
                return;
            }

            // Cada botão nasce como cópia de um que já existe (herda fonte, tamanho e estilo) e
            // fica 100 unidades abaixo dele.
            Button pause = CreateMenuButton(hud.transform, "PausePanel/ResumeButton", new Vector2(0f, -100f));
            Button end = CreateMenuButton(hud.transform, "EndPanel/RestartButton", new Vector2(0f, -100f));

            var serialized = new SerializedObject(hud);
            if (pause != null)
                serialized.FindProperty("pauseMenuButton").objectReferenceValue = pause;
            if (end != null)
                serialized.FindProperty("endMenuButton").objectReferenceValue = end;
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Button CreateMenuButton(Transform hudRoot, string templatePath, Vector2 offset)
        {
            Transform template = hudRoot.Find(templatePath);
            if (template == null)
            {
                Debug.LogWarning($"Não achei {hudRoot.name}/{templatePath}; pulei esse botão.");
                return null;
            }

            Transform existing = template.parent.Find("MenuButton");
            if (existing != null)
                return existing.GetComponent<Button>();

            GameObject copy = Object.Instantiate(template.gameObject, template.parent);
            copy.name = "MenuButton";
            Undo.RegisterCreatedObjectUndo(copy, "Criar MenuButton");

            var rect = (RectTransform)copy.transform;
            rect.anchoredPosition = ((RectTransform)template).anchoredPosition + offset;
            rect.sizeDelta = new Vector2(460f, 80f); // "Menu principal" é mais comprido que "Continuar"

            var image = copy.GetComponent<Image>();
            image.sprite = LoadSprite("ButtonYellow");
            image.type = Image.Type.Sliced;

            var label = copy.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = "Menu principal";

            return copy.GetComponent<Button>();
        }

        // ---------- Cena MainMenu ----------

        private static void BuildMenuScene()
        {
            bool exists = File.Exists(MenuScenePath);
            var scene = exists
                ? EditorSceneManager.OpenScene(MenuScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (!exists)
            {
                CreateCamera();
                CreateBackground();
            }

            var menu = Object.FindFirstObjectByType<MainMenu>();
            if (menu == null)
                menu = new GameObject("MainMenu").AddComponent<MainMenu>();

            FillMenu(menu);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.AddComponent<AudioListener>();

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9f; // mesma área visível do jogo: 32 x 18 unidades
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("0B0E1A");
        }

        private static void CreateBackground()
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(UnlitMaterialGuid);
            var material = string.IsNullOrEmpty(materialPath) ? null : AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                Debug.LogWarning("Não achei o Sprite-Unlit-Default do URP: arraste-o no campo Star Material das camadas do fundo.");

            var background = new GameObject("Background");
            foreach (StarLayer layer in StarLayers)
            {
                var go = new GameObject(layer.name);
                go.transform.SetParent(background.transform, false);

                var parallax = go.AddComponent<ParallaxLayer>();
                var serialized = new SerializedObject(parallax);
                serialized.FindProperty("starMaterial").objectReferenceValue = material;
                serialized.FindProperty("starCount").intValue = layer.count;
                serialized.FindProperty("sizeRange").vector2Value = layer.size;
                serialized.FindProperty("brightnessRange").vector2Value = layer.brightness;
                serialized.FindProperty("tint").colorValue = layer.tint;
                serialized.FindProperty("speed").floatValue = layer.speed;
                serialized.FindProperty("sortingOrder").intValue = layer.order;
                serialized.FindProperty("seed").intValue = layer.seed;
                serialized.ApplyModifiedProperties();
            }
        }

        // Preenche só o que está vazio, para não apagar o que alguém ajustou no Inspector.
        private static void FillMenu(MainMenu menu)
        {
            var serialized = new SerializedObject(menu);

            SerializedProperty list = serialized.FindProperty("difficulties");
            if (list.arraySize == 0)
            {
                List<DifficultyData> found = FindDifficulties();
                list.arraySize = found.Count;
                for (int i = 0; i < found.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }

            SetIfEmpty(serialized, "font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath));
            SetIfEmpty(serialized, "greenButton", LoadSprite("ButtonGreen"));
            SetIfEmpty(serialized, "blueButton", LoadSprite("ButtonBlue"));
            SetIfEmpty(serialized, "yellowButton", LoadSprite("ButtonYellow"));
            SetIfEmpty(serialized, "redButton", LoadSprite("ButtonRed"));

            serialized.ApplyModifiedProperties();
        }

        // Todas as dificuldades de Data/, a mais fácil primeiro (mais vida na nave-mãe = mais fácil).
        private static List<DifficultyData> FindDifficulties()
        {
            var found = new List<DifficultyData>();
            foreach (string guid in AssetDatabase.FindAssets("t:DifficultyData"))
                found.Add(AssetDatabase.LoadAssetAtPath<DifficultyData>(AssetDatabase.GUIDToAssetPath(guid)));

            found.Sort((a, b) =>
            {
                int byHealth = b.MotherShipHealth.CompareTo(a.MotherShipHealth);
                return byHealth != 0 ? byHealth : string.CompareOrdinal(a.name, b.name);
            });
            return found;
        }

        private static void SetIfEmpty(SerializedObject serialized, string field, Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property.objectReferenceValue == null)
                property.objectReferenceValue = value;
        }

        // ---------- Build Settings ----------

        // O .exe abre a cena de índice 0: o menu vem primeiro, depois o jogo.
        private static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(scene => scene.path == MenuScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));

            if (!scenes.Exists(scene => scene.path == GameScenePath))
                scenes.Add(new EditorBuildSettingsScene(GameScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
