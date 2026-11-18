using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace NebulaGuard
{
    /// <summary>
    /// Menu principal: Jogar (com a escolha de dificuldade), Ranking, Sobre e Sair.
    /// O Canvas, os painéis e os botões são criados por código ao dar Play, como no
    /// BuildMenuUI; no Inspector ficam só os dados (dificuldades, texto do Sobre) e a fonte e
    /// as imagens, preenchidas pela ferramenta NebulaGuard → Montar cena MainMenu.
    /// Ao escolher a dificuldade, guarda a escolha em GameSettings e carrega a cena Game.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        private static readonly Color TitleColor = new Color(0.25f, 0.88f, 1f);
        private static readonly Color TextColor = new Color(0.72f, 0.76f, 0.85f);
        private static readonly Color DarkText = new Color(0.1f, 0.13f, 0.22f); // texto sobre botão da Kenney (branco)

        [Header("Jogo")]
        [Tooltip("As dificuldades que aparecem no menu, na ordem. Cada uma é um asset de Data/.")]
        [SerializeField] private DifficultyData[] difficulties;

        [Tooltip("Nome da cena de jogo (precisa estar no Build Settings).")]
        [SerializeField] private string gameSceneName = "Game";

        [Header("Tela Sobre")]
        [TextArea(6, 14)]
        [SerializeField] private string aboutText =
            "Trabalho final de Computação Gráfica (CCC300)\n" +
            "Universidade de Passo Fundo - 2026/2\n\n" +
            "Integrantes:\n" +
            "(escreva os nomes aqui: objeto MainMenu, campo About Text)\n\n" +
            "Sprites e fonte: Kenney (kenney.nl), licença CC0";

        [Header("Visual (preenchido pela ferramenta)")]
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite greenButton;
        [SerializeField] private Sprite blueButton;
        [SerializeField] private Sprite yellowButton;
        [SerializeField] private Sprite redButton;

        private GameObject mainPanel;
        private GameObject difficultyPanel;
        private GameObject rankingPanel;
        private GameObject aboutPanel;
        private TMP_Text rankingText;

        private RectTransform canvasRect;

        private void Awake()
        {
            // Se o jogador chegou aqui pelo "Menu principal" da pausa, o tempo estava parado.
            Time.timeScale = 1f;

            EnsureEventSystem();
            BuildCanvas();
            ShowPanel(mainPanel);
        }

        // ---------- Ações dos botões ----------

        private void Play(DifficultyData difficulty)
        {
            GameSettings.SelectedDifficulty = difficulty;
            SceneManager.LoadScene(gameSceneName);
        }

        private void ShowPanel(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            difficultyPanel.SetActive(panel == difficultyPanel);
            rankingPanel.SetActive(panel == rankingPanel);
            aboutPanel.SetActive(panel == aboutPanel);

            if (panel == rankingPanel)
                RefreshRanking();
        }

        private void RefreshRanking()
        {
            var list = Ranking.GetTop();
            if (list.Count == 0)
            {
                rankingText.text = "Nenhuma partida registrada ainda.";
                return;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                RankingEntry entry = list[i];
                builder.Append(i + 1).Append(".  ").Append(entry.score)
                       .Append("  -  ").Append(entry.difficulty)
                       .Append("  -  ").Append(entry.date);
                if (i < list.Count - 1)
                    builder.Append('\n');
            }
            rankingText.text = builder.ToString();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // no editor, "sair" para o Play
#else
            Application.Quit();
#endif
        }

        // ---------- Montagem da interface por código ----------

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = canvasObject.GetComponent<RectTransform>();

            mainPanel = BuildMainPanel();
            difficultyPanel = BuildDifficultyPanel();
            rankingPanel = BuildRankingPanel();
            aboutPanel = BuildAboutPanel();
        }

        private GameObject BuildMainPanel()
        {
            GameObject panel = CreatePanel("Principal");
            CreateText(panel.transform, "SPACE DEFENSE", 130f, TitleColor);
            CreateText(panel.transform, "Defenda a nave-mãe", 44f, TextColor);
            CreateSpace(panel.transform, 50f);

            CreateButton(panel.transform, "Jogar", greenButton, () => ShowPanel(difficultyPanel));
            CreateButton(panel.transform, "Ranking", blueButton, () => ShowPanel(rankingPanel));
            CreateButton(panel.transform, "Sobre", blueButton, () => ShowPanel(aboutPanel));
            CreateButton(panel.transform, "Sair", redButton, Quit);
            return panel;
        }

        private GameObject BuildDifficultyPanel()
        {
            GameObject panel = CreatePanel("Dificuldade");
            CreateText(panel.transform, "Dificuldade", 90f, TitleColor);
            CreateSpace(panel.transform, 30f);

            if (difficulties == null || difficulties.Length == 0)
            {
                Debug.LogError("MainMenu: a lista Difficulties está vazia. Rode NebulaGuard → Montar cena MainMenu ou arraste os assets de Data/.", this);
                CreateText(panel.transform, "Nenhuma dificuldade configurada.", 40f, TextColor);
            }
            else
            {
                // Verde para a primeira (mais fácil), amarelo, vermelho; as demais, azul.
                Sprite[] colors = { greenButton, yellowButton, redButton };
                for (int i = 0; i < difficulties.Length; i++)
                {
                    DifficultyData difficulty = difficulties[i];
                    Sprite sprite = i < colors.Length ? colors[i] : blueButton;
                    string info = $"{difficulty.Waves.Count} ondas  |  nave-mãe {difficulty.MotherShipHealth} de vida  |  pontos x{difficulty.ScoreMultiplier:0.#}";
                    CreateButton(panel.transform, $"{difficulty.DisplayName}\n<size=45%>{info}</size>", sprite,
                        () => Play(difficulty), 130f, 820f);
                }
            }

            CreateSpace(panel.transform, 20f);
            CreateButton(panel.transform, "Voltar", blueButton, () => ShowPanel(mainPanel));
            return panel;
        }

        private GameObject BuildRankingPanel()
        {
            GameObject panel = CreatePanel("Ranking");
            CreateText(panel.transform, "Ranking", 90f, TitleColor);
            CreateSpace(panel.transform, 20f);

            rankingText = CreateText(panel.transform, string.Empty, 44f, Color.white);
            rankingText.GetComponent<LayoutElement>().minHeight = 340f; // a altura não pula conforme há mais linhas

            CreateSpace(panel.transform, 20f);
            CreateButton(panel.transform, "Limpar ranking", redButton, () =>
            {
                Ranking.Clear();
                RefreshRanking();
            });
            CreateButton(panel.transform, "Voltar", blueButton, () => ShowPanel(mainPanel));
            return panel;
        }

        private GameObject BuildAboutPanel()
        {
            GameObject panel = CreatePanel("Sobre");
            CreateText(panel.transform, "Sobre", 90f, TitleColor);
            CreateSpace(panel.transform, 20f);

            TMP_Text text = CreateText(panel.transform, aboutText, 38f, Color.white);
            text.textWrappingMode = TextWrappingModes.Normal;
            var element = text.GetComponent<LayoutElement>();
            element.preferredWidth = 1300f;
            element.minHeight = 380f;

            CreateSpace(panel.transform, 20f);
            CreateButton(panel.transform, "Voltar", blueButton, () => ShowPanel(mainPanel));
            return panel;
        }

        // Painel de tela cheia que empilha os filhos no centro, de cima para baixo.
        private GameObject CreatePanel(string name)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(canvasRect, false);

            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 22f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return panel;
        }

        private TMP_Text CreateText(Transform parent, string content, float size, Color color)
        {
            var textObject = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap; // título nunca quebra de linha
            text.raycastTarget = false;
            return text;
        }

        private static void CreateSpace(Transform parent, float height)
        {
            var space = new GameObject("Espaço", typeof(RectTransform), typeof(LayoutElement));
            space.transform.SetParent(parent, false);
            space.GetComponent<LayoutElement>().preferredHeight = height;
        }

        private void CreateButton(Transform parent, string label, Sprite sprite, UnityAction onClick, float height = 90f, float width = 560f)
        {
            var buttonObject = new GameObject("Botão", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced; // 9-slice: o botão cresce sem deformar os cantos
            }
            else
            {
                image.color = new Color(0.25f, 0.5f, 0.75f); // sem imagem: cor lisa
            }

            var element = buttonObject.GetComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            // O botão da Kenney é branco: texto escuro para aparecer (sem imagem, branco).
            TMP_Text text = CreateText(buttonObject.transform, label, 42f, sprite != null ? DarkText : Color.white);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Destroy(text.GetComponent<LayoutElement>()); // o botão já tem o tamanho: o texto só o preenche
        }

        // Os botões só funcionam com um EventSystem na cena. Cria um se ninguém criou ainda
        // (o projeto usa o novo Input System, então o módulo de entrada é o dele).
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
