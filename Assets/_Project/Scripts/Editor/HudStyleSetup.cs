using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaGuard.EditorTools
{
    /// <summary>
    /// Ferramenta de editor: aplica o visual da Kenney ao HUD da cena Game, em
    /// NebulaGuard → Aplicar estilo ao HUD. Faz de uma vez o que seria muito clique à mão:
    /// bordas dos botões (9-slice), ícones e posições da barra do topo, cores, botões com a
    /// imagem da Kenney e o texto do motivo na tela de fim (já ligado no HUD).
    /// Só acrescenta e ajusta: acha os objetos pelo nome dentro do HUD_Canvas e cria apenas o
    /// que falta, então rodar de novo não duplica nada. Tudo entra no Undo (Cmd + Z desfaz).
    /// Fica numa pasta "Editor": só existe dentro do Unity e não entra no build do jogo.
    /// </summary>
    public static class HudStyleSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string UiArt = "Assets/_Project/Art/UI/";
        private const string FontPath = "Assets/_Project/Art/Fonts/kenvector_future SDF.asset";

        // Pixels de borda que não esticam nos botões da Kenney (cantos arredondados).
        private const float ButtonBorder = 12f;

        private const float IconSize = 40f;
        private static readonly Vector2 InfoTextSize = new Vector2(160f, 60f);

        // Barra transparente: os números ficam direto sobre o espaço (alfa 0).
        private static readonly Color TopBarColor = Hex("0B1530", 0);
        private static readonly Color HealthBackColor = Hex("1A2238");
        private static readonly Color Gold = Hex("FFD34A");
        private static readonly Color ReasonColor = Hex("B8C2D9");
        private static readonly Color ButtonTextColor = Hex("1A2238");

        private static readonly Vector2 MiddleLeft = new Vector2(0f, 0.5f);
        private static readonly Vector2 MiddleCenter = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 MiddleRight = new Vector2(1f, 0.5f);

        [MenuItem("NebulaGuard/Aplicar estilo ao HUD")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog("Aplicar estilo ao HUD",
                    "Vai abrir a cena Game e ajustar o HUD_Canvas: ícones e posições da barra do topo, " +
                    "cores, botões da Kenney e o texto do motivo do fim; e preencher a fonte e as " +
                    "imagens do menu de construção. A cena é salva no final " +
                    "(Cmd + Z desfaz). Continuar?", "Continuar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            PrepareUiSprites();

            var scene = EditorSceneManager.OpenScene(ScenePath);
            GameObject canvas = GameObject.Find("HUD_Canvas");
            if (canvas == null)
            {
                Debug.LogError("Não achei o HUD_Canvas na cena Game.");
                return;
            }

            Transform root = canvas.transform;
            StyleTopBar(root);
            StyleCountdown(root);
            StyleButton(root, "PausePanel/ResumeButton", "ButtonBlue");
            StyleEndPanel(root, canvas.GetComponent<HUD>());
            StyleBuildMenu();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Estilo do HUD aplicado e cena Game salva.");
        }

        // ---------- Sprites ----------

        /// <summary>
        /// Os PNGs da UI viram sprite único (Single) e os botões ganham borda: no 9-slice os
        /// cantos ficam do tamanho original e só o miolo estica, então o botão aumenta sem
        /// deformar o arredondado.
        /// </summary>
        private static void PrepareUiSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiArt.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;

                bool isButton = System.IO.Path.GetFileName(path).StartsWith("Button");
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

        // ---------- Barra do topo ----------

        private static void StyleTopBar(Transform root)
        {
            Transform bar = Find(root, "TopBar");
            if (bar == null)
                return;

            SetImageColor(bar, TopBarColor);

            Transform healthBack = Find(bar, "HealthBG");
            if (healthBack != null)
            {
                SetImageColor(healthBack, HealthBackColor);
                Place(healthBack, MiddleLeft, new Vector2(230f, 0f), new Vector2(300f, 28f));

                // Preenchimento e número ocupam exatamente o fundo da barra.
                Transform fill = healthBack.Find("HealthFill");
                if (fill != null)
                    Stretch(fill);
                Transform healthText = healthBack.Find("HealthText");
                if (healthText != null)
                {
                    Stretch(healthText);
                    StyleText(healthText, TextAlignmentOptions.Center, null);
                }
            }

            // Ícone à esquerda, número logo depois (texto alinhado à esquerda).
            Icon(bar, "IconHealth", MiddleLeft, 40f);
            Icon(bar, "IconWave", MiddleLeft, 440f);
            InfoText(bar, "WaveText", MiddleLeft, 550f, "0/3");

            Icon(bar, "IconCredits", MiddleRight, -440f);
            InfoText(bar, "CreditsText", MiddleRight, -330f, "100");
            Icon(bar, "IconScore", MiddleRight, -210f);
            InfoText(bar, "ScoreText", MiddleRight, -100f, "0");

            Transform timer = Find(bar, "TimerText");
            if (timer != null)
            {
                Place(timer, MiddleCenter, Vector2.zero, new Vector2(260f, 60f));
                StyleText(timer, TextAlignmentOptions.Center, null);
            }
        }

        private static void Icon(Transform parent, string name, Vector2 anchor, float x)
        {
            Transform icon = parent.Find(name);
            if (icon == null)
            {
                var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(obj, "Criar ícone");
                obj.transform.SetParent(parent, false);
                icon = obj.transform;
            }

            var image = icon.GetComponent<Image>();
            Undo.RecordObject(image, "Ícone");
            image.sprite = LoadSprite(name);
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false; // ícone não recebe clique

            Place(icon, anchor, new Vector2(x, 0f), Vector2.one * IconSize);
        }

        private static void InfoText(Transform parent, string name, Vector2 anchor, float x, string placeholder)
        {
            Transform text = Find(parent, name);
            if (text == null)
                return;

            Place(text, anchor, new Vector2(x, 0f), InfoTextSize);
            StyleText(text, TextAlignmentOptions.MidlineLeft, placeholder);
        }

        // ---------- Contagem, botões e tela de fim ----------

        private static void StyleCountdown(Transform root)
        {
            Transform countdown = Find(root, "CountdownText");
            if (countdown == null)
                return;

            var text = countdown.GetComponent<TMP_Text>();
            Undo.RecordObject(text, "Contagem");
            text.color = Gold;
        }

        private static void StyleButton(Transform root, string path, string spriteName)
        {
            Transform button = Find(root, path);
            if (button == null)
                return;

            var image = button.GetComponent<Image>();
            Undo.RecordObject(image, "Botão");
            image.sprite = LoadSprite(spriteName);
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var rect = (RectTransform)button;
            Undo.RecordObject(rect, "Botão");
            rect.sizeDelta = new Vector2(360f, 80f);

            // Os botões da Kenney são brancos com borda colorida: texto escuro para aparecer.
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                Undo.RecordObject(label, "Texto do botão");
                label.color = ButtonTextColor;
            }
        }

        private static void StyleEndPanel(Transform root, HUD hud)
        {
            Transform panel = Find(root, "EndPanel");
            if (panel == null)
                return;

            Transform title = Find(panel, "EndTitle");
            Transform score = Find(panel, "EndScore");
            if (title == null || score == null)
                return;

            Place(title, MiddleCenter, new Vector2(0f, 120f), new Vector2(1000f, 150f));

            // O motivo nasce como cópia do texto de pontos: herda a fonte e o alinhamento.
            Transform reason = panel.Find("EndReason");
            if (reason == null)
            {
                GameObject copy = Object.Instantiate(score.gameObject, panel);
                copy.name = "EndReason";
                Undo.RegisterCreatedObjectUndo(copy, "Criar EndReason");
                reason = copy.transform;
            }

            Place(reason, MiddleCenter, new Vector2(0f, 30f), new Vector2(1200f, 60f));
            var reasonText = reason.GetComponent<TMP_Text>();
            Undo.RecordObject(reasonText, "EndReason");
            reasonText.text = "A nave-mãe foi destruída";
            reasonText.fontSize = 36f;
            reasonText.color = ReasonColor;
            StyleText(reason, TextAlignmentOptions.Center, null);

            Place(score, MiddleCenter, new Vector2(0f, -40f), new Vector2(800f, 60f));
            var scoreText = score.GetComponent<TMP_Text>();
            Undo.RecordObject(scoreText, "EndScore");
            scoreText.color = Gold;
            StyleText(score, TextAlignmentOptions.Center, null); // sem quebra de linha: a frase do recorde é comprida

            StyleButton(root, "EndPanel/RestartButton", "ButtonGreen");
            Transform restart = Find(panel, "RestartButton");
            if (restart != null)
                Place(restart, MiddleCenter, new Vector2(0f, -160f), new Vector2(360f, 80f));

            // Liga o texto novo no campo End Reason Text do HUD.
            if (hud != null)
            {
                var serialized = new SerializedObject(hud);
                serialized.FindProperty("endReasonText").objectReferenceValue = reasonText;
                serialized.ApplyModifiedProperties();
            }
        }

        // ---------- Menu de construção ----------

        /// <summary>
        /// O menu de compra e venda é montado por código ao dar Play; aqui só preenchemos a
        /// fonte e as imagens que ele usa (campos do componente BuildMenuUI).
        /// </summary>
        private static void StyleBuildMenu()
        {
            var menu = Object.FindFirstObjectByType<BuildMenuUI>();
            if (menu == null)
            {
                Debug.LogWarning("Não achei o BuildMenuUI na cena; pulei o menu de construção.");
                return;
            }

            var serialized = new SerializedObject(menu);
            serialized.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            serialized.FindProperty("buyButtonSprite").objectReferenceValue = LoadSprite("ButtonBlue");
            serialized.FindProperty("sellButtonSprite").objectReferenceValue = LoadSprite("ButtonRed");
            serialized.FindProperty("coinIcon").objectReferenceValue = LoadSprite("IconCredits");
            serialized.ApplyModifiedProperties();
        }

        // ---------- Utilitários ----------

        private static Transform Find(Transform parent, string path)
        {
            Transform found = parent.Find(path);
            if (found == null)
                Debug.LogWarning($"Não achei {parent.name}/{path} na cena; pulei esse item.");
            return found;
        }

        /// <summary>Âncora e pivô no mesmo ponto, posição e tamanho fixos.</summary>
        private static void Place(Transform target, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)target;
            Undo.RecordObject(rect, "Posicionar");
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Estica até as bordas do pai, sem sobra (Left, Right, Top e Bottom = 0).</summary>
        private static void Stretch(Transform target)
        {
            var rect = (RectTransform)target;
            Undo.RecordObject(rect, "Esticar");
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StyleText(Transform target, TextAlignmentOptions alignment, string placeholder)
        {
            var text = target.GetComponent<TMP_Text>();
            if (text == null)
                return;

            Undo.RecordObject(text, "Texto");
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap; // número nunca quebra de linha
            if (placeholder != null)
                text.text = placeholder;
        }

        private static void SetImageColor(Transform target, Color color)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
                return;

            Undo.RecordObject(image, "Cor");
            image.color = color;
        }

        private static Color Hex(string hex, int alpha = 255)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            color.a = alpha / 255f;
            return color;
        }
    }
}
