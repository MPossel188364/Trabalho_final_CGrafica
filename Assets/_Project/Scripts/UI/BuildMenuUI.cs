using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace NebulaGuard
{
    /// <summary>
    /// Menu que aparece ao lado do slot clicado, com um botão por nave (nome e custo) e o
    /// saldo de créditos. Só desenha e avisa qual botão foi apertado (OptionChosen); quem
    /// cobra e posiciona é o BuildController. O Canvas, o painel e os botões são criados
    /// por código ao iniciar, então não há nada para montar na cena.
    /// </summary>
    public class BuildMenuUI : MonoBehaviour
    {
        private const float PanelOffset = 40f;

        private static readonly Color PanelColor = new Color(0.04f, 0.07f, 0.16f, 0.92f);
        private static readonly Color ButtonColor = new Color(0.12f, 0.35f, 0.5f, 1f);

        /// <summary>Disparado com o índice da nave escolhida (posição na lista do BuildController).</summary>
        public event Action<int> OptionChosen;

        private RectTransform canvasRect;
        private RectTransform panel;
        private Text creditsLabel;
        private readonly List<Button> buttons = new List<Button>();
        private readonly List<int> costs = new List<int>();
        private int credits;

        private void Awake()
        {
            EnsureEventSystem();
            BuildCanvas();
            panel.gameObject.SetActive(false);
        }

        /// <summary>Abre o menu ao lado do ponto do mundo, com um botão por nave da lista.</summary>
        public void Show(Vector3 worldPosition, IReadOnlyList<TowerShip> towers, int currentCredits)
        {
            ClearButtons();
            for (int i = 0; i < towers.Count; i++)
                AddButton(i, towers[i].Data.DisplayName, towers[i].Data.Cost);

            credits = currentCredits;
            RefreshState();

            panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            PlaceAt(worldPosition);
        }

        public void Hide()
        {
            panel.gameObject.SetActive(false);
        }

        /// <summary>Atualiza o saldo mostrado (e quais botões estão liberados) enquanto o menu está aberto.</summary>
        public void SetCredits(int currentCredits)
        {
            credits = currentCredits;
            if (panel.gameObject.activeSelf)
                RefreshState();
        }

        private void RefreshState()
        {
            creditsLabel.text = $"Créditos: {credits}";
            for (int i = 0; i < buttons.Count; i++)
                buttons[i].interactable = credits >= costs[i];
        }

        // Posiciona o painel logo acima do slot, sem sair da tela.
        private void PlaceAt(Vector3 worldPosition)
        {
            Vector2 screen = Camera.main.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);

            Rect area = canvasRect.rect;
            Vector2 size = panel.rect.size;
            local.y += PanelOffset;
            local.x = Mathf.Clamp(local.x, area.xMin + size.x / 2f, area.xMax - size.x / 2f);
            local.y = Mathf.Clamp(local.y, area.yMin, area.yMax - size.y);
            panel.anchoredPosition = local;
        }

        // ---------- Montagem da interface por código ----------

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("BuildMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = canvasObject.GetComponent<RectTransform>();

            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelObject.transform.SetParent(canvasObject.transform, false);
            panelObject.GetComponent<Image>().color = PanelColor;

            panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f); // âncora no centro do Canvas
            panel.pivot = new Vector2(0.5f, 0f);                          // cresce para cima a partir do slot

            var layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panelObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            creditsLabel = CreateLabel(panel, "Créditos", 28, FontStyle.Bold);
        }

        private void AddButton(int index, string towerName, int cost)
        {
            var buttonObject = new GameObject($"Option_{index}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(panel, false);
            buttonObject.GetComponent<Image>().color = ButtonColor;
            var element = buttonObject.GetComponent<LayoutElement>();
            element.minHeight = 56f;
            element.minWidth = 280f; // o painel acompanha o maior filho; sem isso o nome da nave quebraria em duas linhas

            Text text = CreateLabel(buttonObject.GetComponent<RectTransform>(), $"{towerName} — {cost}", 28, FontStyle.Normal);
            text.alignment = TextAnchor.MiddleCenter;

            var button = buttonObject.GetComponent<Button>();
            int captured = index; // cópia para o botão lembrar o próprio índice
            button.onClick.AddListener(() => OptionChosen?.Invoke(captured));

            buttons.Add(button);
            costs.Add(cost);
        }

        private void ClearButtons()
        {
            foreach (Button button in buttons)
            {
                // O Destroy só acontece no fim do frame; desativar antes tira o botão
                // do cálculo de layout deste mesmo frame.
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }

            buttons.Clear();
            costs.Clear();
        }

        private static Text CreateLabel(RectTransform parent, string content, int size, FontStyle style)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(parent, false);

            var text = labelObject.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            // Estica o texto até as bordas do pai (num botão, ocupa o botão inteiro).
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
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
